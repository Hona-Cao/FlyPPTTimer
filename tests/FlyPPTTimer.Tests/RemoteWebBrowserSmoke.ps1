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
$server = $null

function Invoke-PlaywrightCli {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & npx.cmd --yes --package '@playwright/cli@0.1.18' playwright-cli "-s=$session" @Arguments 2>&1 | Out-String
    }
    finally { $ErrorActionPreference = $previousErrorAction }
    if ($LASTEXITCODE -ne 0) { throw "Playwright CLI failed: $output" }
    return $output
}

function Wait-ForPort {
    param([int]$Port)
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($watch.Elapsed.TotalSeconds -lt 10) {
        $client = [Net.Sockets.TcpClient]::new()
        try {
            $client.Connect('127.0.0.1', $Port)
            return
        } catch { Start-Sleep -Milliseconds 100 }
        finally { $client.Dispose() }
    }
    throw "Static browser test server did not listen on port $Port."
}

try {
    New-Item -ItemType Directory -Path $assets -Force | Out-Null
    Copy-Item (Join-Path $source 'index.html') (Join-Path $testRoot 'index.html')
    Copy-Item (Join-Path $source 'app.css') (Join-Path $assets 'app.css')
    Copy-Item (Join-Path $source 'app.js') (Join-Path $assets 'app.js')

    $probe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $probe.Start()
    $port = ([Net.IPEndPoint]$probe.LocalEndpoint).Port
    $probe.Stop()
    $stdout = Join-Path $testRoot 'server.stdout.log'
    $stderr = Join-Path $testRoot 'server.stderr.log'
    $server = Start-Process python -ArgumentList @('-m', 'http.server', $port, '--bind', '127.0.0.1') `
        -WorkingDirectory $testRoot -WindowStyle Hidden -RedirectStandardOutput $stdout `
        -RedirectStandardError $stderr -PassThru
    Wait-ForPort $port

    if ($InstallBrowser) { Invoke-PlaywrightCli install-browser chromium | Out-Null }
    Invoke-PlaywrightCli open "http://127.0.0.1:$port/index.html?token=browser-test" | Out-Null

    $state = '{"ok":true,"message":"","timerState":{"mode":"\u5012\u8ba1\u65f6","state":"\u505c\u6b62","running":false,"durationMs":480000,"elapsedMs":0,"remainingMs":480000,"displayText":"08:00","isOvertime":false,"continueOvertime":true,"windowVisible":true,"muted":false,"timeUpBlackoutActive":false,"ruleCount":1},"presentationState":{"powerPointInstalled":true,"powerPointRunning":true,"hasPresentation":true,"isSlideShowRunning":true,"presentationName":"browser-fixture.pptx","presentationPath":"C:/fixture/browser-fixture.pptx","currentSlide":2,"totalSlides":10,"screenMode":"\u6b63\u5e38","presentations":[{"id":"fixture-id","name":"browser-fixture.pptx","directory":"C:/fixture","isActive":true,"isOpen":true,"isManaged":true}]},"version":"4.0.0","connectedClients":1,"revision":7}'
    $command = '{"ok":true,"message":"\u547d\u4ee4\u5df2\u6267\u884c","timerState":{"mode":"\u5012\u8ba1\u65f6","state":"\u8fd0\u884c\u4e2d","running":true,"durationMs":480000,"elapsedMs":1000,"remainingMs":479000,"displayText":"07:59","isOvertime":false,"windowVisible":true,"muted":false,"timeUpBlackoutActive":false,"ruleCount":1},"presentationState":{"powerPointInstalled":true,"powerPointRunning":true,"hasPresentation":true,"isSlideShowRunning":true,"presentationName":"browser-fixture.pptx","currentSlide":3,"totalSlides":10,"screenMode":"\u6b63\u5e38","presentations":[]},"version":"4.0.0","revision":8}'
    # npx.cmd uses Windows batch argument parsing under both Windows PowerShell and pwsh.
    $stateArgument = $state.Replace('"', '\"')
    $commandArgument = $command.Replace('"', '\"')
    Invoke-PlaywrightCli route '**/state*' --content-type application/json --body $stateArgument | Out-Null
    Invoke-PlaywrightCli route '**/command*' --content-type application/json --body $commandArgument | Out-Null
    Invoke-PlaywrightCli reload | Out-Null
    Invoke-PlaywrightCli resize 390 844 | Out-Null
    Invoke-PlaywrightCli snapshot | Out-Null

    $interaction = Invoke-PlaywrightCli run-code "async page => { await page.locator('button[data-command=timer\\.start]').click(); await page.waitForTimeout(100); const message=await page.locator('#message').textContent(); await page.locator('[data-page=pptPage]').click(); return await page.evaluate(message => ({messageLength:message.length,presentationActive:document.querySelector('[data-page=pptPage]').classList.contains('active'),presentation:document.getElementById('pptName').textContent,width:innerWidth,clientWidth:document.documentElement.clientWidth,scrollWidth:document.documentElement.scrollWidth}),message); }"
    if ($interaction -notmatch '"messageLength":5' -or $interaction -notmatch '"presentationActive":true' `
        -or $interaction -notmatch 'browser-fixture.pptx' -or $interaction -notmatch '"scrollWidth":390') {
        throw "Remote browser command/layout validation failed: $interaction"
    }

    $gesture = Invoke-PlaywrightCli run-code "async page => { return await page.evaluate(async () => { const v=document.getElementById('pagesViewport'); const swipe=(from,to)=>{const make=x=>new Touch({identifier:1,target:v,clientX:x,clientY:300,pageX:x,pageY:300,screenX:x,screenY:300});const a=make(from),b=make(to);v.dispatchEvent(new TouchEvent('touchstart',{touches:[a],targetTouches:[a],changedTouches:[a],bubbles:true,cancelable:true}));v.dispatchEvent(new TouchEvent('touchmove',{touches:[b],targetTouches:[b],changedTouches:[b],bubbles:true,cancelable:true}));v.dispatchEvent(new TouchEvent('touchend',{touches:[],targetTouches:[],changedTouches:[b],bubbles:true,cancelable:true}));}; swipe(60,330); await new Promise(r=>setTimeout(r,40)); swipe(320,60); await new Promise(r=>setTimeout(r,40)); swipe(60,330); await new Promise(r=>setTimeout(r,380)); return {timerActive:document.querySelector('[data-page=timerPage]').classList.contains('active'),track:getComputedStyle(document.getElementById('pagesTrack')).transform,bodyScrollWidth:document.body.scrollWidth,clientWidth:document.documentElement.clientWidth}; }); }"
    if ($gesture -notmatch '"timerActive":true' -or $gesture -notmatch 'matrix\(1, 0, 0, 1, 0, 0\)' `
        -or $gesture -notmatch '"bodyScrollWidth":390') {
        throw "Remote browser continuous reverse-swipe validation failed: $gesture"
    }

    $english = Invoke-PlaywrightCli run-code "async page => { await page.addInitScript(() => Object.defineProperty(Navigator.prototype,'language',{get:()=> 'en-US'})); await page.reload(); await page.waitForTimeout(200); return await page.evaluate(() => ({lang:document.documentElement.lang,title:document.title,heading:document.querySelector('h1').textContent,status:document.getElementById('connection').textContent,scrollWidth:document.documentElement.scrollWidth})); }"
    if ($english -notmatch '"lang":"en"' -or $english -notmatch 'FlyPPTTimer Remote' `
        -or $english -notmatch 'Presentation Remote' -or $english -notmatch '"status":"Connected"') {
        throw "Remote browser language validation failed: $english"
    }

    Write-Host 'Real Chromium remote page passed: 390x844 layout, command, presentation tab, continuous reverse swipe, and zh/en behavior.'
}
finally {
    try { Invoke-PlaywrightCli close | Out-Null } catch { }
    if ($null -ne $server) {
        try { if (!$server.HasExited) { Stop-Process -Id $server.Id -Force } } catch { }
        $server.Dispose()
    }
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
