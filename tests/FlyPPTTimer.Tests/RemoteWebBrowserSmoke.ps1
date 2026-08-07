param(
    [Parameter(Mandatory = $true)]
    [string]$WebDirectory,
    [switch]$InstallBrowser
)

$ErrorActionPreference = 'Stop'
$source = (Resolve-Path $WebDirectory).Path
$session = 'flyppttimer-remote-' + [Guid]::NewGuid().ToString('N')
$testRoot = Join-Path ([IO.Path]::GetTempPath()) $session
$assets = Join-Path $testRoot 'assets'
$cliRoot = Join-Path $testRoot 'cli'
$server = $null

try {
    New-Item -ItemType Directory -Path $assets -Force | Out-Null
    Copy-Item (Join-Path $source 'index.html') (Join-Path $testRoot 'index.html')
    Copy-Item (Join-Path $source 'app.css') (Join-Path $assets 'app.css')
    Copy-Item (Join-Path $source 'app.js') (Join-Path $assets 'app.js')

    # Install the playwright package into an isolated prefix. We drive Chromium through the
    # stable playwright Node API directly (headless) instead of the @playwright/cli session
    # flag, which was environment-fragile on CI runners.
    $output = & npm.cmd install --prefix $cliRoot --no-fund --no-audit --loglevel=error '@playwright/cli@0.1.18' 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "npm install of @playwright/cli failed: $output" }

    if ($InstallBrowser) {
        $installOut = & node "$cliRoot/node_modules/playwright/cli.js" install chromium 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) { throw "playwright chromium install failed: $installOut" }
    }

    # Place the smoke script inside the prefix so the bare `playwright` import resolves locally.
    Copy-Item (Join-Path $PSScriptRoot 'RemoteWebBrowserSmoke.mjs') (Join-Path $cliRoot 'RemoteWebBrowserSmoke.mjs')

    $runOut = & node "$cliRoot/RemoteWebBrowserSmoke.mjs" --web $testRoot 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "Remote browser smoke failed: $runOut" }
    Write-Host $runOut
}
finally {
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
