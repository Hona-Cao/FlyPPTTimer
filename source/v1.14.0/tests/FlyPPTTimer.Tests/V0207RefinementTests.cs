namespace FlyPPTTimer.Tests;

public sealed class V0207RefinementTests
{
    [Fact]
    public void EditableReadOnlyAndDisabledControlsHaveDistinctFills()
    {
        var modern = Read("src", "FlyPPTTimer", "Forms", "ModernTheme.cs");
        var remote = Read("src", "FlyPPTTimer", "Forms", "RemoteDashboardTheme.cs");

        Assert.Contains("ControlFill = Color.FromArgb(241, 248, 252)", modern);
        Assert.Contains("ReadOnlyFill = Color.FromArgb(229, 236, 240)", modern);
        Assert.Contains("Field = Color.FromArgb(241, 248, 255)", remote);
        Assert.Contains("ReadOnlyField = Color.FromArgb(237, 241, 245)", remote);
        Assert.Contains("DisabledField = Color.FromArgb(244, 246, 249)", remote);
    }

    [Fact]
    public void PresentationRowsContainOnlyFileNameAndDuration()
    {
        var row = Read("src", "FlyPPTTimer", "Forms", "RemotePresentationRow.cs");

        Assert.Contains("RowCount = 2", row);
        Assert.Contains("_title.Text", row);
        Assert.Contains("_duration.Text", row);
        Assert.DoesNotContain("_status", row);
        Assert.DoesNotContain("_toggle", row);
        Assert.DoesNotContain("EnabledChangedByUser", row);
    }

    [Fact]
    public void PresentationToolbarHasFourLargerActionsAndNoFeedbackRow()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");

        Assert.Contains("CreateActionButton(\"清空列表\"", form);
        Assert.Contains("ClearPresentationRules()", form);
        Assert.Contains("button.Font = RemoteDashboardTheme.CreateFont(10F)", form);
        Assert.DoesNotContain("root.Controls.Add(BuildPresentationStatus()", form);
        Assert.DoesNotContain("flow.Controls.Add(BuildDangerActions())", form);
    }

    [Fact]
    public void NavigationIsCompactSpacedAndSelectedWithoutOutline()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");
        var theme = Read("src", "FlyPPTTimer", "Forms", "RemoteDashboardTheme.cs");
        var button = Read("src", "FlyPPTTimer", "Forms", "RemoteTextButton.cs");

        Assert.Contains("NavigationButtonGap = 12", theme);
        Assert.Contains("Localization.IsEnglish ? 142 : 108", form);
        Assert.Contains("Color.Transparent, RemoteDashboardTheme.Accent", button);
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
            Path.Combine(parts)));
}
