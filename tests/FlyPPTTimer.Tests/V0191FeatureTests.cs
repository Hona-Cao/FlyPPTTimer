namespace FlyPPTTimer.Tests;

public sealed class V0191FeatureTests
{
    private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void MobileSwipeTracksFingerAndAnimatesToSettledPage()
    {
        var web = Path.Combine(Root, "src", "FlyPPTTimer", "Web");
        var script = File.ReadAllText(Path.Combine(web, "app.js"));
        var css = File.ReadAllText(Path.Combine(web, "app.css"));
        var markup = File.ReadAllText(Path.Combine(web, "index.html"));

        Assert.Contains("id=\"pagesViewport\"", markup);
        Assert.Contains("id=\"pagesTrack\"", markup);
        Assert.Contains("touchmove", script);
        Assert.Contains("event.preventDefault()", script);
        Assert.Contains("pagesTrack.style.transform=`translate3d(${nextX}px,0,0)`", script);
        Assert.Contains("cubic-bezier(.22,.75,.25,1)", css);
        Assert.Contains("transition:height 260ms ease", css);
    }

    [Fact]
    public void StartingSlideShowRestoresPersistentSettingsAndPreservesRealDirtyState()
    {
        var source = File.ReadAllText(Path.Combine(Root, "src", "FlyPPTTimer", "Services", "PowerPointControlService.cs"));

        Assert.Contains("originalRangeType = (int)showSettings.RangeType", source);
        Assert.Contains("originalStartingSlide = (int)showSettings.StartingSlide", source);
        Assert.Contains("originalEndingSlide = (int)showSettings.EndingSlide", source);
        Assert.Contains("settings.RangeType = originalRangeType", source);
        Assert.Contains("if (!restored || !savedStateKnown || !wasSaved) return", source);
        Assert.Contains("presentation.Saved = true", source);
        Assert.DoesNotContain("MarkManagedPresentationClean", source);
    }

    [Fact]
    public void OverlayAndTrayMenusContainOnlyRequestedFiveCommands()
    {
        var source = File.ReadAllText(Path.Combine(Root, "src", "FlyPPTTimer", "FlyPPTTimerContext.cs"));
        var start = source.IndexOf("private ContextMenuStrip BuildCommandMenu(bool", StringComparison.Ordinal);
        var end = source.IndexOf("private void ShowCommandMenuAtCursor", start, StringComparison.Ordinal);
        var menu = source[start..end];

        foreach (var label in new[] { "重置计时窗口位置", "静音/取消静音", "远程控制", "设置", "退出" })
            Assert.Contains($"menu.Items.Add(\"{label}\"", menu);

        foreach (var removed in new[] { "开始/暂停", "停止/重置", "显示/隐藏计时窗口", "触发闪烁" })
            Assert.DoesNotContain($"menu.Items.Add(\"{removed}\"", menu);

        Assert.Contains("_overlayMenu = BuildCommandMenu();", source);
        Assert.Contains("_trayMenu = BuildCommandMenu(includeUpdateCheck: true);", source);
    }
}
