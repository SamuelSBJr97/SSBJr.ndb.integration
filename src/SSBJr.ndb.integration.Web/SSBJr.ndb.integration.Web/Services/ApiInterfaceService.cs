using SSBJr.ndb.integration.Web.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SSBJr.ndb.integration.Web.Services;

public class ApiInterfaceService : IApiInterfaceService
{
    private readonly ConcurrentDictionary<Guid, ApiInterface> _apis = new();
    private readonly IDockerContainerService _containerService;
    private readonly IInfrastructureService _infrastructureService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ApiInterfaceService> _logger;
    private readonly IConfiguration _configuration;

    public ApiInterfaceService(
        IDockerContainerService containerService,
        IInfrastructureService infrastructureService,
        IAuditService auditService,
        ILogger<ApiInterfaceService> logger,
        IConfiguration configuration)
    {
        _containerService = containerService;
        _infrastructureService = infrastructureService;
        _auditService = auditService;
        _logger = logger;
        _configuration = configuration;
        
        // Seed with example data
        SeedExampleData();
    }

    public async Task<ApiInterface> CreateAsync(ApiInterfaceCreateRequest request, string userId)
    {
        try
        {
            await ValidateRequestAsync(request);

            var apiInterface = new ApiInterface
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                Version = request.Version,
                GraphQLSchema = request.GraphQLSchema,
                SwaggerJson = request.SwaggerJson,
                Infrastructure = request.Infrastructure,
                Security = request.Security,
                Monitoring = request.Monitoring,
                Status = ApiStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                Tags = request.Tags,
                Metadata = request.Metadata
            };

            _apis[apiInterface.Id] = apiInterface;

            await _auditService.LogAsync("API_CREATED", $"API Interface '{request.Name}' created", userId, apiInterface.Id.ToString());

            _logger.LogInformation("API Interface {Name} created with ID {Id} by user {UserId}", 
                request.Name, apiInterface.Id, userId);

            return apiInterface;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API Interface {Name}", request.Name);
            await _auditService.LogAsync("API_CREATE_ERROR", $"Failed to create API Interface '{request.Name}': {ex.Message}", userId);
            throw;
        }
    }

    public async Task<ApiInterface?> GetByIdAsync(Guid id)
    {
        return await Task.FromResult(_apis.TryGetValue(id, out var api) ? api : null);
    }

    public async Task<IEnumerable<ApiInterface>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        var query = _apis.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(api => 
                api.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                api.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                api.Tags.Any(tag => tag.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        return await Task.FromResult(
            query.OrderByDescending(x => x.CreatedAt)
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize)
                 .ToList());
    }

    public async Task<ApiInterface> UpdateAsync(Guid id, ApiInterfaceUpdateRequest request, string userId)
    {
        if (!_apis.TryGetValue(id, out var existingApi))
        {
            throw new ArgumentException("API Interface not found", nameof(id));
        }

        try
        {
            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(request.Name))
                existingApi.Name = request.Name;
            
            if (!string.IsNullOrWhiteSpace(request.Description))
                existingApi.Description = request.Description;
            
            if (!string.IsNullOrWhiteSpace(request.Version))
                existingApi.Version = request.Version;
            
            if (!string.IsNullOrWhiteSpace(request.GraphQLSchema))
                existingApi.GraphQLSchema = request.GraphQLSchema;
            
            if (!string.IsNullOrWhiteSpace(request.SwaggerJson))
                existingApi.SwaggerJson = request.SwaggerJson;
            
            if (request.Infrastructure != null)
                existingApi.Infrastructure = request.Infrastructure;
            
            if (request.Security != null)
                existingApi.Security = request.Security;
            
            if (request.Monitoring != null)
                existingApi.Monitoring = request.Monitoring;
            
            if (request.Tags != null)
                existingApi.Tags = request.Tags;
            
            if (request.Metadata != null)
                existingApi.Metadata = request.Metadata;

            existingApi.UpdatedAt = DateTime.UtcNow;
            existingApi.UpdatedBy = userId;
            existingApi.Status = ApiStatus.Draft;

            await _auditService.LogAsync("API_UPDATED", $"API Interface '{existingApi.Name}' updated", userId, id.ToString());

            _logger.LogInformation("API Interface {Name} ({Id}) updated by user {UserId}", 
                existingApi.Name, id, userId);

            return existingApi;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API Interface {Id}", id);
            await _auditService.LogAsync("API_UPDATE_ERROR", $"Failed to update API Interface {id}: {ex.Message}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        if (_apis.TryGetValue(id, out var api))
        {
            try
            {
                if (api.Status == ApiStatus.Running)
                {
                    await StopAsync(id);
                }

                await _infrastructureService.CleanupAsync(api);

                _apis.TryRemove(id, out _);

                await _auditService.LogAsync("API_DELETED", $"API Interface '{api.Name}' deleted", "system", id.ToString());

                _logger.LogInformation("API Interface {Name} ({Id}) deleted", api.Name, id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API Interface {Id}", id);
                await _auditService.LogAsync("API_DELETE_ERROR", $"Failed to delete API Interface {id}: {ex.Message}", "system");
                return false;
            }
        }
        return false;
    }

    public async Task<bool> DeployAsync(Guid id)
    {
        if (!_apis.TryGetValue(id, out var api))
            return false;

        try
        {
            api.Status = ApiStatus.Deploying;
            api.ErrorMessage = null;

            _logger.LogInformation("Starting deployment of API Interface {Name} ({Id})", api.Name, id);

            await _infrastructureService.SetupAsync(api);

            var deploymentInfo = await _containerService.DeployAsync(api);
            api.DeploymentInfo = deploymentInfo;

            api.Status = ApiStatus.Running;
            await _auditService.LogAsync("API_DEPLOYED", $"API Interface '{api.Name}' deployed successfully", "system", id.ToString());

            _logger.LogInformation("API Interface {Name} ({Id}) deployed successfully", api.Name, id);
            return true;
        }
        catch (Exception ex)
        {
            api.Status = ApiStatus.Failed;
            api.ErrorMessage = ex.Message;
            
            _logger.LogError(ex, "Error deploying API Interface {Name} ({Id})", api.Name, id);
            await _auditService.LogAsync("API_DEPLOY_ERROR", $"Failed to deploy API Interface '{api.Name}': {ex.Message}", "system", id.ToString());
            return false;
        }
    }

    public async Task<bool> StopAsync(Guid id)
    {
        if (!_apis.TryGetValue(id, out var api))
            return false;

        try
        {
            if (api.DeploymentInfo != null && !string.IsNullOrEmpty(api.DeploymentInfo.ContainerId))
            {
                await _containerService.StopAsync(api.DeploymentInfo.ContainerId);
            }

            api.Status = ApiStatus.Stopped;
            await _auditService.LogAsync("API_STOPPED", $"API Interface '{api.Name}' stopped", "system", id.ToString());

            _logger.LogInformation("API Interface {Name} ({Id}) stopped", api.Name, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping API Interface {Name} ({Id})", api.Name, id);
            return false;
        }
    }

    public async Task<bool> RestartAsync(Guid id)
    {
        await StopAsync(id);
        await Task.Delay(2000);
        return await DeployAsync(id);
    }

    public async Task<ApiInterface> ValidateAsync(Guid id)
    {
        if (!_apis.TryGetValue(id, out var api))
            throw new ArgumentException("API Interface not found", nameof(id));

        try
        {
            api.Status = ApiStatus.Validating;

            if (api.Type == ApiType.GraphQL && !string.IsNullOrWhiteSpace(api.GraphQLSchema))
            {
                await ValidateGraphQLSchemaAsync(api.GraphQLSchema);
            }

            if (!string.IsNullOrWhiteSpace(api.SwaggerJson))
            {
                await ValidateSwaggerJsonAsync(api.SwaggerJson);
            }

            api.Status = ApiStatus.Draft;
            await _auditService.LogAsync("API_VALIDATED", $"API Interface '{api.Name}' validated successfully", "system", id.ToString());

            return api;
        }
        catch (Exception ex)
        {
            api.Status = ApiStatus.Failed;
            api.ErrorMessage = ex.Message;
            
            _logger.LogError(ex, "Validation failed for API Interface {Name} ({Id})", api.Name, id);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetMetricsAsync(Guid id)
    {
        if (!_apis.TryGetValue(id, out var api) || api.DeploymentInfo == null)
            return new Dictionary<string, object>();

        try
        {
            return await _containerService.GetMetricsAsync(api.DeploymentInfo.ContainerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics for API Interface {Id}", id);
            return new Dictionary<string, object> { { "error", ex.Message } };
        }
    }

    public async Task<List<string>> GetLogsAsync(Guid id, int lines = 100)
    {
        if (!_apis.TryGetValue(id, out var api) || api.DeploymentInfo == null)
            return new List<string>();

        try
        {
            return await _containerService.GetLogsAsync(api.DeploymentInfo.ContainerId, lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs for API Interface {Id}", id);
            return new List<string> { $"Error: {ex.Message}" };
        }
    }

    private async Task ValidateRequestAsync(ApiInterfaceCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        if (_apis.Values.Any(x => x.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("An API Interface with this name already exists");

        if (request.Type == ApiType.GraphQL && string.IsNullOrWhiteSpace(request.GraphQLSchema))
            throw new ArgumentException("GraphQL Schema is required for GraphQL APIs");

        await Task.CompletedTask;
    }

    private async Task ValidateGraphQLSchemaAsync(string schema)
    {
        if (!schema.Contains("type") && !schema.Contains("Query"))
            throw new ArgumentException("Invalid GraphQL schema");

        await Task.CompletedTask;
    }

    private async Task ValidateSwaggerJsonAsync(string swaggerJson)
    {
        try
        {
            var document = JsonDocument.Parse(swaggerJson);
            
            if (!document.RootElement.TryGetProperty("openapi", out _) && 
                !document.RootElement.TryGetProperty("swagger", out _))
            {
                throw new ArgumentException("Invalid Swagger/OpenAPI document");
            }
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON format", ex);
        }

        await Task.CompletedTask;
    }

    private void SeedExampleData()
    {
        var exampleApi = new ApiInterface
        {
            Id = Guid.NewGuid(),
            Name = "Demo GraphQL API",
            Description = "API de demonstração completa com GraphQL e infraestrutura moderna",
            Type = ApiType.GraphQL,
            Version = "1.0.0",
            GraphQLSchema = @"
                type Query {
                    users: [User]
                    user(id: ID!): User
                }
                
                type User {
                    id: ID!
                    name: String!
                    email: String!
                    posts: [Post]
                }
                
                type Post {
                    id: ID!
                    title: String!
                    content: String!
                    author: User!
                }
            ",
            Infrastructure = new InfrastructureConfig
            {
                Database = new DatabaseConfig
                {
                    Type = DatabaseType.PostgreSQL,
                    ConnectionString = "postgres://user:password@localhost:5432/demo_db",
                    EnableReadReplicas = true,
                    MaxConnections = 100
                },
                Messaging = new MessagingConfig
                {
                    Type = MessagingType.RabbitMQ,
                    ConnectionString = "amqp://guest:guest@localhost:5672/",
                    Queues = new List<string> { "notifications", "events" }
                },
                Cache = new CacheConfig
                {
                    Type = CacheType.Redis,
                    ConnectionString = "redis://localhost:6379",
                    DefaultTTL = 3600
                }
            },
            Security = new SecurityConfig
            {
                Authentication = new AuthenticationConfig
                {
                    Type = AuthenticationType.JWT
                },
                RateLimit = new RateLimitConfig
                {
                    Enabled = true,
                    RequestsPerMinute = 100
                }
            },
            Status = ApiStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            Tags = new List<string> { "demo", "graphql", "modern" }
        };

        _apis[exampleApi.Id] = exampleApi;
    }
}