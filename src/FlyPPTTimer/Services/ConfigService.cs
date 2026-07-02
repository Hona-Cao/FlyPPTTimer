using System.Text.Json;
using System.Security.Cryptography;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

public sealed class ConfigService(LogService log)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(AppPaths.ConfigPath))
            {
                var defaults = new AppConfig();
                Save(defaults);
                log.Info("Default config created.");
                return defaults;
            }

            var json = File.ReadAllText(AppPaths.ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            Normalize(config);
            Save(config);
            log.Info("Config loaded.");
            return config;
        }
        catch (Exception ex)
        {
            var badPath = $"{AppPaths.ConfigPath}.{DateTime.Now:yyyyMMddHHmmss}.bad.json";
            try
            {
                if (File.Exists(AppPaths.ConfigPath))
                {
                    File.Copy(AppPaths.ConfigPath, badPath, true);
                }
            }
            catch (Exception copyEx)
            {
                log.Error("Failed to backup bad config.", copyEx);
            }

            log.Error("Config is invalid; default config will be used.", ex);
            var defaults = new AppConfig();
            Save(defaults);
            return defaults;
        }
    }

    public void Save(AppConfig config)
    {
        Normalize(config);
        Directory.CreateDirectory(AppPaths.BaseDirectory);
        File.WriteAllText(AppPaths.ConfigPath, JsonSerializer.Serialize(config, JsonOptions));
        log.Info("Config saved.");
    }

    public void Export(AppConfig config, string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(config, JsonOptions));
        log.Info($"Config exported: {path}");
    }

    public AppConfig Import(string path)
    {
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        Normalize(config);
        Save(config);
        log.Info($"Config imported: {path}");
        return config;
    }

    private static void Normalize(AppConfig config)
    {
        config.Version = "0.10.0";
        if (!config.Placement.HasCustomPlacement)
        {
            config.Placement.Anchor = OverlayAnchor.TopCenter;
            config.Placement.OffsetXPercent = 0;
            config.Placement.OffsetYPercent = 0.5m;
        }
        foreach (var rule in config.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.FileName) && !string.IsNullOrWhiteSpace(rule.FilePath))
            {
                rule.FileName = Path.GetFileName(rule.FilePath);
            }
            if (string.IsNullOrWhiteSpace(rule.FilePath) && !string.IsNullOrWhiteSpace(rule.TitlePattern))
            {
                rule.FilePath = rule.TitlePattern;
                rule.FileName = Path.GetFileName(rule.TitlePattern);
            }
            if (string.IsNullOrWhiteSpace(rule.Duration)) rule.Duration = config.Timer.DefaultDuration;
        }
        if (string.IsNullOrWhiteSpace(config.Controls.StartPauseHotkey) || config.Controls.StartPauseHotkey.Contains('+')) config.Controls.StartPauseHotkey = "F3";
        if (string.IsNullOrWhiteSpace(config.Controls.StopResetHotkey) || config.Controls.StopResetHotkey.Contains('+')) config.Controls.StopResetHotkey = "F4";
        if (string.IsNullOrWhiteSpace(config.Controls.ToggleWindowHotkey) || config.Controls.ToggleWindowHotkey.Contains('+')) config.Controls.ToggleWindowHotkey = "F5";
        if (string.IsNullOrWhiteSpace(config.Controls.OpenSettingsHotkey) || config.Controls.OpenSettingsHotkey.Contains('+')) config.Controls.OpenSettingsHotkey = "F6";
        config.Controls.Hotkeys ??= ControlSettings.DefaultHotkeys();
        foreach (var pair in ControlSettings.DefaultHotkeys())
        {
            if (!config.Controls.Hotkeys.ContainsKey(pair.Key)) config.Controls.Hotkeys[pair.Key] = pair.Value;
        }
        config.Controls.Hotkeys["startPause"] = config.Controls.StartPauseHotkey;
        config.Controls.Hotkeys["stopReset"] = config.Controls.StopResetHotkey;
        config.Controls.Hotkeys["toggleWindow"] = config.Controls.ToggleWindowHotkey;
        config.Controls.Hotkeys["openSettings"] = config.Controls.OpenSettingsHotkey;
        if (string.IsNullOrWhiteSpace(config.RemoteControl.Token))
        {
            config.RemoteControl.Token = GenerateToken();
        }
    }

    public static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
