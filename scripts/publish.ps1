# Script de publicacao oficial do pacote portatil Release Candidate do Lodino Native Suite
param(
    [string]$Version = "1.0.0-rc.1",
    [switch]$SkipTests = $false
)

$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent $PSScriptRoot
Set-Location $rootDir

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  LODINO NATIVE SUITE -- PIPELINE DE PUBLICACAO  " -ForegroundColor Cyan
Write-Host "  Versao: $Version                               " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Compilacao Release com Testes de Aceitacao
Write-Host "`n[1/5] Compilando assemblies de producao em modo Release..." -ForegroundColor Yellow
$buildScript = Join-Path $PSScriptRoot "build-core.ps1"

if ($SkipTests) {
    & $buildScript -Release -RunTests:$false
} else {
    & $buildScript -Release -RunTests:$true
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha na compilacao ou nos testes de aceitacao. Codigo de saida: $LASTEXITCODE. Publicacao abortada."
    exit $LASTEXITCODE
}

# 2. Configuracao do Diretorio Isolado de Publicacao
$distDir = Join-Path $rootDir "dist"
$packageBaseName = "Lodino-v" + $Version + "-Portable-win-x64"
$stagingDir = Join-Path $distDir $packageBaseName
$zipFile = Join-Path $distDir ($packageBaseName + ".zip")
$sha256File = Join-Path $distDir ($packageBaseName + ".zip.sha256")
$manifestFile = Join-Path $distDir ($packageBaseName + "-manifest.txt")

Write-Host "`n[2/5] Preparando diretorio de montagem isolado em $stagingDir..." -ForegroundColor Yellow
if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir | Out-Null
}
if (Test-Path $stagingDir) {
    Remove-Item -Path $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null

# 3. Copia EXCLUSIVA dos Binarios de Producao e Documentacao
Write-Host "`n[3/5] Copiando binarios de producao e documentacao para o pacote..." -ForegroundColor Yellow
$buildDir = Join-Path $rootDir "build"
$requiredBinaries = @("Lodino.Core.dll", "Lodino.exe", "Lodino.App.exe")

foreach ($bin in $requiredBinaries) {
    $srcPath = Join-Path $buildDir $bin
    if (-not (Test-Path $srcPath)) {
        Write-Error "Binario obrigatorio nao encontrado: $srcPath"
        exit 1
    }
    Copy-Item -Path $srcPath -Destination $stagingDir -Force
    $fileLen = (Get-Item $srcPath).Length
    $infoMsg = "  + {0} ({1} bytes)" -f $bin, $fileLen
    Write-Host $infoMsg -ForegroundColor Green
}

# Copia dos Documentos de Distribuicao
$docFiles = @("README.txt", "LICENSE.txt", "KNOWN_LIMITATIONS.txt", "THIRD_PARTY_NOTICES.txt")
$docsSourceDir = Join-Path $rootDir "src\distribution\docs"
foreach ($doc in $docFiles) {
    $docSrc = Join-Path $docsSourceDir $doc
    if (Test-Path $docSrc) {
        Copy-Item -Path $docSrc -Destination $stagingDir -Force
        $docMsg = "  + {0}" -f $doc
        Write-Host $docMsg -ForegroundColor Green
    } else {
        Write-Warning "Documento nao encontrado: $docSrc"
    }
}

# 4. Geracao do Arquivo ZIP Portatil
Write-Host "`n[4/5] Empacotando arquivo ZIP portatil: $zipFile..." -ForegroundColor Yellow
if (Test-Path $zipFile) {
    Remove-Item -Path $zipFile -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stagingDir, $zipFile, [System.IO.Compression.CompressionLevel]::Optimal, $false)

if (-not (Test-Path $zipFile)) {
    Write-Error "Falha ao gerar o arquivo ZIP de distribuicao."
    exit 1
}

$zipLength = (Get-Item $zipFile).Length
$zipMsg = "  -> ZIP gerado com sucesso! Tamanho total: {0} bytes" -f $zipLength
Write-Host $zipMsg -ForegroundColor Green

# 5. Calculo do Hash SHA-256 e Geracao do Manifesto
Write-Host "`n[5/5] Calculando checksum SHA-256 e gerando manifesto de integridade..." -ForegroundColor Yellow
$hash = (Get-FileHash -Path $zipFile -Algorithm SHA256).Hash.ToLowerInvariant()
$shaLine = "{0} *{1}.zip" -f $hash, $packageBaseName
Set-Content -Path $sha256File -Value $shaLine
Write-Host "  SHA-256 do ZIP: $hash" -ForegroundColor Cyan

# Manifesto detalhado dos arquivos incluidos no pacote
$manifestLines = New-Object System.Collections.Generic.List[string]
$manifestLines.Add("================================================================================")
$manifestLines.Add("  LODINO NATIVE SUITE -- MANIFESTO DE ARQUIVOS PORTATEIS")
$manifestLines.Add("================================================================================")
$manifestLines.Add("Versao do Produto: " + $Version)
$manifestLines.Add("Data de Geracao: " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
$manifestLines.Add("Arquivo do Pacote: " + $packageBaseName + ".zip (" + $zipLength + " bytes)")
$manifestLines.Add("Checksum SHA-256: " + $hash)
$manifestLines.Add("")
$manifestLines.Add("Arquivos Incluidos no Pacote:")
$manifestLines.Add("--------------------------------------------------------------------------------")

Get-ChildItem -Path $stagingDir | ForEach-Object {
    $fHash = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $line = "{0,-30} {1,10} bytes  SHA256: {2}" -f $_.Name, $_.Length, $fHash
    $manifestLines.Add($line)
}

$manifestLines.Add("--------------------------------------------------------------------------------")
$manifestLines.Add("Total de Arquivos: " + (Get-ChildItem -Path $stagingDir).Count)
$manifestLines.Add("Zero dependencias externas, zero PDBs, zero testes no pacote de producao.")

Set-Content -Path $manifestFile -Value ($manifestLines -join "`r`n")

Write-Host "`n=================================================" -ForegroundColor Green
Write-Host "  PACOTE PORTATIL RC1 PUBLICADO COM SUCESSO!     " -ForegroundColor Green
Write-Host "  Arquivo: $zipFile" -ForegroundColor Green
Write-Host "  SHA256:  $hash" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
