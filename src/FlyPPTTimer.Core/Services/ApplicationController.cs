using System.Text.Json;
using FlyPPTTimer.Core.Abstractions;
using FlyPPTTimer.Core.Models;

namespace FlyPPTTimer.Core.Services;

public sealed class ApplicationController : IDisposable
{
    private readonly IConfigService _configService;
    private readonly ILogService _log;
    private readonly IPowerPointControlService _powerPoint;
    private readonly Timer _presentationPoll;
    private PresentationLifecycleController _lifecycle;

    public ApplicationController(IConfigService configService, ILogService log, IPowerPointControlService powerPoint)
    {
        _configService = configService; _log = log; _powerPoint = powerPoint;
        Config = configService.Load(); Timer = new(); Timer.Configure(Config); Rules = new(Config); State = new(Config, Timer.Snapshot());
        _lifecycle = new(Config, Timer, Rules);
        Timer.Updated += (_, value) => State.UpdateTimer(value);
        _powerPoint.SlideShowStarted += (_, path) => _lifecycle.Observe(true, path);
        _powerPoint.SlideShowEnded += (_, _) => _lifecycle.Observe(false, "");
        _presentationPoll = new Timer(_ => RefreshPresentation(), null, 0, 500);
    }

    public AppConfig Config { get; private set; }
    public TimerService Timer { get; }
    public FileRuleService Rules { get; private set; }
    public ApplicationStateStore State { get; }
    public IPowerPointControlService PowerPoint => _powerPoint;

    public AppConfig CreateEditableConfig() => JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(Config)) ?? new();
    public void ApplyConfig(AppConfig config)
    {
        Validate(config); _configService.Save(config); Config = config; Rules = new(config); _lifecycle = new(config, Timer, Rules); Timer.Configure(config); State.UpdateConfig(config); State.UpdateMessage("设置已应用");
    }
    public void SaveRules() { _configService.Save(Config); State.UpdateConfig(Config); }
    public void ImportConfig(string path) => ApplyConfig(_configService.Import(path));
    public void ExportConfig(string path) => _configService.Export(Config, path);
    public void ResetConfig() => ApplyConfig(new());

    public bool ExecuteTimer(string command)
    {
        switch (command)
        {
            case "timer.start": Timer.Start(); break;
            case "timer.pause": Timer.Pause(); break;
            case "timer.resume": Timer.Resume(); break;
            case "timer.stop": Timer.Stop(true); break;
            case "timer.reset": Timer.Reset(); break;
            case "timer.toggle": Timer.StartOrPause(); break;
            case "mute.toggle": State.ToggleMuted(); break;
            default: return false;
        }
        return true;
    }

    public async Task<PresentationCommandResult> ExecutePresentationAsync(PresentationCommand command, CancellationToken token = default)
    {
        var result = await _powerPoint.ExecuteAsync(command, token); State.UpdatePresentation(result.State); State.UpdateMessage(result.Message); return result;
    }

    private void RefreshPresentation()
    {
        try { State.UpdatePresentation(_powerPoint.GetState()); }
        catch (Exception ex) { _log.Error("PowerPoint 状态刷新失败。", ex); }
    }
    private static void Validate(AppConfig config)
    {
        if (!TimeSpan.TryParse(config.Timer.DefaultDuration, out var duration) || duration <= TimeSpan.Zero) throw new InvalidDataException("默认计时时长无效。");
        foreach (var rule in config.Rules) if (!TimeSpan.TryParseExact(rule.Duration, @"hh\:mm\:ss", null, out var value) || value <= TimeSpan.Zero) throw new InvalidDataException($"{rule.FileName} 的计时时长无效。");
    }
    public void Dispose() { _presentationPoll.Dispose(); Timer.Dispose(); _powerPoint.Dispose(); }
}
