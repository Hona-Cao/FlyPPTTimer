using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class PresentationStaDispatcherTests
{
    [Fact]
    public void InvokeRunsOnDedicatedStaThread()
    {
        using var dispatcher = new PresentationStaDispatcher();
        var callerThread = Environment.CurrentManagedThreadId;

        var result = dispatcher.Invoke(
            () => (ThreadId: Environment.CurrentManagedThreadId, Apartment: Thread.CurrentThread.GetApartmentState()),
            TimeSpan.FromSeconds(2));

        Assert.NotEqual(callerThread, result.ThreadId);
        Assert.Equal(dispatcher.WorkerThreadId, result.ThreadId);
        Assert.Equal(ApartmentState.STA, result.Apartment);
        Assert.Equal(ApartmentState.STA, dispatcher.WorkerApartmentState);
    }

    [Fact]
    public void InvokeRetriesKnownComBusyFailures()
    {
        using var dispatcher = new PresentationStaDispatcher();
        var attempts = 0;

        var result = dispatcher.Invoke(() =>
        {
            attempts++;
            if (attempts < 3)
                throw new COMException("busy", unchecked((int)0x8001010A));
            return 42;
        }, TimeSpan.FromSeconds(2));

        Assert.Equal(42, result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public void InvokeDoesNotRetryUnrelatedComFailures()
    {
        using var dispatcher = new PresentationStaDispatcher();
        var attempts = 0;

        var exception = Assert.Throws<COMException>(() => dispatcher.Invoke<int>(() =>
        {
            attempts++;
            throw new COMException("not busy", unchecked((int)0x80004005));
        }, TimeSpan.FromSeconds(2)));

        Assert.Equal(unchecked((int)0x80004005), exception.HResult);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public void EnqueuedOperationsRunInSubmissionOrder()
    {
        using var dispatcher = new PresentationStaDispatcher();
        var values = new ConcurrentQueue<int>();
        using var completed = new CountdownEvent(3);

        for (var value = 1; value <= 3; value++)
        {
            var captured = value;
            Assert.True(dispatcher.TryEnqueue(() =>
            {
                values.Enqueue(captured);
                completed.Signal();
            }));
        }

        Assert.True(completed.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal(new[] { 1, 2, 3 }, values.ToArray());
    }

    [Fact]
    public void DisposedDispatcherRejectsNewWork()
    {
        var dispatcher = new PresentationStaDispatcher();
        dispatcher.Dispose();

        Assert.False(dispatcher.TryEnqueue(() => { }));
        Assert.Throws<ObjectDisposedException>(() => dispatcher.Invoke(() => 1, TimeSpan.FromSeconds(1)));
    }
}
