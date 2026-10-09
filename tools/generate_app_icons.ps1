# Regenerates the runtime PNG and the multi-resolution Windows ICO from the source artwork.
Add-Type -AssemblyName System.Drawing.Common

$root = Split-Path -Parent $PSScriptRoot
$assetDirectory = Join-Path $root 'AvaWeather/Assets'
$source = [System.Drawing.Bitmap]::new((Join-Path $assetDirectory 'app-icon-source.png'))
try {
    $icon = [System.Drawing.Bitmap]::new(1024, 1024, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($icon)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.DrawImage($source, [System.Drawing.Rectangle]::new(0, 0, 1024, 1024),
                [System.Drawing.Rectangle]::new(100, 110, 1050, 1050), [System.Drawing.GraphicsUnit]::Pixel)
            # The generated artwork has a few stray pixels below the rounded tile.
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.FillRectangle([System.Drawing.Brushes]::Transparent, 0, 994, 1024, 30)
        }
        finally { $graphics.Dispose() }

        $icon.Save((Join-Path $assetDirectory 'app-icon.png'), [System.Drawing.Imaging.ImageFormat]::Png)

        $sizes = @(16, 24, 32, 48, 64, 128, 256)
        $images = foreach ($size in $sizes) {
            $scaled = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            try {
                $drawing = [System.Drawing.Graphics]::FromImage($scaled)
                try {
                    $drawing.Clear([System.Drawing.Color]::Transparent)
                    $drawing.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                    $drawing.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                    $drawing.DrawImage($icon, 0, 0, $size, $size)
                }
                finally { $drawing.Dispose() }
                $memory = [System.IO.MemoryStream]::new()
                try {
                    $scaled.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
                    ,$memory.ToArray()
                }
                finally { $memory.Dispose() }
            }
            finally { $scaled.Dispose() }
        }

        $stream = [System.IO.File]::Create((Join-Path $assetDirectory 'app-icon.ico'))
        $writer = [System.IO.BinaryWriter]::new($stream)
        try {
            $writer.Write([uint16]0)
            $writer.Write([uint16]1)
            $writer.Write([uint16]$sizes.Count)
            $offset = 6 + 16 * $sizes.Count
            for ($index = 0; $index -lt $sizes.Count; $index++) {
                $dimension = if ($sizes[$index] -eq 256) { 0 } else { $sizes[$index] }
                $writer.Write([byte]$dimension)
                $writer.Write([byte]$dimension)
                $writer.Write([byte]0)
                $writer.Write([byte]0)
                $writer.Write([uint16]1)
                $writer.Write([uint16]32)
                $writer.Write([uint32]$images[$index].Length)
                $writer.Write([uint32]$offset)
                $offset += $images[$index].Length
            }
            foreach ($image in $images) { $writer.Write([byte[]]$image) }
        }
        finally { $writer.Dispose() }
    }
    finally { $icon.Dispose() }
}
finally { $source.Dispose() }
