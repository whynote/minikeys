$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RoundedPath([single]$x, [single]$y, [single]$width, [single]$height, [single]$radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = 2 * $radius
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return ,$path
}

$sizes = @(16, 20, 24, 32, 48, 64, 128, 256)
$frames = @()
$blue = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#2563eb'))
$white = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#f8fafc'))
$border = New-Object System.Drawing.Pen ([System.Drawing.ColorTranslator]::FromHtml('#1d4ed8')), 2
try {
    foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap $size, $size
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $stream = New-Object System.IO.MemoryStream
        try {
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.ScaleTransform($size / 64.0, $size / 64.0)
            $path = New-RoundedPath 2 2 60 60 14
            try { $graphics.FillPath($blue, $path); $graphics.DrawPath($border, $path) } finally { $path.Dispose() }
            $path = New-RoundedPath 10 23 44 26 5
            try { $graphics.FillPath($white, $path) } finally { $path.Dispose() }
            foreach ($x in @(16, 28, 40)) {
                $path = New-RoundedPath $x 28 8 6 1.4
                try { $graphics.FillPath($blue, $path) } finally { $path.Dispose() }
            }
            $path = New-RoundedPath 18 39 28 5 2
            try { $graphics.FillPath($blue, $path) } finally { $path.Dispose() }
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $frames += ,$stream.ToArray()
            if ($size -eq 256) { $bitmap.Save((Join-Path $PSScriptRoot 'minikeys.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
        } finally { $stream.Dispose(); $graphics.Dispose(); $bitmap.Dispose() }
    }
} finally { $border.Dispose(); $white.Dispose(); $blue.Dispose() }

$file = [System.IO.File]::Create((Join-Path $PSScriptRoot 'minikeys.ico'))
$writer = New-Object System.IO.BinaryWriter $file
try {
    $writer.Write([UInt16]0); $writer.Write([UInt16]1); $writer.Write([UInt16]$sizes.Length)
    $offset = 6 + 16 * $sizes.Length
    for ($i = 0; $i -lt $sizes.Length; $i++) {
        $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
        $writer.Write([Byte]$dimension); $writer.Write([Byte]$dimension)
        $writer.Write([Byte]0); $writer.Write([Byte]0)
        $writer.Write([UInt16]1); $writer.Write([UInt16]32)
        $writer.Write([UInt32]$frames[$i].Length); $writer.Write([UInt32]$offset)
        $offset += $frames[$i].Length
    }
    foreach ($frame in $frames) { $writer.Write([Byte[]]$frame) }
} finally { $writer.Dispose(); $file.Dispose() }
