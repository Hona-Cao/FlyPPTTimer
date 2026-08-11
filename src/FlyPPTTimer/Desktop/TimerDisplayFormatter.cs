using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Desktop;

internal readonly record struct TimerDisplayContent(string Text, bool IsTimeout);

internal static class TimerDisplayFormatter
{
    public static TimerDisplayContent Format(TimerSnapshot snapshot, AppConfig config)
    {
        var showHours = AlertService.ShouldShowHours(snapshot);
        if (snapshot.Mode == TimerMode.Countdown && (snapshot.State == TimerState.Finished || snapshot.IsOvertime))
        {
            return new TimerDisplayContent(
                snapshot.IsOvertime
                    ? config.Appearance.OvertimePrefix + AlertService.Format(snapshot.Elapsed - snapshot.Duration, showHours)
                    : AlertService.Format(TimeSpan.Zero, showHours),
                true);
        }

        return new TimerDisplayContent(
            AlertService.Format(snapshot.Display, showHours),
            snapshot.State == TimerState.Finished || snapshot.IsOvertime);
    }
}
