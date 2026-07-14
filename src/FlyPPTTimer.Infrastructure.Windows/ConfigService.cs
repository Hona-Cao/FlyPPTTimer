using System.Security.Cryptography;
using System.Text.Json;
using FlyPPTTimer.Core.Abstractions;
using FlyPPTTimer.Core.Models;

namespace FlyPPTTimer.Infrastructure.Windows;

public sealed class ConfigService(ILogService log, string? configPath = null) : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly string _path = configPath ?? AppPaths.ConfigPath;
    private readonly object _sync = new();

    public AppConfig Load()
    {
        lock (_sync)
        {
            if (!File.Exists(_path)) { var created = Normalize(new()); SaveCore(created); return created; }
            try { var config = Normalize(Deserialize(File.ReadAllText(_path))); SaveCore(config); return config; }
            catch (Exception ex)
            {
                PreserveInvalid(); log.Error("配置损坏，正在尝试恢复备份。", ex);
                foreach (var backup in Backups())
                {
                    try { var recovered = Normalize(Deserialize(File.ReadAllText(backup))); SaveCore(recovered); log.Warn($"已恢复配置备份：{Path.GetFileName(backup)}"); return recovered; } catch { }
                }
                var defaults = Normalize(new()); SaveCore(defaults); return defaults;
            }
        }
    }
    public void Save(AppConfig config) { lock (_sync) SaveCore(Normalize(config)); }
    public AppConfig Import(string path) { var config = Normalize(Deserialize(File.ReadAllText(path))); Save(config); return config; }
    public void Export(AppConfig config, string path) => File.WriteAllText(path, JsonSerializer.Serialize(Normalize(config), JsonOptions));
    public static string GenerateToken() { Span<byte> bytes = stackalloc byte[24]; RandomNumberGenerator.Fill(bytes); return Convert.ToHexString(bytes).ToLowerInvariant(); }

    private void SaveCore(AppConfig config)
    {
        var directory = Path.GetDirectoryName(_path)!; Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(config, JsonOptions); _ = Deserialize(json);
        var temp = Path.Combine(directory, $".{Path.GetFileName(_path)}.{Guid.NewGuid():N}.tmp"); File.WriteAllText(temp, json); _ = Deserialize(File.ReadAllText(temp));
        try
        {
            if (File.Exists(_path))
            {
                var backup = $"{_path}.backup.{DateTime.Now:yyyyMMddHHmmssfff}.json";
                try { File.Replace(temp, _path, backup, true); } catch { File.Copy(_path, backup, true); File.Move(temp, _path, true); }
            }
            else File.Move(temp, _path);
            foreach (var old in Backups().Skip(5)) try { File.Delete(old); } catch { }
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private static AppConfig Deserialize(string json) => JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? throw new InvalidDataException("配置为空。");
    private IEnumerable<string> Backups() => Directory.Exists(Path.GetDirectoryName(_path)) ? Directory.GetFiles(Path.GetDirectoryName(_path)!, $"{Path.GetFileName(_path)}.backup.*.json").OrderByDescending(File.GetLastWriteTimeUtc) : [];
    private void PreserveInvalid() { try { File.Copy(_path, $"{_path}.{DateTime.Now:yyyyMMddHHmmss}.bad.json", true); } catch { } }
    private static AppConfig Normalize(AppConfig config)
    {
        config.Version = "0.13.0"; config.Rules ??= []; config.Controls.Hotkeys ??= ControlSettings.DefaultHotkeys();
        if (string.IsNullOrWhiteSpace(config.RemoteControl.Token)) config.RemoteControl.Token = GenerateToken();
        for (var i = 0; i < config.Rules.Count; i++)
        {
            var rule = config.Rules[i]; if (string.IsNullOrWhiteSpace(rule.Id)) rule.Id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(rule.FileName)) rule.FileName = Path.GetFileName(rule.FilePath);
            if (string.IsNullOrWhiteSpace(rule.Duration)) rule.Duration = config.Timer.DefaultDuration; rule.Order = i;
        }
        return config;
    }
}
