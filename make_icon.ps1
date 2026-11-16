# Generate EyeRest.ico (eye on dark rounded square), 8 sizes 16..256.
# Part of P-EyeRest. SPDX-License-Identifier: GPL-3.0-or-later (see LICENSE).
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File make_icon.ps1 -Out EyeRest.ico [-PreviewPng preview.png]
param([Parameter(Mandatory = $true)][string]$Out, [string]$PreviewPng)
Add-Type -AssemblyName System.Drawing
$sizes = 16, 20, 24, 32, 40, 48, 64, 256
$bgColor = [System.Drawing.Color]::FromArgb(22, 26, 34)

function New-Brush($a, $r, $g, $b) { New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb($a, $r, $g, $b)) }

function Draw-Icon([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)

    # rounded square background
    $r = [single]($s * 0.22); $d = 2 * $r; $m = [single]0
    $bg = New-Object System.Drawing.Drawing2D.GraphicsPath
    $bg.AddArc($m, $m, $d, $d, 180, 90); $bg.AddArc($s - $d, $m, $d, $d, 270, 90)
    $bg.AddArc($s - $d, $s - $d, $d, $d, 0, 90); $bg.AddArc($m, $s - $d, $d, $d, 90, 90); $bg.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush($bgColor)), $bg)

    # eye, centered
    $cx = $s / 2.0; $cy = $s / 2.0; $w = $s * 0.40; $h = $s * 0.25
    $eye = New-Object System.Drawing.Drawing2D.GraphicsPath
    $eye.AddBezier([single]($cx - $w), [single]$cy, [single]($cx - $w / 2), [single]($cy - $h * 1.3), [single]($cx + $w / 2), [single]($cy - $h * 1.3), [single]($cx + $w), [single]$cy)
    $eye.AddBezier([single]($cx + $w), [single]$cy, [single]($cx + $w / 2), [single]($cy + $h * 1.3), [single]($cx - $w / 2), [single]($cy + $h * 1.3), [single]($cx - $w), [single]$cy)
    $eye.CloseFigure()
    $g.FillPath((New-Brush 255 225 232 242), $eye)
    $ir = $s * 0.16; $pr = $s * 0.075
    $g.FillEllipse((New-Brush 255 40 110 170), [single]($cx - $ir), [single]($cy - $ir), [single](2 * $ir), [single](2 * $ir))
    $g.FillEllipse((New-Brush 255 15 20 30), [single]($cx - $pr), [single]($cy - $pr), [single](2 * $pr), [single](2 * $pr))
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(120, 200, 255), [single][Math]::Max(1.0, $s * 0.06))
    $pen.LineJoin = 'Round'
    $g.DrawPath($pen, $eye)

    $g.Dispose()
    return $bmp
}

# Sizes < 256: 32bpp DIB (works with System.Drawing.Icon); 256: PNG.
function Get-Entry([System.Drawing.Bitmap]$bmp) {
    $s = $bmp.Width
    $ms = New-Object System.IO.MemoryStream
    if ($s -ge 256) { $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png); return , $ms.ToArray() }
    $bw = New-Object System.IO.BinaryWriter($ms)
    $maskStride = [int]([Math]::Ceiling($s / 32.0) * 4)
    $bw.Write([int]40); $bw.Write([int]$s); $bw.Write([int]($s * 2)); $bw.Write([int16]1); $bw.Write([int16]32)
    $bw.Write([int]0); $bw.Write([int]($s * $s * 4 + $maskStride * $s)); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0)
    for ($y = $s - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $s; $x++) { $c = $bmp.GetPixel($x, $y); $bw.Write([byte]$c.B); $bw.Write([byte]$c.G); $bw.Write([byte]$c.R); $bw.Write([byte]$c.A) }
    }
    $bw.Write((New-Object byte[] ($maskStride * $s)))
    $bw.Flush()
    return , $ms.ToArray()
}

$entries = @()
foreach ($s in $sizes) {
    $b = Draw-Icon $s
    $entries += , @($s, (Get-Entry $b))
    if ($s -eq 256 -and $PreviewPng) { $b.Save($PreviewPng) }
    $b.Dispose()
}

# write to a temp file next to the target, then replace
$tmp = "$Out.$([guid]::NewGuid().ToString('N')).tmp"
$fs = [System.IO.File]::Create($tmp); $w = New-Object System.IO.BinaryWriter($fs)
$w.Write([int16]0); $w.Write([int16]1); $w.Write([int16]$entries.Count)
$offset = 6 + 16 * $entries.Count
foreach ($e in $entries) {
    $s = $e[0]; $data = $e[1]
    $dim = if ($s -ge 256) { 0 } else { $s }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([int16]1); $w.Write([int16]32); $w.Write([int]$data.Length); $w.Write([int]$offset)
    $offset += $data.Length
}
foreach ($e in $entries) { $w.Write([byte[]]$e[1]) }
$w.Close()
$check = New-Object System.Drawing.Icon($tmp, 16, 16); $check.Dispose()   # validate before replacing
if (Test-Path $Out) { [System.IO.File]::Replace($tmp, $Out, [NullString]::Value) } else { Move-Item $tmp $Out }
"wrote $Out ($((Get-Item $Out).Length) bytes, $($entries.Count) sizes)"
