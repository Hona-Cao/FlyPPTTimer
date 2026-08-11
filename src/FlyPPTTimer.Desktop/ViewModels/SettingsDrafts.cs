using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Desktop.ViewModels;

public abstract class SettingsDraft : INotifyPropertyChanged
{
    private readonly Action _changed;

    protected SettingsDraft(Action changed) => _changed = changed;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        _changed();
    }
}

public sealed class PromptDraft : SettingsDraft
{
    private bool _enabled;
    private string _before;
    private bool _speak;
    private string _soundFile;
    private string _flashStyle;
    private string _flashOn;
    private string _flashOff;
    private string _flashSeconds;

    public PromptDraft(PromptSettings prompt, Action changed) : base(changed)
    {
        _enabled = prompt.Enabled;
        _before = prompt.TriggerBeforeEndSeconds.ToString();
        _speak = prompt.Speak;
        _soundFile = prompt.SoundFile;
        _flashStyle = prompt.FlashStyle == "边框+背景" ? "边框加背景" : prompt.FlashStyle;
        _flashOn = prompt.FlashOnMs.ToString();
        _flashOff = prompt.FlashOffMs.ToString();
        _flashSeconds = prompt.FlashSeconds.ToString();
    }

    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }
    public string TriggerBeforeEndSeconds { get => _before; set => Set(ref _before, value); }
    public bool Speak { get => _speak; set => Set(ref _speak, value); }
    public string SoundFile { get => _soundFile; set => Set(ref _soundFile, value); }
    public string FlashStyle { get => _flashStyle; set => Set(ref _flashStyle, value); }
    public string FlashOnMs { get => _flashOn; set => Set(ref _flashOn, value); }
    public string FlashOffMs { get => _flashOff; set => Set(ref _flashOff, value); }
    public string FlashSeconds { get => _flashSeconds; set => Set(ref _flashSeconds, value); }
}

public sealed class FileRuleDraft : SettingsDraft
{
    private string _fileName;
    private string _filePath;
    private string _duration;
    private TimerMode _mode;
    private bool _enabled;
    private bool _selected;

    public FileRuleDraft(FileRule rule, Action changed) : base(changed)
    {
        _fileName = rule.FileName;
        _filePath = rule.FilePath;
        _duration = rule.Duration;
        _mode = rule.Mode;
        _enabled = rule.Enabled;
        TitlePattern = rule.TitlePattern;
        Feature = rule.Feature;
        ExtensionData = rule.ExtensionData;
    }

    public string FileName { get => _fileName; set => Set(ref _fileName, value); }
    public string FilePath { get => _filePath; set => Set(ref _filePath, value); }
    public string Duration { get => _duration; set => Set(ref _duration, value); }
    public TimerMode Mode { get => _mode; set => Set(ref _mode, value); }
    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }
    public bool Selected { get => _selected; set => Set(ref _selected, value); }
    public string TitlePattern { get; }
    public string Feature { get; }
    public Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; }

    public FileRule ToModel() => new()
    {
        FileName = FileName,
        FilePath = FilePath,
        Duration = Duration,
        Mode = Mode,
        Enabled = Enabled,
        TitlePattern = TitlePattern,
        Feature = Feature,
        ExtensionData = ExtensionData
    };
}

public sealed class HotkeyDraft : SettingsDraft
{
    private string _value;

    public HotkeyDraft(string key, string label, string value, Action changed) : base(changed)
    {
        Key = key;
        Label = label;
        _value = value;
    }

    public string Key { get; }
    public string Label { get; }
    public string Value { get => _value; set => Set(ref _value, value); }
}
