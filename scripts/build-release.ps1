param(
    [string]$ApplicationDirectory = "",
    [string]$IsccPath = ""
)
# Build normally, or package an explicitly supplied, already validated application.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$version = [regex]::Match((Get-Content (Join-Path $root "Cargo.toml") -Raw), '(?m)^version = "([^"]+)"').Groups[1].Value
if (-not $version) { throw "Package version missing from Cargo.toml" }

if (-not $ApplicationDirectory) {
    Push-Location $root
    try {
        cargo build --locked --release
        if ($LASTEXITCODE -ne 0) { throw "Release build failed" }
    } finally { Pop-Location }
    $ApplicationDirectory = Join-Path $root "target\release"
}
$ApplicationDirectory = (Resolve-Path $ApplicationDirectory).Path
$exe = Join-Path $ApplicationDirectory "FlyPPTTimer.exe"
if ((Get-Item $exe).VersionInfo.ProductVersion -ne $version) {
    throw "Application executable does not match package version $version"
}

$out = Join-Path $root "artifacts\release\v$version"
$stage = Join-Path $out "portable"
$setupOut = Join-Path $out "installer-output"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force $stage,$setupOut | Out-Null
Copy-Item $exe $stage
Copy-Item (Join-Path $root "docs\v1131-default-config.json") (Join-Path $stage "FlyPPTTimer.config.json")
Copy-Item (Join-Path $root "src\FlyPPTTimer\Assets\app.ico") $stage
foreach ($file in @("README.md","README.zh-CN.md","LICENSE","CHANGELOG.md","CONTRIBUTING.md")) {
    Copy-Item (Join-Path $root $file) $stage
}
foreach ($dll in @("vcruntime140.dll","vcruntime140_1.dll","msvcp140.dll")) {
    $source = Join-Path $ApplicationDirectory $dll
    if (-not (Test-Path $source)) { $source = Join-Path $env:WINDIR "System32\$dll" }
    Copy-Item $source $stage
}
$docs = Join-Path $stage "docs"
New-Item -ItemType Directory -Force $docs | Out-Null
foreach ($file in @("USER_GUIDE.en.md","USER_GUIDE.zh-CN.md","BUILDING.md","DEVELOPMENT_HISTORY.md","RELEASE_NOTES_v1.13.1.md","development-commits.tsv")) {
    Copy-Item (Join-Path $root "docs\$file") $docs
}
New-Item -ItemType Directory -Force (Join-Path $docs "v1") | Out-Null
Copy-Item (Join-Path $root "docs\v1\*.md") (Join-Path $docs "v1")
New-Item -ItemType Directory -Force (Join-Path $docs "media") | Out-Null
Copy-Item (Join-Path $root "docs\media\v1.13.1") (Join-Path $docs "media") -Recurse
foreach ($file in @("donate-alipay.jpg","donate-wechat.png")) {
    Copy-Item (Join-Path $root "docs\media\$file") (Join-Path $docs "media")
}
# Preserve the icon's README-relative path for offline documentation.
$assets = Join-Path $stage "src\FlyPPTTimer\Assets"
New-Item -ItemType Directory -Force $assets | Out-Null
Copy-Item (Join-Path $root "src\FlyPPTTimer\Assets\app.png") $assets
$sourceSha = if ($env:PRODUCT_SOURCE_SHA) { $env:PRODUCT_SOURCE_SHA } else { (git -C $root rev-parse HEAD).Trim() }
@(
    "FlyPPTTimer v$version"
    "Executable source: $sourceSha"
    "Release documentation: https://github.com/Hona-Cao/FlyPPTTimer/releases/tag/v$version"
    "Both editions contain the same application executable."
    "Settings and imported alert sounds are local. Back up personal configuration before upgrading."
    "Read README.md or README.zh-CN.md and docs/USER_GUIDE.*.md."
) | Set-Content (Join-Path $stage "BUILD.txt") -Encoding utf8

$portableZip = Join-Path $out "FlyPPTTimer-v$version-portable-win-x64.zip"
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $portableZip -CompressionLevel Optimal -Force

if (-not $IsccPath) {
    $candidate = Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"
    $IsccPath = if (Test-Path $candidate) { $candidate } else { (Get-Command ISCC.exe -ErrorAction Stop).Source }
}
& $IsccPath /Qp "/DSourceDir=$stage" "/DOutputDir=$setupOut" "/DMyVersion=$version" (Join-Path $root "installer\FlyPPTTimer.iss")
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }
$installer = Join-Path $setupOut "FlyPPTTimer-v$version-setup-win-x64.exe"
$setupZip = Join-Path $out "FlyPPTTimer-v$version-setup-win-x64.zip"
Compress-Archive -LiteralPath $installer -DestinationPath $setupZip -CompressionLevel Optimal -Force
Get-Item $portableZip,$setupZip | Select-Object FullName,Length
