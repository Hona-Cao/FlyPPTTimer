using System.Text.Json;
using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class RemoteWindowLayoutServiceTests
{
    [Theory]
    [InlineData(96, 1180, 760)]
    [InlineData(120, 1475, 950)]
    [InlineData(144, 1770, 1140)]
    [InlineData(168, 2065, 1330)]
    [InlineData(192, 2360, 1520)]
    public void DesignClientSizeConvertsAcrossSupportedDpis(int dpi, int width, int height)
    {
        var physical = RemoteWindowLayoutService.DipToPhysical(
            new Size(RemoteWindowLayoutService.DesignClientWidthDip, RemoteWindowLayoutService.DesignClientHeightDip),
            dpi);

        Assert.Equal(new Size(width, height), physical);
        Assert.Equal(new Size(1180, 760), RemoteWindowLayoutService.PhysicalToDip(physical, dpi));
    }

    [Theory]
    [InlineData(96)]
    [InlineData(120)]
    [InlineData(144)]
    [InlineData(168)]
    [InlineData(192)]
    public void DipRoundTripIsStable(int dpi)
    {
        foreach (var dip in new[] { 8, 10, 14, 40, 158, 174, 660, 1000, 1180 })
            Assert.Equal(dip, RemoteWindowLayoutService.PhysicalToDip(
                RemoteWindowLayoutService.DipToPhysical(dip, dpi), dpi));
    }

    [Fact]
    public void ResponsiveModeUsesLogicalClientSizeOnly()
    {
        Assert.Equal(RemoteLayoutMode.Standard,
            RemoteWindowLayoutService.GetLayoutMode(new Size(1400, 900), 120));
        Assert.Equal(RemoteLayoutMode.Compact,
            RemoteWindowLayoutService.GetLayoutMode(new Size(1399, 900), 120));
        Assert.Equal(RemoteLayoutMode.Compact,
            RemoteWindowLayoutService.GetLayoutMode(new Size(1400, 899), 120));
    }

    [Fact]
    public void CompactModeDoesNotChangeBodyFont()
    {
        Assert.Equal(
            RemoteWindowLayoutService.GetBodyFontSize(RemoteLayoutMode.Standard),
            RemoteWindowLayoutService.GetBodyFontSize(RemoteLayoutMode.Compact));
        Assert.Equal(9.5F, RemoteWindowLayoutService.GetBodyFontSize(RemoteLayoutMode.Compact));
    }

    [Fact]
    public void PositionRatiosRoundTrip()
    {
        var screen = Screen("DISPLAY2", new Rectangle(1920, 0, 2560, 1400), 120, false);
        var bounds = new Rectangle(2200, 140, 1500, 1000);
        var captured = RemoteWindowLayoutService.CapturePlacement(
            bounds,
            new Size(1475, 950),
            screen,
            false);
        var restored = RemoteWindowLayoutService.CreateRestorePlan(
            captured,
            new[] { screen },
            null,
            new Size(25, 50));

        Assert.Equal("DISPLAY2", captured.ScreenDeviceName);
        Assert.Equal(1180, captured.WidthDip);
        Assert.Equal(760, captured.HeightDip);
        Assert.InRange(Math.Abs(restored.WindowBoundsPhysical.Left - bounds.Left), 0, 1);
        Assert.InRange(Math.Abs(restored.WindowBoundsPhysical.Top - bounds.Top), 0, 1);
    }

    [Fact]
    public void MissingSavedScreenFallsBackToPrimary()
    {
        var screens = new[]
        {
            Screen("DISPLAY1", new Rectangle(0, 0, 1920, 1040), 96, true),
            Screen("DISPLAY2", new Rectangle(1920, 0, 2560, 1400), 144, false)
        };
        var selected = RemoteWindowLayoutService.SelectScreen(screens, "REMOVED", null);

        Assert.Equal("DISPLAY1", selected.DeviceName);
        Assert.True(selected.Primary);
    }

    [Fact]
    public void RestoredBoundsAreAlwaysInsideWorkingArea()
    {
        var screen = Screen("DISPLAY1", new Rectangle(-1920, -200, 1600, 900), 192, true);
        var placement = new RemoteWindowPlacement
        {
            HasValue = true,
            ScreenDeviceName = "DISPLAY1",
            WidthDip = 4000,
            HeightDip = 3000,
            LeftRatio = 5,
            TopRatio = -3
        };
        var plan = RemoteWindowLayoutService.CreateRestorePlan(
            placement,
            new[] { screen },
            null,
            new Size(20, 40));

        Assert.True(screen.WorkingArea.Contains(plan.WindowBoundsPhysical));
        Assert.True(plan.ClientSizePhysical.Width <= screen.WorkingArea.Width - 32);
        Assert.True(plan.ClientSizePhysical.Height <= screen.WorkingArea.Height - 32);
    }

    [Fact]
    public void MaximizedStateIsCapturedAndRestored()
    {
        var screen = Screen("DISPLAY1", new Rectangle(0, 0, 2560, 1400), 120, true);
        var placement = RemoteWindowLayoutService.CapturePlacement(
            new Rectangle(100, 80, 1500, 1000),
            new Size(1475, 950),
            screen,
            true);
        var plan = RemoteWindowLayoutService.CreateRestorePlan(
            placement,
            new[] { screen },
            null,
            new Size(25, 50));

        Assert.True(placement.Maximized);
        Assert.True(plan.Maximized);
    }

    [Fact]
    public void MinimizedStateIsNeverSaved()
    {
        Assert.False(RemoteWindowLayoutService.CanSave(FormWindowState.Minimized));
        Assert.True(RemoteWindowLayoutService.CanSave(FormWindowState.Normal));
        Assert.True(RemoteWindowLayoutService.CanSave(FormWindowState.Maximized));
    }

    [Fact]
    public void OldJsonGetsDefaultRemoteWindowPlacement()
    {
        var config = JsonSerializer.Deserialize<AppConfig>("""
            { "Version": "0.16.1", "RemoteControl": { "Enabled": true, "Token": "kept" } }
            """, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(config);
        ConfigService.Normalize(config!);
        Assert.NotNull(config!.RemoteControl.Window);
        Assert.False(config.RemoteControl.Window.HasValue);
        Assert.Equal(1180, config.RemoteControl.Window.WidthDip);
        Assert.Equal(760, config.RemoteControl.Window.HeightDip);
        Assert.Equal("kept", config.RemoteControl.Token);
    }

    [Fact]
    public void PlacementCanBeSavedAndLoadedWithoutRecursiveCallbacks()
    {
        var directory = Path.Combine(Path.GetTempPath(), "FlyPPTTimer.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "config.json");
        try
        {
            var config = new AppConfig();
            config.RemoteControl.Window = new RemoteWindowPlacement
            {
                HasValue = true,
                ScreenDeviceName = "DISPLAY2",
                LeftRatio = 0.25,
                TopRatio = 0.75,
                WidthDip = 1100,
                HeightDip = 700,
                Maximized = true
            };
            var service = new ConfigService(new LogService(), path);
            service.Save(config);
            var loaded = service.Load();

            Assert.True(loaded.RemoteControl.Window.HasValue);
            Assert.Equal("DISPLAY2", loaded.RemoteControl.Window.ScreenDeviceName);
            Assert.Equal(0.25, loaded.RemoteControl.Window.LeftRatio);
            Assert.Equal(0.75, loaded.RemoteControl.Window.TopRatio);
            Assert.True(loaded.RemoteControl.Window.Maximized);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static RemoteScreenMetrics Screen(string name, Rectangle area, int dpi, bool primary) =>
        new(name, area, dpi, primary);
}
