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

$commonInfo = Join-Path $rootDir "src\common\CommonAssemblyInfo.cs"
$iconPath = Join-Path $rootDir "src\desktop\Resources\lodino.ico"
$manifestPath = Join-Path $rootDir "src\desktop\app.manifest"

$iconArg = if (Test-Path $iconPath) { "/win32icon:$iconPath" } else { $null }
$manifestArg = if (Test-Path $manifestPath) { "/win32manifest:$manifestPath" } else { $null }
$commonArg = if (Test-Path $commonInfo) { $commonInfo } else { $null }

# 1. Lodino.Core.dll
$coreDll = Join-Path $buildDir "Lodino.Core.dll"
Write-Host "Compilando Lodino.Core.dll..." -ForegroundColor Cyan
$coreSources = @(Get-ChildItem -Path (Join-Path $rootDir "src\core") -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName)
$coreArgs = @("/nologo", "/target:library", "/out:$coreDll", "/warn:4", $optFlag)
if ($commonArg) { $coreArgs += $commonArg }
$coreArgs += $coreSources
& $cscPath $coreArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 2. Lodino.exe (CLI)
$cliExe = Join-Path $buildDir "Lodino.exe"
Write-Host "Compilando Lodino.exe (CLI)..." -ForegroundColor Cyan
$cliSources = @(Get-ChildItem -Path (Join-Path $rootDir "src\cli") -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName)
$cliArgs = @("/nologo", "/target:exe", "/out:$cliExe", "/reference:$coreDll", "/warn:4", $optFlag)
if ($iconArg) { $cliArgs += $iconArg }
if ($commonArg) { $cliArgs += $commonArg }
$cliArgs += $cliSources
& $cscPath $cliArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 3. Lodino.App.exe (Desktop Nativo Win32)
$appExe = Join-Path $buildDir "Lodino.App.exe"
Write-Host "Compilando Lodino.App.exe (Desktop Nativo)..." -ForegroundColor Cyan
$desktopSources = @(Get-ChildItem -Path (Join-Path $rootDir "src\desktop") -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName)
$desktopArgs = @("/nologo", "/target:winexe", "/out:$appExe", "/reference:$coreDll", "/warn:4", $optFlag)
if ($iconArg) { $desktopArgs += $iconArg }
if ($manifestArg) { $desktopArgs += $manifestArg }
if ($commonArg) { $desktopArgs += $commonArg }
$desktopArgs += $desktopSources
& $cscPath $desktopArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 4. Lodino.Tests.exe
$testExe = Join-Path $buildDir "Lodino.Tests.exe"
Write-Host "Compilando Lodino.Tests.exe..." -ForegroundColor Cyan
$testSources = @(Get-ChildItem -Path (Join-Path $rootDir "tests\unit") -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName)
$testArgs = @("/nologo", "/target:exe", "/out:$testExe", "/reference:$coreDll", "/reference:$appExe", "/warn:4", $optFlag)
if ($commonArg) { $testArgs += $commonArg }
$testArgs += $testSources
& $cscPath $testArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Compilação concluída com sucesso em $buildDir!" -ForegroundColor Green

if ($RunTests) {
    Write-Host "`nExecutando suíte de testes..." -ForegroundColor Yellow
    & (Join-Path $buildDir "Lodino.Tests.exe")
    exit $LASTEXITCODE
}
