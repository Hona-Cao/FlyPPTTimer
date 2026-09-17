using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Tests;

public sealed class V0189FeatureTests
{
    [Fact]
    public void TopCenterSettingDefinesOneImmutableWindowCenterOrigin()
    {
        var area = new Rectangle(0, 0, 2560, 1380);
        var origin = TimerOverlayForm.CalculateOrigin(area, 120, OverlayAnchor.TopCenter, 0, 0.5m);
        var physicalSize = RemoteWindowLayoutService.DipToPhysical(new Size(100, 30), 120);
        var location = TimerOverlayForm.LocationFromCenter(origin, physicalSize, area);
        var actualCenter = new PointF(location.X + physicalSize.Width / 2f, location.Y + physicalSize.Height / 2f);

        Assert.Equal(1280F, origin.X);
        Assert.InRange(origin.Y, 38.39F, 38.41F);
        Assert.InRange(Math.Abs(actualCenter.X - origin.X), 0F, 0.5F);
        Assert.InRange(Math.Abs(actualCenter.Y - origin.Y), 0F, 0.5F);
    }

    [Fact]
    public void TimerUsesOneSingleLineCenteredLikeTextInsideAShape()
    {
        using var font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
        var layout = TimeDisplayControl.MeasureLogicalLayout("08:00", font);
        var origin = TimeDisplayControl.CalculateDrawOrigin(new Size(100, 30), layout);

        Assert.Equal(layout.Width / 2f, layout.AnchorOffset);
        Assert.InRange(origin.X + layout.Width / 2f, 49.99F, 50.01F);
        Assert.InRange(origin.Y + layout.Height / 2f, 14.99F, 15.01F);

        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "TimerOverlayForm.cs"));
        Assert.Contains("TextFormatFlags.SingleLine", source);
        Assert.Contains("TextFormatFlags.HorizontalCenter", source);
        Assert.Contains("TextFormatFlags.VerticalCenter", source);
    }
}
