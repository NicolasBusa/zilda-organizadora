Add-Type -AssemblyName System.Drawing
$taskBitmap = New-Object System.Drawing.Bitmap 128,128
$taskGraphics = [System.Drawing.Graphics]::FromImage($taskBitmap)
$taskGraphics.SmoothingMode = 'AntiAlias'
$taskGraphics.Clear([System.Drawing.Color]::Transparent)
$taskPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$taskPath.AddArc(4,4,38,38,180,90)
$taskPath.AddArc(86,4,38,38,270,90)
$taskPath.AddArc(86,86,38,38,0,90)
$taskPath.AddArc(4,86,38,38,90,90)
$taskPath.CloseFigure()
$taskBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush ([System.Drawing.Point]::new(0,0)),([System.Drawing.Point]::new(128,128)),([System.Drawing.Color]::FromArgb(186,164,255)),([System.Drawing.Color]::FromArgb(117,84,224))
$taskGraphics.FillPath($taskBrush,$taskPath)
$taskFont = New-Object System.Drawing.Font 'Segoe UI',64,([System.Drawing.FontStyle]::Bold),([System.Drawing.GraphicsUnit]::Pixel)
$taskFormat = New-Object System.Drawing.StringFormat
$taskFormat.Alignment = 'Center'
$taskFormat.LineAlignment = 'Center'
$taskGraphics.DrawString('Z',$taskFont,[System.Drawing.Brushes]::White,([System.Drawing.RectangleF]::new(0,-1,128,128)),$taskFormat)
$taskIcon = [System.Drawing.Icon]::FromHandle($taskBitmap.GetHicon())
$taskStream = [System.IO.File]::Create((Join-Path $PSScriptRoot 'zilda.ico'))
$taskIcon.Save($taskStream)
$taskStream.Dispose()
$taskIcon.Dispose()
$taskGraphics.Dispose()
$taskBitmap.Dispose()
$taskBrush.Dispose()
$taskPath.Dispose()
$taskFont.Dispose()
$taskFormat.Dispose()
