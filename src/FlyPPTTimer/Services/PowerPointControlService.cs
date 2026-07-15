using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using FlyPPTTimer.Models;
using FlyPPTTimer.Native;

namespace FlyPPTTimer.Services;

public sealed class PowerPointControlService : IDisposable
{
    private const int SlideShowRunning = 1;
    private const int SlideShowBlackScreen = 3;
    private const int SlideShowWhiteScreen = 4;
    private readonly BlockingCollection<Action> _queue = new(32);
    private readonly Thread _thread;
    private readonly Func<AppConfig> _getConfig;
    private readonly LogService _log;
    private readonly System.Threading.Timer _refreshTimer;
    private readonly object _stateSync = new();
    private readonly object _operationSync = new();
    private readonly Dictionary<string, ManagedPresentation> _managedPresentations = new(StringComparer.OrdinalIgnoreCase);
    private PresentationState _cachedState = new();
    private bool _lastShowRunning;
    private string _lastShowPath = "";
    private long _lastNavigationTick;
    private int _refreshQueued;
    private DateTime _lastRefreshFailureLog = DateTime.MinValue;
    private bool _disposed;
    private PresentationOperationInfo _operation = PresentationOperationInfo.Idle;

    public PowerPointControlService(Func<AppConfig> getConfig, LogService log)
    {
        _getConfig = getConfig;
        _log = log;
        _thread = new Thread(Run) { IsBackground = true, Name = "FlyPPTTimer PowerPoint STA" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _refreshTimer = new System.Threading.Timer(_ => QueueRefresh(), null, 0, 500);
    }

    public event EventHandler<string>? SlideShowStarted;
    public event EventHandler? SlideShowEnded;
    public event EventHandler? SlideShowWindowActivated;
    public event EventHandler? StateChanged;

    public PresentationState GetState()
    {
        lock (_stateSync) return CloneState(_cachedState);
    }

    /// <summary>
    /// Accepts a whitelisted presentation command without making the HTTP request wait for COM.
    /// The existing STA queue remains the sole place where PowerPoint is accessed.
    /// </summary>
    public PresentationCommandResult Queue(RemoteCommand command)
    {
        if (_disposed) return new PresentationCommandResult(false, "演示控制服务已关闭。", GetState());
        if (!IsKnownCommand(command.Command)) return new PresentationCommandResult(false, "命令不在演示控制白名单中。", GetState());
        if (command.Command == "ppt.forceQuitAll" && command.Confirmed != true)
            return new PresentationCommandResult(false, "强制退出会丢失所有未保存内容，请再次确认。", GetState());

        var operation = CreateOperation(command.Command);
        lock (_operationSync)
        {
            if (_operation.IsBusy)
                return new PresentationCommandResult(false, "演示操作正在进行，请等待当前操作完成。", GetState());
            _operation = operation;
        }
        NotifyStateChanged();

        if (!_queue.TryAdd(() => RunQueuedOperation(command, operation), 200))
        {
            SetOperation(PresentationOperationInfo.Failed(operation, "演示命令队列繁忙，请稍后重试。"));
            return new PresentationCommandResult(false, "演示命令队列繁忙，请稍后重试。", GetState());
        }

        var accepted = GetState();
        return new PresentationCommandResult(true, operation.Message, accepted);
    }

    private void RunQueuedOperation(RemoteCommand command, PresentationOperationInfo operation)
    {
        try
        {
            SetOperation(operation with { Message = "正在" + operation.Message });
            var message = RetryComBusy(() => ExecuteCore(command));
            UpdateCachedState();
            SetOperation(PresentationOperationInfo.Idle with { Message = message });
        }
        catch (Exception ex)
        {
            _log.Error($"PowerPoint command failed: {command.Command}", ex);
            var error = FriendlyError(ex);
            lock (_stateSync) _cachedState.Error = error;
            SetOperation(PresentationOperationInfo.Failed(operation, error));
        }
    }

    public PresentationCommandResult Execute(RemoteCommand command)
    {
        var timeout = command.Command is "ppt.openPresentation" or "ppt.startFromBeginning" or "ppt.startFromCurrent"
            ? TimeSpan.FromSeconds(15) : TimeSpan.FromSeconds(5);
        try { return Invoke(() =>
        {
            try
            {
                var message = ExecuteCore(command);
                var state = UpdateCachedState();
                return new PresentationCommandResult(true, message, state);
            }
            catch (Exception ex)
            {
                _log.Error($"PowerPoint command failed: {command.Command}", ex);
                var state = GetState();
                state.Error = FriendlyError(ex);
                return new PresentationCommandResult(false, state.Error, state);
            }
        }, timeout); }
        catch (TimeoutException ex)
        {
            _log.Error($"PowerPoint command timed out: {command.Command}", ex);
            return new PresentationCommandResult(false, "PowerPoint 响应超时，计时遥控仍可继续使用。", GetState());
        }
        catch (InvalidOperationException ex)
        {
            return new PresentationCommandResult(false, ex.Message, GetState());
        }
    }

    private void Run()
    {
        foreach (var action in _queue.GetConsumingEnumerable()) action();
    }

    private T Invoke<T>(Func<T> operation, TimeSpan timeout)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PowerPointControlService));
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var cancellationToken = cancellation.Token;
        if (!_queue.TryAdd(() =>
        {
            if (cancellationToken.IsCancellationRequested) return;
            try { completion.TrySetResult(RetryComBusy(operation)); }
            catch (Exception ex) { completion.SetException(ex); }
        }, 200)) throw new InvalidOperationException("PowerPoint 命令队列繁忙，请稍后重试。");
        if (!completion.Task.Wait(timeout)) { cancellation.Cancel(); throw new TimeoutException(); }
        return completion.Task.GetAwaiter().GetResult();
    }

    private T RetryComBusy<T>(Func<T> operation)
    {
        for (var attempt = 0; ; attempt++)
        {
            try { return operation(); }
            catch (COMException ex) when (attempt < 3 && (ex.HResult == unchecked((int)0x80010001) || ex.HResult == unchecked((int)0x8001010A)))
            {
                Thread.Sleep(100 * (attempt + 1));
            }
        }
    }

    private void QueueRefresh()
    {
        if (_disposed) return;
        if (Interlocked.Exchange(ref _refreshQueued, 1) != 0) return;
        if (!_queue.TryAdd(() =>
        {
            try { RetryComBusy(UpdateCachedState); }
            catch (Exception ex)
            {
                if (DateTime.UtcNow - _lastRefreshFailureLog >= TimeSpan.FromSeconds(30))
                {
                    _lastRefreshFailureLog = DateTime.UtcNow;
                    _log.Warn($"PowerPoint background refresh failed: {FriendlyError(ex)}");
                }
            }
            finally { Interlocked.Exchange(ref _refreshQueued, 0); }
        })) Interlocked.Exchange(ref _refreshQueued, 0);
    }

    private PresentationState UpdateCachedState()
    {
        var state = ReadState();
        if (!string.IsNullOrWhiteSpace(state.Error))
        {
            lock (_stateSync)
            {
                var stale = CloneState(_cachedState);
                stale.Error = state.Error;
                _cachedState = stale;
                return CloneState(stale);
            }
        }
        state.UpdatedAt = DateTime.Now;
        ApplyOperation(state);
        lock (_stateSync) _cachedState = CloneState(state);
        if (state.IsSlideShowRunning && (!_lastShowRunning || !SamePath(_lastShowPath, state.PresentationPath)))
            SlideShowStarted?.Invoke(this, state.PresentationPath);
        else if (!state.IsSlideShowRunning && _lastShowRunning) SlideShowEnded?.Invoke(this, EventArgs.Empty);
        _lastShowRunning = state.IsSlideShowRunning;
        _lastShowPath = state.PresentationPath;
        NotifyStateChanged();
        return state;
    }

    private string ExecuteCore(RemoteCommand command)
    {
        return command.Command switch
        {
            "ppt.refresh" => "状态已刷新",
            "ppt.startFromBeginning" => StartShow(command.PresentationId, false),
            "ppt.startFromCurrent" => StartShow(command.PresentationId, true),
            "ppt.previous" => Navigate(view => ((dynamic)view).Previous(), "已切换到上一页"),
            "ppt.next" => Navigate(view => ((dynamic)view).Next(), "已切换到下一页"),
            "ppt.gotoSlide" => GoToSlide(command.SlideNumber),
            "ppt.endShow" => EndShow(),
            "ppt.blackScreenToggle" => ToggleScreen(SlideShowBlackScreen, "黑屏"),
            "ppt.whiteScreenToggle" => ToggleScreen(SlideShowWhiteScreen, "白屏"),
            "ppt.openPresentation" => OpenPresentation(command.PresentationId),
            "ppt.closeCurrentPresentation" => CloseCurrentPresentation(),
            "ppt.exitApplication" => ExitApplication(),
            "ppt.forceQuitAll" => ForceQuitAll(),
            _ => throw new InvalidOperationException("命令不在 PowerPoint 控制白名单中。")
        };
    }

    private static bool IsKnownCommand(string command) => command is
        "ppt.refresh" or "ppt.startFromBeginning" or "ppt.startFromCurrent" or "ppt.previous" or "ppt.next" or
        "ppt.gotoSlide" or "ppt.endShow" or "ppt.blackScreenToggle" or "ppt.whiteScreenToggle" or
        "ppt.openPresentation" or "ppt.closeCurrentPresentation" or "ppt.exitApplication" or "ppt.forceQuitAll";

    private PresentationOperationInfo CreateOperation(string command)
    {
        var (name, message) = command switch
        {
            "ppt.openPresentation" => ("OpeningPresentation", "正在打开演示文稿"),
            "ppt.startFromBeginning" or "ppt.startFromCurrent" => ("StartingSlideshow", "正在启动放映"),
            "ppt.endShow" => ("StoppingSlideshow", "正在结束放映"),
            "ppt.closeCurrentPresentation" => ("ClosingPresentation", "正在关闭当前受控文稿"),
            "ppt.exitApplication" => ("ExitingApplication", "正在退出演示程序"),
            "ppt.forceQuitAll" => ("ForceExitingApplication", "正在强制退出演示程序"),
            _ => ("Idle", "正在执行演示命令")
        };
        return new PresentationOperationInfo(name, message, DateTime.Now, Guid.NewGuid().ToString("N"), name != "Idle");
    }

    private void SetOperation(PresentationOperationInfo operation)
    {
        lock (_operationSync) _operation = operation;
        lock (_stateSync) ApplyOperation(_cachedState);
        NotifyStateChanged();
    }

    private void ApplyOperation(PresentationState state)
    {
        PresentationOperationInfo operation;
        lock (_operationSync) operation = _operation;
        state.Operation = operation.Name;
        state.OperationMessage = operation.Message;
        state.OperationStartedAt = operation.StartedAt;
        state.OperationId = operation.Id;
        state.IsOperationBusy = operation.IsBusy;
    }

    private void NotifyStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private string StartShow(string? presentationId, bool fromCurrent)
    {
        object? appObject = null, presentation = null, settings = null, slides = null, window = null, view = null, slide = null, showWindows = null, startedWindow = null;
        try
        {
            appObject = GetRunningApplication();
            dynamic app = appObject;
            showWindows = app.SlideShowWindows;
            if ((int)((dynamic)showWindows).Count > 0) return "放映已经在运行，本次重复启动已忽略";
            presentation = ResolvePresentationForShow(app, presentationId);
            if (presentation is null) throw new InvalidOperationException("PowerPoint 中没有活动演示文稿。") ;
            var activation = ActivatePresentationWindow(presentation);
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
            var foreground = ActivateSlideShowWindow(app, path, startedWindow);
            var prefix = fromCurrent ? $"已从第 {start} 页开始放映" : "已从头开始放映";
            return prefix + activation.Message + foreground.Message;
        }
        finally { Release(startedWindow, showWindows, slide, view, window, slides, settings, presentation, appObject); }
    }

    private object? ResolvePresentationForShow(dynamic app, string? presentationId)
    {
        if (string.IsNullOrWhiteSpace(presentationId))
        {
            try { return app.ActivePresentation; }
            catch { return null; }
        }

        if (!PresentationRuleValidator.TryResolveEnabledRule(_getConfig().Rules, presentationId, out var path, out var error))
            throw new InvalidOperationException(error);
        if (!File.Exists(path)) throw new FileNotFoundException("所选演示文稿文件不存在。", path);
        object? presentations = null, presentation = null;
        try
        {
            presentations = app.Presentations;
            presentation = FindOpenPresentation(presentations, path);
            if (presentation is not null) return presentation;
            presentation = ((dynamic)presentations).Open(path, true, false, true);
            _managedPresentations[NormalizePath(path)] = new ManagedPresentation(NormalizePath(path), DateTime.Now, true);
            return presentation;
        }
        finally { Release(presentations); }
    }

    private string GoToSlide(int? slideNumber)
    {
        if (slideNumber is null or <= 0) throw new InvalidOperationException("请输入有效页码。") ;
        return WithSlideShowView(view => ((dynamic)view).GotoSlide(slideNumber.Value), $"已跳转到第 {slideNumber} 页");
    }

    private string EndShow()
    {
        var result = WithSlideShowView(view => ((dynamic)view).Exit(), "已结束放映");
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
            window = FindSlideShowWindow(windows, GetState().PresentationPath);
            if (window is null) throw new InvalidOperationException("未找到当前受控演示文稿对应的放映窗口。");
            view = ((dynamic)window).View;
            action(view);
            return message;
        }
        finally { Release(view, window, windows, appObject); }
    }

    private WindowActivationResult ActivatePresentationWindow(object presentation)
    {
        object? windows = null, window = null, appObject = null;
        try
        {
            dynamic deck = presentation;
            windows = deck.Windows;
            var path = SafeString(() => (string)deck.FullName);
            if ((int)((dynamic)windows).Count <= 0) return WindowActivationResult.Failed("未找到目标文稿窗口", path, IntPtr.Zero);
            window = ((dynamic)windows)[1];
            try { ((dynamic)window).Activate(); }
            catch (Exception ex) { return WindowActivationResult.Failed("文稿已打开但 COM 激活失败", path, IntPtr.Zero, ex); }
            var maximized = true;
            try { ((dynamic)window).WindowState = NativeMethods.SwMaximize; }
            catch (Exception ex) { maximized = false; _log.Warn($"PowerPoint maximize via COM failed: path={path}; {ex.Message}"); }
            try
            {
                appObject = deck.Application;
                var hwnd = (IntPtr)(int)((dynamic)appObject).Hwnd;
                var foreground = ActivateNativeWindow(hwnd, path, "文稿窗口", maximized);
                return foreground;
            }
            catch (Exception ex) { return WindowActivationResult.Failed("文稿已打开但无法读取窗口句柄", path, IntPtr.Zero, ex); }
        }
        finally { Release(appObject, window, windows); }
    }

    private WindowActivationResult ActivateSlideShowWindow(object app, string presentationPath, object? startedWindow)
    {
        object? windows = null, window = null, presentation = null;
        var ownsWindow = false;
        try
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                window = startedWindow;
                ownsWindow = false;
                if (window is null)
                {
                    windows = ((dynamic)app).SlideShowWindows;
                    window = FindSlideShowWindow(windows, presentationPath);
                    ownsWindow = window is not null;
                }
                if (window is not null)
                {
                    presentation = ((dynamic)window).Presentation;
                    if (SamePath(SafeString(() => (string)((dynamic)presentation).FullName), presentationPath))
                    {
                        try { ((dynamic)window).Activate(); }
                        catch (Exception ex) { return WindowActivationResult.Failed("放映已启动但 COM 激活失败", presentationPath, IntPtr.Zero, ex); }
                        try
                        {
                            var hwnd = (IntPtr)(int)((dynamic)window).HWND;
                            var result = ActivateNativeWindow(hwnd, presentationPath, "放映窗口", true, "；放映已启动但置前失败");
                            SlideShowWindowActivated?.Invoke(this, EventArgs.Empty);
                            return result;
                        }
                        catch (Exception ex) { return WindowActivationResult.Failed("放映已启动但无法读取窗口句柄", presentationPath, IntPtr.Zero, ex); }
                    }
                }
                Release(presentation, ownsWindow ? window : null, windows);
                presentation = window = windows = null;
                ownsWindow = false;
                Thread.Sleep(100);
            }
        }
        finally { Release(presentation, ownsWindow ? window : null, windows); }
        return WindowActivationResult.Failed("放映已启动但未找到目标放映窗口", presentationPath, IntPtr.Zero);
    }

    private WindowActivationResult ActivateNativeWindow(IntPtr hwnd, string path, string label, bool maximized, string failurePrefix = "；文稿已打开但最大化或置前失败")
    {
        if (hwnd == IntPtr.Zero) return WindowActivationResult.Failed($"未找到目标{label}", path, hwnd);
        var showResult = NativeMethods.ShowWindow(hwnd, NativeMethods.SwMaximize);
        var showError = Marshal.GetLastWin32Error();
        var bringResult = NativeMethods.BringWindowToTop(hwnd);
        var bringError = Marshal.GetLastWin32Error();
        var foregroundResult = NativeMethods.SetForegroundWindow(hwnd);
        var foregroundError = Marshal.GetLastWin32Error();
        if (!bringResult || !foregroundResult)
        {
            NativeMethods.SetWindowPos(hwnd, NativeMethods.HwndTopmost, 0, 0, 0, 0, NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpShowWindow);
            NativeMethods.SetWindowPos(hwnd, NativeMethods.HwndNoTopmost, 0, 0, 0, 0, NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpShowWindow);
            bringResult = NativeMethods.BringWindowToTop(hwnd);
            bringError = Marshal.GetLastWin32Error();
            foregroundResult = NativeMethods.SetForegroundWindow(hwnd);
            foregroundError = Marshal.GetLastWin32Error();
        }
        if (bringResult && foregroundResult && maximized) return WindowActivationResult.Succeeded("；已最大化并置前", path, hwnd);
        var detail = $"{label} HWND=0x{hwnd.ToInt64():X}; ShowWindow={showResult}/错误{showError}; BringWindowToTop={bringResult}/错误{bringError}; SetForegroundWindow={foregroundResult}/错误{foregroundError}";
        _log.Warn($"PowerPoint window activation incomplete: path={path}; {detail}");
        return WindowActivationResult.Failed(failurePrefix + $"（{detail}）", path, hwnd);
    }

    private static object? FindSlideShowWindow(object windows, string presentationPath)
    {
        for (var i = 1; i <= (int)((dynamic)windows).Count; i++)
        {
            object? window = null, presentation = null;
            var matched = false;
            try
            {
                window = ((dynamic)windows)[i];
                presentation = ((dynamic)window).Presentation;
                var path = SafeString(() => (string)((dynamic)presentation).FullName);
                matched = SamePath(path, presentationPath)
                    || (string.IsNullOrWhiteSpace(presentationPath) && (int)((dynamic)windows).Count == 1);
                if (matched) return window;
            }
            finally
            {
                Release(presentation);
                if (!matched) Release(window);
            }
        }
        return null;
    }

    private string OpenPresentation(string? id)
    {
        var allowed = GetAllowedPaths();
        object? appObject = null, presentations = null, presentation = null, windows = null, window = null;
        try
        {
            appObject = TryGetRunningApplication();
            if (appObject is null)
            {
                PresentationOperationInfo current;
                lock (_operationSync) current = _operation;
                SetOperation(current with { Name = "StartingApplication", Message = "正在启动 PowerPoint" });
            }
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
            presentation = FindOpenPresentation(presentations, path);
            if (presentation is null)
            {
                // FlyPPTTimer-owned files are always opened read-only. Existing user documents are never changed.
                presentation = ((dynamic)presentations).Open(path, true, false, true);
                _managedPresentations[NormalizePath(path)] = new ManagedPresentation(NormalizePath(path), DateTime.Now, true);
            }
            var activation = ActivatePresentationWindow(presentation);
            return $"已打开 {Path.GetFileName(path)}" + activation.Message;
        }
        finally { Release(window, windows, presentation, presentations, appObject); }
    }

    private string CloseCurrentPresentation()
    {
        object? appObject = null, presentations = null, presentation = null;
        try
        {
            appObject = GetRunningApplication();
            dynamic app = appObject;
            presentation = app.ActivePresentation;
            if (presentation is null) throw new InvalidOperationException("当前没有可关闭的演示文稿。");
            var path = SafeString(() => (string)((dynamic)presentation).FullName);
            var key = NormalizePath(path);
            if (!_managedPresentations.ContainsKey(key))
                throw new InvalidOperationException("当前文稿不是由 FlyPPTTimer 打开的，不能通过远程控制静默关闭。");
            EndShowIfShowing(app, path);
            try { ((dynamic)presentation).Saved = true; } catch { }
            ((dynamic)presentation).Close();
            _managedPresentations.Remove(key);
            return "已关闭当前 FlyPPTTimer 打开的只读文稿。";
        }
        finally { Release(presentation, presentations, appObject); }
    }

    private string ExitApplication()
    {
        object? appObject = null, presentations = null;
        try
        {
            appObject = GetRunningApplication();
            dynamic app = appObject;
            presentations = app.Presentations;
            var unmanaged = new List<string>();
            for (var i = 1; i <= (int)((dynamic)presentations).Count; i++)
            {
                object? item = null;
                try
                {
                    item = ((dynamic)presentations)[i];
                    var path = SafeString(() => (string)((dynamic)item).FullName);
                    if (!_managedPresentations.ContainsKey(NormalizePath(path))) unmanaged.Add(path);
                }
                finally { Release(item); }
            }
            if (unmanaged.Count > 0)
                throw new InvalidOperationException("仍有用户自行打开的文稿，已拒绝退出 PowerPoint；请先在电脑端处理这些文稿。");

            EndShowIfShowing(app, "");
            for (var i = (int)((dynamic)presentations).Count; i >= 1; i--)
            {
                object? item = null;
                try
                {
                    item = ((dynamic)presentations)[i];
                    try { ((dynamic)item).Saved = true; } catch { }
                    ((dynamic)item).Close();
                }
                finally { Release(item); }
            }
            _managedPresentations.Clear();
            app.Quit();
            return "已退出 PowerPoint。";
        }
        finally { Release(presentations, appObject); }
    }

    private string ForceQuitAll()
    {
        var names = new[] { "POWERPNT", "WPSOffice", "wpp", "wps" };
        var processes = Process.GetProcesses().Where(process => names.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (processes.Length == 0) return "未发现正在运行的 PowerPoint 或 WPS 演示进程。";
        foreach (var process in processes)
        {
            try { process.Kill(true); }
            catch (Exception ex) { _log.Warn($"Failed to force quit {process.ProcessName}: {ex.Message}"); }
            finally { process.Dispose(); }
        }
        _managedPresentations.Clear();
        return "已请求强制退出全部 PowerPoint/WPS/演示软件。未保存内容不会恢复。";
    }

    private static void EndShowIfShowing(dynamic app, string path)
    {
        object? windows = null, window = null, presentation = null, view = null;
        try
        {
            windows = app.SlideShowWindows;
            for (var i = 1; i <= (int)((dynamic)windows).Count; i++)
            {
                window = ((dynamic)windows)[i];
                presentation = ((dynamic)window).Presentation;
                var showingPath = SafeString(() => (string)((dynamic)presentation).FullName);
                if (string.IsNullOrWhiteSpace(path) || SamePath(showingPath, path))
                {
                    view = ((dynamic)window).View;
                    ((dynamic)view).Exit();
                    return;
                }
                Release(view, presentation, window);
                view = presentation = window = null;
            }
        }
        finally { Release(view, presentation, window, windows); }
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
                PopulateWpsCapabilities(state);
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

            windows = app.SlideShowWindows;
            state.IsSlideShowRunning = (int)((dynamic)windows).Count > 0;
            if (state.IsSlideShowRunning)
            {
                var activePath = "";
                try
                {
                    active = app.ActivePresentation;
                    activePath = SafeString(() => (string)((dynamic)active).FullName);
                }
                catch { active = null; }
                window = FindSlideShowWindow(windows, activePath);
                if (window is null)
                {
                    state.Error = "未能按目标文稿匹配放映窗口。";
                    AddRuleOptions(state, openPaths);
                    return state;
                }
                parent = ((dynamic)window).Presentation;
                view = ((dynamic)window).View;
                slide = ((dynamic)view).Slide;
                state.CurrentSlide = (int)((dynamic)slide).SlideIndex;
                var viewState = (int)((dynamic)view).State;
                state.ScreenMode = viewState == SlideShowBlackScreen ? "黑屏" : viewState == SlideShowWhiteScreen ? "白屏" : "正常";
                state.PresentationName = SafeString(() => (string)((dynamic)parent).Name);
                state.PresentationPath = SafeString(() => (string)((dynamic)parent).FullName);
                slides = ((dynamic)parent).Slides;
                state.TotalSlides = (int)((dynamic)slides).Count;
                state.HasPresentation = true;
            }
            else
            {
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
            }
            state.IsCurrentPresentationManaged = _managedPresentations.ContainsKey(NormalizePath(state.PresentationPath));
            PopulateWpsCapabilities(state);
            AddRuleOptions(state, openPaths);
        }
        catch (Exception ex)
        {
            if (ex is COMException busy && IsComBusy(busy)) throw;
            state.Error = FriendlyError(ex);
            _log.Error("PowerPoint state refresh failed.", ex);
        }
        finally { Release(slides, parent, slide, view, window, windows, active, presentations, appObject); }
        return state;
    }

    private static void PopulateWpsCapabilities(PresentationState state)
    {
        var detected = Process.GetProcesses().Any(process =>
        {
            try { return process.ProcessName.Equals("WPSOffice", StringComparison.OrdinalIgnoreCase) || process.ProcessName.Equals("wpp", StringComparison.OrdinalIgnoreCase) || process.ProcessName.Equals("wps", StringComparison.OrdinalIgnoreCase); }
            finally { process.Dispose(); }
        });
        state.WpsDetected = detected;
        state.WpsCapabilities = detected
            ? new WpsCapabilities { CanEndSlideShow = false, CanClosePresentation = false, CanExitApplication = false, CanForceExit = true, Message = "检测到 WPS 演示；当前版本未声明可靠的 WPS 文稿 COM 关闭能力，只允许明确确认后的强制退出。" }
            : new WpsCapabilities();
    }

    private void AddRuleOptions(PresentationState state, IReadOnlyCollection<string> openPaths)
    {
        var activePath = state.PresentationPath;
        foreach (var path in openPaths.Concat(GetAllowedPaths()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            state.Presentations.Add(new PresentationOption
            {
                Id = IdForPath(path), Name = Path.GetFileName(path), Directory = Path.GetDirectoryName(path) ?? "",
                IsOpen = openPaths.Contains(path, StringComparer.OrdinalIgnoreCase),
                IsActive = SamePath(path, activePath),
                IsSlideShowRunning = state.IsSlideShowRunning && SamePath(path, state.PresentationPath),
                IsManaged = _managedPresentations.ContainsKey(NormalizePath(path))
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

    private static string IdForPath(string path) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(NormalizePath(path).ToUpperInvariant())))[..20];
    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch { return path.Trim(); }
    }
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
    private static bool IsComBusy(COMException ex) => ex.HResult == unchecked((int)0x80010001) || ex.HResult == unchecked((int)0x8001010A);
    private static PresentationState CloneState(PresentationState state) => new()
    {
        PowerPointInstalled = state.PowerPointInstalled,
        PowerPointRunning = state.PowerPointRunning,
        HasPresentation = state.HasPresentation,
        IsSlideShowRunning = state.IsSlideShowRunning,
        PresentationName = state.PresentationName,
        PresentationPath = state.PresentationPath,
        CurrentSlide = state.CurrentSlide,
        TotalSlides = state.TotalSlides,
        ScreenMode = state.ScreenMode,
        UpdatedAt = state.UpdatedAt,
        Error = state.Error,
        Presentations = state.Presentations.Select(x => new PresentationOption { Id = x.Id, Name = x.Name, Directory = x.Directory, IsOpen = x.IsOpen, IsActive = x.IsActive, IsSlideShowRunning = x.IsSlideShowRunning, IsManaged = x.IsManaged }).ToList(),
        Operation = state.Operation,
        OperationMessage = state.OperationMessage,
        OperationStartedAt = state.OperationStartedAt,
        OperationId = state.OperationId,
        IsOperationBusy = state.IsOperationBusy,
        IsCurrentPresentationManaged = state.IsCurrentPresentationManaged,
        WpsDetected = state.WpsDetected,
        WpsCapabilities = new WpsCapabilities
        {
            CanEndSlideShow = state.WpsCapabilities.CanEndSlideShow,
            CanClosePresentation = state.WpsCapabilities.CanClosePresentation,
            CanExitApplication = state.WpsCapabilities.CanExitApplication,
            CanForceExit = state.WpsCapabilities.CanForceExit,
            Message = state.WpsCapabilities.Message
        }
    };

    private sealed record ManagedPresentation(string Path, DateTime OpenedAt, bool ReadOnlyRequested);
    private sealed record WindowActivationResult(bool Success, string Message, string Path, IntPtr Hwnd)
    {
        public static WindowActivationResult Succeeded(string message, string path, IntPtr hwnd) => new(true, message, path, hwnd);
        public static WindowActivationResult Failed(string message, string path, IntPtr hwnd, Exception? exception = null) => new(false, $"；{message}", path, hwnd);
    }
    private sealed record PresentationOperationInfo(string Name, string Message, DateTime? StartedAt, string Id, bool IsBusy)
    {
        public static PresentationOperationInfo Idle { get; } = new("Idle", "", null, "", false);
        public static PresentationOperationInfo Failed(PresentationOperationInfo prior, string message) =>
            new("Failed", message, prior.StartedAt, prior.Id, false);
    }

    private string Navigate(Action<object> action, string message)
    {
        var now = Environment.TickCount64;
        if (now - _lastNavigationTick < 160) return "操作过快，本次翻页已忽略";
        _lastNavigationTick = now;
        return WithSlideShowView(action, message);
    }
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
        _refreshTimer.Dispose();
        _queue.CompleteAdding();
        if (!_thread.Join(TimeSpan.FromSeconds(2)))
        {
            _log.Warn("PowerPoint STA thread did not stop within two seconds; queue disposal deferred until process exit.");
            return;
        }
        _queue.Dispose();
    }
}
