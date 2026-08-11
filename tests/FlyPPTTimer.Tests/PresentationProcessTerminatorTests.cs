using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class PresentationProcessTerminatorTests
{
    [Fact]
    public void NoMatchingProcessReturnsExactMessageAndDisposesAllHandles()
    {
        var source = new FakeSource("explorer", "notepad");
        var terminator = new PresentationProcessTerminator(source);

        var result = terminator.TerminateAll();

        Assert.False(result.AnyDetected);
        Assert.Equal(0, result.MatchingProcessCount);
        Assert.Equal(PresentationProcessTerminator.NoProcessMessage, result.Message);
        Assert.All(source.Handles, handle => Assert.True(handle.Disposed));
        Assert.All(source.Handles, handle => Assert.Empty(handle.KillRequests));
    }

    [Fact]
    public void MatchingProcessesAreKilledWithEntireTreeAndAllHandlesAreDisposed()
    {
        var source = new FakeSource("POWERPNT", "WPSOffice", "chrome");
        var terminator = new PresentationProcessTerminator(source);

        var result = terminator.TerminateAll();

        Assert.True(result.AnyDetected);
        Assert.Equal(2, result.MatchingProcessCount);
        Assert.Equal(2, result.SuccessfulRequestCount);
        Assert.Equal(0, result.FailedRequestCount);
        Assert.Equal(PresentationProcessTerminator.RequestedMessage, result.Message);
        Assert.Single(source.Handles[0].KillRequests);
        Assert.True(source.Handles[0].KillRequests[0]);
        Assert.Single(source.Handles[1].KillRequests);
        Assert.True(source.Handles[1].KillRequests[0]);
        Assert.Empty(source.Handles[2].KillRequests);
        Assert.All(source.Handles, handle => Assert.True(handle.Disposed));
    }

    [Fact]
    public void KillFailureWarnsAndContinues()
    {
        var warnings = new List<string>();
        var source = new FakeSource(
            new FakeHandle("wpp", new InvalidOperationException("blocked")),
            new FakeHandle("wps"));
        var terminator = new PresentationProcessTerminator(source, warnings.Add);

        var result = terminator.TerminateAll();

        Assert.Equal(2, result.MatchingProcessCount);
        Assert.Equal(1, result.SuccessfulRequestCount);
        Assert.Equal(1, result.FailedRequestCount);
        Assert.Equal(PresentationProcessTerminator.RequestedMessage, result.Message);
        Assert.Single(warnings);
        Assert.Equal("Failed to force quit wpp: blocked", warnings[0]);
        Assert.All(source.Handles, handle => Assert.True(handle.Disposed));
    }

    [Theory]
    [InlineData("powerpnt")]
    [InlineData("WPSOFFICE")]
    [InlineData("WPP")]
    [InlineData("Wps")]
    public void ProcessNamesAreMatchedCaseInsensitively(string processName)
    {
        var source = new FakeSource(processName);
        var terminator = new PresentationProcessTerminator(source);

        var result = terminator.TerminateAll();

        Assert.True(result.AnyDetected);
        Assert.Single(source.Handles[0].KillRequests);
    }

    private sealed class FakeSource : IPresentationProcessHandleSource
    {
        public FakeSource(params string[] names)
            : this(names.Select(name => new FakeHandle(name)).ToArray())
        {
        }

        public FakeSource(params FakeHandle[] handles)
        {
            Handles = handles;
        }

        public IReadOnlyList<FakeHandle> Handles { get; }

        public IReadOnlyList<IPresentationProcessHandle> GetProcesses() => Handles;
    }

    private sealed class FakeHandle(string processName, Exception? killException = null) : IPresentationProcessHandle
    {
        public string ProcessName { get; } = processName;
        public List<bool> KillRequests { get; } = [];
        public bool Disposed { get; private set; }

        public void Kill(bool entireProcessTree)
        {
            KillRequests.Add(entireProcessTree);
            if (killException is not null) throw killException;
        }

        public void Dispose() => Disposed = true;
    }
}
