namespace FlyPPTTimer.Tests;

public sealed class V0187FeatureTests
{
    [Fact]
    public void MobileBlackoutControlsAreAlwaysPresentInBothPages()
    {
        var root = RepositoryRoot();
        var markup = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Web", "index.html"));
        var script = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Web", "app.js"));

        Assert.Equal(2, markup.Split("data-command=\"timeup.dismiss\"").Length - 1);
        Assert.DoesNotContain("data-command=\"timeup.dismiss\" class=\"warning\" hidden", markup);
        Assert.Contains("当前无“时间到”黑屏", script);
    }

    [Fact]
    public void WpsOuterPresentationFrameIsMaximizedOnFirstShow()
    {
        var root = RepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Services", "PowerPointControlService.cs"));

        Assert.Contains("WpsFirstFrameMaximizer", source);
        Assert.Contains("PP12FrameClass", source);
        Assert.Contains("SetWinEventHook", File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Native", "NativeMethods.cs")));
        Assert.Contains("FindWpsPresentationFrame", source);
    }

    private static string RepositoryRoot() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
