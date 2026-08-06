using System.Text.Json;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class ConfigSchemaTests
{
    [Fact]
    public void NewConfigurationStartsAtCurrentSchema()
    {
        var config = new AppConfig();

        Assert.Equal(ConfigSchema.Current, config.SchemaVersion);
        Assert.Equal(AppVersion.Current, config.Version);
    }

    [Fact]
    public void LegacyJsonWithoutSchemaVersionIsDetectedAndMigrated()
    {
        var config = JsonSerializer.Deserialize<AppConfig>("""
            {
              "Version": "0.20.5",
              "Timer": { "EnablePerSlideTimer": true },
              "Appearance": { "FontFamily": "Arial" },
              "RemoteControl": {
                "UseRandomPort": true,
                "Port": 52143,
                "Window": { "WidthDip": 1180, "HeightDip": 760, "Maximized": true }
              }
            }
            """, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(config);
        Assert.Equal(0, config!.SchemaVersion);

        ConfigService.Normalize(config);

        Assert.Equal(ConfigSchema.Current, config.SchemaVersion);
        Assert.Equal(AppVersion.Current, config.Version);
        Assert.Equal("Microsoft YaHei UI", config.Appearance.FontFamily);
        Assert.False(config.Timer.EnablePerSlideTimer);
        Assert.False(config.RemoteControl.UseRandomPort);
        Assert.Equal(4080, config.RemoteControl.Port);
        Assert.Equal(700, config.RemoteControl.Window.WidthDip);
        Assert.Equal(510, config.RemoteControl.Window.HeightDip);
        Assert.False(config.RemoteControl.Window.Maximized);
    }

    [Fact]
    public void ApplicationVersionChangeDoesNotOverwriteCurrentSchemaUserSettings()
    {
        var config = new AppConfig
        {
            Version = "3.9.0",
            SchemaVersion = ConfigSchema.Current
        };
        config.Appearance.FontFamily = "Arial";
        config.Timer.EnablePerSlideTimer = true;
        config.Behavior.Prompt1.Text = "自定义提示一";
        config.Behavior.Prompt2.Text = "自定义提示二";
        config.Behavior.EndPrompt.Text = "自定义结束提示";
        config.Behavior.Prompt1.Beep = true;

        ConfigService.Normalize(config);

        Assert.Equal(AppVersion.Current, config.Version);
        Assert.Equal(ConfigSchema.Current, config.SchemaVersion);
        Assert.Equal("Arial", config.Appearance.FontFamily);
        Assert.True(config.Timer.EnablePerSlideTimer);
        Assert.Equal("自定义提示一", config.Behavior.Prompt1.Text);
        Assert.Equal("自定义提示二", config.Behavior.Prompt2.Text);
        Assert.Equal("自定义结束提示", config.Behavior.EndPrompt.Text);
        Assert.True(config.Behavior.Prompt1.Beep);
    }

    [Fact]
    public void LegacyMigrationRunsOnlyOnce()
    {
        var config = new AppConfig
        {
            Version = "0.18.4",
            SchemaVersion = 0
        };
        config.Appearance.FontFamily = "Arial";
        config.Timer.EnablePerSlideTimer = true;

        ConfigService.Normalize(config);
        Assert.Equal("Microsoft YaHei UI", config.Appearance.FontFamily);
        Assert.False(config.Timer.EnablePerSlideTimer);

        config.Version = "4.0.0-alpha.0";
        config.Appearance.FontFamily = "Custom Font";
        config.Timer.EnablePerSlideTimer = true;
        config.Behavior.Prompt1.Text = "迁移后的用户设置";

        ConfigService.Normalize(config);

        Assert.Equal("Custom Font", config.Appearance.FontFamily);
        Assert.True(config.Timer.EnablePerSlideTimer);
        Assert.Equal("迁移后的用户设置", config.Behavior.Prompt1.Text);
        Assert.Equal(ConfigSchema.Current, config.SchemaVersion);
    }

    [Fact]
    public void V0302ConfigurationPreservesUnknownFieldsAtEveryPersistedLevel()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"FlyPPTTimer-Unknown-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "FlyPPTTimer.config.json");
            File.Copy(SourcePath("tests", "FlyPPTTimer.Tests", "Fixtures", "v0.30.2-config.json"), path);
            var service = new ConfigService(new LogService(Path.Combine(directory, "logs")), path);

            var config = service.Load();
            config.Timer.DefaultDuration = "00:10:00";
            service.Save(config);
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            var root = json.RootElement;

            Assert.True(root.GetProperty("FutureRoot").GetProperty("enabled").GetBoolean());
            Assert.Equal(17, root.GetProperty("Timer").GetProperty("FutureTimer").GetInt32());
            Assert.Equal("keep", root.GetProperty("Behavior").GetProperty("FutureBehavior").GetString());
            Assert.Equal(3, root.GetProperty("Behavior").GetProperty("Prompt1").GetProperty("FuturePrompt").GetArrayLength());
            Assert.Equal("#123456", root.GetProperty("Appearance").GetProperty("FutureAppearance").GetString());
            Assert.Equal("value", root.GetProperty("Controls").GetProperty("FutureControl").GetProperty("key").GetString());
            Assert.True(root.GetProperty("RemoteControl").GetProperty("FutureRemote").GetBoolean());
            Assert.Equal(4, root.GetProperty("RemoteControl").GetProperty("Window").GetProperty("FutureWindow").GetInt32());
            Assert.Equal("screen", root.GetProperty("Placement").GetProperty("FuturePlacement").GetString());
            Assert.Equal("keep", root.GetProperty("Rules")[0].GetProperty("FutureRule").GetString());
        }
        finally { Directory.Delete(directory, true); }
    }

    private static string SourcePath(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine([directory.FullName, .. segments]);
            if (File.Exists(path)) return path;
        }
        throw new DirectoryNotFoundException("Unable to locate repository fixture.");
    }
}
