<#
.SYNOPSIS
Создаёт копии изображений с шириной и высотой, уменьшенными на 50 %.

.EXAMPLE
.\ResizeImagesHalf.ps1 -Source "C:\Art\Original" -Destination "C:\Art\HalfSize"

.EXAMPLE
.\ResizeImagesHalf.ps1 -Source .\Assets -Destination .\Assets_Half -Quality 75 -Overwrite
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Source,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Destination,

    # Качество применяется только к JPEG. PNG сохраняется без потерь.
    [ValidateRange(1, 100)]
    [int]$Quality = 85,

    [switch]$Overwrite
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
    throw "Исходная папка не найдена: $Source"
}

$sourcePath = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Source).Path).TrimEnd('\', '/')
$destinationPath = [IO.Path]::GetFullPath($Destination).TrimEnd('\', '/')

if ($sourcePath -eq $destinationPath) {
    throw 'Папка назначения должна отличаться от исходной: оригиналы не перезаписываются.'
}

New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
$destinationPrefix = $destinationPath + [IO.Path]::DirectorySeparatorChar

$jpegCodec = [Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
    Where-Object { $_.MimeType -eq 'image/jpeg' } |
    Select-Object -First 1

function Save-ResizedImage {
    param(
        [Parameter(Mandatory = $true)][string]$InputFile,
        [Parameter(Mandatory = $true)][string]$OutputFile
    )

    $sourceImage = $null
    $canvas = $null
    $graphics = $null
    $encoderParameters = $null

    try {
        $sourceImage = [Drawing.Image]::FromFile($InputFile)
        $newWidth = [Math]::Max(1, [int][Math]::Round($sourceImage.Width / 2.0))
        $newHeight = [Math]::Max(1, [int][Math]::Round($sourceImage.Height / 2.0))

        $canvas = New-Object Drawing.Bitmap $newWidth, $newHeight
        $canvas.SetResolution($sourceImage.HorizontalResolution, $sourceImage.VerticalResolution)
        $graphics = [Drawing.Graphics]::FromImage($canvas)
        $graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.DrawImage($sourceImage, 0, 0, $newWidth, $newHeight)

        $extension = [IO.Path]::GetExtension($OutputFile).ToLowerInvariant()
        if ($extension -in '.jpg', '.jpeg') {
            $encoderParameters = New-Object Drawing.Imaging.EncoderParameters 1
            $encoderParameters.Param[0] = New-Object Drawing.Imaging.EncoderParameter(
                [Drawing.Imaging.Encoder]::Quality,
                [long]$Quality
            )
            $canvas.Save($OutputFile, $jpegCodec, $encoderParameters)
        }
        else {
            # PNG и BMP остаются в исходном формате; PNG не получает дополнительных потерь качества.
            $canvas.Save($OutputFile)
        }
    }
    finally {
        if ($encoderParameters) { $encoderParameters.Dispose() }
        if ($graphics) { $graphics.Dispose() }
        if ($canvas) { $canvas.Dispose() }
        if ($sourceImage) { $sourceImage.Dispose() }
    }
}

$supportedExtensions = '.jpg', '.jpeg', '.png', '.bmp'
$files = Get-ChildItem -LiteralPath $sourcePath -File -Recurse | Where-Object {
    $_.Extension.ToLowerInvariant() -in $supportedExtensions -and
    -not $_.FullName.StartsWith($destinationPrefix, [StringComparison]::OrdinalIgnoreCase)
}

$created = 0
$skipped = 0
foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($sourcePath.Length).TrimStart('\', '/')
    $outputFile = Join-Path $destinationPath $relativePath
    $outputDirectory = Split-Path -Parent $outputFile
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

    if ((Test-Path -LiteralPath $outputFile) -and -not $Overwrite) {
        Write-Warning "Пропущен существующий файл: $outputFile"
        $skipped++
        continue
    }

    Save-ResizedImage -InputFile $file.FullName -OutputFile $outputFile
    Write-Host "Создан: $outputFile"
    $created++
}

Write-Host "Готово. Создано: $created; пропущено: $skipped."
