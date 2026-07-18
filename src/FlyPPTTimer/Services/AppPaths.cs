namespace FlyPPTTimer.Services;

public static class AppPaths
{
    public static string BaseDirectory => AppContext.BaseDirectory;
    public static string ConfigPath => Path.Combine(BaseDirectory, "FlyPPTTimer.config.json");
    public static string LogDirectory => Path.Combine(BaseDirectory, "logs");
    public static string LogPath => Path.Combine(LogDirectory, $"app-{DateTime.Now:yyyyMMdd}.log");
    public static string AlertSoundsDirectory => Path.Combine(BaseDirectory, "alert-sounds");
}
