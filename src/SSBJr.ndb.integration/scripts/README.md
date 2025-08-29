# Scripts de Automa��o - SSBJr.ndb.integration

> Scripts PowerShell para facilitar desenvolvimento e deploy

## ?? Vis�o Geral

Cole��o de scripts PowerShell para automatizar tarefas comuns de desenvolvimento, build, teste e deploy do projeto SSBJr.ndb.integration.

## ?? Scripts Dispon�veis

### Start-Development.ps1
**Script principal** para inicializar todo o ambiente de desenvolvimento.

```powershell
# Uso b�sico
.\Start-Development.ps1

# Com React em paralelo
.\Start-Development.ps1 -DevMode

# Pular React completamente
.\Start-Development.ps1 -SkipReact

# Executar apenas React
.\Start-Development.ps1 -ReactOnly
```

#### Funcionalidades
- ? Verifica��o autom�tica de depend�ncias (.NET, Docker, Node.js)
- ? Build de todos os projetos
- ? Inicializa��o do Aspire AppHost
- ? Op��o de execu��o paralela do React
- ? Verifica��o de portas em uso
- ? URLs de acesso organizadas

### Run-React.ps1
**Script dedicado** para gerenciamento do React App.

```powershell
# Desenvolvimento (padr�o)
.\Run-React.ps1

# Instalar depend�ncias
.\Run-React.ps1 -Install

# Build para produ��o
.\Run-React.ps1 -Build

# Limpeza completa
.\Run-React.ps1 -Clean -Install
```

#### Funcionalidades
- ? Instala��o autom�tica de depend�ncias
- ? Servidor de desenvolvimento com hot reload
- ? Build otimizado para produ��o
- ? Limpeza de cache e node_modules
- ? Verifica��o de Node.js

## ?? Detalhamento dos Scripts

### Start-Development.ps1

#### Par�metros
```powershell
param(
    [switch]$SkipReact,      # Pular execu��o do React
    [switch]$DevMode,        # Modo desenvolvimento com React paralelo
    [switch]$ReactOnly       # Executar apenas React
)
```

#### Verifica��es de Depend�ncias
```powershell
# Docker Desktop
docker --version

# .NET 8 SDK
dotnet --version

# Node.js (se n�o skip React)
node --version && npm --version
```

#### Portas Verificadas
| Porta | Servi�o |
|-------|---------|
| 5000-5001 | Blazor Web |
| 7080 | Blazor HTTPS |
| 8080 | API Service |
| 3000 | React Dev Server |
| 5432 | PostgreSQL |
| 6379 | Redis |
| 15888 | Aspire Dashboard |

#### Build Order
1. **SSBJr.ndb.integration.Web** (Blazor principal)
2. **SSBJr.ndb.integration.Web.ApiService** (API)
3. **SSBJr.ndb.integration.Web.AppHost** (Aspire)

### Run-React.ps1

#### Par�metros
```powershell
param(
    [switch]$Install,        # Instalar depend�ncias
    [switch]$Build,          # Build para produ��o
    [switch]$Clean           # Limpeza completa
)
```

#### Opera��es de Limpeza
```powershell
# Remove node_modules
Remove-Item "node_modules" -Recurse -Force

# Remove package-lock.json
Remove-Item "package-lock.json" -Force

# Remove build artifacts
Remove-Item "dist" -Recurse -Force

# Limpa cache npm
npm cache clean --force
```

#### Build Modes
- **Desenvolvimento**: `npm run dev` (hot reload)
- **Produ��o**: `npm run build` (otimizado)
- **Preview**: `npm run preview` (teste local do build)

## ?? Como Usar

### Cen�rio 1: Desenvolvimento Completo
```powershell
# Iniciar tudo (Aspire + Blazor + API + DB)
.\Start-Development.ps1

# URLs dispon�veis:
# https://localhost:15888  - Dashboard Aspire
# https://localhost:7080   - Blazor Web App
# https://localhost:8080   - API Service
```

### Cen�rio 2: Desenvolvimento com React
```powershell
# Aspire + React em paralelo
.\Start-Development.ps1 -DevMode

# URLs adicionais:
# http://localhost:3000    - React App
```

### Cen�rio 3: Apenas React
```powershell
# Apenas React (desenvolvimento frontend)
.\Start-Development.ps1 -ReactOnly

# Ou diretamente:
.\Run-React.ps1
```

### Cen�rio 4: Reset Completo do React
```powershell
# Limpeza completa e reinstala��o
.\Run-React.ps1 -Clean -Install

# Depois executar
.\Run-React.ps1
```

## ??? Customiza��o

### Modificar Portas Padr�o
```powershell
# No Start-Development.ps1, modificar:
$ports = @(5000, 5001, 7080, 8080, 3000, 5432, 6379)

# No Run-React.ps1, o Vite usa a configura��o em vite.config.js:
server: {
    port: 3000,  # Modificar aqui
    host: '0.0.0.0'
}
```

### Adicionar Novos Projetos
```powershell
# No Start-Development.ps1, adicionar build:
Write-Host "Construindo Novo Projeto..." -ForegroundColor Cyan
dotnet build "caminho/para/NovoProject.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Falha ao construir Novo Projeto" -ForegroundColor Red
    exit 1
}
```

### Verifica��es Personalizadas
```powershell
# Adicionar verifica��o de nova ferramenta
try {
    nova-ferramenta --version | Out-Null
    Write-Host "? Nova Ferramenta encontrada" -ForegroundColor Green
}
catch {
    Write-Host "? Nova Ferramenta n�o encontrada" -ForegroundColor Red
    exit 1
}
```

## ?? Logs e Debugging

### Verbose Mode
```powershell
# Adicionar debug verbose
$VerbosePreference = "Continue"
Write-Verbose "Iniciando verifica��es..."

# Ou usar -Verbose
.\Start-Development.ps1 -Verbose
```

### Captura de Erros
```powershell
try {
    # Opera��o
}
catch {
    Write-Host "? Erro: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Yellow
}
```

### Log File
```powershell
# Redirecionar sa�da para arquivo
.\Start-Development.ps1 | Tee-Object -FilePath "startup.log"

# Apenas erros
.\Start-Development.ps1 2> "errors.log"
```

## ?? Deploy Scripts (Futuros)

### Deploy-Production.ps1
```powershell
# Script para deploy em produ��o
param(
    [string]$Environment = "Production",
    [string]$Version,
    [switch]$SkipTests
)

# Build release
dotnet build -c Release

# Executar testes (se n�o skip)
if (-not $SkipTests) {
    dotnet test
}

# Deploy via Docker
docker build -t ssbjr-app:$Version .
docker push registry.com/ssbjr-app:$Version

# Deploy Kubernetes
kubectl apply -f deploy/k8s/
kubectl set image deployment/ssbjr-app app=registry.com/ssbjr-app:$Version
```

### Setup-Environment.ps1
```powershell
# Script para configurar ambiente de desenvolvimento
param(
    [switch]$InstallTools,
    [switch]$SetupDatabase,
    [switch]$ConfigureDocker
)

if ($InstallTools) {
    # Instalar ferramentas necess�rias
    winget install Microsoft.DotNet.SDK.8
    winget install Docker.DockerDesktop
    winget install OpenJS.NodeJS
}

if ($SetupDatabase) {
    # Configurar PostgreSQL via Docker
    docker run -d --name postgres-dev -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:15
}

if ($ConfigureDocker) {
    # Configurar Docker networks
    docker network create ssbjr-network
}
```

## ?? Testing Scripts

### Run-Tests.ps1
```powershell
param(
    [string]$Project = "All",
    [switch]$Coverage,
    [switch]$Integration
)

switch ($Project) {
    "All" {
        dotnet test --logger trx --results-directory TestResults
    }
    "Unit" {
        dotnet test --filter Category=Unit
    }
    "Integration" {
        if ($Integration) {
            dotnet test --filter Category=Integration
        }
    }
}

if ($Coverage) {
    dotnet test --collect:"XPlat Code Coverage"
    reportgenerator -reports:"**/*.cobertura.xml" -targetdir:"coverage"
}
```

## ?? Seguran�a

### Valida��o de Scripts
```powershell
# Verificar assinatura digital (em produ��o)
if ((Get-AuthenticodeSignature $MyInvocation.MyCommand.Path).Status -ne "Valid") {
    Write-Error "Script n�o assinado ou assinatura inv�lida"
    exit 1
}
```

### Execution Policy
```powershell
# Verificar pol�tica de execu��o
if ((Get-ExecutionPolicy) -eq "Restricted") {
    Write-Host "??  ExecutionPolicy restritiva. Execute:" -ForegroundColor Yellow
    Write-Host "Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser" -ForegroundColor Cyan
    exit 1
}
```

## ?? Suporte

### Problemas Comuns

#### Script n�o executa
```powershell
# Verificar execution policy
Get-ExecutionPolicy

# Alterar se necess�rio
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### Portas em uso
```powershell
# Verificar processos usando porta
netstat -ano | findstr :8080

# Matar processo se necess�rio
taskkill /PID <PID> /F
```

#### Docker n�o responde
```powershell
# Reiniciar Docker Desktop
Restart-Service -Name "Docker Desktop Service"

# Ou via GUI: Right-click Docker icon > Restart
```

---

*Para informa��es gerais do projeto, veja o [README principal](README.md).*