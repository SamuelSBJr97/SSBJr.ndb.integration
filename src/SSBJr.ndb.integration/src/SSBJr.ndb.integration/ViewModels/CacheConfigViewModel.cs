using SSBJr.ndb.integration.Models;
using SSBJr.ndb.integration.Services;
using System.Windows.Input;

namespace SSBJr.ndb.integration.ViewModels;

[QueryProperty(nameof(ApiId), "apiId")]
public class CacheConfigViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IInfrastructureService _infrastructureService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CacheConfigViewModel> _logger;

    private string _apiId = string.Empty;
    private CacheType _selectedType = CacheType.Redis;
    private string _connectionString = string.Empty;
    private int _defaultTTL = 3600;
    private bool _enableDistributed = true;
    private bool _isTesting;
    private TestResult? _testResult;
    private ApiInterface? _currentApi;

    public CacheConfigViewModel(
        IApiService apiService,
        IInfrastructureService infrastructureService,
        INotificationService notificationService,
        ILogger<CacheConfigViewModel> logger)
    {
        _apiService = apiService;
        _infrastructureService = infrastructureService;
        _notificationService = notificationService;
        _logger = logger;

        SelectCacheTypeCommand = new Command<string>(async (type) => await SelectCacheTypeAsync(type));
        TestConnectionCommand = new Command(async () => await TestConnectionAsync());
        SaveConfigCommand = new Command(async () => await SaveConfigurationAsync());
        CancelCommand = new Command(async () => await CancelAsync());
    }

    public string ApiId
    {
        get => _apiId;
        set => SetProperty(ref _apiId, value);
    }

    public CacheType SelectedType
    {
        get => _selectedType;
        set
        {
            if (SetProperty(ref _selectedType, value))
            {
                OnPropertyChanged(nameof(SelectedTypeName));
                OnPropertyChanged(nameof(SelectedTypeDescription));
                OnPropertyChanged(nameof(SelectedTypeIcon));
                OnPropertyChanged(nameof(RequiresConnectionString));
            }
        }
    }

    public string ConnectionString
    {
        get => _connectionString;
        set => SetProperty(ref _connectionString, value);
    }

    public int DefaultTTL
    {
        get => _defaultTTL;
        set
        {
            if (SetProperty(ref _defaultTTL, value))
            {
                OnPropertyChanged(nameof(TTLFormatted));
            }
        }
    }

    public bool EnableDistributed
    {
        get => _enableDistributed;
        set => SetProperty(ref _enableDistributed, value);
    }

    public bool IsTesting
    {
        get => _isTesting;
        set
        {
            if (SetProperty(ref _isTesting, value))
            {
                OnPropertyChanged(nameof(IsNotTesting));
            }
        }
    }

    public bool IsNotTesting => !IsTesting;

    public TestResult? TestResult
    {
        get => _testResult;
        set
        {
            if (SetProperty(ref _testResult, value))
            {
                OnPropertyChanged(nameof(HasTestResult));
            }
        }
    }

    public bool HasTestResult => TestResult != null;

    public string SelectedTypeName => GetCacheTypeName(SelectedType);
    public string SelectedTypeDescription => GetCacheTypeDescription(SelectedType);
    public string SelectedTypeIcon => GetCacheTypeIcon(SelectedType);
    public bool RequiresConnectionString => SelectedType != CacheType.InMemory && SelectedType != CacheType.None;
    public string TTLFormatted => FormatTTL(DefaultTTL);

    public ICommand SelectCacheTypeCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand SaveConfigCommand { get; }
    public ICommand CancelCommand { get; }

    public async Task InitializeAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(ApiId))
            {
                _currentApi = await _apiService.GetApiInterfaceAsync(ApiId);
                LoadCurrentConfiguration();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing cache configuration");
            await _notificationService.ShowAlertAsync("Erro", "Falha ao carregar configuração de cache");
        }
    }

    private void LoadCurrentConfiguration()
    {
        if (_currentApi?.Infrastructure?.Cache == null) return;

        var config = _currentApi.Infrastructure.Cache;
        
        SelectedType = config.Type;
        ConnectionString = config.ConnectionString;
        DefaultTTL = config.DefaultTTL;
        EnableDistributed = config.EnableDistributed;
    }

    private async Task SelectCacheTypeAsync(string typeString)
    {
        try
        {
            if (Enum.TryParse<CacheType>(typeString, out var type))
            {
                SelectedType = type;
                
                // Clear test result when changing type
                TestResult = null;
                
                // Set default connection string based on type
                ConnectionString = GetDefaultConnectionString(type);
                
                await _notificationService.ShowToastAsync($"Tipo de cache alterado para {GetCacheTypeName(type)}", ToastType.Info);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting cache type");
            await _notificationService.ShowToastAsync("Erro ao selecionar tipo de cache", ToastType.Error);
        }
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            if (RequiresConnectionString && string.IsNullOrWhiteSpace(ConnectionString))
            {
                await _notificationService.ShowToastAsync("Digite a string de conexão", ToastType.Warning);
                return;
            }

            IsTesting = true;
            TestResult = null;

            var config = CreateCacheConfig();
            var isSuccess = await _infrastructureService.TestCacheConnectionAsync(config);

            TestResult = new TestResult
            {
                IsSuccess = isSuccess,
                Message = isSuccess ? "Conexão bem-sucedida!" : "Falha na conexão",
                Details = isSuccess ? 
                    $"Conectado com sucesso ao {GetCacheTypeName(SelectedType)}" :
                    "Verifique a string de conexão e tente novamente",
                TestedAt = DateTime.Now
            };

            await _notificationService.ShowToastAsync(
                TestResult.Message,
                isSuccess ? ToastType.Success : ToastType.Error
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing cache connection");
            
            TestResult = new TestResult
            {
                IsSuccess = false,
                Message = "Erro no teste",
                Details = ex.Message,
                TestedAt = DateTime.Now
            };

            await _notificationService.ShowToastAsync("Erro ao testar conexão", ToastType.Error);
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task SaveConfigurationAsync()
    {
        try
        {
            if (_currentApi == null)
            {
                await _notificationService.ShowAlertAsync("Erro", "API não encontrada");
                return;
            }

            if (RequiresConnectionString && string.IsNullOrWhiteSpace(ConnectionString))
            {
                await _notificationService.ShowAlertAsync("Erro", "String de conexão é obrigatória para o tipo selecionado");
                return;
            }

            var config = CreateCacheConfig();
            var updateRequest = new UpdateApiInterfaceRequest
            {
                Infrastructure = _currentApi.Infrastructure
            };
            updateRequest.Infrastructure.Cache = config;

            await _apiService.UpdateApiInterfaceAsync(ApiId, updateRequest);

            await _notificationService.ShowToastAsync("Configuração de cache salva com sucesso", ToastType.Success);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving cache configuration");
            await _notificationService.ShowAlertAsync("Erro", $"Falha ao salvar configuração: {ex.Message}");
        }
    }

    private async Task CancelAsync()
    {
        var hasChanges = HasUnsavedChanges();
        
        if (hasChanges)
        {
            var confirmCancel = await _notificationService.ShowConfirmAsync(
                "Cancelar Alterações",
                "Você possui alterações não salvas. Deseja realmente cancelar?"
            );

            if (!confirmCancel) return;
        }

        await Shell.Current.GoToAsync("..");
    }

    private CacheConfig CreateCacheConfig()
    {
        return new CacheConfig
        {
            Type = SelectedType,
            ConnectionString = ConnectionString,
            DefaultTTL = DefaultTTL,
            EnableDistributed = EnableDistributed,
            Options = new Dictionary<string, object>()
        };
    }

    private bool HasUnsavedChanges()
    {
        if (_currentApi?.Infrastructure?.Cache == null) return true;
        
        var current = _currentApi.Infrastructure.Cache;
        return current.Type != SelectedType ||
               current.ConnectionString != ConnectionString ||
               current.DefaultTTL != DefaultTTL ||
               current.EnableDistributed != EnableDistributed;
    }

    private string GetCacheTypeName(CacheType type)
    {
        return type switch
        {
            CacheType.Redis => "Redis",
            CacheType.Memcached => "Memcached",
            CacheType.InMemory => "In-Memory",
            CacheType.None => "Sem Cache",
            _ => "Desconhecido"
        };
    }

    private string GetCacheTypeDescription(CacheType type)
    {
        return type switch
        {
            CacheType.Redis => "Sistema de cache avançado, distribuído e persistente",
            CacheType.Memcached => "Cache simples e rápido em memória",
            CacheType.InMemory => "Cache local na aplicação",
            CacheType.None => "Sem sistema de cache",
            _ => ""
        };
    }

    private string GetCacheTypeIcon(CacheType type)
    {
        return type switch
        {
            CacheType.Redis => "?",
            CacheType.Memcached => "??",
            CacheType.InMemory => "??",
            CacheType.None => "??",
            _ => "?"
        };
    }

    private string GetDefaultConnectionString(CacheType type)
    {
        return type switch
        {
            CacheType.Redis => "localhost:6379",
            CacheType.Memcached => "localhost:11211",
            _ => ""
        };
    }

    private string FormatTTL(int seconds)
    {
        return seconds switch
        {
            < 60 => $"{seconds}s",
            < 3600 => $"{seconds / 60}min",
            < 86400 => $"{seconds / 3600}h",
            _ => $"{seconds / 86400}d"
        };
    }
}