# Script para executar o React App manualmente
param(
    [switch]$Install,
    [switch]$Build,
    [switch]$Clean
)

$ReactPath = "SSBJr.ndb.integration.React"

Write-Host "?? SSBJr React App Manager" -ForegroundColor Green
Write-Host "=============================" -ForegroundColor Green

# Verificar se Node.js está instalado
try {
    $nodeVersion = node --version
    Write-Host "? Node.js encontrado: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "? Node.js não encontrado. Instale o Node.js primeiro." -ForegroundColor Red
    exit 1
}

# Verificar se o diretório existe
if (-not (Test-Path $ReactPath)) {
    Write-Host "? Diretório $ReactPath não encontrado." -ForegroundColor Red
    exit 1
}

# Ir para o diretório do React
Set-Location $ReactPath

if ($Clean) {
    Write-Host "?? Limpando cache e dependências..." -ForegroundColor Yellow
    Remove-Item "node_modules" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "package-lock.json" -Force -ErrorAction SilentlyContinue
    Remove-Item "dist" -Recurse -Force -ErrorAction SilentlyContinue
    npm cache clean --force
    Write-Host "? Limpeza concluída!" -ForegroundColor Green
}

if ($Install) {
    Write-Host "?? Instalando dependências..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Dependências instaladas com sucesso!" -ForegroundColor Green
    } else {
        Write-Host "? Erro ao instalar dependências" -ForegroundColor Red
        exit 1
    }
}

if ($Build) {
    Write-Host "?? Construindo aplicação..." -ForegroundColor Yellow
    npm run build
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Build concluído com sucesso!" -ForegroundColor Green
        Write-Host "?? Arquivos gerados em: $ReactPath/dist" -ForegroundColor Cyan
    } else {
        Write-Host "? Erro no build" -ForegroundColor Red
        exit 1
    }
} else {
    # Modo desenvolvimento por padrão
    Write-Host "?? Iniciando servidor de desenvolvimento..." -ForegroundColor Yellow
    Write-Host "?? Acesse: http://localhost:3000" -ForegroundColor Cyan
    Write-Host "? Hot reload habilitado" -ForegroundColor Cyan
    Write-Host "?? Pressione Ctrl+C para parar" -ForegroundColor Yellow
    Write-Host ""
    
    npm run dev
}

Set-Location ..
Write-Host "?? Finalizando React App Manager" -ForegroundColor Green