using FlyPPTTimer.Core.Models;
using FlyPPTTimer.Core.Services;

namespace FlyPPTTimer.Tests;

public sealed class WinUiCoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FlyPPTTimer-v013-tests", Guid.NewGuid().ToString("N"));
    public WinUiCoreTests() => Directory.CreateDirectory(_directory);

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    public void FileRules_AddMultiple_PreserveOrderAndIgnoreDuplicates(int count)
    {
        var config = new AppConfig(); var service = new FileRuleService(config);
        var paths = Enumerable.Range(0, count).Select(index => CreateFile($"deck-{index}.pptx")).ToArray();
        Assert.Empty(service.AddFiles(paths.Concat(paths)));
        Assert.Equal(count, service.Rules.Count);
        Assert.Equal(paths.Select(Path.GetFullPath), service.Rules.Select(x => x.FilePath));
    }

    [Fact]
    public void FileRules_MoveRelocateBatchDurationAndActiveDeleteGuard_WorkTogether()
    {
        var config = new AppConfig(); var service = new FileRuleService(config);
        var first = CreateFile("first.pptx"); var second = CreateFile("second.pptm"); var moved = CreateFile("moved.pptx");
        service.AddFiles([first, second]); service.Move(service.Rules[1].Id, MoveDirection.Top);
        Assert.Equal(second, service.Rules[0].FilePath);
        Assert.True(service.SetDuration(service.Rules.Select(x => x.Id), "00:12:34"));
        Assert.All(service.Rules, rule => Assert.Equal("00:12:34", rule.Duration));
        Assert.True(service.Relocate(service.Rules[1].Id, moved, out _));
        Assert.False(service.Remove([service.Rules[0].Id], second, out var error));
        Assert.Contains("正在放映", error);
    }

    [Fact]
    public async Task TimerFinished_PublishesFinalFinishedSnapshot()
    {
        using var timer = new TimerService(); var config = new AppConfig(); config.Timer.DefaultDuration = "00:00:01"; config.Timer.ContinueOvertime = false; timer.Configure(config);
        var completion = new TaskCompletionSource<TimerSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        TimerSnapshot? lastUpdated = null; timer.Updated += (_, value) => lastUpdated = value; timer.Finished += (_, value) => completion.TrySetResult(value);
        timer.Start(); var finished = await completion.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(TimerState.Finished, finished.State); Assert.Equal(TimerState.Finished, lastUpdated?.State); Assert.Equal(TimeSpan.Zero, finished.Remaining);
    }

    [Theory]
    [InlineData(false, false, FlyPPTTimer.Core.Models.TimerState.Running)]
    [InlineData(true, false, FlyPPTTimer.Core.Models.TimerState.Stopped)]
    [InlineData(false, true, FlyPPTTimer.Core.Models.TimerState.Stopped)]
    [InlineData(true, true, FlyPPTTimer.Core.Models.TimerState.Stopped)]
    public void PresentationExit_CombinationsAreDeterministic(bool stop, bool reset, FlyPPTTimer.Core.Models.TimerState expected)
    {
        var config = new AppConfig(); config.Behavior.StopWhenLeavingFullscreen = stop; config.Behavior.ResetWhenLeavingFullscreen = reset;
        using var timer = new TimerService(); timer.Configure(config); var lifecycle = new PresentationLifecycleController(config, timer, new FileRuleService(config));
        lifecycle.Observe(true, CreateFile("show.pptx")); lifecycle.Observe(false, ""); Assert.Equal(expected, timer.State);
    }

    [Fact]
    public void PresentationStart_WhenAutomationDisabled_DoesNotTouchTimer()
    {
        var config = new AppConfig(); config.Behavior.AutoStartOnFullscreen = false;
        using var timer = new TimerService(); timer.Configure(config); var lifecycle = new PresentationLifecycleController(config, timer, new FileRuleService(config));
        lifecycle.Observe(true, CreateFile("manual.pptx")); Assert.Equal(FlyPPTTimer.Core.Models.TimerState.Stopped, timer.State);
    }

    private string CreateFile(string name) { var path = Path.Combine(_directory, name); File.WriteAllBytes(path, [0]); return path; }
    public void Dispose() { try { Directory.Delete(_directory, true); } catch { } }
}
