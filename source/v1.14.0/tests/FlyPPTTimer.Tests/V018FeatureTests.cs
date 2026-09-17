using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class V018FeatureTests
{
    [Fact]
    public void Defaults_UseMedicalPaletteAndRequestedWindowMetrics()
    {
        var appearance = new AppearanceSettings();
        Assert.Equal("医疗卫生（蓝白）", appearance.ColorScheme);
        Assert.Equal(100, appearance.Width);
        Assert.Equal(35, appearance.Height);
        Assert.Equal(18F, appearance.FontSize);
        Assert.Equal("#0B3A66", appearance.TextColor);
        Assert.Equal("#F3F8FC", appearance.BackgroundColor);
    }

    [Fact]
    public void EveryIndustryPresetActuallyChangesStoredColors()
    {
        foreach (var preset in AppearancePresetService.Presets)
        {
            var appearance = new AppearanceSettings();
            Assert.True(AppearancePresetService.Apply(preset.Name, appearance));
            Assert.Equal(preset.Name, appearance.ColorScheme);
            Assert.Equal(preset.TextColor, appearance.TextColor);
            Assert.Equal(preset.BackgroundColor, appearance.BackgroundColor);
        }
    }

    [Fact]
    public void Mp3Import_CopiesIntoApplicationOwnedDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "FlyPPTTimer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "source.mp3");
        File.WriteAllBytes(source, [1, 2, 3, 4]);
        var owned = Path.Combine(root, "owned");

        var result = AlertSoundStorage.ImportSound(source, "prompt1", owned);
        File.Delete(source);

        Assert.True(File.Exists(result));
        Assert.Equal([1, 2, 3, 4], File.ReadAllBytes(result));
        Directory.Delete(root, true);
    }

    [Fact]
    public void RemoteAddresses_RejectLoopbackAndProxyRanges()
    {
        Assert.False(NetworkAddressService.IsLanAddress("127.0.0.1"));
        Assert.False(NetworkAddressService.IsLanAddress("198.18.0.1"));
        Assert.True(NetworkAddressService.IsProxyAddress("198.18.0.1", "Ethernet"));
        Assert.True(NetworkAddressService.IsLanAddress("192.168.1.8"));
    }

    [Fact]
    public void TimerTextWidthIncludesSafetyPadding()
    {
        using var font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
        var measured = TextRenderer.MeasureText("00:08:00", font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
        Assert.True(TimerOverlayForm.CalculateRequiredWidth("00:08:00", font) >= measured + 4);
    }

    [Fact]
    public void CountUp_UsesTheSameNearTargetPromptLogic()
    {
        var config = new AppConfig();
        config.Behavior.Prompt1.Speak = false;
        config.Behavior.Prompt1.PlaySound = false;
        config.Behavior.Prompt1.TriggerBeforeEndSeconds = 120;
        config.Behavior.Prompt2.Enabled = false;
        var alerts = new AlertService(TestLog.Create());
        var visualCount = 0;
        alerts.PromptVisualRequested += (_, _) => visualCount++;
        var snapshot = new TimerSnapshot(
            TimerState.Running,
            TimerMode.CountUp,
            TimeSpan.FromMinutes(7),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(7),
            TimeSpan.FromMinutes(8),
            false);

        alerts.CheckPrompts(config, snapshot);

        Assert.Equal(1, visualCount);
    }

    [Fact]
    public void OldDefaultAppearanceMigratesToV018DefaultsWithoutOverwritingCustomScheme()
    {
        var old = new AppConfig { Version = "0.17.0" };
        old.Appearance.ColorScheme = "默认";
        old.Appearance.Width = 200;
        old.Appearance.FontSize = 20;
        ConfigService.Normalize(old);
        Assert.Equal("医疗卫生（蓝白）", old.Appearance.ColorScheme);
        Assert.Equal(160, old.Appearance.Width);
        Assert.Equal(18F, old.Appearance.FontSize);

        var custom = new AppConfig { Version = "0.17.0" };
        custom.Appearance.ColorScheme = "自定义";
        custom.Appearance.TextColor = "#123456";
        ConfigService.Normalize(custom);
        Assert.Equal("自定义", custom.Appearance.ColorScheme);
        Assert.Equal("#123456", custom.Appearance.TextColor);
    }

    [Fact]
    public void Settings_RemoveStartupPlaceholderAndEditablePromptText()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var settings = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "SettingsForm.cs"));
        Assert.DoesNotContain("开机自启（后续版本）", settings);
        Assert.DoesNotContain("提示1文本", settings);
        Assert.DoesNotContain("提示2文本", settings);
        Assert.Contains("曹虎男", settings);
        Assert.Contains("选择文件", settings);
    }
}
