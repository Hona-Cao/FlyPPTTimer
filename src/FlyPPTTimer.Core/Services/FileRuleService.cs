using FlyPPTTimer.Core.Models;

namespace FlyPPTTimer.Core.Services;

public sealed class FileRuleService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".ppt", ".pptx", ".pptm" };
    private readonly AppConfig _config;
    public FileRuleService(AppConfig config) => _config = config;
    public IReadOnlyList<FileRule> Rules => _config.Rules.OrderBy(x => x.Order).ToList();

    public IReadOnlyList<string> AddFiles(IEnumerable<string> paths)
    {
        var errors = new List<string>();
        foreach (var raw in paths)
        {
            var path = SafeFullPath(raw);
            if (path is null || !File.Exists(path)) { errors.Add($"文件不存在：{raw}"); continue; }
            if (!AllowedExtensions.Contains(Path.GetExtension(path))) { errors.Add($"不支持的格式：{Path.GetFileName(path)}"); continue; }
            if (_config.Rules.Any(x => SamePath(x.FilePath, path))) continue;
            _config.Rules.Add(new FileRule { FileName = Path.GetFileName(path), FilePath = path, Duration = _config.Timer.DefaultDuration, Order = _config.Rules.Count });
        }
        NormalizeOrder();
        return errors;
    }

    public bool SetDuration(IEnumerable<string> ids, string duration)
    {
        if (!TimeSpan.TryParseExact(duration, @"hh\:mm\:ss", null, out var parsed) || parsed <= TimeSpan.Zero) return false;
        foreach (var rule in _config.Rules.Where(x => ids.Contains(x.Id))) rule.Duration = duration;
        return true;
    }

    public bool Remove(IEnumerable<string> ids, string activeSlideShowPath, out string error)
    {
        var selected = _config.Rules.Where(x => ids.Contains(x.Id)).ToList();
        if (selected.Any(x => SamePath(x.FilePath, activeSlideShowPath))) { error = "正在放映的文件不能删除。"; return false; }
        foreach (var item in selected) _config.Rules.Remove(item);
        NormalizeOrder(); error = ""; return true;
    }

    public bool Relocate(string id, string newPath, out string error)
    {
        var rule = _config.Rules.FirstOrDefault(x => x.Id == id);
        var path = SafeFullPath(newPath);
        if (rule is null || path is null || !File.Exists(path) || !AllowedExtensions.Contains(Path.GetExtension(path))) { error = "请选择有效的 PPT 文件。"; return false; }
        if (_config.Rules.Any(x => x.Id != id && SamePath(x.FilePath, path))) { error = "该文件已经在列表中。"; return false; }
        rule.FilePath = path; rule.FileName = Path.GetFileName(path); error = ""; return true;
    }

    public void Move(string id, MoveDirection direction)
    {
        var list = _config.Rules.OrderBy(x => x.Order).ToList();
        var index = list.FindIndex(x => x.Id == id); if (index < 0) return;
        var target = direction switch { MoveDirection.Top => 0, MoveDirection.Up => Math.Max(0, index - 1), MoveDirection.Down => Math.Min(list.Count - 1, index + 1), MoveDirection.Bottom => list.Count - 1, _ => index };
        var item = list[index]; list.RemoveAt(index); list.Insert(target, item);
        for (var i = 0; i < list.Count; i++) list[i].Order = i;
    }

    public void SetOrder(IEnumerable<string> ids)
    {
        var order = ids.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        foreach (var rule in _config.Rules)
            if (order.TryGetValue(rule.Id, out var index)) rule.Order = index;
        _config.Rules.Sort((left, right) => left.Order.CompareTo(right.Order));
        NormalizeOrder();
    }

    public TimeSpan ResolveDuration(string path)
    {
        var rule = _config.Rules.FirstOrDefault(x => x.Enabled && SamePath(x.FilePath, path));
        return TimerService.ParseDuration(rule?.Duration ?? _config.Timer.DefaultDuration);
    }

    private void NormalizeOrder() { for (var i = 0; i < _config.Rules.Count; i++) _config.Rules[i].Order = i; }
    private static string? SafeFullPath(string value) { try { return Path.GetFullPath(value); } catch { return null; } }
    public static bool SamePath(string left, string right) { try { return !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) && string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); } catch { return false; } }
}

public enum MoveDirection { Top, Up, Down, Bottom }
