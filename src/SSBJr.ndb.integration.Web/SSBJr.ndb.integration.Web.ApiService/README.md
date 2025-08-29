# SSBJr.ndb.integration.Web.ApiService

> Microservi�o API - REST e GraphQL endpoints para gerenciamento de APIs

## ?? Vis�o Geral

Este microservi�o fornece endpoints REST e GraphQL para gerenciamento de interfaces de API, deploy de containers, monitoramento e integra��o com sistemas externos. Constru�do com **ASP.NET Core Web API** e otimizado para performance e escalabilidade.

## ??? Arquitetura

```
SSBJr.ndb.integration.Web.ApiService/
??? Controllers/           # Controllers REST API
??? GraphQL/              # Schema e resolvers GraphQL
??? Services/             # Servi�os de neg�cio
??? Models/               # DTOs e modelos
??? Middleware/           # Middleware customizado
??? Extensions/           # Extens�es de configura��o
??? Program.cs           # Configura��o do servi�o
```

## ?? Funcionalidades

### ?? REST API
- **CRUD completo** para interfaces de API
- **Deploy/Stop/Restart** de containers
- **Monitoramento** e m�tricas em tempo real
- **Logs** de containers
- **Health checks** avan�ados
- **Rate limiting** configur�vel
- **Versionamento** de API

### ?? GraphQL API
- **Schema introspection**
- **Queries otimizadas** com DataLoader
- **Subscriptions** para updates em tempo real
- **Mutations** para opera��es CRUD
- **Filtering e pagina��o** avan�ados

### ?? Seguran�a
- **JWT Authentication** com refresh tokens
- **Role-based authorization**
- **CORS** configur�vel
- **Rate limiting** por endpoint
- **API Keys** para integra��o externa

### ?? Docker Integration
- **Deploy autom�tico** de containers
- **Logs em tempo real** via Docker API
- **M�tricas** de containers
- **Health monitoring** de deployments
- **Cleanup** autom�tico de recursos

## ??? Tecnologias

### Core
- **ASP.NET Core 8** - Framework web
- **Entity Framework Core** - ORM
- **AutoMapper** - Mapeamento de objetos
- **FluentValidation** - Valida��o de dados
- **MediatR** - Pattern CQRS/Mediator

### GraphQL
- **HotChocolate** - GraphQL server
- **DataLoader** - Otimiza��o de queries
- **GraphQL Subscriptions** - Real-time updates

### Integra��o
- **Docker.DotNet** - API Docker
- **Newtonsoft.Json** - Serializa��o JSON
- **Serilog** - Logging estruturado
- **Polly** - Resilience patterns

### Documenta��o
- **Swagger/OpenAPI** - Documenta��o autom�tica
- **GraphQL Playground** - Interface GraphQL
- **ReDoc** - Documenta��o avan�ada

## ?? Endpoints REST

### API Management
```
GET    /api/v1/interfaces              # Listar APIs
POST   /api/v1/interfaces              # Criar nova API
GET    /api/v1/interfaces/{id}         # Obter API espec�fica
PUT    /api/v1/interfaces/{id}         # Atualizar API
DELETE /api/v1/interfaces/{id}         # Remover API
```

### Deployment
```
POST   /api/v1/interfaces/{id}/deploy      # Deploy da API
POST   /api/v1/interfaces/{id}/stop        # Parar API
POST   /api/v1/interfaces/{id}/restart     # Reiniciar API
GET    /api/v1/interfaces/{id}/status      # Status do deployment
```

### Monitoring
```
GET    /api/v1/interfaces/{id}/metrics     # M�tricas da API
GET    /api/v1/interfaces/{id}/logs        # Logs em tempo real
GET    /api/v1/interfaces/{id}/health      # Health check da API
```

### System
```
GET    /api/v1/health                      # Health do servi�o
GET    /api/v1/metrics                     # M�tricas do servi�o
GET    /api/v1/info                        # Informa��es do sistema
```

## ?? Schema GraphQL

### Types
```graphql
type ApiInterface {
  id: ID!
  name: String!
  description: String
  type: ApiType!
  version: String!
  status: ApiStatus!
  graphqlSchema: String
  swaggerJson: String
  infrastructure: InfrastructureConfig
  security: SecurityConfig
  monitoring: MonitoringConfig
  createdAt: DateTime!
  updatedAt: DateTime
  createdBy: String!
  tags: [String!]
  deploymentInfo: DeploymentInfo
}

enum ApiType {
  GRAPHQL
  REST
  HYBRID
}

enum ApiStatus {
  DRAFT
  VALIDATING
  DEPLOYING
  RUNNING
  STOPPED
  FAILED
}
```

### Queries
```graphql
type Query {
  # Buscar APIs
  apiInterfaces(
    first: Int
    after: String
    where: ApiInterfaceFilterInput
  ): ApiInterfaceConnection!
  
  apiInterface(id: ID!): ApiInterface
  
  # M�tricas e monitoramento
  apiMetrics(id: ID!): ApiMetrics!
  systemHealth: SystemHealth!
}
```

### Mutations
```graphql
type Mutation {
  # CRUD operations
  createApiInterface(input: CreateApiInterfaceInput!): ApiInterface!
  updateApiInterface(id: ID!, input: UpdateApiInterfaceInput!): ApiInterface!
  deleteApiInterface(id: ID!): Boolean!
  
  # Deployment operations
  deployApiInterface(id: ID!): DeploymentResult!
  stopApiInterface(id: ID!): Boolean!
  restartApiInterface(id: ID!): DeploymentResult!
}
```

### Subscriptions
```graphql
type Subscription {
  # Real-time updates
  apiStatusChanged(id: ID): ApiInterface!
  deploymentProgress(id: ID!): DeploymentProgress!
  systemNotifications: SystemNotification!
}
```

## ?? Como Executar

### Desenvolvimento
```bash
# Restaurar depend�ncias
dotnet restore

# Executar em desenvolvimento
dotnet run

# Com hot reload
dotnet watch run
```

### Produ��o
```bash
# Build
dotnet build -c Release

# Publish
dotnet publish -c Release -o ./publish

# Run
dotnet ./publish/SSBJr.ndb.integration.Web.ApiService.dll
```

### Docker
```bash
# Build da imagem
docker build -t ssbjr-api-service .

# Executar container
docker run -p 8080:80 ssbjr-api-service
```

## ?? Configura��o

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=apimanager;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key",
    "Issuer": "SSBJr.ApiService",
    "Audience": "SSBJr.ApiService.Clients",
    "ExpiryMinutes": 60,
    "RefreshExpiryDays": 7
  },
  "Docker": {
    "BaseUrl": "npipe://./pipe/docker_engine",
    "RegistryUrl": "localhost:5000",
    "NetworkName": "ssbjr-network"
  },
  "RateLimiting": {
    "PermitLimit": 100,
    "Window": "00:01:00",
    "ReplenishmentPeriod": "00:00:10",
    "TokensPerPeriod": 10
  }
}
```

### Vari�veis de Ambiente
```bash
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Host=prod-db;Database=apimanager;Username=api;Password=secure"
export JwtSettings__SecretKey="production-secret-key"
export Docker__BaseUrl="tcp://docker-host:2376"
```

## ?? Testes

### Executar Testes
```bash
# Todos os testes
dotnet test

# Testes espec�ficos
dotnet test --filter Category=Integration
dotnet test --filter Category=Unit
```

### Tipos de Teste
- **Unit Tests** - Testes de servi�os isolados
- **Integration Tests** - Testes com TestServer
- **API Tests** - Testes de endpoints REST
- **GraphQL Tests** - Testes de schema e resolvers

### Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"coverage.cobertura.xml" -targetdir:"coverage"
```

## ?? Monitoring e Observabilidade

### M�tricas
```csharp
// M�tricas customizadas
services.AddScoped<IMetricsCollector, PrometheusMetricsCollector>();

// Endpoints de m�tricas
app.MapGet("/metrics", async (IMetricsCollector metrics) => 
{
    return await metrics.GetMetricsAsync();
});
```

### Logging
```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { 
        "Name": "File", 
        "Args": { 
          "path": "logs/api-.txt", 
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### Health Checks
```csharp
services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddRedis(redisConnection)
    .AddDockerHealthCheck()
    .AddGraphQLHealthCheck();
```

## ?? Seguran�a

### Authentication Middleware
```csharp
[Authorize(Roles = "Administrator,ApiManager")]
[RequirePermission("api.create")]
public async Task<IActionResult> CreateApiInterface([FromBody] CreateApiInterfaceDto dto)
{
    // Implementation
}
```

### Rate Limiting
```csharp
services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

## ?? Deploy

### Via Docker Compose
```yaml
version: '3.8'
services:
  api-service:
    build: .
    ports:
      - "8080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=apimanager;Username=api;Password=secure
    depends_on:
      - postgres
      - redis
```

### Via Kubernetes
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ssbjr-api-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: ssbjr-api-service
  template:
    spec:
      containers:
      - name: api-service
        image: ssbjr-api-service:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
```

## ?? Performance

### Otimiza��es
- **Entity Framework** com compiled queries
- **Redis caching** para queries frequentes
- **Response compression** autom�tica
- **GraphQL DataLoader** para N+1 queries
- **Async/await** em todas as opera��es I/O
- **Memory pooling** para objetos grandes

### Benchmarks
```bash
# Load testing com k6
k6 run --vus 100 --duration 30s load-test.js

# Profiling com dotMemory
dotnet-counters monitor --name SSBJr.ndb.integration.Web.ApiService
```

## ?? Troubleshooting

### Logs de Debug
```bash
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Default=Debug
dotnet run
```

### Problemas Comuns

#### Docker n�o conecta
```bash
# Windows
set DOCKER_HOST=npipe://./pipe/docker_engine

# Linux/Mac
export DOCKER_HOST=unix:///var/run/docker.sock
```

#### GraphQL n�o funciona
- Verifique se o endpoint `/graphql` est� habilitado
- Confirme o schema no GraphQL Playground
- Verifique logs de erro no console

---

*Para informa��es gerais do projeto, veja o [README principal](../../../README.md).*