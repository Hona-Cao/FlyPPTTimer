using FlyPPTTimer.Core.Models;

namespace FlyPPTTimer.Core.Services;

public sealed class PresentationLifecycleController(AppConfig config, TimerService timer, FileRuleService rules)
{
    private bool _showing;
    private bool _automationActive;
    private string _path = "";
    public void Observe(bool showing, string path)
    {
        path ??= "";
        if (showing)
        {
            if (_showing && FileRuleService.SamePath(_path, path)) return;
            _showing = true; _path = path;
            if (!config.Behavior.AutoStartOnFullscreen) { _automationActive = false; return; }
            timer.SetDuration(rules.ResolveDuration(path)); timer.Stop(true); timer.Start(); _automationActive = true; return;
        }
        if (!_showing) return;
        _showing = false; _path = "";
        if (!_automationActive) return;
        if (config.Behavior.StopWhenLeavingFullscreen) timer.Stop(config.Behavior.ResetWhenLeavingFullscreen);
        else if (config.Behavior.ResetWhenLeavingFullscreen) timer.Reset();
        _automationActive = false;
    }
}
