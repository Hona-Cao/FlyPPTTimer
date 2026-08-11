using FlyPPTTimer.Native;

namespace FlyPPTTimer.Tests;

public sealed class NativeMethodsTests
{
    [Fact]
    public void InvalidWindowHandleReturnsEmptyProcessName()
    {
        Assert.Equal("", NativeMethods.GetProcessName(IntPtr.Zero));
    }

    [Fact]
    public void ProcessLookupUsesDeterministicDisposal()
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(
            root,
            "src", "FlyPPTTimer", "Native", "NativeMethods.cs"));

        Assert.Contains("using var process = Process.GetProcessById", source);
        Assert.DoesNotContain("return Process.GetProcessById", source);
    }
}
