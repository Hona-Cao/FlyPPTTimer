using System.Diagnostics;
using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;
using FlyPPTTimer.Native;
using FlyPPTTimer.Services;

namespace FlyPPTTimer;

public sealed class FlyPPTTimerContext : ApplicationContext
{
    private readonly LogService _log = new();
    private readonly SynchronizationContext _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
    private readonly ConfigService _configService;
    private readonly TimerService _timer;
    private readonly AlertService _alerts;
    private readonly SystemAudioService _systemAudio;
    private readonly FullscreenDetector _fullscreen;
    private readonly HotkeyService _hotkeys;
    private readonly NetworkAddressService _networkAddresses = new();
    private readonly AppCommandService _commands;
    private readonly PowerPointControlService _powerPoint;
    private readonly PresentationLifecycleController _presentationLifecycle;
    private readonly RemoteControlService _remoteControl;
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _overlayMenu;
    private readonly ContextMenuStrip _trayMenu;
    private readonly List<TimerOverlayForm> _overlays = [];
    private readonly Dictionary<string, PointF> _overlayCenters = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<TimeUpBlackoutForm> _timeUpScreens = [];
    private readonly System.Windows.Forms.Timer _screenTimer = new() { Interval = 1500 };
    private readonly System.Windows.Forms.Timer _menuCloseTimer = new() { Interval = 40 };
    private readonly Form _menuOwner = new()
    {
        ShowInTaskbar = false,
        FormBorderStyle = FormBorderStyle.None,
        StartPosition = FormStartPosition.Manual,
        Size = new Size(1, 1),
        Opacity = 0,
        Location = new Point(-32000, -32000)
    };
    private ContextMenuStrip? _activeMenu;
    private SettingsForm? _settings;
    private RemoteControlForm? _remoteControlWindow;
    private AppConfig _config;
    private string _screenSignature = "";
    private bool _preserveTimeUpScreens;

    public FlyPPTTimerContext()
    {
        _log.Info("Application started.");
        _configService = new ConfigService(_log);
        _config = _configService.Load();
        _powerPoint = new PowerPointControlService(() => _config, _log);
        _timer = new TimerService(_log);
        _alerts = new AlertService(_log);
        _systemAudio = new SystemAudioService(_log);
        _fullscreen = new FullscreenDetector(() => _powerPoint.GetState(), _log);
        _hotkeys = new HotkeyService(_log);
        _overlayMenu = BuildCommandMenu();
        _trayMenu = BuildCommandMenu();
        _commands = new AppCommandService(
            _timer,
            _alerts,
            () => _config,
            SaveConfigOnly,
            ShowOverlay,
            HideOverlay,
            ToggleOverlay,
            () => _overlays.Any(x => x.Visible),
            FlashOverlay,
            ShowSettings,
            _log,
            _systemAudio,
            () => _preserveTimeUpScreens || _timeUpScreens.Count > 0,
            DismissTimeUpBlackout);
        _presentationLifecycle = new PresentationLifecycleController(
            () => _config,
            ApplyPresentationRuleSettings,
            _alerts.ResetTriggers,
            _commands.StopReset,
            _commands.Start,
            reset => _timer.Stop(reset),
            _timer.Reset,
            _log);
        _powerPoint.SlideShowStarted += (_, path) => RunOnUi(() => HandlePresentationStarted(path, "远程控制"));
        _powerPoint.SlideShowEnded += (_, _) => RunOnUi(() => HandlePresentationEnded("远程控制"));
        _powerPoint.SlideShowWindowActivated += (_, _) => RunOnUi(() =>
        {
            foreach (var overlay in _overlays) overlay.ReassertTopMost();
        });
        _remoteControl = new RemoteControlService(() => _config, SaveConfigOnly, _commands, _powerPoint, _log);

        _timer.Configure(_config);
        RebuildOverlays();

        _timer.Updated += (_, snapshot) =>
        {
            if (_preserveTimeUpScreens && snapshot.State == TimerState.Running)
            {
                _preserveTimeUpScreens = false;
                HideTimeUpScreens();
            }
            else if (!_preserveTimeUpScreens && (snapshot.State == TimerState.Stopped || (!_timer.FinishRaised && snapshot.Elapsed < TimeSpan.FromSeconds(1))))
            {
                HideTimeUpScreens();
            }
            foreach (var overlay in _overlays) overlay.UpdateTime(snapshot);
            _alerts.CheckPrompts(_config, snapshot);
        };
        _timer.Finished += (_, snapshot) =>
        {
            _alerts.TriggerEnd(_config, snapshot);
            HandleTimeReached();
        };
        _alerts.PromptVisualRequested += (_, prompt) =>
        {
            foreach (var overlay in _overlays) overlay.Flash(prompt, prompt.FlashSeconds);
        };
        _fullscreen.Configure(_config);
        _fullscreen.StateChanged += OnFullscreenChanged;
        _fullscreen.Start();
        if (_config.RemoteControl.Enabled) _remoteControl.Start();
        _screenSignature = GetScreenSignature();
        _screenTimer.Tick += (_, _) => CheckScreenChanges();
        _screenTimer.Start();
        _menuCloseTimer.Tick += (_, _) => CloseMenuIfClickedOutside();

        _tray = new NotifyIcon
        {
            Text = "演讲计时器",
            Icon = LoadAppIcon(),
            Visible = true
        };
        _tray.DoubleClick += (_, _) => ShowSettings();
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Right) ShowCommandMenuAtCursor(_trayMenu);
        };

        RegisterHotkeys();
        foreach (var overlay in _overlays.Where(x => _config.Placement.Visible)) overlay.Show();
    }

    private ContextMenuStrip BuildCommandMenu()
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new ModernContextMenuRenderer(),
            BackColor = Color.White,
            ForeColor = ModernTheme.Text,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            Padding = new Padding(7),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            AutoClose = true
        };
        menu.Opened += (_, _) =>
        {
            ModernTheme.ApplyRoundedRegion(menu, 8);
            _activeMenu = menu;
            _menuCloseTimer.Start();
        };
        menu.Items.Add("开始/暂停", null, (_, _) => _commands.StartOrPause());
        menu.Items.Add("停止/重置", null, (_, _) => _commands.StopReset());
        menu.Items.Add("显示/隐藏计时窗口", null, (_, _) => _commands.ToggleOverlay());
        menu.Items.Add("重置计时窗口位置", null, (_, _) => ResetOverlayPosition());
        menu.Items.Add("触发闪烁", null, (_, _) => _commands.FlashOverlay());
        menu.Items.Add("静音/取消静音", null, (_, _) => _commands.ToggleMute());
        menu.Items.Add("远程控制", null, (_, _) => ShowRemoteControl());
        menu.Items.Add("设置", null, (_, _) => _commands.OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Exit());
        foreach (ToolStripItem item in menu.Items)
        {
            if (item is ToolStripSeparator) continue;
            item.AutoSize = false;
            item.Size = new Size(224, 38);
            item.Padding = new Padding(12, 0, 12, 0);
        }
        return menu;
    }

    private void ShowCommandMenuAtCursor(ContextMenuStrip menu)
    {
        _overlayMenu.Close();
        _trayMenu.Close();
        _activeMenu = menu;
        if (!_menuOwner.IsHandleCreated) _menuOwner.CreateControl();
        menu.PerformLayout();
        var menuSize = menu.GetPreferredSize(Size.Empty);
        if (menuSize.Width <= 0 || menuSize.Height <= 0) menuSize = new Size(220, 320);
        var cursor = Cursor.Position;
        var area = Screen.FromPoint(cursor).WorkingArea;
        var x = Math.Clamp(cursor.X - menuSize.Width, area.Left, Math.Max(area.Left, area.Right - menuSize.Width));
        var y = Math.Clamp(cursor.Y - menuSize.Height, area.Top, Math.Max(area.Top, area.Bottom - menuSize.Height));
        _menuOwner.Location = new Point(x, y);
        NativeMethods.SetForegroundWindow(_menuOwner.Handle);
        menu.Show(_menuOwner, Point.Empty);
        NativeMethods.PostMessage(_menuOwner.Handle, 0, IntPtr.Zero, IntPtr.Zero);
        _menuCloseTimer.Start();
    }

    private void CloseMenuIfClickedOutside()
    {
        if (_activeMenu is not { Visible: true } menu)
        {
            _menuCloseTimer.Stop();
            _activeMenu = null;
            return;
        }

        if (Control.MouseButtons == MouseButtons.None) return;
        if (menu.Bounds.Contains(Cursor.Position)) return;
        menu.Close(ToolStripDropDownCloseReason.AppClicked);
        _menuCloseTimer.Stop();
        _activeMenu = null;
    }

    private void RegisterHotkeys()
    {
        var actions = new Dictionary<string, Action>
        {
            ["startPause"] = _commands.StartOrPause,
            ["start"] = _commands.Start,
            ["pause"] = _commands.Pause,
            ["resume"] = _commands.Resume,
            ["stopReset"] = _commands.StopReset,
            ["stop"] = _commands.Stop,
            ["reset"] = _commands.Reset,
            ["toggleWindow"] = _commands.ToggleOverlay,
            ["showWindow"] = _commands.ShowOverlay,
            ["hideWindow"] = _commands.HideOverlay,
            ["flash"] = _commands.FlashOverlay,
            ["toggleMute"] = _commands.ToggleMute,
            ["toggleMode"] = _commands.ToggleMode,
            ["addMinute"] = () => _commands.AddDuration(TimeSpan.FromMinutes(1)),
            ["subtractMinute"] = () => _commands.AddDuration(TimeSpan.FromMinutes(-1)),
            ["preset3"] = () => _commands.SetPresetDuration(TimeSpan.FromMinutes(3)),
            ["preset5"] = () => _commands.SetPresetDuration(TimeSpan.FromMinutes(5)),
            ["preset8"] = () => _commands.SetPresetDuration(TimeSpan.FromMinutes(8)),
            ["preset10"] = () => _commands.SetPresetDuration(TimeSpan.FromMinutes(10)),
            ["preset15"] = () => _commands.SetPresetDuration(TimeSpan.FromMinutes(15))
        };
        _hotkeys.RegisterAll(_config.Controls.Hotkeys, actions);
    }

    private void ToggleOverlay()
    {
        _config.Placement.Visible = !_overlays.Any(x => x.Visible);
        foreach (var overlay in _overlays) overlay.Visible = _config.Placement.Visible;
        _configService.Save(_config);
    }

    private void ShowOverlay()
    {
        _config.Placement.Visible = true;
        foreach (var overlay in _overlays) overlay.Visible = true;
        _configService.Save(_config);
    }

    private void HideOverlay()
    {
        _config.Placement.Visible = false;
        foreach (var overlay in _overlays) overlay.Visible = false;
        _configService.Save(_config);
    }

    private void FlashOverlay(int seconds)
    {
        var prompt = new PromptSettings { Enabled = true, FlashBackground = true, Text = "闪烁测试" };
        foreach (var overlay in _overlays) overlay.Flash(prompt, seconds);
    }

    private void ShowSettings()
    {
        if (_settings is null || _settings.IsDisposed)
        {
            _settings = new SettingsForm(_config, _remoteControl, _networkAddresses);
            _settings.ConfigApplied += (_, cfg) => ApplyConfig(cfg);
            _settings.ResetRequested += (_, _) => ApplyConfig(new AppConfig());
            _settings.ExportRequested += (_, _) => ExportConfig();
            _settings.ImportRequested += (_, _) => ImportConfig();
            _settings.OpenConfigRequested += (_, _) => OpenPath(Path.GetDirectoryName(AppPaths.ConfigPath)!);
            _settings.OpenLogRequested += (_, _) => OpenPath(AppPaths.LogDirectory);
            _settings.ResetOverlayPositionRequested += (_, _) => ResetOverlayPosition();
        }
        _settings.Show();
        _settings.Activate();
    }

    private void ShowRemoteControl()
    {
        _config.RemoteControl.Enabled = true;
        SaveConfigOnly(_config);
        if (!_remoteControl.IsRunning) _remoteControl.Start();
        if (_remoteControlWindow is null || _remoteControlWindow.IsDisposed)
        {
            _remoteControlWindow = new RemoteControlForm(_config, _remoteControl, _networkAddresses, SaveConfigOnly);
        }

        _remoteControlWindow.Show();
        _remoteControlWindow.Activate();
    }

    private void ApplyConfig(AppConfig config)
    {
        var nextConfig = ConfigService.Clone(config);
        var samePlacement = SamePlacement(_config.Placement, nextConfig.Placement);
        var preserveCenters = samePlacement
            ? new Dictionary<string, PointF>(_overlayCenters, StringComparer.OrdinalIgnoreCase)
            : null;
        if (!samePlacement) _overlayCenters.Clear();
        _config = nextConfig;
        _timer.Configure(_config);
        RebuildOverlays(preserveCenters);
        _fullscreen.Configure(_config);
        _configService.Save(_config);
        if (_config.RemoteControl.Enabled)
        {
            if (!_remoteControl.IsRunning) _remoteControl.Start();
        }
        else
        {
            _remoteControl.Stop();
        }
        RegisterHotkeys();
        _remoteControl.NotifyStateChanged();
        _remoteControlWindow?.ReloadConfig(_config);
    }

    private void SaveConfigOnly(AppConfig config)
    {
        _config = ConfigService.Clone(config);
        _configService.Save(_config);
        _remoteControl.NotifyStateChanged();
        _remoteControlWindow?.ReloadConfig(_config);
        _settings?.SyncRules(_config.Rules);
        _settings?.SyncTimerSettings(_config.Timer);
    }

    private void HandleTimeReached()
    {
        switch (_config.Timer.EndAction)
        {
            case TimerEndAction.BlackScreen:
                _preserveTimeUpScreens = true;
                EndSlideShowAtTimeUp();
                _timer.Stop(true);
                ShowTimeUpScreens();
                break;
            case TimerEndAction.ExitSlideShow:
                _preserveTimeUpScreens = false;
                EndSlideShowAtTimeUp();
                _timer.Stop(true);
                HideTimeUpScreens();
                break;
            default:
                _preserveTimeUpScreens = false;
                break;
        }
    }

    private void EndSlideShowAtTimeUp()
    {
        var result = _powerPoint.Queue(new RemoteCommand { Command = "ppt.endShow" });
        if (!result.Success) _log.Warn($"Time-up slideshow exit was not accepted: {result.Message}");
    }

    private void ShowTimeUpScreens()
    {
        HideTimeUpScreens();
        foreach (var screen in Screen.AllScreens)
        {
            var blackout = new TimeUpBlackoutForm(screen);
            blackout.FormClosed += (_, _) => _timeUpScreens.Remove(blackout);
            _timeUpScreens.Add(blackout);
            blackout.Show();
        }
        _remoteControl?.NotifyStateChanged();
    }

    private void DismissTimeUpBlackout()
    {
        _preserveTimeUpScreens = false;
        HideTimeUpScreens();
        _remoteControl?.NotifyStateChanged();
    }

    private void HideTimeUpScreens()
    {
        foreach (var blackout in _timeUpScreens.ToArray())
        {
            blackout.Close();
            blackout.Dispose();
        }
        _timeUpScreens.Clear();
    }

    private void ExportConfig()
    {
        using var dialog = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = "FlyPPTTimer.config.json" };
        if (dialog.ShowDialog() == DialogResult.OK) _configService.Export(_config, dialog.FileName);
    }

    private void ImportConfig()
    {
        using var dialog = new OpenFileDialog { Filter = "JSON (*.json)|*.json" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            ApplyConfig(_configService.Import(dialog.FileName));
        }
    }

    private void OpenPath(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to open path: {path}", ex);
        }
    }

    private void OnFullscreenChanged(object? sender, FullscreenState state)
    {
        _presentationLifecycle.Observe(state.IsFullscreen, state.PresentationPath, "FullscreenDetector");
    }

    private void HandlePresentationStarted(string presentationPath, string source)
    {
        _settings?.Hide();
        _remoteControlWindow?.Hide();
        _presentationLifecycle.Observe(true, presentationPath, source);
    }

    private void HandlePresentationEnded(string source)
    {
        _presentationLifecycle.Observe(false, "", source);
    }

    private void RunOnUi(Action action)
    {
        if (SynchronizationContext.Current == _uiContext) action();
        else _uiContext.Send(_ => action(), null);
    }

    private void RebuildOverlays(IReadOnlyDictionary<string, PointF>? preservedCenters = null)
    {
        _overlayCenters.Clear();
        foreach (var overlay in _overlays)
        {
            overlay.PositionChangedByUser -= OverlayMoved;
            overlay.SizeExpansionRequested -= OverlaySizeExpansionRequested;
            overlay.Close();
            overlay.Dispose();
        }
        _overlays.Clear();

        var screens = GetTargetScreens();
        foreach (var screen in screens)
        {
            var overlay = new TimerOverlayForm(_overlayMenu, ShowCommandMenuAtCursor);
            overlay.PositionChangedByUser += OverlayMoved;
            overlay.SizeExpansionRequested += OverlaySizeExpansionRequested;
            // Create the Per-Monitor V2 HWND before assigning physical bounds.
            // Otherwise WinForms applies a second creation-time DPI scale while
            // keeping the left/top fixed, which moves the center right/down.
            overlay.CreateControl();
            _ = overlay.Handle;
            PointF? preservedCenter = null;
            if (preservedCenters is not null && preservedCenters.TryGetValue(screen.DeviceName, out var center))
                preservedCenter = center;
            overlay.ApplyConfig(_config, screen, preservedCenter);
            _overlayCenters[screen.DeviceName] = preservedCenter ?? overlay.CenterPoint;
            _overlays.Add(overlay);
            overlay.UpdateTime(_timer.CreateSnapshot());
            if (_config.Placement.Visible) overlay.Show();
        }
    }

    private void OverlaySizeExpansionRequested(object? sender, OverlaySizeExpansionEventArgs e)
    {
        if (sender is not TimerOverlayForm source) return;
        var dpi = RemoteScreenDpiProvider.FromScreen(source.TargetScreen).Dpi;
        var requiredWidthDip = RemoteWindowLayoutService.PhysicalToDip(e.RequiredWidth, dpi);
        var requiredHeightDip = RemoteWindowLayoutService.PhysicalToDip(e.RequiredHeight, dpi);
        if (requiredWidthDip <= _config.Appearance.Width && requiredHeightDip <= _config.Appearance.Height) return;
        _config.Appearance.Width = Math.Min(2000, Math.Max(_config.Appearance.Width, requiredWidthDip));
        _config.Appearance.Height = Math.Min(1000, Math.Max(_config.Appearance.Height, requiredHeightDip));
        _configService.Save(_config);
        foreach (var overlay in _overlays)
        {
            var center = _overlayCenters.TryGetValue(overlay.TargetScreen.DeviceName, out var savedCenter)
                ? savedCenter
                : overlay.CenterPoint;
            overlay.ApplyConfig(_config, overlay.TargetScreen, center);
            overlay.UpdateTime(_timer.CreateSnapshot());
        }
        MessageBox.Show($"当前时间文字需要更大的显示区域，窗口已自动调整为 {_config.Appearance.Width} × {_config.Appearance.Height}。", "演讲计时器", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ResetOverlayPosition()
    {
        // The configured screen/anchor/offset combination is the immutable origin.
        // Reset only discards a user's drag displacement; it must not replace the
        // selected origin with the historical primary-screen/top-center default.
        _config.Placement.HasCustomPlacement = true;
        _config.Placement.ScreenDeviceName = _config.Placement.TargetScreenDeviceName;
        _config.Placement.X = 0;
        _config.Placement.Y = 0;
        _overlayCenters.Clear();
        _configService.Save(_config);
        RebuildOverlays();
        _remoteControl.NotifyStateChanged();
    }

    private IEnumerable<Screen> GetTargetScreens()
    {
        if (_config.Placement.ShowOnAllScreens)
        {
            return Screen.AllScreens.OrderBy(x => x.Bounds.Left).ThenBy(x => x.Bounds.Top).ToArray();
        }

        var target = Screen.AllScreens.FirstOrDefault(x => string.Equals(x.DeviceName, _config.Placement.TargetScreenDeviceName, StringComparison.OrdinalIgnoreCase));
        return new[] { target ?? Screen.PrimaryScreen! };
    }

    private void OverlayMoved(object? sender, OverlayMovedEventArgs e)
    {
        var baseOrigin = TimerOverlayForm.CalculateOrigin(e.Screen, _config.Placement.Anchor, 0, 0);
        var area = e.Screen.WorkingArea;
        var overlay = (TimerOverlayForm)sender!;
        var movedCenter = new PointF(e.Location.X + overlay.Width / 2f, e.Location.Y + overlay.Height / 2f);
        _config.Placement.OffsetXPercent = Math.Round((decimal)(movedCenter.X - baseOrigin.X) * 100m / Math.Max(1, area.Width), 2);
        _config.Placement.OffsetYPercent = Math.Round((decimal)(movedCenter.Y - baseOrigin.Y) * 100m / Math.Max(1, area.Height), 2);
        _config.Placement.TargetScreenDeviceName = e.Screen.DeviceName;
        _config.Placement.ScreenDeviceName = e.Screen.DeviceName;
        _config.Placement.X = e.Location.X;
        _config.Placement.Y = e.Location.Y;
        _config.Placement.HasCustomPlacement = true;
        _overlayCenters[e.Screen.DeviceName] = movedCenter;
    }

    private static bool SamePlacement(WindowPlacement left, WindowPlacement right) =>
        left.ShowOnAllScreens == right.ShowOnAllScreens
        && string.Equals(left.TargetScreenDeviceName, right.TargetScreenDeviceName, StringComparison.OrdinalIgnoreCase)
        && left.Anchor == right.Anchor
        && left.OffsetXPercent == right.OffsetXPercent
        && left.OffsetYPercent == right.OffsetYPercent;

    private void ApplyPresentationRuleSettings(string presentationPath)
    {
        var durationText = _config.Timer.DefaultDuration;
        var mode = _config.Timer.Mode;
        if (!string.IsNullOrWhiteSpace(presentationPath))
        {
            var matched = _config.Rules.FirstOrDefault(rule =>
                rule.Enabled
                && !string.IsNullOrWhiteSpace(rule.FilePath)
                && SameFilePath(rule.FilePath, presentationPath));
            if (matched is not null)
            {
                durationText = matched.Duration;
                mode = matched.Mode;
            }
        }

        if (TimeSpan.TryParse(durationText, out var duration))
        {
            _timer.SetDuration(duration);
            _timer.SetMode(mode);
        }
    }

    private static bool SameFilePath(string left, string right)
    {
        try
        {
            return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void CheckScreenChanges()
    {
        var signature = GetScreenSignature();
        if (signature == _screenSignature) return;
        _screenSignature = signature;
        _log.Info($"Screen layout changed: {signature}");
        RebuildOverlays();
    }

    private static string GetScreenSignature()
    {
        return string.Join("|", Screen.AllScreens.Select(s => $"{s.DeviceName}:{s.Bounds.Left},{s.Bounds.Top},{s.Bounds.Width},{s.Bounds.Height}:{s.Primary}"));
    }

    private Icon LoadAppIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
        {
            try { return new Icon(iconPath); }
            catch (Exception ex) { _log.Error("Failed to load app icon.", ex); }
        }
        return SystemIcons.Application;
    }

    private void Exit()
    {
        if (_overlays.FirstOrDefault() is { } overlay)
        {
            _config.Placement.X = overlay.Left;
            _config.Placement.Y = overlay.Top;
            _config.Placement.Visible = overlay.Visible;
        }
        _configService.Save(_config);
        _log.Info("Application exiting.");
        _tray.Visible = false;
        _tray.Dispose();
        _trayMenu.Dispose();
        _menuOwner.Dispose();
        _hotkeys.Dispose();
        _fullscreen.Dispose();
        _remoteControl.Dispose();
        _powerPoint.Dispose();
        _alerts.Dispose();
        _screenTimer.Dispose();
        _menuCloseTimer.Dispose();
        foreach (var timerWindow in _overlays) timerWindow.Dispose();
        HideTimeUpScreens();
        _settings?.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Dispose();
            _trayMenu.Dispose();
            _menuOwner.Dispose();
            _hotkeys.Dispose();
            _fullscreen.Dispose();
            _powerPoint.Dispose();
            _alerts.Dispose();
            _screenTimer.Dispose();
            _menuCloseTimer.Dispose();
            foreach (var overlay in _overlays) overlay.Dispose();
            HideTimeUpScreens();
            _settings?.Dispose();
        }
        base.Dispose(disposing);
    }
}
