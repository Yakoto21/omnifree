# Script de compilação do núcleo nativo Lodino e execução dos testes unitários
param(
    [switch]$RunTests = $true,
    [switch]$Release = $false
)

$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent $PSScriptRoot
Set-Location $rootDir

$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $cscPath)) {
    Write-Error "Compilador csc.exe não encontrado em $cscPath"
    exit 1
}

$buildDir = Join-Path $rootDir "build"
if (-not (Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir | Out-Null
}

$optFlag = if ($Release) { "/optimize+" } else { "/debug:pdbonly" }

$coreDll = Join-Path $buildDir "Lodino.Core.dll"
Write-Host "Compilando Lodino.Core.dll..." -ForegroundColor Cyan
$coreSources = @(Get-ChildItem -Path (Join-Path $rootDir "src\core") -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName)
& $cscPath /nologo /target:library "/out:$coreDll" /warn:4 $optFlag $coreSources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$cliExe = Join-Path $buildDir "Lodino.exe"
Write-Host "Compilando Lodino.exe (CLI)..." -ForegroundColor Cyan
$cliSources = @(Get-ChildItem -Path (Join-Path $rootDir "src\cli") -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName)
& $cscPath /nologo /target:exe "/out:$cliExe" "/reference:$coreDll" /warn:4 $optFlag $cliSources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$appExe = Join-Path $buildDir "Lodino.App.exe"
Write-Host "Compilando Lodino.App.exe (Desktop Nativo)..." -ForegroundColor Cyan
$desktopSources = @(Get-ChildItem -Path (Join-Path $rootDir "src\desktop") -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName)
& $cscPath /nologo /target:winexe "/out:$appExe" "/reference:$coreDll" /warn:4 $optFlag $desktopSources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$testExe = Join-Path $buildDir "Lodino.Tests.exe"
Write-Host "Compilando Lodino.Tests.exe..." -ForegroundColor Cyan
$testSources = @(Get-ChildItem -Path (Join-Path $rootDir "tests\unit") -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName)
& $cscPath /nologo /target:exe "/out:$testExe" "/reference:$coreDll" "/reference:$appExe" /warn:4 $optFlag $testSources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Compilação concluída com sucesso em $buildDir!" -ForegroundColor Green

if ($RunTests) {
    Write-Host "`nExecutando suíte de testes..." -ForegroundColor Yellow
    & (Join-Path $buildDir "Lodino.Tests.exe")
    exit $LASTEXITCODE
}
