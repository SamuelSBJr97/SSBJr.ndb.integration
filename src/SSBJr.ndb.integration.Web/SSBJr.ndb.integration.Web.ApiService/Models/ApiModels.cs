namespace SSBJr.ndb.integration.Web.ApiService.Models;

public class ApiDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SwaggerJson { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public ApiStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastHealthCheck { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum ApiStatus
{
    Created,    // Equivalent to Draft in Blazor
    Validating, // New status for validation phase
    Deploying,
    Running,
    Failed,
    Stopped,
    Updating    // New status for updates
}

public class ApiDeploymentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SwaggerJson { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class ApiHealthCheck
{
    public Guid ApiId { get; set; }
    public bool IsHealthy { get; set; }
    public DateTime CheckedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
}

public class ApiInterfaceCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ApiType Type { get; set; }
    public string Version { get; set; } = "1.0.0";
    public string GraphQLSchema { get; set; } = string.Empty;
    public string SwaggerJson { get; set; } = string.Empty;
    public InfrastructureConfig Infrastructure { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();
    public MonitoringConfig Monitoring { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum ApiType
{
    GraphQL,
    REST,
    Hybrid
}

public class InfrastructureConfig
{
    public DatabaseConfig Database { get; set; } = new();
    public MessagingConfig Messaging { get; set; } = new();
    public CacheConfig Cache { get; set; } = new();
    public StorageConfig Storage { get; set; } = new();
    public ScalingConfig Scaling { get; set; } = new();
}

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool EnableReadReplicas { get; set; }
    public int MaxConnections { get; set; } = 100;
    public bool EnableMigrations { get; set; } = true;
    public Dictionary<string, object> Options { get; set; } = new();
}

public class MessagingConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = new();
    public List<string> Queues { get; set; } = new();
    public List<string> Subscriptions { get; set; } = new();
    public bool EnableDeadLetterQueue { get; set; } = true;
    public bool EnableBatching { get; set; } = true;
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 3;
    public bool EnablePartitioning { get; set; } = true;
    public int PartitionCount { get; set; } = 3;
    public string ConsumerGroup { get; set; } = "default";
    public Dictionary<string, object> Options { get; set; } = new();
}

public class CacheConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public int DefaultTTL { get; set; } = 3600;
    public bool EnableDistributed { get; set; } = true;
    public Dictionary<string, object> Options { get; set; } = new();
}

public class StorageConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public bool EnableCDN { get; set; }
    public Dictionary<string, object> Options { get; set; } = new();
}

public class ScalingConfig
{
    public int MinInstances { get; set; } = 1;
    public int MaxInstances { get; set; } = 10;
    public int TargetCPU { get; set; } = 70;
    public int TargetMemory { get; set; } = 80;
    public bool EnableAutoScaling { get; set; } = true;
    public LoadBalancerConfig LoadBalancer { get; set; } = new();
}

public class LoadBalancerConfig
{
    public string Algorithm { get; set; } = "round_robin";
    public HealthCheckConfig HealthCheck { get; set; } = new();
}

public class HealthCheckConfig
{
    public string Path { get; set; } = "/health";
    public int IntervalSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 5;
    public int HealthyThreshold { get; set; } = 2;
    public int UnhealthyThreshold { get; set; } = 3;
}

public class SecurityConfig
{
    public AuthenticationConfig Authentication { get; set; } = new();
    public AuthorizationConfig Authorization { get; set; } = new();
    public EncryptionConfig Encryption { get; set; } = new();
    public RateLimitConfig RateLimit { get; set; } = new();
    public CorsConfig Cors { get; set; } = new();
}

public class AuthenticationConfig
{
    public ApiKeyConfig? ApiKey { get; set; }
}

public class ApiKeyConfig
{
    public string HeaderName { get; set; } = string.Empty;
}

public class AuthorizationConfig
{
    public bool EnableRoleBasedAccess { get; set; }
    public bool EnablePolicyBasedAccess { get; set; }
    public List<string> RequiredRoles { get; set; } = new();
    public List<string> RequiredPolicies { get; set; } = new();
}

public class EncryptionConfig
{
    public bool EnableTLS { get; set; } = true;
    public string TLSVersion { get; set; } = "1.2";
    public bool EnableDataEncryption { get; set; }
    public string EncryptionAlgorithm { get; set; } = "AES-256";
}

public class RateLimitConfig
{
    public bool Enabled { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 100;
    public int RequestsPerHour { get; set; } = 1000;
    public int RequestsPerDay { get; set; } = 10000;
    public string Strategy { get; set; } = "sliding_window";
}

public class CorsConfig
{
    public bool Enabled { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new() { "*" };
    public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE" };
    public List<string> AllowedHeaders { get; set; } = new() { "*" };
    public bool AllowCredentials { get; set; }
}

public class MonitoringConfig
{
    public LoggingConfig Logging { get; set; } = new();
    public MetricsConfig Metrics { get; set; } = new();
    public TracingConfig Tracing { get; set; } = new();
    public AuditConfig Audit { get; set; } = new();
    public AlertingConfig Alerting { get; set; } = new();
}

public class LoggingConfig
{
    public string Level { get; set; } = "Information";
    public bool EnableStructuredLogging { get; set; } = true;
    public bool EnableRequestLogging { get; set; } = true;
    public bool EnablePerformanceLogging { get; set; } = true;
    public List<LogSink> Sinks { get; set; } = new();
}

public class LogSink
{
    public string Type { get; set; } = "Console";
    public string ConnectionString { get; set; } = string.Empty;
    public string MinimumLevel { get; set; } = "Information";
    public Dictionary<string, object> Options { get; set; } = new();
}

public class MetricsConfig
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "Prometheus";
    public string Endpoint { get; set; } = "/metrics";
    public List<string> CustomMetrics { get; set; } = new();
}

public class TracingConfig
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "OpenTelemetry";
    public string Endpoint { get; set; } = string.Empty;
    public double SamplingRate { get; set; } = 0.1;
}

public class AuditConfig
{
    public bool Enabled { get; set; } = true;
    public bool AuditRequests { get; set; } = true;
    public bool AuditResponses { get; set; } = true;
    public bool AuditDataChanges { get; set; } = true;
    public bool AuditAuthentication { get; set; } = true;
    public string StorageType { get; set; } = "Database";
    public int RetentionDays { get; set; } = 365;
}

public class AlertingConfig
{
    public bool Enabled { get; set; } = true;
    public List<AlertRule> Rules { get; set; } = new();
    public List<NotificationChannel> NotificationChannels { get; set; } = new();
}

public class AlertRule
{
    public string Name { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public int ThresholdValue { get; set; }
    public int DurationMinutes { get; set; } = 5;
}

public class NotificationChannel
{
    public string Type { get; set; } = "Email";
    public string Configuration { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}