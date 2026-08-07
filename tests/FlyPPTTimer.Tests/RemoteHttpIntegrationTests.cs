using System.Net;
using System.Text;
using System.Text.Json;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class RemoteHttpIntegrationTests
{
    [Fact]
    public async Task RealListenerAuthenticatesRoutesCommandsAndInvalidatesOldToken()
    {
        var log = TestLog.Create();
        var config = new AppConfig();
        config.RemoteControl.Enabled = true;
        config.RemoteControl.UseRandomPort = true;
        config.RemoteControl.Token = "integration-token";

        var timer = new TimerService(log);
        timer.Configure(config);
        var commands = new AppCommandService(
            timer, new AlertService(log), () => config, saved => config = saved,
            () => { }, () => { }, () => { }, () => false, _ => { }, () => { }, log);
        var presentationAdapter = new RecordingPresentationController();
        var presentationCommands = new PresentationCommandService(presentationAdapter);
        using var service = new RemoteControlService(
            () => config, saved => config = saved, commands, presentationCommands, log);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        service.Start();
        Assert.True(service.IsRunning);
        Assert.InRange(service.CurrentPort, 1, 65535);
        var origin = $"http://127.0.0.1:{service.CurrentPort}";

        using (var rejected = await client.GetAsync($"{origin}/state?token=wrong"))
            Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);

        using (var state = await client.GetAsync($"{origin}/state?token=integration-token"))
        {
            Assert.Equal(HttpStatusCode.OK, state.StatusCode);
            var json = JsonDocument.Parse(await state.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("ok").GetBoolean());
            Assert.Equal(AppVersion.Current, json.RootElement.GetProperty("version").GetString());
        }

        using (var timerResponse = await PostJson(
                   client, $"{origin}/command?token=integration-token",
                   new RemoteCommand { Command = "timer.start" }))
        {
            Assert.Equal(HttpStatusCode.OK, timerResponse.StatusCode);
            Assert.Equal(TimerState.Running, timer.State);
        }

        using (var presentationResponse = await PostJson(
                   client, $"{origin}/command?token=integration-token",
                   new RemoteCommand { Command = "ppt.next", OperationId = "http-fixture" }))
        {
            Assert.Equal(HttpStatusCode.OK, presentationResponse.StatusCode);
            var queued = Assert.Single(presentationAdapter.Queued);
            Assert.Equal("ppt.next", queued.Command);
            Assert.Equal("http-fixture", queued.OperationId);
        }

        service.DisconnectAll();
        Assert.NotEqual("integration-token", config.RemoteControl.Token);
        using (var oldLink = await client.GetAsync($"{origin}/state?token=integration-token"))
            Assert.Equal(HttpStatusCode.Forbidden, oldLink.StatusCode);
        using (var newLink = await client.GetAsync($"{origin}/state?token={Uri.EscapeDataString(config.RemoteControl.Token)}"))
            Assert.Equal(HttpStatusCode.OK, newLink.StatusCode);
    }

    private static Task<HttpResponseMessage> PostJson(HttpClient client, string url, RemoteCommand command) =>
        client.PostAsync(url, new StringContent(
            JsonSerializer.Serialize(command), Encoding.UTF8, "application/json"));

    private sealed class RecordingPresentationController : IPresentationControlService
    {
        public List<RemoteCommand> Queued { get; } = [];
        public event EventHandler<string>? SlideShowStarted { add { } remove { } }
        public event EventHandler? SlideShowEnded { add { } remove { } }
        public event EventHandler? SlideShowWindowActivated { add { } remove { } }
        public event EventHandler? StateChanged { add { } remove { } }

        public PresentationState GetState() => new() { PowerPointInstalled = true };

        public PresentationCommandResult Queue(RemoteCommand command)
        {
            Queued.Add(command);
            return new PresentationCommandResult(true, "queued", GetState());
        }

        public PresentationCommandResult Execute(RemoteCommand command) =>
            new(true, "executed", GetState());

        public void Dispose() { }
    }
}
