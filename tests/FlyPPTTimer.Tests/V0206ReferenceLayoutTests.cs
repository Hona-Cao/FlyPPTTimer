using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class V0206ReferenceLayoutTests
{
    [Fact]
    public void RemoteWindowMatchesReferenceSizeAndBrandTitle()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");

        Assert.Equal(700, RemoteWindowLayoutService.DesignClientWidthDip);
        Assert.Equal(510, RemoteWindowLayoutService.DesignClientHeightDip);
        Assert.Equal(700, RemoteWindowLayoutService.MinimumClientWidthDip);
        Assert.Equal(510, RemoteWindowLayoutService.MinimumClientHeightDip);
        Assert.Contains("Text = \"FlyPPTTimer\"", form);
    }

    [Fact]
    public void ReferenceColumnsAndCompactCardsArePreserved()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");

        Assert.Contains("new ColumnStyle(SizeType.Percent, 41.5F)", form);
        Assert.Contains("new ColumnStyle(SizeType.Percent, 58.5F)", form);
        Assert.Contains("new ColumnStyle(SizeType.Percent, 44)", form);
        Assert.Contains("new ColumnStyle(SizeType.Percent, 56)", form);
        Assert.Contains("_ruleEditorCard.Height = D(178)", form);
        Assert.Contains("_presentationActionsCard.Height = D(152)", form);
        Assert.DoesNotContain("flow.Controls.Add(BuildDangerActions())", form);
    }

    [Fact]
    public void FixedRowsAreConvertedForTheActiveDpi()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");

        Assert.Contains("int D(int dip) => LogicalToDeviceUnits(dip)", form);
        Assert.Contains("_workspace.RowStyles[0].Height = D(RemoteDashboardTheme.NavigationHeight)", form);
        Assert.Contains("_connectionBody.RowStyles[0].Height = D(45)", form);
        Assert.Contains("_browserLayout.RowStyles[i].Height = D(rows[i])", form);
        Assert.Contains("_ruleEditorLayout.RowStyles[i].Height = D(rows[i])", form);
    }

    [Fact]
    public void UpgradeMigratesTheOldLargeRemoteWindow()
    {
        var config = new AppConfig
        {
            Version = "0.20.5",
            RemoteControl = new RemoteControlSettings
            {
                Window = new RemoteWindowPlacement
                {
                    HasValue = true,
                    WidthDip = 1180,
                    HeightDip = 760,
                    Maximized = true
                }
            }
        };

        ConfigService.Normalize(config);

        Assert.Equal("0.30.2", config.Version);
        Assert.Equal(700, config.RemoteControl.Window.WidthDip);
        Assert.Equal(510, config.RemoteControl.Window.HeightDip);
        Assert.False(config.RemoteControl.Window.Maximized);
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
            Path.Combine(parts)));
}
