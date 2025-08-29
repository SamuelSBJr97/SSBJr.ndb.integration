using Docker.DotNet;
using Docker.DotNet.Models;
using SSBJr.ndb.integration.Web.Models;
using System.Text;
using ApiResourceRequirements = SSBJr.ndb.integration.Web.Models.ResourceRequirements;

namespace SSBJr.ndb.integration.Web.Services;

public interface IDockerContainerService
{
    Task<DeploymentInfo> DeployAsync(ApiInterface apiInterface);
    Task StopAsync(string containerId);
    Task<Dictionary<string, object>> GetMetricsAsync(string containerId);
    Task<List<string>> GetLogsAsync(string containerId, int lines = 100);
    Task<bool> IsRunningAsync(string containerId);
}

public class DockerContainerService : IDockerContainerService
{
    private readonly DockerClient _dockerClient;
    private readonly ILogger<DockerContainerService> _logger;
    private readonly IConfiguration _configuration;

    public DockerContainerService(ILogger<DockerContainerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        var dockerEndpoint = configuration.GetConnectionString("Docker") ?? "npipe://./pipe/docker_engine";
        _dockerClient = new DockerClientConfiguration(new Uri(dockerEndpoint)).CreateClient();
    }

    public async Task<DeploymentInfo> DeployAsync(ApiInterface apiInterface)
    {
        try
        {
            _logger.LogInformation("Starting deployment for API {Name}", apiInterface.Name);

            // Generate Dockerfile
            var dockerfile = GenerateDockerfile(apiInterface);
            
            // Build image
            var imageName = $"ssbjr-api-{apiInterface.Name.ToLowerInvariant().Replace(" ", "-")}:{apiInterface.Version}";
            await BuildImageAsync(imageName, dockerfile, apiInterface);

            // Create and start container
            var container = await CreateContainerAsync(imageName, apiInterface);
            await _dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());

            var deploymentInfo = new DeploymentInfo
            {
                ContainerImage = imageName,
                ContainerId = container.ID,
                NetworkPorts = GetNetworkPorts(apiInterface),
                EnvironmentVariables = GetEnvironmentVariables(apiInterface),
                Resources = new ApiResourceRequirements(),
                DeployedAt = DateTime.UtcNow,
                DeployedBy = "system"
            };

            _logger.LogInformation("Successfully deployed API {Name} with container {ContainerId}", 
                apiInterface.Name, container.ID);

            return deploymentInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy API {Name}", apiInterface.Name);
            throw;
        }
    }

    public async Task StopAsync(string containerId)
    {
        try
        {
            await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters
            {
                WaitBeforeKillSeconds = 30
            });

            await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters
            {
                Force = true
            });

            _logger.LogInformation("Container {ContainerId} stopped and removed", containerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop container {ContainerId}", containerId);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetMetricsAsync(string containerId)
    {
        try
        {
            // Simplified metrics - in production would use proper Docker stats
            var metrics = new Dictionary<string, object>
            {
                ["cpu_usage_percent"] = 15.5,
                ["memory_usage_bytes"] = 128 * 1024 * 1024, // 128MB
                ["memory_limit_bytes"] = 512 * 1024 * 1024, // 512MB
                ["memory_usage_percent"] = 25.0,
                ["timestamp"] = DateTime.UtcNow,
                ["container_id"] = containerId,
                ["status"] = "simulated"
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics for container {ContainerId}", containerId);
            return new Dictionary<string, object> { { "error", ex.Message } };
        }
    }

    public async Task<List<string>> GetLogsAsync(string containerId, int lines = 100)
    {
        try
        {
            // Simplified logs - in production would read actual container logs
            var logLines = new List<string>
            {
                $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Container {containerId} started",
                $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] API service initialized",
                $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Database connection established",
                $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Health check endpoint active",
                $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Service ready to accept requests"
            };

            return await Task.FromResult(logLines.Take(lines).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs for container {ContainerId}", containerId);
            return new List<string> { $"Error retrieving logs: {ex.Message}" };
        }
    }

    public async Task<bool> IsRunningAsync(string containerId)
    {
        try
        {
            var container = await _dockerClient.Containers.InspectContainerAsync(containerId);
            return container.State.Running;
        }
        catch
        {
            return false;
        }
    }

    private string GenerateDockerfile(ApiInterface apiInterface)
    {
        var dockerfile = new StringBuilder();
        
        dockerfile.AppendLine("FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base");
        dockerfile.AppendLine("WORKDIR /app");
        dockerfile.AppendLine("EXPOSE 80");
        dockerfile.AppendLine("EXPOSE 443");
        dockerfile.AppendLine();

        dockerfile.AppendLine("FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build");
        dockerfile.AppendLine("WORKDIR /src");
        dockerfile.AppendLine();

        // Add GraphQL dependencies if needed
        if (apiInterface.Type == ApiType.GraphQL || apiInterface.Type == ApiType.Hybrid)
        {
            dockerfile.AppendLine("# Install GraphQL packages");
            dockerfile.AppendLine("RUN dotnet new webapi -n ApiApp");
            dockerfile.AppendLine("WORKDIR /src/ApiApp");
            dockerfile.AppendLine("RUN dotnet add package HotChocolate.AspNetCore");
            dockerfile.AppendLine("RUN dotnet add package HotChocolate.Data.EntityFramework");
        }

        // Add database packages based on configuration
        switch (apiInterface.Infrastructure.Database.Type)
        {
            case DatabaseType.PostgreSQL:
                dockerfile.AppendLine("RUN dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL");
                break;
            case DatabaseType.MongoDB:
                dockerfile.AppendLine("RUN dotnet add package MongoDB.Driver");
                break;
            case DatabaseType.MySQL:
                dockerfile.AppendLine("RUN dotnet add package MySql.EntityFrameworkCore");
                break;
            case DatabaseType.SQLServer:
                dockerfile.AppendLine("RUN dotnet add package Microsoft.EntityFrameworkCore.SqlServer");
                break;
        }

        // Add messaging packages
        switch (apiInterface.Infrastructure.Messaging.Type)
        {
            case MessagingType.RabbitMQ:
                dockerfile.AppendLine("RUN dotnet add package RabbitMQ.Client");
                dockerfile.AppendLine("RUN dotnet add package MassTransit.RabbitMQ");
                break;
            case MessagingType.Apache_Kafka:
                dockerfile.AppendLine("RUN dotnet add package Confluent.Kafka");
                break;
        }

        // Add caching packages
        if (apiInterface.Infrastructure.Cache.Type == CacheType.Redis)
        {
            dockerfile.AppendLine("RUN dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis");
        }

        // Add monitoring packages
        dockerfile.AppendLine("RUN dotnet add package Serilog.AspNetCore");
        dockerfile.AppendLine("RUN dotnet add package Serilog.Sinks.Console");
        dockerfile.AppendLine("RUN dotnet add package OpenTelemetry.Extensions.Hosting");
        dockerfile.AppendLine("RUN dotnet add package OpenTelemetry.Instrumentation.AspNetCore");
        dockerfile.AppendLine("RUN dotnet add package Prometheus.Client.AspNetCore");

        // Generate application code
        dockerfile.AppendLine();
        dockerfile.AppendLine("# Copy generated application code");
        dockerfile.AppendLine("COPY . .");
        dockerfile.AppendLine("RUN dotnet restore");
        dockerfile.AppendLine("RUN dotnet build -c Release -o /app/build");
        dockerfile.AppendLine();

        dockerfile.AppendLine("FROM build AS publish");
        dockerfile.AppendLine("RUN dotnet publish -c Release -o /app/publish");
        dockerfile.AppendLine();

        dockerfile.AppendLine("FROM base AS final");
        dockerfile.AppendLine("WORKDIR /app");
        dockerfile.AppendLine("COPY --from=publish /app/publish .");
        dockerfile.AppendLine("ENTRYPOINT [\"dotnet\", \"ApiApp.dll\"]");

        return dockerfile.ToString();
    }

    private async Task BuildImageAsync(string imageName, string dockerfile, ApiInterface apiInterface)
    {
        var buildContext = GenerateBuildContext(dockerfile, apiInterface);
        
        await _dockerClient.Images.BuildImageFromDockerfileAsync(
            buildContext,
            new ImageBuildParameters
            {
                Tags = new[] { imageName },
                BuildArgs = new Dictionary<string, string>
                {
                    ["API_NAME"] = apiInterface.Name,
                    ["API_VERSION"] = apiInterface.Version
                }
            });
    }

    private Stream GenerateBuildContext(string dockerfile, ApiInterface apiInterface)
    {
        // Generate a simple build context with Dockerfile and basic API structure
        var context = new MemoryStream();
        
        // For now, return a simple context
        // In a real implementation, this would generate the complete API structure
        var dockerfileBytes = Encoding.UTF8.GetBytes(dockerfile);
        context.Write(dockerfileBytes);
        context.Position = 0;
        
        return context;
    }

    private async Task<CreateContainerResponse> CreateContainerAsync(string imageName, ApiInterface apiInterface)
    {
        var ports = new Dictionary<string, EmptyStruct>();
        var portBindings = new Dictionary<string, IList<PortBinding>>();

        // Configure ports
        var httpPort = GetAvailablePort();
        ports["80/tcp"] = default;
        portBindings["80/tcp"] = new List<PortBinding>
        {
            new() { HostPort = httpPort.ToString() }
        };

        if (apiInterface.Security.Encryption.EnableTLS)
        {
            var httpsPort = GetAvailablePort();
            ports["443/tcp"] = default;
            portBindings["443/tcp"] = new List<PortBinding>
            {
                new() { HostPort = httpsPort.ToString() }
            };
        }

        var containerConfig = new CreateContainerParameters
        {
            Image = imageName,
            Name = $"ssbjr-api-{apiInterface.Name.ToLowerInvariant().Replace(" ", "-")}-{Guid.NewGuid():N}",
            ExposedPorts = ports,
            HostConfig = new HostConfig
            {
                PortBindings = portBindings,
                Memory = ParseMemoryString("512Mi") * 1024 * 1024, // Convert to bytes
                RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
            },
            Env = GetEnvironmentVariables(apiInterface).Select(kvp => $"{kvp.Key}={kvp.Value}").ToList(),
            Labels = new Dictionary<string, string>
            {
                ["ssbjr.api.name"] = apiInterface.Name,
                ["ssbjr.api.version"] = apiInterface.Version,
                ["ssbjr.api.type"] = apiInterface.Type.ToString(),
                ["ssbjr.api.id"] = apiInterface.Id.ToString()
            }
        };

        return await _dockerClient.Containers.CreateContainerAsync(containerConfig);
    }

    private List<string> GetNetworkPorts(ApiInterface apiInterface)
    {
        var ports = new List<string> { "80" };
        
        if (apiInterface.Security.Encryption.EnableTLS)
        {
            ports.Add("443");
        }

        return ports;
    }

    private Dictionary<string, string> GetEnvironmentVariables(ApiInterface apiInterface)
    {
        var envVars = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["API_NAME"] = apiInterface.Name,
            ["API_VERSION"] = apiInterface.Version,
            ["API_TYPE"] = apiInterface.Type.ToString(),
            ["DATABASE_TYPE"] = apiInterface.Infrastructure.Database.Type.ToString(),
            ["DATABASE_CONNECTION"] = apiInterface.Infrastructure.Database.ConnectionString,
            ["CACHE_TYPE"] = apiInterface.Infrastructure.Cache.Type.ToString(),
            ["CACHE_CONNECTION"] = apiInterface.Infrastructure.Cache.ConnectionString,
            ["MESSAGING_TYPE"] = apiInterface.Infrastructure.Messaging.Type.ToString(),
            ["MESSAGING_CONNECTION"] = apiInterface.Infrastructure.Messaging.ConnectionString,
            ["LOGGING_LEVEL"] = apiInterface.Monitoring.Logging.Level.ToString(),
            ["METRICS_ENABLED"] = apiInterface.Monitoring.Metrics.Enabled.ToString(),
            ["TRACING_ENABLED"] = apiInterface.Monitoring.Tracing.Enabled.ToString(),
            ["AUDIT_ENABLED"] = apiInterface.Monitoring.Audit.Enabled.ToString()
        };

        // Add authentication configuration
        if (apiInterface.Security.Authentication.Type == AuthenticationType.JWT && 
            apiInterface.Security.Authentication.Jwt != null)
        {
            envVars["JWT_SECRET"] = apiInterface.Security.Authentication.Jwt.SecretKey;
            envVars["JWT_ISSUER"] = apiInterface.Security.Authentication.Jwt.Issuer;
            envVars["JWT_AUDIENCE"] = apiInterface.Security.Authentication.Jwt.Audience;
        }

        return envVars;
    }

    private double CalculateCPUUsage(CPUStats current, CPUStats previous)
    {
        if (previous == null) return 0.0;

        var cpuDelta = current.CPUUsage.TotalUsage - previous.CPUUsage.TotalUsage;
        var systemDelta = current.SystemUsage - previous.SystemUsage;

        if (systemDelta > 0 && cpuDelta > 0)
        {
            return (double)cpuDelta / systemDelta * current.CPUUsage.PercpuUsage.Count * 100.0;
        }

        return 0.0;
    }

    private int GetAvailablePort()
    {
        // Simple port allocation - in production, use a proper port manager
        var random = new Random();
        return random.Next(8000, 9000);
    }

    private long ParseMemoryString(string memoryString)
    {
        // Simple memory parsing (e.g., "512Mi" -> 512)
        // In production, implement proper parsing for different units
        return 512; // Default 512MB
    }

    public void Dispose()
    {
        _dockerClient?.Dispose();
    }
}