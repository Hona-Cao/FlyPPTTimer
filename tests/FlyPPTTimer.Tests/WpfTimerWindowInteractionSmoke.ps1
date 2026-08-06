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
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ("FlyPPTTimer-WpfTimerSmoke-" + [Guid]::NewGuid().ToString('N'))
$mainExe = Join-Path $testDirectory 'FlyPPTTimer.exe'
$mainProcess = $null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class WpfTimerSmokeNative {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
'@

function Find-ByProcessAndAutomationId {
    param([int]$ProcessId, [string]$AutomationId, [Windows.Automation.TreeScope]$Scope = [Windows.Automation.TreeScope]::Descendants)
    $processCondition = [Windows.Automation.PropertyCondition]::new(
        [Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcessId)
    $idCondition = [Windows.Automation.PropertyCondition]::new(
        [Windows.Automation.AutomationElement]::AutomationIdProperty, $AutomationId)
    $condition = [Windows.Automation.AndCondition]::new($processCondition, $idCondition)
    return [Windows.Automation.AutomationElement]::RootElement.FindFirst($Scope, $condition)
}

function Wait-ForElement {
    param([int]$ProcessId, [string]$AutomationId, [int]$TimeoutSeconds)
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $element = $null
    do {
        try { $element = Find-ByProcessAndAutomationId $ProcessId $AutomationId }
        catch { $element = $null }
        if ($null -eq $element) { Start-Sleep -Milliseconds 50 }
    } while ($null -eq $element -and $watch.Elapsed.TotalSeconds -lt $TimeoutSeconds)
    $watch.Stop()
    return [pscustomobject]@{ Element = $element; Elapsed = $watch.Elapsed }
}

try {
    if (!(Test-Path -LiteralPath $sourceMainExe)) { throw 'FlyPPTTimer.exe is missing from the publish directory.' }
    New-Item -ItemType Directory -Path $testDirectory | Out-Null
    Copy-Item -LiteralPath $sourceMainExe -Destination $mainExe
    $settingsExe = Join-Path $sourceDirectory 'FlyPPTTimer.Settings.exe'
    if (Test-Path -LiteralPath $settingsExe) { Copy-Item -LiteralPath $settingsExe -Destination $testDirectory }

    $mainProcess = Start-Process -FilePath $mainExe -WorkingDirectory $testDirectory -PassThru
    $startup = Wait-ForElement $mainProcess.Id 'TimerOverlayWindow' $LaunchTimeoutSeconds
    if ($null -eq $startup.Element) {
        if ($ReportHostedRunnerUnavailability -and $env:GITHUB_ACTIONS -eq 'true') {
            Write-Warning 'GitHub-hosted Windows runner did not expose the WPF timer through UI Automation. The workflow must run the explicit STA real-WPF-window/Dispatcher fallback; this is not a passing or skipped interaction test.'
            $global:LASTEXITCODE = 43
            return
        }
        throw "The WPF timer window was not shown within $LaunchTimeoutSeconds seconds."
    }
    Write-Host ("WPF timer cold startup: {0:N0} ms" -f $startup.Elapsed.TotalMilliseconds)

    $display = (Wait-ForElement $mainProcess.Id 'TimerDisplayText' $OperationTimeoutSeconds).Element
    if ($null -eq $display) { throw 'The real WPF timer TextBlock was not found.' }
    $initialText = $display.Current.Name

    # Window discovery measures cold startup only. Hotkey registration happens later in the
    # application composition root and is readiness work, not part of the 3-second operation SLA.
    $readinessWatch = [Diagnostics.Stopwatch]::StartNew()
    $logPath = Join-Path $testDirectory ("logs\app-" + (Get-Date -Format 'yyyyMMdd') + '.log')
    $hotkeyReady = $false
    do {
        if (Test-Path -LiteralPath $logPath) {
            $hotkeyReady = $null -ne (Select-String -LiteralPath $logPath -SimpleMatch 'Hotkey registered: F3' -ErrorAction SilentlyContinue)
        }
        if (!$hotkeyReady) { Start-Sleep -Milliseconds 50 }
    } while (!$hotkeyReady -and $readinessWatch.Elapsed.TotalSeconds -lt $LaunchTimeoutSeconds)
    $readinessWatch.Stop()
    if (!$hotkeyReady) { throw "The default F3 hotkey was not ready within $LaunchTimeoutSeconds seconds." }
    Write-Host ("Application command readiness: {0:N0} ms" -f $readinessWatch.Elapsed.TotalMilliseconds)

    $keyWatch = [Diagnostics.Stopwatch]::StartNew()
    [WpfTimerSmokeNative]::keybd_event(0x72, 0, 0, [UIntPtr]::Zero)
    [WpfTimerSmokeNative]::keybd_event(0x72, 0, 2, [UIntPtr]::Zero)
    do {
        Start-Sleep -Milliseconds 50
        $display = (Wait-ForElement $mainProcess.Id 'TimerDisplayText' 1).Element
        $updatedText = if ($null -eq $display) { '' } else { $display.Current.Name }
    } while ($updatedText -eq $initialText -and $keyWatch.Elapsed.TotalSeconds -lt $OperationTimeoutSeconds)
    $keyWatch.Stop()
    if ($updatedText -eq $initialText) { throw "F3 did not update the WPF timer within $OperationTimeoutSeconds seconds." }
    if (!$mainProcess.Responding) { throw 'The main process stopped responding after F3.' }
    Write-Host ("F3 timer update: {0:N0} ms ({1} -> {2}), responsive" -f $keyWatch.Elapsed.TotalMilliseconds, $initialText, $updatedText)

    $hideWatch = [Diagnostics.Stopwatch]::StartNew()
    [WpfTimerSmokeNative]::keybd_event(0x74, 0, 0, [UIntPtr]::Zero)
    [WpfTimerSmokeNative]::keybd_event(0x74, 0, 2, [UIntPtr]::Zero)
    do {
        Start-Sleep -Milliseconds 50
        $hiddenWindow = (Wait-ForElement $mainProcess.Id 'TimerOverlayWindow' 1).Element
    } while ($null -ne $hiddenWindow -and $hideWatch.Elapsed.TotalSeconds -lt $OperationTimeoutSeconds)
    $hideWatch.Stop()
    if ($null -ne $hiddenWindow) { throw "F5 did not hide the WPF timer within $OperationTimeoutSeconds seconds." }
    Write-Host ("F5 hide WPF timer: {0:N0} ms" -f $hideWatch.Elapsed.TotalMilliseconds)

    $showWatch = [Diagnostics.Stopwatch]::StartNew()
    [WpfTimerSmokeNative]::keybd_event(0x74, 0, 0, [UIntPtr]::Zero)
    [WpfTimerSmokeNative]::keybd_event(0x74, 0, 2, [UIntPtr]::Zero)
    $shownAgain = (Wait-ForElement $mainProcess.Id 'TimerOverlayWindow' $OperationTimeoutSeconds).Element
    $showWatch.Stop()
    if ($null -eq $shownAgain) { throw "F5 did not show the WPF timer within $OperationTimeoutSeconds seconds." }
    if (!$mainProcess.Responding) { throw 'The main process stopped responding after WPF timer hide/show.' }
    Write-Host ("F5 show WPF timer: {0:N0} ms, responsive" -f $showWatch.Elapsed.TotalMilliseconds)
    Write-Host 'Published WPF timer window interaction smoke test passed.'
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
