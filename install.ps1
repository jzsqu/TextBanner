# Install TextBanner for the current user (no admin required).
# Requires a self-contained publish first:  .\publish.ps1 -SelfContained
param([string]$Source = "publish")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root $Source
$exe = Join-Path $src "TextBanner.exe"
if (-not (Test-Path $exe)) { throw "TextBanner.exe not found in $src. Run .\publish.ps1 -SelfContained first." }

$installDir = Join-Path $env:LOCALAPPDATA "Programs\TextBanner"
$startMenu  = Join-Path ([Environment]::GetFolderPath("Programs")) "TextBanner"
$desktop    = [Environment]::GetFolderPath("Desktop")
$ico        = Join-Path $src "app.ico"

Write-Host "Installing to $installDir ..."

# 1) Copy files
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item -Path (Join-Path $src "*") -Destination $installDir -Recurse -Force

# 2) Shortcuts (Start Menu + Desktop)
New-Item -ItemType Directory -Force -Path $startMenu | Out-Null
$wsh = New-Object -ComObject WScript.Shell
foreach ($lnk in @((Join-Path $startMenu "TextBanner.lnk"), (Join-Path $desktop "TextBanner.lnk"))) {
    $sc = $wsh.CreateShortcut($lnk)
    $sc.TargetPath = (Join-Path $installDir "TextBanner.exe")
    $sc.WorkingDirectory = $installDir
    $sc.IconLocation = (Join-Path $installDir "app.ico")
    $sc.Description = "TextBanner"
    $sc.Save()
}

# 3) Register uninstaller
Copy-Item (Join-Path $root "uninstall.ps1") (Join-Path $installDir "uninstall.ps1") -Force
$uk = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\TextBanner"
New-Item -Path $uk -Force | Out-Null
Set-ItemProperty -Path $uk -Name "DisplayName"     -Value "TextBanner"
Set-ItemProperty -Path $uk -Name "DisplayIcon"     -Value (Join-Path $installDir "app.ico")
Set-ItemProperty -Path $uk -Name "DisplayVersion"  -Value "1.0.0"
Set-ItemProperty -Path $uk -Name "Publisher"       -Value "TextBanner"
Set-ItemProperty -Path $uk -Name "InstallLocation" -Value $installDir
Set-ItemProperty -Path $uk -Name "UninstallString" -Value "powershell -NoProfile -ExecutionPolicy Bypass -File `"$installDir\uninstall.ps1`""
Set-ItemProperty -Path $uk -Name "NoModify"        -Value 1
Set-ItemProperty -Path $uk -Name "NoRepair"        -Value 1

Write-Host "Done. Start menu / desktop shortcuts created." -ForegroundColor Green
Write-Host "Run uninstall.ps1 (in $installDir) to remove." -ForegroundColor Green
