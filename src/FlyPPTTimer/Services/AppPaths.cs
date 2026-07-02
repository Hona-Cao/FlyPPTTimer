namespace FlyPPTTimer.Services;

public static class AppPaths
{
    public static string BaseDirectory => AppContext.BaseDirectory;
    public static string ConfigPath => Path.Combine(BaseDirectory, "FlyPPTTimer.config.json");
    public static string LogDirectory => Path.Combine(BaseDirectory, "logs");
    public static string LogPath => Path.Combine(LogDirectory, "app.log");
}
