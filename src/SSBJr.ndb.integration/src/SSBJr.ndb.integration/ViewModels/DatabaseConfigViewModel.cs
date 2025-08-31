using SSBJr.ndb.integration.Models;
using SSBJr.ndb.integration.Services;
using System.Windows.Input;

namespace SSBJr.ndb.integration.ViewModels;

[QueryProperty(nameof(ApiId), "apiId")]
public class DatabaseConfigViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IInfrastructureService _infrastructureService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DatabaseConfigViewModel> _logger;

    private string _apiId = string.Empty;
    private DatabaseType _selectedType = DatabaseType.PostgreSQL;
    private string _connectionString = string.Empty;
    private bool _enableReadReplicas = false;
    private int _maxConnections = 100;
    private bool _enableMigrations = true;
    private bool _isTesting;
    private bool _isAdvancedVisible;
    private TestResult? _testResult;
    private ApiInterface? _currentApi;

    public DatabaseConfigViewModel(
        IApiService apiService,
        IInfrastructureService infrastructureService,
        INotificationService notificationService,
        ILogger<DatabaseConfigViewModel> logger)
    {
        _apiService = apiService;
        _infrastructureService = infrastructureService;
        _notificationService = notificationService;
        _logger = logger;

        SelectDatabaseTypeCommand = new Command<string>(async (type) => await SelectDatabaseTypeAsync(type));
        TestConnectionCommand = new Command(async () => await TestConnectionAsync());
        SaveConfigCommand = new Command(async () => await SaveConfigurationAsync());
        CancelCommand = new Command(async () => await CancelAsync());
    }

    public string ApiId
    {
        get => _apiId;
        set => SetProperty(ref _apiId, value);
    }

    public DatabaseType SelectedType
    {
        get => _selectedType;
        set
        {
            if (SetProperty(ref _selectedType, value))
            {
                OnPropertyChanged(nameof(SelectedTypeName));
                OnPropertyChanged(nameof(SelectedTypeDescription));
                OnPropertyChanged(nameof(SelectedTypeIcon));
            }
        }
    }

    public string ConnectionString
    {
        get => _connectionString;
        set => SetProperty(ref _connectionString, value);
    }

    public bool EnableReadReplicas
    {
        get => _enableReadReplicas;
        set => SetProperty(ref _enableReadReplicas, value);
    }

    public int MaxConnections
    {
        get => _maxConnections;
        set => SetProperty(ref _maxConnections, value);
    }

    public bool EnableMigrations
    {
        get => _enableMigrations;
        set => SetProperty(ref _enableMigrations, value);
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

    public bool IsAdvancedVisible
    {
        get => _isAdvancedVisible;
        set => SetProperty(ref _isAdvancedVisible, value);
    }

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

    public string SelectedTypeName => GetDatabaseTypeName(SelectedType);
    public string SelectedTypeDescription => GetDatabaseTypeDescription(SelectedType);
    public string SelectedTypeIcon => GetDatabaseTypeIcon(SelectedType);

    public ICommand SelectDatabaseTypeCommand { get; }
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
            _logger.LogError(ex, "Error initializing database configuration");
            await _notificationService.ShowAlertAsync("Erro", "Falha ao carregar configuração de banco de dados");
        }
    }

    private void LoadCurrentConfiguration()
    {
        if (_currentApi?.Infrastructure?.Database == null) return;

        var config = _currentApi.Infrastructure.Database;
        
        SelectedType = config.Type;
        ConnectionString = config.ConnectionString;
        EnableReadReplicas = config.EnableReadReplicas;
        MaxConnections = config.MaxConnections;
        EnableMigrations = config.EnableMigrations;
    }

    private async Task SelectDatabaseTypeAsync(string typeString)
    {
        try
        {
            if (Enum.TryParse<DatabaseType>(typeString, out var type))
            {
                SelectedType = type;
                
                // Clear test result when changing type
                TestResult = null;
                
                // Set default connection string based on type
                ConnectionString = GetDefaultConnectionString(type);
                
                await _notificationService.ShowToastAsync($"Tipo de banco alterado para {GetDatabaseTypeName(type)}", ToastType.Info);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting database type");
            await _notificationService.ShowToastAsync("Erro ao selecionar tipo de banco", ToastType.Error);
        }
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                await _notificationService.ShowToastAsync("Digite a string de conexão", ToastType.Warning);
                return;
            }

            IsTesting = true;
            TestResult = null;

            var config = CreateDatabaseConfig();
            var isSuccess = await _infrastructureService.TestDatabaseConnectionAsync(config);

            TestResult = new TestResult
            {
                IsSuccess = isSuccess,
                Message = isSuccess ? "Conexão bem-sucedida!" : "Falha na conexão",
                Details = isSuccess ? 
                    $"Conectado com sucesso ao {GetDatabaseTypeName(SelectedType)}" :
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
            _logger.LogError(ex, "Error testing database connection");
            
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

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                await _notificationService.ShowAlertAsync("Erro", "String de conexão é obrigatória");
                return;
            }

            var config = CreateDatabaseConfig();
            var updateRequest = new UpdateApiInterfaceRequest
            {
                Infrastructure = _currentApi.Infrastructure
            };
            updateRequest.Infrastructure.Database = config;

            await _apiService.UpdateApiInterfaceAsync(ApiId, updateRequest);

            await _notificationService.ShowToastAsync("Configuração de banco de dados salva com sucesso", ToastType.Success);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving database configuration");
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

    private DatabaseConfig CreateDatabaseConfig()
    {
        return new DatabaseConfig
        {
            Type = SelectedType,
            ConnectionString = ConnectionString,
            EnableReadReplicas = EnableReadReplicas,
            MaxConnections = MaxConnections,
            EnableMigrations = EnableMigrations,
            Options = new Dictionary<string, object>()
        };
    }

    private bool HasUnsavedChanges()
    {
        if (_currentApi?.Infrastructure?.Database == null) return true;
        
        var current = _currentApi.Infrastructure.Database;
        return current.Type != SelectedType ||
               current.ConnectionString != ConnectionString ||
               current.EnableReadReplicas != EnableReadReplicas ||
               current.MaxConnections != MaxConnections ||
               current.EnableMigrations != EnableMigrations;
    }

    private string GetDatabaseTypeName(DatabaseType type)
    {
        return type switch
        {
            DatabaseType.PostgreSQL => "PostgreSQL",
            DatabaseType.MongoDB => "MongoDB",
            DatabaseType.MySQL => "MySQL",
            DatabaseType.SQLServer => "SQL Server",
            DatabaseType.Redis => "Redis",
            _ => "Desconhecido"
        };
    }

    private string GetDatabaseTypeDescription(DatabaseType type)
    {
        return type switch
        {
            DatabaseType.PostgreSQL => "Banco relacional avançado, open source",
            DatabaseType.MongoDB => "Banco NoSQL orientado a documentos",
            DatabaseType.MySQL => "Banco relacional popular e confiável",
            DatabaseType.SQLServer => "Banco enterprise da Microsoft",
            DatabaseType.Redis => "Banco em memória, cache e estruturas de dados",
            _ => ""
        };
    }

    private string GetDatabaseTypeIcon(DatabaseType type)
    {
        return type switch
        {
            DatabaseType.PostgreSQL => "??",
            DatabaseType.MongoDB => "??",
            DatabaseType.MySQL => "??",
            DatabaseType.SQLServer => "??",
            DatabaseType.Redis => "?",
            _ => "??"
        };
    }

    private string GetDefaultConnectionString(DatabaseType type)
    {
        return type switch
        {
            DatabaseType.PostgreSQL => "Host=localhost;Database=mydb;Username=myuser;Password=mypass;",
            DatabaseType.MongoDB => "mongodb://localhost:27017/mydb",
            DatabaseType.MySQL => "Server=localhost;Database=mydb;Uid=myuser;Pwd=mypass;",
            DatabaseType.SQLServer => "Server=localhost;Database=mydb;Trusted_Connection=true;",
            DatabaseType.Redis => "localhost:6379",
            _ => ""
        };
    }
}