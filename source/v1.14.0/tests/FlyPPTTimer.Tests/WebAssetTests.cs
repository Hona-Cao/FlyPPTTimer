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

    [Fact]
    public void V017_MobileTimerCanSetDurationAndModeAndShowOvertime()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "FlyPPTTimer", "Web"));
        var script = File.ReadAllText(Path.Combine(root, "app.js"));
        var markup = File.ReadAllText(Path.Combine(root, "index.html"));
        var styles = File.ReadAllText(Path.Combine(root, "app.css"));

        Assert.Contains("durationHours", markup);
        Assert.Contains("durationMinutes", markup);
        Assert.Contains("durationSeconds", markup);
        Assert.Contains("data-timer-mode=\"countdown\"", markup);
        Assert.Contains("data-timer-mode=\"countup\"", markup);
        Assert.Contains("timer.setDuration", script);
        Assert.Contains("timer.setMode", script);
        Assert.Contains("t.isOvertime?'已超时'", script);
        Assert.Contains(".timer-card.overtime", styles);
    }

    [Fact]
    public void V0186_MobilePersistsSelectedRuleAndCanDismissTimeUpBlackout()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "FlyPPTTimer", "Web"));
        var script = File.ReadAllText(Path.Combine(root, "app.js"));
        var markup = File.ReadAllText(Path.Combine(root, "index.html"));

        Assert.Contains("presentationId:selectedPresentationId", script);
        Assert.Equal(2, markup.Split("data-command=\"timeup.dismiss\"").Length - 1);
        Assert.Contains("timeUpBlackoutActive", script);
    }
}
