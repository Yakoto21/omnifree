# ==============================================================================
# LODINO NATIVE SUITE -- BUILD DO INSTALADOR INNO SETUP
# ==============================================================================
param(
    [string]$Version = "1.0.0-rc.2",
    [switch]$CheckOnly = $false
)

$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent $PSScriptRoot
Set-Location $rootDir

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  LODINO NATIVE SUITE -- COMPILADOR DE INSTALADOR" -ForegroundColor Cyan
Write-Host "  Versao: $Version                               " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Inspecao Exaustiva do Compilador Inno Setup (ISCC.exe)
Write-Host "`n[1/3] Verificando presenca do compilador Inno Setup (ISCC.exe)..." -ForegroundColor Yellow

$isccCandidates = @(
    "ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 5\ISCC.exe",
    "C:\tools\InnoSetup\ISCC.exe",
    "C:\ProgramData\chocolatey\bin\ISCC.exe"
)

$isccPath = $null

# Checar PATH primeiro
$pathCmd = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if ($pathCmd) {
    $isccPath = $pathCmd.Source
} else {
    foreach ($candidate in $isccCandidates) {
        if ($candidate -and (Test-Path $candidate)) {
            $isccPath = $candidate
            break
        }
    }
}

if (-not $isccPath) {
    Write-Host "`n[STATUS: BLOCKED] Compilador Inno Setup (ISCC.exe) nao foi detectado no host." -ForegroundColor Red
    Write-Host "Caminhos verificados:" -ForegroundColor DarkGray
    foreach ($candidate in $isccCandidates) {
        Write-Host "  - $candidate" -ForegroundColor DarkGray
    }
    Write-Host "`nRequisito Mandatorio:" -ForegroundColor Yellow
    Write-Host "  - Inno Setup 6.2 ou superior (https://jrsoftware.org/isdl.php)" -ForegroundColor White
    Write-Host "  - Regra de Politica: Instalacao silenciosa de terceiros nao e permitida." -ForegroundColor White
    Write-Host "  - A especificacao 'installer\lodino_setup.iss' esta pronta para compilacao assim que a ferramenta estiver presente." -ForegroundColor Green
    
    if ($CheckOnly) {
        return @{ Status = "BLOCKED"; Reason = "ISCC not found" }
    }
    exit 2
}

Write-Host "Compilador detectado: $isccPath" -ForegroundColor Green

if ($CheckOnly) {
    return @{ Status = "AVAILABLE"; Path = $isccPath }
}

# 2. Verificacao dos Artefatos de Staging
Write-Host "`n[2/3] Verificando diretorio de montagem Release..." -ForegroundColor Yellow
$stagingDir = Join-Path $rootDir "dist\Lodino-v$Version-Portable-win-x64"

if (-not (Test-Path $stagingDir)) {
    Write-Error "Diretorio de staging nao encontrado: $stagingDir. Execute scripts\publish.ps1 -Version $Version primeiro."
    exit 1
}

$requiredFiles = @(
    "Lodino.App.exe", "Lodino.exe", "Lodino.Core.dll",
    "README.txt", "LICENSE.txt", "KNOWN_LIMITATIONS.txt", "THIRD_PARTY_NOTICES.txt"
)

foreach ($rf in $requiredFiles) {
    $itemPath = Join-Path $stagingDir $rf
    if (-not (Test-Path $itemPath)) {
        Write-Error "Arquivo mandatorio nao encontrado no staging: $itemPath"
        exit 1
    }
}
Write-Host "Todos os 7 artefatos mandatorios presentes no staging." -ForegroundColor Green

# 3. Compilacao do Instalador
Write-Host "`n[3/3] Compilando instalador Inno Setup..." -ForegroundColor Yellow
$issPath = Join-Path $rootDir "installer\lodino_setup.iss"

& "$isccPath" "/DMyAppVersion=$Version" "/DSourceDir=$stagingDir" "$issPath"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha na compilacao do instalador pelo ISCC. Codigo de saida: $LASTEXITCODE"
    exit $LASTEXITCODE
}

$distDir = Join-Path $rootDir "dist"
$installerExe = Join-Path $distDir "Lodino-v$Version-Setup-win-x64.exe"

if (Test-Path $installerExe) {
    $hash = (Get-FileHash -Path $installerExe -Algorithm SHA256).Hash.ToLowerInvariant()
    $size = (Get-Item $installerExe).Length
    Write-Host "`n=================================================" -ForegroundColor Green
    Write-Host "  INSTALADOR COMPILADO COM SUCESSO!" -ForegroundColor Green
    Write-Host "  Arquivo: $installerExe" -ForegroundColor Green
    Write-Host "  Tamanho: $size bytes" -ForegroundColor Green
    Write-Host "  SHA256:  $hash" -ForegroundColor Green
    Write-Host "=================================================" -ForegroundColor Green
} else {
    Write-Error "Arquivo de instalador esperado nao foi encontrado em: $installerExe"
    exit 1
}
