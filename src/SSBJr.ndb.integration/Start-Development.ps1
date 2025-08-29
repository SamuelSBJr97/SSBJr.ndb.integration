# Script para iniciar todos os servi�os
param(
    [switch]$SkipReact,
    [switch]$DevMode,
    [switch]$ReactOnly
)

if ($ReactOnly) {
    Write-Host "?? Iniciando apenas o React App..." -ForegroundColor Green
    .\Run-React.ps1
    exit
}

Write-Host "?? Iniciando SSBJr API Manager..." -ForegroundColor Green

# Fun��o para verificar se uma porta est� em uso
function Test-Port {
    param([int]$Port)
    try {
        $connection = New-Object System.Net.Sockets.TcpClient("localhost", $Port)
        $connection.Close()
        return $true
    }
    catch {
        return $false
    }
}

# Verificar depend�ncias
Write-Host "?? Verificando depend�ncias..." -ForegroundColor Yellow

# Verificar Docker
try {
    docker --version | Out-Null
    Write-Host "? Docker encontrado" -ForegroundColor Green
}
catch {
    Write-Host "? Docker n�o encontrado. Instale o Docker Desktop." -ForegroundColor Red
    exit 1
}

# Verificar .NET 8
try {
    dotnet --version | Out-Null
    Write-Host "? .NET SDK encontrado" -ForegroundColor Green
}
catch {
    Write-Host "? .NET 8 SDK n�o encontrado. Instale o .NET 8 SDK." -ForegroundColor Red
    exit 1
}

# Se n�o for para pular React, verificar Node.js
if (-not $SkipReact) {
    try {
        node --version | Out-Null
        npm --version | Out-Null
        Write-Host "? Node.js e npm encontrados" -ForegroundColor Green
    }
    catch {
        Write-Host "??  Node.js n�o encontrado. React app ser� executado separadamente." -ForegroundColor Yellow
        Write-Host "?? Para executar React: .\Run-React.ps1" -ForegroundColor Cyan
        $SkipReact = $true
    }
}

# Verificar portas dispon�veis
$ports = @(5000, 5001, 7080, 8080, 3000, 5432, 6379)
foreach ($port in $ports) {
    if (Test-Port $port) {
        Write-Host "??  Porta $port est� em uso" -ForegroundColor Yellow
    }
}

# Construir aplica��es
Write-Host "?? Construindo aplica��es..." -ForegroundColor Yellow

# Build do projeto principal
Write-Host "Construindo projeto Blazor..." -ForegroundColor Cyan
dotnet build "SSBJr.ndb.integration.Web/SSBJr.ndb.integration.Web/SSBJr.ndb.integration.Web.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Falha ao construir projeto Blazor" -ForegroundColor Red
    exit 1
}

# Build do API Service
Write-Host "Construindo API Service..." -ForegroundColor Cyan
dotnet build "SSBJr.ndb.integration.Web/SSBJr.ndb.integration.Web.ApiService/SSBJr.ndb.integration.Web.ApiService.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Falha ao construir API Service" -ForegroundColor Red
    exit 1
}

# Build do AppHost
Write-Host "Construindo AppHost..." -ForegroundColor Cyan
dotnet build "SSBJr.ndb.integration.Web/SSBJr.ndb.integration.Web.AppHost/SSBJr.ndb.integration.Web.AppHost.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Falha ao construir AppHost" -ForegroundColor Red
    exit 1
}

Write-Host "? Build conclu�do com sucesso!" -ForegroundColor Green

# Iniciar React em paralelo se solicitado
if ($DevMode -and -not $SkipReact) {
    Write-Host "?? Iniciando React em modo paralelo..." -ForegroundColor Yellow
    Start-Process powershell -ArgumentList "-NoExit", "-Command", ".\Run-React.ps1"
    Start-Sleep -Seconds 2
}

# Iniciar aplica��o
Write-Host "?? Iniciando Aspire AppHost..." -ForegroundColor Green
Write-Host ""
Write-Host "?? URLs esperadas:" -ForegroundColor Yellow
Write-Host "  - Dashboard Aspire: https://localhost:15888" -ForegroundColor Cyan
Write-Host "  - Blazor Web App: https://localhost:7080" -ForegroundColor Cyan
Write-Host "  - API Service: https://localhost:8080" -ForegroundColor Cyan
if ($DevMode -and -not $SkipReact) {
    Write-Host "  - React App (paralelo): http://localhost:3000" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "?? Credenciais de demo:" -ForegroundColor Yellow
Write-Host "  - Usu�rio: admin" -ForegroundColor Cyan
Write-Host "  - Senha: admin123" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Para executar React separadamente: .\Run-React.ps1" -ForegroundColor Cyan
Write-Host ""

# Executar AppHost
cd "SSBJr.ndb.integration.Web/SSBJr.ndb.integration.Web.AppHost"
dotnet run

Write-Host "?? Aplica��o finalizada!" -ForegroundColor Green