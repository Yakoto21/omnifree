$ErrorActionPreference = 'Stop'
$testDir = 'scratch\deflate_dynamic\interop_final'
if (Test-Path $testDir) { Remove-Item $testDir -Recurse -Force }
New-Item -ItemType Directory -Path "$testDir\input" -Force | Out-Null
New-Item -ItemType Directory -Path "$testDir\out_ps" -Force | Out-Null
New-Item -ItemType Directory -Path "$testDir\out_py" -Force | Out-Null
New-Item -ItemType Directory -Path "$testDir\out_tar" -Force | Out-Null
New-Item -ItemType Directory -Path "$testDir\out_lodino" -Force | Out-Null

# 1. Prepare files
$text = (1..100 | ForEach-Object { "Line $_ : Dynamic Huffman compression test in Lodino Phase 12.6." }) -join "`r`n"
[System.IO.File]::WriteAllText("$testDir\input\prose.txt", $text, [System.Text.Encoding]::UTF8)

$short = "port=8080`r`nhost=localhost`r`n"
[System.IO.File]::WriteAllText("$testDir\input\small.ini", $short, [System.Text.Encoding]::UTF8)

$rnd = New-Object byte[] 4096
(New-Object System.Random(42)).NextBytes($rnd)
[System.IO.File]::WriteAllBytes("$testDir\input\random.bin", $rnd)

# Compute reference SHA-256
$hashText = (Get-FileHash "$testDir\input\prose.txt" -Algorithm SHA256).Hash
$hashShort = (Get-FileHash "$testDir\input\small.ini" -Algorithm SHA256).Hash
$hashRnd = (Get-FileHash "$testDir\input\random.bin" -Algorithm SHA256).Hash

Write-Host "Reference SHA-256 Hashes:" -ForegroundColor Cyan
Write-Host "  prose.txt:  $hashText"
Write-Host "  small.ini:  $hashShort"
Write-Host "  random.bin: $hashRnd"

# 2. Compress using Lodino.exe
$zipPath = "$testDir\lodino_phase12_6.zip"
& build\Lodino.exe zip "$testDir\input" "$zipPath" --level optimal
Write-Host ("`nZIP created via Lodino.exe. Size: {0} bytes" -f (Get-Item $zipPath).Length) -ForegroundColor Cyan

# 3. Extract with PowerShell Expand-Archive
Expand-Archive -Path $zipPath -DestinationPath "$testDir\out_ps" -Force
$psText = (Get-FileHash (Get-ChildItem "$testDir\out_ps" -Recurse -Filter "prose.txt").FullName -Algorithm SHA256).Hash
$psShort = (Get-FileHash (Get-ChildItem "$testDir\out_ps" -Recurse -Filter "small.ini").FullName -Algorithm SHA256).Hash
$psRnd = (Get-FileHash (Get-ChildItem "$testDir\out_ps" -Recurse -Filter "random.bin").FullName -Algorithm SHA256).Hash

$psMatch = ($psText -eq $hashText) -and ($psShort -eq $hashShort) -and ($psRnd -eq $hashRnd)
Write-Host ("PowerShell Expand-Archive 100% Match: {0}" -f $psMatch) -ForegroundColor $(if ($psMatch) { 'Green' } else { 'Red' })

# 4. Extract with Python zipfile
$pyCode = @"
import zipfile, sys
with zipfile.ZipFile(r'$zipPath', 'r') as z:
    z.extractall(r'$testDir\out_py')
"@
python -c $pyCode
$pyText = (Get-FileHash (Get-ChildItem "$testDir\out_py" -Recurse -Filter "prose.txt").FullName -Algorithm SHA256).Hash
$pyShort = (Get-FileHash (Get-ChildItem "$testDir\out_py" -Recurse -Filter "small.ini").FullName -Algorithm SHA256).Hash
$pyRnd = (Get-FileHash (Get-ChildItem "$testDir\out_py" -Recurse -Filter "random.bin").FullName -Algorithm SHA256).Hash

$pyMatch = ($pyText -eq $hashText) -and ($pyShort -eq $hashShort) -and ($pyRnd -eq $hashRnd)
Write-Host ("Python zipfile 100% Match: {0}" -f $pyMatch) -ForegroundColor $(if ($pyMatch) { 'Green' } else { 'Red' })

# 5. Extract with tar.exe
& tar.exe -xf $zipPath -C "$testDir\out_tar"
$tarText = (Get-FileHash (Get-ChildItem "$testDir\out_tar" -Recurse -Filter "prose.txt").FullName -Algorithm SHA256).Hash
$tarShort = (Get-FileHash (Get-ChildItem "$testDir\out_tar" -Recurse -Filter "small.ini").FullName -Algorithm SHA256).Hash
$tarRnd = (Get-FileHash (Get-ChildItem "$testDir\out_tar" -Recurse -Filter "random.bin").FullName -Algorithm SHA256).Hash

$tarMatch = ($tarText -eq $hashText) -and ($tarShort -eq $hashShort) -and ($tarRnd -eq $hashRnd)
Write-Host ("Windows tar.exe 100% Match: {0}" -f $tarMatch) -ForegroundColor $(if ($tarMatch) { 'Green' } else { 'Red' })

# 6. Extract using Lodino.exe unzip
& build\Lodino.exe unzip "$zipPath" "$testDir\out_lodino"
$lodText = (Get-FileHash (Get-ChildItem "$testDir\out_lodino" -Recurse -Filter "prose.txt").FullName -Algorithm SHA256).Hash
$lodShort = (Get-FileHash (Get-ChildItem "$testDir\out_lodino" -Recurse -Filter "small.ini").FullName -Algorithm SHA256).Hash
$lodRnd = (Get-FileHash (Get-ChildItem "$testDir\out_lodino" -Recurse -Filter "random.bin").FullName -Algorithm SHA256).Hash

$lodMatch = ($lodText -eq $hashText) -and ($lodShort -eq $hashShort) -and ($lodRnd -eq $hashRnd)
Write-Host ("Lodino.exe unzip 100% Match: {0}" -f $lodMatch) -ForegroundColor $(if ($lodMatch) { 'Green' } else { 'Red' })
