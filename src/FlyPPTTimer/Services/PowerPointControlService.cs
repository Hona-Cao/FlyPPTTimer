using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

public sealed class PowerPointControlService : IDisposable
{
    private const int SlideShowRunning = 1;
    private const int SlideShowBlackScreen = 3;
    private const int SlideShowWhiteScreen = 4;
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;
    private readonly Func<AppConfig> _getConfig;
    private readonly LogService _log;
    private bool _disposed;

    public PowerPointControlService(Func<AppConfig> getConfig, LogService log)
    {
        _getConfig = getConfig;
        _log = log;
        _thread = new Thread(Run) { IsBackground = true, Name = "FlyPPTTimer PowerPoint STA" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public event EventHandler<string>? SlideShowStarted;
    public event EventHandler? SlideShowEnded;

    public PresentationState GetState() => Invoke(ReadState);

    public PresentationCommandResult Execute(RemoteCommand command)
    {
        return Invoke(() =>
        {
            try
            {
                var message = ExecuteCore(command);
                var state = ReadState();
                return new PresentationCommandResult(true, message, state);
            }
            catch (Exception ex)
            {
                _log.Error($"PowerPoint command failed: {command.Command}", ex);
                var state = ReadState();
                state.Error = FriendlyError(ex);
                return new PresentationCommandResult(false, state.Error, state);
            }
        });
    }

    private void Run()
    {
        foreach (var action in _queue.GetConsumingEnumerable()) action();
    }

    private T Invoke<T>(Func<T> operation)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PowerPointControlService));
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Add(() =>
        {
            try { completion.SetResult(operation()); }
            catch (Exception ex) { completion.SetException(ex); }
        });
        return completion.Task.GetAwaiter().GetResult();
    }

    private string ExecuteCore(RemoteCommand command)
    {
        return command.Command switch
        {
            "ppt.refresh" => "状态已刷新",
            "ppt.startFromBeginning" => StartShow(false),
            "ppt.startFromCurrent" => StartShow(true),
            "ppt.previous" => WithSlideShowView(view => ((dynamic)view).Previous(), "已切换到上一页"),
            "ppt.next" => WithSlideShowView(view => ((dynamic)view).Next(), "已切换到下一页"),
            "ppt.gotoSlide" => GoToSlide(command.SlideNumber),
            "ppt.endShow" => EndShow(),
            "ppt.blackScreenToggle" => ToggleScreen(SlideShowBlackScreen, "黑屏"),
            "ppt.whiteScreenToggle" => ToggleScreen(SlideShowWhiteScreen, "白屏"),
            "ppt.openPresentation" => OpenPresentation(command.PresentationId),
            _ => throw new InvalidOperationException("命令不在 PowerPoint 控制白名单中。")
        };
    }

    private string StartShow(bool fromCurrent)
    {
        object? appObject = null, presentation = null, settings = null, slides = null, window = null, view = null, slide = null, showWindows = null, startedWindow = null;
        try
        {
            appObject = GetRunningApplication();
            dynamic app = appObject;
            showWindows = app.SlideShowWindows;
            if ((int)((dynamic)showWindows).Count > 0) return "放映已经在运行，本次重复启动已忽略";
            presentation = app.ActivePresentation;
            if (presentation is null) throw new InvalidOperationException("PowerPoint 中没有活动演示文稿。") ;
            dynamic deck = presentation;
            slides = deck.Slides;
            var total = (int)((dynamic)slides).Count;
            var start = 1;
            if (fromCurrent)
            {
                try
                {
                    window = app.ActiveWindow;
                    view = ((dynamic)window).View;
                    slide = ((dynamic)view).Slide;
                    start = Math.Clamp((int)((dynamic)slide).SlideIndex, 1, total);
                }
                catch { start = 1; }
            }
            settings = deck.SlideShowSettings;
            dynamic showSettings = settings;
            showSettings.RangeType = fromCurrent ? 2 : 1;
            showSettings.StartingSlide = start;
            showSettings.EndingSlide = total;
            startedWindow = showSettings.Run();
            var path = SafeString(() => (string)deck.FullName);
            SlideShowStarted?.Invoke(this, path);
            return fromCurrent ? $"已从第 {start} 页开始放映" : "已从头开始放映";
        }
        finally { Release(startedWindow, showWindows, slide, view, window, slides, settings, presentation, appObject); }
    }

    private string GoToSlide(int? slideNumber)
    {
        if (slideNumber is null or <= 0) throw new InvalidOperationException("请输入有效页码。") ;
        return WithSlideShowView(view => ((dynamic)view).GotoSlide(slideNumber.Value), $"已跳转到第 {slideNumber} 页");
    }

    private string EndShow()
    {
        var result = WithSlideShowView(view => ((dynamic)view).Exit(), "已结束放映");
        SlideShowEnded?.Invoke(this, EventArgs.Empty);
        return result;
    }

    private string ToggleScreen(int targetState, string label)
    {
        return WithSlideShowView(view =>
        {
            dynamic v = view;
            v.State = (int)v.State == targetState ? SlideShowRunning : targetState;
        }, $"已切换{label}状态");
    }

    private string WithSlideShowView(Action<object> action, string message)
    {
        object? appObject = null, windows = null, window = null, view = null;
        try
        {
            appObject = GetRunningApplication();
            dynamic app = appObject;
            windows = app.SlideShowWindows;
            if ((int)((dynamic)windows).Count <= 0) throw new InvalidOperationException("当前没有正在运行的 PowerPoint 放映。") ;
            window = ((dynamic)windows)[1];
            view = ((dynamic)window).View;
            action(view);
            return message;
        }
        finally { Release(view, window, windows, appObject); }
    }

    private string OpenPresentation(string? id)
    {
        var allowed = GetAllowedPaths();
        object? appObject = null, presentations = null, presentation = null, windows = null, window = null;
        try
        {
            appObject = TryGetRunningApplication();
            if (appObject is not null)
            {
                presentations = ((dynamic)appObject).Presentations;
                for (var i = 1; i <= (int)((dynamic)presentations).Count; i++)
                {
                    object? item = null;
                    try
                    {
                        item = ((dynamic)presentations)[i];
                        var openPath = SafeString(() => (string)((dynamic)item).FullName);
                        if (IdForPath(openPath) == id) allowed.Add(openPath);
                    }
                    finally { Release(item); }
                }
                Release(presentations);
                presentations = null;
            }

            var path = allowed.FirstOrDefault(x => IdForPath(x) == id);
            if (path is null) throw new InvalidOperationException("所选文件不在允许的演示文稿列表中。") ;
            if (!File.Exists(path)) throw new FileNotFoundException("演示文稿文件不存在。", path);
            appObject ??= GetOrCreateApplication();
            dynamic app = appObject;
            app.Visible = true;
            presentations = app.Presentations;
            presentation = FindOpenPresentation(presentations, path) ?? ((dynamic)presentations).Open(path);
            windows = ((dynamic)presentation).Windows;
            if ((int)((dynamic)windows).Count > 0)
            {
                window = ((dynamic)windows)[1];
                ((dynamic)window).Activate();
            }
            return $"已打开 {Path.GetFileName(path)}";
        }
        finally { Release(window, windows, presentation, presentations, appObject); }
    }

    private PresentationState ReadState()
    {
        var state = new PresentationState { PowerPointInstalled = Type.GetTypeFromProgID("PowerPoint.Application") is not null };
        object? appObject = null, presentations = null, active = null, windows = null, window = null, view = null, slide = null, parent = null, slides = null;
        try
        {
            appObject = TryGetRunningApplication();
            state.PowerPointRunning = appObject is not null;
            if (appObject is null)
            {
                AddRuleOptions(state, []);
                return state;
            }

            dynamic app = appObject;
            presentations = app.Presentations;
            var openPaths = new List<string>();
            for (var i = 1; i <= (int)((dynamic)presentations).Count; i++)
            {
                object? item = null;
                try
                {
                    item = ((dynamic)presentations)[i];
                    var path = SafeString(() => (string)((dynamic)item).FullName);
                    if (!string.IsNullOrWhiteSpace(path)) openPaths.Add(path);
                }
                finally { Release(item); }
            }

            try { active = app.ActivePresentation; } catch { active = null; }
            if (active is not null)
            {
                dynamic deck = active;
                state.HasPresentation = true;
                state.PresentationName = SafeString(() => (string)deck.Name);
                state.PresentationPath = SafeString(() => (string)deck.FullName);
                slides = deck.Slides;
                state.TotalSlides = (int)((dynamic)slides).Count;
            }

            windows = app.SlideShowWindows;
            state.IsSlideShowRunning = (int)((dynamic)windows).Count > 0;
            if (state.IsSlideShowRunning)
            {
                window = ((dynamic)windows)[1];
                view = ((dynamic)window).View;
                slide = ((dynamic)view).Slide;
                state.CurrentSlide = (int)((dynamic)slide).SlideIndex;
                var viewState = (int)((dynamic)view).State;
                state.ScreenMode = viewState == SlideShowBlackScreen ? "黑屏" : viewState == SlideShowWhiteScreen ? "白屏" : "正常";
                parent = ((dynamic)slide).Parent;
                if (!state.HasPresentation)
                {
                    state.PresentationName = SafeString(() => (string)((dynamic)parent).Name);
                    state.PresentationPath = SafeString(() => (string)((dynamic)parent).FullName);
                    state.HasPresentation = true;
                }
            }
            AddRuleOptions(state, openPaths);
        }
        catch (Exception ex)
        {
            state.Error = FriendlyError(ex);
            _log.Error("PowerPoint state refresh failed.", ex);
        }
        finally { Release(slides, parent, slide, view, window, windows, active, presentations, appObject); }
        return state;
    }

    private void AddRuleOptions(PresentationState state, IReadOnlyCollection<string> openPaths)
    {
        var activePath = state.PresentationPath;
        foreach (var path in openPaths.Concat(GetAllowedPaths()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            state.Presentations.Add(new PresentationOption
            {
                Id = IdForPath(path), Name = Path.GetFileName(path),
                IsOpen = openPaths.Contains(path, StringComparer.OrdinalIgnoreCase),
                IsActive = SamePath(path, activePath)
            });
        }
    }

    private List<string> GetAllowedPaths() => _getConfig().Rules
        .Where(x => x.Enabled && !string.IsNullOrWhiteSpace(x.FilePath))
        .Select(x => Path.GetFullPath(x.FilePath)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static object? FindOpenPresentation(object presentations, string path)
    {
        for (var i = 1; i <= (int)((dynamic)presentations).Count; i++)
        {
            object? item = ((dynamic)presentations)[i];
            if (SamePath(SafeString(() => (string)((dynamic)item).FullName), path)) return item;
            Release(item);
        }
        return null;
    }

    private static object GetRunningApplication() => TryGetRunningApplication() ?? throw new InvalidOperationException("Microsoft PowerPoint 未运行。") ;
    private static object GetOrCreateApplication()
    {
        var running = TryGetRunningApplication();
        if (running is not null) return running;
        var type = Type.GetTypeFromProgID("PowerPoint.Application") ?? throw new InvalidOperationException("未安装 Microsoft PowerPoint。") ;
        return Activator.CreateInstance(type) ?? throw new InvalidOperationException("无法启动 Microsoft PowerPoint。") ;
    }
    private static object? TryGetRunningApplication()
    {
        try
        {
            if (CLSIDFromProgID("PowerPoint.Application", out var clsid) != 0) return null;
            GetActiveObject(ref clsid, IntPtr.Zero, out var app);
            return app;
        }
        catch { return null; }
    }

    private static string IdForPath(string path) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(path))))[..20];
    private static bool SamePath(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
        try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
        catch { return string.Equals(left, right, StringComparison.OrdinalIgnoreCase); }
    }
    private static string SafeString(Func<string> value) { try { return value() ?? ""; } catch { return ""; } }
    private static string FriendlyError(Exception ex) => ex switch
    {
        COMException => "PowerPoint 正忙、文稿受保护，或当前操作不可用，请稍后重试。",
        FileNotFoundException => "所选演示文稿文件不存在。",
        InvalidOperationException => ex.Message,
        _ => "PowerPoint 操作失败，请查看程序日志。"
    };
    private static void Release(params object?[] values)
    {
        foreach (var value in values)
        {
            if (value is null || !Marshal.IsComObject(value)) continue;
            try { Marshal.FinalReleaseComObject(value); } catch { }
        }
    }

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID(string progId, out Guid clsid);
    [DllImport("oleaut32.dll", PreserveSig = false)]
    private static extern void GetActiveObject(ref Guid clsid, IntPtr reserved, [MarshalAs(UnmanagedType.IUnknown)] out object instance);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _queue.CompleteAdding();
        if (!_thread.Join(TimeSpan.FromSeconds(2))) _log.Warn("PowerPoint STA thread did not stop within two seconds.");
        _queue.Dispose();
    }
}
