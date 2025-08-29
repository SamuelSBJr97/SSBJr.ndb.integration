# SSBJr.ndb.integration.Web

> Aplica��o Web Principal - Blazor Server com gerenciamento completo de APIs

## ?? Vis�o Geral

Este � o projeto principal da aplica��o web, constru�do com **Blazor Server** e **ASP.NET Core**. Fornece a interface principal para gerenciamento de APIs, deploy autom�tico, monitoramento e administra��o do sistema.

## ??? Arquitetura

```
SSBJr.ndb.integration.Web/
??? Components/              # Componentes Blazor
?   ??? Layout/             # Layouts da aplica��o
?   ??? Pages/              # P�ginas principais
?   ??? Shared/             # Componentes compartilhados
??? Data/                   # Contexto do Entity Framework
??? Services/               # Servi�os de neg�cio
??? Middleware/             # Middleware customizado
??? Models/                 # Modelos de dados
??? wwwroot/               # Arquivos est�ticos
??? Program.cs             # Configura��o da aplica��o
```

## ?? Funcionalidades Principais

### ?? Dashboard
- **M�tricas em tempo real** de APIs ativas
- **Gr�ficos interactivos** com Chart.js
- **Status de infraestrutura** (Docker, PostgreSQL, Redis)
- **Atividades recentes** com auditoria

### ?? Gerenciamento de APIs
- **CRUD completo** de interfaces de API
- **Suporte multi-tipo**: GraphQL, REST, H�brida
- **Configura��o de infraestrutura**: DB, Cache, Messaging
- **Valida��o de schemas** GraphQL e OpenAPI
- **Deploy autom�tico** via Docker

### ?? Sistema de Autentica��o
- **Login JWT** com refresh tokens
- **Autoriza��o baseada em roles/permiss�es**
- **Middleware de autoriza��o** customizado
- **Sess�es seguras** com Redis

### ?? Monitoramento e Logs
- **P�gina de auditoria** com filtros avan�ados
- **Logs em tempo real** dos containers
- **M�tricas de performance**
- **Health checks** automatizados

### ?? Notifica��es em Tempo Real
- **SignalR Hub** para notifica��es
- **Toast notifications**
- **Sistema de alertas** para deploys e falhas

## ??? Tecnologias Utilizadas

### Frontend
- **Blazor Server** - Framework UI principal
- **Bootstrap 5** - Framework CSS
- **Bootstrap Icons** - Iconografia
- **Chart.js** - Gr�ficos e visualiza��es
- **SignalR Client** - Comunica��o em tempo real

### Backend
- **ASP.NET Core 8** - Framework web
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco principal (com fallback InMemory)
- **Redis** - Cache e sessions
- **JWT Bearer** - Autentica��o
- **BCrypt** - Hash de senhas
- **Serilog** - Logging estruturado

### Integra��o
- **Docker.DotNet** - Integra��o com Docker
- **Aspire** - Observabilidade e orquestra��o
- **SignalR** - Comunica��o em tempo real

## ?? Como Executar

### Pr�-requisitos
- .NET 8 SDK
- Docker Desktop
- PostgreSQL (opcional, h� fallback)
- Redis (opcional, h� fallback)

### Execu��o via Aspire (Recomendada)
```bash
cd ../SSBJr.ndb.integration.Web.AppHost
dotnet run
```

### Execu��o Standalone
```bash
dotnet run
```

### Configura��o

#### appsettings.json
```json
{
  "ConnectionStrings": {
    "postgres": "Host=localhost;Database=apimanager;Username=postgres;Password=postgres",
    "cache": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-here",
    "Issuer": "SSBJr.ApiManager",
    "Audience": "SSBJr.ApiManager.Users"
  }
}
```

## ?? Estrutura Detalhada

### Components/Layout/
- `MainLayout.razor` - Layout principal da aplica��o
- `NavMenu.razor` - Menu de navega��o lateral
- `TopBar.razor` - Barra superior com usu�rio/notifica��es

### Components/Pages/
- `Dashboard.razor` - Dashboard principal com m�tricas
- `ApiInterfaces.razor` - CRUD de interfaces de API
- `AuditLogs.razor` - P�gina de auditoria e logs
- `Login.razor` - P�gina de autentica��o

### Components/Shared/
- `ApiInterfaceForm.razor` - Formul�rio complexo de cria��o/edi��o
- `NotificationSystem.razor` - Sistema de notifica��es
- `LoadingSpinner.razor` - Indicadores de carregamento

### Services/
- `ApiInterfaceService.cs` - Gerenciamento de APIs
- `DockerContainerService.cs` - Integra��o com Docker
- `InfrastructureService.cs` - Configura��o de infraestrutura
- `AuditService.cs` - Sistema de auditoria
- `AuthService.cs` - Autentica��o e autoriza��o
- `CustomAuthenticationStateProvider.cs` - Provedor de estado de auth

### Data/
- `ApiManagerDbContext.cs` - Contexto do Entity Framework
- Configura��es de entidades e relacionamentos

## ?? Seguran�a

### Autentica��o
- JWT Tokens com refresh
- Valida��o server-side
- Prote��o CSRF/XSS
- Sessions seguras

### Autoriza��o
```csharp
// Permiss�es dispon�veis
public static class Permissions
{
    public const string ApiRead = "api.read";
    public const string ApiCreate = "api.create";
    public const string ApiUpdate = "api.update";
    public const string ApiDelete = "api.delete";
    public const string ApiDeploy = "api.deploy";
    public const string AuditRead = "audit.read";
}
```

### Middleware
- `AuthorizationMiddleware` - Autoriza��o fine-grained
- Rate limiting autom�tico
- Logging de seguran�a

## ?? APIs REST Expostas

### Authentication
```
POST /api/auth/login       # Login do usu�rio
POST /api/auth/refresh     # Refresh token
POST /api/auth/logout      # Logout
GET  /api/auth/me         # Dados do usu�rio atual
```

### API Management
```
GET    /api/interfaces              # Listar APIs
POST   /api/interfaces              # Criar API
GET    /api/interfaces/{id}         # Obter API espec�fica
PUT    /api/interfaces/{id}         # Atualizar API
DELETE /api/interfaces/{id}         # Deletar API
POST   /api/interfaces/{id}/deploy  # Deploy API
POST   /api/interfaces/{id}/stop    # Parar API
```

### Monitoring
```
GET /api/interfaces/{id}/metrics    # M�tricas da API
GET /api/interfaces/{id}/logs       # Logs da API
GET /health                        # Health check
```

## ?? Testes

### Executar Testes
```bash
dotnet test
```

### Testes Inclusos
- Testes de integra��o com TestHost
- Testes de API endpoints
- Testes de servi�os de neg�cio
- Testes de autentica��o/autoriza��o

## ?? Configura��o Avan�ada

### Logging com Serilog
```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { 
        "Name": "File", 
        "Args": { "path": "logs/app-.txt", "rollingInterval": "Day" }
      }
    ]
  }
}
```

### Docker Integration
```csharp
// Configura��o autom�tica de containers
services.AddScoped<IDockerContainerService, DockerContainerService>();
```

### SignalR Hub
```csharp
// Hub para notifica��es em tempo real
app.MapHub<NotificationHub>("/notifications");
```

## ?? Deploy em Produ��o

### Via Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SSBJr.ndb.integration.Web.dll"]
```

### Via Aspire
```bash
# Deploy completo via Aspire
cd ../SSBJr.ndb.integration.Web.AppHost
dotnet run --environment Production
```

## ?? Performance

### Otimiza��es Implementadas
- **Blazor Server** com SignalR otimizado
- **Entity Framework** com lazy loading
- **Redis caching** para sess�es e dados
- **Output caching** para APIs
- **Compression** autom�tica
- **CDN** para assets est�ticos

### M�tricas
- Suporte a **Application Insights**
- Logs estruturados com **Serilog**
- Health checks com **AspNetCore.HealthChecks**

## ?? Troubleshooting

### Problemas Comuns

#### Erro de Conex�o com PostgreSQL
```bash
# Use InMemory como fallback
export ConnectionStrings__postgres=""
dotnet run
```

#### Erro de Redis
```bash
# Cache em mem�ria ser� usado automaticamente
export ConnectionStrings__cache=""
dotnet run
```

#### Problemas de Autentica��o
```bash
# Verifique a configura��o JWT
export JwtSettings__SecretKey="your-secret-key-here"
```

## ?? Suporte

Para suporte espec�fico deste componente:
- Logs dispon�veis em `logs/`
- Health check: `GET /health`
- Metrics endpoint: `GET /metrics` (se habilitado)

---

*Este README cobre especificamente o componente Blazor Web. Para informa��es gerais do projeto, veja o [README principal](../../../README.md).*