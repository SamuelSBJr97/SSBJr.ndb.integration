using SSBJr.ndb.integration.Models;

namespace SSBJr.ndb.integration.Services;

public class NotificationService : INotificationService
{
    public async Task ShowAlertAsync(string title, string message)
    {
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }
    }

    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        if (Application.Current?.MainPage != null)
        {
            return await Application.Current.MainPage.DisplayAlert(title, message, "Sim", "Não");
        }
        return false;
    }

    public async Task ShowToastAsync(string message, ToastType type = ToastType.Info)
    {
        // MAUI doesn't have built-in toast, using alert as fallback
        var icon = type switch
        {
            ToastType.Success => "?",
            ToastType.Warning => "??",
            ToastType.Error => "?",
            _ => "??"
        };

        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert($"{icon} Notificação", message, "OK");
        }
    }

    public void ShowLoading(string message = "Carregando...")
    {
        // MAUI doesn't have built-in loading indicator
        // This would be implemented with a custom loading view or third-party library
    }

    public void HideLoading()
    {
        // Hide the loading indicator
    }
}

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string key, T defaultValue = default!)
    {
        try
        {
            var value = await SecureStorage.GetAsync(key);
            if (value != null)
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)value;
                }
                
                return System.Text.Json.JsonSerializer.Deserialize<T>(value) ?? defaultValue;
            }
            
            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting {Key}", key);
            return defaultValue;
        }
    }

    public async Task SetAsync<T>(string key, T value)
    {
        try
        {
            string stringValue;
            
            if (typeof(T) == typeof(string))
            {
                stringValue = value?.ToString() ?? string.Empty;
            }
            else
            {
                stringValue = System.Text.Json.JsonSerializer.Serialize(value);
            }
            
            await SecureStorage.SetAsync(key, stringValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            SecureStorage.Remove(key);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing setting {Key}", key);
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            SecureStorage.RemoveAll();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all settings");
        }
    }
}