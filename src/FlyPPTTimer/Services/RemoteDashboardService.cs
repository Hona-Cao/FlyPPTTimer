using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

/// <summary>
/// Application boundary for the local remote-control dashboard. It coordinates
/// configuration, HTTP lifecycle, address discovery and presentation commands;
/// WPF controls do not operate listeners, files, processes or COM directly.
/// </summary>
public sealed class RemoteDashboardService
{
    private readonly Func<AppConfig> _getConfig;
    private readonly Action<AppConfig> _saveConfig;
    private readonly RemoteControlService _remote;
    private readonly NetworkAddressService _addresses;

    public RemoteDashboardService(
        Func<AppConfig> getConfig,
        Action<AppConfig> saveConfig,
        RemoteControlService remote,
        NetworkAddressService addresses)
    {
        _getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
        _saveConfig = saveConfig ?? throw new ArgumentNullException(nameof(saveConfig));
        _remote = remote ?? throw new ArgumentNullException(nameof(remote));
        _addresses = addresses ?? throw new ArgumentNullException(nameof(addresses));
    }

    public RemoteDashboardSnapshot GetSnapshot() => new(
        ConfigService.Clone(_getConfig()),
        _remote.IsRunning,
        _remote.StatusText,
        _remote.CurrentPort,
        _remote.ConnectedClients,
        _addresses.GetIPv4Addresses(),
        _remote.PresentationCommands?.GetState() ?? new PresentationState
        {
            Error = "演示控制服务当前不可用。"
        });

    public void SetServiceEnabled(bool enabled)
    {
        var config = ConfigService.Clone(_getConfig());
        config.RemoteControl.Enabled = enabled;
        _saveConfig(config);
        if (enabled) _remote.Start();
        else _remote.Stop();
    }

    public bool TryApplyEndpoint(bool useRandomPort, int port, out string error)
    {
        if (!useRandomPort && port is < 1 or > 65535)
        {
            error = "端口必须在 1 到 65535 之间。";
            return false;
        }

        var config = ConfigService.Clone(_getConfig());
        config.RemoteControl.UseRandomPort = useRandomPort;
        if (!useRandomPort) config.RemoteControl.Port = port;
        _saveConfig(config);
        if (config.RemoteControl.Enabled) _remote.Restart();
        error = "";
        return true;
    }

    public void DisconnectAll() => _remote.DisconnectAll();

    public string BuildAccessUrl(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return "";
        var config = _getConfig();
        var port = _remote.CurrentPort > 0
            ? _remote.CurrentPort
            : Math.Clamp(config.RemoteControl.Port, 1, 65535);
        return $"http://{address}:{port}/?token={config.RemoteControl.Token}";
    }

    public int AddRules(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var config = ConfigService.Clone(_getConfig());
        var added = 0;
        foreach (var rawPath in paths)
        {
            var path = NormalizePath(rawPath);
            if (string.IsNullOrWhiteSpace(path)
                || config.Rules.Any(rule => SamePath(rule.FilePath, path))) continue;
            config.Rules.Add(new FileRule
            {
                FileName = Path.GetFileName(path),
                FilePath = path,
                Duration = config.Timer.DefaultDuration,
                Mode = config.Timer.Mode,
                Enabled = true
            });
            added++;
        }
        if (added > 0) _saveConfig(config);
        return added;
    }

    public bool TryUpdateRule(string path, string duration, TimerMode mode, bool enabled, out string error)
    {
        if (!PresentationRuleValidator.TryNormalizeDuration(duration, out var normalized, out error))
        {
            return false;
        }

        var config = ConfigService.Clone(_getConfig());
        var rule = config.Rules.FirstOrDefault(item => SamePath(item.FilePath, path));
        if (rule is null)
        {
            error = "请先选择。";
            return false;
        }
        rule.Duration = normalized;
        rule.Mode = mode;
        rule.Enabled = enabled;
        _saveConfig(config);
        error = "";
        return true;
    }

    public bool RemoveRule(string path)
    {
        var config = ConfigService.Clone(_getConfig());
        var removed = config.Rules.RemoveAll(rule => SamePath(rule.FilePath, path));
        if (removed > 0) _saveConfig(config);
        return removed > 0;
    }

    public void ClearRules()
    {
        var config = ConfigService.Clone(_getConfig());
        if (config.Rules.Count == 0) return;
        config.Rules.Clear();
        _saveConfig(config);
    }

    public void SaveWindowPlacement(RemoteWindowPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        var config = ConfigService.Clone(_getConfig());
        config.RemoteControl.Window = placement;
        _saveConfig(config);
    }

    public PresentationCommandResult Execute(PresentationCommand command) =>
        _remote.PresentationCommands?.Execute(command)
        ?? new PresentationCommandResult(false, "演示控制服务当前不可用。", GetSnapshot().PresentationState);

    public PresentationCommandResult Queue(PresentationCommand command) =>
        _remote.PresentationCommands?.Queue(command)
        ?? new PresentationCommandResult(false, "演示控制服务当前不可用。", GetSnapshot().PresentationState);

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        try { return Path.GetFullPath(path); }
        catch { return path.Trim(); }
    }

    private static bool SamePath(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
        try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
        catch { return string.Equals(left, right, StringComparison.OrdinalIgnoreCase); }
    }
}

public sealed record RemoteDashboardSnapshot(
    AppConfig Config,
    bool IsRunning,
    string StatusText,
    int CurrentPort,
    int ConnectedClients,
    IReadOnlyList<NetworkAddressInfo> Addresses,
    PresentationState PresentationState);
