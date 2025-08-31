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
            var response = await _httpClient.DeleteAsync($"/api/interfaces/{id}");
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
            var response = await _httpClient.PostAsync($"/api/interfaces/{id}/deploy", null);
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
            var response = await _httpClient.PostAsync($"/api/interfaces/{id}/stop", null);
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
            await StopAsync(id);
            await Task.Delay(1000);
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
            var response = await _httpClient.GetAsync($"/api/interfaces/{id}/metrics");
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
            const string endpoint = "/api/interfaces";
            _logger.LogInformation("Calling {Endpoint} from {BaseAddress}", endpoint, _httpClient.BaseAddress);
            var response = await _httpClient.GetAsync(endpoint);
            var contentType = response.Content.Headers.ContentType?.MediaType;
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Interfaces response status: {Status} content-type: {ContentType}", response.StatusCode, contentType);

            if (response.IsSuccessStatusCode && contentType == "application/json")
            {
                return ParseServicesJson(responseContent);
            }

            _logger.LogWarning("Endpoint {Endpoint} returned {Status} ({ContentType}). Falling back to mock.", endpoint, response.StatusCode, contentType);
            return GetMockServices();
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP request failed calling interfaces endpoint. BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
            return GetMockServices();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting interfaces");
            return GetMockServices();
        }
    }

    private IEnumerable<ServiceDefinition> ParseServicesJson(string json)
    {
        try
        {
            var jsonDocument = JsonDocument.Parse(json);
            var services = new List<ServiceDefinition>();

            if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in jsonDocument.RootElement.EnumerateArray())
                {
                    try
                    {
                        var service = new ServiceDefinition
                        {
                            Id = element.TryGetProperty("id", out var idProp) && Guid.TryParse(idProp.GetString(), out var guid) ? guid : Guid.NewGuid(),
                            Name = element.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "",
                            Description = element.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "",
                            ServiceType = "REST",
                            Version = "1.0.0",
                            Status = MapApiStatusToString(element.TryGetProperty("status", out var statusProp) ? statusProp.GetString() ?? "" : ""),
                            Endpoint = element.TryGetProperty("baseUrl", out var urlProp) ? urlProp.GetString() ?? "" : "",
                            Configuration = element.TryGetProperty("metadata", out var metaProp) ? ParseMetadata(metaProp) : new Dictionary<string, object>(),
                            Tags = new List<string>(),
                            CreatedAt = element.TryGetProperty("createdAt", out var createdProp) && DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.Now,
                            LastHealthCheck = element.TryGetProperty("lastHealthCheck", out var healthProp) && DateTime.TryParse(healthProp.GetString(), out var health) ? health : null,
                            IsHealthy = MapStatusToHealthy(element.TryGetProperty("status", out var statusProp2) ? statusProp2.GetString() ?? "" : "")
                        };

                        services.Add(service);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing individual service from JSON");
                    }
                }
            }

            return services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing services JSON");
            return GetMockServices();
        }
    }

    private IEnumerable<ServiceDefinition> GetMockServices()
    {
        return new List<ServiceDefinition>
        {
            new ServiceDefinition
            {
                Id = Guid.NewGuid(),
                Name = "Petstore API (Mock)",
                Description = "API de exemplo para demonstração",
                ServiceType = "REST",
                Version = "1.0.0",
                Status = "Running",
                Endpoint = "https://petstore.swagger.io/v2",
                Configuration = new Dictionary<string, object>
                {
                    ["source"] = "mock",
                    ["environment"] = "development"
                },
                Tags = new List<string> { "demo", "petstore", "mock" },
                CreatedAt = DateTime.Now.AddDays(-1),
                LastHealthCheck = DateTime.Now.AddMinutes(-5),
                IsHealthy = true
            },
            new ServiceDefinition
            {
                Id = Guid.NewGuid(),
                Name = "JSON Placeholder API (Mock)",
                Description = "API fake para testes e prototipagem",
                ServiceType = "REST", 
                Version = "1.0.0",
                Status = "Running",
                Endpoint = "https://jsonplaceholder.typicode.com",
                Configuration = new Dictionary<string, object>
                {
                    ["source"] = "mock",
                    ["environment"] = "development"
                },
                Tags = new List<string> { "demo", "fake", "mock" },
                CreatedAt = DateTime.Now.AddHours(-6),
                LastHealthCheck = DateTime.Now.AddMinutes(-2),
                IsHealthy = true
            }
        };
    }

    private Dictionary<string, object> ParseMetadata(JsonElement metadataElement)
    {
        var metadata = new Dictionary<string, object>();
        try
        {
            if (metadataElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in metadataElement.EnumerateObject())
                {
                    metadata[property.Name] = property.Value.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing metadata");
        }
        return metadata;
    }

    private string MapApiStatusToString(string apiStatus)
    {
        // Map API service status to Blazor display status
        return apiStatus?.ToLowerInvariant() switch
        {
            "created" => "Draft",
            "deploying" => "Deploying", 
            "running" => "Running",
            "failed" => "Failed",
            "stopped" => "Stopped",
            _ => apiStatus ?? "Unknown"
        };
    }

    private bool MapStatusToHealthy(string status)
    {
        return status?.ToLowerInvariant() switch
        {
            "running" => true,
            _ => false
        };
    }
}