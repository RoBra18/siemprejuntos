# Code-faithful previews of GameDuoMobileInput.OnGUI, rendered over a gameplay asset.
# These are visual previews, not screenshots from a running phone.
Add-Type -AssemblyName System.Drawing
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$source = Join-Path $root 'Assets\GameDuo\TutorialFactory-preview.png'
$background = [System.Drawing.Image]::FromFile($source)

function Draw-Disc($graphics, [single]$x, [single]$y, [single]$radius, $color) {
    $brush = [System.Drawing.SolidBrush]::new($color)
    $graphics.FillEllipse($brush, $x-$radius, $y-$radius, $radius*2, $radius*2)
    $brush.Dispose()
}

function Draw-Button($graphics, [single]$x, [single]$y, [single]$radius, [string]$label, $color) {
    Draw-Disc $graphics $x $y $radius $color
    $font = [System.Drawing.Font]::new('Arial', 16, [System.Drawing.FontStyle]::Bold)
    $format = [System.Drawing.StringFormat]::new()
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Black)
    $graphics.DrawString($label, $font, $brush, [System.Drawing.RectangleF]::new($x-$radius, $y-$radius, $radius*2, $radius*2), $format)
    $brush.Dispose(); $font.Dispose(); $format.Dispose()
}

function Draw-Player($graphics, [single]$stick, [single]$action, [single]$dash, $color) {
    Draw-Disc $graphics $stick 424 77 ([System.Drawing.Color]::FromArgb(128, 20, 18, 38))
    Draw-Disc $graphics $stick 424 31 $color
    Draw-Button $graphics $action 425 55 'SALTO' $color
    Draw-Button $graphics $action 300 46 'ULTI' $color
    Draw-Button $graphics $dash 300 40 'DASH' $color
    Draw-Button $graphics $stick 303 39 'BAJAR' $color
}

function Save-Preview([string]$name, [bool]$guest) {
    $bitmap = [System.Drawing.Bitmap]::new(1920, 1080, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.DrawImage($background, 0, 0, 1920, 1080)
    $graphics.ScaleTransform(2, 2)
    $pink = [System.Drawing.Color]::FromArgb(191, 255, 115, 166)
    $mint = [System.Drawing.Color]::FromArgb(191, 128, 255, 179)
    if ($guest) {
        Draw-Player $graphics 240 730 480 $mint
    } else {
        Draw-Player $graphics 112 326 220 $pink
        Draw-Player $graphics 848 634 740 $mint
    }
    $bitmap.Save((Join-Path $PSScriptRoot $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $bitmap.Dispose()
}

Save-Preview 'controles-dos-jugadores.png' $false
Save-Preview 'control-invitado.png' $true
$background.Dispose()
