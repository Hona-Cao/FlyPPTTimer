param(
    [string]$IsccPath = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$version = "1.5.0"
$artifacts = Join-Path $root "artifacts\release"
$portable = Join-Path $artifacts "FlyPPTTimer-v$version-portable-win-x64"
$installerSource = Join-Path $artifacts "installer-source"
$installerOutput = Join-Path $artifacts "installer-output"

Push-Location $root
try {
    cargo build --release
    if ($LASTEXITCODE -ne 0) {
        throw "cargo build --release failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

if (Test-Path -LiteralPath $artifacts) {
    Remove-Item -LiteralPath $artifacts -Recurse -Force
}
New-Item -ItemType Directory -Path $portable, $installerSource, $installerOutput | Out-Null

$files = [ordered]@{
    "FlyPPTTimer.exe" = Join-Path $root "target\release\FlyPPTTimer.exe"
    "FlyPPTTimer.config.json" = Join-Path $root "docs\default-config.json"
    "app.ico" = Join-Path $root "src\FlyPPTTimer\Assets\app.ico"
    "README.md" = Join-Path $root "README.md"
    "README.zh-CN.md" = Join-Path $root "README.zh-CN.md"
}
foreach ($item in $files.GetEnumerator()) {
    if (-not (Test-Path -LiteralPath $item.Value)) {
        throw "Release file is missing: $($item.Value)"
    }
    Copy-Item -LiteralPath $item.Value -Destination (Join-Path $portable $item.Key) -Force
    Copy-Item -LiteralPath $item.Value -Destination (Join-Path $installerSource $item.Key) -Force
}

$portableZip = Join-Path $artifacts "FlyPPTTimer-v$version-portable-win-x64.zip"
Compress-Archive -Path (Join-Path $portable "*") -DestinationPath $portableZip -CompressionLevel Optimal

if ([string]::IsNullOrWhiteSpace($IsccPath)) {
    $programFilesX86 = [Environment]::GetFolderPath("ProgramFilesX86")
    $candidates = @(
        "D:\APP\Inno Setup 6\ISCC.exe",
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        (Join-Path $programFilesX86 "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )
    $IsccPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($IsccPath)) {
        $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
        if ($command) {
            $IsccPath = $command.Source
        }
    }
}
if ([string]::IsNullOrWhiteSpace($IsccPath) -or -not (Test-Path -LiteralPath $IsccPath)) {
    throw "Inno Setup 6 compiler was not found."
}

$iss = Join-Path $root "installer\FlyPPTTimer.iss"
& $IsccPath /Qp "/DSourceDir=$installerSource" "/DOutputDir=$installerOutput" "/DMyVersion=$version" $iss
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE"
}
$installer = Join-Path $installerOutput "FlyPPTTimer-v$version-setup-win-x64.exe"
if (-not (Test-Path -LiteralPath $installer)) {
    throw "Inno Setup did not create the expected installer."
}
$finalInstaller = Join-Path $artifacts "FlyPPTTimer-v$version-setup-win-x64.exe"
Copy-Item -LiteralPath $installer -Destination $finalInstaller -Force

Get-Item -LiteralPath (Join-Path $root "target\release\FlyPPTTimer.exe"), $portableZip, $finalInstaller |
    Select-Object FullName, Length
