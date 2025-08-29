using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using SSBJr.ndb.integration.Web.Models;
using SSBJr.ndb.integration.Web.Services.Messaging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SSBJr.ndb.integration.Web.Services.Messaging;

public class AzureServiceBusMessagingService : BaseMessagingService
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusAdministrationClient _adminClient;
    private readonly ConcurrentDictionary<string, ServiceBusProcessor> _processors = new();
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    public AzureServiceBusMessagingService(MessagingConfig config, ILogger logger) : base(config, logger)
    {
        var options = new ServiceBusClientOptions
        {
            TransportType = ServiceBusTransportType.AmqpTcp,
            RetryOptions = new ServiceBusRetryOptions
            {
                MaxRetries = config.MaxRetries,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromMinutes(1),
                Mode = ServiceBusRetryMode.Exponential
            }
        };

        _client = new ServiceBusClient(config.ConnectionString, options);
        _adminClient = new ServiceBusAdministrationClient(config.ConnectionString);
    }

    public override async Task<bool> SendMessageAsync<T>(string queueOrTopic, T message, Dictionary<string, string>? headers = null)
    {
        try
        {
            var sender = GetOrCreateSender(queueOrTopic);
            
            var serviceBusMessage = new ServiceBusMessage(SerializeMessage(message))
            {
                MessageId = Guid.NewGuid().ToString(),
                ContentType = "application/json",
                TimeToLive = _config.MessageTimeout
            };

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    serviceBusMessage.ApplicationProperties[header.Key] = header.Value;
                }
            }

            await sender.SendMessageAsync(serviceBusMessage);
            
            IncrementMessagesSent();
            _logger.LogDebug("Message sent to Service Bus {QueueOrTopic} with MessageId {MessageId}",
                queueOrTopic, serviceBusMessage.MessageId);

            return true;
        }
        catch (ServiceBusException ex)
        {
            IncrementMessagesFailed();
            _logger.LogError(ex, "Service Bus error sending message to {QueueOrTopic}: {Reason}",
                queueOrTopic, ex.Reason);
            return false;
        }
        catch (Exception ex)
        {
            IncrementMessagesFailed();
            _logger.LogError(ex, "Unexpected error sending message to Service Bus {QueueOrTopic}", queueOrTopic);
            return false;
        }
    }

    public override async Task<bool> SendBatchAsync<T>(string queueOrTopic, IEnumerable<T> messages, Dictionary<string, string>? headers = null)
    {
        try
        {
            var sender = GetOrCreateSender(queueOrTopic);
            var serviceBusMessages = messages.Select(message =>
            {
                var serviceBusMessage = new ServiceBusMessage(SerializeMessage(message))
                {
                    MessageId = Guid.NewGuid().ToString(),
                    ContentType = "application/json",
                    TimeToLive = _config.MessageTimeout
                };

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        serviceBusMessage.ApplicationProperties[header.Key] = header.Value;
                    }
                }

                return serviceBusMessage;
            }).ToList();

            if (_config.EnableBatching)
            {
                // Send as batch when possible
                await sender.SendMessagesAsync(serviceBusMessages);
            }
            else
            {
                // Send individually
                foreach (var msg in serviceBusMessages)
                {
                    await sender.SendMessageAsync(msg);
                }
            }

            IncrementMessagesSent();
            _logger.LogDebug("Batch of {Count} messages sent to Service Bus {QueueOrTopic}",
                serviceBusMessages.Count, queueOrTopic);

            return true;
        }
        catch (ServiceBusException ex)
        {
            IncrementMessagesFailed();
            _logger.LogError(ex, "Service Bus error sending batch to {QueueOrTopic}: {Reason}",
                queueOrTopic, ex.Reason);
            return false;
        }
        catch (Exception ex)
        {
            IncrementMessagesFailed();
            _logger.LogError(ex, "Unexpected error sending batch to Service Bus {QueueOrTopic}", queueOrTopic);
            return false;
        }
    }

    public override async Task StartConsumerAsync<T>(string queueOrTopic, Func<T, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        if (_processors.ContainsKey(queueOrTopic))
        {
            _logger.LogWarning("Processor for {QueueOrTopic} is already running", queueOrTopic);
            return;
        }

        var processorOptions = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = Environment.ProcessorCount,
            AutoCompleteMessages = false, // Manual completion for better control
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = _config.BatchSize,
            MaxAutoLockRenewalDuration = _config.MessageTimeout
        };

        var processor = _client.CreateProcessor(queueOrTopic, processorOptions);
        _processors[queueOrTopic] = processor;

        processor.ProcessMessageAsync += async args =>
        {
            IncrementMessagesReceived();
            
            try
            {
                var messageBody = args.Message.Body.ToString();
                var message = DeserializeMessage<T>(messageBody);
                
                await messageHandler(message);
                await args.CompleteMessageAsync(args.Message);
                
                IncrementMessagesProcessed();
                _logger.LogDebug("Processed message from Service Bus {QueueOrTopic} with MessageId {MessageId}",
                    queueOrTopic, args.Message.MessageId);
            }
            catch (Exception ex)
            {
                IncrementMessagesFailed();
                _logger.LogError(ex, "Failed to process message from Service Bus {QueueOrTopic} with MessageId {MessageId}",
                    queueOrTopic, args.Message.MessageId);

                // Send to dead letter queue if max retries exceeded
                if (args.Message.DeliveryCount >= _config.MaxRetries)
                {
                    await args.DeadLetterMessageAsync(args.Message, "Max retries exceeded", ex.Message);
                }
                else
                {
                    await args.AbandonMessageAsync(args.Message);
                }
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "Service Bus processor error for {QueueOrTopic}: {Reason}",
                queueOrTopic, args.Exception.Message);
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(cancellationToken);
        _logger.LogInformation("Started Service Bus processor for {QueueOrTopic}", queueOrTopic);
    }

    public override async Task StopConsumerAsync(string queueOrTopic)
    {
        if (_processors.TryRemove(queueOrTopic, out var processor))
        {
            await processor.StopProcessingAsync();
            await processor.DisposeAsync();
            _logger.LogInformation("Stopped Service Bus processor for {QueueOrTopic}", queueOrTopic);
        }
    }

    public override async Task<bool> CreateTopicOrQueueAsync(string name, Dictionary<string, object>? options = null)
    {
        try
        {
            // Determine if it's a topic or queue based on name convention or options
            bool isTopic = options?.ContainsKey("type") == true && options["type"].ToString() == "topic";

            if (isTopic)
            {
                var topicOptions = new CreateTopicOptions(name)
                {
                    EnableBatchedOperations = _config.EnableBatching,
                    EnablePartitioning = _config.EnablePartitioning,
                    MaxSizeInMegabytes = 5120, // 5GB
                    DefaultMessageTimeToLive = _config.MessageTimeout,
                    DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(10)
                };

                await _adminClient.CreateTopicAsync(topicOptions);
                _logger.LogInformation("Created Service Bus topic {Topic}", name);
            }
            else
            {
                var queueOptions = new CreateQueueOptions(name)
                {
                    EnableBatchedOperations = _config.EnableBatching,
                    EnablePartitioning = _config.EnablePartitioning,
                    MaxSizeInMegabytes = 5120, // 5GB
                    DefaultMessageTimeToLive = _config.MessageTimeout,
                    MaxDeliveryCount = _config.MaxRetries,
                    DeadLetteringOnMessageExpiration = _config.EnableDeadLetterQueue,
                    LockDuration = TimeSpan.FromMinutes(5)
                };

                await _adminClient.CreateQueueAsync(queueOptions);
                _logger.LogInformation("Created Service Bus queue {Queue}", name);
            }

            return true;
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
        {
            _logger.LogInformation("Service Bus entity {Name} already exists", name);
            return true;
        }
        catch (ServiceBusException ex)
        {
            _logger.LogError(ex, "Service Bus error creating {Name}: {Reason}", name, ex.Reason);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating Service Bus entity {Name}", name);
            return false;
        }
    }

    public override async Task<bool> DeleteTopicOrQueueAsync(string name)
    {
        try
        {
            // Try both queue and topic deletion
            try
            {
                await _adminClient.DeleteQueueAsync(name);
                _logger.LogInformation("Deleted Service Bus queue {Queue}", name);
                return true;
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                // Try as topic
                await _adminClient.DeleteTopicAsync(name);
                _logger.LogInformation("Deleted Service Bus topic {Topic}", name);
                return true;
            }
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            _logger.LogWarning("Service Bus entity {Name} not found", name);
            return true; // Consider it successful if already doesn't exist
        }
        catch (ServiceBusException ex)
        {
            _logger.LogError(ex, "Service Bus error deleting {Name}: {Reason}", name, ex.Reason);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting Service Bus entity {Name}", name);
            return false;
        }
    }

    public override async Task<MessagingHealthStatus> GetHealthAsync()
    {
        try
        {
            // Test connection by creating a temporary receiver
            var receiver = _client.CreateReceiver("test-queue");
            await receiver.CloseAsync();

            return new MessagingHealthStatus
            {
                IsHealthy = true,
                Status = "Connected",
                Details = new Dictionary<string, object>
                {
                    ["service"] = "Azure Service Bus",
                    ["connection_state"] = "Active"
                }
            };
        }
        catch (Exception ex)
        {
            return new MessagingHealthStatus
            {
                IsHealthy = false,
                Status = "Disconnected",
                Details = new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["service"] = "Azure Service Bus"
                }
            };
        }
    }

    private ServiceBusSender GetOrCreateSender(string queueOrTopic)
    {
        return _senders.GetOrAdd(queueOrTopic, name => _client.CreateSender(name));
    }

    public void Dispose()
    {
        foreach (var processor in _processors.Values)
        {
            processor.StopProcessingAsync().GetAwaiter().GetResult();
            processor.DisposeAsync().GetAwaiter().GetResult();
        }

        foreach (var sender in _senders.Values)
        {
            sender.DisposeAsync().GetAwaiter().GetResult();
        }

        _client.DisposeAsync().GetAwaiter().GetResult();
    }
}