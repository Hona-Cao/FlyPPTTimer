using System.Diagnostics;

namespace FlyPPTTimer.Services;

public interface IPresentationProcessHandle : IDisposable
{
    string ProcessName { get; }
    void Kill(bool entireProcessTree);
}

public interface IPresentationProcessHandleSource
{
    IReadOnlyList<IPresentationProcessHandle> GetProcesses();
}

public sealed class SystemPresentationProcessHandleSource : IPresentationProcessHandleSource
{
    public IReadOnlyList<IPresentationProcessHandle> GetProcesses() =>
        Process.GetProcesses()
            .Select(process => (IPresentationProcessHandle)new SystemPresentationProcessHandle(process))
            .ToArray();

    private sealed class SystemPresentationProcessHandle(Process process) : IPresentationProcessHandle
    {
        public string ProcessName => process.ProcessName;

        public void Kill(bool entireProcessTree) => process.Kill(entireProcessTree);

        public void Dispose() => process.Dispose();
    }
}

public sealed record PresentationProcessTerminationResult(
    int MatchingProcessCount,
    int SuccessfulRequestCount,
    int FailedRequestCount,
    string Message)
{
    public bool AnyDetected => MatchingProcessCount > 0;
}

public sealed class PresentationProcessTerminator
{
    public const string NoProcessMessage = "未发现正在运行的 PowerPoint 或 WPS 演示进程。";
    public const string RequestedMessage = "已请求退出演示软件。未保存内容不会恢复。";

    private readonly IPresentationProcessHandleSource _source;
    private readonly Action<string> _warn;

    public PresentationProcessTerminator(
        IPresentationProcessHandleSource? source = null,
        Action<string>? warn = null)
    {
        _source = source ?? new SystemPresentationProcessHandleSource();
        _warn = warn ?? (_ => { });
    }

    public PresentationProcessTerminationResult TerminateAll()
    {
        var processes = _source.GetProcesses();
        try
        {
            var matching = processes
                .Where(process => PresentationProcessDetector.IsPresentationProcessName(process.ProcessName))
                .ToArray();

            if (matching.Length == 0)
                return new PresentationProcessTerminationResult(0, 0, 0, NoProcessMessage);

            var successful = 0;
            var failed = 0;
            foreach (var process in matching)
            {
                try
                {
                    process.Kill(true);
                    successful++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _warn($"Failed to force quit {process.ProcessName}: {ex.Message}");
                }
            }

            return new PresentationProcessTerminationResult(
                matching.Length,
                successful,
                failed,
                RequestedMessage);
        }
        finally
        {
            foreach (var process in processes) process.Dispose();
        }
    }
}
