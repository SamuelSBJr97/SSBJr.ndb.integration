# SSBJr.ndb.integration.Web

> Aplicação Web Principal - Blazor Server com gerenciamento completo de APIs

## ?? Visão Geral

Este é o projeto principal da aplicação web, construído com **Blazor Server** e **ASP.NET Core**. Fornece a interface principal para gerenciamento de APIs, deploy automático, monitoramento e administração do sistema.

## ??? Arquitetura

```
SSBJr.ndb.integration.Web/
??? Components/              # Componentes Blazor
?   ??? Layout/             # Layouts da aplicação
?   ??? Pages/              # Páginas principais
?   ??? Shared/             # Componentes compartilhados
??? Data/                   # Contexto do Entity Framework
??? Services/               # Serviços de negócio
??? Middleware/             # Middleware customizado
??? Models/                 # Modelos de dados
??? wwwroot/               # Arquivos estáticos
??? Program.cs             # Configuração da aplicação
```

## ?? Funcionalidades Principais

### ?? Dashboard
- **Métricas em tempo real** de APIs ativas
- **Gráficos interactivos** com Chart.js
- **Status de infraestrutura** (Docker, PostgreSQL, Redis)
- **Atividades recentes** com auditoria

### ?? Gerenciamento de APIs
- **CRUD completo** de interfaces de API
- **Suporte multi-tipo**: GraphQL, REST, Híbrida
- **Configuração de infraestrutura**: DB, Cache, Messaging
- **Validação de schemas** GraphQL e OpenAPI
- **Deploy automático** via Docker

### ?? Sistema de Autenticação
- **Login JWT** com refresh tokens
- **Autorização baseada em roles/permissões**
- **Middleware de autorização** customizado
- **Sessões seguras** com Redis

### ?? Monitoramento e Logs
- **Página de auditoria** com filtros avançados
- **Logs em tempo real** dos containers
- **Métricas de performance**
- **Health checks** automatizados

### ?? Notificações em Tempo Real
- **SignalR Hub** para notificações
- **Toast notifications**
- **Sistema de alertas** para deploys e falhas

## ??? Tecnologias Utilizadas

### Frontend
- **Blazor Server** - Framework UI principal
- **Bootstrap 5** - Framework CSS
- **Bootstrap Icons** - Iconografia
- **Chart.js** - Gráficos e visualizações
- **SignalR Client** - Comunicação em tempo real

### Backend
- **ASP.NET Core 8** - Framework web
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco principal (com fallback InMemory)
- **Redis** - Cache e sessions
- **JWT Bearer** - Autenticação
- **BCrypt** - Hash de senhas
- **Serilog** - Logging estruturado

### Integração
- **Docker.DotNet** - Integração com Docker
- **Aspire** - Observabilidade e orquestração
- **SignalR** - Comunicação em tempo real

## ?? Como Executar

### Pré-requisitos
- .NET 8 SDK
- Docker Desktop
- PostgreSQL (opcional, há fallback)
- Redis (opcional, há fallback)

### Execução via Aspire (Recomendada)
```bash
cd ../SSBJr.ndb.integration.Web.AppHost
dotnet run
```

### Execução Standalone
```bash
dotnet run
```

### Configuração

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
- `MainLayout.razor` - Layout principal da aplicação
- `NavMenu.razor` - Menu de navegação lateral
- `TopBar.razor` - Barra superior com usuário/notificações

### Components/Pages/
- `Dashboard.razor` - Dashboard principal com métricas
- `ApiInterfaces.razor` - CRUD de interfaces de API
- `AuditLogs.razor` - Página de auditoria e logs
- `Login.razor` - Página de autenticação

### Components/Shared/
- `ApiInterfaceForm.razor` - Formulário complexo de criação/edição
- `NotificationSystem.razor` - Sistema de notificações
- `LoadingSpinner.razor` - Indicadores de carregamento

### Services/
- `ApiInterfaceService.cs` - Gerenciamento de APIs
- `DockerContainerService.cs` - Integração com Docker
- `InfrastructureService.cs` - Configuração de infraestrutura
- `AuditService.cs` - Sistema de auditoria
- `AuthService.cs` - Autenticação e autorização
- `CustomAuthenticationStateProvider.cs` - Provedor de estado de auth

### Data/
- `ApiManagerDbContext.cs` - Contexto do Entity Framework
- Configurações de entidades e relacionamentos

## ?? Segurança

### Autenticação
- JWT Tokens com refresh
- Validação server-side
- Proteção CSRF/XSS
- Sessions seguras

### Autorização
```csharp
// Permissões disponíveis
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
- `AuthorizationMiddleware` - Autorização fine-grained
- Rate limiting automático
- Logging de segurança

## ?? APIs REST Expostas

### Authentication
```
POST /api/auth/login       # Login do usuário
POST /api/auth/refresh     # Refresh token
POST /api/auth/logout      # Logout
GET  /api/auth/me         # Dados do usuário atual
```

### API Management
```
GET    /api/interfaces              # Listar APIs
POST   /api/interfaces              # Criar API
GET    /api/interfaces/{id}         # Obter API específica
PUT    /api/interfaces/{id}         # Atualizar API
DELETE /api/interfaces/{id}         # Deletar API
POST   /api/interfaces/{id}/deploy  # Deploy API
POST   /api/interfaces/{id}/stop    # Parar API
```

### Monitoring
```
GET /api/interfaces/{id}/metrics    # Métricas da API
GET /api/interfaces/{id}/logs       # Logs da API
GET /health                        # Health check
```

## ?? Testes

### Executar Testes
```bash
dotnet test
```

### Testes Inclusos
- Testes de integração com TestHost
- Testes de API endpoints
- Testes de serviços de negócio
- Testes de autenticação/autorização

## ?? Configuração Avançada

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
// Configuração automática de containers
services.AddScoped<IDockerContainerService, DockerContainerService>();
```

### SignalR Hub
```csharp
// Hub para notificações em tempo real
app.MapHub<NotificationHub>("/notifications");
```

## ?? Deploy em Produção

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

### Otimizações Implementadas
- **Blazor Server** com SignalR otimizado
- **Entity Framework** com lazy loading
- **Redis caching** para sessões e dados
- **Output caching** para APIs
- **Compression** automática
- **CDN** para assets estáticos

### Métricas
- Suporte a **Application Insights**
- Logs estruturados com **Serilog**
- Health checks com **AspNetCore.HealthChecks**

## ?? Troubleshooting

### Problemas Comuns

#### Erro de Conexão com PostgreSQL
```bash
# Use InMemory como fallback
export ConnectionStrings__postgres=""
dotnet run
```

#### Erro de Redis
```bash
# Cache em memória será usado automaticamente
export ConnectionStrings__cache=""
dotnet run
```

#### Problemas de Autenticação
```bash
# Verifique a configuração JWT
export JwtSettings__SecretKey="your-secret-key-here"
```

## ?? Suporte

Para suporte específico deste componente:
- Logs disponíveis em `logs/`
- Health check: `GET /health`
- Metrics endpoint: `GET /metrics` (se habilitado)

---

*Este README cobre especificamente o componente Blazor Web. Para informações gerais do projeto, veja o [README principal](../../../README.md).*