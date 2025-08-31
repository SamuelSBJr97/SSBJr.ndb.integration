using System.Collections.ObjectModel;
using SSBJr.ndb.integration.Models;
using SSBJr.ndb.integration.Services;
using System.Windows.Input;

namespace SSBJr.ndb.integration.ViewModels;

public class ApiManagerViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly INotificationService _notificationService;
    private readonly IInfrastructureService _infrastructureService;
    private readonly ILogger<ApiManagerViewModel> _logger;

    private bool _isLoading;
    private bool _isRefreshing;
    private string _searchText = string.Empty;
    private ApiInterface? _selectedApi;
    private ObservableCollection<ApiInterface> _apiInterfaces = new();
    private ObservableCollection<ApiInterface> _filteredApiInterfaces = new();

    public ApiManagerViewModel(
        IApiService apiService,
        INotificationService notificationService,
        IInfrastructureService infrastructureService,
        ILogger<ApiManagerViewModel> logger)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        _infrastructureService = infrastructureService;
        _logger = logger;

        LoadApisCommand = new Command(async () => await LoadApisAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());
        SelectApiCommand = new Command<ApiInterface>(async (api) => await SelectApiAsync(api));
        CreateApiCommand = new Command(async () => await CreateApiAsync());
        EditApiCommand = new Command<ApiInterface>(async (api) => await EditApiAsync(api));
        DeleteApiCommand = new Command<ApiInterface>(async (api) => await DeleteApiAsync(api));
        DeployApiCommand = new Command<ApiInterface>(async (api) => await DeployApiAsync(api));
        StopApiCommand = new Command<ApiInterface>(async (api) => await StopApiAsync(api));
        RestartApiCommand = new Command<ApiInterface>(async (api) => await RestartApiAsync(api));
        ViewDetailsCommand = new Command<ApiInterface>(async (api) => await ViewDetailsAsync(api));
        ViewLogsCommand = new Command<ApiInterface>(async (api) => await ViewLogsAsync(api));
        ViewMetricsCommand = new Command<ApiInterface>(async (api) => await ViewMetricsAsync(api));
        ConfigureInfrastructureCommand = new Command<ApiInterface>(async (api) => await ConfigureInfrastructureAsync(api));

        // Filter apis when search text changes
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchText))
        {
            FilterApis();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public ApiInterface? SelectedApi
    {
        get => _selectedApi;
        set => SetProperty(ref _selectedApi, value);
    }

    public ObservableCollection<ApiInterface> ApiInterfaces
    {
        get => _apiInterfaces;
        set => SetProperty(ref _apiInterfaces, value);
    }

    public ObservableCollection<ApiInterface> FilteredApiInterfaces
    {
        get => _filteredApiInterfaces;
        set => SetProperty(ref _filteredApiInterfaces, value);
    }

    public ICommand LoadApisCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectApiCommand { get; }
    public ICommand CreateApiCommand { get; }
    public ICommand EditApiCommand { get; }
    public ICommand DeleteApiCommand { get; }
    public ICommand DeployApiCommand { get; }
    public ICommand StopApiCommand { get; }
    public ICommand RestartApiCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand ViewLogsCommand { get; }
    public ICommand ViewMetricsCommand { get; }
    public ICommand ConfigureInfrastructureCommand { get; }

    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            await LoadApisAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing API manager");
            await _notificationService.ShowAlertAsync("Erro", "Falha ao carregar gerenciador de APIs");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadApisAsync()
    {
        try
        {
            var apis = await _apiService.GetApiInterfacesAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ApiInterfaces.Clear();
                foreach (var api in apis)
                {
                    ApiInterfaces.Add(api);
                }
                FilterApis();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading APIs");
            await _notificationService.ShowToastAsync("Erro ao carregar APIs", ToastType.Error);
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;
            await LoadApisAsync();
            await _notificationService.ShowToastAsync("APIs atualizadas", ToastType.Success);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task SelectApiAsync(ApiInterface api)
    {
        SelectedApi = api;
        await Task.CompletedTask;
    }

    private async Task CreateApiAsync()
    {
        try
        {
            // Navigate to create API page
            await Shell.Current.GoToAsync("//CreateApiPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to create API page");
            await _notificationService.ShowAlertAsync("Erro", "Falha ao navegar para criação de API");
        }
    }

    private async Task EditApiAsync(ApiInterface api)
    {
        try
        {
            // Navigate to edit API page with parameter
            await Shell.Current.GoToAsync($"//EditApiPage?apiId={api.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to edit API page");
            await _notificationService.ShowAlertAsync("Erro", "Falha ao navegar para edição de API");
        }
    }

    private async Task DeleteApiAsync(ApiInterface api)
    {
        try
        {
            var confirmResult = await _notificationService.ShowConfirmAsync(
                "Confirmar Exclusão",
                $"Deseja realmente excluir a API '{api.Name}'?\n\nEsta ação não pode ser desfeita."
            );

            if (!confirmResult) return;

            IsLoading = true;
            _notificationService.ShowLoading("Excluindo API...");

            await _apiService.DeleteApiInterfaceAsync(api.Id.ToString());

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ApiInterfaces.Remove(api);
                FilterApis();
            });

            await _notificationService.ShowToastAsync($"API '{api.Name}' excluída com sucesso", ToastType.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API {ApiName}", api.Name);
            await _notificationService.ShowAlertAsync("Erro", $"Falha ao excluir API: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            _notificationService.HideLoading();
        }
    }

    private async Task DeployApiAsync(ApiInterface api)
    {
        try
        {
            IsLoading = true;
            _notificationService.ShowLoading($"Implantando API '{api.Name}'...");

            var result = await _apiService.DeployApiInterfaceAsync(api.Id.ToString());

            if (result)
            {
                // Update API status
                api.Status = Models.ApiStatus.Deploying;
                await _notificationService.ShowToastAsync($"Implantação da API '{api.Name}' iniciada", ToastType.Success);
                
                // Refresh API data after a delay to get updated status
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    await RefreshSingleApiAsync(api.Id.ToString());
                });
            }
            else
            {
                await _notificationService.ShowAlertAsync("Erro", $"Falha ao implantar API '{api.Name}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying API {ApiName}", api.Name);
            await _notificationService.ShowAlertAsync("Erro", $"Falha ao implantar API: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            _notificationService.HideLoading();
        }
    }

    private async Task StopApiAsync(ApiInterface api)
    {
        try
        {
            var confirmResult = await _notificationService.ShowConfirmAsync(
                "Parar API",
                $"Deseja parar a API '{api.Name}'?"
            );

            if (!confirmResult) return;

            IsLoading = true;
            _notificationService.ShowLoading($"Parando API '{api.Name}'...");

            var result = await _apiService.StopApiInterfaceAsync(api.Id.ToString());

            if (result)
            {
                api.Status = Models.ApiStatus.Stopped;
                await _notificationService.ShowToastAsync($"API '{api.Name}' parada com sucesso", ToastType.Success);
            }
            else
            {
                await _notificationService.ShowAlertAsync("Erro", $"Falha ao parar API '{api.Name}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping API {ApiName}", api.Name);
            await _notificationService.ShowAlertAsync("Erro", $"Falha ao parar API: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            _notificationService.HideLoading();
        }
    }

    private async Task RestartApiAsync(ApiInterface api)
    {
        try
        {
            IsLoading = true;
            _notificationService.ShowLoading($"Reiniciando API '{api.Name}'...");

            var result = await _apiService.RestartApiInterfaceAsync(api.Id.ToString());

            if (result)
            {
                api.Status = Models.ApiStatus.Running;
                await _notificationService.ShowToastAsync($"API '{api.Name}' reiniciada com sucesso", ToastType.Success);
            }
            else
            {
                await _notificationService.ShowAlertAsync("Erro", $"Falha ao reiniciar API '{api.Name}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting API {ApiName}", api.Name);
            await _notificationService.ShowAlertAsync("Erro", $"Falha ao reiniciar API: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            _notificationService.HideLoading();
        }
    }

    private async Task ViewDetailsAsync(ApiInterface api)
    {
        try
        {
            await Shell.Current.GoToAsync($"//ApiDetailPage?apiId={api.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to API details");
            await _notificationService.ShowAlertAsync("Erro", "Falha ao navegar para detalhes da API");
        }
    }

    private async Task ViewLogsAsync(ApiInterface api)
    {
        try
        {
            await Shell.Current.GoToAsync($"//ApiLogsPage?apiId={api.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to API logs");
            await _notificationService.ShowAlertAsync("Erro", "Falha ao navegar para logs da API");
        }
    }

    private async Task ViewMetricsAsync(ApiInterface api)
    {
        try
        {
            await Shell.Current.GoToAsync($"//ApiMetricsPage?apiId={api.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to API metrics");
            await _notificationService.ShowAlertAsync("Erro", "Falha ao navegar para métricas da API");
        }
    }

    private async Task ConfigureInfrastructureAsync(ApiInterface api)
    {
        try
        {
            await Shell.Current.GoToAsync($"//InfrastructureConfigPage?apiId={api.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to infrastructure configuration");
            await _notificationService.ShowAlertAsync("Erro", "Falha ao navegar para configuração de infraestrutura");
        }
    }

    private async Task RefreshSingleApiAsync(string apiId)
    {
        try
        {
            var updatedApi = await _apiService.GetApiInterfaceAsync(apiId);
            var index = ApiInterfaces.ToList().FindIndex(x => x.Id.ToString() == apiId);
            
            if (index >= 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ApiInterfaces[index] = updatedApi;
                    FilterApis();
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing single API {ApiId}", apiId);
        }
    }

    private void FilterApis()
    {
        var filteredList = string.IsNullOrWhiteSpace(SearchText)
            ? ApiInterfaces.ToList()
            : ApiInterfaces.Where(api =>
                api.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                api.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                api.Tags.Any(tag => tag.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            ).ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            FilteredApiInterfaces.Clear();
            foreach (var api in filteredList)
            {
                FilteredApiInterfaces.Add(api);
            }
        });
    }

    public string GetStatusIcon(Models.ApiStatus status)
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

    public Color GetStatusColor(Models.ApiStatus status)
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