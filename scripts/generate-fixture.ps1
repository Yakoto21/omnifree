$ErrorActionPreference = "Stop"
$coreDll = Resolve-Path "build\Lodino.Core.dll"
[Reflection.Assembly]::LoadFile($coreDll.Path) | Out-Null

$pb = New-Object Lodino.Core.Formats.Image.PixelBuffer(64, 48)
$pb.Fill([Lodino.Core.Formats.Image.PixelColor]::Cyan)
$bytes = [Lodino.Core.Formats.Image.BmpEncoder]::Encode($pb, [uint16]24)

$fixtureDir = "tests\fixtures\image"
if (-not (Test-Path $fixtureDir)) {
    New-Item -ItemType Directory -Path $fixtureDir -Force | Out-Null
}

[System.IO.File]::WriteAllBytes((Join-Path $fixtureDir "sample.bmp"), $bytes)
Write-Host "Arquivo sample.bmp gerado com sucesso!"
