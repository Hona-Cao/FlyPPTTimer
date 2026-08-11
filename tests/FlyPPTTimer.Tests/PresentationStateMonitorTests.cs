using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class PresentationStateMonitorTests
{
    [Fact]
    public void RefreshStoresDecoratedSnapshotWithInjectedTimestamp()
    {
        using var dispatcher = new PresentationStaDispatcher();
        var timestamp = new DateTime(2026, 8, 4, 20, 0, 0, DateTimeKind.Local);
        using var monitor = CreateMonitor(
            dispatcher,
            () => new PresentationState { PresentationName = "Demo", CurrentSlide = 3 },
            state => state.Operation = "Refreshing",
            now: () => timestamp);

        var refreshed = monitor.RefreshNow();
        var cached = monitor.GetState();

        Assert.Equal("Demo", refreshed.PresentationName);
        Assert.Equal(3, cached.CurrentSlide);
        Assert.Equal("Refreshing", cached.Operation);
        Assert.Equal(timestamp, cached.UpdatedAt);
    }

    [Fact]
    public void GetStateReturnsDeepClone()
    {
        using var dispatcher = new PresentationStaDispatcher();
        using var monitor = CreateMonitor(dispatcher, () => new PresentationState
        {
            PresentationName = "Original",
            Presentations = [new PresentationOption { Id = "one", Name = "Deck" }],
            WpsCapabilities = new WpsCapabilities { CanForceExit = true, Message = "Detected" }
        });

        monitor.RefreshNow();
        var first = monitor.GetState();
        first.PresentationName = "Changed";
        first.Presentations[0].Name = "Changed deck";
        first.WpsCapabilities.Message = "Changed capability";

        var second = monitor.GetState();
        Assert.Equal("Original", second.PresentationName);
        Assert.Equal("Deck", second.Presentations[0].Name);
        Assert.Equal("Detected", second.WpsCapabilities.Message);
    }

    [Fact]
    public void RefreshPublishesSlideShowTransitionsWithoutDuplicateStarts()
    {
        using var dispatcher = new PresentationStaDispatcher();
        var states = new Queue<PresentationState>(
        [
            new PresentationState { IsSlideShowRunning = false },
            new PresentationState { IsSlideShowRunning = true, PresentationPath = @"C:\Decks\One.pptx" },
            new PresentationState { IsSlideShowRunning = true, PresentationPath = @"C:\Decks\One.pptx" },
            new PresentationState { IsSlideShowRunning = true, PresentationPath = @"C:\Decks\Two.pptx" },
            new PresentationState { IsSlideShowRunning = false }
        ]);
        using var monitor = CreateMonitor(dispatcher, () => states.Dequeue());
        var started = new List<string>();
        var ended = 0;
        var changed = 0;
        monitor.SlideShowStarted += (_, path) => started.Add(path);
        monitor.SlideShowEnded += (_, _) => ended++;
        monitor.StateChanged += (_, _) => changed++;

        for (var index = 0; index < 5; index++) monitor.RefreshNow();

        Assert.Equal([@"C:\Decks\One.pptx", @"C:\Decks\Two.pptx"], started);
        Assert.Equal(1, ended);
        Assert.Equal(5, changed);
    }

    [Fact]
    public void FailedRefreshKeepsLastSuccessfulStateAndAddsError()
    {
        using var dispatcher = new PresentationStaDispatcher();
        var states = new Queue<PresentationState>(
        [
            new PresentationState { PresentationName = "Stable", CurrentSlide = 7 },
            new PresentationState { Error = "PowerPoint busy" }
        ]);
        using var monitor = CreateMonitor(dispatcher, () => states.Dequeue());
        var changed = 0;
        monitor.StateChanged += (_, _) => changed++;

        monitor.RefreshNow();
        var failed = monitor.RefreshNow();

        Assert.Equal("Stable", failed.PresentationName);
        Assert.Equal(7, failed.CurrentSlide);
        Assert.Equal("PowerPoint busy", failed.Error);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void MutateCurrentUpdatesSnapshotAndCanNotify()
    {
        using var dispatcher = new PresentationStaDispatcher();
        using var monitor = CreateMonitor(dispatcher, () => new PresentationState());
        var changed = 0;
        monitor.StateChanged += (_, _) => changed++;

        monitor.MutateCurrent(state =>
        {
            state.Operation = "OpeningPresentation";
            state.IsOperationBusy = true;
        });

        var state = monitor.GetState();
        Assert.Equal("OpeningPresentation", state.Operation);
        Assert.True(state.IsOperationBusy);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void QueueRefreshCoalescesConcurrentRequests()
    {
        using var dispatcher = new PresentationStaDispatcher();
        using var gate = new ManualResetEventSlim();
        using var completed = new ManualResetEventSlim();
        var reads = 0;
        using var monitor = CreateMonitor(dispatcher, () =>
        {
            Interlocked.Increment(ref reads);
            gate.Wait(TimeSpan.FromSeconds(2));
            completed.Set();
            return new PresentationState();
        });

        monitor.QueueRefresh();
        monitor.QueueRefresh();
        Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref reads) == 1, TimeSpan.FromSeconds(2)));
        gate.Set();
        Assert.True(completed.Wait(TimeSpan.FromSeconds(2)));
        Thread.Sleep(50);

        Assert.Equal(1, Volatile.Read(ref reads));
    }

    [Fact]
    public void DisposedMonitorRejectsDirectOperationsAndIgnoresQueuedRefresh()
    {
        using var dispatcher = new PresentationStaDispatcher();
        var reads = 0;
        var monitor = CreateMonitor(dispatcher, () =>
        {
            reads++;
            return new PresentationState();
        });
        monitor.Dispose();

        monitor.QueueRefresh();
        Assert.Throws<ObjectDisposedException>(() => monitor.RefreshNow());
        Assert.Throws<ObjectDisposedException>(() => monitor.MutateCurrent(_ => { }));
        Assert.Equal(0, reads);
    }

    private static PresentationStateMonitor CreateMonitor(
        PresentationStaDispatcher dispatcher,
        Func<PresentationState> readState,
        Action<PresentationState>? decorateState = null,
        Func<DateTime>? now = null) =>
        new(
            dispatcher,
            readState,
            decorateState ?? (_ => { }),
            exception => exception.Message,
            startTimer: false,
            now: now);
}
