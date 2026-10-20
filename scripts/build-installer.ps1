# ==============================================================================
# LODINO NATIVE SUITE -- BUILD DO INSTALADOR NSIS (Nullsoft Scriptable Install System)
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
Write-Host "  Ferramenta: NSIS 3 (makensis.exe)              " -ForegroundColor Cyan
Write-Host "  Versao Alvo: $Version                          " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Inspecao Exaustiva do Compilador NSIS (makensis.exe)
Write-Host "`n[1/3] Verificando presenca do compilador NSIS (makensis.exe)..." -ForegroundColor Yellow

$makensisCandidates = @(
    "makensis.exe",
    "${env:ProgramFiles(x86)}\NSIS\Bin\makensis.exe",
    "${env:ProgramFiles(x86)}\NSIS\makensis.exe",
    "${env:ProgramFiles}\NSIS\Bin\makensis.exe",
    "${env:ProgramFiles}\NSIS\makensis.exe",
    "${env:LOCALAPPDATA}\Programs\NSIS\makensis.exe",
    "C:\tools\nsis\makensis.exe",
    "C:\ProgramData\chocolatey\bin\makensis.exe"
)

$makensisPath = $null

# Checar PATH primeiro
$pathCmd = Get-Command "makensis.exe" -ErrorAction SilentlyContinue
if ($pathCmd) {
    $makensisPath = $pathCmd.Source
} else {
    foreach ($candidate in $makensisCandidates) {
        if ($candidate -and (Test-Path $candidate)) {
            $makensisPath = $candidate
            break
        }
    }
}

if (-not $makensisPath) {
    Write-Host "`n[STATUS: BLOCKED] Compilador NSIS (makensis.exe) nao foi detectado no host." -ForegroundColor Red
    Write-Host "Caminhos verificados:" -ForegroundColor DarkGray
    foreach ($candidate in $makensisCandidates) {
        Write-Host "  - $candidate" -ForegroundColor DarkGray
    }
    Write-Host "`nRequisito Mandatorio:" -ForegroundColor Yellow
    Write-Host "  - NSIS 3.10 ou 3.13 (https://nsis.sourceforge.io/Download)" -ForegroundColor White
    Write-Host "  - Alternativa winget: winget install --id NSIS.NSIS -e" -ForegroundColor White
    Write-Host "  - Regra de Politica: Instalacao silenciosa de terceiros nao e permitida." -ForegroundColor White
    Write-Host "  - A especificacao 'installer\lodino_setup.nsi' esta pronta para compilacao assim que a ferramenta estiver presente." -ForegroundColor Green
    
    if ($CheckOnly) {
        return @{ Status = "BLOCKED"; Reason = "makensis not found" }
    }
    exit 2
}

# Detectar versao do makensis
$detectedVersion = "Desconhecida"
try {
    $verOutput = & "$makensisPath" /VERSION 2>&1
    if ($verOutput) {
        $detectedVersion = $verOutput.ToString().Trim()
    }
} catch {
    # Ignora falha de deteccao de versao
}

Write-Host "Compilador detectado: $makensisPath (Versao: $detectedVersion)" -ForegroundColor Green

if ($CheckOnly) {
    return @{ Status = "AVAILABLE"; Path = $makensisPath; Version = $detectedVersion }
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

# 3. Compilacao do Instalador via NSIS
Write-Host "`n[3/3] Compilando instalador NSIS..." -ForegroundColor Yellow
$nsiPath = Join-Path $rootDir "installer\lodino_setup.nsi"

if (-not (Test-Path $nsiPath)) {
    Write-Error "Arquivo de especificacao NSIS nao encontrado: $nsiPath"
    exit 1
}

& "$makensisPath" "/DPRODUCT_VERSION=$Version" "/DSOURCE_DIR=$stagingDir" "$nsiPath"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha na compilacao do instalador pelo NSIS (makensis.exe). Codigo de saida: $LASTEXITCODE"
    exit $LASTEXITCODE
}

$distDir = Join-Path $rootDir "dist"
$installerExe = Join-Path $distDir "Lodino-v$Version-Setup-win-x64.exe"

if (Test-Path $installerExe) {
    $hash = (Get-FileHash -Path $installerExe -Algorithm SHA256).Hash.ToLowerInvariant()
    $size = (Get-Item $installerExe).Length
    Write-Host "`n=================================================" -ForegroundColor Green
    Write-Host "  INSTALADOR NSIS COMPILADO COM SUCESSO!" -ForegroundColor Green
    Write-Host "  Arquivo: $installerExe" -ForegroundColor Green
    Write-Host "  Tamanho: $size bytes" -ForegroundColor Green
    Write-Host "  SHA256:  $hash" -ForegroundColor Green
    Write-Host "=================================================" -ForegroundColor Green
} else {
    Write-Error "Arquivo de instalador esperado nao foi encontrado em: $installerExe"
    exit 1
}
