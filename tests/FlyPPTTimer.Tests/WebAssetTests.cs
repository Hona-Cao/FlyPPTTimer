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

    [Fact]
    public void DangerousPresentationActions_UseInPageConfirmationWithoutClosingTheBrowser()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "FlyPPTTimer", "Web"));
        var script = File.ReadAllText(Path.Combine(root, "app.js"));
        var markup = File.ReadAllText(Path.Combine(root, "index.html"));
        Assert.Contains("confirmPanel", markup);
        Assert.Contains("requestConfirmation", script);
        Assert.DoesNotContain("confirm(", script);
        Assert.DoesNotContain("window.close", script);
        Assert.DoesNotContain("self.close", script);
        Assert.DoesNotContain("history.back", script);
    }
}
