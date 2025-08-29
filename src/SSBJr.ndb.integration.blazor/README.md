# SSBJr.ndb.integration.Blazor

> Blazor WebAssembly - Aplicação SPA standalone para gerenciamento de APIs

## ?? Visão Geral

Esta é uma aplicação **Blazor WebAssembly** standalone que funciona como uma Single Page Application (SPA) para gerenciamento de interfaces de API. Oferece uma alternativa leve e rápida ao Blazor Server, executando completamente no navegador com comunicação via APIs REST.

## ??? Arquitetura

```
SSBJr.ndb.integration.Blazor/
??? Components/              # Componentes Blazor
?   ??? Layout/             # Layouts da aplicação
?   ??? Pages/              # Páginas da aplicação
?   ??? Shared/             # Componentes reutilizáveis
??? Services/               # Serviços cliente (HTTP)
??? Models/                 # DTOs e modelos
??? wwwroot/               # Assets estáticos
??? Program.cs             # Entry point da aplicação
??? App.razor              # Componente raiz
```

## ?? Funcionalidades

### ?? SPA Experience
- **Client-side routing** com Blazor Router
- **Offline capability** com service workers
- **Progressive Web App** (PWA) features
- **Fast startup** após download inicial
- **No server round-trips** para navegação

### ?? API Management
- **CRUD operations** via HTTP API calls
- **Real-time updates** com SignalR client
- **Async operations** com loading states
- **Error handling** robusto
- **Caching** inteligente de dados

### ?? Interface
- **Responsive design** com Bootstrap
- **Modern UI** components
- **Interactive charts** com Chart.js
- **Toast notifications**
- **Loading spinners** e skeletons

### ?? Authentication
- **JWT token** management
- **Automatic token refresh**
- **Protected routes**
- **Role-based UI** rendering

## ??? Tecnologias

### Core
- **Blazor WebAssembly** - Framework SPA
- **.NET 8** - Runtime WebAssembly
- **HttpClient** - API communication
- **System.Text.Json** - JSON serialization

### UI/UX
- **Bootstrap 5** - CSS framework
- **Bootstrap Icons** - Iconografia
- **Chart.js** - Visualizações
- **CSS Grid/Flexbox** - Layouts

### Comunicação
- **HTTP Client** - REST API calls
- **SignalR Client** - Real-time updates
- **Polly** - Retry policies
- **Refit** - Typed HTTP client

### PWA
- **Service Worker** - Offline support
- **Web App Manifest** - Installable app
- **IndexedDB** - Client storage
- **Cache API** - Resource caching

## ?? Como Executar

### Desenvolvimento
```bash
# Restaurar dependências
dotnet restore

# Executar em desenvolvimento
dotnet run

# Com hot reload
dotnet watch run

# Acesso: https://localhost:5173
```

### Build para Produção
```bash
# Build otimizado
dotnet publish -c Release -o publish

# Servir arquivos estáticos
cd publish/wwwroot
python -m http.server 8000
```

### Via Aspire
```bash
# O Blazor WASM é automaticamente incluído no AppHost
cd ../SSBJr.ndb.integration.Web.AppHost
dotnet run
```

## ?? Configuração

### Program.cs
```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Adicionar componente raiz
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configurar HttpClient base
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// Configurar API HttpClient
builder.Services.AddHttpClient("ApiService", client =>
{
    client.BaseAddress = new Uri("https://localhost:8080/");
    client.DefaultRequestHeaders.Add("User-Agent", "SSBJr.Blazor.Client/1.0");
});

// Serviços da aplicação
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// SignalR para real-time
builder.Services.AddScoped<HubConnectionBuilder>();

await builder.Build().RunAsync();
```

### appsettings.json
```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:8080",
    "Timeout": 30,
    "RetryCount": 3
  },
  "SignalR": {
    "HubUrl": "https://localhost:8080/notifications",
    "ReconnectInterval": 5000,
    "MaxReconnectAttempts": 10
  },
  "Features": {
    "EnableOfflineMode": true,
    "EnablePushNotifications": true,
    "EnablePWA": true
  }
}
```

## ?? Serviços HTTP

### ApiService
```csharp
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;

    public ApiService(IHttpClientFactory httpClientFactory, ILogger<ApiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ApiService");
        _logger = logger;
    }

    public async Task<List<ApiInterface>> GetApiInterfacesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/interfaces");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ApiInterface>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? new List<ApiInterface>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching API interfaces");
            throw;
        }
    }

    public async Task<ApiInterface> CreateApiInterfaceAsync(CreateApiInterfaceRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/api/v1/interfaces", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiInterface>(responseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })!;
    }
}
```

### AuthService
```csharp
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;

    public async Task<bool> LoginAsync(string username, string password)
    {
        var request = new { Username = username, Password = password };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/auth/login", content);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result?.Success == true)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "accessToken", result.AccessToken);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", result.RefreshToken);
                return true;
            }
        }
        
        return false;
    }
}
```

## ?? Progressive Web App

### Manifest.json
```json
{
  "name": "SSBJr API Manager",
  "short_name": "SSBJr",
  "description": "Sistema de gerenciamento de APIs",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#667eea",
  "icons": [
    {
      "src": "icon-192.png",
      "sizes": "192x192",
      "type": "image/png"
    },
    {
      "src": "icon-512.png",
      "sizes": "512x512",
      "type": "image/png"
    }
  ]
}
```

### Service Worker
```javascript
const CACHE_NAME = 'ssbjr-v1';
const urlsToCache = [
  '/',
  '/css/app.css',
  '/js/app.js',
  '/_framework/blazor.webassembly.js'
];

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(urlsToCache))
  );
});

self.addEventListener('fetch', event => {
  event.respondWith(
    caches.match(event.request)
      .then(response => {
        return response || fetch(event.request);
      }
    )
  );
});
```

## ?? SignalR Integration

### Connection Setup
```csharp
public class NotificationService : INotificationService, IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly ILogger<NotificationService> _logger;

    public async Task StartAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("https://localhost:8080/notifications")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string, string>("ReceiveNotification", (title, message) =>
        {
            OnNotificationReceived?.Invoke(new Notification { Title = title, Message = message });
        });

        await _hubConnection.StartAsync();
        _logger.LogInformation("SignalR connection started");
    }

    public event Action<Notification>? OnNotificationReceived;

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
```

## ?? Componentes UI

### ApiInterfaceCard.razor
```razor
@using SSBJr.ndb.integration.Blazor.Models

<div class="card api-interface-card">
    <div class="card-header d-flex justify-content-between align-items-center">
        <h6 class="mb-0">@ApiInterface.Name</h6>
        <span class="badge @GetStatusBadgeClass()">@ApiInterface.Status</span>
    </div>
    <div class="card-body">
        <p class="card-text text-muted">@ApiInterface.Description</p>
        <div class="d-flex justify-content-between align-items-center">
            <small class="text-muted">
                <i class="bi bi-tag"></i> @ApiInterface.Type
            </small>
            <div class="btn-group" role="group">
                <button class="btn btn-sm btn-outline-primary" @onclick="OnEdit">
                    <i class="bi bi-pencil"></i>
                </button>
                <button class="btn btn-sm btn-outline-success" @onclick="OnDeploy">
                    <i class="bi bi-play"></i>
                </button>
                <button class="btn btn-sm btn-outline-danger" @onclick="OnDelete">
                    <i class="bi bi-trash"></i>
                </button>
            </div>
        </div>
    </div>
</div>

@code {
    [Parameter] public ApiInterface ApiInterface { get; set; } = null!;
    [Parameter] public EventCallback<ApiInterface> OnEdit { get; set; }
    [Parameter] public EventCallback<ApiInterface> OnDeploy { get; set; }
    [Parameter] public EventCallback<ApiInterface> OnDelete { get; set; }

    private string GetStatusBadgeClass() => ApiInterface.Status switch
    {
        ApiStatus.Running => "bg-success",
        ApiStatus.Stopped => "bg-secondary",
        ApiStatus.Failed => "bg-danger",
        ApiStatus.Deploying => "bg-warning",
        _ => "bg-info"
    };
}
```

## ?? Charts e Visualizações

### Chart.js Integration
```javascript
window.chartFunctions = {
    createPieChart: (canvasId, data, labels) => {
        const ctx = document.getElementById(canvasId).getContext('2d');
        return new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: [
                        '#FF6384',
                        '#36A2EB',
                        '#FFCE56',
                        '#4BC0C0'
                    ]
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }
};
```

## ?? Testes

### Testes Unitários
```csharp
[Test]
public async Task ApiService_Should_Fetch_ApiInterfaces()
{
    // Arrange
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(new List<ApiInterface>()))
        });

    var httpClient = new HttpClient(mockHandler.Object)
    {
        BaseAddress = new Uri("https://localhost:8080/")
    };

    var service = new ApiService(httpClient);

    // Act
    var result = await service.GetApiInterfacesAsync();

    // Assert
    result.Should().NotBeNull();
    result.Should().BeOfType<List<ApiInterface>>();
}
```

### Testes E2E com Playwright
```csharp
[Test]
public async Task Dashboard_Should_Display_ApiInterfaces()
{
    // Arrange
    await Page.GotoAsync("https://localhost:5173");
    
    // Act
    await Page.WaitForSelectorAsync(".api-interface-card");
    
    // Assert
    var cards = await Page.QuerySelectorAllAsync(".api-interface-card");
    cards.Should().NotBeEmpty();
}
```

## ?? Deploy

### Azure Static Web Apps
```bash
# Build para produção
dotnet publish -c Release

# Deploy via Azure CLI
az staticwebapp create \
  --name ssbjr-blazor-app \
  --source ./publish/wwwroot \
  --location "East US 2"
```

### GitHub Pages
```yaml
name: Deploy to GitHub Pages

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Publish
      run: dotnet publish -c Release -o publish
      
    - name: Deploy to GitHub Pages
      uses: peaceiris/actions-gh-pages@v3
      with:
        github_token: ${{ secrets.GITHUB_TOKEN }}
        publish_dir: publish/wwwroot
```

### CDN Deployment
```bash
# Build e upload para CDN
dotnet publish -c Release
aws s3 sync ./publish/wwwroot s3://ssbjr-blazor-app --delete
aws cloudfront create-invalidation --distribution-id ABCDEFGHIJK --paths "/*"
```

## ?? Performance

### Lazy Loading
```csharp
// Lazy loading de assemblies
@page "/admin"
@attribute [Authorize(Roles = "Administrator")]
@using Microsoft.AspNetCore.Components.WebAssembly.Authentication

<AdminPanel />

@code {
    // Assembly será carregado apenas quando necessário
}
```

### AOT Compilation
```xml
<PropertyGroup>
  <RunAOTCompilation>true</RunAOTCompilation>
  <PublishTrimmed>true</PublishTrimmed>
</PropertyGroup>
```

## ?? Troubleshooting

### Problemas de CORS
```csharp
// Configure CORS no backend API
services.AddCors(options =>
{
    options.AddPolicy("BlazorWasmPolicy", builder =>
    {
        builder.WithOrigins("https://localhost:5173")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
```

### WebAssembly não carrega
- Verificar se o servidor suporta arquivos `.wasm`
- Confirmar Content-Type: `application/wasm`
- Verificar console do navegador para erros

---

*Para informações gerais do projeto, veja o [README principal](../../README.md).*