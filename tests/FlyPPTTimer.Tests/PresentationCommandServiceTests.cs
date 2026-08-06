using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class PresentationCommandServiceTests
{
    public static TheoryData<PresentationCommandKind, string> CommandMappings => new()
    {
        { PresentationCommandKind.Refresh, "ppt.refresh" },
        { PresentationCommandKind.StartFromBeginning, "ppt.startFromBeginning" },
        { PresentationCommandKind.StartFromCurrent, "ppt.startFromCurrent" },
        { PresentationCommandKind.Previous, "ppt.previous" },
        { PresentationCommandKind.Next, "ppt.next" },
        { PresentationCommandKind.GoToSlide, "ppt.gotoSlide" },
        { PresentationCommandKind.EndShow, "ppt.endShow" },
        { PresentationCommandKind.ToggleBlackScreen, "ppt.blackScreenToggle" },
        { PresentationCommandKind.ToggleWhiteScreen, "ppt.whiteScreenToggle" },
        { PresentationCommandKind.OpenPresentation, "ppt.openPresentation" },
        { PresentationCommandKind.CloseActivePresentation, "ppt.closeActivePresentation" },
        { PresentationCommandKind.CloseCurrentPresentation, "ppt.closeCurrentPresentation" },
        { PresentationCommandKind.ForceQuitAll, "ppt.forceQuitAll" }
    };

    [Theory]
    [MemberData(nameof(CommandMappings))]
    public void TypedCommandMapsToStableProtocol(PresentationCommandKind kind, string expected)
    {
        var controller = new RecordingPresentationController();
        var sut = new PresentationCommandService(controller);

        var result = sut.Execute(new PresentationCommand(kind, "deck-id", 17, true));

        Assert.True(result.Success);
        var sent = Assert.Single(controller.Executed);
        Assert.Equal(expected, sent.Command);
        Assert.Equal("deck-id", sent.PresentationId);
        Assert.Equal(17, sent.SlideNumber);
        Assert.True(sent.Confirmed);
    }

    [Theory]
    [MemberData(nameof(CommandMappings))]
    public void RemoteProtocolMapsToTypedCommandAndQueuesUnchanged(PresentationCommandKind expectedKind, string protocol)
    {
        var controller = new RecordingPresentationController();
        var sut = new PresentationCommandService(controller);
        var remote = new RemoteCommand
        {
            Command = protocol,
            PresentationId = "deck-id",
            SlideNumber = 8,
            Confirmed = true
        };

        Assert.True(PresentationCommandService.TryFromRemoteCommand(remote, out var typed));
        Assert.Equal(expectedKind, typed.Kind);
        Assert.Equal("deck-id", typed.PresentationId);
        Assert.Equal(8, typed.SlideNumber);
        Assert.True(typed.Confirmed);

        var result = sut.QueueRemote(remote);
        Assert.True(result.Success);
        var queued = Assert.Single(controller.Queued);
        Assert.Equal(protocol, queued.Command);
        Assert.Equal("deck-id", queued.PresentationId);
        Assert.Equal(8, queued.SlideNumber);
        Assert.True(queued.Confirmed);
    }

    [Fact]
    public void UnknownRemotePresentationCommandIsRejectedBeforeAdapter()
    {
        var controller = new RecordingPresentationController();
        var sut = new PresentationCommandService(controller);

        var result = sut.QueueRemote(new RemoteCommand { Command = "ppt.notAllowed" });

        Assert.False(result.Success);
        Assert.Equal("命令不在演示控制白名单中。", result.Message);
        Assert.Empty(controller.Queued);
        Assert.Empty(controller.Executed);
    }

    [Fact]
    public void LifecycleEventsAreForwardedFromReplaceableAdapter()
    {
        var controller = new RecordingPresentationController();
        var sut = new PresentationCommandService(controller);
        var observed = new List<string>();
        sut.SlideShowStarted += (_, path) => observed.Add("start:" + path);
        sut.SlideShowEnded += (_, _) => observed.Add("end");
        sut.SlideShowWindowActivated += (_, _) => observed.Add("window");
        sut.StateChanged += (_, _) => observed.Add("state");

        controller.RaiseLifecycleEvents(@"C:\slides\demo.pptx");

        Assert.Equal([@"start:C:\slides\demo.pptx", "end", "window", "state"], observed);
    }

    private sealed class RecordingPresentationController : IPresentationControlService
    {
        public List<RemoteCommand> Queued { get; } = [];
        public List<RemoteCommand> Executed { get; } = [];
        public event EventHandler<string>? SlideShowStarted;
        public event EventHandler? SlideShowEnded;
        public event EventHandler? SlideShowWindowActivated;
        public event EventHandler? StateChanged;

        public PresentationState GetState() => new() { PresentationName = "fixture" };

        public PresentationCommandResult Queue(RemoteCommand command)
        {
            Queued.Add(command);
            return new PresentationCommandResult(true, "queued", GetState());
        }

        public PresentationCommandResult Execute(RemoteCommand command)
        {
            Executed.Add(command);
            return new PresentationCommandResult(true, "executed", GetState());
        }

        public void RaiseLifecycleEvents(string path)
        {
            SlideShowStarted?.Invoke(this, path);
            SlideShowEnded?.Invoke(this, EventArgs.Empty);
            SlideShowWindowActivated?.Invoke(this, EventArgs.Empty);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose() { }
    }
}
