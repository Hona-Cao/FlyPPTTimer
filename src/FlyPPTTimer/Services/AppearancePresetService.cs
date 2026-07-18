using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

public sealed record AppearancePreset(string Name, string TextColor, string BackgroundColor, string TimeoutTextColor, string TimeoutBackgroundColor, string FlashBackgroundColor);

public static class AppearancePresetService
{
    public const string CustomName = "自定义";
    public static IReadOnlyList<AppearancePreset> Presets { get; } =
    [
        new("医疗卫生（蓝白）", "#0B3A66", "#F3F8FC", "#FFFFFF", "#B00020", "#4EA3D8"),
        new("教育培训（深蓝金）", "#FFFFFF", "#17365D", "#FFFFFF", "#9C1C1C", "#F2C14E"),
        new("商务会议（石墨蓝）", "#F5F7FA", "#263445", "#FFFFFF", "#B42318", "#5B8DEF"),
        new("科技发布（深色青蓝）", "#E6FAFF", "#102A43", "#FFFFFF", "#C62828", "#00B8D9"),
        new("高对比警示（黑红）", "#FFFFFF", "#111111", "#FFFFFF", "#D00000", "#FFD400")
    ];

    public static string[] Names => Presets.Select(x => x.Name).Append(CustomName).ToArray();

    public static bool Apply(string name, AppearanceSettings appearance)
    {
        var preset = Presets.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal));
        if (preset is null) return false;
        appearance.ColorScheme = preset.Name;
        appearance.TextColor = preset.TextColor;
        appearance.BackgroundColor = preset.BackgroundColor;
        appearance.TimeoutTextColor = preset.TimeoutTextColor;
        appearance.TimeoutBackgroundColor = preset.TimeoutBackgroundColor;
        appearance.FlashBackgroundColor = preset.FlashBackgroundColor;
        return true;
    }
}
