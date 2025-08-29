using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using SSBJr.ndb.integration.Web.Models;
using SSBJr.ndb.integration.Web.Services.Messaging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SSBJr.ndb.integration.Web.Services.Messaging;

// AWS SQS Implementation
public class AwsSqsMessagingService : BaseMessagingService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ConcurrentDictionary<string, string> _queueUrls = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _consumers = new();

    public AwsSqsMessagingService(MessagingConfig config, ILogger logger) : base(config, logger)
    {
        var sqsConfig = new AmazonSQSConfig();
        
        // Parse additional options for AWS configuration
        foreach (var option in config.Options)
        {
            switch (option.Key.ToLowerInvariant())
            {
                case "region":
                    sqsConfig.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(option.Value.ToString()!);
                    break;
                case "service_url":
                    sqsConfig.ServiceURL = option.Value.ToString();
                    break;
            }
        }

        _sqsClient = new AmazonSQSClient(sqsConfig);
    }

    public override async Task<bool> SendMessageAsync<T>(string queueName, T message, Dictionary<string, string>? headers = null)
    {
        try
        {
            var queueUrl = await GetQueueUrlAsync(queueName);
            if (queueUrl == null) return false;

            var request = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = SerializeMessage(message),
                DelaySeconds = 0
            };

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.MessageAttributes[header.Key] = new MessageAttributeValue
                    {
                        StringValue = header.Value,
                        DataType = "String"
                    };
                }
            }

            await _sqsClient.SendMessageAsync(request);
            IncrementMessagesSent();
            
            return true;
        }
        catch (Exception ex)
        {
            IncrementMessagesFailed();
            _logger.LogError(ex, "Failed to send message to SQS queue {Queue}", queueName);
            return false;
        }
    }

    public override async Task<bool> SendBatchAsync<T>(string queueName, IEnumerable<T> messages, Dictionary<string, string>? headers = null)
    {
        try
        {
            var queueUrl = await GetQueueUrlAsync(queueName);
            if (queueUrl == null) return false;

            var messageList = messages.ToList();
            var batchSize = Math.Min(_config.BatchSize, 10); // SQS batch limit is 10

            for (int i = 0; i < messageList.Count; i += batchSize)
            {
                var batch = messageList.Skip(i).Take(batchSize).ToList();
                var entries = batch.Select((msg, index) => new SendMessageBatchRequestEntry
                {
                    Id = $"msg_{i + index}",
                    MessageBody = SerializeMessage(msg)
                }).ToList();

                var request = new SendMessageBatchRequest
                {
                    QueueUrl = queueUrl,
                    Entries = entries
                };

                await _sqsClient.SendMessageBatchAsync(request);
            }

            IncrementMessagesSent();
            return true;
        }
        catch (Exception ex)
        {
            IncrementMessagesFailed();
            _logger.LogError(ex, "Failed to send batch to SQS queue {Queue}", queueName);
            return false;
        }
    }

    public override async Task StartConsumerAsync<T>(string queueName, Func<T, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        if (_consumers.ContainsKey(queueName)) return;

        var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _consumers[queueName] = tokenSource;

        var queueUrl = await GetQueueUrlAsync(queueName);
        if (queueUrl == null) return;

        _ = Task.Run(async () =>
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var request = new ReceiveMessageRequest
                    {
                        QueueUrl = queueUrl,
                        MaxNumberOfMessages = _config.BatchSize,
                        WaitTimeSeconds = 20, // Long polling
                        MessageAttributeNames = new List<string> { "All" }
                    };

                    var response = await _sqsClient.ReceiveMessageAsync(request, tokenSource.Token);

                    foreach (var msg in response.Messages)
                    {
                        IncrementMessagesReceived();

                        try
                        {
                            var message = DeserializeMessage<T>(msg.Body);
                            await messageHandler(message);

                            await _sqsClient.DeleteMessageAsync(queueUrl, msg.ReceiptHandle);
                            IncrementMessagesProcessed();
                        }
                        catch (Exception ex)
                        {
                            IncrementMessagesFailed();
                            _logger.LogError(ex, "Failed to process SQS message from queue {Queue}", queueName);
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SQS consumer error for queue {Queue}", queueName);
                    await Task.Delay(5000, tokenSource.Token); // Wait before retrying
                }
            }
        }, tokenSource.Token);
    }

    public override async Task StopConsumerAsync(string queueName)
    {
        if (_consumers.TryRemove(queueName, out var tokenSource))
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
        await Task.CompletedTask;
    }

    public override async Task<bool> CreateTopicOrQueueAsync(string name, Dictionary<string, object>? options = null)
    {
        try
        {
            var request = new CreateQueueRequest
            {
                QueueName = name
            };

            if (options != null)
            {
                foreach (var option in options)
                {
                    request.Attributes[option.Key] = option.Value.ToString()!;
                }
            }

            var response = await _sqsClient.CreateQueueAsync(request);
            _queueUrls[name] = response.QueueUrl;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SQS queue {Queue}", name);
            return false;
        }
    }

    public override async Task<bool> DeleteTopicOrQueueAsync(string name)
    {
        try
        {
            var queueUrl = await GetQueueUrlAsync(name);
            if (queueUrl != null)
            {
                await _sqsClient.DeleteQueueAsync(queueUrl);
                _queueUrls.TryRemove(name, out _);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete SQS queue {Queue}", name);
            return false;
        }
    }

    public override async Task<MessagingHealthStatus> GetHealthAsync()
    {
        try
        {
            await _sqsClient.ListQueuesAsync(new ListQueuesRequest { MaxResults = 1 });
            return new MessagingHealthStatus
            {
                IsHealthy = true,
                Status = "Connected",
                Details = new Dictionary<string, object> { ["service"] = "AWS SQS" }
            };
        }
        catch (Exception ex)
        {
            return new MessagingHealthStatus
            {
                IsHealthy = false,
                Status = "Error",
                Details = new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["service"] = "AWS SQS"
                }
            };
        }
    }

    private async Task<string?> GetQueueUrlAsync(string queueName)
    {
        if (_queueUrls.TryGetValue(queueName, out var cachedUrl))
            return cachedUrl;

        try
        {
            var response = await _sqsClient.GetQueueUrlAsync(queueName);
            _queueUrls[queueName] = response.QueueUrl;
            return response.QueueUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SQS queue URL for {Queue}", queueName);
            return null;
        }
    }

    public void Dispose()
    {
        foreach (var tokenSource in _consumers.Values)
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
        _sqsClient.Dispose();
    }
}

// AWS EventBridge Implementation
public class AwsEventBridgeMessagingService : BaseMessagingService
{
    private readonly IAmazonEventBridge _eventBridgeClient;

    public AwsEventBridgeMessagingService(MessagingConfig config, ILogger logger) : base(config, logger)
    {
        var eventBridgeConfig = new AmazonEventBridgeConfig();
        
        foreach (var option in config.Options)
        {
            if (option.Key.ToLowerInvariant() == "region")
                eventBridgeConfig.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(option.Value.ToString()!);
        }

        _eventBridgeClient = new AmazonEventBridgeClient(eventBridgeConfig);
    }

    public override async Task<bool> SendMessageAsync<T>(string eventBusName, T message, Dictionary<string, string>? headers = null)
    {
        try
        {
            var request = new PutEventsRequest
            {
                Entries = new List<PutEventsRequestEntry>
                {
                    new PutEventsRequestEntry
                    {
                        Source = "ssbjr.api.manager",
                        DetailType = typeof(T).Name,
                        Detail = SerializeMessage(message),
                        EventBusName = eventBusName
                    }
                }
            };

            await _eventBridgeClient.PutEventsAsync(request);
            IncrementMessagesSent();
            return true;
        }
        catch (Exception ex)
        {
            IncrementMessagesFailed();
            _logger.LogError(ex, "Failed to send event to EventBridge bus {Bus}", eventBusName);
            return false;
        }
    }

    public override Task<bool> SendBatchAsync<T>(string eventBusName, IEnumerable<T> messages, Dictionary<string, string>? headers = null)
    {
        // EventBridge supports batch natively
        return SendMessageAsync(eventBusName, messages, headers);
    }

    public override Task StartConsumerAsync<T>(string eventBusName, Func<T, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        // EventBridge uses rules and targets, not direct consumers
        _logger.LogWarning("EventBridge consumers are configured through rules and targets, not direct subscriptions");
        return Task.CompletedTask;
    }

    public override Task StopConsumerAsync(string eventBusName)
    {
        return Task.CompletedTask;
    }

    public override async Task<bool> CreateTopicOrQueueAsync(string name, Dictionary<string, object>? options = null)
    {
        try
        {
            await _eventBridgeClient.CreateEventBusAsync(new CreateEventBusRequest
            {
                Name = name
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create EventBridge bus {Bus}", name);
            return false;
        }
    }

    public override async Task<bool> DeleteTopicOrQueueAsync(string name)
    {
        try
        {
            await _eventBridgeClient.DeleteEventBusAsync(new DeleteEventBusRequest
            {
                Name = name
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete EventBridge bus {Bus}", name);
            return false;
        }
    }

    public override async Task<MessagingHealthStatus> GetHealthAsync()
    {
        try
        {
            await _eventBridgeClient.ListEventBusesAsync(new ListEventBusesRequest { Limit = 1 });
            return new MessagingHealthStatus
            {
                IsHealthy = true,
                Status = "Connected",
                Details = new Dictionary<string, object> { ["service"] = "AWS EventBridge" }
            };
        }
        catch (Exception ex)
        {
            return new MessagingHealthStatus
            {
                IsHealthy = false,
                Status = "Error",
                Details = new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["service"] = "AWS EventBridge"
                }
            };
        }
    }

    public void Dispose()
    {
        _eventBridgeClient.Dispose();
    }
}