using System.Media;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

public sealed class AlertService(LogService log)
{
    private readonly HashSet<string> _triggered = [];

    public event EventHandler<PromptSettings>? PromptVisualRequested;
    public bool Muted { get; private set; }

    public void ResetTriggers() => _triggered.Clear();
    public void ToggleMute()
    {
        Muted = !Muted;
        log.Info($"Mute toggled: {Muted}");
    }

    public void CheckPrompts(AppConfig config, TimerSnapshot snapshot)
    {
        if (snapshot.Mode != TimerMode.Countdown || snapshot.State != TimerState.Running) return;
        TryTrigger("prompt1", config.Behavior.Prompt1, snapshot);
        TryTrigger("prompt2", config.Behavior.Prompt2, snapshot);
    }

    public void TriggerEnd(AppConfig config, TimerSnapshot snapshot)
    {
        Trigger("end", config.Behavior.EndPrompt, snapshot);
    }

    private void TryTrigger(string key, PromptSettings prompt, TimerSnapshot snapshot)
    {
        if (!prompt.Enabled || _triggered.Contains(key)) return;
        if (snapshot.Remaining.TotalSeconds <= prompt.TriggerBeforeEndSeconds)
        {
            Trigger(key, prompt, snapshot);
        }
    }

    private void Trigger(string key, PromptSettings prompt, TimerSnapshot snapshot)
    {
        _triggered.Add(key);
        var text = ApplyTemplate(prompt.Text, snapshot);
        log.Info($"Prompt triggered: {key} {text}");
        if (Muted)
        {
            PromptVisualRequested?.Invoke(this, prompt);
            return;
        }

        if (prompt.Beep)
        {
            try { SystemSounds.Beep.Play(); } catch (Exception ex) { log.Error("System beep failed.", ex); }
        }

        if (prompt.Speak && !string.IsNullOrWhiteSpace(text))
        {
            try
            {
                var type = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (type is not null)
                {
                    dynamic voice = Activator.CreateInstance(type)!;
                    voice.Speak(text, 1);
                }
            }
            catch (Exception ex)
            {
                log.Error("SAPI speak failed.", ex);
            }
        }

        if (prompt.PlaySound)
        {
            if (File.Exists(prompt.SoundFile))
            {
                try { new SoundPlayer(prompt.SoundFile).Play(); }
                catch (Exception ex) { log.Error("WAV playback failed.", ex); }
            }
            else
            {
                log.Warn($"Sound file not found: {prompt.SoundFile}");
            }
        }

        PromptVisualRequested?.Invoke(this, prompt);
    }

    private static string ApplyTemplate(string template, TimerSnapshot snapshot)
    {
        return template
            .Replace("{time}", Format(snapshot.Display, ShouldShowHours(snapshot)))
            .Replace("{remaining}", Format(snapshot.Remaining, ShouldShowHours(snapshot)))
            .Replace("{elapsed}", Format(snapshot.Elapsed, ShouldShowHours(snapshot)))
            .Replace("{title}", "")
            .Replace("{current}", "")
            .Replace("{total}", "");
    }

    public static string Format(TimeSpan time, bool forceHours = false)
    {
        if (time < TimeSpan.Zero) time = time.Duration();
        var totalHours = (int)Math.Floor(time.TotalHours);
        if (forceHours || totalHours > 0)
        {
            return $"{totalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }

        var totalMinutes = (int)Math.Floor(time.TotalMinutes);
        return $"{totalMinutes:00}:{time.Seconds:00}";
    }

    public static bool ShouldShowHours(TimerSnapshot snapshot) =>
        snapshot.Duration.TotalHours >= 1
        || snapshot.Elapsed.TotalHours >= 1
        || snapshot.Display.TotalHours >= 1;
}
