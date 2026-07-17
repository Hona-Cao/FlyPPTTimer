$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$root = $PSScriptRoot
$dotnet = Join-Path $root ".dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
$project = Join-Path $root "src\FlyPPTTimer\FlyPPTTimer.csproj"
[xml]$projectXml = Get-Content -LiteralPath $project -Raw
$version = ([string]$projectXml.Project.PropertyGroup.Version).Trim()
$dist = Join-Path $root "dist\v$version"
if (Test-Path -LiteralPath $dist) { throw "Versioned dist directory already exists and will not be overwritten: $dist" }
$null = New-Item -ItemType Directory -Path $dist
$log = Join-Path $root "build.log"
"Build started: $(Get-Date -Format o)" | Tee-Object -FilePath $log -Append
& $dotnet --info 2>&1 | Tee-Object -FilePath $log -Append
& $dotnet build $project -c Release -p:NuGetAudit=false 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
& $dotnet publish $project -c Release -r win-x64 --self-contained true -p:NuGetAudit=false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=false -o $dist 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
Copy-Item -LiteralPath (Join-Path $root "README.md") -Destination (Join-Path $dist "README.md")
Copy-Item -LiteralPath (Join-Path $root "docs\default-config.json") -Destination (Join-Path $dist "FlyPPTTimer.config.json")
Copy-Item -LiteralPath (Join-Path $root "src\FlyPPTTimer\Assets\app.ico") -Destination (Join-Path $dist "app.ico")
"Build finished: $(Get-Date -Format o)" | Tee-Object -FilePath $log -Append
