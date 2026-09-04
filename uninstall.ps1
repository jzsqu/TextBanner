# Uninstall TextBanner.
$ErrorActionPreference = "SilentlyContinue"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\TextBanner"

Stop-Process -Name TextBanner -Force

Remove-Item (Join-Path ([Environment]::GetFolderPath("Programs")) "TextBanner\TextBanner.lnk") -Force
Remove-Item (Join-Path ([Environment]::GetFolderPath("Desktop")) "TextBanner.lnk") -Force
Remove-Item "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\TextBanner" -Recurse -Force
Remove-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "TextBanner"
Remove-Item $installDir -Recurse -Force

Write-Host "TextBanner uninstalled."
