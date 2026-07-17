namespace FlyPPTTimer;

static class Program
{
    private static readonly Services.LogService CrashLog = new();

    [STAThread]
    static void Main()
    {
        using var singleInstance = new Mutex(true, "Local\\FlyPPTTimer.SingleInstance", out var isFirstInstance);
        if (!isFirstInstance) return;

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => HandleFatal("UI thread exception", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => HandleFatal("Unhandled application exception", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            e.SetObserved();
            HandleFatal("Unobserved background task exception", e.Exception);
        };
        ApplicationConfiguration.Initialize();
        try { Application.Run(new FlyPPTTimerContext()); }
        catch (Exception ex) { HandleFatal("Application startup failure", ex); }
    }

    private static void HandleFatal(string message, Exception? exception)
    {
        CrashLog.Error(message, exception);
        try { Application.Exit(); } catch { }
    }
}
