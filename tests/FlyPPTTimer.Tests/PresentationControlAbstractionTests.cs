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
}
