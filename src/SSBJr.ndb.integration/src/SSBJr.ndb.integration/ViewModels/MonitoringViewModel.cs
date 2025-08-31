using System.Collections.ObjectModel;
using SSBJr.ndb.integration.Models;
using SSBJr.ndb.integration.Services;
using System.Windows.Input;

namespace SSBJr.ndb.integration.ViewModels;

public class MonitoringViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IInfrastructureService _infrastructureService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MonitoringViewModel> _logger;

    private int _totalApis;
    private int _runningApis;
    private int _failedApis;
    private double _systemHealthPercentage;
    private ObservableCollection<ApiStatus> _apiStatuses = new();
    private ObservableCollection<RecentActivity> _recentActivities = new();
    private bool _isRefreshing;
    private string _name = "Monitoring";
    private string _status = "Healthy";
    private DateTime _lastUpdated = DateTime.UtcNow;
    private int _responseTime;
    private string _icon = "heart";

    public MonitoringViewModel(
        IApiService apiService,
        IInfrastructureService infrastructureService,
        INotificationService notificationService,
        ILogger<MonitoringViewModel> logger)
    {
        _apiService = apiService;
        _infrastructureService = infrastructureService;
        _notificationService = notificationService;
        _logger = logger;

        RefreshCommand = new Command(async () => await RefreshDataAsync());
        ViewApiDetailsCommand = new Command<ApiStatus>(async (apiStatus) => await ViewApiDetailsAsync(apiStatus));
        ViewDetailedMetricsCommand = new Command(async () => await ViewDetailedMetricsAsync());
        ViewSystemLogsCommand = new Command(async () => await ViewSystemLogsAsync());
        ConfigureAlertsCommand = new Command(async () => await ConfigureAlertsAsync());
        ViewReportsCommand = new Command(async () => await ViewReportsAsync());

        // Auto refresh every 30 seconds
        StartAutoRefresh();
    }

    public int TotalApis
    {
        get => _totalApis;
        set => SetProperty(ref _totalApis, value);
    }

    public int RunningApis
    {
        get => _runningApis;
        set => SetProperty(ref _runningApis, value);
    }

    public int FailedApis
    {
        get => _failedApis;
        set => SetProperty(ref _failedApis, value);
    }

    public double SystemHealthPercentage
    {
        get => _systemHealthPercentage;
        set
        {
            if (SetProperty(ref _systemHealthPercentage, value))
            {
                OnPropertyChanged(nameof(SystemHealthColor));
                OnPropertyChanged(nameof(SystemHealthIcon));
            }
        }
    }

    public Color SystemHealthColor
    {
        get
        {
            return SystemHealthPercentage switch
            {
                >= 90 => Colors.Green,
                >= 70 => Colors.Orange,
                >= 50 => Colors.Yellow,
                _ => Colors.Red
            };
        }
    }

    public string SystemHealthIcon
    {
        get
        {
            return SystemHealthPercentage switch
            {
                >= 90 => "??",
                >= 70 => "??",
                >= 50 => "??",
                _ => "??"
            };
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public ObservableCollection<ApiStatus> ApiStatuses
    {
        get => _apiStatuses;
        set => SetProperty(ref _apiStatuses, value);
    }

    public ObservableCollection<RecentActivity> RecentActivities
    {
        get => _recentActivities;
        set => SetProperty(ref _recentActivities, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ViewApiDetailsCommand { get; }
    public ICommand ViewDetailedMetricsCommand { get; }
    public ICommand ViewSystemLogsCommand { get; }
    public ICommand ConfigureAlertsCommand { get; }
    public ICommand ViewReportsCommand { get; }

    public DateTime LastUpdated
    {
        get => _lastUpdated;
        set => SetProperty(ref _lastUpdated, value);
    }

    public int ResponseTime
    {
        get => _responseTime;
        set => SetProperty(ref _responseTime, value);
    }

    public string Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    public async Task InitializeAsync()
    {
        await RefreshDataAsync();
    }

    private async Task RefreshDataAsync()
    {
        try
        {
            IsRefreshing = true;
            
            await Task.WhenAll(
                LoadApiStatusesAsync(),
                LoadSystemHealthAsync(),
                LoadRecentActivitiesAsync()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing monitoring data");
            await _notificationService.ShowToastAsync("Erro ao atualizar dados de monitoramento", ToastType.Error);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task LoadApiStatusesAsync()
    {
        try
        {
            var apis = await _apiService.GetApiInterfacesAsync();
            var apiStatuses = new List<ApiStatus>();

            foreach (var api in apis)
            {
                try
                {
                    var metrics = await _apiService.GetApiMetricsAsync(api.Id.ToString());
                    var responseTime = metrics.ContainsKey("response_time") ? 
                        Convert.ToInt32(metrics["response_time"]) : 0;

                    apiStatuses.Add(new ApiStatus
                    {
                        Id = api.Id,
                        Name = api.Name,
                        Status = api.Status.ToString(),
                        StatusIcon = GetStatusIcon(api.Status),
                        ResponseTime = responseTime,
                        LastUpdated = api.UpdatedAt ?? api.CreatedAt
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get metrics for API {ApiName}", api.Name);
                    
                    apiStatuses.Add(new ApiStatus
                    {
                        Id = api.Id,
                        Name = api.Name,
                        Status = api.Status.ToString(),
                        StatusIcon = GetStatusIcon(api.Status),
                        ResponseTime = 0,
                        LastUpdated = api.UpdatedAt ?? api.CreatedAt
                    });
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ApiStatuses.Clear();
                foreach (var status in apiStatuses)
                {
                    ApiStatuses.Add(status);
                }

                // Update summary stats
                TotalApis = apis.Count;
                RunningApis = apis.Count(a => a.Status == Models.ApiStatus.Running);
                FailedApis = apis.Count(a => a.Status == Models.ApiStatus.Failed);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading API statuses");
        }
    }

    private async Task LoadSystemHealthAsync()
    {
        try
        {
            var health = await _infrastructureService.GetSystemHealthAsync();
            
            if (health.Services.Any())
            {
                var healthyServices = health.Services.Count(s => s.Value.IsHealthy);
                SystemHealthPercentage = (double)healthyServices / health.Services.Count * 100;
            }
            else
            {
                SystemHealthPercentage = health.IsHealthy ? 100 : 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading system health");
            SystemHealthPercentage = 0;
        }
    }

    private async Task LoadRecentActivitiesAsync()
    {
        try
        {
            // This would typically come from an activity log service
            // For now, we'll generate some mock data
            var activities = new List<RecentActivity>
            {
                new() { Icon = "?", Message = "API 'UserService' implantada com sucesso", Timestamp = DateTime.Now.AddMinutes(-5) },
                new() { Icon = "??", Message = "Sistema de cache atualizado", Timestamp = DateTime.Now.AddMinutes(-15) },
                new() { Icon = "??", Message = "Alta latência detectada em 'PaymentAPI'", Timestamp = DateTime.Now.AddMinutes(-32) },
                new() { Icon = "??", Message = "Backup automático concluído", Timestamp = DateTime.Now.AddHours(-1) },
                new() { Icon = "??", Message = "Nova versão da API 'OrderService' implantada", Timestamp = DateTime.Now.AddHours(-2) }
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecentActivities.Clear();
                foreach (var activity in activities)
                {
                    RecentActivities.Add(activity);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading recent activities");
        }
    }

    private async Task ViewApiDetailsAsync(ApiStatus apiStatus)
    {
        try
        {
            await Shell.Current.GoToAsync($"//ApiDetailPage?apiId={apiStatus.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to API details");
            await _notificationService.ShowToastAsync("Erro ao navegar para detalhes da API", ToastType.Error);
        }
    }

    private async Task ViewDetailedMetricsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//DetailedMetricsPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to detailed metrics");
            await _notificationService.ShowToastAsync("Funcionalidade em desenvolvimento", ToastType.Info);
        }
    }

    private async Task ViewSystemLogsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//SystemLogsPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to system logs");
            await _notificationService.ShowToastAsync("Funcionalidade em desenvolvimento", ToastType.Info);
        }
    }

    private async Task ConfigureAlertsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//AlertConfigPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to alert configuration");
            await _notificationService.ShowToastAsync("Funcionalidade em desenvolvimento", ToastType.Info);
        }
    }

    private async Task ViewReportsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//ReportsPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to reports");
            await _notificationService.ShowToastAsync("Funcionalidade em desenvolvimento", ToastType.Info);
        }
    }

    private void StartAutoRefresh()
    {
        Device.StartTimer(TimeSpan.FromSeconds(30), () =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (!IsRefreshing)
                {
                    await RefreshDataAsync();
                }
            });
            return true; // Continue the timer
        });
    }

    private string GetStatusIcon(Models.ApiStatus status)
    {
        return status switch
        {
            Models.ApiStatus.Draft => "??",
            Models.ApiStatus.Validating => "??",
            Models.ApiStatus.Deploying => "??",
            Models.ApiStatus.Running => "?",
            Models.ApiStatus.Stopped => "??",
            Models.ApiStatus.Failed => "?",
            Models.ApiStatus.Updating => "??",
            _ => "?"
        };
    }
}

// Support classes
public class ApiStatus
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusIcon { get; set; } = string.Empty;
    public int ResponseTime { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class RecentActivity
{
    public string Icon { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}