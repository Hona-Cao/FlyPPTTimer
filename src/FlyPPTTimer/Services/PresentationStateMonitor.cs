using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

/// <summary>
/// Maintains the presentation state snapshot and coordinates background refreshes.
/// Provider-specific COM reads stay outside this type and are supplied as delegates.
/// </summary>
internal sealed class PresentationStateMonitor : IDisposable
{
    internal static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromMilliseconds(500);
    internal static readonly TimeSpan RefreshFailureLogInterval = TimeSpan.FromSeconds(30);

    private readonly PresentationStaDispatcher _dispatcher;
    private readonly Func<PresentationState> _readState;
    private readonly Action<PresentationState> _decorateState;
    private readonly Func<Exception, string> _friendlyError;
    private readonly Action<string>? _warn;
    private readonly Func<DateTime> _now;
    private readonly Func<DateTime> _utcNow;
    private readonly object _stateSync = new();
    private readonly System.Threading.Timer? _refreshTimer;

    private PresentationState _cachedState = new();
    private bool _lastShowRunning;
    private string _lastShowPath = "";
    private int _refreshQueued;
    private DateTime _lastRefreshFailureLog = DateTime.MinValue;
    private int _disposed;

    public PresentationStateMonitor(
        PresentationStaDispatcher dispatcher,
        Func<PresentationState> readState,
        Action<PresentationState> decorateState,
        Func<Exception, string> friendlyError,
        Action<string>? warn = null,
        TimeSpan? refreshInterval = null,
        bool startTimer = true,
        Func<DateTime>? now = null,
        Func<DateTime>? utcNow = null)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _readState = readState ?? throw new ArgumentNullException(nameof(readState));
        _decorateState = decorateState ?? throw new ArgumentNullException(nameof(decorateState));
        _friendlyError = friendlyError ?? throw new ArgumentNullException(nameof(friendlyError));
        _warn = warn;
        _now = now ?? (() => DateTime.Now);
        _utcNow = utcNow ?? (() => DateTime.UtcNow);

        var interval = refreshInterval ?? DefaultRefreshInterval;
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(refreshInterval));
        if (startTimer)
            _refreshTimer = new System.Threading.Timer(_ => QueueRefresh(), null, TimeSpan.Zero, interval);
    }

    public event EventHandler<string>? SlideShowStarted;
    public event EventHandler? SlideShowEnded;
    public event EventHandler? StateChanged;

    public PresentationState GetState()
    {
        lock (_stateSync) return CloneState(_cachedState);
    }

    public PresentationState RefreshNow()
    {
        ThrowIfDisposed();
        var state = _readState();
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

        state.UpdatedAt = _now();
        _decorateState(state);
        lock (_stateSync) _cachedState = CloneState(state);

        if (state.IsSlideShowRunning && (!_lastShowRunning || !SamePath(_lastShowPath, state.PresentationPath)))
            SlideShowStarted?.Invoke(this, state.PresentationPath);
        else if (!state.IsSlideShowRunning && _lastShowRunning)
            SlideShowEnded?.Invoke(this, EventArgs.Empty);

        _lastShowRunning = state.IsSlideShowRunning;
        _lastShowPath = state.PresentationPath;
        StateChanged?.Invoke(this, EventArgs.Empty);
        return CloneState(state);
    }

    public void MutateCurrent(Action<PresentationState> mutation, bool notify = true)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        ThrowIfDisposed();
        lock (_stateSync) mutation(_cachedState);
        if (notify) StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void QueueRefresh()
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        if (Interlocked.Exchange(ref _refreshQueued, 1) != 0) return;

        if (!_dispatcher.TryEnqueue(() =>
        {
            try { _dispatcher.ExecuteWithBusyRetry(RefreshNow); }
            catch (Exception ex)
            {
                var now = _utcNow();
                if (now - _lastRefreshFailureLog >= RefreshFailureLogInterval)
                {
                    _lastRefreshFailureLog = now;
                    _warn?.Invoke($"Presentation background refresh failed: {_friendlyError(ex)}");
                }
            }
            finally { Interlocked.Exchange(ref _refreshQueued, 0); }
        }))
        {
            Interlocked.Exchange(ref _refreshQueued, 0);
        }
    }

    private static bool SamePath(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
        try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
        catch { return string.Equals(left, right, StringComparison.OrdinalIgnoreCase); }
    }

    internal static PresentationState CloneState(PresentationState state) => new()
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
        Presentations = state.Presentations.Select(option => new PresentationOption
        {
            Id = option.Id,
            Name = option.Name,
            Directory = option.Directory,
            IsOpen = option.IsOpen,
            IsActive = option.IsActive,
            IsSlideShowRunning = option.IsSlideShowRunning,
            IsManaged = option.IsManaged
        }).ToList(),
        Operation = state.Operation,
        OperationMessage = state.OperationMessage,
        OperationStartedAt = state.OperationStartedAt,
        OperationId = state.OperationId,
        IsOperationBusy = state.IsOperationBusy,
        IsCurrentPresentationManaged = state.IsCurrentPresentationManaged,
        OpenPresentationCount = state.OpenPresentationCount,
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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _refreshTimer?.Dispose();
    }
}
