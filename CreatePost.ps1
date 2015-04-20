param(
    [Parameter(Mandatory=$true)]
    [string]$title
)

$date = Get-Date -format "yyyy-MM-dd"
$name = $date + "-" + $title.ToLowerInvariant().Replace(' ', '-')
$postPath = "_posts/" + $name + ".md"
$assetsPath = "assets/" + $name + "/"
$postContents = @"
---
title: $title
assets: /$assetsPath
tags: [ ]
---
Example of referencing an asset (in this case, an image):

![Example]({{ page.assets }}example.png "Example")
"@

Write-Host "Creating post..."
New-Item $postPath -type file -value $postContents
Write-Host "...done"

Write-Host "Creating assets directory..."
New-Item $assetsPath -type directory
Write-Host "...done"