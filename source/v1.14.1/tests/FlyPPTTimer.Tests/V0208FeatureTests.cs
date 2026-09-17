namespace FlyPPTTimer.Tests;

public sealed class V0208FeatureTests
{
    [Fact]
    public void EnglishPresentationLayoutUsesLargerToolbarAndConsistentDetailSpacing()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");

        Assert.Contains("button.Height = 36", form);
        Assert.Contains("RemoteDashboardTheme.CreateFont(10F)", form);
        Assert.Contains("_presentationRoot.Padding = new Padding(0, D(8), 0, 0)", form);
        Assert.Contains("var rows = new[] { 24, 6, 20, 6, 36, 8, 20, 6, 36 }", form);
        Assert.Contains("_ruleEditorCard.Height = D(178)", form);
    }

    [Fact]
    public void PresentationRefreshAvoidsReassigningLocalizedTitleEverySecond()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");
        var row = Read("src", "FlyPPTTimer", "Forms", "RemotePresentationRow.cs");

        Assert.Contains("SetTextIfChanged(_detailTitle", form);
        Assert.Contains("var localized = Localization.T(text)", form);
        Assert.Contains("if (_title.Text != title)", row);
        Assert.Contains("ControlStyles.OptimizedDoubleBuffer", form);
    }

    [Fact]
    public void SettingsExposeSmallTimerVisibilityAndDedicatedBigScreenTarget()
    {
        var model = Read("src", "FlyPPTTimer", "Models", "AppConfig.cs");
        var settings = Read("src", "FlyPPTTimer", "Forms", "SettingsForm.cs");
        var context = Read("src", "FlyPPTTimer", "FlyPPTTimerContext.cs");
        var bigScreen = Read("src", "FlyPPTTimer", "Forms", "BigScreenTimerForm.cs");

        Assert.Contains("BigScreenEnabled", model);
        Assert.Contains("BigScreenDeviceName", model);
        Assert.Contains("\"timerWindowVisible\"", settings);
        Assert.Contains("\"bigScreenEnabled\"", settings);
        Assert.Contains("\"bigScreenTarget\"", settings);
        Assert.Contains("RebuildBigScreenTimer()", context);
        Assert.Contains("Bounds = screen.WorkingArea", bigScreen);
    }

    [Fact]
    public void RoundedSettingsHostsPaintBorderlessFillsWithoutResizeRegions()
    {
        var theme = Read("src", "FlyPPTTimer", "Forms", "ModernTheme.cs");

        Assert.Contains("e.Graphics.Clear(Parent?.BackColor ?? ModernTheme.Card)", theme);
        Assert.Contains("new Rectangle(0, 0, Width, Height)", theme);
        Assert.DoesNotContain("ApplyRoundedRegion(this, CornerRadius)", theme);
    }

    [Fact]
    public void MobileRestartAndBothPresentationCloseActionsAreAvailable()
    {
        var html = Read("src", "FlyPPTTimer", "Web", "index.html");
        var script = Read("src", "FlyPPTTimer", "Web", "app.js");
        var commands = Read("src", "FlyPPTTimer", "Services", "AppCommandService.cs");
        var presentation = Read("src", "FlyPPTTimer", "Services", "PowerPointControlService.cs");
        var desktop = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");

        Assert.Contains("data-command=\"timer.restart\"", html);
        Assert.Contains("case \"timer.restart\"", commands);
        Assert.Contains("ppt.closeActivePresentation", html);
        Assert.Contains("ppt.closeCurrentPresentation", html);
        Assert.Contains("CloseActivePresentation()", presentation);
        Assert.Contains("\"关闭当前文档\"", desktop);
        Assert.Contains("presentationId:selectedPresentationId", script);
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
            Path.Combine(parts)));
}
