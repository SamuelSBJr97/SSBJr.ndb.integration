# SSBJr.ndb.integration

> Sistema de Gerenciamento e Provisionamento de APIs com .NET 8, Blazor, React e MAUI

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Blazor](https://img.shields.io/badge/Blazor-Server-purple.svg)](https://blazor.net/)
[![React](https://img.shields.io/badge/React-19.1.1-61dafb.svg)](https://reactjs.org/)
[![MAUI](https://img.shields.io/badge/.NET%20MAUI-8.0-green.svg)](https://dotnet.microsoft.com/apps/maui)
[![Aspire](https://img.shields.io/badge/Aspire-9.3.1-orange.svg)](https://learn.microsoft.com/en-us/dotnet/aspire/)

## ?? Visão Geral

O **SSBJr.ndb.integration** é uma plataforma completa e moderna para gerenciamento e provisionamento automático de APIs, oferecendo múltiplas interfaces de usuário e uma arquitetura robusta baseada em microserviços.

### ?? Características Principais

- **?? Multi-plataforma**: Web (Blazor), Mobile (MAUI), Desktop (MAUI), Web SPA (React)
- **?? APIs Automáticas**: Geração e deploy automático de GraphQL, REST e APIs híbridas
- **?? Containerização**: Deploy automático via Docker com orquestração
- **?? Monitoramento**: Dashboard em tempo real com métricas e logs
- **?? Segurança**: Autenticação JWT, autorização baseada em roles/permissões
- **?? Performance**: Cache Redis, banco PostgreSQL, otimizações avançadas
- **?? Observabilidade**: Integração com Aspire, logs estruturados, health checks

## ??? Arquitetura

```
???????????????????????????????????????????????????????????????????
?                        Aspire AppHost                           ?
?                    (Orquestração e Dashboard)                   ?
???????????????????????????????????????????????????????????????????
                                   ?
        ???????????????????????????????????????????????????????
        ?                          ?                          ?
??????????????????    ?????????????????    ????????????????????
?   Blazor Web   ?    ? Blazor Server ?    ?   React SPA     ?
?   (Principal)  ?    ? (Standalone)  ?    ?  (Frontend)     ?
??????????????????    ?????????????????    ????????????????????
                                   ?
                        ???????????????????????
                        ?    API Service      ?
                        ?   (.NET 8 Web API)  ?
                        ???????????????????????
                                   ?
        ???????????????????????????????????????????????????????
        ?                          ?                          ?
??????????????????    ?????????????????    ????????????????????
?   PostgreSQL   ?    ?     Redis     ?    ?   Docker API    ?
?  (Database)    ?    ?   (Cache)     ?    ?  (Containers)   ?
??????????????????    ?????????????????    ????????????????????
```

## ?? Quick Start

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (versão 8.0 ou superior)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Node.js 18+](https://nodejs.org/) (para React)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) ou [VS Code](https://code.visualstudio.com/)

### Instalação e Execução

#### Opção 1: Execução Automática (Recomendada)
```powershell
# Clone o repositório
git clone https://github.com/SamuelSBJr97/SSBJr.ndb.integration.git
cd SSBJr.ndb.integration

# Execute o script de inicialização
.\Start-Development.ps1
```

#### Opção 2: Execução Manual
```powershell
# 1. Compile o projeto
dotnet build

# 2. Execute o Aspire AppHost
cd src\SSBJr.ndb.integration.Web\SSBJr.ndb.integration.Web.AppHost
dotnet run

# 3. (Opcional) Execute o React separadamente
.\Run-React.ps1
```

### ?? URLs de Acesso

Após inicialização, acesse:

| Serviço | URL | Descrição |
|---------|-----|-----------|
| **Dashboard Aspire** | https://localhost:15888 | Orquestração e monitoramento |
| **Blazor Web App** | https://localhost:7080 | Interface principal de gerenciamento |
| **API Service** | https://localhost:8080 | REST/GraphQL APIs |
| **React App** | http://localhost:3000 | Interface alternativa SPA |
| **Swagger UI** | https://localhost:8080/swagger | Documentação da API |

### ?? Credenciais Demo

```
Usuário: admin
Senha: admin123
```

## ?? Estrutura do Projeto

```
SSBJr.ndb.integration/
??? src/
?   ??? SSBJr.ndb.integration/              # Projeto MAUI (Mobile/Desktop)
?   ??? SSBJr.ndb.integration.Blazor/       # Blazor WebAssembly Standalone
?   ??? SSBJr.ndb.integration.React/        # React SPA Frontend
?   ??? SSBJr.ndb.integration.Web/          # Blazor Server + APIs
?       ??? SSBJr.ndb.integration.Web/      # Aplicação Web Principal
?       ??? SSBJr.ndb.integration.Web.ApiService/  # Microserviço API
?       ??? SSBJr.ndb.integration.Web.AppHost/     # Aspire Host
?       ??? SSBJr.ndb.integration.Web.ServiceDefaults/ # Configurações Aspire
?       ??? SSBJr.ndb.integration.Web.Tests/       # Testes
??? docs/                                   # Documentação
??? scripts/                               # Scripts de automação
??? Start-Development.ps1                  # Script principal
??? Run-React.ps1                         # Script React standalone
??? README.md                             # Este arquivo
```

## ??? Tecnologias Utilizadas

### Backend
- **.NET 8** - Framework principal
- **Blazor Server** - UI Framework web
- **ASP.NET Core Web API** - APIs REST
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco de dados principal
- **Redis** - Cache e session storage
- **SignalR** - Comunicação em tempo real
- **JWT** - Autenticação e autorização
- **Docker** - Containerização
- **Aspire** - Orquestração e observabilidade

### Frontend
- **Blazor Server** - Interface web principal
- **Blazor WebAssembly** - SPA standalone
- **React 19** - Interface alternativa
- **.NET MAUI** - Apps mobile e desktop
- **Bootstrap 5** - Framework CSS
- **SignalR Client** - Tempo real
- **Vite** - Build tool (React)

### DevOps & Infraestrutura
- **Docker & Docker Compose** - Containerização
- **Aspire Dashboard** - Monitoramento
- **GitHub Actions** - CI/CD
- **Serilog** - Logging estruturado
- **Health Checks** - Monitoramento de saúde

## ?? Funcionalidades

### ?? Core Features

- **? Gerenciamento de APIs**: CRUD completo de interfaces de API
- **? Deploy Automático**: Containerização e deploy via Docker
- **? Monitoramento**: Métricas em tempo real, logs, health checks
- **? Multi-frontend**: Blazor, React, MAUI
- **? Autenticação**: JWT com roles e permissões
- **? Auditoria**: Log completo de todas as operações
- **? Cache Inteligente**: Redis para performance
- **? Notificações**: SignalR para updates em tempo real

### ?? Tipos de API Suportados

- **GraphQL**: Schema-first, queries otimizadas
- **REST**: OpenAPI/Swagger, endpoints RESTful
- **Híbrida**: Combinação GraphQL + REST

### ?? Infraestrutura Suportada

- **Bancos**: PostgreSQL, MySQL, SQLite, MongoDB, Redis
- **Mensageria**: RabbitMQ, Apache Kafka, Azure Service Bus, AWS SQS
- **Cache**: Redis, Memcached, In-Memory
- **Containers**: Docker com orquestração automática

## ?? Guias de Uso

### Para Desenvolvedores
- [Guia de Desenvolvimento](docs/development-guide.md)
- [API Reference](docs/api-reference.md)
- [Contribuindo](docs/contributing.md)

### Para Usuários
- [Manual do Usuário](docs/user-guide.md)
- [Configuração](docs/configuration.md)
- [Troubleshooting](docs/troubleshooting.md)

### Para DevOps
- [Deploy em Produção](docs/deployment.md)
- [Monitoramento](docs/monitoring.md)
- [Backup e Recuperação](docs/backup-recovery.md)

## ?? Testes

```powershell
# Executar todos os testes
dotnet test

# Executar testes com cobertura
dotnet test --collect:"XPlat Code Coverage"

# Executar testes específicos
dotnet test --filter "Category=Integration"
```

## ?? Contribuindo

Contribuições são muito bem-vindas! Por favor, leia nosso [Guia de Contribuição](docs/contributing.md) para detalhes sobre nosso código de conduta e o processo de submissão de pull requests.

### Desenvolvimento Local

1. **Fork** o projeto
2. **Clone** seu fork: `git clone https://github.com/SeuUsuario/SSBJr.ndb.integration.git`
3. **Crie** uma branch: `git checkout -b feature/nova-funcionalidade`
4. **Commit** suas mudanças: `git commit -am 'Adiciona nova funcionalidade'`
5. **Push** para a branch: `git push origin feature/nova-funcionalidade`
6. **Abra** um Pull Request

## ?? Licença

Este projeto está licenciado sob a [MIT License](LICENSE) - veja o arquivo LICENSE para detalhes.

## ?? Autores

- **Samuel Silva Barbosa Jr** - *Desenvolvedor Principal* - [@SamuelSBJr97](https://github.com/SamuelSBJr97)

## ?? Agradecimentos

- Microsoft pela excelente documentação do .NET e Aspire
- Comunidade open source pelas bibliotecas utilizadas
- Contributors que ajudaram no desenvolvimento

## ?? Suporte

- **Issues**: [GitHub Issues](https://github.com/SamuelSBJr97/SSBJr.ndb.integration/issues)
- **Discussions**: [GitHub Discussions](https://github.com/SamuelSBJr97/SSBJr.ndb.integration/discussions)
- **Email**: samuel.sbj97@gmail.com

## ?? Status do Projeto

- ? **Core System**: Completo e funcional
- ? **Web Frontend**: Blazor Server implementado
- ? **API Service**: REST/GraphQL funcionais
- ? **Database**: PostgreSQL + Redis
- ? **Authentication**: JWT completo
- ? **Docker Integration**: Deploy automático
- ?? **MAUI Apps**: Em desenvolvimento
- ?? **React Frontend**: Funcional (execução separada)
- ?? **Mobile Push**: Planejado
- ?? **Advanced Analytics**: Planejado

---

? **Star este projeto se ele foi útil para você!**

---

*Última atualização: Dezembro 2024*