using SSBJr.ndb.integration.Models;

namespace SSBJr.ndb.integration.Services;

public interface IApiService
{
    Task<List<ApiInterface>> GetApiInterfacesAsync();
    Task<ApiInterface> GetApiInterfaceAsync(string id);
    Task<ApiInterface> CreateApiInterfaceAsync(CreateApiInterfaceRequest request);
    Task<ApiInterface> UpdateApiInterfaceAsync(string id, UpdateApiInterfaceRequest request);
    Task DeleteApiInterfaceAsync(string id);
    Task<bool> DeployApiInterfaceAsync(string id);
    Task<bool> StopApiInterfaceAsync(string id);
    Task<bool> RestartApiInterfaceAsync(string id);
    Task<Dictionary<string, object>> GetApiMetricsAsync(string id);
    Task<List<string>> GetApiLogsAsync(string id, int count = 100);
}

public interface IInfrastructureService
{
    Task<List<InfrastructureTemplate>> GetTemplatesAsync();
    Task<bool> ValidateInfrastructureConfigAsync(InfrastructureConfig config);
    Task<SystemHealth> GetSystemHealthAsync();
    Task<Dictionary<string, object>> GetInfrastructureStatusAsync(string apiId);
    Task<bool> TestDatabaseConnectionAsync(DatabaseConfig config);
    Task<bool> TestMessagingConnectionAsync(MessagingConfig config);
    Task<bool> TestCacheConnectionAsync(CacheConfig config);
}

public interface INotificationService
{
    Task ShowToastAsync(string message, ToastType type = ToastType.Info);
    Task ShowAlertAsync(string title, string message);
    Task<bool> ShowConfirmAsync(string title, string message);
    void ShowLoading(string message = "Carregando...");
    void HideLoading();
}

public interface ISettingsService
{
    Task<T> GetAsync<T>(string key, T defaultValue = default!);
    Task SetAsync<T>(string key, T value);
    Task RemoveAsync(string key);
    Task ClearAsync();
}

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}