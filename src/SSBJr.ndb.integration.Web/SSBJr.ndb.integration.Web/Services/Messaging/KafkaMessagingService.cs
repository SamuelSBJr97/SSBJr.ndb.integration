using Confluent.Kafka;
using Confluent.Kafka.Admin;
using SSBJr.ndb.integration.Web.Models;
using SSBJr.ndb.integration.Web.Services.Messaging;
using System.Collections.Concurrent;

namespace SSBJr.ndb.integration.Web.Services.Messaging;

public class KafkaMessagingService : BaseMessagingService
{
    private readonly IProducer<string, string> _producer;
    private readonly ConcurrentDictionary<string, IConsumer<string, string>> _consumers = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _consumerTokens = new();

    public KafkaMessagingService(MessagingConfig config, ILogger logger) : base(config, logger)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config.ConnectionString,
            Acks = Acks.All,
            EnableIdempotence = true,
            CompressionType = CompressionType.Snappy,
            BatchSize = config.BatchSize,
            LingerMs = 10,
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public override async Task<bool> SendMessageAsync<T>(string topic, T message, Dictionary<string, string>? headers = null)
    {
        try
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = SerializeMessage(message),
                Timestamp = new Timestamp(DateTimeOffset.UtcNow)
            };

            if (headers != null)
            {
                kafkaMessage.Headers = new Headers();
                foreach (var header in headers)
                {
                    kafkaMessage.Headers.Add(header.Key, System.Text.Encoding.UTF8.GetBytes(header.Value));
                }
            }

            var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage);
            
            IncrementMessagesSent();
            _logger.LogDebug("Message sent to Kafka topic {Topic}, partition {Partition}, offset {Offset}",
                topic, deliveryResult.Partition.Value, deliveryResult.Offset.Value);

            return true;
        }
        catch (Exception ex)
        {
            IncrementMessagesFailed();
            _logger.LogError(ex, "Failed to send message to Kafka topic {Topic}", topic);
            return false;
        }
    }

    public override async Task<bool> SendBatchAsync<T>(string topic, IEnumerable<T> messages, Dictionary<string, string>? headers = null)
    {
        try
        {
            var tasks = messages.Select(message => SendMessageAsync(topic, message, headers));
            var results = await Task.WhenAll(tasks);
            return results.All(result => result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send batch to Kafka topic {Topic}", topic);
            return false;
        }
    }

    public override async Task StartConsumerAsync<T>(string topic, Func<T, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        if (_consumers.ContainsKey(topic))
        {
            _logger.LogWarning("Consumer for topic {Topic} is already running", topic);
            return;
        }

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config.ConnectionString,
            GroupId = _config.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _consumers[topic] = consumer;
        var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _consumerTokens[topic] = tokenSource;

        consumer.Subscribe(topic);

        _ = Task.Run(async () =>
        {
            try
            {
                while (!tokenSource.Token.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(tokenSource.Token);
                    
                    if (consumeResult?.Message != null)
                    {
                        IncrementMessagesReceived();
                        
                        try
                        {
                            var message = DeserializeMessage<T>(consumeResult.Message.Value);
                            await messageHandler(message);
                            
                            consumer.Commit(consumeResult);
                            IncrementMessagesProcessed();
                        }
                        catch (Exception ex)
                        {
                            IncrementMessagesFailed();
                            _logger.LogError(ex, "Failed to process message from Kafka topic {Topic}", topic);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka consumer for topic {Topic} was cancelled", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kafka consumer for topic {Topic} encountered an error", topic);
            }
            finally
            {
                consumer.Close();
            }
        }, tokenSource.Token);

        await Task.CompletedTask;
    }

    public override async Task StopConsumerAsync(string topic)
    {
        if (_consumerTokens.TryRemove(topic, out var tokenSource))
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }

        if (_consumers.TryRemove(topic, out var consumer))
        {
            consumer.Dispose();
        }

        await Task.CompletedTask;
    }

    public override async Task<bool> CreateTopicOrQueueAsync(string name, Dictionary<string, object>? options = null)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _config.ConnectionString
            }).Build();

            var topicSpec = new TopicSpecification
            {
                Name = name,
                NumPartitions = _config.PartitionCount,
                ReplicationFactor = 1
            };

            await adminClient.CreateTopicsAsync(new[] { topicSpec });
            _logger.LogInformation("Created Kafka topic {Topic} with {Partitions} partitions", name, _config.PartitionCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Kafka topic {Topic}", name);
            return false;
        }
    }

    public override async Task<bool> DeleteTopicOrQueueAsync(string name)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _config.ConnectionString
            }).Build();

            await adminClient.DeleteTopicsAsync(new[] { name });
            _logger.LogInformation("Deleted Kafka topic {Topic}", name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Kafka topic {Topic}", name);
            return false;
        }
    }

    public override async Task<MessagingHealthStatus> GetHealthAsync()
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _config.ConnectionString
            }).Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            
            return new MessagingHealthStatus
            {
                IsHealthy = true,
                Status = "Connected",
                Details = new Dictionary<string, object>
                {
                    ["brokers"] = metadata.Brokers.Count,
                    ["topics"] = metadata.Topics.Count
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
                    ["error"] = ex.Message
                }
            };
        }
    }

    public void Dispose()
    {
        foreach (var tokenSource in _consumerTokens.Values)
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }

        foreach (var consumer in _consumers.Values)
        {
            consumer.Dispose();
        }

        _producer.Dispose();
    }
}