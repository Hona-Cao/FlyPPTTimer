using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace FlyPPTTimer.Services;

/// <summary>
/// Owns the dedicated STA thread used by presentation providers.
/// All COM-facing work is serialized through this dispatcher so UI, HTTP,
/// and background refresh callers never access presentation automation directly.
/// </summary>
internal sealed class PresentationStaDispatcher : IDisposable
{
    internal const int DefaultCapacity = 32;
    internal const int DefaultEnqueueTimeoutMilliseconds = 200;
    private const int RpcCallRejected = unchecked((int)0x80010001);
    private const int RpcServerCallRetryLater = unchecked((int)0x8001010A);

    private readonly BlockingCollection<Action> _queue;
    private readonly Thread _thread;
    private readonly Action<string>? _warn;
    private int _disposed;

    public PresentationStaDispatcher(
        string threadName = "FlyPPTTimer Presentation STA",
        int capacity = DefaultCapacity,
        Action<string>? warn = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        _warn = warn;
        _queue = new BlockingCollection<Action>(capacity);
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = threadName
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    internal int WorkerThreadId => _thread.ManagedThreadId;
    internal ApartmentState WorkerApartmentState => _thread.GetApartmentState();

    public bool TryEnqueue(Action operation, int millisecondsTimeout = DefaultEnqueueTimeoutMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (millisecondsTimeout < Timeout.Infinite)
            throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout));
        if (Volatile.Read(ref _disposed) != 0) return false;

        try
        {
            return _queue.TryAdd(operation, millisecondsTimeout);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    public T Invoke<T>(Func<T> operation, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (timeout != Timeout.InfiniteTimeSpan && timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));
        ThrowIfDisposed();

        // Provider code may compose dispatcher operations. Running nested work
        // directly on the worker prevents a self-deadlock while preserving STA.
        if (Environment.CurrentManagedThreadId == _thread.ManagedThreadId)
            return ExecuteWithBusyRetry(operation);

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var cancellationToken = cancellation.Token;

        if (!TryEnqueue(() =>
        {
            if (cancellationToken.IsCancellationRequested) return;
            try { completion.TrySetResult(ExecuteWithBusyRetry(operation)); }
            catch (Exception ex) { completion.TrySetException(ex); }
        }))
        {
            throw new InvalidOperationException("演示命令队列繁忙，请稍后重试。");
        }

        if (!completion.Task.Wait(timeout))
        {
            cancellation.Cancel();
            throw new TimeoutException();
        }

        return completion.Task.GetAwaiter().GetResult();
    }

    internal T ExecuteWithBusyRetry<T>(Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        for (var attempt = 0; ; attempt++)
        {
            try { return operation(); }
            catch (COMException ex) when (attempt < 3 && IsComBusy(ex))
            {
                Thread.Sleep(100 * (attempt + 1));
            }
        }
    }

    internal void ExecuteWithBusyRetry(Action operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ExecuteWithBusyRetry(() =>
        {
            operation();
            return true;
        });
    }

    private void Run()
    {
        foreach (var operation in _queue.GetConsumingEnumerable())
        {
            try { operation(); }
            catch (Exception ex) { _warn?.Invoke($"Unhandled presentation STA operation failed: {ex.Message}"); }
        }
    }

    private static bool IsComBusy(COMException exception) =>
        exception.HResult is RpcCallRejected or RpcServerCallRetryLater;

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _queue.CompleteAdding();

        if (Environment.CurrentManagedThreadId == _thread.ManagedThreadId) return;
        if (!_thread.Join(TimeSpan.FromSeconds(2)))
        {
            _warn?.Invoke("Presentation STA thread did not stop within two seconds; queue disposal deferred until process exit.");
            return;
        }

        _queue.Dispose();
    }
}
