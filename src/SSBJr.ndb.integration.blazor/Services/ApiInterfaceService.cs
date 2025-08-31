using SSBJr.ndb.integration.Blazor.Models;
using System.Text.Json;
using System.Text;

namespace SSBJr.ndb.integration.Blazor.Services;

public interface IApiInterfaceService
{
    Task<IEnumerable<ApiInterface>> GetAllAsync(string? search = null);
    Task<ApiInterface?> GetByIdAsync(Guid id);
    Task<ApiInterface> CreateAsync(ApiInterfaceCreateRequest request);
    Task<ApiInterface> UpdateAsync(Guid id, ApiInterfaceUpdateRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeployAsync(Guid id);
    Task<bool> StopAsync(Guid id);
    Task<bool> RestartAsync(Guid id);
    Task<ApiInterface> ValidateAsync(Guid id);
    Task<Dictionary<string, object>> GetMetricsAsync(Guid id);
    Task<List<string>> GetLogsAsync(Guid id, int lines = 100);
    Task<IEnumerable<ServiceDefinition>> GetAllServicesAsync();
}

public class ApiInterfaceService : IApiInterfaceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiInterfaceService> _logger;

    public ApiInterfaceService(HttpClient httpClient, ILogger<ApiInterfaceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<ApiInterface>> GetAllAsync(string? search = null)
    {
        try
        {
            var url = "/api/interfaces";
            if (!string.IsNullOrEmpty(search))
            {
                url += $"?search={Uri.EscapeDataString(search)}";
            }

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ApiInterface>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<ApiInterface>();
            }

            _logger.LogWarning("Failed to get API interfaces. Status: {StatusCode}", response.StatusCode);
            return new List<ApiInterface>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API interfaces");
            return new List<ApiInterface>();
        }
    }

    public async Task<ApiInterface?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/interfaces/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiInterface>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API interface {Id}", id);
            return null;
        }
    }

    public async Task<ApiInterface> CreateAsync(ApiInterfaceCreateRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/interfaces", content);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiInterface>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize response");
            }

            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API returned {response.StatusCode}: {errorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API interface");
            throw;
        }
    }

    public async Task<ApiInterface> UpdateAsync(Guid id, ApiInterfaceUpdateRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"/api/interfaces/{id}", content);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiInterface>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize response");
            }

            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API returned {response.StatusCode}: {errorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API interface {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/apis/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API interface {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeployAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/apis/{id}/start", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying API interface {Id}", id);
            return false;
        }
    }

    public async Task<bool> StopAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/apis/{id}/stop", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping API interface {Id}", id);
            return false;
        }
    }

    public async Task<bool> RestartAsync(Guid id)
    {
        try
        {
            // For restart, we'll stop and then start
            await StopAsync(id);
            await Task.Delay(1000); // Brief delay
            return await DeployAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting API interface {Id}", id);
            return false;
        }
    }

    public async Task<ApiInterface> ValidateAsync(Guid id)
    {
        try
        {
            // For now, just return the current API state
            // In a real implementation, this would validate the API configuration
            var api = await GetByIdAsync(id);
            if (api == null)
            {
                throw new ArgumentException("API interface not found");
            }
            return api;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API interface {Id}", id);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetMetricsAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/apis/{id}/health");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Dictionary<string, object>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new Dictionary<string, object>();
            }
            return new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics for API interface {Id}", id);
            return new Dictionary<string, object> { { "error", ex.Message } };
        }
    }

    public async Task<List<string>> GetLogsAsync(Guid id, int lines = 100)
    {
        try
        {
            // Return mock logs for now since the API doesn't have a logs endpoint yet
            return new List<string>
            {
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: API {id} is running",
                $"[{DateTime.Now.AddMinutes(-1):yyyy-MM-dd HH:mm:ss}] INFO: Health check passed",
                $"[{DateTime.Now.AddMinutes(-5):yyyy-MM-dd HH:mm:ss}] INFO: API started successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs for API interface {Id}", id);
            return new List<string> { $"Error: {ex.Message}" };
        }
    }

    public async Task<IEnumerable<ServiceDefinition>> GetAllServicesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/services");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apis = JsonSerializer.Deserialize<List<ApiDefinition>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<ApiDefinition>();

                // Convert ApiDefinition to ServiceDefinition
                var services = apis.Select(api => new ServiceDefinition
                {
                    Id = api.Id,
                    Name = api.Name,
                    Description = api.Description,
                    ServiceType = "REST",
                    Version = "1.0.0",
                    Status = api.Status.ToString(),
                    Endpoint = api.BaseUrl,
                    Configuration = api.Metadata,
                    Tags = new List<string>(),
                    CreatedAt = api.CreatedAt,
                    LastHealthCheck = api.LastHealthCheck,
                    IsHealthy = api.Status == ApiStatus.Running
                }).ToList();

                return services;
            }

            _logger.LogWarning("Failed to get services. Status: {StatusCode}", response.StatusCode);
            return new List<ServiceDefinition>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all services");
            return new List<ServiceDefinition>();
        }
    }
}