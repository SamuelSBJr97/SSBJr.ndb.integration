# SSBJr.ndb.integration.MAUI

> .NET MAUI App - Aplicação cross-platform para gerenciamento de APIs

## ?? Visão Geral

Aplicação **.NET MAUI** (Multi-platform App UI) que oferece uma interface nativa para iOS, Android, Windows e macOS. Permite gerenciamento completo de interfaces de API com experiência mobile-first e integração com o sistema backend.

## ??? Arquitetura

```
SSBJr.ndb.integration.MAUI/
??? Platforms/              # Código específico de plataforma
?   ??? Android/           # Configurações Android
?   ??? iOS/               # Configurações iOS
?   ??? MacCatalyst/       # Configurações macOS
?   ??? Windows/           # Configurações Windows
?   ??? Tizen/             # Configurações Tizen (opcional)
??? Views/                  # Páginas XAML
??? ViewModels/            # ViewModels MVVM
??? Services/              # Serviços de negócio
??? Models/                # Modelos de dados
??? Resources/             # Recursos (imagens, estilos)
??? MauiProgram.cs         # Configuração da aplicação
??? App.xaml               # Aplicação base
```

## ?? Funcionalidades

### ?? Cross-Platform
- **Android** (API 21+)
- **iOS** (iOS 11+)
- **Windows** (Windows 11, Windows 10 versão 1809+)
- **macOS** (macOS 10.15+)
- **Tizen** (suporte experimental)

### ?? Gerenciamento de APIs
- **CRUD completo** de interfaces de API
- **Deploy móvel** via API calls
- **Monitoramento** em tempo real
- **Sincronização offline**
- **Push notifications**

### ?? UI Nativa
- **Shell Navigation** com flyout
- **Native controls** por plataforma
- **Material Design** no Android
- **iOS Human Interface** no iOS
- **WinUI 3** no Windows
- **AppKit** no macOS

### ?? Conectividade
- **HTTP Client** para APIs REST
- **SignalR** para real-time
- **Offline-first** com SQLite local
- **Background sync**
- **Network detection**

## ??? Tecnologias

### Core
- **.NET 8** - Framework base
- **.NET MAUI** - UI framework cross-platform
- **C# 12** - Linguagem principal
- **XAML** - Markup de UI

### Arquitetura
- **MVVM** - Model-View-ViewModel pattern
- **Community Toolkit** - Helpers e converters
- **Dependency Injection** - Microsoft.Extensions.DI
- **MediatR** - Pattern mediator

### Dados e Conectividade
- **HttpClient** - Chamadas HTTP
- **System.Text.Json** - Serialização JSON
- **SignalR Client** - Real-time updates
- **SQLite** - Banco local
- **Entity Framework Core** - ORM local

### UI e UX
- **Shell** - Navigation framework
- **CollectionView** - Lists performáticas
- **Animations** - Animações nativas
- **Handlers** - Customização de controles
- **Platform-specific UI** - Código nativo quando necessário

## ?? Como Executar

### Pré-requisitos
- Visual Studio 2022 ou VS Code
- .NET 8 SDK
- Android SDK (para Android)
- Xcode (para iOS/macOS)
- Windows SDK (para Windows)

### Desenvolvimento
```bash
# Restaurar dependências
dotnet restore

# Executar no Android (emulador)
dotnet run -f net8.0-android

# Executar no iOS (simulador) - apenas macOS
dotnet run -f net8.0-ios

# Executar no Windows
dotnet run -f net8.0-windows10.0.19041.0

# Executar no macOS
dotnet run -f net8.0-maccatalyst
```

### Visual Studio
1. Abrir solução no Visual Studio 2022
2. Selecionar projeto MAUI como startup
3. Escolher target (Android/iOS/Windows)
4. F5 para executar

## ?? Configuração

### MauiProgram.cs
```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<INotificationService, NotificationService>();
        builder.Services.AddSingleton<ILocalDataService, LocalDataService>();

        // HTTP Client
        builder.Services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri("https://localhost:8080/");
            client.DefaultRequestHeaders.Add("User-Agent", "SSBJr.MAUI/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // ViewModels
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<ApiListViewModel>();
        builder.Services.AddTransient<ApiDetailViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ApiListPage>();
        builder.Services.AddTransient<ApiDetailPage>();
        builder.Services.AddTransient<SettingsPage>();

        // Local Database
        builder.Services.AddSingleton<LocalDbContext>();

        return builder.Build();
    }
}
```

### Configurações por Plataforma

#### Android (Platforms/Android/AndroidManifest.xml)
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
<uses-permission android:name="android.permission.WAKE_LOCK" />
<uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED" />

<application android:allowBackup="true" 
             android:icon="@mipmap/appicon" 
             android:theme="@style/Maui.SplashTheme"
             android:networkSecurityConfig="@xml/network_security_config">
    
    <!-- Firebase Messaging (se usar push notifications) -->
    <service android:name=".Services.MyFirebaseMessagingService"
             android:exported="false">
        <intent-filter>
            <action android:name="com.google.firebase.MESSAGING_EVENT" />
        </intent-filter>
    </service>
</application>
```

#### iOS (Platforms/iOS/Info.plist)
```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSExceptionDomains</key>
    <dict>
        <key>localhost</key>
        <dict>
            <key>NSExceptionAllowsInsecureHTTPLoads</key>
            <true/>
        </dict>
    </dict>
</dict>

<!-- Background Modes -->
<key>UIBackgroundModes</key>
<array>
    <string>background-fetch</string>
    <string>background-processing</string>
</array>
```

## ??? Estrutura MVVM

### MainPageViewModel
```csharp
public partial class MainPageViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string title = "SSBJr API Manager";

    [ObservableProperty]
    private ObservableCollection<ApiInterface> apiInterfaces = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public MainPageViewModel(IApiService apiService, INotificationService notificationService)
    {
        _apiService = apiService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    async Task LoadApisAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var apis = await _apiService.GetApiInterfacesAsync();
            ApiInterfaces.Clear();
            
            foreach (var api in apis)
            {
                ApiInterfaces.Add(api);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await _notificationService.ShowErrorAsync("Erro", "Falha ao carregar APIs");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    async Task RefreshAsync()
    {
        await LoadApisAsync();
    }

    [RelayCommand]
    async Task NavigateToDetailAsync(ApiInterface apiInterface)
    {
        var parameters = new Dictionary<string, object>
        {
            ["ApiInterface"] = apiInterface
        };

        await Shell.Current.GoToAsync("detail", parameters);
    }
}
```

### MainPage.xaml
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage x:Class="SSBJr.ndb.integration.MAUI.Views.MainPage"
             xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:SSBJr.ndb.integration.MAUI.ViewModels"
             xmlns:models="clr-namespace:SSBJr.ndb.integration.MAUI.Models"
             x:DataType="vm:MainPageViewModel"
             Title="{Binding Title}">

    <Shell.TitleView>
        <Grid ColumnDefinitions="*,Auto">
            <Label Text="{Binding Title}" 
                   FontSize="18" 
                   FontAttributes="Bold"
                   VerticalOptions="Center" />
            
            <Button Grid.Column="1"
                    Text="?"
                    FontSize="16"
                    Command="{Binding RefreshCommand}"
                    IsVisible="{Binding IsLoading, Converter={StaticResource InvertedBoolConverter}}" />
        </Grid>
    </Shell.TitleView>

    <Grid RowDefinitions="Auto,*">
        
        <!-- Loading Indicator -->
        <ActivityIndicator Grid.Row="0"
                          IsVisible="{Binding IsLoading}"
                          IsRunning="{Binding IsLoading}"
                          HeightRequest="50"
                          Color="{StaticResource Primary}" />

        <!-- Error Message -->
        <Label Grid.Row="0"
               Text="{Binding ErrorMessage}"
               IsVisible="{Binding ErrorMessage, Converter={StaticResource StringToBoolConverter}}"
               BackgroundColor="Red"
               TextColor="White"
               Padding="10"
               FontAttributes="Bold" />

        <!-- APIs List -->
        <RefreshView Grid.Row="1"
                     Command="{Binding RefreshCommand}"
                     IsRefreshing="{Binding IsLoading}">
            
            <CollectionView ItemsSource="{Binding ApiInterfaces}"
                           SelectionMode="Single"
                           SelectionChangedCommand="{Binding NavigateToDetailCommand}"
                           SelectionChangedCommandParameter="{Binding SelectedItem, Source={RelativeSource Self}}">
                
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="models:ApiInterface">
                        <Grid Padding="15,10" 
                              RowDefinitions="Auto,Auto,Auto"
                              ColumnDefinitions="*,Auto">
                            
                            <!-- API Name -->
                            <Label Grid.Row="0" Grid.Column="0"
                                   Text="{Binding Name}"
                                   FontSize="16"
                                   FontAttributes="Bold"
                                   TextColor="{StaticResource Primary}" />

                            <!-- Status Badge -->
                            <Frame Grid.Row="0" Grid.Column="1"
                                   BackgroundColor="{Binding Status, Converter={StaticResource StatusToColorConverter}}"
                                   Padding="8,4"
                                   CornerRadius="12"
                                   HasShadow="False">
                                <Label Text="{Binding Status}"
                                       TextColor="White"
                                       FontSize="12"
                                       FontAttributes="Bold" />
                            </Frame>

                            <!-- Description -->
                            <Label Grid.Row="1" Grid.ColumnSpan="2"
                                   Text="{Binding Description}"
                                   FontSize="14"
                                   TextColor="{StaticResource Gray600}"
                                   LineBreakMode="TailTruncation"
                                   MaxLines="2" />

                            <!-- Type and Version -->
                            <StackLayout Grid.Row="2" Grid.ColumnSpan="2"
                                        Orientation="Horizontal"
                                        Spacing="10"
                                        Margin="0,5,0,0">
                                <Label Text="{Binding Type}"
                                       FontSize="12"
                                       BackgroundColor="{StaticResource Gray200}"
                                       Padding="6,2"
                                       TextColor="{StaticResource Gray900}" />
                                <Label Text="{Binding Version, StringFormat='v{0}'}"
                                       FontSize="12"
                                       TextColor="{StaticResource Gray600}" />
                            </StackLayout>

                            <!-- Separator -->
                            <BoxView Grid.Row="3" Grid.ColumnSpan="2"
                                     HeightRequest="1"
                                     BackgroundColor="{StaticResource Gray200}"
                                     Margin="0,10,0,0" />
                        </Grid>
                    </DataTemplate>
                </CollectionView.ItemTemplate>

                <CollectionView.EmptyView>
                    <ContentView>
                        <StackLayout VerticalOptions="CenterAndExpand"
                                    HorizontalOptions="CenterAndExpand"
                                    Spacing="20">
                            <Label Text="??"
                                   FontSize="48"
                                   HorizontalOptions="Center" />
                            <Label Text="Nenhuma API encontrada"
                                   FontSize="18"
                                   FontAttributes="Bold"
                                   HorizontalOptions="Center" />
                            <Label Text="Conecte-se ao servidor para ver suas APIs"
                                   FontSize="14"
                                   TextColor="{StaticResource Gray600}"
                                   HorizontalOptions="Center"
                                   HorizontalTextAlignment="Center" />
                            <Button Text="Tentar Novamente"
                                    Command="{Binding RefreshCommand}"
                                    HorizontalOptions="Center" />
                        </StackLayout>
                    </ContentView>
                </CollectionView.EmptyView>
            </CollectionView>
        </RefreshView>
    </Grid>

</ContentPage>
```

## ?? Serviços

### ApiService
```csharp
public interface IApiService
{
    Task<List<ApiInterface>> GetApiInterfacesAsync();
    Task<ApiInterface> GetApiInterfaceAsync(string id);
    Task<ApiInterface> CreateApiInterfaceAsync(CreateApiInterfaceRequest request);
    Task<ApiInterface> UpdateApiInterfaceAsync(string id, UpdateApiInterfaceRequest request);
    Task DeleteApiInterfaceAsync(string id);
    Task<bool> DeployApiInterfaceAsync(string id);
    Task<bool> StopApiInterfaceAsync(string id);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private readonly IAuthService _authService;

    public ApiService(IHttpClientFactory httpClientFactory, ILogger<ApiService> logger, IAuthService authService)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
        _logger = logger;
        _authService = authService;
    }

    public async Task<List<ApiInterface>> GetApiInterfacesAsync()
    {
        try
        {
            await EnsureAuthenticatedAsync();
            
            var response = await _httpClient.GetAsync("/api/v1/interfaces");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var apis = JsonSerializer.Deserialize<List<ApiInterface>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return apis ?? new List<ApiInterface>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching API interfaces");
            throw;
        }
    }

    public async Task<ApiInterface> CreateApiInterfaceAsync(CreateApiInterfaceRequest request)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/v1/interfaces", content);
            
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiInterface>(responseJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API interface");
            throw;
        }
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (!await _authService.IsAuthenticatedAsync())
        {
            await _authService.LoginAsync();
        }

        var token = await _authService.GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
```

## ?? Features por Plataforma

### Android
- **Material Design** components
- **Bottom navigation** nativo
- **Android notifications** com channels
- **Background services** para sync
- **Biometric authentication**

### iOS
- **iOS navigation** patterns
- **Native iOS alerts** e action sheets
- **iOS-style lists** e cells
- **Push notifications** via APNs
- **Face ID/Touch ID** support

### Windows
- **WinUI 3** controls
- **Windows 11** styling
- **System tray** integration
- **Windows notifications**
- **File association**

### macOS
- **macOS menu bar**
- **Native macOS controls**
- **Spotlight integration**
- **macOS notifications**

## ?? Offline Support

### Local Database
```csharp
public class LocalDbContext : DbContext
{
    public DbSet<ApiInterface> ApiInterfaces { get; set; }
    public DbSet<SyncQueue> SyncQueue { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "local.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiInterface>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<SyncQueue>()
            .HasKey(x => x.Id);
    }
}
```

### Sync Service
```csharp
public class SyncService : ISyncService
{
    private readonly LocalDbContext _localDb;
    private readonly IApiService _apiService;
    private readonly IConnectivity _connectivity;

    public async Task SyncWhenOnlineAsync()
    {
        if (_connectivity.NetworkAccess != NetworkAccess.Internet)
            return;

        try
        {
            // Sync local changes to server
            await SyncLocalChangesToServerAsync();
            
            // Pull updates from server
            await SyncServerChangesToLocalAsync();
        }
        catch (Exception ex)
        {
            // Log sync errors
        }
    }

    private async Task SyncLocalChangesToServerAsync()
    {
        var pendingChanges = await _localDb.SyncQueue.ToListAsync();
        
        foreach (var change in pendingChanges)
        {
            try
            {
                switch (change.Operation)
                {
                    case SyncOperation.Create:
                        await _apiService.CreateApiInterfaceAsync(change.Data);
                        break;
                    case SyncOperation.Update:
                        await _apiService.UpdateApiInterfaceAsync(change.EntityId, change.Data);
                        break;
                    case SyncOperation.Delete:
                        await _apiService.DeleteApiInterfaceAsync(change.EntityId);
                        break;
                }

                _localDb.SyncQueue.Remove(change);
            }
            catch
            {
                // Keep in queue for retry
            }
        }

        await _localDb.SaveChangesAsync();
    }
}
```

## ?? Push Notifications

### Firebase (Android)
```csharp
[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class FirebaseMessagingService : Firebase.Messaging.FirebaseMessagingService
{
    public override void OnMessageReceived(RemoteMessage message)
    {
        var notification = new LocalNotification
        {
            Title = message.GetNotification().Title,
            Body = message.GetNotification().Body,
            Data = message.Data
        };

        DependencyService.Get<INotificationService>()
            .ShowNotification(notification);
    }

    public override void OnNewToken(string token)
    {
        // Send token to server
        DependencyService.Get<IApiService>()
            .RegisterPushTokenAsync(token);
    }
}
```

## ?? Testes

### Unit Tests
```csharp
[Test]
public async Task ApiService_Should_Fetch_ApiInterfaces()
{
    // Arrange
    var mockHttp = new Mock<HttpMessageHandler>();
    mockHttp.Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(new List<ApiInterface>()))
        });

    var httpClient = new HttpClient(mockHttp.Object);
    var apiService = new ApiService(() => httpClient, Mock.Of<ILogger<ApiService>>(), Mock.Of<IAuthService>());

    // Act
    var result = await apiService.GetApiInterfacesAsync();

    // Assert
    result.Should().NotBeNull();
    result.Should().BeOfType<List<ApiInterface>>();
}
```

### UI Tests
```csharp
[Test]
public void MainPage_Should_Display_Title()
{
    // Arrange
    var app = AppiumDriver.LaunchApp();

    // Act
    var titleElement = app.FindElement(By.Id("MainPageTitle"));

    // Assert
    Assert.That(titleElement.Text, Is.EqualTo("SSBJr API Manager"));
}
```

## ?? Deploy

### Android (Google Play)
```bash
# Build release
dotnet build -c Release -f net8.0-android

# Create APK
dotnet publish -c Release -f net8.0-android -p:AndroidPackageFormats=apk

# Create AAB (recommended for Play Store)
dotnet publish -c Release -f net8.0-android -p:AndroidPackageFormats=aab
```

### iOS (App Store)
```bash
# Build for device
dotnet build -c Release -f net8.0-ios -p:RuntimeIdentifier=ios-arm64

# Archive for distribution
dotnet publish -c Release -f net8.0-ios -p:ArchiveOnBuild=true
```

### Windows (Microsoft Store)
```bash
# Create MSIX package
dotnet publish -c Release -f net8.0-windows10.0.19041.0 -p:GenerateAppxPackageOnBuild=true
```

---

*Para informações gerais do projeto, veja o [README principal](../../README.md).*