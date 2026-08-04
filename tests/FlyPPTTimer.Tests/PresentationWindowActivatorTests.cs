using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class PresentationWindowActivatorTests
{
    [Fact]
    public void ZeroHandleFailsWithoutCallingNativeApi()
    {
        var native = new FakeNativeApi { Maximized = true };
        var activator = new PresentationWindowActivator(native);

        var result = activator.Activate(IntPtr.Zero, @"C:\Decks\demo.pptx", "放映窗口");

        Assert.False(result.Success);
        Assert.Equal("；未找到目标放映窗口", result.Message);
        Assert.Equal(0, native.TotalCalls);
    }

    [Fact]
    public void MaximizedForegroundWindowSucceedsWithoutTopmostPulse()
    {
        var native = new FakeNativeApi
        {
            Maximized = true,
            ShowResults = [new(true, 0)],
            BringResults = [new(true, 0)],
            ForegroundResults = [new(true, 0)]
        };
        var activator = new PresentationWindowActivator(native);

        var result = activator.Activate(new IntPtr(0x1234), "demo.pptx", "文稿窗口");

        Assert.True(result.Success);
        Assert.Equal("；已最大化并置前", result.Message);
        Assert.Equal(0, native.PulseCount);
        Assert.Equal(1, native.BringCount);
        Assert.Equal(1, native.ForegroundCount);
    }

    [Fact]
    public void FailedForegroundAttemptUsesTopmostPulseAndRetries()
    {
        var native = new FakeNativeApi
        {
            Maximized = true,
            ShowResults = [new(true, 0)],
            BringResults = [new(false, 5), new(true, 0)],
            ForegroundResults = [new(false, 5), new(true, 0)]
        };
        var activator = new PresentationWindowActivator(native);

        var result = activator.Activate(new IntPtr(0x2345), "demo.pptx", "文稿窗口");

        Assert.True(result.Success);
        Assert.Equal("；已最大化并置前", result.Message);
        Assert.Equal(1, native.PulseCount);
        Assert.Equal(2, native.BringCount);
        Assert.Equal(2, native.ForegroundCount);
    }

    [Fact]
    public void MaximizedWindowRemainsSuccessfulWhenWindowsDeniesForeground()
    {
        var native = new FakeNativeApi
        {
            Maximized = true,
            ShowResults = [new(true, 0)],
            BringResults = [new(true, 0), new(true, 0)],
            ForegroundResults = [new(false, 5), new(false, 5)]
        };
        var activator = new PresentationWindowActivator(native);

        var result = activator.Activate(new IntPtr(0x3456), "demo.pptx", "放映窗口");

        Assert.True(result.Success);
        Assert.Equal("；已最大化（Windows 未允许强制置前）", result.Message);
        Assert.Equal(1, native.PulseCount);
    }

    [Fact]
    public void NonMaximizedWindowReturnsDiagnosticFailureAndWarns()
    {
        var warnings = new List<string>();
        var native = new FakeNativeApi
        {
            Maximized = false,
            ShowResults = [new(false, 87)],
            BringResults = [new(true, 0)],
            ForegroundResults = [new(true, 0)]
        };
        var activator = new PresentationWindowActivator(native, warnings.Add);

        var result = activator.Activate(new IntPtr(0x4567), @"C:\Decks\demo.pptx", "文稿窗口");

        Assert.False(result.Success);
        Assert.Contains("文稿已打开但最大化或置前失败", result.Message);
        Assert.Contains("HWND=0x4567", result.Message);
        Assert.Contains("ShowWindow=False/错误87", result.Message);
        Assert.Single(warnings);
        Assert.Contains(@"path=C:\Decks\demo.pptx", warnings[0]);
    }

    private sealed class FakeNativeApi : IPresentationWindowNativeApi
    {
        public Queue<NativeWindowCallResult> ShowResults { get; init; } = new([new(true, 0)]);
        public Queue<NativeWindowCallResult> BringResults { get; init; } = new([new(true, 0)]);
        public Queue<NativeWindowCallResult> ForegroundResults { get; init; } = new([new(true, 0)]);
        public bool Maximized { get; init; }
        public int ShowCount { get; private set; }
        public int BringCount { get; private set; }
        public int ForegroundCount { get; private set; }
        public int PulseCount { get; private set; }
        public int TotalCalls => ShowCount + BringCount + ForegroundCount + PulseCount;

        public NativeWindowCallResult ShowMaximized(IntPtr hwnd)
        {
            ShowCount++;
            return Next(ShowResults);
        }

        public NativeWindowCallResult BringToTop(IntPtr hwnd)
        {
            BringCount++;
            return Next(BringResults);
        }

        public NativeWindowCallResult SetForeground(IntPtr hwnd)
        {
            ForegroundCount++;
            return Next(ForegroundResults);
        }

        public void PulseTopmost(IntPtr hwnd) => PulseCount++;

        public bool IsMaximized(IntPtr hwnd) => Maximized;

        private static NativeWindowCallResult Next(Queue<NativeWindowCallResult> results) =>
            results.Count > 1 ? results.Dequeue() : results.Peek();
    }
}
