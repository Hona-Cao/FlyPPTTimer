using System.Drawing;
using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

/// <summary>Pure physical-pixel placement rules shared by the compatibility and WPF timer windows.</summary>
public static class OverlayPlacementService
{
    public static PointF CalculateOrigin(
        Rectangle workingArea,
        int dpi,
        OverlayAnchor anchor,
        decimal offsetXPercent,
        decimal offsetYPercent)
    {
        var baseline = RemoteWindowLayoutService.DipToPhysical(new Size(140, 50), dpi);
        var x = anchor switch
        {
            OverlayAnchor.TopCenter or OverlayAnchor.Center or OverlayAnchor.BottomCenter => workingArea.Left + workingArea.Width / 2f,
            OverlayAnchor.TopRight or OverlayAnchor.MiddleRight or OverlayAnchor.BottomRight => workingArea.Right - baseline.Width / 2f,
            _ => workingArea.Left + baseline.Width / 2f
        };
        var y = anchor switch
        {
            OverlayAnchor.MiddleLeft or OverlayAnchor.Center or OverlayAnchor.MiddleRight => workingArea.Top + workingArea.Height / 2f,
            OverlayAnchor.BottomLeft or OverlayAnchor.BottomCenter or OverlayAnchor.BottomRight => workingArea.Bottom - baseline.Height / 2f,
            _ => workingArea.Top + baseline.Height / 2f
        };

        x += (float)(workingArea.Width * (double)offsetXPercent / 100d);
        y += (float)(workingArea.Height * (double)offsetYPercent / 100d);
        return new PointF(x, y);
    }

    public static Point LocationFromCenter(PointF center, Size size) => new(
        (int)Math.Round(center.X - size.Width / 2f, MidpointRounding.AwayFromZero),
        (int)Math.Round(center.Y - size.Height / 2f, MidpointRounding.AwayFromZero));
}
