using System.Diagnostics;

namespace FlyPPTTimer;

static class Program
{
    private static readonly Services.LogService CrashLog = new();

    [STAThread]
    static void Main()
    {
        WaitForRestartParent(Environment.GetCommandLineArgs());
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
        var showSettings = Environment.GetCommandLineArgs()
            .Any(arg => string.Equals(arg, "--show-settings", StringComparison.OrdinalIgnoreCase));
        var showRemoteControl = Environment.GetCommandLineArgs()
            .Any(arg => string.Equals(arg, "--show-remote", StringComparison.OrdinalIgnoreCase));
        try { Application.Run(new FlyPPTTimerContext(showSettings, showRemoteControl)); }
        catch (Exception ex) { HandleFatal("Application startup failure", ex); }
    }

    private static void WaitForRestartParent(string[] args)
    {
        if (args.Length < 3
            || !string.Equals(args[1], "--restart-after", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(args[2], out var processId)
            || processId <= 0
            || processId == Environment.ProcessId) return;
        try
        {
            using var process = Process.GetProcessById(processId);
            process.WaitForExit(15000);
        }
        catch (ArgumentException) { }
        catch (InvalidOperationException) { }
    }

    private static void HandleFatal(string message, Exception? exception)
    {
        CrashLog.Error(message, exception);
        try { Application.Exit(); } catch { }
    }
}
