$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$root = $PSScriptRoot
$dotnet = Join-Path $root ".dotnet\dotnet.exe"
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = "dotnet" }
$project = Join-Path $root "src\FlyPPTTimer.WinUI\FlyPPTTimer.WinUI.csproj"
$sdkRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
$kit = Get-ChildItem -LiteralPath $sdkRoot -Directory | Where-Object Name -Match '^\d+\.' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if ($null -eq $kit) { throw "Windows SDK tools were not found." }
$tools = Join-Path $kit.FullName "x64"
$makePri = Join-Path $tools "makepri.exe"
$env:Path = "$tools;$env:Path"
if (-not (Test-Path -LiteralPath $makePri)) { throw "makepri.exe was not found: $makePri" }

& $dotnet restore $project --configfile (Join-Path $root "NuGet.Config") -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw "WinUI restore failed." }
& $dotnet build $project -c Release -r win-x64 --no-restore -p:MakePriExeFullPath=$makePri
if ($LASTEXITCODE -ne 0) { throw "WinUI build failed." }
