param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\AstralAtelier\resource\ui')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskUiRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
$taskTexturePaths = [System.Collections.Generic.List[string]]::new()

function New-UiColor([double]$r, [double]$g, [double]$b, [double]$a = 1) {
    return [System.Drawing.Color]::FromArgb(
        [int][Math]::Round($a * 255), [int][Math]::Round($r * 255),
        [int][Math]::Round($g * 255), [int][Math]::Round($b * 255))
}

function Save-UiTexture([string]$RelativePath, [System.Drawing.Color]$FillColor,
    [System.Drawing.Color]$BorderColor = [System.Drawing.Color]::Transparent, [single]$BorderWidth = 0) {
    $taskDestination = Join-Path $taskUiRoot $RelativePath
    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($taskDestination)) | Out-Null
    $taskBitmap = [System.Drawing.Bitmap]::new(24, 24)
    $taskGraphics = [System.Drawing.Graphics]::FromImage($taskBitmap)
    try {
        $taskGraphics.Clear($FillColor)
        if ($BorderWidth -gt 0) {
            $taskPen = [System.Drawing.Pen]::new($BorderColor, $BorderWidth)
            try {
                $taskInset = $BorderWidth / 2
                $taskGraphics.DrawRectangle($taskPen, $taskInset, $taskInset,
                    [single](24 - $BorderWidth), [single](24 - $BorderWidth))
            } finally { $taskPen.Dispose() }
        }
        $taskBitmap.Save($taskDestination, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $taskGraphics.Dispose()
        $taskBitmap.Dispose()
    }
    $taskTexturePaths.Add($RelativePath.Replace('\', '/'))
}

$taskPanel = New-UiColor .06 .07 .14 .94
$taskHover = New-UiColor .17 .19 .3
$taskAccentColors = [ordered]@{
    white = (New-UiColor .97 .96 1)
    muted = (New-UiColor .74 .76 .85)
    gold = (New-UiColor 1 .83 .44)
    pink = (New-UiColor 1 .35 .61)
    blue = (New-UiColor .37 .76 1)
    green = (New-UiColor .58 .9 .47)
}

Save-UiTexture 'panels/panel_dark.png' $taskPanel
Save-UiTexture 'panels/academy_overlay.png' (New-UiColor .03 .04 .1 .32)
Save-UiTexture 'panels/black.png' ([System.Drawing.Color]::Black)
Save-UiTexture 'hp/empty.png' (New-UiColor .08 .08 .12)
Save-UiTexture 'hp/player.png' $taskAccentColors.green
Save-UiTexture 'hp/enemy.png' $taskAccentColors.pink

foreach ($taskAccentName in $taskAccentColors.Keys) {
    $taskAccent = $taskAccentColors[$taskAccentName]
    Save-UiTexture "buttons/button_$taskAccentName.png" $taskPanel $taskAccent 1.5
    Save-UiTexture "buttons/button_${taskAccentName}_hover.png" $taskHover $taskAccent 3
    foreach ($taskWidthEntry in @(@('1', 1), @('15', 1.5), @('2', 2), @('3', 3))) {
        Save-UiTexture "frames/${taskAccentName}_$($taskWidthEntry[0]).png" `
            ([System.Drawing.Color]::Transparent) $taskAccent ([single]$taskWidthEntry[1])
    }
}

$taskManifest = [ordered]@{
    format = 'PNG'
    tileSize = 24
    nineSliceCorner = 8
    description = 'Runtime UI textures. Buttons and frames use nine-slice drawing; HP fills stretch.'
    textures = $taskTexturePaths.ToArray()
}
$taskManifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $taskUiRoot 'assets.json') -Encoding utf8
Write-Output ("Exported {0} UI textures to {1}" -f $taskTexturePaths.Count, $taskUiRoot)
