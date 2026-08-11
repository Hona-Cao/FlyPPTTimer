using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Desktop.ViewModels;

public sealed record SettingsChoice<T>(T Value, string Label);

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly AppConfig _config;
    private readonly ConfigService _configService;
    private readonly string _originalDefaultDuration;
    private string _defaultDuration;
    private TimerMode _selectedTimerMode;
    private bool _continueOvertime;
    private TimerEndAction _selectedEndAction;
    private bool _autoStartOnFullscreen;
    private bool _stopWhenLeavingFullscreen;
    private bool _resetWhenLeavingFullscreen;
    private bool _flashOnPauseResume;
    private bool _flashPausedTime;
    private string _fontSizeText;
    private string _widthText;
    private string _heightText;
    private string _backgroundOpacityText;
    private string _textOpacityText;
    private bool _alwaysOnTop;
    private bool _borderless;
    private string _startPauseHotkey;
    private string _stopResetHotkey;
    private string _toggleWindowHotkey;
    private bool _minimizeToTray;
    private bool _checkOnStartup;
    private string _colorScheme;
    private string _textColor;
    private string _backgroundColor;
    private string _timeoutTextColor;
    private string _timeoutBackgroundColor;
    private string _flashBackgroundColor;
    private string _shape;
    private string _overtimePrefix;
    private bool _timerWindowVisible;
    private bool _showOnAllScreens;
    private string _targetScreen;
    private bool _bigScreenEnabled;
    private string _bigScreenTarget;
    private OverlayAnchor _anchor;
    private string _offsetX;
    private string _offsetY;
    private bool _clickThrough;
    private bool _lockPosition;
    private CloseButtonBehavior _closeButtonBehavior;
    private bool _remoteEnabled;
    private bool _remoteRandomPort;
    private string _remotePort;
    private string _language;
    private bool _isDirty;
    private string _errorMessage = "";

    public SettingsViewModel(AppConfig config, ConfigService configService, IReadOnlyList<SettingsChoice<string>>? screens = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _originalDefaultDuration = config.Timer.DefaultDuration;

        _defaultDuration = config.Timer.DefaultDuration;
        _selectedTimerMode = config.Timer.Mode;
        _continueOvertime = config.Timer.ContinueOvertime;
        _selectedEndAction = config.Timer.EndAction;
        _autoStartOnFullscreen = config.Behavior.AutoStartOnFullscreen;
        _stopWhenLeavingFullscreen = config.Behavior.StopWhenLeavingFullscreen;
        _resetWhenLeavingFullscreen = config.Behavior.ResetWhenLeavingFullscreen;
        _flashOnPauseResume = config.Behavior.FlashOnPauseResume;
        _flashPausedTime = config.Behavior.FlashPausedTime;
        _fontSizeText = config.Appearance.FontSize.ToString(CultureInfo.InvariantCulture);
        _widthText = config.Appearance.Width.ToString(CultureInfo.InvariantCulture);
        _heightText = config.Appearance.Height.ToString(CultureInfo.InvariantCulture);
        _backgroundOpacityText = config.Appearance.BackgroundOpacity.ToString(CultureInfo.InvariantCulture);
        _textOpacityText = config.Appearance.TextOpacity.ToString(CultureInfo.InvariantCulture);
        _alwaysOnTop = config.Appearance.AlwaysOnTop;
        _borderless = config.Appearance.Borderless;
        _startPauseHotkey = config.Controls.StartPauseHotkey;
        _stopResetHotkey = config.Controls.StopResetHotkey;
        _toggleWindowHotkey = config.Controls.ToggleWindowHotkey;
        _minimizeToTray = config.Controls.MinimizeToTray;
        _checkOnStartup = config.Update.CheckOnStartup;
        _colorScheme = config.Appearance.ColorScheme;
        _textColor = config.Appearance.TextColor;
        _backgroundColor = config.Appearance.BackgroundColor;
        _timeoutTextColor = config.Appearance.TimeoutTextColor;
        _timeoutBackgroundColor = config.Appearance.TimeoutBackgroundColor;
        _flashBackgroundColor = config.Appearance.FlashBackgroundColor;
        _shape = config.Appearance.Shape;
        _overtimePrefix = config.Appearance.OvertimePrefix;
        _timerWindowVisible = config.Placement.Visible;
        _showOnAllScreens = config.Placement.ShowOnAllScreens;
        _targetScreen = config.Placement.TargetScreenDeviceName;
        _bigScreenEnabled = config.Placement.BigScreenEnabled;
        _bigScreenTarget = config.Placement.BigScreenDeviceName;
        _anchor = config.Placement.Anchor;
        _offsetX = config.Placement.OffsetXPercent.ToString(CultureInfo.InvariantCulture);
        _offsetY = config.Placement.OffsetYPercent.ToString(CultureInfo.InvariantCulture);
        _clickThrough = config.Controls.ClickThrough;
        _lockPosition = config.Controls.LockPosition;
        _closeButtonBehavior = config.Controls.CloseButtonBehavior;
        _remoteEnabled = config.RemoteControl.Enabled;
        _remoteRandomPort = config.RemoteControl.UseRandomPort;
        _remotePort = config.RemoteControl.Port.ToString(CultureInfo.InvariantCulture);
        _language = config.Language;
        ScreenOptions = screens is { Count: > 0 } ? screens : [new("", "主屏幕")];
        ExtendedScreenOptions = ScreenOptions.Where(item => item.Value.Length > 0 && item.Label != "主屏幕").ToArray();
        if (ExtendedScreenOptions.Count == 0)
        {
            _bigScreenEnabled = false;
            _bigScreenTarget = "";
        }

        Prompt1 = new PromptDraft(config.Behavior.Prompt1, MarkDirty);
        Prompt2 = new PromptDraft(config.Behavior.Prompt2, MarkDirty);
        EndPrompt = new PromptDraft(config.Behavior.EndPrompt, MarkDirty);
        Rules = new ObservableCollection<FileRuleDraft>(config.Rules.Select(rule => new FileRuleDraft(rule, MarkDirty)));
        Hotkeys = new ObservableCollection<HotkeyDraft>(HotkeyDefinitions.Select(item =>
            new HotkeyDraft(item.Key, Localization.T(item.Label), config.Controls.Hotkeys.GetValueOrDefault(item.Key, ""), MarkDirty)));

        SaveAndCloseCommand = new RelayCommand(() =>
        {
            if (TrySave()) CloseRequested?.Invoke(false);
        });
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(true));
        RemoveSelectedRulesCommand = new RelayCommand(RemoveSelectedRules);
        ClearRulesCommand = new RelayCommand(() => { if (Rules.Count == 0) return; Rules.Clear(); MarkDirty(); });
        RegenerateTokenCommand = new RelayCommand(() => { _config.RemoteControl.Token = ConfigService.GenerateToken(); MarkDirty(); });
    }

    public IReadOnlyList<SettingsChoice<TimerMode>> TimerModeOptions { get; } =
    [
        new(TimerMode.Countdown, Localization.T("倒计时")),
        new(TimerMode.CountUp, Localization.T("正计时"))
    ];

    public IReadOnlyList<SettingsChoice<TimerEndAction>> EndActionOptions { get; } =
    [
        new(TimerEndAction.None, Localization.T("无")),
        new(TimerEndAction.BlackScreen, Localization.T("黑屏")),
        new(TimerEndAction.ExitSlideShow, Localization.T("结束放映"))
    ];

    public ICommand SaveAndCloseCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RemoveSelectedRulesCommand { get; }
    public ICommand ClearRulesCommand { get; }
    public ICommand RegenerateTokenCommand { get; }
    public event Action<bool>? CloseRequested;
    public Func<string, int, bool>? SyncRuleDurationsRequested { get; set; }

    public string DefaultDuration { get => _defaultDuration; set => Set(ref _defaultDuration, value); }
    public TimerMode SelectedTimerMode { get => _selectedTimerMode; set => Set(ref _selectedTimerMode, value); }
    public bool ContinueOvertime { get => _continueOvertime; set => Set(ref _continueOvertime, value); }
    public TimerEndAction SelectedEndAction { get => _selectedEndAction; set => Set(ref _selectedEndAction, value); }
    public bool AutoStartOnFullscreen { get => _autoStartOnFullscreen; set => Set(ref _autoStartOnFullscreen, value); }
    public bool StopWhenLeavingFullscreen { get => _stopWhenLeavingFullscreen; set => Set(ref _stopWhenLeavingFullscreen, value); }
    public bool ResetWhenLeavingFullscreen { get => _resetWhenLeavingFullscreen; set => Set(ref _resetWhenLeavingFullscreen, value); }
    public bool FlashOnPauseResume { get => _flashOnPauseResume; set => Set(ref _flashOnPauseResume, value); }
    public bool FlashPausedTime { get => _flashPausedTime; set => Set(ref _flashPausedTime, value); }
    public string FontSizeText { get => _fontSizeText; set => Set(ref _fontSizeText, value); }
    public string WidthText { get => _widthText; set => Set(ref _widthText, value); }
    public string HeightText { get => _heightText; set => Set(ref _heightText, value); }
    public string BackgroundOpacityText { get => _backgroundOpacityText; set => Set(ref _backgroundOpacityText, value); }
    public string TextOpacityText { get => _textOpacityText; set => Set(ref _textOpacityText, value); }
    public bool AlwaysOnTop { get => _alwaysOnTop; set => Set(ref _alwaysOnTop, value); }
    public bool Borderless { get => _borderless; set => Set(ref _borderless, value); }
    public string StartPauseHotkey { get => _startPauseHotkey; set => Set(ref _startPauseHotkey, value); }
    public string StopResetHotkey { get => _stopResetHotkey; set => Set(ref _stopResetHotkey, value); }
    public string ToggleWindowHotkey { get => _toggleWindowHotkey; set => Set(ref _toggleWindowHotkey, value); }
    public bool MinimizeToTray { get => _minimizeToTray; set => Set(ref _minimizeToTray, value); }
    public bool CheckOnStartup { get => _checkOnStartup; set => Set(ref _checkOnStartup, value); }
    public string ColorScheme { get => _colorScheme; set => Set(ref _colorScheme, value); }
    public string TextColor { get => _textColor; set => Set(ref _textColor, value); }
    public string BackgroundColor { get => _backgroundColor; set => Set(ref _backgroundColor, value); }
    public string TimeoutTextColor { get => _timeoutTextColor; set => Set(ref _timeoutTextColor, value); }
    public string TimeoutBackgroundColor { get => _timeoutBackgroundColor; set => Set(ref _timeoutBackgroundColor, value); }
    public string FlashBackgroundColor { get => _flashBackgroundColor; set => Set(ref _flashBackgroundColor, value); }
    public string Shape { get => _shape; set => Set(ref _shape, value); }
    public string OvertimePrefix { get => _overtimePrefix; set => Set(ref _overtimePrefix, value); }
    public bool TimerWindowVisible { get => _timerWindowVisible; set => Set(ref _timerWindowVisible, value); }
    public bool ShowOnAllScreens { get => _showOnAllScreens; set => Set(ref _showOnAllScreens, value); }
    public string TargetScreen { get => _targetScreen; set => Set(ref _targetScreen, value); }
    public bool BigScreenEnabled { get => _bigScreenEnabled; set => Set(ref _bigScreenEnabled, value); }
    public string BigScreenTarget { get => _bigScreenTarget; set => Set(ref _bigScreenTarget, value); }
    public OverlayAnchor Anchor { get => _anchor; set => Set(ref _anchor, value); }
    public string OffsetX { get => _offsetX; set => Set(ref _offsetX, value); }
    public string OffsetY { get => _offsetY; set => Set(ref _offsetY, value); }
    public bool ClickThrough { get => _clickThrough; set => Set(ref _clickThrough, value); }
    public bool LockPosition { get => _lockPosition; set => Set(ref _lockPosition, value); }
    public CloseButtonBehavior CloseButtonBehavior { get => _closeButtonBehavior; set => Set(ref _closeButtonBehavior, value); }
    public bool RemoteEnabled { get => _remoteEnabled; set => Set(ref _remoteEnabled, value); }
    public bool RemoteRandomPort { get => _remoteRandomPort; set => Set(ref _remoteRandomPort, value); }
    public string RemotePort { get => _remotePort; set => Set(ref _remotePort, value); }
    public string Language { get => _language; set => Set(ref _language, value); }
    public PromptDraft Prompt1 { get; }
    public PromptDraft Prompt2 { get; }
    public PromptDraft EndPrompt { get; }
    public ObservableCollection<FileRuleDraft> Rules { get; }
    public ObservableCollection<HotkeyDraft> Hotkeys { get; }
    public IReadOnlyList<string> ColorSchemes { get; } = AppearancePresetService.Names;
    public IReadOnlyList<string> Shapes { get; } = ["直角矩形", "圆角矩形（小）", "圆角矩形（大）"];
    public IReadOnlyList<string> FlashStyles { get; } = ["无", "闪烁文字", "闪烁背景", "实线边框", "边框加背景"];
    public IReadOnlyList<SettingsChoice<OverlayAnchor>> AnchorOptions { get; } =
    [
        new(OverlayAnchor.TopLeft, Localization.T("左上")), new(OverlayAnchor.TopCenter, Localization.T("上中")), new(OverlayAnchor.TopRight, Localization.T("右上")),
        new(OverlayAnchor.MiddleLeft, Localization.T("左中")), new(OverlayAnchor.Center, Localization.T("正中")), new(OverlayAnchor.MiddleRight, Localization.T("右中")),
        new(OverlayAnchor.BottomLeft, Localization.T("左下")), new(OverlayAnchor.BottomCenter, Localization.T("下中")), new(OverlayAnchor.BottomRight, Localization.T("右下"))
    ];
    public IReadOnlyList<TimerMode> TimerModes { get; } = [TimerMode.Countdown, TimerMode.CountUp];
    public IReadOnlyList<SettingsChoice<CloseButtonBehavior>> CloseBehaviorOptions { get; } =
    [new(CloseButtonBehavior.Exit, Localization.T("退出程序")), new(CloseButtonBehavior.MinimizeToTray, Localization.T("最小化到托盘"))];
    public IReadOnlyList<SettingsChoice<string>> LanguageOptions { get; } =
    [new(Localization.Auto, Localization.T("跟随系统")), new(Localization.English, "English"), new(Localization.SimplifiedChinese, Localization.T("简体中文"))];
    public IReadOnlyList<SettingsChoice<string>> ScreenOptions { get; }
    public IReadOnlyList<SettingsChoice<string>> ExtendedScreenOptions { get; }
    public bool IsBigScreenAvailable => ExtendedScreenOptions.Count > 0;

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (_isDirty == value) return;
            _isDirty = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UnsavedStatus));
        }
    }

    public string UnsavedStatus => IsDirty ? "有未保存的更改" : "所有更改已保存";

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (string.Equals(_errorMessage, value, StringComparison.Ordinal)) return;
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool TrySave()
    {
        ErrorMessage = "";
        if (!TryParseDuration(DefaultDuration, out var duration) || duration <= TimeSpan.Zero)
            return Fail("默认时长必须是大于零的有效 hh:mm:ss / TimeSpan 值。");
        if (!TryParseFloat(FontSizeText, out var fontSize) || fontSize is < 8 or > 96)
            return Fail("字体大小必须是 8–96 之间的数字。");
        if (!TryParseInt(WidthText, out var width) || width is < 40 or > 2000)
            return Fail("窗口宽度必须是 40–2000 之间的整数。");
        if (!TryParseInt(HeightText, out var height) || height is < 20 or > 1000)
            return Fail("窗口高度必须是 20–1000 之间的整数。");
        if (!TryParseInt(BackgroundOpacityText, out var backgroundOpacity) || backgroundOpacity is < 0 or > 100)
            return Fail("背景透明度必须是 0–100 之间的整数。");
        if (!TryParseInt(TextOpacityText, out var textOpacity) || textOpacity is < 0 or > 100)
            return Fail("文字透明度必须是 0–100 之间的整数。");
        if (!TryParseDecimal(OffsetX, out var offsetX) || offsetX is < -50 or > 50
            || !TryParseDecimal(OffsetY, out var offsetY) || offsetY is < -50 or > 50)
            return Fail("窗口位置微调必须是 -50–50 之间的数字。");
        if (!TryParseInt(RemotePort, out var remotePort) || remotePort is < 1 or > 65535)
            return Fail("远程控制端口必须是 1–65535 之间的整数。");
        if (!ValidatePrompt(Prompt1, "提示 1", true, out var promptError)
            || !ValidatePrompt(Prompt2, "提示 2", true, out promptError)
            || !ValidatePrompt(EndPrompt, "计时结束", false, out promptError))
            return Fail(promptError);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in Rules)
        {
            if (!TryParseDuration(rule.Duration, out var ruleDuration) || ruleDuration <= TimeSpan.Zero)
                return Fail($"文件规则“{rule.FileName}”的计时时长无效。");
            var key = NormalizePath(rule.FilePath);
            if (string.IsNullOrWhiteSpace(key) || !paths.Add(key))
                return Fail("文件规则中不能包含空路径或重复添加同一份演示文稿。");
        }
        var hotkeyValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var hotkey in Hotkeys.Where(item => !string.IsNullOrWhiteSpace(item.Value)))
            if (!hotkeyValues.Add(hotkey.Value.Trim())) return Fail($"快捷键“{hotkey.Value.Trim()}”重复，请重新设置。");
        var syncRuleDurations = Rules.Count > 0
            && !string.Equals(_originalDefaultDuration, DefaultDuration.Trim(), StringComparison.Ordinal)
            && SyncRuleDurationsRequested?.Invoke(DefaultDuration.Trim(), Rules.Count) == true;

        _config.Timer.DefaultDuration = DefaultDuration.Trim();
        _config.Timer.Mode = SelectedTimerMode;
        _config.Timer.ContinueOvertime = ContinueOvertime;
        _config.Timer.EndAction = SelectedEndAction;
        _config.Behavior.AutoStartOnFullscreen = AutoStartOnFullscreen;
        _config.Behavior.StopWhenLeavingFullscreen = StopWhenLeavingFullscreen;
        _config.Behavior.ResetWhenLeavingFullscreen = ResetWhenLeavingFullscreen;
        _config.Behavior.FlashOnPauseResume = FlashOnPauseResume;
        _config.Behavior.FlashPausedTime = FlashPausedTime;
        _config.Appearance.FontSize = fontSize;
        _config.Appearance.Width = width;
        _config.Appearance.Height = height;
        _config.Appearance.BackgroundOpacity = backgroundOpacity;
        _config.Appearance.TextOpacity = textOpacity;
        _config.Appearance.AlwaysOnTop = AlwaysOnTop;
        _config.Appearance.Borderless = Borderless;
        _config.Controls.StartPauseHotkey = StartPauseHotkey.Trim();
        _config.Controls.StopResetHotkey = StopResetHotkey.Trim();
        _config.Controls.ToggleWindowHotkey = ToggleWindowHotkey.Trim();
        _config.Controls.MinimizeToTray = MinimizeToTray;
        _config.Update.CheckOnStartup = CheckOnStartup;
        _config.Appearance.ColorScheme = ColorScheme;
        _config.Appearance.TextColor = TextColor.Trim();
        _config.Appearance.BackgroundColor = BackgroundColor.Trim();
        _config.Appearance.TimeoutTextColor = TimeoutTextColor.Trim();
        _config.Appearance.TimeoutBackgroundColor = TimeoutBackgroundColor.Trim();
        _config.Appearance.FlashBackgroundColor = FlashBackgroundColor.Trim();
        _config.Appearance.Shape = Shape;
        _config.Appearance.OvertimePrefix = OvertimePrefix;
        _config.Placement.Visible = TimerWindowVisible;
        _config.Placement.ShowOnAllScreens = ShowOnAllScreens;
        _config.Placement.TargetScreenDeviceName = TargetScreen.Trim();
        _config.Placement.BigScreenEnabled = BigScreenEnabled;
        _config.Placement.BigScreenDeviceName = BigScreenTarget.Trim();
        _config.Placement.Anchor = Anchor;
        _config.Placement.OffsetXPercent = offsetX;
        _config.Placement.OffsetYPercent = offsetY;
        _config.Placement.HasCustomPlacement = true;
        _config.Controls.ClickThrough = ClickThrough;
        _config.Controls.LockPosition = LockPosition;
        _config.Controls.CloseButtonBehavior = CloseButtonBehavior;
        _config.Controls.Hotkeys = Hotkeys.ToDictionary(item => item.Key, item => item.Value.Trim(), StringComparer.OrdinalIgnoreCase);
        _config.Controls.Hotkeys["startPause"] = _config.Controls.StartPauseHotkey;
        _config.Controls.Hotkeys["stopReset"] = _config.Controls.StopResetHotkey;
        _config.Controls.Hotkeys["toggleWindow"] = _config.Controls.ToggleWindowHotkey;
        _config.RemoteControl.Enabled = RemoteEnabled;
        _config.RemoteControl.UseRandomPort = RemoteRandomPort;
        _config.RemoteControl.Port = remotePort;
        _config.Language = Language;
        ApplyPrompt(Prompt1, _config.Behavior.Prompt1, true);
        ApplyPrompt(Prompt2, _config.Behavior.Prompt2, true);
        ApplyPrompt(EndPrompt, _config.Behavior.EndPrompt, false);
        _config.Rules = Rules.Select(rule =>
        {
            var model = rule.ToModel();
            if (syncRuleDurations) model.Duration = DefaultDuration.Trim();
            return model;
        }).ToList();

        try
        {
            _configService.Save(_config);
            IsDirty = false;
            return true;
        }
        catch (Exception ex)
        {
            return Fail($"保存配置失败：{ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Fail(string message)
    {
        ErrorMessage = message;
        return false;
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
        IsDirty = true;
        ErrorMessage = "";
    }

    public void AddRulePaths(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            if (Rules.Any(rule => string.Equals(NormalizePath(rule.FilePath), NormalizePath(path), StringComparison.OrdinalIgnoreCase))) continue;
            Rules.Add(new FileRuleDraft(new FileRule
            {
                FileName = Path.GetFileName(path), FilePath = path, Duration = DefaultDuration, Mode = SelectedTimerMode, Enabled = true
            }, MarkDirty));
            MarkDirty();
        }
    }

    public void ApplyBatchToSelectedRules(string duration, TimerMode mode)
    {
        foreach (var rule in Rules.Where(rule => rule.Selected))
        {
            rule.Duration = duration;
            rule.Mode = mode;
        }
    }

    public async Task<bool> ImportAsync(string path)
    {
        try
        {
            await Task.Run(() => _configService.Import(path));
            CloseRequested?.Invoke(false);
            return true;
        }
        catch (Exception ex) { return Fail($"导入配置失败：{ex.Message}"); }
    }

    public async Task<bool> ExportAsync(string path)
    {
        if (!TrySave()) return false;
        try { await Task.Run(() => _configService.Export(_config, path)); return true; }
        catch (Exception ex) { return Fail($"导出配置失败：{ex.Message}"); }
    }

    public async Task<bool> ResetAsync()
    {
        try
        {
            await Task.Run(() => _configService.Save(new AppConfig()));
            CloseRequested?.Invoke(false);
            return true;
        }
        catch (Exception ex) { return Fail($"恢复默认配置失败：{ex.Message}"); }
    }

    public string ConfigDirectory => Path.GetDirectoryName(_configService.ConfigPath) ?? AppContext.BaseDirectory;
    public string LogDirectory => AppPaths.LogDirectory;

    private void RemoveSelectedRules()
    {
        foreach (var rule in Rules.Where(rule => rule.Selected).ToArray()) Rules.Remove(rule);
        MarkDirty();
    }

    private void MarkDirty()
    {
        IsDirty = true;
        ErrorMessage = "";
    }

    private static bool ValidatePrompt(PromptDraft draft, string label, bool validateBefore, out string error)
    {
        if (validateBefore && (!TryParseInt(draft.TriggerBeforeEndSeconds, out var before) || before is < 0 or > 99999))
        { error = $"{label}的提前秒数必须是 0–99999 之间的整数。"; return false; }
        if (!TryParseInt(draft.FlashOnMs, out var on) || on is < 50 or > 5000
            || !TryParseInt(draft.FlashOffMs, out var off) || off is < 50 or > 5000
            || !TryParseInt(draft.FlashSeconds, out var seconds) || seconds is < 0 or > 120)
        { error = $"{label}的闪烁参数超出允许范围。"; return false; }
        error = "";
        return true;
    }

    private static void ApplyPrompt(PromptDraft draft, PromptSettings prompt, bool applyBefore)
    {
        prompt.Enabled = draft.Enabled;
        if (applyBefore) prompt.TriggerBeforeEndSeconds = int.Parse(draft.TriggerBeforeEndSeconds, CultureInfo.InvariantCulture);
        prompt.Speak = draft.Speak;
        prompt.Beep = false;
        prompt.SoundFile = draft.SoundFile.Trim();
        prompt.PlaySound = !string.IsNullOrWhiteSpace(prompt.SoundFile);
        prompt.FlashStyle = draft.FlashStyle.Replace("边框加背景", "边框+背景", StringComparison.Ordinal);
        prompt.FlashOnMs = int.Parse(draft.FlashOnMs, CultureInfo.InvariantCulture);
        prompt.FlashOffMs = int.Parse(draft.FlashOffMs, CultureInfo.InvariantCulture);
        prompt.FlashSeconds = int.Parse(draft.FlashSeconds, CultureInfo.InvariantCulture);
        prompt.FlashText = prompt.FlashStyle.Contains("文字", StringComparison.Ordinal);
        prompt.FlashBackground = prompt.FlashStyle != "无";
    }

    private static string NormalizePath(string path)
    {
        try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch { return path.Trim(); }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static bool TryParseDuration(string value, out TimeSpan duration) =>
        TimeSpan.TryParse(value, CultureInfo.CurrentCulture, out duration)
        || TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out duration);

    private static bool TryParseFloat(string value, out float number) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out number)
        || float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);

    private static bool TryParseInt(string value, out int number) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out number)
        || int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);

    private static bool TryParseDecimal(string value, out decimal number) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out number)
        || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out number);

    private static readonly (string Key, string Label)[] HotkeyDefinitions =
    [
        ("startPause", "开始/暂停"), ("start", "开始"), ("pause", "暂停"), ("resume", "继续"),
        ("stopReset", "停止/重置"), ("stop", "停止"), ("reset", "重置"),
        ("toggleWindow", "显示/隐藏窗口"), ("showWindow", "显示窗口"), ("hideWindow", "隐藏窗口"),
        ("flash", "触发闪烁"), ("toggleMute", "静音/取消静音"), ("toggleMode", "切换倒计时/正计时"),
        ("addMinute", "增加 1 分钟"), ("subtractMinute", "减少 1 分钟"),
        ("preset3", "设置为 3 分钟"), ("preset5", "设置为 5 分钟"), ("preset8", "设置为 8 分钟"),
        ("preset10", "设置为 10 分钟"), ("preset15", "设置为 15 分钟")
    ];

    private sealed class RelayCommand(Action execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
