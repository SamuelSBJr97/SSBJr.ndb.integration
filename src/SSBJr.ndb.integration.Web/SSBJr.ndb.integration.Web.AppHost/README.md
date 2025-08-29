# SSBJr.ndb.integration.Web.AppHost

> Aspire AppHost - Orquestra��o e observabilidade para toda a aplica��o

## ?? Vis�o Geral

O **AppHost** � o ponto de entrada principal da aplica��o, utilizando **.NET Aspire** para orquestra��o de todos os servi�os, incluindo Blazor Web, API Service, bancos de dados, cache e monitoramento. Fornece um dashboard centralizado para desenvolvimento e observabilidade.

## ??? Arquitetura Aspire

```
???????????????????????????????????????????????????????????????
?                    Aspire Dashboard                         ?
?              https://localhost:15888                        ?
???????????????????????????????????????????????????????????????
                              ?
    ?????????????????????????????????????????????????????
    ?                         ?                         ?
????????????         ??????????????????        ?????????????????
?PostgreSQL?         ?     Redis      ?        ?   Services    ?
?Container ?         ?   Container    ?        ?   Projects    ?
????????????         ??????????????????        ?????????????????
                                                        ?
                              ?????????????????????????????????????????????????????
                              ?                         ?                         ?
                        ??????????????         ??????????????????        ?????????????????
                        ?Blazor Web  ?         ?  API Service   ?        ?Blazor Wasm    ?
                        ?  (Main)    ?         ? (Microservice) ?        ? (Standalone)  ?
                        ??????????????         ??????????????????        ?????????????????
```

## ?? Funcionalidades

### ?? Orquestra��o de Servi�os
- **Blazor Web** - Aplica��o principal
- **API Service** - Microservi�o de APIs
- **Blazor WebAssembly** - Aplica��o standalone
- **PostgreSQL** - Banco de dados principal
- **Redis** - Cache e sess�es

### ?? Dashboard de Observabilidade
- **M�tricas em tempo real** de todos os servi�os
- **Logs centralizados** com filtragem
- **Distributed tracing** com OpenTelemetry
- **Health checks** automatizados
- **Resource monitoring** (CPU, Memory, Network)

### ?? Service Discovery
- **Automatic service registration**
- **Load balancing** entre inst�ncias
- **Health monitoring** cont�nuo
- **Configuration management** centralizado

### ?? Telemetria
- **OpenTelemetry** integration completa
- **Custom metrics** por servi�o
- **Distributed logging** com correlation IDs
- **Performance monitoring** autom�tico

## ??? Configura��o

### AppHost.cs
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infraestrutura
var cache = builder.AddRedis("cache");
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("apimanager");

// API Service
var apiService = builder.AddProject<Projects.SSBJr_ndb_integration_Web_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(cache)
    .WithReference(postgres)
    .WaitFor(cache)
    .WaitFor(postgres);

// Blazor Web App (Principal)
var webFrontend = builder.AddProject<Projects.SSBJr_ndb_integration_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WithReference(postgres)
    .WithReference(apiService)
    .WaitFor(cache)
    .WaitFor(postgres)
    .WaitFor(apiService);

// Blazor WebAssembly (Standalone)
var blazorApp = builder.AddProject<Projects.SSBJr_ndb_integration_Blazor>("blazorapp")
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
```

## ?? Como Executar

### Desenvolvimento
```bash
# Navegar para o diret�rio
cd SSBJr.ndb.integration.Web.AppHost

# Executar o AppHost
dotnet run

# Acessar o dashboard
# https://localhost:15888
```

### Via Script (Recomendado)
```powershell
# Na raiz do projeto
.\Start-Development.ps1
```

### Produ��o
```bash
# Build para produ��o
dotnet build -c Release

# Executar em produ��o
dotnet run --environment Production
```

## ?? Dashboard Aspire

### Acesso
- **URL**: `https://localhost:15888`
- **Login**: Autom�tico em desenvolvimento
- **Token**: Fornecido no console durante startup

### Funcionalidades do Dashboard

#### ?? Overview
- Status de todos os servi�os
- Health checks em tempo real
- Resource utilization
- Service dependencies

#### ?? Metrics
- **CPU Usage** por servi�o
- **Memory Consumption**
- **Network I/O**
- **Request rates** e latency
- **Custom metrics** da aplica��o

#### ?? Logs
- **Centralized logging** de todos os servi�os
- **Filtros avan�ados** por servi�o, n�vel, tempo
- **Real-time log streaming**
- **Log correlation** com trace IDs

#### ?? Traces
- **Distributed tracing** com OpenTelemetry
- **Request flow** atrav�s dos servi�os
- **Performance bottlenecks** identification
- **Dependency mapping**

#### ?? Health
- **Health check endpoints** monitoring
- **Service availability** tracking
- **Automatic alerts** para falhas
- **Recovery procedures** documentation

## ?? Configura��o Avan�ada

### Vari�veis de Ambiente
```bash
# Ambiente de execu��o
export ASPNETCORE_ENVIRONMENT=Development

# Configura��o de observabilidade
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
export OTEL_SERVICE_NAME=SSBJr.AppHost

# Configura��o de recursos
export DOTNET_RESOURCE_MONITORING_SAMPLING_RATE=1.0
```

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Aspire": "Debug",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  "Dashboard": {
    "ApplicationUrl": "https://localhost:15888",
    "OtlpUrl": "http://localhost:16686"
  },
  "ResourceMonitoring": {
    "Enabled": true,
    "RefreshInterval": "00:00:05",
    "RetentionPeriod": "01:00:00"
  }
}
```

## ?? Services Configurados

### PostgreSQL
- **Container**: `postgres:15`
- **Database**: `apimanager`
- **Port**: `5432` (internal)
- **Volume**: Persistent data storage
- **Health Check**: `pg_isready`

### Redis
- **Container**: `redis:7-alpine`
- **Port**: `6379` (internal)
- **Usage**: Cache, sessions, SignalR backplane
- **Health Check**: `redis-cli ping`

### Blazor Web (webfrontend)
- **Framework**: Blazor Server
- **Port**: `7080` (HTTPS)
- **Health Check**: `GET /health`
- **Dependencies**: PostgreSQL, Redis, API Service

### API Service (apiservice)
- **Framework**: ASP.NET Core Web API
- **Port**: `8080` (HTTP)
- **Health Check**: `GET /health`
- **Dependencies**: PostgreSQL, Redis

### Blazor WebAssembly (blazorapp)
- **Framework**: Blazor WASM
- **Port**: `5173` (HTTP)
- **Health Check**: `GET /health`
- **Dependencies**: API Service

## ?? Monitoring e Observabilidade

### OpenTelemetry Configuration
```csharp
// Automatic instrumentation
builder.Services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder.AddAspNetCoreInstrumentation()
               .AddEntityFrameworkCoreInstrumentation()
               .AddRedisInstrumentation()
               .AddHttpClientInstrumentation();
    })
    .WithMetrics(builder =>
    {
        builder.AddAspNetCoreInstrumentation()
               .AddRuntimeInstrumentation()
               .AddProcessInstrumentation();
    });
```

### Custom Metrics
```csharp
// M�tricas customizadas da aplica��o
services.AddSingleton<IMetrics>(provider =>
{
    var meterProvider = provider.GetRequiredService<IMeterProvider>();
    return new ApplicationMetrics(meterProvider);
});
```

### Distributed Tracing
- **Trace correlation** entre todos os servi�os
- **Automatic span creation** para HTTP requests
- **Database query tracing** com EF Core
- **Cache operation tracing** com Redis

## ?? Deploy em Produ��o

### Docker Compose (Desenvolvimento)
```yaml
version: '3.8'
services:
  apphost:
    build: .
    ports:
      - "15888:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ./logs:/app/logs
```

### Kubernetes (Produ��o)
```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: ssbjr-apphost
spec:
  source:
    repoURL: https://github.com/SamuelSBJr97/SSBJr.ndb.integration
    path: deploy/k8s
    targetRevision: main
  destination:
    server: https://kubernetes.default.svc
    namespace: ssbjr-production
```

### Azure Container Apps
```bash
# Deploy via Azure CLI
az containerapp env create --name ssbjr-env --resource-group ssbjr-rg
az containerapp create --name ssbjr-apphost --resource-group ssbjr-rg --environment ssbjr-env --image ssbjr/apphost:latest
```

## ?? Testes

### Health Checks Testing
```csharp
[Test]
public async Task AppHost_Services_Should_Be_Healthy()
{
    // Arrange
    using var appHost = await DistributedApplicationTestingBuilder
        .CreateAsync<Projects.SSBJr_ndb_integration_Web_AppHost>();
    
    await using var app = await appHost.BuildAsync();
    await app.StartAsync();
    
    // Act & Assert
    var httpClient = app.CreateHttpClient("webfrontend");
    var response = await httpClient.GetAsync("/health");
    
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### Integration Testing
```bash
# Executar testes de integra��o
dotnet test --filter Category=Integration

# Testes com AppHost real
dotnet test --filter Category=E2E
```

## ?? Performance Tuning

### Resource Limits
```csharp
// Configura��o de recursos por servi�o
var webFrontend = builder.AddProject<Projects.SSBJr_ndb_integration_Web>("webfrontend")
    .WithResourceLimits(cpuLimit: 1.0, memoryLimit: 512_000_000); // 512MB
```

### Scaling Configuration
```csharp
// Auto-scaling baseado em m�tricas
var apiService = builder.AddProject<Projects.SSBJr_ndb_integration_Web_ApiService>("apiservice")
    .WithReplicas(minReplicas: 2, maxReplicas: 10)
    .WithScalingPolicy(targetCpuUtilization: 70);
```

## ?? Seguran�a

### HTTPS Configuration
```csharp
// Configura��o HTTPS para produ��o
builder.Services.Configure<HttpsRedirectionOptions>(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 443;
});
```

### Secrets Management
```bash
# Development secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."

# Production - Azure Key Vault
export AZURE_KEY_VAULT_ENDPOINT=https://ssbjr-keyvault.vault.azure.net/
```

## ?? Troubleshooting

### Logs de Debug
```json
{
  "Logging": {
    "LogLevel": {
      "Aspire": "Debug",
      "Microsoft.Extensions.Hosting": "Debug",
      "DcpHosting": "Debug"
    }
  }
}
```

### Problemas Comuns

#### Services n�o iniciam
```bash
# Verificar logs detalhados
export ASPIRE_DASHBOARD_DEBUGGING=true
dotnet run
```

#### Dashboard n�o carrega
- Verificar se a porta 15888 est� dispon�vel
- Confirmar firewall e antivirus
- Acessar via `http://localhost:15888` se HTTPS falhar

#### PostgreSQL n�o conecta
- Verificar se Docker Desktop est� executando
- Confirmar porta 5432 dispon�vel
- Verificar logs do container PostgreSQL

---

*Para informa��es gerais do projeto, veja o [README principal](../../../README.md).*