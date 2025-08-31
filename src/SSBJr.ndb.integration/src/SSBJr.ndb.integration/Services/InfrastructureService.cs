using SSBJr.ndb.integration.Models;
using System.Text.Json;

namespace SSBJr.ndb.integration.Services;

public class InfrastructureService : IInfrastructureService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InfrastructureService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public InfrastructureService(HttpClient httpClient, ILogger<InfrastructureService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<List<InfrastructureTemplate>> GetTemplatesAsync()
    {
        // Return predefined templates for now
        return new List<InfrastructureTemplate>
        {
            new()
            {
                Name = "Desenvolvimento Simples",
                Description = "Configuração básica para desenvolvimento local",
                Icon = "???",
                IsRecommended = true,
                Configuration = new InfrastructureConfig
                {
                    Database = new DatabaseConfig { Type = DatabaseType.PostgreSQL },
                    Messaging = new MessagingConfig { Type = MessagingType.None },
                    Cache = new CacheConfig { Type = CacheType.InMemory },
                    Storage = new StorageConfig { Type = StorageType.Local },
                    Scaling = new ScalingConfig { MinInstances = 1, MaxInstances = 2, EnableAutoScaling = false }
                }
            },
            new()
            {
                Name = "Alto Performance",
                Description = "Configuração otimizada para alto throughput",
                Icon = "??",
                Configuration = new InfrastructureConfig
                {
                    Database = new DatabaseConfig { Type = DatabaseType.PostgreSQL, EnableReadReplicas = true },
                    Messaging = new MessagingConfig { Type = MessagingType.Apache_Kafka, EnableBatching = true, BatchSize = 1000 },
                    Cache = new CacheConfig { Type = CacheType.Redis, EnableDistributed = true },
                    Storage = new StorageConfig { Type = StorageType.AWS_S3, EnableCDN = true },
                    Scaling = new ScalingConfig { MinInstances = 3, MaxInstances = 20, EnableAutoScaling = true }
                }
            },
            new()
            {
                Name = "Cloud Native",
                Description = "Configuração para ambientes cloud",
                Icon = "??",
                Configuration = new InfrastructureConfig
                {
                    Database = new DatabaseConfig { Type = DatabaseType.PostgreSQL },
                    Messaging = new MessagingConfig { Type = MessagingType.Azure_ServiceBus },
                    Cache = new CacheConfig { Type = CacheType.Redis },
                    Storage = new StorageConfig { Type = StorageType.Azure_Blob },
                    Scaling = new ScalingConfig { MinInstances = 2, MaxInstances = 15, EnableAutoScaling = true }
                }
            },
            new()
            {
                Name = "Microserviços",
                Description = "Configuração para arquitetura de microserviços",
                Icon = "??",
                Configuration = new InfrastructureConfig
                {
                    Database = new DatabaseConfig { Type = DatabaseType.MongoDB },
                    Messaging = new MessagingConfig { Type = MessagingType.NATS, EnableBatching = true },
                    Cache = new CacheConfig { Type = CacheType.Redis, EnableDistributed = true },
                    Storage = new StorageConfig { Type = StorageType.MinIO },
                    Scaling = new ScalingConfig { MinInstances = 2, MaxInstances = 10, EnableAutoScaling = true }
                }
            }
        };
    }

    public async Task<bool> ValidateInfrastructureConfigAsync(InfrastructureConfig config)
    {
        try
        {
            // Validate database configuration
            if (config.Database.Type != DatabaseType.SQLServer && string.IsNullOrEmpty(config.Database.ConnectionString))
            {
                return false;
            }

            // Validate messaging configuration
            if (config.Messaging.Type != MessagingType.None && string.IsNullOrEmpty(config.Messaging.ConnectionString))
            {
                return false;
            }

            // Validate cache configuration
            if (config.Cache.Type != CacheType.None && config.Cache.Type != CacheType.InMemory && string.IsNullOrEmpty(config.Cache.ConnectionString))
            {
                return false;
            }

            // Validate storage configuration
            if (config.Storage.Type != StorageType.None && config.Storage.Type != StorageType.Local && string.IsNullOrEmpty(config.Storage.ConnectionString))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating infrastructure configuration");
            return false;
        }
    }

    public async Task<SystemHealth> GetSystemHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/system/health");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SystemHealth>(json, _jsonOptions) ?? new SystemHealth();
            }

            return new SystemHealth
            {
                IsHealthy = false,
                LastCheck = DateTime.UtcNow,
                Services = new Dictionary<string, ServiceStatus>
                {
                    ["API"] = new ServiceStatus { IsHealthy = false, Status = "Disconnected", ErrorMessage = "Unable to connect to API service" }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching system health");
            return new SystemHealth
            {
                IsHealthy = false,
                LastCheck = DateTime.UtcNow,
                Services = new Dictionary<string, ServiceStatus>
                {
                    ["API"] = new ServiceStatus { IsHealthy = false, Status = "Error", ErrorMessage = ex.Message }
                }
            };
        }
    }

    public async Task<Dictionary<string, object>> GetInfrastructureStatusAsync(string apiId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/interfaces/{apiId}/infrastructure/status");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions) ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching infrastructure status for API {Id}", apiId);
            return new Dictionary<string, object>();
        }
    }

    public async Task<bool> TestDatabaseConnectionAsync(DatabaseConfig config)
    {
        try
        {
            var testRequest = new { Type = config.Type.ToString(), ConnectionString = config.ConnectionString };
            var json = JsonSerializer.Serialize(testRequest, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/infrastructure/test/database", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing database connection");
            return false;
        }
    }

    public async Task<bool> TestMessagingConnectionAsync(MessagingConfig config)
    {
        try
        {
            var testRequest = new { Type = config.Type.ToString(), ConnectionString = config.ConnectionString };
            var json = JsonSerializer.Serialize(testRequest, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/infrastructure/test/messaging", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing messaging connection");
            return false;
        }
    }

    public async Task<bool> TestCacheConnectionAsync(CacheConfig config)
    {
        try
        {
            var testRequest = new { Type = config.Type.ToString(), ConnectionString = config.ConnectionString };
            var json = JsonSerializer.Serialize(testRequest, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/infrastructure/test/cache", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing cache connection");
            return false;
        }
    }
}