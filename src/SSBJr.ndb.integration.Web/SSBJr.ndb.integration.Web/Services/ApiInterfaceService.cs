using SSBJr.ndb.integration.Web.Models;
using SSBJr.ndb.integration.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace SSBJr.ndb.integration.Web.Services;

public class ApiInterfaceService : IApiInterfaceService
{
    private readonly ApiManagerDbContext _context;
    private readonly IDockerContainerService _containerService;
    private readonly IInfrastructureService _infrastructureService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ApiInterfaceService> _logger;
    private readonly IConfiguration _configuration;

    public ApiInterfaceService(
        ApiManagerDbContext context,
        IDockerContainerService containerService,
        IInfrastructureService infrastructureService,
        IAuditService auditService,
        ILogger<ApiInterfaceService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _containerService = containerService;
        _infrastructureService = infrastructureService;
        _auditService = auditService;
        _logger = logger;
        _configuration = configuration;
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
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            _context.ApiInterfaces.Add(apiInterface);
            await _context.SaveChangesAsync();

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
        return await _context.ApiInterfaces.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<ApiInterface>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        var query = _context.ApiInterfaces.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(api => 
                api.Name.Contains(search) ||
                api.Description.Contains(search) ||
                api.Tags.Any(tag => tag.Contains(search)));
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<ApiInterface> UpdateAsync(Guid id, ApiInterfaceUpdateRequest request, string userId)
    {
        var existingApi = await _context.ApiInterfaces.FirstOrDefaultAsync(a => a.Id == id);
        if (existingApi == null)
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
            existingApi.Status = ApiStatus.Draft; // Reset to draft after update

            await _context.SaveChangesAsync();

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
        var api = await _context.ApiInterfaces.FirstOrDefaultAsync(a => a.Id == id);
        if (api == null)
            return false;

        try
        {
            // Stop and cleanup container if running
            if (api.Status == ApiStatus.Running)
            {
                await StopAsync(id);
            }

            // Cleanup infrastructure
            await _infrastructureService.CleanupAsync(api);

            _context.ApiInterfaces.Remove(api);
            await _context.SaveChangesAsync();

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

    public async Task<bool> DeployAsync(Guid id)
    {
        var api = await _context.ApiInterfaces.FirstOrDefaultAsync(a => a.Id == id);
        if (api == null)
            return false;

        try
        {
            api.Status = ApiStatus.Deploying;
            api.ErrorMessage = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Starting deployment of API Interface {Name} ({Id})", api.Name, id);

            // Setup infrastructure
            await _infrastructureService.SetupAsync(api);

            // Build and deploy container
            var deploymentInfo = await _containerService.DeployAsync(api);
            api.DeploymentInfo = deploymentInfo;

            api.Status = ApiStatus.Running;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("API_DEPLOYED", $"API Interface '{api.Name}' deployed successfully", "system", id.ToString());

            _logger.LogInformation("API Interface {Name} ({Id}) deployed successfully", api.Name, id);
            return true;
        }
        catch (Exception ex)
        {
            api.Status = ApiStatus.Failed;
            api.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync();
            
            _logger.LogError(ex, "Error deploying API Interface {Name} ({Id})", api.Name, id);
            await _auditService.LogAsync("API_DEPLOY_ERROR", $"Failed to deploy API Interface '{api.Name}': {ex.Message}", "system", id.ToString());
            return false;
        }
    }

    public async Task<bool> StopAsync(Guid id)
    {
        var api = await _context.ApiInterfaces.FirstOrDefaultAsync(a => a.Id == id);
        if (api == null)
            return false;

        try
        {
            if (api.DeploymentInfo != null && !string.IsNullOrEmpty(api.DeploymentInfo.ContainerId))
            {
                await _containerService.StopAsync(api.DeploymentInfo.ContainerId);
            }

            api.Status = ApiStatus.Stopped;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("API_STOPPED", $"API Interface '{api.Name}' stopped", "system", id.ToString());

            _logger.LogInformation("API Interface {Name} ({Id}) stopped", api.Name, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping API Interface {Name} ({Id})", api.Name, id);
            await _auditService.LogAsync("API_STOP_ERROR", $"Failed to stop API Interface '{api.Name}': {ex.Message}", "system", id.ToString());
            return false;
        }
    }

    public async Task<bool> RestartAsync(Guid id)
    {
        await StopAsync(id);
        await Task.Delay(2000); // Wait for graceful shutdown
        return await DeployAsync(id);
    }

    public async Task<ApiInterface> ValidateAsync(Guid id)
    {
        var api = await _context.ApiInterfaces.FirstOrDefaultAsync(a => a.Id == id);
        if (api == null)
            throw new ArgumentException("API Interface not found", nameof(id));

        try
        {
            api.Status = ApiStatus.Validating;
            await _context.SaveChangesAsync();

            // Validate GraphQL schema
            if (api.Type == ApiType.GraphQL && !string.IsNullOrWhiteSpace(api.GraphQLSchema))
            {
                await ValidateGraphQLSchemaAsync(api.GraphQLSchema);
            }

            // Validate Swagger JSON
            if (!string.IsNullOrWhiteSpace(api.SwaggerJson))
            {
                await ValidateSwaggerJsonAsync(api.SwaggerJson);
            }

            // Validate infrastructure configuration
            await ValidateInfrastructureAsync(api.Infrastructure);

            api.Status = ApiStatus.Draft; // Return to draft after successful validation
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("API_VALIDATED", $"API Interface '{api.Name}' validated successfully", "system", id.ToString());

            return api;
        }
        catch (Exception ex)
        {
            api.Status = ApiStatus.Failed;
            api.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync();
            
            _logger.LogError(ex, "Validation failed for API Interface {Name} ({Id})", api.Name, id);
            await _auditService.LogAsync("API_VALIDATION_ERROR", $"Validation failed for API Interface '{api.Name}': {ex.Message}", "system", id.ToString());
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetMetricsAsync(Guid id)
    {
        var api = await _context.ApiInterfaces.FirstOrDefaultAsync(a => a.Id == id);
        if (api == null || api.DeploymentInfo == null)
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
        var api = await _context.ApiInterfaces.FirstOrDefaultAsync(a => a.Id == id);
        if (api == null || api.DeploymentInfo == null)
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

        if (await _context.ApiInterfaces.AnyAsync(x => x.Name.Equals(request.Name)))
            throw new ArgumentException("An API Interface with this name already exists");

        if (request.Type == ApiType.GraphQL && string.IsNullOrWhiteSpace(request.GraphQLSchema))
            throw new ArgumentException("GraphQL Schema is required for GraphQL APIs");

        await Task.CompletedTask;
    }

    private async Task ValidateGraphQLSchemaAsync(string schema)
    {
        // Basic GraphQL schema validation
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

    private async Task ValidateInfrastructureAsync(InfrastructureConfig infrastructure)
    {
        // Validate database configuration
        if (infrastructure.Database.Type != DatabaseType.Redis && 
            string.IsNullOrWhiteSpace(infrastructure.Database.ConnectionString))
        {
            throw new ArgumentException("Database connection string is required");
        }

        await Task.CompletedTask;
    }
}