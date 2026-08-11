param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory,
    [ValidateRange(1, 120)]
    [int]$LaunchTimeoutSeconds = 20,
    [ValidateRange(1, 10)]
    [int]$OperationTimeoutSeconds = 3,
    [switch]$ReportHostedRunnerUnavailability
)

$ErrorActionPreference = 'Stop'
$global:LASTEXITCODE = 0
$sourceDirectory = (Resolve-Path $PublishDirectory).Path
$sourceMainExe = Join-Path $sourceDirectory 'FlyPPTTimer.exe'
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ("FlyPPTTimer-WpfRemoteSmoke-" + [Guid]::NewGuid().ToString('N'))
$mainExe = Join-Path $testDirectory 'FlyPPTTimer.exe'
$mainProcess = $null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Find-ByProcessAndAutomationId {
    param([int]$ProcessId, [string]$AutomationId)
    $processCondition = [Windows.Automation.PropertyCondition]::new(
        [Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcessId)
    $idCondition = [Windows.Automation.PropertyCondition]::new(
        [Windows.Automation.AutomationElement]::AutomationIdProperty, $AutomationId)
    return [Windows.Automation.AutomationElement]::RootElement.FindFirst(
        [Windows.Automation.TreeScope]::Descendants,
        [Windows.Automation.AndCondition]::new($processCondition, $idCondition))
}

function Wait-ForElement {
    param([int]$ProcessId, [string]$AutomationId, [int]$TimeoutSeconds)
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $element = $null
    do {
        try { $element = Find-ByProcessAndAutomationId $ProcessId $AutomationId } catch { $element = $null }
        if ($null -eq $element) { Start-Sleep -Milliseconds 50 }
    } while ($null -eq $element -and $watch.Elapsed.TotalSeconds -lt $TimeoutSeconds)
    $watch.Stop()
    return [pscustomobject]@{ Element = $element; Elapsed = $watch.Elapsed }
}

function Invoke-Control {
    param([Windows.Automation.AutomationElement]$Element, [string]$Label, [int]$TimeoutSeconds)
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $pattern = $Element.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
    do {
        Start-Sleep -Milliseconds 25
    } while (!$mainProcess.Responding -and $watch.Elapsed.TotalSeconds -lt $TimeoutSeconds)
    $watch.Stop()
    if (!$mainProcess.Responding) { throw "$Label left the main process unresponsive after $TimeoutSeconds seconds." }
    if ($watch.Elapsed.TotalSeconds -ge $TimeoutSeconds) { throw "$Label exceeded $TimeoutSeconds seconds." }
    Write-Host ("{0}: {1:N0} ms, responsive" -f $Label, $watch.Elapsed.TotalMilliseconds)
}

try {
    if (!(Test-Path -LiteralPath $sourceMainExe)) { throw 'FlyPPTTimer.exe is missing from the publish directory.' }
    New-Item -ItemType Directory -Path $testDirectory | Out-Null
    Copy-Item -LiteralPath $sourceMainExe -Destination $mainExe
    $settingsExe = Join-Path $sourceDirectory 'FlyPPTTimer.Settings.exe'
    if (Test-Path -LiteralPath $settingsExe) { Copy-Item -LiteralPath $settingsExe -Destination $testDirectory }

    $mainProcess = Start-Process -FilePath $mainExe -ArgumentList '--show-remote' -WorkingDirectory $testDirectory -PassThru
    $startup = Wait-ForElement $mainProcess.Id 'RemoteDashboardWindow' $LaunchTimeoutSeconds
    if ($null -eq $startup.Element) {
        if ($ReportHostedRunnerUnavailability -and $env:GITHUB_ACTIONS -eq 'true') {
            Write-Warning 'GitHub-hosted Windows runner did not expose the WPF remote dashboard. The workflow must run the explicit STA real-WPF-control/Dispatcher fallback; this is not a passing or skipped interaction test.'
            $global:LASTEXITCODE = 44
            return
        }
        throw "The WPF remote dashboard was not shown within $LaunchTimeoutSeconds seconds."
    }
    Write-Host ("WPF remote dashboard startup: {0:N0} ms" -f $startup.Elapsed.TotalMilliseconds)

    foreach ($id in @('RemoteServiceToggle', 'RemoteAddress', 'RemoteAccessUrl', 'DisconnectAll', 'RemoteDashboardTabs')) {
        if ($null -eq (Wait-ForElement $mainProcess.Id $id $OperationTimeoutSeconds).Element) {
            throw "The real WPF remote control '$id' was not found."
        }
    }

    $toggle = (Wait-ForElement $mainProcess.Id 'RemoteServiceToggle' $OperationTimeoutSeconds).Element
    Invoke-Control $toggle 'Remote service toggle' $OperationTimeoutSeconds
    $toggle = (Wait-ForElement $mainProcess.Id 'RemoteServiceToggle' $OperationTimeoutSeconds).Element
    Invoke-Control $toggle 'Remote service restore' $OperationTimeoutSeconds

    $window = $startup.Element
    $resizeWatch = [Diagnostics.Stopwatch]::StartNew()
    $transform = $window.GetCurrentPattern([Windows.Automation.TransformPattern]::Pattern)
    if ($transform.Current.CanResize) { $transform.Resize(780, 600) }
    Start-Sleep -Milliseconds 100
    $resizeWatch.Stop()
    if ($resizeWatch.Elapsed.TotalSeconds -ge $OperationTimeoutSeconds -or !$mainProcess.Responding) {
        throw "Remote dashboard responsive resize exceeded $OperationTimeoutSeconds seconds."
    }
    Write-Host ("Remote dashboard resize: {0:N0} ms, responsive" -f $resizeWatch.Elapsed.TotalMilliseconds)

    $tabCondition = [Windows.Automation.AndCondition]::new(
        [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::ProcessIdProperty, $mainProcess.Id),
        [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::NameProperty, '演示文稿'))
    $tab = [Windows.Automation.AutomationElement]::RootElement.FindFirst([Windows.Automation.TreeScope]::Descendants, $tabCondition)
    if ($null -eq $tab) { throw 'The WPF presentation tab was not found.' }
    $tabWatch = [Diagnostics.Stopwatch]::StartNew()
    $tab.GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $list = (Wait-ForElement $mainProcess.Id 'PresentationList' $OperationTimeoutSeconds).Element
    $duration = (Wait-ForElement $mainProcess.Id 'PresentationDuration' $OperationTimeoutSeconds).Element
    $tabWatch.Stop()
    if ($null -eq $list -or $null -eq $duration -or $tabWatch.Elapsed.TotalSeconds -ge $OperationTimeoutSeconds) {
        throw "Presentation tab controls did not respond within $OperationTimeoutSeconds seconds."
    }
    Write-Host ("Presentation tab selection: {0:N0} ms, responsive" -f $tabWatch.Elapsed.TotalMilliseconds)
    Write-Host 'Published WPF remote dashboard interaction smoke test passed.'
}
finally {
    if ($null -ne $mainProcess) {
        try {
            if (!$mainProcess.HasExited) {
                Stop-Process -Id $mainProcess.Id -Force
                $mainProcess.WaitForExit(3000) | Out-Null
            }
        } catch { }
        $mainProcess.Dispose()
    }
    for ($attempt = 0; $attempt -lt 20 -and (Test-Path -LiteralPath $testDirectory); $attempt++) {
        try { Remove-Item -LiteralPath $testDirectory -Recurse -Force -ErrorAction Stop }
        catch { Start-Sleep -Milliseconds 250 }
    }
    if (Test-Path -LiteralPath $testDirectory) { throw "Failed to clean temporary directory '$testDirectory'." }
}
