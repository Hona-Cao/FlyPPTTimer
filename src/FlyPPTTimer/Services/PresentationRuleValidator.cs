using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

/// <summary>Shared, side-effect-free validation for presentation file rules.</summary>
public static class PresentationRuleValidator
{
    private const string DurationFormat = @"hh\:mm\:ss";

    public static bool TryNormalizeDuration(string? value, out string normalized, out string error)
    {
        normalized = "";
        error = "";
        if (!TimeSpan.TryParseExact(value?.Trim(), DurationFormat, CultureInfo.InvariantCulture, TimeSpanStyles.None, out var duration)
            || duration <= TimeSpan.Zero)
        {
            error = "计时时长必须是大于 00:00:00 的 HH:mm:ss。";
            return false;
        }

        normalized = duration.ToString(DurationFormat, CultureInfo.InvariantCulture);
        return true;
    }

    public static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch { return path.Trim(); }
    }

    public static string IdForPath(string? path)
    {
        var normalized = NormalizePath(path);
        return string.IsNullOrWhiteSpace(normalized)
            ? ""
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized.ToUpperInvariant())))[..20];
    }

    public static bool TryResolveEnabledRule(IEnumerable<FileRule> rules, string? presentationId, out string path, out string error)
    {
        path = "";
        error = "";
        if (string.IsNullOrWhiteSpace(presentationId))
        {
            error = "请先选择演示文稿。";
            return false;
        }

        var matching = rules
            .Where(rule => rule.Enabled && !string.IsNullOrWhiteSpace(rule.FilePath))
            .Select(rule => NormalizePath(rule.FilePath))
            .FirstOrDefault(candidate => string.Equals(IdForPath(candidate), presentationId, StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(matching))
        {
            error = "所选演示文稿不在已启用规则中。";
            return false;
        }
        path = matching;
        return true;
    }

    public static IReadOnlyList<PresentationListEntry> MergeRulesAndOpenPresentations(IEnumerable<FileRule> rules, IEnumerable<PresentationOption> presentations)
    {
        var entries = new Dictionary<string, PresentationListEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules)
        {
            var path = NormalizePath(rule.FilePath);
            if (string.IsNullOrWhiteSpace(path)) continue;
            var option = presentations.FirstOrDefault(item =>
                string.Equals(NormalizePath(Path.Combine(item.Directory, item.Name)), path, StringComparison.OrdinalIgnoreCase));
            entries[path] = new PresentationListEntry(path, rule, option);
        }
        foreach (var option in presentations.Where(item => item.IsOpen))
        {
            var path = NormalizePath(Path.Combine(option.Directory, option.Name));
            if (!string.IsNullOrWhiteSpace(path) && !entries.ContainsKey(path))
                entries[path] = new PresentationListEntry(path, null, option);
        }
        return entries.Values.OrderBy(entry => entry.Rule?.FileName ?? entry.Presentation?.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}

public sealed record PresentationListEntry(string Path, FileRule? Rule, PresentationOption? Presentation);
