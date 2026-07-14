using FlyPPTTimer.Core.Models;

namespace FlyPPTTimer.Core.Abstractions;

public interface IConfigService { AppConfig Load(); void Save(AppConfig config); AppConfig Import(string path); void Export(AppConfig config, string path); }
public interface ILogService { void Info(string message); void Warn(string message); void Error(string message, Exception? exception = null); IReadOnlyList<string> ReadRecent(int count = 200); }
public interface IPowerPointControlService : IDisposable
{
    event EventHandler<string>? SlideShowStarted;
    event EventHandler? SlideShowEnded;
    PresentationState GetState();
    Task<PresentationCommandResult> ExecuteAsync(PresentationCommand command, CancellationToken cancellationToken = default);
    Task<string?> GenerateFirstSlideThumbnailAsync(string presentationId, string cacheDirectory, CancellationToken cancellationToken = default);
    Task RestartAsync(CancellationToken cancellationToken = default);
}
public interface IRemoteControlService : IDisposable { bool IsRunning { get; } int CurrentPort { get; } int ConnectedClients { get; } void Start(); void Stop(); void Restart(); void DisconnectAll(); }
