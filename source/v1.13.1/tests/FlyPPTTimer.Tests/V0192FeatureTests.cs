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
        Assert.Contains("SWIPE_DIRECTION_DISTANCE=10", script);
        Assert.Contains("SWIPE_MAX_ANGLE_DEGREES=35", script);
        Assert.Contains("Math.atan2(Math.abs(dy),Math.abs(dx))*180/Math.PI", script);
        Assert.Contains("if(angle>SWIPE_MAX_ANGLE_DEGREES){swipeStart=null", script);
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
