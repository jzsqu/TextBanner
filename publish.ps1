param(
    [switch]$SelfContained,
    [switch]$Zip,
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj = Join-Path $root "TextBanner.csproj"
$out  = Join-Path $root "publish"

$publishArgs = @("publish", $proj, "-c", "Release", "-o", $out)
if ($SelfContained) {
    $publishArgs += @("-r", $Runtime, "--self-contained", "true")
}

Write-Host "Publishing TextBanner..." -ForegroundColor Cyan
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host ""
Write-Host "Done. Output: $out" -ForegroundColor Green
Write-Host "Executable: $out\TextBanner.exe" -ForegroundColor Green

if ($Zip) {
    $zipPath = Join-Path $root "TextBanner.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    $items = @($out)
    if (Test-Path (Join-Path $root "browser-extension")) { $items += (Join-Path $root "browser-extension") }
    if (Test-Path (Join-Path $root "使用说明.md")) { $items += (Join-Path $root "使用说明.md") }
    Compress-Archive -Path $items -DestinationPath $zipPath
    Write-Host "Zip: $zipPath" -ForegroundColor Green
}
