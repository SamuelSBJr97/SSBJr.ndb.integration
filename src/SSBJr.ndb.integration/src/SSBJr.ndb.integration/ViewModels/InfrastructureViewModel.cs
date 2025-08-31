using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SSBJr.ndb.integration.Models;
using SSBJr.ndb.integration.Services;
using System.Windows.Input;

namespace SSBJr.ndb.integration.ViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class InfrastructureViewModel : BaseViewModel
{
    private readonly IInfrastructureService _infrastructureService;
    private readonly INotificationService _notificationService;
    private readonly IApiService _apiService;
    private readonly ILogger<InfrastructureViewModel> _logger;

    private bool _isLoading;
    private string _searchText = string.Empty;
    private InfrastructureTemplate? _selectedTemplate;
    private SystemHealth? _systemHealth;
    private ObservableCollection<InfrastructureTemplate> _templates = new();
    private ObservableCollection<ApiInterface> _apiInterfaces = new();

    public InfrastructureViewModel(
        IInfrastructureService infrastructureService,
        INotificationService notificationService,
        IApiService apiService,
        ILogger<InfrastructureViewModel> logger)
    {
        _infrastructureService = infrastructureService;
        _notificationService = notificationService;
        _apiService = apiService;
        _logger = logger;

        LoadTemplatesCommand = new Command(async () => await LoadTemplatesAsync());
        LoadSystemHealthCommand = new Command(async () => await LoadSystemHealthAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());
        SelectTemplateCommand = new Command<InfrastructureTemplate>(async (template) => await SelectTemplateAsync(template));
        ApplyTemplateCommand = new Command<ApiInterface>(async (api) => await ApplyTemplateAsync(api));
        TestDatabaseCommand = new Command<DatabaseConfig>(async (config) => await TestDatabaseConnectionAsync(config));
        TestMessagingCommand = new Command<MessagingConfig>(async (config) => await TestMessagingConnectionAsync(config));
        TestCacheCommand = new Command<CacheConfig>(async (config) => await TestCacheConnectionAsync(config));
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public InfrastructureTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set => SetProperty(ref _selectedTemplate, value);
    }

    public SystemHealth? SystemHealth
    {
        get => _systemHealth;
        set => SetProperty(ref _systemHealth, value);
    }

    public ObservableCollection<InfrastructureTemplate> Templates
    {
        get => _templates;
        set => SetProperty(ref _templates, value);
    }

    public ObservableCollection<ApiInterface> ApiInterfaces
    {
        get => _apiInterfaces;
        set => SetProperty(ref _apiInterfaces, value);
    }

    public ICommand LoadTemplatesCommand { get; }
    public ICommand LoadSystemHealthCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectTemplateCommand { get; }
    public ICommand ApplyTemplateCommand { get; }
    public ICommand TestDatabaseCommand { get; }
    public ICommand TestMessagingCommand { get; }
    public ICommand TestCacheCommand { get; }

    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            await Task.WhenAll(
                LoadTemplatesAsync(),
                LoadSystemHealthAsync(),
                LoadApiInterfacesAsync()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing infrastructure view");
            await _notificationService.ShowAlertAsync("Erro", "Falha ao carregar dados de infraestrutura");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTemplatesAsync()
    {
        try
        {
            var templates = await _infrastructureService.GetTemplatesAsync();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Templates.Clear();
                foreach (var template in templates)
                {
                    Templates.Add(template);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading infrastructure templates");
            await _notificationService.ShowToastAsync("Erro ao carregar templates", ToastType.Error);
        }
    }

    private async Task LoadSystemHealthAsync()
    {
        try
        {
            var health = await _infrastructureService.GetSystemHealthAsync();
            SystemHealth = health;

            if (!health.IsHealthy)
            {
                await _notificationService.ShowToastAsync("Sistema com problemas de saúde", ToastType.Warning);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading system health");
            SystemHealth = new SystemHealth
            {
                IsHealthy = false,
                LastCheck = DateTime.UtcNow,
                Services = new Dictionary<string, ServiceStatus>
                {
                    ["Sistema"] = new ServiceStatus 
                    { 
                        IsHealthy = false, 
                        Status = "Erro", 
                        ErrorMessage = "Falha ao verificar saúde do sistema" 
                    }
                }
            };
        }
    }

    private async Task LoadApiInterfacesAsync()
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
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading API interfaces");
            await _notificationService.ShowToastAsync("Erro ao carregar APIs", ToastType.Error);
        }
    }

    private async Task RefreshAsync()
    {
        await InitializeAsync();
        await _notificationService.ShowToastAsync("Dados atualizados", ToastType.Success);
    }

    private async Task SelectTemplateAsync(InfrastructureTemplate template)
    {
        try
        {
            SelectedTemplate = template;
            await _notificationService.ShowToastAsync($"Template '{template.Name}' selecionado", ToastType.Info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting template");
            await _notificationService.ShowToastAsync("Erro ao selecionar template", ToastType.Error);
        }
    }

    private async Task ApplyTemplateAsync(ApiInterface api)
    {
        try
        {
            if (SelectedTemplate == null)
            {
                await _notificationService.ShowAlertAsync("Aviso", "Selecione um template primeiro");
                return;
            }

            var confirmResult = await _notificationService.ShowConfirmAsync(
                "Aplicar Template",
                $"Deseja aplicar o template '{SelectedTemplate.Name}' à API '{api.Name}'?\n\nIsto substituirá a configuração atual de infraestrutura.");

            if (!confirmResult) return;

            IsLoading = true;
            _notificationService.ShowLoading("Aplicando template...");

            var updateRequest = new UpdateApiInterfaceRequest
            {
                Infrastructure = SelectedTemplate.Configuration
            };

            await _apiService.UpdateApiInterfaceAsync(api.Id.ToString(), updateRequest);
            
            // Update local data
            var updatedApi = await _apiService.GetApiInterfaceAsync(api.Id.ToString());
            var index = ApiInterfaces.ToList().FindIndex(x => x.Id == api.Id);
            if (index >= 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ApiInterfaces[index] = updatedApi;
                });
            }

            await _notificationService.ShowToastAsync($"Template aplicado com sucesso à API '{api.Name}'", ToastType.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying template to API {ApiName}", api.Name);
            await _notificationService.ShowAlertAsync("Erro", $"Falha ao aplicar template: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            _notificationService.HideLoading();
        }
    }

    private async Task TestDatabaseConnectionAsync(DatabaseConfig config)
    {
        try
        {
            IsLoading = true;
            _notificationService.ShowLoading("Testando conexão com banco de dados...");

            var result = await _infrastructureService.TestDatabaseConnectionAsync(config);
            
            var message = result ? "Conexão com banco de dados bem-sucedida" : "Falha na conexão com banco de dados";
            var type = result ? ToastType.Success : ToastType.Error;
            
            await _notificationService.ShowToastAsync(message, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing database connection");
            await _notificationService.ShowToastAsync("Erro ao testar conexão com banco de dados", ToastType.Error);
        }
        finally
        {
            IsLoading = false;
            _notificationService.HideLoading();
        }
    }

    private async Task TestMessagingConnectionAsync(MessagingConfig config)
    {
        try
        {
            IsLoading = true;
            _notificationService.ShowLoading("Testando conexão com sistema de mensageria...");

            var result = await _infrastructureService.TestMessagingConnectionAsync(config);
            
            var message = result ? "Conexão com sistema de mensageria bem-sucedida" : "Falha na conexão com sistema de mensageria";
            var type = result ? ToastType.Success : ToastType.Error;
            
            await _notificationService.ShowToastAsync(message, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing messaging connection");
            await _notificationService.ShowToastAsync("Erro ao testar conexão com mensageria", ToastType.Error);
        }
        finally
        {
            IsLoading = false;
            _notificationService.HideLoading();
        }
    }

    private async Task TestCacheConnectionAsync(CacheConfig config)
    {
        try
        {
            IsLoading = true;
            _notificationService.ShowLoading("Testando conexão com cache...");

            var result = await _infrastructureService.TestCacheConnectionAsync(config);
            
            var message = result ? "Conexão com cache bem-sucedida" : "Falha na conexão com cache";
            var type = result ? ToastType.Success : ToastType.Error;
            
            await _notificationService.ShowToastAsync(message, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing cache connection");
            await _notificationService.ShowToastAsync("Erro ao testar conexão com cache", ToastType.Error);
        }
        finally
        {
            IsLoading = false;
            _notificationService.HideLoading();
        }
    }
}