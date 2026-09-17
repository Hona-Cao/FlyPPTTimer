using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

public sealed class PresentationLifecycleController(
    Func<AppConfig> getConfig,
    Action<string> applyPresentationDuration,
    Action resetAlerts,
    Action stopAndReset,
    Action start,
    Action<bool> stop,
    Action reset,
    LogService log)
{
    private bool _isShowing;
    private bool _automationActive;
    private string _presentationPath = "";

    public void Observe(bool isShowing, string presentationPath, string source)
    {
        presentationPath ??= "";
        if (isShowing)
        {
            if (_isShowing && SamePath(_presentationPath, presentationPath)) return;
            _isShowing = true;
            _presentationPath = presentationPath;
            if (!getConfig().Behavior.AutoStartOnFullscreen)
            {
                _automationActive = false;
                log.Info($"Presentation detected without automatic timer start ({source}).");
                return;
            }

            resetAlerts();
            applyPresentationDuration(presentationPath);
            stopAndReset();
            start();
            _automationActive = true;
            log.Info($"Presentation start applied from {source}: {presentationPath}");
            return;
        }

        if (!_isShowing) return;
        _isShowing = false;
        _presentationPath = "";
        if (!_automationActive) return;

        var behavior = getConfig().Behavior;
        if (behavior.StopWhenLeavingFullscreen) stop(behavior.ResetWhenLeavingFullscreen);
        else if (behavior.ResetWhenLeavingFullscreen) reset();
        _automationActive = false;
        log.Info($"Presentation end applied from {source}.");
    }

    private static bool SamePath(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return left == right;
        try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
        catch { return string.Equals(left, right, StringComparison.OrdinalIgnoreCase); }
    }
}
