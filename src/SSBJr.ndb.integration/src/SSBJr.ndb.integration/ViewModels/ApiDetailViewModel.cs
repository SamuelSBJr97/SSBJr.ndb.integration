using SSBJr.ndb.integration.Models;
using SSBJr.ndb.integration.Services;
using System.Windows.Input;

namespace SSBJr.ndb.integration.ViewModels;

[QueryProperty(nameof(ApiId), "apiId")]
public class ApiDetailViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IInfrastructureService _infrastructureService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ApiDetailViewModel> _logger;

    private string _apiId = string.Empty;
    private string _apiName = string.Empty;
    private string _apiDescription = string.Empty;
    private Models.ApiStatus _status = Models.ApiStatus.Draft;
    private int _responseTime;
    private int _requestsToday;
    private string _uptime = "0h 0m";
    private double _errorRate;
    private string _databaseTypeName = string.Empty;
    private string _databaseStatus = string.Empty;
    private string _databaseStatusIcon = "?";
    private string _messagingTypeName = string.Empty;
    private string _messagingStatus = string.Empty;
    private string _messagingStatusIcon = "?";
    private string _cacheTypeName = string.Empty;
    private string _cacheStatus = string.Empty;
    private string _cacheStatusIcon = "?";
    private bool _hasMessaging;
    private bool _hasCache;
    private ApiInterface? _currentApi;

    public ApiDetailViewModel(
        IApiService apiService,
        IInfrastructureService infrastructureService,
        INotificationService notificationService,
        ILogger<ApiDetailViewModel> logger)
    {
        _apiService = apiService;
        _infrastructureService = infrastructureService;
        _notificationService = notificationService;
        _logger = logger;

        ConfigureInfrastructureCommand = new Command(async () => await ConfigureInfrastructureAsync());
        ViewMetricsCommand = new Command(async () => await ViewMetricsAsync());
        ViewLogsCommand = new Command(async () => await ViewLogsAsync());
        DeployCommand = new Command(async () => await DeployAsync());
        StopCommand = new Command(async () => await StopAsync());
        RestartCommand = new Command(async () => await RestartAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    public string ApiId
    {
        get => _apiId;
        set => SetProperty(ref _apiId, value);
    }

    public string ApiName
    {
        get => _apiName;
        set => SetProperty(ref _apiName, value);
    }

    public string ApiDescription
    {
        get => _apiDescription;
        set => SetProperty(ref _apiDescription, value);
    }

    public Models.ApiStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(CanDeploy));
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(CanRestart));
            }
        }
    }

    public int ResponseTime
    {
        get => _responseTime;
        set => SetProperty(ref _responseTime, value);
    }

    public int RequestsToday
    {
        get => _requestsToday;
        set => SetProperty(ref _requestsToday, value);
    }

    public string Uptime
    {
        get => _uptime;
        set => SetProperty(ref _uptime, value);
    }

    public double ErrorRate
    {
        get => _errorRate;
        set
        {
            if (SetProperty(ref _errorRate, value))
            {
                OnPropertyChanged(nameof(ErrorRateColor));
            }
        }
    }

    public string DatabaseTypeName
    {
        get => _databaseTypeName;
        set => SetProperty(ref _databaseTypeName, value);
    }

    public string DatabaseStatus
    {
        get => _databaseStatus;
        set => SetProperty(ref _databaseStatus, value);
    }

    public string DatabaseStatusIcon
    {
        get => _databaseStatusIcon;
        set => SetProperty(ref _databaseStatusIcon, value);
    }

    public string MessagingTypeName
    {
        get => _messagingTypeName;
        set => SetProperty(ref _messagingTypeName, value);
    }

    public string MessagingStatus
    {
        get => _messagingStatus;
        set => SetProperty(ref _messagingStatus, value);
    }

    public string MessagingStatusIcon
    {
        get => _messagingStatusIcon;
        set => SetProperty(ref _messagingStatusIcon, value);
    }

    public string CacheTypeName
    {
        get => _cacheTypeName;
        set => SetProperty(ref _cacheTypeName, value);
    }

    public string CacheStatus
    {
        get => _cacheStatus;
        set => SetProperty(ref _cacheStatus, value);
    }

    public string CacheStatusIcon
    {
        get => _cacheStatusIcon;
        set => SetProperty(ref _cacheStatusIcon, value);
    }

    public bool HasMessaging
    {
        get => _hasMessaging;
        set => SetProperty(ref _hasMessaging, value);
    }

    public bool HasCache
    {
        get => _hasCache;
        set => SetProperty(ref _hasCache, value);
    }

    public string StatusText => Status.ToString();
    public Color StatusColor => GetStatusColor(Status);
    public Color ErrorRateColor => ErrorRate > 5 ? Colors.Red : ErrorRate > 2 ? Colors.Orange : Colors.Green;
    
    public bool CanDeploy => Status == Models.ApiStatus.Draft || Status == Models.ApiStatus.Stopped || Status == Models.ApiStatus.Failed;
    public bool CanStop => Status == Models.ApiStatus.Running || Status == Models.ApiStatus.Deploying;
    public bool CanRestart => Status == Models.ApiStatus.Running || Status == Models.ApiStatus.Failed;

    public ICommand ConfigureInfrastructureCommand { get; }
    public ICommand ViewMetricsCommand { get; }
    public ICommand ViewLogsCommand { get; }
    public ICommand DeployCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand RestartCommand { get; }
    public ICommand RefreshCommand { get; }

    public async Task InitializeAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(ApiId))
            {
                await LoadApiDetailsAsync();
                await LoadMetricsAsync();
                await LoadInfrastructureStatusAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing API details");
            await _notificationService.ShowAlertAsync("Erro", "Falha ao carregar detalhes da API");
        }
    }

    private async Task LoadApiDetailsAsync()
    {
        try
        {
            _currentApi = await _apiService.GetApiInterfaceAsync(ApiId);
            
            ApiName = _currentApi.Name;
            ApiDescription = _currentApi.Description;
            Status = _currentApi.Status;

            // Load infrastructure info
            var infrastructure = _currentApi.Infrastructure;
            DatabaseTypeName = infrastructure.Database.Type.ToString();
            DatabaseStatus = "Conectado";
            DatabaseStatusIcon = "?";

            HasMessaging = infrastructure.Messaging.Type != MessagingType.None;
            if (HasMessaging)
            {
                MessagingTypeName = infrastructure.Messaging.Type.ToString();
                MessagingStatus = "Conectado";
                MessagingStatusIcon = "?";
            }

            HasCache = infrastructure.Cache.Type != CacheType.None;
            if (HasCache)
            {
                CacheTypeName = infrastructure.Cache.Type.ToString();
                CacheStatus = "Conectado";
                CacheStatusIcon = "?";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading API details");
            throw;
        }
    }

    private async Task LoadMetricsAsync()
    {
        try
        {
            var metrics = await _apiService.GetApiMetricsAsync(ApiId);
            
            ResponseTime = metrics.ContainsKey("response_time") ? Convert.ToInt32(metrics["response_time"]) : 0;
            RequestsToday = metrics.ContainsKey("requests_today") ? Convert.ToInt32(metrics["requests_today"]) : 0;
            ErrorRate = metrics.ContainsKey("error_rate") ? Convert.ToDouble(metrics["error_rate"]) : 0.0;
            
            if (metrics.ContainsKey("uptime_seconds"))
            {
                var uptimeSeconds = Convert.ToInt32(metrics["uptime_seconds"]);
                var hours = uptimeSeconds / 3600;
                var minutes = (uptimeSeconds % 3600) / 60;
                Uptime = $"{hours}h {minutes}m";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading API metrics");
            // Set default values on error
            ResponseTime = 0;
            RequestsToday = 0;
            ErrorRate = 0.0;
            Uptime = "N/A";
        }
    }

    private async Task LoadInfrastructureStatusAsync()
    {
        try
        {
            var status = await _infrastructureService.GetInfrastructureStatusAsync(ApiId);
            
            // Update status based on infrastructure health
            if (status.ContainsKey("database"))
            {
                var dbHealthy = Convert.ToBoolean(status["database"]);
                DatabaseStatus = dbHealthy ? "Conectado" : "Erro de Conexão";
                DatabaseStatusIcon = dbHealthy ? "?" : "?";
            }

            if (HasMessaging && status.ContainsKey("messaging"))
            {
                var msgHealthy = Convert.ToBoolean(status["messaging"]);
                MessagingStatus = msgHealthy ? "Conectado" : "Erro de Conexão";
                MessagingStatusIcon = msgHealthy ? "?" : "?";
            }

            if (HasCache && status.ContainsKey("cache"))
            {
                var cacheHealthy = Convert.ToBoolean(status["cache"]);
                CacheStatus = cacheHealthy ? "Conectado" : "Erro de Conexão";
                CacheStatusIcon = cacheHealthy ? "?" : "?";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading infrastructure status");
        }
    }

    private async Task RefreshAsync()
    {
        await InitializeAsync();
        await _notificationService.ShowToastAsync("Dados atualizados", ToastType.Success);
    }

    private async Task ConfigureInfrastructureAsync()
    {
        await Shell.Current.GoToAsync($"//InfrastructureConfigPage?apiId={ApiId}");
    }

    private async Task ViewMetricsAsync()
    {
        await Shell.Current.GoToAsync($"//ApiMetricsPage?apiId={ApiId}");
    }

    private async Task ViewLogsAsync()
    {
        await Shell.Current.GoToAsync($"//ApiLogsPage?apiId={ApiId}");
    }

    private async Task DeployAsync()
    {
        try
        {
            var result = await _apiService.DeployApiInterfaceAsync(ApiId);
            if (result)
            {
                Status = Models.ApiStatus.Deploying;
                await _notificationService.ShowToastAsync("Deploy iniciado com sucesso", ToastType.Success);
                
                // Refresh after delay
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    await RefreshAsync();
                });
            }
            else
            {
                await _notificationService.ShowToastAsync("Falha no deploy", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying API");
            await _notificationService.ShowToastAsync("Erro no deploy", ToastType.Error);
        }
    }

    private async Task StopAsync()
    {
        try
        {
            var confirmResult = await _notificationService.ShowConfirmAsync(
                "Parar API", 
                $"Deseja parar a API '{ApiName}'?"
            );

            if (!confirmResult) return;

            var result = await _apiService.StopApiInterfaceAsync(ApiId);
            if (result)
            {
                Status = Models.ApiStatus.Stopped;
                await _notificationService.ShowToastAsync("API parada com sucesso", ToastType.Success);
            }
            else
            {
                await _notificationService.ShowToastAsync("Falha ao parar API", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping API");
            await _notificationService.ShowToastAsync("Erro ao parar API", ToastType.Error);
        }
    }

    private async Task RestartAsync()
    {
        try
        {
            var result = await _apiService.RestartApiInterfaceAsync(ApiId);
            if (result)
            {
                Status = Models.ApiStatus.Running;
                await _notificationService.ShowToastAsync("API reiniciada com sucesso", ToastType.Success);
            }
            else
            {
                await _notificationService.ShowToastAsync("Falha ao reiniciar API", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting API");
            await _notificationService.ShowToastAsync("Erro ao reiniciar API", ToastType.Error);
        }
    }

    private Color GetStatusColor(Models.ApiStatus status)
    {
        return status switch
        {
            Models.ApiStatus.Draft => Colors.Gray,
            Models.ApiStatus.Validating => Colors.Orange,
            Models.ApiStatus.Deploying => Colors.Blue,
            Models.ApiStatus.Running => Colors.Green,
            Models.ApiStatus.Stopped => Colors.Red,
            Models.ApiStatus.Failed => Colors.Red,
            Models.ApiStatus.Updating => Colors.Orange,
            _ => Colors.Gray
        };
    }
}