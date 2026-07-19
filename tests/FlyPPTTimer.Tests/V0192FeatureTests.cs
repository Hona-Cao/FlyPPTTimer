namespace FlyPPTTimer.Tests;

public sealed class V0192FeatureTests
{
    [Fact]
    public void SwipeCanStartAnywhereAndInterruptAnInFlightTransition()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "FlyPPTTimer", "Web"));
        var script = File.ReadAllText(Path.Combine(root, "app.js"));

        Assert.Contains("function readTrackX()", script);
        Assert.Contains("function freezeTrack()", script);
        Assert.Contains("getComputedStyle(pagesTrack).transform", script);
        Assert.Contains("baseX:freezeTrack()", script);
        Assert.DoesNotContain("event.target.closest('input,button", script);
        Assert.DoesNotContain("Math.abs(dx)<=Math.abs(dy)", script);
        Assert.Contains("Math.abs(dx)<6", script);
        Assert.Contains("Math.abs(dx)>=18||Math.abs(velocity)>=.12", script);
        Assert.Contains("suppressSwipeClick", script);
    }

    [Fact]
    public void TabClicksAlsoReplaceTheCurrentAnimationFromItsVisiblePosition()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "FlyPPTTimer", "Web"));
        var script = File.ReadAllText(Path.Combine(root, "app.js"));

        Assert.Contains("if(animate)freezeTrack()", script);
        Assert.Contains("tab.addEventListener('click',()=>activatePage", script);
        Assert.Contains("pagesTrack.addEventListener('transitionend'", script);
    }
}
