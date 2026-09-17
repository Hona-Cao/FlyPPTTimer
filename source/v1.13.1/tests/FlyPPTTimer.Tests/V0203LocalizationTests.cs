using FlyPPTTimer.Services;
using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FlyPPTTimer.Tests;

[Collection("Localization UI")]
public sealed class V0203LocalizationTests
{
    [Fact]
    public void LanguageCodesAreNormalizedAndSystemFallbackIsSupported()
    {
        Assert.Equal(Localization.Auto, Localization.Normalize(null));
        Assert.Equal(Localization.Auto, Localization.Normalize("fr"));
        Assert.Equal(Localization.English, Localization.Normalize("en"));
        Assert.Equal(Localization.SimplifiedChinese, Localization.Normalize("zh-CN"));
        Assert.Contains(Localization.DetectSystemLanguage(), new[] { Localization.English, Localization.SimplifiedChinese });
    }

    [Fact]
    public void EnglishPresentationLayerTranslatesCoreCommands()
    {
        try
        {
            Localization.Initialize(Localization.English);
            Assert.Equal("Settings", Localization.T("设置"));
            Assert.Equal("Countdown", Localization.T("倒计时"));
            Assert.Equal("Remote Control", Localization.T("远程控制"));
            Assert.Equal("3 items", Localization.T("3 项"));
            Assert.Equal("Alert 1 flash style", Localization.T("Alert 1闪烁样式"));
        }
        finally
        {
            Localization.Initialize(Localization.SimplifiedChinese);
        }
    }

    [Fact]
    public void EnglishSettingsStartCleanAndContainNoChineseDisplayText()
    {
        try
        {
            Localization.Initialize(Localization.English);
            var config = new AppConfig { Language = Localization.English };
            using var remote = new RemoteControlService(() => config, next => config = next, null!, null, TestLog.Create());
            using var form = new SettingsForm(config, remote, new NetworkAddressService());

            var dirty = (bool)(typeof(SettingsForm)
                .GetField("_isDirty", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(form) ?? true);
            Assert.False(dirty);

            var visibleTextControls = Descendants(form)
                .Where(control => control is Label or Button or CheckBox or Form
                    || control is TextBox { ReadOnly: true })
                .Where(control => !string.IsNullOrWhiteSpace(control.Text))
                .Where(control => !control.Text.Contains("http://", StringComparison.OrdinalIgnoreCase))
                .Where(control => !control.Text.TrimStart().StartsWith("netsh ", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var mixed = visibleTextControls
                .Where(control => Regex.IsMatch(control.Text, @"[\u4e00-\u9fff]"))
                .Select(control => $"{control.GetType().Name}: {control.Text}")
                .ToArray();
            Assert.Empty(mixed);

            Assert.All(
                visibleTextControls.OfType<Label>(),
                label => Assert.False(label.AutoEllipsis));

            using var dashboard = new RemoteControlForm(config, remote, new NetworkAddressService(), _ => { });
            var dashboardMixed = Descendants(dashboard)
                .Where(control => control is not TextBox { ReadOnly: false })
                .Where(control => !string.IsNullOrWhiteSpace(control.Text))
                .Where(control => !control.Text.Contains("http://", StringComparison.OrdinalIgnoreCase))
                .Where(control => !control.Text.Contains(@":\", StringComparison.Ordinal))
                .Where(control => Regex.IsMatch(control.Text, @"[\u4e00-\u9fff]"))
                .Select(control => $"{control.GetType().Name}: {control.Text}")
                .ToArray();
            Assert.Empty(dashboardMixed);
        }
        finally
        {
            Localization.Initialize(Localization.SimplifiedChinese);
        }
    }

    [Fact]
    public void ReleasePackagingProvidesInstallerLanguagesAndMarker()
    {
        var script = File.ReadAllText(Path.Combine(RepoRoot(), "package_release.ps1"));
        Assert.Contains("Name: \"english\"", script);
        Assert.Contains("Name: \"chinesesimp\"", script);
        Assert.Contains("install-language.txt", script);
        Assert.Contains("ActiveLanguage", script);
    }

    [Fact]
    public void WebRemoteUsesDeviceLanguageWithoutManualSelection()
    {
        var webRoot = Path.Combine(RepoRoot(), "src", "FlyPPTTimer", "Web");
        var html = File.ReadAllText(Path.Combine(webRoot, "index.html"));
        var script = File.ReadAllText(Path.Combine(webRoot, "app.js"));
        Assert.DoesNotContain("languageSelect", html);
        Assert.Contains("navigator.language", script);
        Assert.DoesNotContain("flyppt-language", script);
    }

    [Fact]
    public void LanguageChangeOffersImmediateRestartAndUsesAParentWaitHandshake()
    {
        var root = RepoRoot();
        var settings = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "SettingsForm.cs"));
        var context = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "FlyPPTTimerContext.cs"));
        var program = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Program.cs"));

        Assert.Contains("RestartRequested", settings);
        Assert.Contains("MessageBoxButtons.YesNo", settings);
        Assert.Contains("RestartRequested", context);
        Assert.Contains("--restart-after", context);
        Assert.Contains("--restart-after", program);
        Assert.Contains("WaitForExit", program);
    }

    private static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static IEnumerable<Control> Descendants(Control root)
    {
        yield return root;
        foreach (Control child in root.Controls)
        {
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}

[CollectionDefinition("Localization UI", DisableParallelization = true)]
public sealed class LocalizationUiCollection;
