$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$root = $PSScriptRoot
$dotnet = Join-Path $root ".dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
$log = Join-Path $root "build.log"
"Build started: $(Get-Date -Format o)" | Tee-Object -FilePath $log -Append
& $dotnet --info 2>&1 | Tee-Object -FilePath $log -Append
& $dotnet build (Join-Path $root "src\FlyPPTTimer\FlyPPTTimer.csproj") -c Release 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
& $dotnet publish (Join-Path $root "src\FlyPPTTimer\FlyPPTTimer.csproj") -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o (Join-Path $root "dist") 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
Copy-Item -LiteralPath (Join-Path $root "README.md") -Destination (Join-Path $root "dist\README.md") -Force
Copy-Item -LiteralPath (Join-Path $root "docs\default-config.json") -Destination (Join-Path $root "dist\FlyPPTTimer.config.json") -Force
Copy-Item -LiteralPath (Join-Path $root "src\FlyPPTTimer\Assets\app.ico") -Destination (Join-Path $root "dist\app.ico") -Force
"Build finished: $(Get-Date -Format o)" | Tee-Object -FilePath $log -Append
