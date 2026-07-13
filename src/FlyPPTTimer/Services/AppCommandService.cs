using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

public sealed class AppCommandService
{
    private readonly TimerService _timer;
    private readonly AlertService _alerts;
    private readonly Func<AppConfig> _getConfig;
    private readonly Action<AppConfig> _saveConfig;
    private readonly Action _showOverlay;
    private readonly Action _hideOverlay;
    private readonly Action _toggleOverlay;
    private readonly Func<bool> _isOverlayVisible;
    private readonly Action<int> _flashOverlay;
    private readonly Action _openSettings;
    private readonly LogService _log;
    private readonly SynchronizationContext? _uiContext;

    public AppCommandService(
        TimerService timer,
        AlertService alerts,
        Func<AppConfig> getConfig,
        Action<AppConfig> saveConfig,
        Action showOverlay,
        Action hideOverlay,
        Action toggleOverlay,
        Func<bool> isOverlayVisible,
        Action<int> flashOverlay,
        Action openSettings,
        LogService log)
    {
        _timer = timer;
        _alerts = alerts;
        _getConfig = getConfig;
        _saveConfig = saveConfig;
        _showOverlay = showOverlay;
        _hideOverlay = hideOverlay;
        _toggleOverlay = toggleOverlay;
        _isOverlayVisible = isOverlayVisible;
        _flashOverlay = flashOverlay;
        _openSettings = openSettings;
        _log = log;
        _uiContext = SynchronizationContext.Current;
    }

    public void StartOrPause()
    {
        if (_timer.State is TimerState.Running) Pause();
        else if (_timer.State is TimerState.Paused) Resume();
        else Start();
    }

    public void Start()
    {
        _alerts.ResetTriggers();
        _timer.Start();
    }

    public void Pause() => _timer.Pause();
    public void Resume() => _timer.Resume();
    public void Stop() => _timer.Stop(false);
    public void Reset()
    {
        _alerts.ResetTriggers();
        _timer.Reset();
    }

    public void StopReset()
    {
        _alerts.ResetTriggers();
        _timer.Stop(true);
    }

    public void SetDuration(TimeSpan duration)
    {
        var config = _getConfig();
        config.Timer.DefaultDuration = duration.ToString(@"hh\:mm\:ss");
        _timer.SetDuration(duration);
        _saveConfig(config);
    }

    public void SetMode(TimerMode mode)
    {
        var config = _getConfig();
        config.Timer.Mode = mode;
        _timer.SetMode(mode);
        _saveConfig(config);
    }

    public void ToggleMode() => SetMode(_timer.Mode == TimerMode.Countdown ? TimerMode.CountUp : TimerMode.Countdown);
    public void ToggleOverlay() => _toggleOverlay();
    public void ShowOverlay() => _showOverlay();
    public void HideOverlay() => _hideOverlay();
    public void FlashOverlay() => _flashOverlay(3);
    public void TestPrompt() => _alerts.TriggerEnd(_getConfig(), _timer.CreateSnapshot());
    public void ToggleMute() => _alerts.ToggleMute();
    public void AddDuration(TimeSpan delta)
    {
        var next = _timer.Duration + delta;
        if (next < TimeSpan.FromMinutes(1)) next = TimeSpan.FromMinutes(1);
        SetDuration(next);
    }

    public void SetPresetDuration(TimeSpan duration) => SetDuration(duration);
    public void OpenSettings() => _openSettings();

    public RemoteState GetRemoteState()
    {
        if (ShouldMarshalToUi())
        {
            RemoteState? state = null;
            Exception? error = null;
            _uiContext!.Send(_ =>
            {
                try { state = GetRemoteStateCore(); }
                catch (Exception ex) { error = ex; }
            }, null);
            if (error is not null) throw error;
            return state!;
        }

        return GetRemoteStateCore();
    }

    private RemoteState GetRemoteStateCore()
    {
        var snapshot = _timer.CreateSnapshot();
        var timerState = new TimerRemoteState
        {
            Mode = snapshot.Mode == TimerMode.Countdown ? "倒计时" : "正计时",
            State = snapshot.State switch
            {
                TimerState.Running => "运行中",
                TimerState.Paused => "暂停",
                TimerState.Finished => "已结束",
                _ => "停止"
            },
            Running = snapshot.State == TimerState.Running,
            DurationMs = (long)snapshot.Duration.TotalMilliseconds,
            ElapsedMs = (long)snapshot.Elapsed.TotalMilliseconds,
            RemainingMs = (long)snapshot.Remaining.TotalMilliseconds,
            DisplayText = AlertService.Format(snapshot.Display, AlertService.ShouldShowHours(snapshot)),
            WindowVisible = _isOverlayVisible(),
            Muted = _alerts.Muted
        };
        return new RemoteState
        {
            TimerState = timerState,
            Mode = timerState.Mode,
            State = timerState.State,
            Running = timerState.Running,
            DurationMs = timerState.DurationMs,
            ElapsedMs = timerState.ElapsedMs,
            RemainingMs = timerState.RemainingMs,
            DisplayText = timerState.DisplayText,
            WindowVisible = timerState.WindowVisible,
            Muted = timerState.Muted,
            Version = "0.11.0"
        };
    }

    public bool ExecuteRemoteCommand(RemoteCommand command)
    {
        _log.Info($"Remote command requested: {command.Command}");
        if (ShouldMarshalToUi())
        {
            var handled = false;
            Exception? error = null;
            _uiContext!.Send(_ =>
            {
                try { handled = ExecuteRemoteCommandCore(command); }
                catch (Exception ex) { error = ex; }
            }, null);
            if (error is not null) throw error;
            return handled;
        }

        return ExecuteRemoteCommandCore(command);
    }

    private bool ExecuteRemoteCommandCore(RemoteCommand command)
    {
        switch (command.Command)
        {
            case "timer.start": Start(); return true;
            case "timer.pause": Pause(); return true;
            case "timer.resume": Resume(); return true;
            case "timer.stop": StopReset(); return true;
            case "timer.reset": Reset(); return true;
            case "timer.setDuration":
                if (command.DurationMs is > 0) SetDuration(TimeSpan.FromMilliseconds(command.DurationMs.Value));
                else if (TimeSpan.TryParse(command.Duration, out var duration)) SetDuration(duration);
                else return false;
                return true;
            case "timer.setMode":
                SetMode(command.Mode == "countup" || command.Mode == "正计时" ? TimerMode.CountUp : TimerMode.Countdown);
                return true;
            case "window.show": ShowOverlay(); return true;
            case "window.hide": HideOverlay(); return true;
            case "window.toggle": ToggleOverlay(); return true;
            case "window.flash": FlashOverlay(); return true;
            case "mute.toggle": ToggleMute(); return true;
            case "state.get": return true;
            default:
                _log.Warn($"Rejected unknown remote command: {command.Command}");
                return false;
        }
    }

    private bool ShouldMarshalToUi() => _uiContext is not null && SynchronizationContext.Current != _uiContext;
}
