# Crops a manufacturer's product picture for the hold-down catalog, locally: trims the plain background (the colour of the
# top-left pixel, white or light grey), leaves a small margin, fits it in 280 x 280 px and saves it as <Line>.png beside
# this script (HDUE.png, MSTC.png ...: the product line, as HoldownPictures.LineOf names it).
#   .\crop.ps1 -source C:\Downloads\mstc.jpg -line MSTC
# The app reads these files from its own folder (copied there by the build); it never loads a picture from the web.
param([Parameter(Mandatory)][string]$source, [Parameter(Mandatory)][string]$line, [int]$size = 280, [int]$tolerance = 28)
Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Bitmap]::FromFile((Resolve-Path $source))
try {
    $bg = $src.GetPixel(0, 0)
    function Differs($c) { [Math]::Abs($c.R - $bg.R) + [Math]::Abs($c.G - $bg.G) + [Math]::Abs($c.B - $bg.B) -gt $tolerance }
    $x0 = $src.Width; $y0 = $src.Height; $x1 = -1; $y1 = -1
    for ($y = 0; $y -lt $src.Height; $y += 2) {
        for ($x = 0; $x -lt $src.Width; $x += 2) {
            if (Differs $src.GetPixel($x, $y)) {
                if ($x -lt $x0) { $x0 = $x }; if ($x -gt $x1) { $x1 = $x }
                if ($y -lt $y0) { $y0 = $y }; if ($y -gt $y1) { $y1 = $y }
            }
        }
    }
    if ($x1 -lt 0) { throw "Nothing but background in $source" }
    $pad = 6
    $x0 = [Math]::Max(0, $x0 - $pad); $y0 = [Math]::Max(0, $y0 - $pad)
    $x1 = [Math]::Min($src.Width - 1, $x1 + $pad); $y1 = [Math]::Min($src.Height - 1, $y1 + $pad)
    $w = $x1 - $x0 + 1; $h = $y1 - $y0 + 1
    $k = [Math]::Min(1.0, [Math]::Min($size / $w, $size / $h))
    $ow = [int][Math]::Round($w * $k); $oh = [int][Math]::Round($h * $k)
    $out = New-Object System.Drawing.Bitmap $ow, $oh
    $g = [System.Drawing.Graphics]::FromImage($out)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::White)
    $g.DrawImage($src, (New-Object System.Drawing.Rectangle 0, 0, $ow, $oh), (New-Object System.Drawing.Rectangle $x0, $y0, $w, $h), [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
    $dest = Join-Path $PSScriptRoot ($line.ToUpperInvariant() + ".png")
    $out.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
    "saved $dest ($ow x $oh, from $w x $h at $x0,$y0)"
}
finally { $src.Dispose() }
