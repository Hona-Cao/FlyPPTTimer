using FlyPPTTimer.Core.Abstractions;

namespace FlyPPTTimer.Infrastructure.Windows;

public sealed class LogService : ILogService
{
    private const long MaxTotalBytes = 20 * 1024 * 1024;
    private readonly object _sync = new();
    private readonly string _directory;
    public LogService(string? directory = null) { _directory = directory ?? AppPaths.LogDirectory; Directory.CreateDirectory(_directory); Prune(); }
    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message, Exception? exception = null) => Write("ERROR", exception is null ? message : $"{message} | {exception}");
    public IReadOnlyList<string> ReadRecent(int count = 200)
    {
        lock (_sync)
        {
            return Directory.GetFiles(_directory, "app-*.log").OrderByDescending(File.GetLastWriteTimeUtc)
                .SelectMany(path => File.ReadLines(path).Reverse()).Take(count).Reverse().ToList();
        }
    }
    private void Write(string level, string message)
    {
        lock (_sync)
        {
            try { File.AppendAllText(Path.Combine(_directory, $"app-{DateTime.Now:yyyyMMdd}.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}"); Prune(); } catch { }
        }
    }
    private void Prune()
    {
        var files = Directory.GetFiles(_directory, "app-*.log").Select(x => new FileInfo(x)).OrderByDescending(x => x.LastWriteTimeUtc).ToList();
        foreach (var old in files.Where(x => x.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-30))) { try { old.Delete(); } catch { } }
        var kept = files.Where(x => x.Exists).ToList(); long total = kept.Sum(x => x.Length);
        foreach (var old in kept.OrderBy(x => x.LastWriteTimeUtc)) { if (total <= MaxTotalBytes) break; try { total -= old.Length; old.Delete(); } catch { } }
    }
}
