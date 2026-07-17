using System.Security.Cryptography;
using System.Text.Json;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

public sealed class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly LogService _log;
    private readonly string _configPath;

    public ConfigService(LogService log, string? configPath = null)
    {
        _log = log;
        _configPath = configPath ?? AppPaths.ConfigPath;
    }

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
        {
            var defaults = new AppConfig();
            Save(defaults);
            _log.Info("Default config created.");
            return defaults;
        }

        try
        {
            var config = Deserialize(File.ReadAllText(_configPath));
            Normalize(config);
            Save(config);
            _log.Info("Config loaded.");
            return config;
        }
        catch (Exception ex)
        {
            BackupInvalidConfig();
            _log.Error("Config is invalid; attempting backup recovery.", ex);
            foreach (var backup in FindBackups())
            {
                try
                {
                    var recovered = Deserialize(File.ReadAllText(backup));
                    Normalize(recovered);
                    Save(recovered);
                    _log.Warn($"Config recovered from backup: {Path.GetFileName(backup)}");
                    return recovered;
                }
                catch (Exception backupEx)
                {
                    _log.Warn($"Config backup is invalid: {Path.GetFileName(backup)} ({backupEx.Message})");
                }
            }

            var defaults = new AppConfig();
            Save(defaults);
            _log.Warn("No valid config backup was found; defaults created.");
            return defaults;
        }
    }

    public void Save(AppConfig config)
    {
        Normalize(config);
        var directory = Path.GetDirectoryName(_configPath) ?? AppPaths.BaseDirectory;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        _ = Deserialize(json);
        var tempPath = Path.Combine(directory, $".{Path.GetFileName(_configPath)}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tempPath, json);
        _ = Deserialize(File.ReadAllText(tempPath));

        try
        {
            if (File.Exists(_configPath))
            {
                var backupPath = GetBackupPath();
                try { File.Replace(tempPath, _configPath, backupPath, true); }
                catch (PlatformNotSupportedException) { ReplaceWithMove(tempPath, backupPath); }
                catch (IOException) { ReplaceWithMove(tempPath, backupPath); }
            }
            else File.Move(tempPath, _configPath);
            PruneBackups();
            _log.Info("Config saved atomically.");
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public void Export(AppConfig config, string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(config, JsonOptions));
        _log.Info($"Config exported: {path}");
    }

    public AppConfig Import(string path)
    {
        var config = Deserialize(File.ReadAllText(path));
        Normalize(config);
        Save(config);
        _log.Info($"Config imported: {path}");
        return config;
    }

    private void ReplaceWithMove(string tempPath, string backupPath)
    {
        File.Copy(_configPath, backupPath, true);
        File.Move(tempPath, _configPath, true);
    }

    private AppConfig Deserialize(string json) => JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)
        ?? throw new InvalidDataException("Config JSON is empty.");

    private string GetBackupPath() => $"{_configPath}.backup.{DateTime.Now:yyyyMMddHHmmssfff}.json";

    private IEnumerable<string> FindBackups() => Directory.Exists(Path.GetDirectoryName(_configPath))
        ? Directory.GetFiles(Path.GetDirectoryName(_configPath)!, $"{Path.GetFileName(_configPath)}.backup.*.json").OrderByDescending(File.GetLastWriteTimeUtc)
        : [];

    private void BackupInvalidConfig()
    {
        try { if (File.Exists(_configPath)) File.Copy(_configPath, $"{_configPath}.{DateTime.Now:yyyyMMddHHmmss}.bad.json", true); }
        catch (Exception ex) { _log.Error("Failed to preserve invalid config.", ex); }
    }

    private void PruneBackups()
    {
        foreach (var file in FindBackups().Skip(5))
        {
            try { File.Delete(file); } catch { }
        }
    }

    internal static void Normalize(AppConfig config)
    {
        config.Version = AppVersion.Current;
        if (!config.Placement.HasCustomPlacement)
        {
            config.Placement.Anchor = OverlayAnchor.TopCenter;
            config.Placement.OffsetXPercent = 0;
            config.Placement.OffsetYPercent = 0.5m;
        }
        foreach (var rule in config.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.FileName) && !string.IsNullOrWhiteSpace(rule.FilePath)) rule.FileName = Path.GetFileName(rule.FilePath);
            if (string.IsNullOrWhiteSpace(rule.FilePath) && !string.IsNullOrWhiteSpace(rule.TitlePattern))
            {
                rule.FilePath = rule.TitlePattern;
                rule.FileName = Path.GetFileName(rule.TitlePattern);
            }
            if (string.IsNullOrWhiteSpace(rule.Duration)) rule.Duration = config.Timer.DefaultDuration;
        }
        config.Rules = config.Rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.FilePath))
            .GroupBy(rule => NormalizeRulePath(rule.FilePath), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        if (string.IsNullOrWhiteSpace(config.Controls.StartPauseHotkey) || config.Controls.StartPauseHotkey.Contains('+')) config.Controls.StartPauseHotkey = "F3";
        if (string.IsNullOrWhiteSpace(config.Controls.StopResetHotkey) || config.Controls.StopResetHotkey.Contains('+')) config.Controls.StopResetHotkey = "F4";
        if (string.IsNullOrWhiteSpace(config.Controls.ToggleWindowHotkey) || config.Controls.ToggleWindowHotkey.Contains('+')) config.Controls.ToggleWindowHotkey = "F5";
        if (string.IsNullOrWhiteSpace(config.Controls.OpenSettingsHotkey) || config.Controls.OpenSettingsHotkey.Contains('+')) config.Controls.OpenSettingsHotkey = "F6";
        config.Controls.Hotkeys ??= ControlSettings.DefaultHotkeys();
        foreach (var pair in ControlSettings.DefaultHotkeys()) if (!config.Controls.Hotkeys.ContainsKey(pair.Key)) config.Controls.Hotkeys[pair.Key] = pair.Value;
        config.Controls.Hotkeys["startPause"] = config.Controls.StartPauseHotkey;
        config.Controls.Hotkeys["stopReset"] = config.Controls.StopResetHotkey;
        config.Controls.Hotkeys["toggleWindow"] = config.Controls.ToggleWindowHotkey;
        config.Controls.Hotkeys["openSettings"] = config.Controls.OpenSettingsHotkey;
        if (string.IsNullOrWhiteSpace(config.RemoteControl.Token)) config.RemoteControl.Token = GenerateToken();
    }

    public static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizeRulePath(string path)
    {
        try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch { return path.Trim(); }
    }

    internal static AppConfig Clone(AppConfig config)
    {
        var clone = JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(config, JsonOptions), JsonOptions)
            ?? throw new InvalidOperationException("Unable to clone configuration.");
        Normalize(clone);
        return clone;
    }
}
