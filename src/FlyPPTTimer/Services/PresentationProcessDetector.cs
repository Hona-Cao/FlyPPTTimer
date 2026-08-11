using System.Diagnostics;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

public interface IPresentationProcessSource
{
    IReadOnlyList<string> GetProcessNames();
}

public sealed class SystemPresentationProcessSource : IPresentationProcessSource
{
    public IReadOnlyList<string> GetProcessNames()
    {
        var processes = Process.GetProcesses();
        try
        {
            var names = new List<string>(processes.Length);
            foreach (var process in processes) names.Add(process.ProcessName);
            return names;
        }
        finally
        {
            foreach (var process in processes) process.Dispose();
        }
    }
}

public sealed record PresentationProcessSnapshot(
    bool PowerPointDetected,
    bool WpsDetected,
    IReadOnlyList<string> MatchingProcessNames);

public sealed class PresentationProcessDetector
{
    public const string WpsCapabilityMessage =
        "检测到 WPS 演示；当前版本未声明可靠的 WPS 文稿 COM 关闭能力，只允许明确确认后的强制退出。";

    private static readonly HashSet<string> PowerPointProcessNames =
        new(StringComparer.OrdinalIgnoreCase) { "POWERPNT" };

    private static readonly HashSet<string> WpsProcessNames =
        new(StringComparer.OrdinalIgnoreCase) { "WPSOffice", "wpp", "wps" };

    private readonly IPresentationProcessSource _source;

    public PresentationProcessDetector(IPresentationProcessSource? source = null)
    {
        _source = source ?? new SystemPresentationProcessSource();
    }

    public PresentationProcessSnapshot Detect()
    {
        var matching = _source.GetProcessNames()
            .Where(IsPresentationProcessName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PresentationProcessSnapshot(
            matching.Any(IsPowerPointProcessName),
            matching.Any(IsWpsProcessName),
            matching);
    }

    public static bool IsPowerPointProcessName(string? processName) =>
        !string.IsNullOrWhiteSpace(processName) && PowerPointProcessNames.Contains(processName);

    public static bool IsWpsProcessName(string? processName) =>
        !string.IsNullOrWhiteSpace(processName) && WpsProcessNames.Contains(processName);

    public static bool IsPresentationProcessName(string? processName) =>
        IsPowerPointProcessName(processName) || IsWpsProcessName(processName);

    public static WpsCapabilities CreateWpsCapabilities(bool detected) => detected
        ? new WpsCapabilities
        {
            CanEndSlideShow = false,
            CanClosePresentation = false,
            CanExitApplication = false,
            CanForceExit = true,
            Message = WpsCapabilityMessage
        }
        : new WpsCapabilities();
}
