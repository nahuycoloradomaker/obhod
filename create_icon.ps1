Add-Type -AssemblyName System.Drawing

function Make-Bitmap([int]$sz) {
    $bmp = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = $sz / 24.0
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [float](2.5 * $s))
    $pen.StartCap = 'Round'
    $pen.EndCap = 'Round'

    # Центр (12, 12), радиус 10
    # Рисуем дугу (аналог GeoLogo)
    # DrawArc(pen, x, y, width, height, startAngle, sweepAngle)
    $g.DrawArc($pen, [float](2 * $s), [float](2 * $s), [float](20 * $s), [float](20 * $s), -90, -300)
    
    # Линия из центра (12,12) в (22,2)
    $g.DrawLine($pen, [float](12 * $s), [float](12 * $s), [float](22 * $s), [float](2 * $s))

    $pen.Dispose()
    $g.Dispose()
    return $bmp
}

$sizes = @(16, 32, 48, 256)
$pngData = @{}

foreach ($sz in $sizes) {
    $bmp = Make-Bitmap $sz
    if ($sz -eq 256) {
        $bmp.Save((Join-Path $PSScriptRoot "Resources\icon.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngData[$sz] = $ms.ToArray()
    $ms.Dispose()
    $bmp.Dispose()
}

$outMs = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter($outMs)

$w.Write([uint16]0)
$w.Write([uint16]1)
$w.Write([uint16]$sizes.Count)

$offset = 6 + $sizes.Count * 16
foreach ($sz in $sizes) {
    $dim = if ($sz -ge 256) { 0 } else { $sz }
    $w.Write([byte]$dim)
    $w.Write([byte]$dim)
    $w.Write([byte]0)
    $w.Write([byte]0)
    $w.Write([uint16]1)
    $w.Write([uint16]32)
    $w.Write([uint32]$pngData[$sz].Length)
    $w.Write([uint32]$offset)
    $offset += $pngData[$sz].Length
}
foreach ($sz in $sizes) { $w.Write($pngData[$sz]) }
$w.Flush()

$out = Join-Path $PSScriptRoot 'Resources\icon.ico'
[IO.File]::WriteAllBytes($out, $outMs.ToArray())
$outMs.Dispose()

Write-Host "icon.ico -> $out" -ForegroundColor Cyan
