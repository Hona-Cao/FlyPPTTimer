using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;
using System.Reflection;

namespace FlyPPTTimer.Tests;

[Collection("Localization UI")]
public sealed class V0204LayoutTests
{
    [Fact]
    public void SettingsNavigationStaysOnOneLineAndDefinesItsRequiredMinimumWidth()
    {
        Localization.Initialize(Localization.SimplifiedChinese);
        var config = new AppConfig();
        using var remote = new RemoteControlService(() => config, next => config = next, null!, null, TestLog.Create());
        using var form = new SettingsForm(config, remote, new NetworkAddressService());

        var nav = (FlowLayoutPanel)typeof(SettingsForm)
            .GetField("_navBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;
        var settingsArea = (TableLayoutPanel)typeof(SettingsForm)
            .GetField("_settingsArea", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;

        Assert.False(nav.WrapContents);
        Assert.Equal(6, nav.Controls.Count);
        Assert.Equal(64, settingsArea.RowStyles[0].Height);
        Assert.True(form.MinimumSize.Width >= 780);
    }

    [Fact]
    public void RemoteConnectionUsesAutomaticAddressAndDoesNotExposePortEditing()
    {
        var formSource = File.ReadAllText(Path.Combine(RepoRoot(), "src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs"));
        var selectorSource = File.ReadAllText(Path.Combine(RepoRoot(), "src", "FlyPPTTimer", "Forms", "RemoteAddressSelector.cs"));
        var defaults = new RemoteControlSettings();

        Assert.False(defaults.UseRandomPort);
        Assert.Equal(4080, defaults.Port);
        Assert.DoesNotContain("TextBox _port", formSource);
        Assert.DoesNotContain("CreateFieldLabel(\"端口\"", formSource);
        Assert.DoesNotContain("CreateInputHost(_port", formSource);
        Assert.DoesNotContain("ContextMenuStrip", selectorSource);
        Assert.DoesNotContain("RemoteTextButton", selectorSource);

        using var selector = new RemoteAddressSelector();
        selector.SetAddresses(["192.168.1.20", "192.168.1.21"], null);
        Assert.Equal("192.168.1.20", selector.SelectedAddress);
        Assert.Empty(Descendants(selector).OfType<Button>());
    }

    [Fact]
    public void UpgradeMigratesTheOldRandomPortDefaultTo4080ButKeepsExplicitFixedPorts()
    {
        var oldRandom = new AppConfig
        {
            Version = "0.20.3",
            SchemaVersion = 0,
            RemoteControl = new RemoteControlSettings { UseRandomPort = true, Port = 52143 }
        };
        ConfigService.Normalize(oldRandom);
        Assert.False(oldRandom.RemoteControl.UseRandomPort);
        Assert.Equal(4080, oldRandom.RemoteControl.Port);

        var oldFixed = new AppConfig
        {
            Version = "0.20.3",
            SchemaVersion = 0,
            RemoteControl = new RemoteControlSettings { UseRandomPort = false, Port = 5090 }
        };
        ConfigService.Normalize(oldFixed);
        Assert.False(oldFixed.RemoteControl.UseRandomPort);
        Assert.Equal(5090, oldFixed.RemoteControl.Port);
    }

    private static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
