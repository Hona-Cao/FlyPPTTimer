using System.Globalization;
using System.Text.RegularExpressions;

namespace FlyPPTTimer.Services;

/// <summary>
/// Lightweight UI localization for the WinForms application. Chinese strings remain
/// the stable internal values used by existing settings and command logic; English is
/// applied only at the presentation layer.
/// </summary>
public static class Localization
{
    public const string Auto = "auto";
    public const string English = "en";
    public const string SimplifiedChinese = "zh-CN";

    private static string _configuredLanguage = Auto;
    private static readonly string SystemLanguage =
        CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? SimplifiedChinese
            : English;
    private static readonly HashSet<Control> Attached = [];
    private static bool _translating;

    public static string ConfiguredLanguage => _configuredLanguage;
    public static string EffectiveLanguage { get; private set; } = DetectSystemLanguage();
    public static bool IsEnglish => EffectiveLanguage == English;

    public static void Initialize(string? language)
    {
        _configuredLanguage = Normalize(language);
        EffectiveLanguage = _configuredLanguage == Auto ? DetectSystemLanguage() : _configuredLanguage;
        var culture = EffectiveLanguage == SimplifiedChinese
            ? CultureInfo.GetCultureInfo("zh-CN")
            : CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = culture;
    }

    public static string Normalize(string? language) => language?.Trim() switch
    {
        English => English,
        SimplifiedChinese => SimplifiedChinese,
        _ => Auto
    };

    public static string DetectSystemLanguage() => SystemLanguage;

    public static string T(string? text)
    {
        if (!IsEnglish || string.IsNullOrEmpty(text)) return text ?? "";
        if (EnglishText.TryGetValue(text, out var translated)) return translated;

        var result = text;
        foreach (var pair in EnglishFragments)
            result = result.Replace(pair.Key, pair.Value, StringComparison.Ordinal);

        result = Regex.Replace(result, @"^已选择 (\d+) 条规则$", "Selected $1 rules");
        result = Regex.Replace(result, @"^(\d+) 项$", "$1 items");
        result = Regex.Replace(result, @"^(\d+) 个项目$", "$1 items");
        result = Regex.Replace(result, @"^当前地址 (.+)$", "Current address: $1");
        result = Regex.Replace(result, @"^快捷键重复：(.+)$", "Duplicate hotkey: $1");
        result = Regex.Replace(result, @"^快捷键注册失败：(.+)$", "Failed to register hotkey: $1");
        result = Regex.Replace(result, @"^文件规则“(.+)”的计时时长无效。$", "Presentation rule “$1” has an invalid duration.");
        result = Regex.Replace(result, @"^当前时间文字需要更大的显示区域，窗口已自动调整为 (.+)。$", "The current timer text needs more space. The window was resized to $1.");
        result = Regex.Replace(result, @"^当前已是最新版本：(.+)$", "You already have the latest version: $1");
        result = Regex.Replace(result, @"^(.+)闪烁持续（秒）$", match => $"{T(match.Groups[1].Value)} flash duration (seconds)");
        result = Regex.Replace(result, @"^(.+)闪烁样式$", match => $"{T(match.Groups[1].Value)} flash style");
        result = Regex.Replace(result, @"^(.+)闪现时长（毫秒）$", match => $"{T(match.Groups[1].Value)} visible interval (ms)");
        result = Regex.Replace(result, @"^(.+)隐藏时长（毫秒）$", match => $"{T(match.Groups[1].Value)} hidden interval (ms)");
        result = Regex.Replace(result, @"^已从第 (\d+) 页开始放映$", "Slide show started from slide $1");
        result = Regex.Replace(result, @"^已跳转到第 (\d+) 页$", "Moved to slide $1");
        result = Regex.Replace(result, @"^已打开 (.+)$", "Opened $1");
        result = Regex.Replace(result, @"^已关闭最后打开的文稿：(.+)。$", "Closed the last-opened presentation: $1.");
        result = Regex.Replace(result, @"^已切换(.+)状态$", "Changed $1 state");
        result = Regex.Replace(result, @"^未找到目标(.+)$", "Target $1 not found");
        result = Regex.Replace(
            result,
            @"^检测或下载新版本失败：\r?\n(.+?)\r?\n\r?\n可稍后重试，或前往 Gitee Release 页面手动下载。$",
            "Update check or download failed:\r\n$1\r\n\r\nTry again later or download manually from the Gitee release page.",
            RegexOptions.Singleline);
        return result;
    }

    public static void Attach(Control root)
    {
        if (!IsEnglish) return;
        AttachOne(root);
    }

    private static void AttachOne(Control control)
    {
        if (!Attached.Add(control)) return;
        TranslateControl(control);
        control.ControlAdded += (_, e) =>
        {
            if (e.Control is not null) AttachOne(e.Control);
        };
        control.TextChanged += (_, _) => TranslateControl(control);
        foreach (Control? child in control.Controls)
        {
            if (child is not null) AttachOne(child);
        }
    }

    private static void TranslateControl(Control control)
    {
        if (_translating || !IsEnglish) return;
        if (control is TextBox { ReadOnly: false }) return;
        var translated = T(control.Text);
        if (translated == control.Text) return;
        try
        {
            _translating = true;
            control.Text = translated;
        }
        finally
        {
            _translating = false;
        }
    }

    private static readonly IReadOnlyDictionary<string, string> EnglishText =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["演讲计时器"] = "Presentation Timer",
            ["演讲计时器设置"] = "FlyPPTTimer Settings",
            ["远程控制"] = "Remote Control",
            ["时长设置"] = "Timer",
            ["行为设置"] = "Behavior",
            ["外观与显示"] = "Appearance & Display",
            ["控制设置"] = "Controls",
            ["其他设置"] = "Other",
            ["基础计时"] = "Basic Timer",
            ["文件规则"] = "Presentation Rules",
            ["提示 1"] = "Alert 1",
            ["提示 2"] = "Alert 2",
            ["计时结束"] = "Time Up",
            ["配色"] = "Colors",
            ["窗口尺寸与字号"] = "Window Size & Font",
            ["计时器窗口"] = "Timer window",
            ["显示计时器窗口"] = "Show timer window",
            ["大屏计时模式"] = "Full-screen timer mode",
            ["默认位置"] = "Default Position",
            ["多屏显示"] = "Multiple Displays",
            ["启用大屏计时器"] = "Enable full-screen timer",
            ["大屏显示屏幕"] = "Full-screen timer display",
            ["需要扩展屏"] = "Extended display required",
            ["窗口行为"] = "Window Behavior",
            ["快捷键"] = "Hotkeys",
            ["全局与启动"] = "Global & Startup",
            ["本地网页遥控"] = "Local Web Remote",
            ["访问地址"] = "Access Addresses",
            ["操作"] = "Actions",
            ["防火墙排障"] = "Firewall Troubleshooting",
            ["软件更新"] = "Software Updates",
            ["语言"] = "Language",
            ["界面语言"] = "Display language",
            ["配置管理"] = "Configuration",
            ["文件位置"] = "File Locations",
            ["关于 FlyPPTTimer"] = "About FlyPPTTimer",
            ["作者与协作"] = "Author & Collaboration",
            ["应用"] = "Apply",
            ["确定"] = "OK",
            ["确认"] = "Confirm",
            ["取消"] = "Cancel",
            ["退出"] = "Exit",
            ["设置"] = "Settings",
            ["保存"] = "Save",
            ["删除"] = "Delete",
            ["清空"] = "Clear",
            ["清空列表"] = "Clear list",
            ["添加"] = "Add",
            ["添加文件"] = "Add files",
            ["批量设置"] = "Batch edit",
            ["恢复默认"] = "Restore defaults",
            ["选择文件"] = "Choose file",
            ["启用"] = "Enabled",
            ["关闭"] = "Stop",
            ["启动"] = "Start",
            ["重启"] = "Restart",
            ["刷新"] = "Refresh",
            ["更多"] = "More",
            ["打开"] = "Open",
            ["复制链接"] = "Copy link",
            ["浏览器打开"] = "Open in browser",
            ["复制路径"] = "Copy path",
            ["显示文件"] = "Show file",
            ["当前版本"] = "Current version",
            ["项目介绍"] = "About the project",
            ["作者的话"] = "From the author",
            ["联系邮箱"] = "Email",
            ["联系作者"] = "Contact the author",
            ["发送邮件"] = "Send email",
            ["GitHub 项目主页"] = "GitHub project",
            ["Gitee 项目主页"] = "Gitee mirror",
            ["打开 GitHub（可能需要网络工具）"] = "Open GitHub",
            ["打开 Gitee（中国大陆可直接访问）"] = "Open Gitee",
            ["启动时检测新版本"] = "Check for updates at startup",
            ["手动检测"] = "Manual check",
            ["立即检测新版本"] = "Check for updates",
            ["配置导入"] = "Import configuration",
            ["配置导出"] = "Export configuration",
            ["配置文件"] = "Configuration file",
            ["日志文件"] = "Log files",
            ["打开配置文件位置"] = "Open configuration folder",
            ["打开日志文件位置"] = "Open log folder",
            ["默认时长 HH:mm:ss"] = "Default duration (HH:mm:ss)",
            ["计时模式"] = "Timer mode",
            ["倒计时"] = "Countdown",
            ["正计时"] = "Count up",
            ["到达预设时间后"] = "At the preset time",
            ["停止计时"] = "Stop timer",
            ["继续显示超时"] = "Continue into overtime",
            ["到零后停止"] = "Stop at zero",
            ["时间到后的操作"] = "Time-up action",
            ["仅提示"] = "Alert only",
            ["黑屏并显示“时间到”"] = "Black screen with “Time's up”",
            ["退出放映"] = "End slide show",
            ["距离预设时间还剩（秒）"] = "Seconds before the preset time",
            ["提示1"] = "Alert 1",
            ["提示2"] = "Alert 2",
            ["到时语音播报"] = "Time-up voice announcement",
            ["提示1语音播报"] = "Alert 1 voice announcement",
            ["提示2语音播报"] = "Alert 2 voice announcement",
            ["到时提示音"] = "Time-up sound",
            ["提示1提示音"] = "Alert 1 sound",
            ["提示2提示音"] = "Alert 2 sound",
            ["选择到时提示音"] = "Choose time-up sound",
            ["选择提示1提示音"] = "Choose Alert 1 sound",
            ["选择提示2提示音"] = "Choose Alert 2 sound",
            ["清除到时提示音"] = "Clear time-up sound",
            ["清除提示1提示音"] = "Clear Alert 1 sound",
            ["清除提示2提示音"] = "Clear Alert 2 sound",
            ["配色方案"] = "Color scheme",
            ["字体颜色"] = "Text color",
            ["背景颜色"] = "Background color",
            ["超时文字颜色"] = "Overtime text color",
            ["超时背景颜色"] = "Overtime background color",
            ["闪烁背景颜色"] = "Flash background color",
            ["背景不透明度"] = "Background opacity",
            ["宽"] = "Width",
            ["高"] = "Height",
            ["字号"] = "Font size",
            ["外观形状"] = "Window shape",
            ["超时前缀"] = "Overtime prefix",
            ["指定屏幕"] = "Target display",
            ["单屏显示屏幕"] = "Single-display target",
            ["主屏幕"] = "Primary display",
            ["默认点位"] = "Default anchor",
            ["左上"] = "Top left",
            ["上中"] = "Top center",
            ["右上"] = "Top right",
            ["左中"] = "Middle left",
            ["正中"] = "Center",
            ["右中"] = "Middle right",
            ["左下"] = "Bottom left",
            ["下中"] = "Bottom center",
            ["右下"] = "Bottom right",
            ["水平微调百分比"] = "Horizontal offset (%)",
            ["垂直微调百分比"] = "Vertical offset (%)",
            ["所有屏幕同时显示"] = "Show on all displays",
            ["窗口位置"] = "Window position",
            ["重置计时窗口位置"] = "Reset timer window position",
            ["鼠标穿透"] = "Click-through",
            ["锁定窗口"] = "Lock window",
            ["托盘最小化"] = "Minimize to tray",
            ["关闭按钮行为"] = "Close button behavior",
            ["退出程序"] = "Exit application",
            ["最小化到托盘"] = "Minimize to tray",
            ["开始/暂停快捷键"] = "Start/Pause hotkey",
            ["停止/重置快捷键"] = "Stop/Reset hotkey",
            ["显示/隐藏快捷键"] = "Show/Hide hotkey",
            ["全屏白名单自动开始"] = "Auto-start for fullscreen apps",
            ["退出全屏时停止计时"] = "Stop when leaving fullscreen",
            ["退出全屏时重置"] = "Reset when leaving fullscreen",
            ["暂停时闪烁当前时间"] = "Flash current time when paused",
            ["启用远程控制"] = "Enable remote control",
            ["当前服务状态"] = "Service status",
            ["本次启动端口"] = "Current port",
            ["下次服务端口"] = "Port on next start",
            ["使用随机端口"] = "Use a random port",
            ["连接设备数量"] = "Connected devices",
            ["推荐访问地址"] = "Recommended address",
            ["手机可用局域网地址"] = "LAN addresses for mobile devices",
            ["重启远程服务"] = "Restart remote service",
            ["重启远程服务并应用端口"] = "Restart service and apply port",
            ["重新生成令牌"] = "Regenerate token",
            ["断开所有设备"] = "Disconnect all devices",
            ["断开所有远程设备"] = "Disconnect all remote devices",
            ["复制访问地址"] = "Copy access address",
            ["复制推荐 URL"] = "Copy recommended URL",
            ["打开本机控制页"] = "Open local control page",
            ["防火墙说明"] = "Firewall help",
            ["修复命令"] = "Repair command",
            ["复制修复命令"] = "Copy repair command",
            ["复制防火墙修复命令"] = "Copy firewall repair command",
            ["二维码显示"] = "QR code",
            ["远程连接"] = "Remote connection",
            ["演示文稿"] = "Presentations",
            ["页面导航"] = "Navigation",
            ["手机或浏览器访问"] = "Mobile or browser access",
            ["通过手机或浏览器控制演示"] = "Mobile or browser access",
            ["规则与放映"] = "Rules and slide show",
            ["服务状态卡"] = "Service status",
            ["手机扫码"] = "Scan with your phone",
            ["手机扫码连接"] = "Scan with your phone",
            ["浏览器访问"] = "Browser access",
            ["地址"] = "Address",
            ["链接"] = "Link",
            ["本机 IP"] = "IP address",
            ["访问链接"] = "Access link",
            ["复制"] = "Copy",
            ["在浏览器中打开"] = "Open in browser",
            ["允许远程控制"] = "Allow remote control",
            ["停止服务"] = "Stop service",
            ["启动服务"] = "Start service",
            ["路径"] = "Path",
            ["时长与规则"] = "Duration and rule",
            ["放映"] = "Slide show",
            ["放映控制"] = "Slide show",
            ["危险操作"] = "Danger zone",
            ["运行中"] = "Running",
            ["已停止"] = "Stopped",
            ["同一网络可访问。"] = "Available on the same network.",
            ["同一网络下的手机或电脑均可访问。"] = "Available on the same network.",
            ["手机与电脑需连接同一网络。"] = "The phone and computer must be on the same network.",
            ["手机与电脑需连接同一局域网；也可通过手机热点或电脑热点创建局域网进行控制。"] = "Connect through the same LAN, or create one with a phone or computer hotspot.",
            ["未检测到可供手机访问的局域网地址"] = "No mobile-accessible LAN address found",
            ["演示文稿列表"] = "Presentation list",
            ["暂无演示文稿"] = "No presentations",
            ["未选择"] = "Not selected",
            ["未选择演示文稿"] = "Not selected",
            ["浏览"] = "Browse",
            ["请选择演示文稿。"] = "Select a presentation.",
            ["规则已启用。"] = "Rule enabled.",
            ["规则已禁用。"] = "Rule disabled.",
            ["启用规则"] = "Enable rule",
            ["禁用规则"] = "Disable rule",
            ["规则设置"] = "Rule settings",
            ["已启用"] = "Enabled",
            ["已禁用"] = "Disabled",
            ["待打开"] = "Ready to open",
            ["当前"] = "Current",
            ["已打开"] = "Open",
            ["放映中"] = "Presenting",
            ["无规则"] = "No rule",
            ["缺失"] = "Missing",
            ["从头放映"] = "Start from beginning",
            ["当前页放映"] = "Start from current slide",
            ["结束放映"] = "End slide show",
            ["关闭当前文档"] = "Close current presentation",
            ["关闭当前文稿"] = "Close current presentation",
            ["打开演示文稿"] = "Open presentation",
            ["退出软件"] = "Quit software",
            ["点击退出并关闭程序"] = "Quit the application",
            ["退出软件卡"] = "Quit-software card",
            ["确认退出软件"] = "Confirm quit",
            ["关闭最后打开的文稿"] = "Close last-opened presentation",
            ["退出演示软件"] = "Quit presentation software",
            ["确认退出演示软件"] = "Confirm quitting presentation software",
            ["有未应用的更改"] = "Unapplied changes",
            ["批量设置文件规则"] = "Batch edit presentation rules",
            ["同步文件规则时长"] = "Sync presentation-rule durations",
            ["统一时长"] = "Set duration",
            ["统一计时方式"] = "Set timer mode",
            ["未选择文件"] = "No file selected",
            ["选择"] = "Select",
            ["简体中文"] = "Simplified Chinese",
            ["跟随系统"] = "Follow system",
            ["是"] = "Yes",
            ["否"] = "No",
            ["确定"] = "OK",
            ["取消"] = "Cancel",
            ["重试"] = "Retry",
            ["下次启动生效"] = "Takes effect after restart",
            ["更改界面语言后，请退出并重新启动 FlyPPTTimer。安装版和便携版均会记住此选项。"] = "After changing the display language, exit and restart FlyPPTTimer. Both installed and portable editions remember this setting.",
            ["FlyPPTTimer 是一款面向演讲、教学和会议场景的 Windows 演示计时工具，提供倒计时、正计时、多显示器悬浮显示、演示文稿规则，以及手机或浏览器局域网远程控制功能。软件配置、规则和日志默认保存在本机；远程控制仅在本地网络中运行，不依赖云端账户，也不会主动上传演示文稿内容。"] = "FlyPPTTimer is a Windows presentation timer for talks, teaching, and meetings. It provides countdown and count-up modes, multi-display overlays, presentation-specific rules, and LAN remote control from a phone or browser. Configuration, rules, and logs stay on this computer. Remote control runs only on the local network, requires no cloud account, and never uploads presentation content.",
            ["FlyPPTTimer 由曹虎男发起并从零开发。作者毕业于南京大学医学院护理专业，目前就职于江苏省人民医院宿迁医院。在工作实践中发现了演讲计时、演示控制和台下远程调整的实际需求，因此将这个想法逐步实现为本项目。希望它能让大家的演讲、教学和会议更加从容，也欢迎有兴趣的朋友参与测试、提出建议或共同开发。祝大家使用愉快！"] = "FlyPPTTimer was created and built from scratch by Hunan Cao, a nursing graduate of Nanjing University Medical School who currently works at Suqian Hospital of Jiangsu Province Hospital. Practical needs around talk timing, presentation control, and off-stage adjustments inspired the project. Contributions, testing, and suggestions are welcome.",
            ["服务运行中端口会保持固定。修改端口或随机端口设置后，请点击“重启远程服务并应用端口”，或下次启动后生效。"] = "The port remains fixed while the service is running. After changing the port or random-port option, restart the remote service or restart the app.",
            ["手机无法访问时，常见原因是 Windows 防火墙、IP 选错、端口被占用、手机和电脑不在同一网络。不要关闭防火墙；只为当前程序和当前端口添加入站规则。"] = "If a phone cannot connect, common causes include Windows Firewall, a wrong IP address, an occupied port, or devices being on different networks. Do not disable the firewall; add an inbound rule only for this app and port.",
            ["可从计时器或托盘右键菜单打开远程控制二维码。"] = "Open the remote-control QR code from the timer or tray context menu.",
            ["正在检测新版本，请稍候。"] = "Checking for updates. Please wait.",
            ["检测新版本"] = "Check for updates",
            ["发现新版本"] = "Update available",
            ["当前使用的是绿色便携版，程序不会自动覆盖文件。是否打开 Gitee Release 页面自行选择下载？"] = "You are using the portable edition, so the app will not overwrite its own files. Open the Gitee release page to download the update?",
            ["此 Release 暂未找到 Windows x64 安装包。是否打开 Gitee Release 页面？"] = "No Windows x64 installer was found in this release. Open the Gitee release page?",
            ["是否立即下载安装？安装时会保留当前配置，新功能仍使用默认设置，之后可自行选择。"] = "Download and install now? Your current configuration will be preserved.",
            ["设置中有未应用的更改。是：应用并关闭；否：放弃更改；取消：继续编辑。"] = "Settings contain unapplied changes. Yes: apply and close; No: discard; Cancel: continue editing.",
            ["确定清空所有文件计时规则？"] = "Clear all presentation timer rules?",
            ["请先勾选要批量修改的文件规则。"] = "Select at least one presentation rule to edit.",
            ["应用设置后，计时窗口中心将还原到当前选择的默认点位。"] = "After applying, the timer window center will return to the selected default anchor.",
            ["未检测到手机可访问的局域网地址。请先让手机和电脑连接同一 Wi-Fi 或局域网。"] = "No mobile-accessible LAN address was found. Connect the phone and computer to the same Wi-Fi or LAN.",
            ["默认时长必须是 HH:mm:ss 格式。"] = "Default duration must use HH:mm:ss format.",
            ["文件规则中不能重复添加同一份演示文稿。"] = "The same presentation cannot be added more than once.",
            ["计时时长必须是大于 00:00:00 的 HH:mm:ss。"] = "Duration must be HH:mm:ss and greater than 00:00:00.",
            ["FlyPPTTimer 更新"] = "FlyPPTTimer Update",
            ["时间到"] = "TIME'S UP",
            ["闪烁测试"] = "Flash test",
            ["直角矩形"] = "Rectangle",
            ["圆角矩形（小）"] = "Rounded rectangle (small)",
            ["圆角矩形（大）"] = "Rounded rectangle (large)",
            ["医疗卫生（蓝白）"] = "Healthcare (blue & white)",
            ["无"] = "None",
            ["闪烁文字"] = "Flash text",
            ["闪烁背景"] = "Flash background",
            ["实线边框"] = "Solid border",
            ["边框加背景"] = "Border + background",
            ["边框+背景"] = "Border + background",
            ["需要重启"] = "Restart required",
            ["界面语言已更改。是否立即重启 FlyPPTTimer 以应用更改？\r\n\r\n选择“否”将在下次启动时应用。"] = "The display language has changed. Restart FlyPPTTimer now to apply it?\r\n\r\nChoose No to apply it the next time the app starts.",
            ["无法自动重启 FlyPPTTimer。请手动退出并重新打开软件。"] = "FlyPPTTimer could not restart automatically. Exit and reopen the app manually.",
            ["触发闪烁"] = "Trigger flash",
            ["打开失败，请复制链接。"] = "Failed to open. Copy the link instead.",
            ["端口"] = "Port",
            ["端口生效说明"] = "Port activation",
            ["二维码卡"] = "QR code card",
            ["放行命令"] = "Allow command",
            ["放映卡"] = "Slide-show card",
            ["规则编辑卡"] = "Rule editor card",
            ["继续"] = "Resume",
            ["减少 1 分钟"] = "Subtract 1 minute",
            ["将强制关闭全部 PowerPoint/WPS 演示进程，未保存内容会丢失。"] = "This force-closes all PowerPoint/WPS presentation processes. Unsaved work will be lost.",
            ["静音/取消静音"] = "Mute / Unmute",
            ["开始"] = "Start",
            ["开始/暂停"] = "Start / Pause",
            ["浏览器访问卡"] = "Browser access card",
            ["命令已复制。"] = "Command copied.",
            ["切换倒计时/正计时"] = "Toggle countdown / count up",
            ["请让手机与电脑连接同一 Wi-Fi 或局域网后刷新。"] = "Connect the phone and computer to the same Wi-Fi or LAN, then refresh.",
            ["请让手机与电脑连接同一 Wi-Fi 或局域网后重试。"] = "Connect the phone and computer to the same Wi-Fi or LAN, then try again.",
            ["请先让手机与电脑连接同一局域网。"] = "Connect the phone and computer to the same LAN first.",
            ["设置为 3 分钟"] = "Set to 3 minutes",
            ["设置为 5 分钟"] = "Set to 5 minutes",
            ["设置为 8 分钟"] = "Set to 8 minutes",
            ["设置为 10 分钟"] = "Set to 10 minutes",
            ["设置为 15 分钟"] = "Set to 15 minutes",
            ["时间即将结束"] = "Time is almost up",
            ["时长"] = "Duration",
            ["手机与电脑需连接同一网络。"] = "The phone and computer must be on the same network.",
            ["添加演示文稿"] = "Add presentations",
            ["停止"] = "Stop",
            ["停止/重置"] = "Stop / Reset",
            ["危险操作卡"] = "Danger-zone card",
            ["未检测到局域网地址"] = "No LAN address detected",
            ["未检测到可供手机访问的局域网地址。"] = "No mobile-accessible LAN address found.",
            ["文件"] = "File",
            ["文字"] = "Text",
            ["显示/隐藏窗口"] = "Show / Hide window",
            ["显示窗口"] = "Show window",
            ["显示已隐藏 token；复制仍为完整链接。"] = "The token is hidden on screen; copying still uses the full link.",
            ["选择 PPT 文件"] = "Select presentation files",
            ["选择此文件规则"] = "Select this presentation rule",
            ["选择提示音文件"] = "Select alert sound",
            ["演示工具栏"] = "Presentation toolbar",
            ["演示文稿列表卡"] = "Presentation-list card",
            ["演示状态提示"] = "Presentation status",
            ["已打开。"] = "Opened.",
            ["音频文件 (*.mp3;*.wav;*.wma;*.m4a)|*.mp3;*.wav;*.wma;*.m4a"] = "Audio files (*.mp3;*.wav;*.wma;*.m4a)|*.mp3;*.wav;*.wma;*.m4a",
            ["隐藏窗口"] = "Hide window",
            ["预设时间到"] = "Preset time reached",
            ["暂停"] = "Pause",
            ["增加 1 分钟"] = "Add 1 minute",
            ["正在从 Gitee 检测新版本…"] = "Checking Gitee for updates…",
            ["正在下载更新，完成前请勿退出程序…"] = "Downloading the update. Do not exit until it finishes…",
            ["重置"] = "Reset",
            ["状态"] = "Status",
            ["Gitee 项目目前还没有发布 Release。"] = "The Gitee project has no releases yet.",
            ["PowerPoint (*.ppt;*.pptx;*.pptm;*.pps;*.ppsx)|*.ppt;*.pptx;*.pptm;*.pps;*.ppsx|所有文件 (*.*)|*.*"] = "PowerPoint (*.ppt;*.pptx;*.pptm;*.pps;*.ppsx)|*.ppt;*.pptx;*.pptm;*.pps;*.ppsx|All files (*.*)|*.*",
            ["演示文稿 (*.ppt;*.pptx;*.pptm)|*.ppt;*.pptx;*.pptm|所有文件 (*.*)|*.*"] = "Presentations (*.ppt;*.pptx;*.pptm)|*.ppt;*.pptx;*.pptm|All files (*.*)|*.*",
            ["0 项"] = "0 items",
            ["Microsoft PowerPoint 未运行。"] = "Microsoft PowerPoint is not running.",
            ["PowerPoint 操作失败，请查看程序日志。"] = "The PowerPoint operation failed. See the application log.",
            ["PowerPoint 命令队列繁忙，请稍后重试。"] = "The PowerPoint command queue is busy. Try again shortly.",
            ["PowerPoint 响应超时，计时遥控仍可继续使用。"] = "PowerPoint timed out. Timer remote control is still available.",
            ["PowerPoint 正忙、文稿受保护，或当前操作不可用，请稍后重试。"] = "PowerPoint is busy, the presentation is protected, or the operation is unavailable. Try again shortly.",
            ["PowerPoint 中没有活动演示文稿。"] = "PowerPoint has no active presentation.",
            ["白屏"] = "White screen",
            ["黑屏"] = "Black screen",
            ["正常"] = "Normal",
            ["电脑声音已恢复"] = "Computer audio restored",
            ["电脑已静音"] = "Computer muted",
            ["当前没有可关闭的演示文稿。"] = "There is no presentation to close.",
            ["当前没有正在运行的 PowerPoint 放映。"] = "No PowerPoint slide show is running.",
            ["操作过快，本次翻页已忽略"] = "The command was too fast; this slide change was ignored.",
            ["放映已经在运行，本次重复启动已忽略"] = "The slide show is already running; the duplicate start was ignored.",
            ["强制退出会丢失所有未保存内容，请再次确认。"] = "Force quit will discard all unsaved work. Confirm again.",
            ["请输入有效页码。"] = "Enter a valid slide number.",
            ["请先选择演示文稿。"] = "Select a presentation first.",
            ["所选演示文稿文件不存在。"] = "The selected presentation file does not exist.",
            ["未安装 Microsoft PowerPoint。"] = "Microsoft PowerPoint is not installed.",
            ["未发现正在运行的 PowerPoint 或 WPS 演示进程。"] = "No running PowerPoint or WPS Presentation process was found.",
            ["无法启动 Microsoft PowerPoint。"] = "Microsoft PowerPoint could not be started.",
            ["无法切换电脑主音量静音状态。"] = "The computer's master mute state could not be changed.",
            ["演示操作正在进行，请等待当前操作完成。"] = "A presentation operation is in progress. Wait for it to finish.",
            ["演示控制服务当前不可用。"] = "The presentation-control service is unavailable.",
            ["演示控制服务已关闭。"] = "The presentation-control service is disabled.",
            ["演示命令队列繁忙，请稍后重试。"] = "The presentation command queue is busy. Try again shortly.",
            ["演示文稿文件不存在。"] = "The presentation file does not exist.",
            ["已从头开始放映"] = "Slide show started from the beginning",
            ["已结束"] = "Ended",
            ["已结束放映"] = "Slide show ended",
            ["已启动"] = "Started",
            ["已切换到上一页"] = "Moved to the previous slide",
            ["已切换到下一页"] = "Moved to the next slide",
            ["已请求退出演示软件。未保存内容不会恢复。"] = "Presentation software exit requested. Unsaved work cannot be recovered.",
            ["已退出“时间到”黑屏"] = "Dismissed the “Time's up” screen",
            ["正在打开演示文稿"] = "Opening presentation",
            ["正在关闭最后打开的文稿"] = "Closing the last-opened presentation",
            ["正在结束放映"] = "Ending slide show",
            ["正在启动 PowerPoint"] = "Starting PowerPoint",
            ["正在启动放映"] = "Starting slide show",
            ["正在强制退出演示程序"] = "Force-quitting presentation software",
            ["正在执行演示命令"] = "Running presentation command",
            ["状态已刷新"] = "Status refreshed",
            ["Gitee Release 的版本标签无法识别。"] = "The Gitee release version tag is not recognized.",
            ["Release 中的 SHA-256 校验文件格式不正确。"] = "The release SHA-256 checksum file has an invalid format.",
            ["此 Release 中未找到 Windows x64 安装版。"] = "No Windows x64 installer was found in this release.",
            ["当前程序版本无法识别。"] = "The current application version is not recognized.",
            ["下载的安装程序 SHA-256 校验失败，已停止安装。"] = "The downloaded installer failed SHA-256 verification. Installation was stopped.",
            ["下载的安装程序不存在。"] = "The downloaded installer does not exist.",
            ["无法启动更新安装程序。"] = "The update installer could not be started.",
            ["WPS 演示主窗口"] = "WPS Presentation main window",
            ["放映窗口"] = "Slide-show window",
            ["文稿窗口"] = "Presentation window",
            ["高对比警示（黑红）"] = "High-contrast warning (black & red)",
            ["教育培训（深蓝金）"] = "Education (navy & gold)",
            ["科技发布（深色青蓝）"] = "Technology launch (dark cyan)",
            ["商务会议（石墨蓝）"] = "Business meeting (graphite blue)",
            ["医疗与卫生-手术室蓝"] = "Healthcare – operating-room blue",
            ["默认"] = "Default",
            ["自定义"] = "Custom",
            ["无线网络"] = "Wi-Fi",
            ["以太网"] = "Ethernet",
            ["手机热点或 USB 共享网络"] = "Mobile hotspot or USB tethering",
            ["代理/TUN 虚拟网卡（手机不可用）"] = "Proxy/TUN virtual adapter (not available to phones)",
            ["虚拟网卡"] = "Virtual adapter",
            ["自动私有地址"] = "Automatic private address",
            ["其他网络"] = "Other network",
            ["未启动"] = "Not started",
            ["未找到"] = "Not found",
            ["正在"] = "In progress",
            ["启动失败"] = "Failed to start",
            ["命令已执行"] = "Command completed",
            ["令牌无效或远程控制已关闭"] = "The token is invalid or remote control is disabled",
            ["命令不被允许"] = "Command not allowed",
            ["命令不在 PowerPoint 控制白名单中。"] = "The command is not in the PowerPoint control allowlist.",
            ["命令不在演示控制白名单中。"] = "The command is not in the presentation-control allowlist.",
            ["连接在请求头完成前关闭。"] = "The connection closed before the request headers completed.",
            ["请求体过大。"] = "The request body is too large.",
            ["请求体未完整发送。"] = "The request body was not sent completely.",
            ["请求头过大。"] = "The request headers are too large.",
            ["请选择 MP3、WAV、WMA 或 M4A 音频文件。"] = "Select an MP3, WAV, WMA, or M4A audio file.",
            ["所选提示音文件不存在。"] = "The selected alert-sound file does not exist.",
            ["所选文件不在允许的演示文稿列表中。"] = "The selected file is not in the allowed presentation list.",
            ["所选演示文稿不在已启用规则中。"] = "The selected presentation is not in the enabled rules.",
            ["未能按目标文稿匹配放映窗口。"] = "No slide-show window matched the target presentation.",
            ["未找到当前受控演示文稿对应的放映窗口。"] = "The slide-show window for the controlled presentation was not found.",
            ["文稿已打开但无法定位可最大化的窗口"] = "The presentation opened, but no window could be found to maximize",
            ["放映已启动但 COM 激活失败"] = "The slide show started, but COM activation failed",
            ["放映已启动但未找到目标放映窗口"] = "The slide show started, but the target slide-show window was not found",
            ["放映已启动但无法读取窗口句柄"] = "The slide show started, but its window handle could not be read",
            ["检测到 WPS 演示；当前版本未声明可靠的 WPS 文稿 COM 关闭能力，只允许明确确认后的强制退出。"] = "WPS Presentation was detected. This version does not claim reliable COM close support for WPS documents; only explicitly confirmed force quit is allowed."
        };

    private static readonly IReadOnlyDictionary<string, string> EnglishFragments =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["已保存。"] = "Saved.",
            ["已删除。"] = "Deleted.",
            ["已添加。"] = "Added.",
            ["已复制。"] = "Copied.",
            ["复制失败。"] = "Copy failed.",
            ["打开失败。"] = "Failed to open.",
            ["请先选择。"] = "Select an item first.",
            ["文件不存在。"] = "File not found.",
            ["请选择演示文稿"] = "Select a presentation",
            ["PowerPoint 不可用。"] = "PowerPoint is unavailable.",
            ["未检测到 PowerPoint"] = "PowerPoint not detected",
            ["未打开演示文稿"] = "No presentation is open"
        };
}
