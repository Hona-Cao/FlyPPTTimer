param(
    [Parameter(Mandatory = $true)]
    [string]$SettingsExe,

    [ValidateRange(1, 120)]
    [int]$LaunchTimeoutSeconds = 20,

    [ValidateRange(1, 10)]
    [int]$OperationTimeoutSeconds = 3
)

$ErrorActionPreference = 'Stop'
$launchTimeout = [TimeSpan]::FromSeconds($LaunchTimeoutSeconds)
$operationTimeout = [TimeSpan]::FromSeconds($OperationTimeoutSeconds)
$sourceExe = (Resolve-Path $SettingsExe).Path
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ("FlyPPTTimer-WpfSmoke-" + [Guid]::NewGuid().ToString('N'))
$testExe = Join-Path $testDirectory 'FlyPPTTimer.Settings.exe'
$process = $null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Find-ByAutomationId {
    param(
        [Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId
    )

    $condition = [Windows.Automation.PropertyCondition]::new(
        [Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)
    $element = $Root.FindFirst([Windows.Automation.TreeScope]::Descendants, $condition)
    if ($null -eq $element) { throw "Automation element '$AutomationId' was not found." }
    return $element
}

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

function Assert-Responsive {
    param(
        [Diagnostics.Process]$Process,
        [Windows.Automation.AutomationElement]$Window,
        [string]$Operation,
        [Diagnostics.Stopwatch]$Stopwatch
    )

    $windowPattern = [Windows.Automation.WindowPattern]$Window.GetCurrentPattern(
        [Windows.Automation.WindowPattern]::Pattern)
    if (!$windowPattern.WaitForInputIdle(2000)) { throw "$Operation did not return the WPF Dispatcher to idle." }
    $Process.Refresh()
    if ($Process.HasExited) { throw "$Operation terminated the settings process." }
    if (!$Process.Responding) { throw "$Operation left the settings window unresponsive." }
    $Stopwatch.Stop()
    if ($Stopwatch.Elapsed -ge $operationTimeout) { throw "$Operation took $($Stopwatch.Elapsed.TotalMilliseconds) ms." }
    Write-Host ("{0}: {1:N0} ms, responsive" -f $Operation, $Stopwatch.Elapsed.TotalMilliseconds)
}

function Invoke-ControlOperation {
    param(
        [string]$Name,
        [scriptblock]$Action,
        [Diagnostics.Process]$Process,
        [Windows.Automation.AutomationElement]$Window
    )

    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    & $Action
    Assert-Responsive -Process $Process -Window $Window -Operation $Name -Stopwatch $stopwatch
}

try {
    New-Item -ItemType Directory -Path $testDirectory | Out-Null
    Copy-Item -LiteralPath $sourceExe -Destination $testExe
    $process = Start-Process -FilePath $testExe -WorkingDirectory $testDirectory -PassThru

    $launchStopwatch = [Diagnostics.Stopwatch]::StartNew()
    $window = $null
    do {
        Start-Sleep -Milliseconds 100
        $process.Refresh()
        if ($process.HasExited) { throw 'The settings process exited before showing its window.' }
        if ($process.MainWindowHandle -ne 0) {
            try { $window = [Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle) }
            catch { $window = $null }
        }
        if ($null -eq $window) {
            try { $window = Find-WindowByProcessId $process.Id }
            catch { $window = $null }
        }
    } while ($null -eq $window -and $launchStopwatch.Elapsed -lt $launchTimeout)
    $launchStopwatch.Stop()
    if ($null -eq $window) {
        throw "The settings window was not found within $LaunchTimeoutSeconds seconds by MainWindowHandle or UI Automation process ID lookup."
    }
    Write-Host ("Settings window startup: {0:N0} ms" -f $launchStopwatch.Elapsed.TotalMilliseconds)

    $configPath = Join-Path $testDirectory 'FlyPPTTimer.config.json'
    $initialConfigHash = $null
    if (Test-Path $configPath) {
        $initialConfigHash = (Get-FileHash -LiteralPath $configPath -Algorithm SHA256).Hash
    }
    $defaultDuration = Find-ByAutomationId $window 'DefaultDuration'
    $timerMode = Find-ByAutomationId $window 'TimerMode'
    $continueOvertime = Find-ByAutomationId $window 'ContinueOvertime'
    $width = Find-ByAutomationId $window 'Width'
    $unsavedStatus = Find-ByAutomationId $window 'UnsavedStatus'
    $initialStatus = $unsavedStatus.Current.Name
    $cancel = Find-ByAutomationId $window 'Cancel'
    $saveAndClose = Find-ByAutomationId $window 'SaveAndClose'
    if (!$saveAndClose.Current.IsEnabled) { throw 'SaveAndClose is unexpectedly disabled.' }

    Invoke-ControlOperation 'DefaultDuration text edit' {
        $pattern = [Windows.Automation.ValuePattern]$defaultDuration.GetCurrentPattern(
            [Windows.Automation.ValuePattern]::Pattern)
        $pattern.SetValue('00:09:30')
    } $process $window

    Invoke-ControlOperation 'TimerMode combo selection' {
        $expand = [Windows.Automation.ExpandCollapsePattern]$timerMode.GetCurrentPattern(
            [Windows.Automation.ExpandCollapsePattern]::Pattern)
        $expand.Expand()
        $optionCondition = [Windows.Automation.PropertyCondition]::new(
            [Windows.Automation.AutomationElement]::ControlTypeProperty,
            [Windows.Automation.ControlType]::ListItem)
        $options = $timerMode.FindAll([Windows.Automation.TreeScope]::Descendants, $optionCondition)
        if ($options.Count -lt 2) { throw 'The timer mode options were not found.' }
        $option = $options.Item(1)
        $selection = [Windows.Automation.SelectionItemPattern]$option.GetCurrentPattern(
            [Windows.Automation.SelectionItemPattern]::Pattern)
        $selection.Select()
        $expand.Collapse()
    } $process $window

    Invoke-ControlOperation 'ContinueOvertime checkbox toggle' {
        $pattern = [Windows.Automation.TogglePattern]$continueOvertime.GetCurrentPattern(
            [Windows.Automation.TogglePattern]::Pattern)
        $pattern.Toggle()
    } $process $window

    Invoke-ControlOperation 'Width numeric edit' {
        $pattern = [Windows.Automation.ValuePattern]$width.GetCurrentPattern(
            [Windows.Automation.ValuePattern]::Pattern)
        $pattern.SetValue('680')
    } $process $window

    $currentStatus = (Find-ByAutomationId $window 'UnsavedStatus').Current.Name
    if ([string]::IsNullOrWhiteSpace($currentStatus) -or $currentStatus -eq $initialStatus) {
        throw 'The unsaved-settings status did not change.'
    }

    $cancelStopwatch = [Diagnostics.Stopwatch]::StartNew()
    $cancelPattern = [Windows.Automation.InvokePattern]$cancel.GetCurrentPattern(
        [Windows.Automation.InvokePattern]::Pattern)
    $cancelPattern.Invoke()
    if (!$process.WaitForExit($OperationTimeoutSeconds * 1000)) {
        throw "Cancel did not close the settings process within $OperationTimeoutSeconds seconds."
    }
    $cancelStopwatch.Stop()
    if ($cancelStopwatch.Elapsed -ge $operationTimeout) { throw "Cancel took $($cancelStopwatch.Elapsed.TotalMilliseconds) ms." }
    Write-Host ("Cancel without saving: {0:N0} ms, process closed" -f $cancelStopwatch.Elapsed.TotalMilliseconds)
    $finalConfigHash = $null
    if (Test-Path $configPath) {
        $finalConfigHash = (Get-FileHash -LiteralPath $configPath -Algorithm SHA256).Hash
    }
    if ($finalConfigHash -ne $initialConfigHash) {
        throw 'Cancel unexpectedly changed the configuration file.'
    }
    Write-Host 'Published WPF settings interaction smoke test passed.'
}
finally {
    if ($null -ne $process) {
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
    if (Test-Path $testDirectory) {
        throw "Failed to clean temporary directory '$testDirectory'."
    }
}
