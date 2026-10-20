# ==============================================================================
# LODINO NATIVE SUITE -- BUILD DO PACOTE MSIX (Microsoft Store / Windows Desktop Bridge)
# ==============================================================================
param(
    [string]$PackageVersion = "1.0.0.0",
    [string]$InformationalVersion = "1.0.0-rc.2",
    [switch]$CheckOnly = $false,
    [switch]$SkipBuild = $false,
    [string]$CertThumbprint = ""
)

$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent $PSScriptRoot
Set-Location $rootDir

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  LODINO NATIVE SUITE -- EMPACOTADOR MSIX (DESKTOP BRIDGE)" -ForegroundColor Cyan
Write-Host "  Identidade: LuizGustavoGodoy.Lodino                    " -ForegroundColor Cyan
Write-Host "  Versao Pacote: $PackageVersion                         " -ForegroundColor Cyan
Write-Host "  Versao Informacional: $InformationalVersion            " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Inspecao de Ferramentas de Empacotamento MSIX (MakeAppx.exe / SignTool.exe)
Write-Host "`n[1/4] Verificando ferramentas do Windows SDK (MakeAppx.exe, SignTool.exe)..." -ForegroundColor Yellow

$makeAppxCandidates = @(
    "MakeAppx.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22000.0\x64\makeappx.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.19041.0\x64\makeappx.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.18362.0\x64\makeappx.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.17763.0\x64\makeappx.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64\makeappx.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\App Certification Kit\makeappx.exe",
    "${env:ProgramFiles}\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe",
    "${env:ProgramFiles}\Windows Kits\10\bin\x64\makeappx.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\8.1\bin\x64\makeappx.exe"
)

# Pesquisar nas pastas de Windows Kits via glob caso haja versoes pontuais
if (Test-Path "${env:ProgramFiles(x86)}\Windows Kits\10\bin") {
    $wildcardCandidates = Get-ChildItem -Path "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Filter "makeappx.exe" -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like "*x64*" } |
        Select-Object -ExpandProperty FullName
    if ($wildcardCandidates) {
        $makeAppxCandidates = @($wildcardCandidates) + $makeAppxCandidates
    }
}

$makeAppxPath = $null
$pathCmd = Get-Command "makeappx.exe" -ErrorAction SilentlyContinue
if ($pathCmd) {
    $makeAppxPath = $pathCmd.Source
} else {
    foreach ($cand in $makeAppxCandidates) {
        if ($cand -and (Test-Path $cand)) {
            $makeAppxPath = $cand
            break
        }
    }
}

# Pesquisar SignTool.exe
$signToolCandidates = @(
    "signtool.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64\signtool.exe"
)
$signToolPath = $null
$pathSign = Get-Command "signtool.exe" -ErrorAction SilentlyContinue
if ($pathSign) {
    $signToolPath = $pathSign.Source
} else {
    foreach ($cand in $signToolCandidates) {
        if ($cand -and (Test-Path $cand)) {
            $signToolPath = $cand
            break
        }
    }
}

if ($makeAppxPath) {
    Write-Host "  [OK] MakeAppx localizado: $makeAppxPath" -ForegroundColor Green
} else {
    Write-Host "  [AUSENTE] MakeAppx.exe nao encontrado no host." -ForegroundColor Yellow
}

if ($signToolPath) {
    Write-Host "  [OK] SignTool localizado: $signToolPath" -ForegroundColor Green
} else {
    Write-Host "  [INFO] SignTool.exe nao encontrado (necessario apenas para autoassinatura de teste local)." -ForegroundColor DarkGray
}

# 2. Verificacao e Compilacao dos Binarios de Producao
Write-Host "`n[2/4] Verificando binarios de producao..." -ForegroundColor Yellow

$coreDll = "build\Lodino.Core.dll"
$cliExe = "build\Lodino.exe"
$guiExe = "build\Lodino.App.exe"

$needBuild = (-not (Test-Path $coreDll)) -or (-not (Test-Path $cliExe)) -or (-not (Test-Path $guiExe))

if ($needBuild -and (-not $SkipBuild)) {
    Write-Host "  Compilando binarios em modo Release com csc.exe..." -ForegroundColor White
    & ".\scripts\build-core.ps1" -Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERRO: Falha na compilacao de producao." -ForegroundColor Red
        exit 1
    }
}

if ((-not (Test-Path $coreDll)) -or (-not (Test-Path $cliExe)) -or (-not (Test-Path $guiExe))) {
    Write-Host "ERRO: Binarios de producao ausentes em build\." -ForegroundColor Red
    exit 1
}
Write-Host "  [OK] Binarios de producao validados." -ForegroundColor Green

# 3. Montagem da Area de Staging para o Pacote MSIX
Write-Host "`n[3/4] Montando area de staging do pacote MSIX..." -ForegroundColor Yellow

$stagingDir = "dist\msix_staging"
if (Test-Path $stagingDir) {
    Remove-Item -Path $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null
New-Item -ItemType Directory -Path "$stagingDir\Assets" -Force | Out-Null

# Copiar os 3 binarios essenciais
Copy-Item -Path $coreDll -Destination "$stagingDir\Lodino.Core.dll" -Force
Copy-Item -Path $cliExe -Destination "$stagingDir\Lodino.exe" -Force
Copy-Item -Path $guiExe -Destination "$stagingDir\Lodino.App.exe" -Force

# Copiar os 4 arquivos de documentacao
$docFiles = @(
    "src\distribution\docs\README.txt",
    "src\distribution\docs\LICENSE.txt",
    "src\distribution\docs\KNOWN_LIMITATIONS.txt",
    "src\distribution\docs\THIRD_PARTY_NOTICES.txt"
)
foreach ($doc in $docFiles) {
    if (Test-Path $doc) {
        Copy-Item -Path $doc -Destination "$stagingDir\$(Split-Path $doc -Leaf)" -Force
    } else {
        Write-Host "AVISO: Documento nao encontrado: $doc" -ForegroundColor Yellow
    }
}

# Copiar manifesto oficial
$manifestSrc = "msix\AppxManifest.xml"
if (-not (Test-Path $manifestSrc)) {
    Write-Host "ERRO: Manifesto nao encontrado em $manifestSrc" -ForegroundColor Red
    exit 1
}
Copy-Item -Path $manifestSrc -Destination "$stagingDir\AppxManifest.xml" -Force

# Copiar ativos visuais
$assetFiles = Get-ChildItem -Path "msix\Assets" -Filter "*.png"
if ($assetFiles.Count -eq 0) {
    Write-Host "ERRO: Nenhum ativo visual PNG encontrado em msix\Assets." -ForegroundColor Red
    exit 1
}
foreach ($asset in $assetFiles) {
    Copy-Item -Path $asset.FullName -Destination "$stagingDir\Assets\$($asset.Name)" -Force
}

Write-Host "  [OK] Staging montado com sucesso em: $stagingDir" -ForegroundColor Green
$stagingItems = Get-ChildItem -Path $stagingDir -Recurse | Where-Object { -not $_.PSIsContainer }
Write-Host "  Total de arquivos no pacote MSIX: $($stagingItems.Count)" -ForegroundColor White
foreach ($item in $stagingItems) {
    $rel = $item.FullName.Substring((Resolve-Path $stagingDir).Path.Length + 1)
    Write-Host "    - $rel ($($item.Length) bytes)" -ForegroundColor DarkGray
}

# 4. Compilacao do Pacote MSIX com MakeAppx.exe
Write-Host "`n[4/4] Processamento do pacote MSIX..." -ForegroundColor Yellow

if (-not $makeAppxPath) {
    Write-Host "`n[STATUS: BLOCKED] MakeAppx.exe nao esta disponivel no ambiente host." -ForegroundColor Red
    Write-Host "Diagnostico do Ambiente:" -ForegroundColor Yellow
    Write-Host "  - O Windows SDK nao esta instalado ou suas ferramentas de linha de comando nao estao no PATH." -ForegroundColor White
    Write-Host "  - A area de staging foi preparada integralmente em '$stagingDir'." -ForegroundColor White
    Write-Host "  - O manifesto oficial 'msix\AppxManifest.xml' e os ativos graficos estao 100% prontos." -ForegroundColor White
    Write-Host "`nInstrucoes para geracao posterior:" -ForegroundColor Yellow
    Write-Host "  1. Instalar o Windows 10/11 SDK (ex: Windows SDK 10.0.22621.0 ou superior):" -ForegroundColor White
    Write-Host "     winget install --id Microsoft.WindowsSDK -e" -ForegroundColor White
    Write-Host "  2. Executar novamente o script de empacotamento:" -ForegroundColor White
    Write-Host "     powershell -ExecutionPolicy Bypass -File scripts\build-msix.ps1" -ForegroundColor White
    Write-Host "  3. Ou executar MakeAppx diretamente:" -ForegroundColor White
    Write-Host "     MakeAppx.exe pack /d dist\msix_staging /p dist\Lodino-v1.0.0.0-win-x64.msix /o /v" -ForegroundColor White

    if ($CheckOnly) {
        return @{ Status = "BLOCKED"; Reason = "MakeAppx.exe not found" }
    }
    exit 2
}

# Se MakeAppx estiver disponivel, compilar o pacote
$msixOut = "dist\Lodino-v$PackageVersion-win-x64.msix"
Write-Host "Executando MakeAppx pack..." -ForegroundColor White
& "$makeAppxPath" pack /d "$stagingDir" /p "$msixOut" /o /v
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: MakeAppx pack retornou codigo de saida $LASTEXITCODE." -ForegroundColor Red
    exit 1
}

Write-Host "  [SUCESSO] Pacote MSIX gerado em: $msixOut" -ForegroundColor Green

# Calcular SHA-256
$hash = (Get-FileHash -Path $msixOut -Algorithm SHA256).Hash.ToLowerInvariant()
$hashFile = "$msixOut.sha256"
Set-Content -Path $hashFile -Value "$hash  $(Split-Path $msixOut -Leaf)" -Encoding ASCII
Write-Host "  SHA-256: $hash" -ForegroundColor Cyan

# Assinatura local para teste, se solicitada
if ($CertThumbprint -and $signToolPath) {
    Write-Host "`nAssinando pacote para teste local com thumbprint $CertThumbprint..." -ForegroundColor Yellow
    & "$signToolPath" sign /fd SHA256 /sha1 "$CertThumbprint" "$msixOut"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] Pacote assinado para teste local com sucesso." -ForegroundColor Green
    } else {
        Write-Host "  [AVISO] Falha na assinatura local com SignTool." -ForegroundColor Yellow
    }
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  EMPACOTAMENTO MSIX CONCLUIDO COM SUCESSO" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
