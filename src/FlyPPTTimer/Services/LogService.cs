namespace FlyPPTTimer.Services;

public sealed class LogService
{
    private readonly object _sync = new();

    public LogService()
    {
        Directory.CreateDirectory(AppPaths.LogDirectory);
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message, Exception? ex = null) => Write("ERROR", ex is null ? message : $"{message} | {ex}");

    private void Write(string level, string message)
    {
        lock (_sync)
        {
            File.AppendAllText(AppPaths.LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
        }
    }
}
