using SSBJr.ndb.integration.Web.Models;
using System.Text.Json;

namespace SSBJr.ndb.integration.Web.Services.Messaging;

public interface IMessagingService
{
    Task<bool> SendMessageAsync<T>(string topicOrQueue, T message, Dictionary<string, string>? headers = null);
    Task<bool> SendBatchAsync<T>(string topicOrQueue, IEnumerable<T> messages, Dictionary<string, string>? headers = null);
    Task StartConsumerAsync<T>(string topicOrQueue, Func<T, Task> messageHandler, CancellationToken cancellationToken = default);
    Task StopConsumerAsync(string topicOrQueue);
    Task<bool> CreateTopicOrQueueAsync(string name, Dictionary<string, object>? options = null);
    Task<bool> DeleteTopicOrQueueAsync(string name);
    Task<MessagingHealthStatus> GetHealthAsync();
    Task<MessagingMetrics> GetMetricsAsync();
}

public class MessagingHealthStatus
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public class MessagingMetrics
{
    private long _messagesSent;
    private long _messagesReceived;
    private long _messagesProcessed;
    private long _messagesFailed;

    public long MessagesSent => _messagesSent;
    public long MessagesReceived => _messagesReceived;
    public long MessagesProcessed => _messagesProcessed;
    public long MessagesFailed => _messagesFailed;
    public double AverageProcessingTime { get; set; }
    public Dictionary<string, long> TopicMetrics { get; set; } = new();
    public Dictionary<string, long> QueueMetrics { get; set; } = new();

    public void IncrementMessagesSent() => Interlocked.Increment(ref _messagesSent);
    public void IncrementMessagesReceived() => Interlocked.Increment(ref _messagesReceived);
    public void IncrementMessagesProcessed() => Interlocked.Increment(ref _messagesProcessed);
    public void IncrementMessagesFailed() => Interlocked.Increment(ref _messagesFailed);
}

// Factory para criar serviços de mensageria
public interface IMessagingServiceFactory
{
    IMessagingService CreateMessagingService(MessagingConfig config);
}

public class MessagingServiceFactory : IMessagingServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessagingServiceFactory> _logger;

    public MessagingServiceFactory(IServiceProvider serviceProvider, ILogger<MessagingServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IMessagingService CreateMessagingService(MessagingConfig config)
    {
        return config.Type switch
        {
            MessagingType.Apache_Kafka => new KafkaMessagingService(config, _logger),
            MessagingType.Azure_ServiceBus => new AzureServiceBusMessagingService(config, _logger),
            MessagingType.AWS_SQS => new AwsSqsMessagingService(config, _logger),
            MessagingType.AWS_EventBridge => new AwsEventBridgeMessagingService(config, _logger),
            MessagingType.Google_PubSub => new GooglePubSubMessagingService(config, _logger),
            MessagingType.NATS => new NatsMessagingService(config, _logger),
            MessagingType.Apache_Pulsar => new PulsarMessagingService(config, _logger),
            MessagingType.RabbitMQ => new RabbitMqMessagingService(config, _logger),
            MessagingType.None => new NoOpMessagingService(),
            _ => throw new NotSupportedException($"Messaging type {config.Type} is not supported")
        };
    }
}

// Implementação base abstrata
public abstract class BaseMessagingService : IMessagingService
{
    protected readonly MessagingConfig _config;
    protected readonly ILogger _logger;
    protected readonly MessagingMetrics _metrics;

    protected BaseMessagingService(MessagingConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _metrics = new MessagingMetrics();
    }

    public abstract Task<bool> SendMessageAsync<T>(string topicOrQueue, T message, Dictionary<string, string>? headers = null);
    public abstract Task<bool> SendBatchAsync<T>(string topicOrQueue, IEnumerable<T> messages, Dictionary<string, string>? headers = null);
    public abstract Task StartConsumerAsync<T>(string topicOrQueue, Func<T, Task> messageHandler, CancellationToken cancellationToken = default);
    public abstract Task StopConsumerAsync(string topicOrQueue);
    public abstract Task<bool> CreateTopicOrQueueAsync(string name, Dictionary<string, object>? options = null);
    public abstract Task<bool> DeleteTopicOrQueueAsync(string name);
    public abstract Task<MessagingHealthStatus> GetHealthAsync();

    public virtual Task<MessagingMetrics> GetMetricsAsync()
    {
        return Task.FromResult(_metrics);
    }

    protected virtual string SerializeMessage<T>(T message)
    {
        return JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    protected virtual T DeserializeMessage<T>(string message)
    {
        return JsonSerializer.Deserialize<T>(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })!;
    }

    protected void IncrementMessagesSent() => _metrics.IncrementMessagesSent();
    protected void IncrementMessagesReceived() => _metrics.IncrementMessagesReceived();
    protected void IncrementMessagesProcessed() => _metrics.IncrementMessagesProcessed();
    protected void IncrementMessagesFailed() => _metrics.IncrementMessagesFailed();
}

// No-op implementation
public class NoOpMessagingService : IMessagingService
{
    public Task<bool> SendMessageAsync<T>(string topicOrQueue, T message, Dictionary<string, string>? headers = null)
        => Task.FromResult(true);

    public Task<bool> SendBatchAsync<T>(string topicOrQueue, IEnumerable<T> messages, Dictionary<string, string>? headers = null)
        => Task.FromResult(true);

    public Task StartConsumerAsync<T>(string topicOrQueue, Func<T, Task> messageHandler, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task StopConsumerAsync(string topicOrQueue)
        => Task.CompletedTask;

    public Task<bool> CreateTopicOrQueueAsync(string name, Dictionary<string, object>? options = null)
        => Task.FromResult(true);

    public Task<bool> DeleteTopicOrQueueAsync(string name)
        => Task.FromResult(true);

    public Task<MessagingHealthStatus> GetHealthAsync()
        => Task.FromResult(new MessagingHealthStatus { IsHealthy = true, Status = "No messaging configured" });

    public Task<MessagingMetrics> GetMetricsAsync()
        => Task.FromResult(new MessagingMetrics());
}