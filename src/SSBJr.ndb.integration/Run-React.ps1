# Script para executar o React App manualmente
param(
    [switch]$Install,
    [switch]$Build,
    [switch]$Clean
)

$ReactPath = "SSBJr.ndb.integration.React"

Write-Host "?? SSBJr React App Manager" -ForegroundColor Green
Write-Host "=============================" -ForegroundColor Green

# Verificar se Node.js est� instalado
try {
    $nodeVersion = node --version
    Write-Host "? Node.js encontrado: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "? Node.js n�o encontrado. Instale o Node.js primeiro." -ForegroundColor Red
    exit 1
}

# Verificar se o diret�rio existe
if (-not (Test-Path $ReactPath)) {
    Write-Host "? Diret�rio $ReactPath n�o encontrado." -ForegroundColor Red
    exit 1
}

# Ir para o diret�rio do React
Set-Location $ReactPath

if ($Clean) {
    Write-Host "?? Limpando cache e depend�ncias..." -ForegroundColor Yellow
    Remove-Item "node_modules" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "package-lock.json" -Force -ErrorAction SilentlyContinue
    Remove-Item "dist" -Recurse -Force -ErrorAction SilentlyContinue
    npm cache clean --force
    Write-Host "? Limpeza conclu�da!" -ForegroundColor Green
}

if ($Install) {
    Write-Host "?? Instalando depend�ncias..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Depend�ncias instaladas com sucesso!" -ForegroundColor Green
    } else {
        Write-Host "? Erro ao instalar depend�ncias" -ForegroundColor Red
        exit 1
    }
}

if ($Build) {
    Write-Host "?? Construindo aplica��o..." -ForegroundColor Yellow
    npm run build
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Build conclu�do com sucesso!" -ForegroundColor Green
        Write-Host "?? Arquivos gerados em: $ReactPath/dist" -ForegroundColor Cyan
    } else {
        Write-Host "? Erro no build" -ForegroundColor Red
        exit 1
    }
} else {
    # Modo desenvolvimento por padr�o
    Write-Host "?? Iniciando servidor de desenvolvimento..." -ForegroundColor Yellow
    Write-Host "?? Acesse: http://localhost:3000" -ForegroundColor Cyan
    Write-Host "? Hot reload habilitado" -ForegroundColor Cyan
    Write-Host "?? Pressione Ctrl+C para parar" -ForegroundColor Yellow
    Write-Host ""
    
    npm run dev
}

Set-Location ..
Write-Host "?? Finalizando React App Manager" -ForegroundColor Green