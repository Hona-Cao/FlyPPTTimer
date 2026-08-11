param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory,

    [ValidateRange(1, 120)]
    [int]$LaunchTimeoutSeconds = 20,

    [ValidateRange(1, 10)]
    [int]$ResponseTimeoutSeconds = 3
)

$ErrorActionPreference = 'Stop'
$sourceDirectory = (Resolve-Path $PublishDirectory).Path
$sourceMainExe = Join-Path $sourceDirectory 'FlyPPTTimer.exe'
$sourceSettingsExe = Join-Path $sourceDirectory 'FlyPPTTimer.Settings.exe'
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ("FlyPPTTimer-WpfExitSmoke-" + [Guid]::NewGuid().ToString('N'))
$mainExe = Join-Path $testDirectory 'FlyPPTTimer.exe'
$settingsExe = Join-Path $testDirectory 'FlyPPTTimer.Settings.exe'
$mainProcess = $null
$settingsProcess = $null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Find-WindowByProcessId {
    param([int]$ProcessId)

    $processCondition = [Windows.Automation.PropertyCondition]::new(
        [Windows.Automation.AutomationElement]::ProcessIdProperty,
        $ProcessId)
    $windowCondition = [Windows.Automation.PropertyCondition]::new(
        [Windows.Automation.AutomationElement]::ControlTypeProperty,
        [Windows.Automation.ControlType]::Window)
    $condition = [Windows.Automation.AndCondition]::new($processCondition, $windowCondition)
    return [Windows.Automation.AutomationElement]::RootElement.FindFirst(
        [Windows.Automation.TreeScope]::Children,
        $condition)
}

function Find-ByAutomationId {
    param(
        [Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId
    )

    $condition = [Windows.Automation.PropertyCondition]::new(
        [Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)
    return $Root.FindFirst([Windows.Automation.TreeScope]::Descendants, $condition)
}

try {
    if (!(Test-Path -LiteralPath $sourceMainExe)) { throw 'FlyPPTTimer.exe is missing from the publish directory.' }
    if (!(Test-Path -LiteralPath $sourceSettingsExe)) { throw 'FlyPPTTimer.Settings.exe is missing from the publish directory.' }
    New-Item -ItemType Directory -Path $testDirectory | Out-Null
    Copy-Item -LiteralPath $sourceMainExe -Destination $mainExe
    Copy-Item -LiteralPath $sourceSettingsExe -Destination $settingsExe

    $mainProcess = Start-Process -FilePath $mainExe -ArgumentList '--show-settings' -WorkingDirectory $testDirectory -PassThru
    $launchStopwatch = [Diagnostics.Stopwatch]::StartNew()
    $settingsWindow = $null
    do {
        Start-Sleep -Milliseconds 100
        $mainProcess.Refresh()
        if ($mainProcess.HasExited) { throw 'The main application exited before showing WPF settings.' }
        $settingsProcess = Get-Process -Name 'FlyPPTTimer.Settings' -ErrorAction SilentlyContinue |
            Where-Object { $_.Path -eq $settingsExe } |
            Select-Object -First 1
        if ($null -ne $settingsProcess) {
            try { $settingsWindow = Find-WindowByProcessId $settingsProcess.Id }
            catch { $settingsWindow = $null }
        }
    } while ($null -eq $settingsWindow -and $launchStopwatch.Elapsed.TotalSeconds -lt $LaunchTimeoutSeconds)
    $launchStopwatch.Stop()
    if ($null -eq $settingsWindow -or $null -eq $settingsProcess) {
        throw "The main application did not show WPF settings within $LaunchTimeoutSeconds seconds."
    }
    Write-Host ("Integrated WPF settings startup: {0:N0} ms" -f $launchStopwatch.Elapsed.TotalMilliseconds)

    $cancel = Find-ByAutomationId $settingsWindow 'Cancel'
    if ($null -eq $cancel) { throw 'The WPF settings Cancel button was not found.' }
    $logPath = Join-Path $testDirectory ("logs\app-" + (Get-Date -Format 'yyyyMMdd') + '.log')
    $loadsBeforeExit = 0
    if (Test-Path $logPath) {
        $loadsBeforeExit = @(Select-String -LiteralPath $logPath -SimpleMatch 'Config loaded.').Count
    }

    $exitStopwatch = [Diagnostics.Stopwatch]::StartNew()
    $cancelPattern = [Windows.Automation.InvokePattern]$cancel.GetCurrentPattern(
        [Windows.Automation.InvokePattern]::Pattern)
    $cancelPattern.Invoke()
    if (!$settingsProcess.WaitForExit($ResponseTimeoutSeconds * 1000)) {
        throw "WPF settings did not exit within $ResponseTimeoutSeconds seconds."
    }

    $mainReloaded = $false
    do {
        Start-Sleep -Milliseconds 100
        $mainProcess.Refresh()
        if ($mainProcess.HasExited) { throw 'The main application exited after WPF settings closed.' }
        $loadsAfterExit = 0
        if (Test-Path $logPath) {
            $loadsAfterExit = @(Select-String -LiteralPath $logPath -SimpleMatch 'Config loaded.').Count
        }
        $mainReloaded = $loadsAfterExit -gt $loadsBeforeExit
    } while (!$mainReloaded -and $exitStopwatch.Elapsed.TotalSeconds -lt $ResponseTimeoutSeconds)
    $exitStopwatch.Stop()

    if (!$mainReloaded) { throw 'The main application did not reload configuration after WPF settings exited.' }
    if (!$mainProcess.Responding) { throw 'The main application became unresponsive after WPF settings exited.' }
    Write-Host ("Main application response after settings exit: {0:N0} ms" -f $exitStopwatch.Elapsed.TotalMilliseconds)
    Write-Host 'Main application WPF settings exit smoke test passed.'
}
finally {
    foreach ($process in @($settingsProcess, $mainProcess)) {
        if ($null -eq $process) { continue }
        try {
            if (!$process.HasExited) {
                Stop-Process -Id $process.Id -Force
                $process.WaitForExit(3000) | Out-Null
            }
        }
        catch { }
        $process.Dispose()
    }
    for ($attempt = 0; $attempt -lt 10 -and (Test-Path $testDirectory); $attempt++) {
        try { Remove-Item -LiteralPath $testDirectory -Recurse -Force -ErrorAction Stop }
        catch { Start-Sleep -Milliseconds 200 }
    }
    if (Test-Path $testDirectory) { throw "Failed to clean temporary directory '$testDirectory'." }
}
