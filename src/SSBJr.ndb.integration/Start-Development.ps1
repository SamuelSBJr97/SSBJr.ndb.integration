# Script para iniciar todos os servi�os
param(
    [switch]$SkipReact,
    [switch]$DevMode
)

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
        Write-Host "??  Node.js n�o encontrado. React app ser� pulado." -ForegroundColor Yellow
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

# Preparar React app se necess�rio
if (-not $SkipReact) {
    Write-Host "?? Preparando React app..." -ForegroundColor Yellow
    
    Push-Location "SSBJr.ndb.integration.React"
    try {
        # Instalar depend�ncias se necess�rio
        if (-not (Test-Path "node_modules")) {
            Write-Host "Instalando depend�ncias do React..." -ForegroundColor Cyan
            npm install
            if ($LASTEXITCODE -ne 0) {
                Write-Host "? Falha ao instalar depend�ncias do React" -ForegroundColor Red
                $SkipReact = $true
            }
        }
        
        # Build do React app se n�o for modo dev
        if (-not $DevMode -and -not $SkipReact) {
            Write-Host "Construindo React app..." -ForegroundColor Cyan
            npm run build
            if ($LASTEXITCODE -ne 0) {
                Write-Host "? Falha ao construir React app" -ForegroundColor Red
                $SkipReact = $true
            }
        }
    }
    finally {
        Pop-Location
    }
}

# Iniciar aplica��o
Write-Host "?? Iniciando aplica��o..." -ForegroundColor Green

if ($DevMode) {
    Write-Host "?? Modo de desenvolvimento ativado" -ForegroundColor Yellow
    
    # Iniciar React em modo dev em paralelo se n�o for para pular
    if (-not $SkipReact) {
        Write-Host "Iniciando React em modo dev..." -ForegroundColor Cyan
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'SSBJr.ndb.integration.React'; npm run dev"
    }
    
    # Aguardar um pouco antes de iniciar o AppHost
    Start-Sleep -Seconds 3
}

Write-Host "Iniciando Aspire AppHost..." -ForegroundColor Cyan
Write-Host ""
Write-Host "?? URLs esperadas:" -ForegroundColor Yellow
Write-Host "  - Dashboard Aspire: https://localhost:15888" -ForegroundColor Cyan
Write-Host "  - Blazor App: https://localhost:7080" -ForegroundColor Cyan
Write-Host "  - API Service: https://localhost:8080" -ForegroundColor Cyan
if (-not $SkipReact) {
    if ($DevMode) {
        Write-Host "  - React App (dev): http://localhost:5173" -ForegroundColor Cyan
    } else {
        Write-Host "  - React App: http://localhost:3000" -ForegroundColor Cyan
    }
}
Write-Host ""
Write-Host "?? Credenciais de demo:" -ForegroundColor Yellow
Write-Host "  - Usu�rio: admin" -ForegroundColor Cyan
Write-Host "  - Senha: admin123" -ForegroundColor Cyan
Write-Host ""

# Executar AppHost
cd "SSBJr.ndb.integration.Web/SSBJr.ndb.integration.Web.AppHost"
dotnet run

Write-Host "?? Aplica��o finalizada!" -ForegroundColor Green