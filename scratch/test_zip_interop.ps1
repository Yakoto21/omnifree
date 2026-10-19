# Auditoria Externa e Teste de Interoperabilidade Bidirecional do Motor ZIP do Lodino
$ErrorActionPreference = "Stop"

Write-Host "=== TESTE DE INTEROPERABILIDADE BIDIRECIONAL ZIP ===" -ForegroundColor Cyan

$tempRoot = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "lodino_zip_interop_" + [System.Guid]::NewGuid().ToString("N"))
[System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null

try {
    $srcDir = [System.IO.Path]::Combine($tempRoot, "source_files")
    [System.IO.Directory]::CreateDirectory($srcDir) | Out-Null
    [System.IO.Directory]::CreateDirectory([System.IO.Path]::Combine($srcDir, "subpasta")) | Out-Null

    # Criar arquivos de teste com acentos e conteúdo variado
    $f1 = [System.IO.Path]::Combine($srcDir, "documento_café.txt")
    $f2 = [System.IO.Path]::Combine($srcDir, "subpasta", "dados.json")
    $f3 = [System.IO.Path]::Combine($srcDir, "repetitivo.txt")

    [System.IO.File]::WriteAllText($f1, "Relatório financeiro do café e açúcar - 2026. Acentos: áéíóú çãõ.", [System.Text.Encoding]::UTF8)
    [System.IO.File]::WriteAllText($f2, '{"sistema":"lodino","versao":"1.0","seguranca":true}', [System.Text.Encoding]::UTF8)

    $sb = New-Object System.Text.StringBuilder
    for ($i = 0; $i -lt 100; $i++) {
        $sb.AppendLine("Linha repetitiva de teste LZ77 $i com padrões recorrentes de compressão.") | Out-Null
    }
    [System.IO.File]::WriteAllText($f3, $sb.ToString(), [System.Text.Encoding]::UTF8)

    Write-Host "[1/6] Diretório fonte preparado com 3 arquivos e 1 subpasta." -ForegroundColor Yellow

    # Teste 1: Lodino CLI zip -> Windows Expand-Archive
    $lodinoZip = [System.IO.Path]::Combine($tempRoot, "lodino_created.zip")
    $cliPath = "build\Lodino.exe"

    Write-Host "[2/6] Executando 'Lodino.exe zip'..." -ForegroundColor Yellow
    $zipOutput = & $cliPath zip $srcDir -o $lodinoZip -c optimal
    Write-Host $zipOutput

    if (-not [System.IO.File]::Exists($lodinoZip)) {
        throw "Arquivo ZIP gerado pelo Lodino não foi encontrado: $lodinoZip"
    }

    $zipSize = (Get-Item $lodinoZip).Length
    Write-Host "Tamanho do ZIP gerado: $zipSize bytes" -ForegroundColor Green

    # Teste 2: Lodino CLI list-zip e inspect
    Write-Host "[3/6] Executando 'Lodino.exe list-zip' e 'inspect'..." -ForegroundColor Yellow
    $listOutput = & $cliPath list-zip $lodinoZip
    Write-Host $listOutput

    $inspectOutput = & $cliPath inspect $lodinoZip
    Write-Host $inspectOutput

    # Teste 3: Windows Expand-Archive contra ZIP do Lodino
    Write-Host "[4/6] Descompactando ZIP do Lodino com 'Expand-Archive' nativo do Windows..." -ForegroundColor Yellow
    $winExtractDir = [System.IO.Path]::Combine($tempRoot, "win_extracted")
    Expand-Archive -Path $lodinoZip -DestinationPath $winExtractDir -Force

    # Validar integridade com hash SHA256
    $hOrig1 = (Get-FileHash $f1 -Algorithm SHA256).Hash
    $hExt1 = (Get-FileHash (Join-Path $winExtractDir "documento_café.txt") -Algorithm SHA256).Hash
    if ($hOrig1 -ne $hExt1) { throw "Hash mismatch em documento_café.txt!" }

    $hOrig2 = (Get-FileHash $f2 -Algorithm SHA256).Hash
    $hExt2 = (Get-FileHash (Join-Path $winExtractDir "subpasta\dados.json") -Algorithm SHA256).Hash
    if ($hOrig2 -ne $hExt2) { throw "Hash mismatch em subpasta\dados.json!" }

    $hOrig3 = (Get-FileHash $f3 -Algorithm SHA256).Hash
    $hExt3 = (Get-FileHash (Join-Path $winExtractDir "repetitivo.txt") -Algorithm SHA256).Hash
    if ($hOrig3 -ne $hExt3) { throw "Hash mismatch em repetitivo.txt!" }

    Write-Host ">>> Windows Expand-Archive extraiu com SUCESSO 100% dos arquivos com SHA256 IDÊNTICO! <<<" -ForegroundColor Green

    # Teste 4: Windows Compress-Archive -> Lodino CLI unzip
    Write-Host "[5/6] Criando arquivo ZIP com ferramenta nativa do Windows (Compress-Archive)..." -ForegroundColor Yellow
    $winZip = [System.IO.Path]::Combine($tempRoot, "windows_created.zip")
    Compress-Archive -Path "$srcDir\*" -DestinationPath $winZip -Force

    Write-Host "ZIP gerado pelo Windows: $((Get-Item $winZip).Length) bytes"

    Write-Host "[6/6] Descompactando ZIP do Windows com 'Lodino.exe unzip'..." -ForegroundColor Yellow
    $lodinoExtractDir = [System.IO.Path]::Combine($tempRoot, "lodino_extracted")
    $unzipOutput = & $cliPath unzip $winZip -o $lodinoExtractDir
    Write-Host $unzipOutput

    $hLod1 = (Get-FileHash (Join-Path $lodinoExtractDir "documento_café.txt") -Algorithm SHA256).Hash
    if ($hOrig1 -ne $hLod1) { throw "Hash mismatch Lodino em documento_café.txt!" }

    $hLod2 = (Get-FileHash (Join-Path $lodinoExtractDir "subpasta\dados.json") -Algorithm SHA256).Hash
    if ($hOrig2 -ne $hLod2) { throw "Hash mismatch Lodino em subpasta\dados.json!" }

    $hLod3 = (Get-FileHash (Join-Path $lodinoExtractDir "repetitivo.txt") -Algorithm SHA256).Hash
    if ($hOrig3 -ne $hLod3) { throw "Hash mismatch Lodino em repetitivo.txt!" }

    Write-Host ">>> Lodino.exe unzip extraiu com SUCESSO 100% dos arquivos gerados pelo Windows com SHA256 IDÊNTICO! <<<" -ForegroundColor Green

    Write-Host "`n=======================================================" -ForegroundColor Cyan
    Write-Host "  AUDITORIA DE INTEROPERABILIDADE CONCLUÍDA COM SUCESSO! " -ForegroundColor Cyan
    Write-Host "=======================================================" -ForegroundColor Cyan
}
finally {
    if ([System.IO.Directory]::Exists($tempRoot)) {
        [System.IO.Directory]::Delete($tempRoot, $true)
    }
}
