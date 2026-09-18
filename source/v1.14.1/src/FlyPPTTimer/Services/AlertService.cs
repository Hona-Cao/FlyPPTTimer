using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

public sealed class AlertService : IDisposable
{
    private readonly LogService _log;
    private readonly HashSet<string> _triggered = [];
    private readonly IAlertPlaybackEngine _playback;

    public AlertService(LogService log, IAlertPlaybackEngine? playback = null)
    {
        _log = log;
        _playback = playback ?? new WindowsAlertPlaybackEngine(log);
    }

    public event EventHandler<PromptSettings>? PromptVisualRequested;

    public void ResetTriggers() => _triggered.Clear();

    public void CheckPrompts(AppConfig config, TimerSnapshot snapshot)
    {
        if (snapshot.State != TimerState.Running || snapshot.Elapsed >= snapshot.Duration) return;
        TryTrigger("prompt1", config.Behavior.Prompt1, snapshot);
        TryTrigger("prompt2", config.Behavior.Prompt2, snapshot);
    }

    public void TriggerEnd(AppConfig config, TimerSnapshot snapshot)
    {
        if (!config.Behavior.EndPrompt.Enabled) return;
        Trigger("end", config.Behavior.EndPrompt, snapshot);
    }

    private void TryTrigger(string key, PromptSettings prompt, TimerSnapshot snapshot)
    {
        if (!prompt.Enabled || _triggered.Contains(key)) return;
        if ((snapshot.Duration - snapshot.Elapsed).TotalSeconds <= prompt.TriggerBeforeEndSeconds)
            Trigger(key, prompt, snapshot);
    }

    private void Trigger(string key, PromptSettings prompt, TimerSnapshot snapshot)
    {
        _triggered.Add(key);
        var text = ApplyTemplate(prompt.Text, snapshot);
        _log.Info($"Prompt queued: {key} {text}");
        if (prompt.Speak || prompt.PlaySound)
        {
            var selectedSound = prompt.PlaySound && !string.IsNullOrWhiteSpace(prompt.SoundFile)
                ? prompt.SoundFile
                : "";
            _playback.Enqueue(new AlertPlaybackRequest(
                prompt.Speak && string.IsNullOrWhiteSpace(selectedSound) ? text : "",
                selectedSound));
        }
        PromptVisualRequested?.Invoke(this, prompt);
    }

    private static string ApplyTemplate(string template, TimerSnapshot snapshot) => template
        .Replace("{time}", Format(snapshot.Display, ShouldShowHours(snapshot)))
        .Replace("{remaining}", Format(snapshot.Remaining, ShouldShowHours(snapshot)))
        .Replace("{elapsed}", Format(snapshot.Elapsed, ShouldShowHours(snapshot)))
        .Replace("{title}", "")
        .Replace("{current}", "")
        .Replace("{total}", "");

    public static string Format(TimeSpan time, bool forceHours = false)
    {
        if (time < TimeSpan.Zero) time = time.Duration();
        var totalHours = (int)Math.Floor(time.TotalHours);
        if (forceHours || totalHours > 0)
            return $"{totalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        var totalMinutes = (int)Math.Floor(time.TotalMinutes);
        return $"{totalMinutes:00}:{time.Seconds:00}";
    }

    public static bool ShouldShowHours(TimerSnapshot snapshot) =>
        snapshot.Duration.TotalHours >= 1
        || snapshot.Elapsed.TotalHours >= 1
        || snapshot.Display.TotalHours >= 1;

    public void Dispose() => _playback.Dispose();
}

public sealed record AlertPlaybackRequest(string SpeechText, string SoundFile);

public interface IAlertPlaybackEngine : IDisposable
{
    void Enqueue(AlertPlaybackRequest request);
}

internal sealed class WindowsAlertPlaybackEngine : IAlertPlaybackEngine
{
    private readonly BlockingCollection<AlertPlaybackRequest> _queue = new(32);
    private readonly Thread _thread;
    private readonly LogService _log;
    private bool _disposed;

    public WindowsAlertPlaybackEngine(LogService log)
    {
        _log = log;
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "FlyPPTTimer alert playback"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public void Enqueue(AlertPlaybackRequest request)
    {
        if (_disposed || !_queue.TryAdd(request))
            _log.Warn("Alert playback queue is full; prompt was not queued.");
    }

    private void Run()
    {
        object? voiceObject = null;
        object? playerObject = null;
        try
        {
            var voiceType = Type.GetTypeFromProgID("SAPI.SpVoice");
            if (voiceType is not null) voiceObject = Activator.CreateInstance(voiceType);
            var playerType = Type.GetTypeFromProgID("WMPlayer.OCX");
            if (playerType is not null) playerObject = Activator.CreateInstance(playerType);

            foreach (var request in _queue.GetConsumingEnumerable())
            {
                if (!string.IsNullOrWhiteSpace(request.SpeechText))
                    SpeakFully(voiceObject, request.SpeechText);
                if (!string.IsNullOrWhiteSpace(request.SoundFile))
                    PlayFileFully(playerObject, request.SoundFile);
            }
        }
        catch (Exception ex)
        {
            _log.Error("Alert playback worker failed.", ex);
        }
        finally
        {
            if (playerObject is not null)
            {
                try { ((dynamic)playerObject).controls.stop(); } catch { }
            }
        }
    }

    private void SpeakFully(object? voiceObject, string text)
    {
        if (voiceObject is null)
        {
            _log.Warn("Windows speech service is unavailable.");
            return;
        }
        try
        {
            // SPF_DEFAULT (0) is synchronous: the worker does not dequeue another item
            // until the complete sentence has finished playing.
            ((dynamic)voiceObject).Speak(text, 0);
        }
        catch (Exception ex) { _log.Error("SAPI speech playback failed.", ex); }
    }

    private void PlayFileFully(object? playerObject, string path)
    {
        if (!File.Exists(path))
        {
            _log.Warn($"Prompt sound file not found: {path}");
            return;
        }
        if (TryPlayWithWindowsMci(path)) return;
        if (playerObject is null) { _log.Warn("Windows media playback services are unavailable."); return; }
        try
        {
            dynamic player = playerObject;
            player.settings.autoStart = false;
            player.settings.mute = false;
            player.settings.volume = 100;
            player.URL = path;
            var opening = Stopwatch.StartNew();
            while (opening.Elapsed < TimeSpan.FromSeconds(10))
            {
                var openState = (int)player.openState;
                if (openState == 13) break; // wmposMediaOpen
                Thread.Sleep(50);
            }
            player.controls.play();
            var watch = Stopwatch.StartNew();
            var started = false;
            while (watch.Elapsed < TimeSpan.FromMinutes(30))
            {
                var state = (int)player.playState;
                if (state == 3) started = true; // wmppsPlaying
                if (started && state is 1 or 8 or 10) break; // stopped, media ended, ready
                if (!started && watch.Elapsed > TimeSpan.FromSeconds(10)) break;
                Thread.Sleep(100);
            }
            if (started)
                _log.Info($"Prompt sound playback completed: {Path.GetFileName(path)}");
            else
                _log.Warn($"Prompt sound did not enter the playing state: {path}");
        }
        catch (Exception ex) { _log.Error("Prompt sound playback failed.", ex); }
    }

    private bool TryPlayWithWindowsMci(string path)
    {
        const string alias = "FlyPPTTimerAlert";
        var safePath = path.Replace("\"", "");
        MciSendString($"close {alias}", null, 0, IntPtr.Zero);
        var openResult = MciSendString($"open \"{safePath}\" alias {alias}", null, 0, IntPtr.Zero);
        if (openResult != 0)
        {
            _log.Warn($"Windows MCI could not open prompt sound ({MciError(openResult)}); trying media-player fallback.");
            return false;
        }
        try
        {
            // MCI's WAIT flag keeps this dedicated worker blocked until the complete
            // file has played, so UI clicks and later prompts cannot interrupt it.
            var playResult = MciSendString($"play {alias} wait", null, 0, IntPtr.Zero);
            if (playResult != 0)
            {
                _log.Warn($"Windows MCI could not play prompt sound ({MciError(playResult)}); trying media-player fallback.");
                return false;
            }
            _log.Info($"Prompt sound playback completed: {Path.GetFileName(path)}");
            return true;
        }
        finally
        {
            MciSendString($"close {alias}", null, 0, IntPtr.Zero);
        }
    }

    private static string MciError(int code)
    {
        var buffer = new StringBuilder(256);
        return MciGetErrorString(code, buffer, buffer.Capacity) ? buffer.ToString() : $"code {code}";
    }

    [DllImport("winmm.dll", EntryPoint = "mciSendStringW", CharSet = CharSet.Unicode)]
    private static extern int MciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr callback);

    [DllImport("winmm.dll", EntryPoint = "mciGetErrorStringW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MciGetErrorString(int errorCode, StringBuilder errorText, int errorTextSize);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _queue.CompleteAdding();
        if (_thread.Join(TimeSpan.FromSeconds(2))) _queue.Dispose();
    }
}
