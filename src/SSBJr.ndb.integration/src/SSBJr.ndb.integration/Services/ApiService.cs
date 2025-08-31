using SSBJr.ndb.integration.Models;
using System.Text.Json;
using System.Text;

namespace SSBJr.ndb.integration.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<List<ApiInterface>> GetApiInterfacesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/interfaces");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ApiInterface>>(json, _jsonOptions) ?? new List<ApiInterface>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching API interfaces");
            throw;
        }
    }

    public async Task<ApiInterface> GetApiInterfaceAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/interfaces/{id}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiInterface>(json, _jsonOptions)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching API interface {Id}", id);
            throw;
        }
    }

    public async Task<ApiInterface> CreateApiInterfaceAsync(CreateApiInterfaceRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/v1/interfaces", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiInterface>(responseJson, _jsonOptions)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API interface");
            throw;
        }
    }

    public async Task<ApiInterface> UpdateApiInterfaceAsync(string id, UpdateApiInterfaceRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/api/v1/interfaces/{id}", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiInterface>(responseJson, _jsonOptions)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API interface {Id}", id);
            throw;
        }
    }

    public async Task DeleteApiInterfaceAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/interfaces/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API interface {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeployApiInterfaceAsync(string id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/v1/interfaces/{id}/deploy", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying API interface {Id}", id);
            return false;
        }
    }

    public async Task<bool> StopApiInterfaceAsync(string id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/v1/interfaces/{id}/stop", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping API interface {Id}", id);
            return false;
        }
    }

    public async Task<bool> RestartApiInterfaceAsync(string id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/v1/interfaces/{id}/restart", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting API interface {Id}", id);
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetApiMetricsAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/interfaces/{id}/metrics");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions) ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching metrics for API interface {Id}", id);
            return new Dictionary<string, object>();
        }
    }

    public async Task<List<string>> GetApiLogsAsync(string id, int count = 100)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/interfaces/{id}/logs?count={count}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching logs for API interface {Id}", id);
            return new List<string>();
        }
    }
}