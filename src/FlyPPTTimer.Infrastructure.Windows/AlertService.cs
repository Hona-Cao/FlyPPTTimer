using System.Runtime.InteropServices;
using FlyPPTTimer.Core.Abstractions;
using FlyPPTTimer.Core.Models;

namespace FlyPPTTimer.Infrastructure.Windows;

public sealed class AlertService(Func<AppConfig> getConfig, Func<bool> isMuted, ILogService log)
{
    private readonly HashSet<string> _triggered = [];
    private TimerState _previousState;
    public event EventHandler<PromptSettings>? VisualRequested;

    public void Handle(TimerSnapshot snapshot)
    {
        if (snapshot.State == TimerState.Running && _previousState != TimerState.Running) _triggered.Clear();
        _previousState = snapshot.State;
        if (snapshot.Mode != TimerMode.Countdown || snapshot.State != TimerState.Running) return;
        var config = getConfig();
        TryTrigger("prompt1", config.Behavior.Prompt1, snapshot);
        TryTrigger("prompt2", config.Behavior.Prompt2, snapshot);
    }

    public void HandleFinished(TimerSnapshot snapshot) => Trigger("end", getConfig().Behavior.EndPrompt, snapshot);

    private void TryTrigger(string key, PromptSettings prompt, TimerSnapshot snapshot)
    {
        if (prompt.Enabled && !_triggered.Contains(key) && snapshot.Remaining.TotalSeconds <= prompt.TriggerBeforeEndSeconds)
            Trigger(key, prompt, snapshot);
    }

    private void Trigger(string key, PromptSettings prompt, TimerSnapshot snapshot)
    {
        if (!_triggered.Add(key)) return;
        var text = prompt.Text.Replace("{remaining}", snapshot.DisplayText).Replace("{time}", snapshot.DisplayText);
        log.Info($"计时提示已触发：{key}");
        if (!isMuted())
        {
            if (prompt.Beep) MessageBeep(0x40);
            if (prompt.Speak && !string.IsNullOrWhiteSpace(text)) _ = Task.Run(() => Speak(text));
            if (!string.IsNullOrWhiteSpace(prompt.SoundFile) && File.Exists(prompt.SoundFile))
                _ = Task.Run(() => PlaySound(prompt.SoundFile, IntPtr.Zero, 0x00020001));
        }
        VisualRequested?.Invoke(this, prompt);
    }

    private void Speak(string text)
    {
        object? voice = null;
        try { var type = Type.GetTypeFromProgID("SAPI.SpVoice"); if (type is not null) { voice = Activator.CreateInstance(type); ((dynamic)voice!).Speak(text, 0); } }
        catch (Exception ex) { log.Error("语音提示播放失败。", ex); }
        finally { if (voice is not null && Marshal.IsComObject(voice)) try { Marshal.FinalReleaseComObject(voice); } catch { } }
    }

    [DllImport("user32.dll")] private static extern bool MessageBeep(uint type);
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)] private static extern bool PlaySound(string file, IntPtr module, uint flags);
}
