using Docker.DotNet;
using Docker.DotNet.Models;
using SSBJr.ndb.integration.Web.Models;

namespace SSBJr.ndb.integration.Web.Services;

public interface IInfrastructureService
{
    Task SetupAsync(ApiInterface apiInterface);
    Task CleanupAsync(ApiInterface apiInterface);
    Task<bool> HealthCheckAsync(ApiInterface apiInterface);
    Task<Dictionary<string, object>> GetInfrastructureStatusAsync(ApiInterface apiInterface);
}

public class InfrastructureService : IInfrastructureService
{
    private readonly DockerClient _dockerClient;
    private readonly ILogger<InfrastructureService> _logger;

    public InfrastructureService(ILogger<InfrastructureService> logger)
    {
        _logger = logger;
        _dockerClient = new DockerClientConfiguration().CreateClient();
    }

    public async Task SetupAsync(ApiInterface apiInterface)
    {
        _logger.LogInformation("Setting up infrastructure for API {Name}", apiInterface.Name);

        try
        {
            // Setup database
            await SetupDatabaseAsync(apiInterface);

            // Setup messaging
            await SetupMessagingAsync(apiInterface);

            // Setup cache
            await SetupCacheAsync(apiInterface);

            // Setup monitoring
            await SetupMonitoringAsync(apiInterface);

            _logger.LogInformation("Infrastructure setup completed for API {Name}", apiInterface.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup infrastructure for API {Name}", apiInterface.Name);
            throw;
        }
    }

    public async Task CleanupAsync(ApiInterface apiInterface)
    {
        _logger.LogInformation("Cleaning up infrastructure for API {Name}", apiInterface.Name);

        try
        {
            var containerPrefix = $"ssbjr-{apiInterface.Name.ToLowerInvariant().Replace(" ", "-")}";
            
            // Get all containers with our prefix
            var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { [containerPrefix] = true }
                }
            });

            // Stop and remove containers
            foreach (var container in containers)
            {
                try
                {
                    await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters
                    {
                        WaitBeforeKillSeconds = 30
                    });
                    
                    await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters
                    {
                        Force = true
                    });

                    _logger.LogInformation("Removed container {ContainerName} ({ContainerId})", 
                        container.Names.FirstOrDefault(), container.ID);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove container {ContainerId}", container.ID);
                }
            }

            // Remove networks
            await CleanupNetworksAsync(containerPrefix);

            // Remove volumes
            await CleanupVolumesAsync(containerPrefix);

            _logger.LogInformation("Infrastructure cleanup completed for API {Name}", apiInterface.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup infrastructure for API {Name}", apiInterface.Name);
            throw;
        }
    }

    public async Task<bool> HealthCheckAsync(ApiInterface apiInterface)
    {
        try
        {
            var containerPrefix = $"ssbjr-{apiInterface.Name.ToLowerInvariant().Replace(" ", "-")}";
            
            var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { [containerPrefix] = true },
                    ["status"] = new Dictionary<string, bool> { ["running"] = true }
                }
            });

            return containers.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for API {Name}", apiInterface.Name);
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetInfrastructureStatusAsync(ApiInterface apiInterface)
    {
        var status = new Dictionary<string, object>();

        try
        {
            var containerPrefix = $"ssbjr-{apiInterface.Name.ToLowerInvariant().Replace(" ", "-")}";
            
            var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { [containerPrefix] = true }
                }
            });

            var containerStatuses = new List<object>();
            foreach (var container in containers)
            {
                containerStatuses.Add(new
                {
                    Id = container.ID,
                    Name = container.Names.FirstOrDefault(),
                    Status = container.Status,
                    State = container.State,
                    Image = container.Image,
                    Created = container.Created
                });
            }

            status["containers"] = containerStatuses;
            status["total_containers"] = containers.Count;
            status["running_containers"] = containers.Count(c => c.State == "running");
            status["timestamp"] = DateTime.UtcNow;

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get infrastructure status for API {Name}", apiInterface.Name);
            status["error"] = ex.Message;
            return status;
        }
    }

    private async Task SetupDatabaseAsync(ApiInterface apiInterface)
    {
        if (apiInterface.Infrastructure.Database.Type == DatabaseType.Redis)
            return; // Skip if using Redis as primary database

        var dbConfig = apiInterface.Infrastructure.Database;
        var containerName = $"ssbjr-{apiInterface.Name.ToLowerInvariant().Replace(" ", "-")}-db";

        try
        {
            // Check if database container already exists
            var existingContainers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { [containerName] = true }
                }
            });

            if (existingContainers.Any())
            {
                _logger.LogInformation("Database container {ContainerName} already exists", containerName);
                return;
            }

            var (imageName, envVars, ports) = GetDatabaseConfig(dbConfig.Type);

            // Pull image if not exists
            await _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters
            {
                FromImage = imageName
            }, null, new Progress<JSONMessage>());

            // Create container
            var createResponse = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = imageName,
                Name = containerName,
                Env = envVars,
                ExposedPorts = ports.ToDictionary(p => $"{p}/tcp", _ => default(EmptyStruct)),
                HostConfig = new HostConfig
                {
                    PortBindings = ports.ToDictionary(p => $"{p}/tcp", p => (IList<PortBinding>)new List<PortBinding>
                    {
                        new() { HostPort = GetAvailablePort().ToString() }
                    }),
                    RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
                },
                Labels = new Dictionary<string, string>
                {
                    ["ssbjr.service"] = "database",
                    ["ssbjr.api"] = apiInterface.Name
                }
            });

            // Start container
            await _dockerClient.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters());

            _logger.LogInformation("Database container {ContainerName} created and started", containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup database for API {Name}", apiInterface.Name);
            throw;
        }
    }

    private async Task SetupMessagingAsync(ApiInterface apiInterface)
    {
        var messagingConfig = apiInterface.Infrastructure.Messaging;
        if (messagingConfig.Type == MessagingType.None)
            return;

        var containerName = $"ssbjr-{apiInterface.Name.ToLowerInvariant().Replace(" ", "-")}-messaging";

        try
        {
            var (imageName, envVars, ports) = GetMessagingConfig(messagingConfig.Type);

            // Create container
            var createResponse = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = imageName,
                Name = containerName,
                Env = envVars,
                ExposedPorts = ports.ToDictionary(p => $"{p}/tcp", _ => default(EmptyStruct)),
                HostConfig = new HostConfig
                {
                    PortBindings = ports.ToDictionary(p => $"{p}/tcp", p => (IList<PortBinding>)new List<PortBinding>
                    {
                        new() { HostPort = GetAvailablePort().ToString() }
                    }),
                    RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
                },
                Labels = new Dictionary<string, string>
                {
                    ["ssbjr.service"] = "messaging",
                    ["ssbjr.api"] = apiInterface.Name
                }
            });

            await _dockerClient.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters());

            _logger.LogInformation("Messaging container {ContainerName} created and started", containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup messaging for API {Name}", apiInterface.Name);
            throw;
        }
    }

    private async Task SetupCacheAsync(ApiInterface apiInterface)
    {
        var cacheConfig = apiInterface.Infrastructure.Cache;
        if (cacheConfig.Type == CacheType.None || cacheConfig.Type == CacheType.InMemory)
            return;

        var containerName = $"ssbjr-{apiInterface.Name.ToLowerInvariant().Replace(" ", "-")}-cache";

        try
        {
            var (imageName, envVars, ports) = GetCacheConfig(cacheConfig.Type);

            var createResponse = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = imageName,
                Name = containerName,
                Env = envVars,
                ExposedPorts = ports.ToDictionary(p => $"{p}/tcp", _ => default(EmptyStruct)),
                HostConfig = new HostConfig
                {
                    PortBindings = ports.ToDictionary(p => $"{p}/tcp", p => (IList<PortBinding>)new List<PortBinding>
                    {
                        new() { HostPort = GetAvailablePort().ToString() }
                    }),
                    RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
                },
                Labels = new Dictionary<string, string>
                {
                    ["ssbjr.service"] = "cache",
                    ["ssbjr.api"] = apiInterface.Name
                }
            });

            await _dockerClient.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters());

            _logger.LogInformation("Cache container {ContainerName} created and started", containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup cache for API {Name}", apiInterface.Name);
            throw;
        }
    }

    private async Task SetupMonitoringAsync(ApiInterface apiInterface)
    {
        if (!apiInterface.Monitoring.Metrics.Enabled)
            return;

        var containerName = $"ssbjr-{apiInterface.Name.ToLowerInvariant().Replace(" ", "-")}-monitoring";

        try
        {
            // Setup Prometheus for metrics
            var createResponse = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = "prom/prometheus:latest",
                Name = containerName,
                ExposedPorts = new Dictionary<string, EmptyStruct> { ["9090/tcp"] = default },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        ["9090/tcp"] = new List<PortBinding>
                        {
                            new() { HostPort = GetAvailablePort().ToString() }
                        }
                    },
                    RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
                },
                Labels = new Dictionary<string, string>
                {
                    ["ssbjr.service"] = "monitoring",
                    ["ssbjr.api"] = apiInterface.Name
                }
            });

            await _dockerClient.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters());

            _logger.LogInformation("Monitoring container {ContainerName} created and started", containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup monitoring for API {Name}", apiInterface.Name);
            throw;
        }
    }

    private async Task CleanupNetworksAsync(string prefix)
    {
        try
        {
            var networks = await _dockerClient.Networks.ListNetworksAsync();
            var networksToRemove = networks.Where(n => n.Name.StartsWith(prefix)).ToList();

            foreach (var network in networksToRemove)
            {
                await _dockerClient.Networks.DeleteNetworkAsync(network.ID);
                _logger.LogInformation("Removed network {NetworkName}", network.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup networks with prefix {Prefix}", prefix);
        }
    }

    private async Task CleanupVolumesAsync(string prefix)
    {
        try
        {
            var volumes = await _dockerClient.Volumes.ListAsync();
            var volumesToRemove = volumes.Volumes.Where(v => v.Name.StartsWith(prefix)).ToList();

            foreach (var volume in volumesToRemove)
            {
                await _dockerClient.Volumes.RemoveAsync(volume.Name);
                _logger.LogInformation("Removed volume {VolumeName}", volume.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup volumes with prefix {Prefix}", prefix);
        }
    }

    private (string imageName, List<string> envVars, List<int> ports) GetDatabaseConfig(DatabaseType type)
    {
        return type switch
        {
            DatabaseType.PostgreSQL => ("postgres:15", new List<string> 
            { 
                "POSTGRES_DB=apidb", 
                "POSTGRES_USER=apiuser", 
                "POSTGRES_PASSWORD=apipass123" 
            }, new List<int> { 5432 }),
            
            DatabaseType.MongoDB => ("mongo:6", new List<string> 
            { 
                "MONGO_INITDB_ROOT_USERNAME=root", 
                "MONGO_INITDB_ROOT_PASSWORD=rootpass123" 
            }, new List<int> { 27017 }),
            
            DatabaseType.MySQL => ("mysql:8", new List<string> 
            { 
                "MYSQL_DATABASE=apidb", 
                "MYSQL_USER=apiuser", 
                "MYSQL_PASSWORD=apipass123", 
                "MYSQL_ROOT_PASSWORD=rootpass123" 
            }, new List<int> { 3306 }),
            
            DatabaseType.SQLServer => ("mcr.microsoft.com/mssql/server:2022-latest", new List<string> 
            { 
                "SA_PASSWORD=ApiPass123!", 
                "ACCEPT_EULA=Y" 
            }, new List<int> { 1433 }),
            
            _ => throw new ArgumentException($"Unsupported database type: {type}")
        };
    }

    private (string imageName, List<string> envVars, List<int> ports) GetMessagingConfig(MessagingType type)
    {
        return type switch
        {
            MessagingType.RabbitMQ => ("rabbitmq:3-management", new List<string> 
            { 
                "RABBITMQ_DEFAULT_USER=guest", 
                "RABBITMQ_DEFAULT_PASS=guest" 
            }, new List<int> { 5672, 15672 }),
            
            MessagingType.Apache_Kafka => ("confluentinc/cp-kafka:latest", new List<string> 
            { 
                "KAFKA_ZOOKEEPER_CONNECT=localhost:2181", 
                "KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://localhost:9092" 
            }, new List<int> { 9092 }),
            
            _ => throw new ArgumentException($"Unsupported messaging type: {type}")
        };
    }

    private (string imageName, List<string> envVars, List<int> ports) GetCacheConfig(CacheType type)
    {
        return type switch
        {
            CacheType.Redis => ("redis:7-alpine", new List<string>(), new List<int> { 6379 }),
            CacheType.Memcached => ("memcached:1.6-alpine", new List<string>(), new List<int> { 11211 }),
            _ => throw new ArgumentException($"Unsupported cache type: {type}")
        };
    }

    private int GetAvailablePort()
    {
        var random = new Random();
        return random.Next(10000, 20000);
    }

    public void Dispose()
    {
        _dockerClient?.Dispose();
    }
}