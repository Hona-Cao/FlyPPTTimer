namespace FlyPPTTimer.Tests;

public sealed class WebAssetTests
{
    [Fact]
    public void PresentationListAvailability_IsRefreshedAfterBusyCompletes()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "FlyPPTTimer", "Web", "app.js"));
        var script = File.ReadAllText(path);
        Assert.Contains("refreshPresentationButtons();", script);
        Assert.Contains("finally{busy=false;setAvailability", script);
        Assert.DoesNotContain("if(host.dataset.signature===signature)return", script);
    }
}
