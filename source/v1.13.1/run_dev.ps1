$ErrorActionPreference = "Stop"
$dotnet = Join-Path $PSScriptRoot ".dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
& $dotnet run --project (Join-Path $PSScriptRoot "src\FlyPPTTimer\FlyPPTTimer.csproj")
