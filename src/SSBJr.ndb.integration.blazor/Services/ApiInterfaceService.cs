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
    private readonly string _baseApiUrl;

    public ApiInterfaceService(HttpClient httpClient, ILogger<ApiInterfaceService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseApiUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7080";
    }

    public async Task<IEnumerable<ApiInterface>> GetAllAsync(string? search = null)
    {
        try
        {
            var url = $"{_baseApiUrl}/api/interfaces";
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
            var response = await _httpClient.GetAsync($"{_baseApiUrl}/api/interfaces/{id}");
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
            
            var response = await _httpClient.PostAsync($"{_baseApiUrl}/api/interfaces", content);
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
            
            var response = await _httpClient.PutAsync($"{_baseApiUrl}/api/interfaces/{id}", content);
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
            var response = await _httpClient.DeleteAsync($"{_baseApiUrl}/api/interfaces/{id}");
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
            var response = await _httpClient.PostAsync($"{_baseApiUrl}/api/interfaces/{id}/deploy", null);
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
            var response = await _httpClient.PostAsync($"{_baseApiUrl}/api/interfaces/{id}/stop", null);
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
            var response = await _httpClient.PostAsync($"{_baseApiUrl}/api/interfaces/{id}/restart", null);
            return response.IsSuccessStatusCode;
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
            var response = await _httpClient.PostAsync($"{_baseApiUrl}/api/interfaces/{id}/validate", null);
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
            _logger.LogError(ex, "Error validating API interface {Id}", id);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetMetricsAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseApiUrl}/api/interfaces/{id}/metrics");
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
            var response = await _httpClient.GetAsync($"{_baseApiUrl}/api/interfaces/{id}/logs?lines={lines}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<string>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<string>();
            }
            return new List<string>();
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
            // Get API interfaces and convert to service definitions
            var interfaces = await GetAllAsync();
            var services = new List<ServiceDefinition>();

            foreach (var apiInterface in interfaces)
            {
                services.Add(new ServiceDefinition
                {
                    Id = apiInterface.Id,
                    Name = apiInterface.Name,
                    Description = apiInterface.Description,
                    ServiceType = apiInterface.Type.ToString(),
                    Version = apiInterface.Version,
                    Status = apiInterface.Status.ToString(),
                    Endpoint = apiInterface.BaseUrl ?? "",
                    Configuration = apiInterface.Metadata,
                    Tags = apiInterface.Tags,
                    CreatedAt = apiInterface.CreatedAt,
                    LastHealthCheck = apiInterface.LastHealthCheck,
                    IsHealthy = apiInterface.Status == ApiStatus.Running
                });
            }

            // Also get the simple API manager services
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:8080/api/apis");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var simpleApis = JsonSerializer.Deserialize<List<dynamic>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Add simple APIs to services list
                    // This part might need adjustment based on the actual structure
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load simple APIs from API service");
            }

            return services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all services");
            return new List<ServiceDefinition>();
        }
    }
}