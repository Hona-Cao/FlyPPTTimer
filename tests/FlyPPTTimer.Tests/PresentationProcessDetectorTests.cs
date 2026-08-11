using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class PresentationProcessDetectorTests
{
    [Fact]
    public void DetectsWpsAliasesCaseInsensitivelyAndRemovesDuplicates()
    {
        var detector = new PresentationProcessDetector(new FakeSource(
            "wpp", "WPP", "wps", "WPSOffice", "explorer"));

        var snapshot = detector.Detect();

        Assert.False(snapshot.PowerPointDetected);
        Assert.True(snapshot.WpsDetected);
        Assert.Equal(3, snapshot.MatchingProcessNames.Count);
        Assert.Contains(snapshot.MatchingProcessNames, name => name.Equals("wpp", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(snapshot.MatchingProcessNames, name => name.Equals("wps", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(snapshot.MatchingProcessNames, name => name.Equals("WPSOffice", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DetectsPowerPointWithoutDeclaringWps()
    {
        var detector = new PresentationProcessDetector(new FakeSource("POWERPNT", "explorer"));

        var snapshot = detector.Detect();

        Assert.True(snapshot.PowerPointDetected);
        Assert.False(snapshot.WpsDetected);
        Assert.Single(snapshot.MatchingProcessNames);
        Assert.Equal("POWERPNT", snapshot.MatchingProcessNames[0], ignoreCase: true);
    }

    [Fact]
    public void IgnoresUnrelatedProcesses()
    {
        var detector = new PresentationProcessDetector(new FakeSource("explorer", "notepad", "chrome"));

        var snapshot = detector.Detect();

        Assert.False(snapshot.PowerPointDetected);
        Assert.False(snapshot.WpsDetected);
        Assert.Empty(snapshot.MatchingProcessNames);
    }

    [Fact]
    public void WpsCapabilitiesAllowOnlyConfirmedForceExit()
    {
        var capabilities = PresentationProcessDetector.CreateWpsCapabilities(true);

        Assert.False(capabilities.CanEndSlideShow);
        Assert.False(capabilities.CanClosePresentation);
        Assert.False(capabilities.CanExitApplication);
        Assert.True(capabilities.CanForceExit);
        Assert.Equal(PresentationProcessDetector.WpsCapabilityMessage, capabilities.Message);
    }

    [Fact]
    public void MissingWpsUsesDefaultCapabilities()
    {
        var capabilities = PresentationProcessDetector.CreateWpsCapabilities(false);

        Assert.False(capabilities.CanEndSlideShow);
        Assert.False(capabilities.CanClosePresentation);
        Assert.False(capabilities.CanExitApplication);
        Assert.False(capabilities.CanForceExit);
        Assert.Equal("WPS 演示未检测到。", capabilities.Message);
    }

    [Theory]
    [InlineData("POWERPNT", true, false)]
    [InlineData("powerpnt", true, false)]
    [InlineData("WPSOffice", false, true)]
    [InlineData("wpp", false, true)]
    [InlineData("WPS", false, true)]
    [InlineData("powerpnt.exe", false, false)]
    [InlineData("", false, false)]
    [InlineData(null, false, false)]
    public void ProcessNamePolicyIsExplicit(string? processName, bool powerPoint, bool wps)
    {
        Assert.Equal(powerPoint, PresentationProcessDetector.IsPowerPointProcessName(processName));
        Assert.Equal(wps, PresentationProcessDetector.IsWpsProcessName(processName));
        Assert.Equal(powerPoint || wps, PresentationProcessDetector.IsPresentationProcessName(processName));
    }

    private sealed class FakeSource(params string[] names) : IPresentationProcessSource
    {
        public IReadOnlyList<string> GetProcessNames() => names;
    }
}
