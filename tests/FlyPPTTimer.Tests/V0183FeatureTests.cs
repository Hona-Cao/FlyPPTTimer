using System.Drawing;
using FlyPPTTimer.Forms;

namespace FlyPPTTimer.Tests;

public sealed class V0183FeatureTests
{
    [Fact]
    public void MinuteSecond_DisplayAnchorsColonAtWindowCenter()
    {
        using var font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
        var layout = TimeDisplayControl.MeasureLogicalLayout("08:00", font);
        var origin = TimeDisplayControl.CalculateDrawOrigin(new Size(140, 50), layout);

        Assert.InRange(origin.X + layout.AnchorOffset, 69.99F, 70.01F);
        Assert.InRange(origin.Y + layout.InkCenterY, 24.99F, 25.01F);
    }

    [Fact]
    public void HourMinuteSecond_DisplayAnchorsMinutePairAtWindowCenter()
    {
        using var font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
        var layout = TimeDisplayControl.MeasureLogicalLayout("01:08:00", font);
        var origin = TimeDisplayControl.CalculateDrawOrigin(new Size(180, 60), layout);

        Assert.InRange(origin.X + layout.AnchorOffset, 89.99F, 90.01F);
        Assert.InRange(origin.Y + layout.InkCenterY, 29.99F, 30.01F);
    }

    [Fact]
    public void RepeatedOddEvenResizes_DoNotAccumulateHorizontalDrift()
    {
        var canonicalCenter = new PointF(900.5F, 500.5F);
        var area = new Rectangle(0, 0, 1920, 1080);
        Point? firstSmall = null;

        for (var i = 0; i < 20; i++)
        {
            var small = TimerOverlayForm.LocationFromCenter(canonicalCenter, new Size(140, 50), area);
            _ = TimerOverlayForm.LocationFromCenter(canonicalCenter, new Size(191, 71), area);
            firstSmall ??= small;
            Assert.Equal(firstSmall.Value, small);
        }
    }

    [Fact]
    public void ContextKeepsCanonicalPerScreenCentersInsteadOfReReadingRoundedBounds()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var context = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "FlyPPTTimerContext.cs"));
        var overlay = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "TimerOverlayForm.cs"));

        Assert.Contains("Dictionary<string, PointF> _overlayCenters", context);
        Assert.Contains("new Dictionary<string, PointF>(_overlayCenters", context);
        Assert.Contains("TimeDisplayControl", overlay);
        Assert.DoesNotContain("TextAlign = ContentAlignment.MiddleCenter", overlay);
    }
}
