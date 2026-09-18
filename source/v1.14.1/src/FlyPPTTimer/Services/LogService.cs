namespace FlyPPTTimer.Services;

public sealed class LogService
{
    private const long MaxLogBytes = 2 * 1024 * 1024;
    private readonly object _sync = new();
    private readonly string _directory;

    public LogService(string? directory = null)
    {
        _directory = directory ?? AppPaths.LogDirectory;
        Directory.CreateDirectory(_directory);
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message, Exception? ex = null) => Write("ERROR", ex is null ? message : $"{message} | {ex}");

    private void Write(string level, string message)
    {
        lock (_sync)
        {
            try
            {
                var path = CurrentLogPath();
                File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
            }
            catch { }
        }
    }

    private string CurrentLogPath()
    {
        var baseName = $"app-{DateTime.Now:yyyyMMdd}";
        var path = Path.Combine(_directory, baseName + ".log");
        if (!File.Exists(path) || new FileInfo(path).Length < MaxLogBytes) return path;
        for (var i = 1; ; i++)
        {
            path = Path.Combine(_directory, $"{baseName}-{i}.log");
            if (!File.Exists(path) || new FileInfo(path).Length < MaxLogBytes) return path;
        }
    }
}
