using SSBJr.ndb.integration.Services;
using System.Windows.Input;

namespace SSBJr.ndb.integration.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SettingsViewModel> _logger;

    private string _apiBaseUrl = "https://localhost:8080";
    private int _apiTimeout = 30;
    private bool _enableAutoRefresh = true;
    private int _autoRefreshInterval = 30;
    private bool _enableNotifications = true;
    private bool _enableDarkMode = false;
    private string _cacheSize = "0 MB";
    private string _appVersion = "1.0.0";
    private string _appBuild = "1";
    private string _platform = DeviceInfo.Platform.ToString();

    public SettingsViewModel(
        ISettingsService settingsService,
        INotificationService notificationService,
        ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _notificationService = notificationService;
        _logger = logger;

        TestApiConnectionCommand = new Command(async () => await TestApiConnectionAsync());
        ClearCacheCommand = new Command(async () => await ClearCacheAsync());
        ExportSettingsCommand = new Command(async () => await ExportSettingsAsync());
        ImportSettingsCommand = new Command(async () => await ImportSettingsAsync());
        ContactSupportCommand = new Command(async () => await ContactSupportAsync());
        SaveAllCommand = new Command(async () => await SaveAllAsync());
        ResetToDefaultsCommand = new Command(async () => await ResetToDefaultsAsync());
    }

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => SetProperty(ref _apiBaseUrl, value);
    }

    public int ApiTimeout
    {
        get => _apiTimeout;
        set => SetProperty(ref _apiTimeout, value);
    }

    public bool EnableAutoRefresh
    {
        get => _enableAutoRefresh;
        set => SetProperty(ref _enableAutoRefresh, value);
    }

    public int AutoRefreshInterval
    {
        get => _autoRefreshInterval;
        set => SetProperty(ref _autoRefreshInterval, value);
    }

    public bool EnableNotifications
    {
        get => _enableNotifications;
        set => SetProperty(ref _enableNotifications, value);
    }

    public bool EnableDarkMode
    {
        get => _enableDarkMode;
        set => SetProperty(ref _enableDarkMode, value);
    }

    public string CacheSize
    {
        get => _cacheSize;
        set => SetProperty(ref _cacheSize, value);
    }

    public string AppVersion
    {
        get => _appVersion;
        set => SetProperty(ref _appVersion, value);
    }

    public string AppBuild
    {
        get => _appBuild;
        set => SetProperty(ref _appBuild, value);
    }

    public string Platform
    {
        get => _platform;
        set => SetProperty(ref _platform, value);
    }

    public ICommand TestApiConnectionCommand { get; }
    public ICommand ClearCacheCommand { get; }
    public ICommand ExportSettingsCommand { get; }
    public ICommand ImportSettingsCommand { get; }
    public ICommand ContactSupportCommand { get; }
    public ICommand SaveAllCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }

    public async Task InitializeAsync()
    {
        try
        {
            await LoadSettingsAsync();
            await CalculateCacheSizeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing settings");
            await _notificationService.ShowToastAsync("Erro ao carregar configurações", ToastType.Error);
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            ApiBaseUrl = await _settingsService.GetAsync("ApiBaseUrl", "https://localhost:8080");
            ApiTimeout = await _settingsService.GetAsync("ApiTimeout", 30);
            EnableAutoRefresh = await _settingsService.GetAsync("EnableAutoRefresh", true);
            AutoRefreshInterval = await _settingsService.GetAsync("AutoRefreshInterval", 30);
            EnableNotifications = await _settingsService.GetAsync("EnableNotifications", true);
            EnableDarkMode = await _settingsService.GetAsync("EnableDarkMode", false);

            // Get app info
            AppVersion = AppInfo.VersionString;
            AppBuild = AppInfo.BuildString;
            Platform = DeviceInfo.Platform.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
        }
    }

    private async Task CalculateCacheSizeAsync()
    {
        try
        {
            // Calculate app cache size (simplified)
            var cacheDir = FileSystem.CacheDirectory;
            if (Directory.Exists(cacheDir))
            {
                var files = Directory.GetFiles(cacheDir, "*", SearchOption.AllDirectories);
                var totalSize = files.Sum(file => new FileInfo(file).Length);
                var sizeInMB = totalSize / (1024.0 * 1024.0);
                CacheSize = $"{sizeInMB:F1} MB";
            }
            else
            {
                CacheSize = "0 MB";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cache size");
            CacheSize = "N/A";
        }
    }

    private async Task TestApiConnectionAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(ApiBaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(ApiTimeout);

            var response = await httpClient.GetAsync("/api/health");
            var isSuccess = response.IsSuccessStatusCode;

            var message = isSuccess ? "Conexão com API bem-sucedida!" : "Falha na conexão com API";
            var type = isSuccess ? ToastType.Success : ToastType.Error;

            await _notificationService.ShowToastAsync(message, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing API connection");
            await _notificationService.ShowToastAsync($"Erro ao testar conexão: {ex.Message}", ToastType.Error);
        }
    }

    private async Task ClearCacheAsync()
    {
        try
        {
            var confirmResult = await _notificationService.ShowConfirmAsync(
                "Limpar Cache",
                "Deseja realmente limpar todo o cache local do aplicativo?"
            );

            if (!confirmResult) return;

            var cacheDir = FileSystem.CacheDirectory;
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
                Directory.CreateDirectory(cacheDir);
            }

            await CalculateCacheSizeAsync();
            await _notificationService.ShowToastAsync("Cache limpo com sucesso", ToastType.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            await _notificationService.ShowToastAsync("Erro ao limpar cache", ToastType.Error);
        }
    }

    private async Task ExportSettingsAsync()
    {
        try
        {
            var settings = new
            {
                ApiBaseUrl,
                ApiTimeout,
                EnableAutoRefresh,
                AutoRefreshInterval,
                EnableNotifications,
                EnableDarkMode,
                ExportedAt = DateTime.Now
            };

            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            var fileName = $"SSBJr_Settings_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            
            await File.WriteAllTextAsync(filePath, json);

            await _notificationService.ShowToastAsync($"Configurações exportadas para {fileName}", ToastType.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting settings");
            await _notificationService.ShowToastAsync("Erro ao exportar configurações", ToastType.Error);
        }
    }

    private async Task ImportSettingsAsync()
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.json" } },
                    { DevicePlatform.Android, new[] { "application/json" } },
                    { DevicePlatform.WinUI, new[] { ".json" } },
                    { DevicePlatform.Tizen, new[] { "*/*" } },
                    { DevicePlatform.macOS, new[] { "json" } },
                });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecione arquivo de configurações",
                FileTypes = customFileType
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                
                if (settings != null)
                {
                    if (settings.TryGetValue("ApiBaseUrl", out var apiBaseUrl))
                        ApiBaseUrl = apiBaseUrl.ToString() ?? ApiBaseUrl;
                    
                    if (settings.TryGetValue("ApiTimeout", out var apiTimeout))
                        ApiTimeout = Convert.ToInt32(apiTimeout);
                    
                    if (settings.TryGetValue("EnableAutoRefresh", out var enableAutoRefresh))
                        EnableAutoRefresh = Convert.ToBoolean(enableAutoRefresh);
                    
                    if (settings.TryGetValue("AutoRefreshInterval", out var autoRefreshInterval))
                        AutoRefreshInterval = Convert.ToInt32(autoRefreshInterval);
                    
                    if (settings.TryGetValue("EnableNotifications", out var enableNotifications))
                        EnableNotifications = Convert.ToBoolean(enableNotifications);
                    
                    if (settings.TryGetValue("EnableDarkMode", out var enableDarkMode))
                        EnableDarkMode = Convert.ToBoolean(enableDarkMode);

                    await SaveAllAsync();
                    await _notificationService.ShowToastAsync("Configurações importadas com sucesso", ToastType.Success);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing settings");
            await _notificationService.ShowToastAsync("Erro ao importar configurações", ToastType.Error);
        }
    }

    private async Task ContactSupportAsync()
    {
        try
        {
            var result = await _notificationService.ShowConfirmAsync(
                "Contatar Suporte",
                "Deseja abrir o email para contato com o suporte técnico?"
            );

            if (result)
            {
                await Email.Default.ComposeAsync(new EmailMessage
                {
                    To = new List<string> { "support@ssbjr.com" },
                    Subject = "Suporte - SSBJr API Manager",
                    Body = $"Versão do App: {AppVersion}\nBuild: {AppBuild}\nPlataforma: {Platform}\n\nDescreva seu problema:\n"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error contacting support");
            await _notificationService.ShowToastAsync("Erro ao abrir email", ToastType.Error);
        }
    }

    private async Task SaveAllAsync()
    {
        try
        {
            await _settingsService.SetAsync("ApiBaseUrl", ApiBaseUrl);
            await _settingsService.SetAsync("ApiTimeout", ApiTimeout);
            await _settingsService.SetAsync("EnableAutoRefresh", EnableAutoRefresh);
            await _settingsService.SetAsync("AutoRefreshInterval", AutoRefreshInterval);
            await _settingsService.SetAsync("EnableNotifications", EnableNotifications);
            await _settingsService.SetAsync("EnableDarkMode", EnableDarkMode);

            await _notificationService.ShowToastAsync("Configurações salvas com sucesso", ToastType.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving all settings");
            await _notificationService.ShowToastAsync("Erro ao salvar configurações", ToastType.Error);
        }
    }

    private async Task ResetToDefaultsAsync()
    {
        try
        {
            var confirmResult = await _notificationService.ShowConfirmAsync(
                "Restaurar Padrões",
                "Deseja restaurar todas as configurações para os valores padrão?"
            );

            if (!confirmResult) return;

            ApiBaseUrl = "https://localhost:8080";
            ApiTimeout = 30;
            EnableAutoRefresh = true;
            AutoRefreshInterval = 30;
            EnableNotifications = true;
            EnableDarkMode = false;

            await SaveAllAsync();
            await _notificationService.ShowToastAsync("Configurações restauradas para os padrões", ToastType.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting to defaults");
            await _notificationService.ShowToastAsync("Erro ao restaurar configurações", ToastType.Error);
        }
    }
}