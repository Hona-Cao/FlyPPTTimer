using System.Reflection;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class PresentationControlAbstractionTests
{
    [Fact]
    public void PowerPointAdapterImplementsStablePresentationBoundary()
    {
        Assert.True(typeof(IPresentationControlService)
            .IsAssignableFrom(typeof(PowerPointPresentationAdapter)));

        var constructor = Assert.Single(typeof(PowerPointPresentationAdapter).GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());
        Assert.Equal(typeof(PowerPointControlService), parameter.ParameterType);
    }

    [Fact]
    public void PresentationBoundaryExposesOnlyApplicationFacingOperations()
    {
        var contract = typeof(IPresentationControlService);
        var methods = contract.GetMethods().Where(method => !method.IsSpecialName).ToList();

        Assert.Equal(3, methods.Count);
        Assert.Contains(methods, method =>
            method.Name == nameof(IPresentationControlService.GetState)
            && method.ReturnType == typeof(PresentationState)
            && method.GetParameters().Length == 0);
        Assert.Contains(methods, method =>
            method.Name == nameof(IPresentationControlService.Queue)
            && method.ReturnType == typeof(PresentationCommandResult)
            && method.GetParameters().Single().ParameterType == typeof(RemoteCommand));
        Assert.Contains(methods, method =>
            method.Name == nameof(IPresentationControlService.Execute)
            && method.ReturnType == typeof(PresentationCommandResult)
            && method.GetParameters().Single().ParameterType == typeof(RemoteCommand));
    }

    [Theory]
    [InlineData(nameof(IPresentationControlService.SlideShowStarted))]
    [InlineData(nameof(IPresentationControlService.SlideShowEnded))]
    [InlineData(nameof(IPresentationControlService.SlideShowWindowActivated))]
    [InlineData(nameof(IPresentationControlService.StateChanged))]
    public void PresentationBoundaryPublishesRequiredLifecycleEvents(string eventName)
    {
        Assert.NotNull(typeof(IPresentationControlService).GetEvent(eventName));
    }

    [Fact]
    public void RemoteControlServiceUsesApplicationPresentationCommands()
    {
        var constructor = Assert.Single(typeof(RemoteControlService).GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters(), item => item.Name == "presentationCommands");

        Assert.Equal(typeof(PresentationCommandService), parameter.ParameterType);
        Assert.Equal(
            NullabilityState.Nullable,
            new NullabilityInfoContext().Create(parameter).ReadState);
        Assert.Equal(
            typeof(PresentationCommandService),
            typeof(RemoteControlService).GetProperty(nameof(RemoteControlService.PresentationCommands))!.PropertyType);
    }

    [Fact]
    public void ApplicationContextWrapsLegacyServiceInPresentationAdapter()
    {
        var source = File.ReadAllText(SourcePath("src", "FlyPPTTimer", "FlyPPTTimerContext.cs"));

        Assert.Contains("new PowerPointPresentationAdapter(", source);
        Assert.Contains("new PowerPointControlService(() => _config, _log)", source);
    }

    [Fact]
    public void RemoteControlFormUsesApplicationCommandBoundaryField()
    {
        var source = File.ReadAllText(SourcePath("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs"));

        Assert.Contains("PresentationCommandService? _presentationCommands", source);
        Assert.DoesNotContain("IPresentationControlService? _powerPoint", source);
    }

    [Fact]
    public void PowerPointControlServiceUsesPresentationStaDispatcher()
    {
        var source = File.ReadAllText(SourcePath("src", "FlyPPTTimer", "Services", "PowerPointControlService.cs"));

        Assert.Contains("PresentationStaDispatcher _dispatcher", source);
        Assert.Contains("_dispatcher.TryEnqueue", source);
        Assert.Contains("_dispatcher.Invoke", source);
        Assert.Contains("_dispatcher.ExecuteWithBusyRetry", source);
        Assert.DoesNotContain("BlockingCollection<Action> _queue", source);
        Assert.DoesNotContain("new Thread(Run)", source);
        Assert.DoesNotContain("private T Invoke<T>", source);
        Assert.DoesNotContain("private T RetryComBusy<T>", source);
    }

    [Fact]
    public void PowerPointControlServiceUsesPresentationStateMonitor()
    {
        var source = File.ReadAllText(SourcePath("src", "FlyPPTTimer", "Services", "PowerPointControlService.cs"));

        Assert.Contains("PresentationStateMonitor _stateMonitor", source);
        Assert.Contains("new PresentationStateMonitor(", source);
        Assert.Contains("GetState() => _stateMonitor.GetState()", source);
        Assert.Contains("UpdateCachedState() => _stateMonitor.RefreshNow()", source);
        Assert.Contains("_stateMonitor.MutateCurrent(ApplyOperation)", source);
        Assert.Contains("notify: false", source);
        Assert.True(source.IndexOf("_stateMonitor.Dispose()", StringComparison.Ordinal) <
                    source.IndexOf("_dispatcher.Dispose()", StringComparison.Ordinal));
        Assert.DoesNotContain("_refreshTimer", source);
        Assert.DoesNotContain("_stateSync", source);
        Assert.DoesNotContain("_cachedState", source);
        Assert.DoesNotContain("_lastShowRunning", source);
        Assert.DoesNotContain("_lastShowPath", source);
        Assert.DoesNotContain("_refreshQueued", source);
        Assert.DoesNotContain("_lastRefreshFailureLog", source);
        Assert.DoesNotContain("CloneState(PresentationState", source);
    }

    [Fact]
    public void PowerPointControlServiceDelegatesNativeWindowActivation()
    {
        var source = File.ReadAllText(SourcePath("src", "FlyPPTTimer", "Services", "PowerPointControlService.cs"));

        Assert.Contains("PresentationWindowActivator _windowActivator", source);
        Assert.Contains("_windowActivator = new PresentationWindowActivator(warn: _log.Warn)", source);
        Assert.Contains("_windowActivator.Activate(hwnd, path, label, failurePrefix)", source);
        Assert.Contains("return new WindowActivationResult(", source);
        Assert.DoesNotContain("PowerPoint window activation incomplete", source);
    }

    [Fact]
    public void PowerPointControlServiceDelegatesWpsProcessDetection()
    {
        var source = File.ReadAllText(SourcePath("src", "FlyPPTTimer", "Services", "PowerPointControlService.cs"));

        Assert.Contains("PresentationProcessDetector _processDetector", source);
        Assert.Equal(
            1,
            source.Split("_processDetector = new PresentationProcessDetector();", StringSplitOptions.None).Length - 1);
        Assert.Contains("private void PopulateWpsCapabilities(PresentationState state)", source);
        Assert.Contains("var snapshot = _processDetector.Detect();", source);
        Assert.Contains("state.WpsDetected = snapshot.WpsDetected;", source);
        Assert.Contains("PresentationProcessDetector.CreateWpsCapabilities(snapshot.WpsDetected)", source);
    }

    [Fact]
    public void PowerPointControlServiceDelegatesPresentationProcessTermination()
    {
        var source = File.ReadAllText(SourcePath("src", "FlyPPTTimer", "Services", "PowerPointControlService.cs"));

        Assert.Contains("PresentationProcessTerminator _processTerminator", source);
        Assert.Equal(
            1,
            source.Split("_processTerminator = new PresentationProcessTerminator(warn: _log.Warn);", StringSplitOptions.None).Length - 1);
        Assert.Contains("private string ForceQuitAll()", source);
        Assert.Contains("var result = _processTerminator.TerminateAll();", source);
        Assert.Contains("if (!result.AnyDetected) return result.Message;", source);
        Assert.Contains("_managedPresentations.Clear();", source);
        Assert.Contains("return result.Message;", source);
        Assert.DoesNotContain("process.Kill(true)", source);
    }

    [Fact]
    public void ManagedCloseSuppressesOnlyItsReadOnlyPrompt()
    {
        var calls = new List<string>();

        PowerPointControlService.ClosePresentationPreservingUserChanges(
            true,
            () => calls.Add("suppress"),
            () => calls.Add("close"));

        Assert.Equal(["suppress", "close"], calls);
    }

    [Fact]
    public void ExternalClosePreservesNativeUnsavedChangesPrompt()
    {
        var calls = new List<string>();

        PowerPointControlService.ClosePresentationPreservingUserChanges(
            false,
            () => calls.Add("suppress"),
            () => calls.Add("close"));

        Assert.Equal(["close"], calls);
    }

    [Fact]
    public void ComCleanupDoesNotFinalReleaseSharedPowerPointRcws()
    {
        var source = File.ReadAllText(SourcePath("src", "FlyPPTTimer", "Services", "PowerPointControlService.cs"));

        Assert.Contains("Marshal.ReleaseComObject(value)", source);
        Assert.DoesNotContain("Marshal.FinalReleaseComObject(value)", source);
    }

    [Fact]
    public void NativeFallbackPrefersForegroundSlideShowOverWpsDocumentFrame()
    {
        var selected = PowerPointControlService.SelectPreferredSlideShowWindow(
        [
            new PresentationNativeWindowCandidate(new IntPtr(1), false, false, true, 2_000_000),
            new PresentationNativeWindowCandidate(new IntPtr(2), true, true, false, 1_000_000),
            new PresentationNativeWindowCandidate(new IntPtr(3), false, false, false, 3_000_000)
        ]);

        Assert.Equal(new IntPtr(2), selected);
    }

    private static string SourcePath(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var path = Path.Combine([directory.FullName, .. segments]);
            if (File.Exists(path)) return path;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository source files.");
    }
}
