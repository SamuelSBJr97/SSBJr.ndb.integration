using SSBJr.ndb.integration.Web.ApiService.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SSBJr.ndb.integration.Web.ApiService.Services;

public interface IApiManagerService
{
    Task<ApiDefinition> CreateApiAsync(ApiInterfaceCreateRequest request);
    Task<ApiDefinition?> GetApiAsync(Guid id);
    Task<IEnumerable<ApiDefinition>> GetAllApisAsync();
    Task<bool> DeleteApiAsync(Guid id);
    Task<bool> StartApiAsync(Guid id);
    Task<bool> StopApiAsync(Guid id);
    Task<ApiHealthCheck> CheckApiHealthAsync(Guid id);
}

public class ApiManagerService : IApiManagerService
{
    private readonly ConcurrentDictionary<Guid, ApiDefinition> _apis = new();
    private readonly INotificationService _notificationService;
    private readonly ILogger<ApiManagerService> _logger;
    private readonly HttpClient _httpClient;

    public ApiManagerService(
        INotificationService notificationService, 
        ILogger<ApiManagerService> logger,
        HttpClient httpClient)
    {
        _notificationService = notificationService;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ApiDefinition> CreateApiAsync(ApiInterfaceCreateRequest request)
    {
        try
        {
            // Validar o JSON do Swagger
            await ValidateSwaggerJsonAsync(request.SwaggerJson);

            var api = new ApiDefinition
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                SwaggerJson = request.SwaggerJson,
                Status = ApiStatus.Created,
                CreatedAt = DateTime.UtcNow,
                BaseUrl = $"https://localhost:8080/api/proxy/{Guid.NewGuid():N}",
                Metadata = request.Metadata
            };

            _apis[api.Id] = api;

            _logger.LogInformation("API {Name} created with ID {ApiId}", request.Name, api.Id);
            await _notificationService.NotifyApiStatusChange(api.Id, "Created", "API criada com sucesso");
            _ = Task.Run(() => DeployApiAsync(api.Id));
            return api;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar API {Name}", request.Name);
            await _notificationService.NotifyError($"Erro ao criar API {request.Name}", ex.Message);
            throw;
        }
    }

    public async Task<ApiDefinition?> GetApiAsync(Guid id)
    {
        return await Task.FromResult(_apis.TryGetValue(id, out var api) ? api : null);
    }

    public async Task<IEnumerable<ApiDefinition>> GetAllApisAsync()
    {
        return await Task.FromResult(_apis.Values.ToList());
    }

    public async Task<bool> DeleteApiAsync(Guid id)
    {
        if (_apis.TryRemove(id, out var api))
        {
            _logger.LogInformation("API {Name} ({ApiId}) removida", api.Name, id);
            await _notificationService.NotifyApiStatusChange(id, "Deleted", "API removida");
            return true;
        }
        return false;
    }

    public async Task<bool> StartApiAsync(Guid id)
    {
        if (_apis.TryGetValue(id, out var api))
        {
            api.Status = ApiStatus.Running;
            _logger.LogInformation("API {Name} ({ApiId}) iniciada", api.Name, id);
            await _notificationService.NotifyApiStatusChange(id, "Running", "API iniciada");
            return true;
        }
        return false;
    }

    public async Task<bool> StopApiAsync(Guid id)
    {
        if (_apis.TryGetValue(id, out var api))
        {
            api.Status = ApiStatus.Stopped;
            _logger.LogInformation("API {Name} ({ApiId}) parada", api.Name, id);
            await _notificationService.NotifyApiStatusChange(id, "Stopped", "API parada");
            return true;
        }
        return false;
    }

    public async Task<ApiHealthCheck> CheckApiHealthAsync(Guid id)
    {
        if (!_apis.TryGetValue(id, out var api))
        {
            throw new ArgumentException("API não encontrada", nameof(id));
        }

        var healthCheck = new ApiHealthCheck
        {
            ApiId = id,
            CheckedAt = DateTime.UtcNow
        };

        try
        {
            var startTime = DateTime.UtcNow;
            
            // Simular verificação de saúde (pode ser implementado conforme necessário)
            await Task.Delay(100); // Simular latência
            
            var responseTime = DateTime.UtcNow - startTime;
            
            healthCheck.IsHealthy = api.Status == ApiStatus.Running;
            healthCheck.ResponseTime = responseTime;

            api.LastHealthCheck = DateTime.UtcNow;

            await _notificationService.NotifyApiHealthCheck(id, healthCheck.IsHealthy, 
                healthCheck.IsHealthy ? "API saudável" : "API com problemas");
        }
        catch (Exception ex)
        {
            healthCheck.IsHealthy = false;
            healthCheck.ErrorMessage = ex.Message;
            
            await _notificationService.NotifyApiHealthCheck(id, false, ex.Message);
        }

        return healthCheck;
    }

    private async Task ValidateSwaggerJsonAsync(string swaggerJson)
    {
        try
        {
            var document = JsonDocument.Parse(swaggerJson);
            
            // Verificar se tem estrutura básica do OpenAPI/Swagger
            if (!document.RootElement.TryGetProperty("openapi", out _) && 
                !document.RootElement.TryGetProperty("swagger", out _))
            {
                throw new ArgumentException("JSON não é um documento Swagger/OpenAPI válido");
            }
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("JSON inválido", ex);
        }
    }

    private async Task DeployApiAsync(Guid apiId)
    {
        if (!_apis.TryGetValue(apiId, out var api))
            return;

        try
        {
            api.Status = ApiStatus.Deploying;
            await _notificationService.NotifyApiStatusChange(apiId, "Deploying", "Iniciando deployment da API");

            // Simular processo de deployment
            await Task.Delay(3000);

            api.Status = ApiStatus.Running;
            await _notificationService.NotifyApiStatusChange(apiId, "Running", "API deployada e executando com sucesso");

            _logger.LogInformation("API {Name} ({ApiId}) deployada com sucesso", api.Name, apiId);
        }
        catch (Exception ex)
        {
            api.Status = ApiStatus.Failed;
            api.ErrorMessage = ex.Message;
            
            await _notificationService.NotifyApiStatusChange(apiId, "Failed", $"Erro no deployment: {ex.Message}");
            await _notificationService.NotifyError($"Falha no deployment da API {api.Name}", ex.Message);
            
            _logger.LogError(ex, "Erro no deployment da API {Name} ({ApiId})", api.Name, apiId);
        }
    }
}