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
    public void RemoteControlServiceUsesPresentationBoundary()
    {
        var constructor = Assert.Single(typeof(RemoteControlService).GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters(), item => item.Name == "powerPoint");

        Assert.Equal(typeof(IPresentationControlService), parameter.ParameterType);
        Assert.Equal(
            NullabilityState.Nullable,
            new NullabilityInfoContext().Create(parameter).ReadState);
        Assert.Equal(
            typeof(IPresentationControlService),
            typeof(RemoteControlService).GetProperty(nameof(RemoteControlService.PresentationController))!.PropertyType);
    }

    [Fact]
    public void ApplicationContextWrapsLegacyServiceInPresentationAdapter()
    {
        var source = File.ReadAllText(SourcePath("src", "FlyPPTTimer", "FlyPPTTimerContext.cs"));

        Assert.Contains("new PowerPointPresentationAdapter(", source);
        Assert.Contains("new PowerPointControlService(() => _config, _log)", source);
    }

    [Fact]
    public void RemoteControlFormUsesPresentationBoundaryField()
    {
        var source = File.ReadAllText(SourcePath("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs"));

        Assert.Contains("IPresentationControlService? _powerPoint", source);
        Assert.DoesNotContain("PowerPointControlService? _powerPoint", source);
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
