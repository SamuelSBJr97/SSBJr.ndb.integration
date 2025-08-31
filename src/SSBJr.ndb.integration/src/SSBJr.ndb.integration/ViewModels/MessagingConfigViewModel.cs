using System.Collections.ObjectModel;
using SSBJr.ndb.integration.Models;
using SSBJr.ndb.integration.Services;
using System.Windows.Input;

namespace SSBJr.ndb.integration.ViewModels;

[QueryProperty(nameof(ApiId), "apiId")]
public class MessagingConfigViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IInfrastructureService _infrastructureService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MessagingConfigViewModel> _logger;

    private string _apiId = string.Empty;
    private MessagingType _selectedType = MessagingType.None;
    private string _connectionString = string.Empty;
    private bool _enableBatching = true;
    private int _batchSize = 100;
    private bool _enablePartitioning = true;
    private int _partitionCount = 3;
    private string _consumerGroup = "default";
    private bool _enableDeadLetterQueue = true;
    private int _maxRetries = 3;
    private bool _isTesting;
    private TestResult? _testResult;
    private ApiInterface? _currentApi;
    private ObservableCollection<string> _topics = new();

    public MessagingConfigViewModel(
        IApiService apiService,
        IInfrastructureService infrastructureService,
        INotificationService notificationService,
        ILogger<MessagingConfigViewModel> logger)
    {
        _apiService = apiService;
        _infrastructureService = infrastructureService;
        _notificationService = notificationService;
        _logger = logger;

        SelectMessagingTypeCommand = new Command<string>(async (type) => await SelectMessagingTypeAsync(type));
        AddTopicCommand = new Command<string>(async (topic) => await AddTopicAsync(topic));
        RemoveTopicCommand = new Command<string>(async (topic) => await RemoveTopicAsync(topic));
        TestConnectionCommand = new Command(async () => await TestConnectionAsync());
        SaveConfigCommand = new Command(async () => await SaveConfigurationAsync());
        CancelCommand = new Command(async () => await CancelAsync());

        LoadMessagingTypes();
    }

    public string ApiId
    {
        get => _apiId;
        set => SetProperty(ref _apiId, value);
    }

    public MessagingType SelectedType
    {
        get => _selectedType;
        set
        {
            if (SetProperty(ref _selectedType, value))
            {
                OnPropertyChanged(nameof(SelectedTypeName));
                OnPropertyChanged(nameof(SelectedTypeDescription));
                OnPropertyChanged(nameof(SelectedTypeIcon));
                OnPropertyChanged(nameof(IsKafka));
                OnPropertyChanged(nameof(ShowPartitionConfig));
            }
        }
    }

    public string ConnectionString
    {
        get => _connectionString;
        set => SetProperty(ref _connectionString, value);
    }

    public bool EnableBatching
    {
        get => _enableBatching;
        set => SetProperty(ref _enableBatching, value);
    }

    public int BatchSize
    {
        get => _batchSize;
        set => SetProperty(ref _batchSize, value);
    }

    public bool EnablePartitioning
    {
        get => _enablePartitioning;
        set
        {
            if (SetProperty(ref _enablePartitioning, value))
            {
                OnPropertyChanged(nameof(ShowPartitionConfig));
            }
        }
    }

    public int PartitionCount
    {
        get => _partitionCount;
        set => SetProperty(ref _partitionCount, value);
    }

    public string ConsumerGroup
    {
        get => _consumerGroup;
        set => SetProperty(ref _consumerGroup, value);
    }

    public bool EnableDeadLetterQueue
    {
        get => _enableDeadLetterQueue;
        set => SetProperty(ref _enableDeadLetterQueue, value);
    }

    public int MaxRetries
    {
        get => _maxRetries;
        set => SetProperty(ref _maxRetries, value);
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

    public ObservableCollection<string> Topics
    {
        get => _topics;
        set => SetProperty(ref _topics, value);
    }

    public string SelectedTypeName => GetMessagingTypeName(SelectedType);
    public string SelectedTypeDescription => GetMessagingTypeDescription(SelectedType);
    public string SelectedTypeIcon => GetMessagingTypeIcon(SelectedType);
    public bool IsKafka => SelectedType == MessagingType.Apache_Kafka;
    public bool ShowPartitionConfig => IsKafka && EnablePartitioning;

    public ICommand SelectMessagingTypeCommand { get; }
    public ICommand AddTopicCommand { get; }
    public ICommand RemoveTopicCommand { get; }
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
            _logger.LogError(ex, "Error initializing messaging configuration");
            await _notificationService.ShowAlertAsync("Erro", "Falha ao carregar configuração de mensageria");
        }
    }

    private void LoadCurrentConfiguration()
    {
        if (_currentApi?.Infrastructure?.Messaging == null) return;

        var config = _currentApi.Infrastructure.Messaging;
        
        SelectedType = config.Type;
        ConnectionString = config.ConnectionString;
        EnableBatching = config.EnableBatching;
        BatchSize = config.BatchSize;
        EnablePartitioning = config.EnablePartitioning;
        PartitionCount = config.PartitionCount;
        ConsumerGroup = config.ConsumerGroup;
        EnableDeadLetterQueue = config.EnableDeadLetterQueue;
        MaxRetries = config.MaxRetries;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Topics.Clear();
            foreach (var topic in config.Topics)
            {
                Topics.Add(topic);
            }
        });
    }

    private void LoadMessagingTypes()
    {
        // This would typically load from configuration or service
        // For now, we'll use the hardcoded types
    }

    private async Task SelectMessagingTypeAsync(string typeString)
    {
        try
        {
            if (Enum.TryParse<MessagingType>(typeString, out var type))
            {
                SelectedType = type;
                
                // Clear test result when changing type
                TestResult = null;
                
                // Set default connection string based on type
                ConnectionString = GetDefaultConnectionString(type);
                
                await _notificationService.ShowToastAsync($"Tipo de mensageria alterado para {GetMessagingTypeName(type)}", ToastType.Info);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting messaging type");
            await _notificationService.ShowToastAsync("Erro ao selecionar tipo de mensageria", ToastType.Error);
        }
    }

    private async Task AddTopicAsync(string topic)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                await _notificationService.ShowToastAsync("Digite o nome do tópico/fila", ToastType.Warning);
                return;
            }

            if (Topics.Contains(topic))
            {
                await _notificationService.ShowToastAsync("Tópico/fila já existe", ToastType.Warning);
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Topics.Add(topic.Trim());
            });

            await _notificationService.ShowToastAsync($"Tópico '{topic}' adicionado", ToastType.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding topic");
            await _notificationService.ShowToastAsync("Erro ao adicionar tópico", ToastType.Error);
        }
    }

    private async Task RemoveTopicAsync(string topic)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Topics.Remove(topic);
            });

            await _notificationService.ShowToastAsync($"Tópico '{topic}' removido", ToastType.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing topic");
            await _notificationService.ShowToastAsync("Erro ao remover tópico", ToastType.Error);
        }
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            if (SelectedType == MessagingType.None)
            {
                await _notificationService.ShowToastAsync("Selecione um tipo de mensageria", ToastType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                await _notificationService.ShowToastAsync("Digite a string de conexão", ToastType.Warning);
                return;
            }

            IsTesting = true;
            TestResult = null;

            var config = CreateMessagingConfig();
            var isSuccess = await _infrastructureService.TestMessagingConnectionAsync(config);

            TestResult = new TestResult
            {
                IsSuccess = isSuccess,
                Message = isSuccess ? "Conexão bem-sucedida!" : "Falha na conexão",
                Details = isSuccess ? 
                    $"Conectado com sucesso ao {GetMessagingTypeName(SelectedType)}" :
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
            _logger.LogError(ex, "Error testing messaging connection");
            
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

            if (SelectedType != MessagingType.None && string.IsNullOrWhiteSpace(ConnectionString))
            {
                await _notificationService.ShowAlertAsync("Erro", "String de conexão é obrigatória para o tipo selecionado");
                return;
            }

            var config = CreateMessagingConfig();
            var updateRequest = new UpdateApiInterfaceRequest
            {
                Infrastructure = _currentApi.Infrastructure
            };
            updateRequest.Infrastructure.Messaging = config;

            await _apiService.UpdateApiInterfaceAsync(ApiId, updateRequest);

            await _notificationService.ShowToastAsync("Configuração de mensageria salva com sucesso", ToastType.Success);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving messaging configuration");
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

    private MessagingConfig CreateMessagingConfig()
    {
        return new MessagingConfig
        {
            Type = SelectedType,
            ConnectionString = ConnectionString,
            Topics = Topics.ToList(),
            EnableBatching = EnableBatching,
            BatchSize = BatchSize,
            EnablePartitioning = EnablePartitioning,
            PartitionCount = PartitionCount,
            ConsumerGroup = ConsumerGroup,
            EnableDeadLetterQueue = EnableDeadLetterQueue,
            MaxRetries = MaxRetries,
            MessageTimeout = TimeSpan.FromMinutes(5)
        };
    }

    private bool HasUnsavedChanges()
    {
        if (_currentApi?.Infrastructure?.Messaging == null) return true;
        
        var current = _currentApi.Infrastructure.Messaging;
        return current.Type != SelectedType ||
               current.ConnectionString != ConnectionString ||
               current.EnableBatching != EnableBatching ||
               current.BatchSize != BatchSize ||
               current.EnablePartitioning != EnablePartitioning ||
               current.PartitionCount != PartitionCount ||
               current.ConsumerGroup != ConsumerGroup ||
               current.EnableDeadLetterQueue != EnableDeadLetterQueue ||
               current.MaxRetries != MaxRetries ||
               !current.Topics.SequenceEqual(Topics);
    }

    private string GetMessagingTypeName(MessagingType type)
    {
        return type switch
        {
            MessagingType.Apache_Kafka => "Apache Kafka",
            MessagingType.Azure_ServiceBus => "Azure Service Bus",
            MessagingType.AWS_SQS => "AWS SQS",
            MessagingType.AWS_EventBridge => "AWS EventBridge",
            MessagingType.Google_PubSub => "Google Pub/Sub",
            MessagingType.NATS => "NATS",
            MessagingType.Apache_Pulsar => "Apache Pulsar",
            MessagingType.RabbitMQ => "RabbitMQ (Deprecated)",
            MessagingType.None => "Nenhuma Mensageria",
            _ => "Desconhecido"
        };
    }

    private string GetMessagingTypeDescription(MessagingType type)
    {
        return type switch
        {
            MessagingType.Apache_Kafka => "Alto throughput, stream processing, event sourcing",
            MessagingType.Azure_ServiceBus => "Enterprise cloud messaging, integração Microsoft",
            MessagingType.AWS_SQS => "Filas simples e confiáveis na AWS",
            MessagingType.AWS_EventBridge => "Event-driven architecture na AWS",
            MessagingType.Google_PubSub => "Mensageria serverless no Google Cloud",
            MessagingType.NATS => "Simplicidade, performance, baixa latência",
            MessagingType.Apache_Pulsar => "Multi-tenant, geo-replicação",
            MessagingType.RabbitMQ => "?? Legado - considere migrar para alternativas modernas",
            MessagingType.None => "Sem sistema de mensageria",
            _ => ""
        };
    }

    private string GetMessagingTypeIcon(MessagingType type)
    {
        return type switch
        {
            MessagingType.Apache_Kafka => "??",
            MessagingType.Azure_ServiceBus => "??",
            MessagingType.AWS_SQS => "??",
            MessagingType.AWS_EventBridge => "??",
            MessagingType.Google_PubSub => "??",
            MessagingType.NATS => "?",
            MessagingType.Apache_Pulsar => "??",
            MessagingType.RabbitMQ => "??",
            MessagingType.None => "??",
            _ => "?"
        };
    }

    private string GetDefaultConnectionString(MessagingType type)
    {
        return type switch
        {
            MessagingType.Apache_Kafka => "localhost:9092",
            MessagingType.Azure_ServiceBus => "Endpoint=sb://yournamespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=yourkey",
            MessagingType.AWS_SQS => "https://sqs.us-east-1.amazonaws.com",
            MessagingType.AWS_EventBridge => "us-east-1",
            MessagingType.Google_PubSub => "your-project-id",
            MessagingType.NATS => "nats://localhost:4222",
            MessagingType.Apache_Pulsar => "pulsar://localhost:6650",
            MessagingType.RabbitMQ => "amqp://guest:guest@localhost:5672/",
            _ => ""
        };
    }
}

public class TestResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime TestedAt { get; set; }
}