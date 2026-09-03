use std::{cell::RefCell, collections::BTreeSet, fs, path::PathBuf, rc::Rc};

use slint::{ComponentHandle, ModelRc, SharedString, VecModel};

use crate::{
    app::{RuleItem, SettingItem, SettingsWindow},
    config::{AppConfig, CloseButtonBehavior, FileRule, OverlayAnchor, TimerEndAction, TimerMode},
    display,
    remote::RemoteServer,
};

#[derive(Clone, Copy)]
struct Language(bool);

impl Language {
    fn from_config(language: &str) -> Self {
        Self(crate::config::ui_is_english(language))
    }
    fn english(self) -> bool {
        self.0
    }
}

fn t<'a>(lang: Language, zh: &'a str, en: &'a str) -> &'a str {
    if lang.english() { en } else { zh }
}

fn localize(lang: Language, value: &str) -> &str {
    if !lang.english() {
        return value;
    }
    match value {
        "时长设置" => "Timer",
        "行为设置" => "Behavior",
        "外观与显示" => "Appearance & Display",
        "远程控制" => "Remote Control",
        "控制设置" => "Controls",
        "其他设置" => "Other",
        "基础计时" => "Basic Timer",
        "文件规则" => "Presentation Rules",
        "默认时长 HH:mm:ss" => "Default duration (HH:mm:ss)",
        "计时模式" => "Timer mode",
        "倒计时" => "Countdown",
        "正计时" => "Count up",
        "到达预设时间后" => "At the preset time",
        "停止计时" => "Stop timer",
        "继续显示超时" => "Continue into overtime",
        "时间到后的操作" => "Time-up action",
        "仅提示" => "Alert only",
        "黑屏并显示“时间到”" => "Black screen with “Time's up”",
        "退出放映" => "End slide show",
        "启用" => "Enabled",
        "全局与启动" => "Global & Startup",
        "退出全屏时停止计时" => "Stop when leaving fullscreen",
        "全屏白名单自动开始" => "Auto-start for fullscreen apps",
        "退出全屏时重置" => "Reset when leaving fullscreen",
        "暂停时闪烁当前时间" => "Flash current time when paused",
        "提示 1" => "Alert 1",
        "提示1" => "Alert 1",
        "距离预设时间还剩（秒）" => "Seconds before the preset time",
        "提示1语音播报" => "Alert 1 voice announcement",
        "提示1提示音" => "Alert 1 sound",
        "选择提示1提示音" => "Choose Alert 1 sound",
        "清除提示1提示音" => "Clear Alert 1 sound",
        "提示 2" => "Alert 2",
        "提示2" => "Alert 2",
        "提示2语音播报" => "Alert 2 voice announcement",
        "提示2提示音" => "Alert 2 sound",
        "选择提示2提示音" => "Choose Alert 2 sound",
        "清除提示2提示音" => "Clear Alert 2 sound",
        "计时结束" => "Time Up",
        "到时语音播报" => "Time-up voice announcement",
        "到时提示音" => "Time-up sound",
        "选择到时提示音" => "Choose time-up sound",
        "清除到时提示音" => "Clear time-up sound",
        "无" => "None",
        "闪烁文字" => "Flash text",
        "闪烁背景" => "Flash background",
        "实线边框" => "Solid border",
        "边框加背景" => "Border + background",
        "超时文字颜色" => "Overtime text color",
        "超时背景颜色" => "Overtime background color",
        "超时前缀" => "Overtime prefix",
        "计时器窗口" => "Timer window",
        "显示计时器窗口" => "Show timer window",
        "配色" => "Colors",
        "配色方案" => "Color scheme",
        "字体颜色" => "Text color",
        "背景颜色" => "Background color",
        "闪烁背景颜色" => "Flash background color",
        "医疗卫生（蓝白）" => "Healthcare (blue & white)",
        "教育培训（深蓝金）" => "Education (deep blue & gold)",
        "商务会议（石墨蓝）" => "Business (graphite blue)",
        "科技发布（深色青蓝）" => "Tech launch (dark teal)",
        "高对比警示（黑红）" => "High-contrast warning (black & red)",
        "自定义" => "Custom",
        "窗口尺寸与字号" => "Window Size & Font",
        "宽" => "Width",
        "高" => "Height",
        "字号" => "Font size",
        "外观形状" => "Window shape",
        "直角矩形" => "Rectangle",
        "圆角矩形（小）" => "Rounded rectangle (small)",
        "圆角矩形（大）" => "Rounded rectangle (large)",
        "背景不透明度" => "Background opacity",
        "多屏显示" => "Multiple Displays",
        "所有屏幕同时显示" => "Show on all displays",
        "单屏显示屏幕" => "Single-display target",
        "主屏幕" => "Primary display",
        "大屏计时模式" => "Full-screen timer mode",
        "启用大屏计时器" => "Enable full-screen timer",
        "大屏显示屏幕" => "Full-screen timer display",
        "需要扩展屏" => "Extended display required",
        "默认位置" => "Default Position",
        "默认点位" => "Default anchor",
        "左上" => "Top left",
        "上中" => "Top center",
        "右上" => "Top right",
        "左中" => "Middle left",
        "正中" => "Center",
        "右中" => "Middle right",
        "左下" => "Bottom left",
        "下中" => "Bottom center",
        "右下" => "Bottom right",
        "水平微调百分比" => "Horizontal offset (%)",
        "垂直微调百分比" => "Vertical offset (%)",
        "窗口位置" => "Window position",
        "重置计时窗口位置" => "Reset timer window position",
        "快捷键" => "Hotkeys",
        "开始/暂停快捷键" => "Start/Pause hotkey",
        "停止/重置快捷键" => "Stop/Reset hotkey",
        "显示/隐藏快捷键" => "Show/Hide hotkey",
        "窗口行为" => "Window Behavior",
        "鼠标穿透" => "Click-through",
        "锁定窗口" => "Lock window",
        "托盘最小化" => "Minimize to tray",
        "关闭按钮行为" => "Close button behavior",
        "退出程序" => "Exit application",
        "最小化到托盘" => "Minimize to tray",
        "本地网页遥控" => "Local Web Remote",
        "启用远程控制" => "Enable remote control",
        "当前服务状态" => "Service status",
        "本次启动端口" => "Current port",
        "下次服务端口" => "Port on next start",
        "使用随机端口" => "Use a random port",
        "端口生效说明" => "Port activation",
        "连接设备数量" => "Connected devices",
        "访问地址" => "Access Addresses",
        "推荐访问地址" => "Recommended address",
        "手机可用局域网地址" => "LAN addresses for mobile devices",
        "操作" => "Actions",
        "重启远程服务" => "Restart remote service",
        "重启远程服务并应用端口" => "Restart service and apply port",
        "重新生成令牌" => "Regenerate token",
        "断开所有设备" => "Disconnect all devices",
        "断开所有远程设备" => "Disconnect all remote devices",
        "复制访问地址" => "Copy access address",
        "复制推荐 URL" => "Copy recommended URL",
        "打开本机控制页" => "Open local control page",
        "防火墙排障" => "Firewall Troubleshooting",
        "防火墙说明" => "Firewall help",
        "修复命令" => "Repair command",
        "复制修复命令" => "Copy repair command",
        "复制防火墙修复命令" => "Copy firewall repair command",
        "二维码显示" => "QR code",
        "语言" => "Language",
        "界面语言" => "Display language",
        "跟随系统" => "Follow system",
        "下次启动生效" => "Takes effect after restart",
        "软件更新" => "Software Updates",
        "启动时检测新版本" => "Check for updates at startup",
        "手动检测" => "Manual check",
        "立即检测新版本" => "Check for updates",
        "配置管理" => "Configuration",
        "配置导入" => "Import configuration",
        "配置导出" => "Export configuration",
        "恢复默认" => "Restore defaults",
        "文件位置" => "File Locations",
        "配置文件" => "Configuration file",
        "打开配置文件位置" => "Open configuration folder",
        "日志文件" => "Log files",
        "打开日志文件位置" => "Open log folder",
        "关于 FlyPPTTimer" => "About FlyPPTTimer",
        "当前版本" => "Current version",
        "项目介绍" => "About the project",
        "作者与协作" => "Author & Collaboration",
        "作者的话" => "From the author",
        "联系邮箱" => "Email",
        "GitHub 项目主页" => "GitHub project",
        "打开 GitHub（可能需要网络工具）" => "Open GitHub",
        "Gitee 项目主页" => "Gitee mirror",
        "打开 Gitee（中国大陆可直接访问）" => "Open Gitee",
        "联系作者" => "Contact the author",
        "发送邮件" => "Send email",
        "文件" => "File",
        "路径" => "Path",
        "时长" => "Duration",
        "模式" => "Mode",
        "状态" => "Status",
        "添加文件" => "Add files",
        "删除" => "Delete",
        "清空" => "Clear",
        "批量设置" => "Batch edit",
        "统一时长" => "Set duration",
        "统一计时方式" => "Set timer mode",
        "确定" => "OK",
        "取消" => "Cancel",
        "应用" => "Apply",
        "有未应用的更改" => "Unapplied changes",
        _ => value,
    }
}

#[derive(Clone)]
struct Row {
    key: String,
    label: String,
    kind: i32,
    value: String,
    checked: bool,
    options: Vec<String>,
    selected: i32,
    enabled: bool,
    button: String,
    row_height: i32,
}

impl Row {
    fn section(label: impl Into<String>) -> Self {
        Self::new("", label, 0)
    }
    fn check(key: impl Into<String>, label: impl Into<String>, checked: bool) -> Self {
        Self {
            checked,
            button: "启用".to_owned(),
            ..Self::new(key, label, 1)
        }
    }
    fn text(key: impl Into<String>, label: impl Into<String>, value: impl Into<String>) -> Self {
        Self {
            value: value.into(),
            ..Self::new(key, label, 2)
        }
    }
    fn combo(
        key: impl Into<String>,
        label: impl Into<String>,
        options: Vec<&'static str>,
        selected: i32,
    ) -> Self {
        Self {
            options: options.into_iter().map(str::to_owned).collect(),
            selected,
            ..Self::new(key, label, 3)
        }
    }
    fn combo_owned(
        key: impl Into<String>,
        label: impl Into<String>,
        options: Vec<String>,
        selected: i32,
    ) -> Self {
        Self {
            options,
            selected,
            ..Self::new(key, label, 3)
        }
    }
    fn action(key: impl Into<String>, label: impl Into<String>, button: impl Into<String>) -> Self {
        Self {
            button: button.into(),
            ..Self::new(key, label, 4)
        }
    }
    fn info(label: impl Into<String>, value: impl Into<String>) -> Self {
        Self {
            value: value.into(),
            enabled: false,
            row_height: 88,
            ..Self::new("", label, 5)
        }
    }
    fn new(key: impl Into<String>, label: impl Into<String>, kind: i32) -> Self {
        Self {
            key: key.into(),
            label: label.into(),
            kind,
            value: String::new(),
            checked: false,
            options: vec![],
            selected: 0,
            enabled: true,
            button: String::new(),
            row_height: 0,
        }
    }
    fn disabled(mut self) -> Self {
        self.enabled = false;
        self
    }
    fn available(mut self, enabled: bool) -> Self {
        self.enabled = enabled;
        self
    }
    fn tall(mut self, height: i32) -> Self {
        self.row_height = height;
        self
    }
    fn localized(self, lang: Language) -> SettingItem {
        let label = localize(lang, &self.label);
        SettingItem {
            key: self.key.into(),
            label: label.into(),
            kind: self.kind,
            value: self.value.into(),
            checked: self.checked,
            options: ModelRc::new(VecModel::from(
                self.options
                    .into_iter()
                    .map(|v| localize(lang, &v).into())
                    .collect::<Vec<SharedString>>(),
            )),
            selected: self.selected,
            enabled: self.enabled,
            button_text: localize(lang, &self.button).into(),
            row_height: self.row_height,
        }
    }
}

pub mod native {
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        IDNO, IDYES, MB_DEFBUTTON1, MB_ICONINFORMATION, MB_ICONWARNING, MB_OK, MB_YESNO,
        MB_YESNOCANCEL, MessageBoxW,
    };

    pub fn message(text: &str, title: &str, error: bool) {
        let text = wide(text);
        let title = wide(title);
        unsafe {
            MessageBoxW(
                std::ptr::null_mut(),
                text.as_ptr(),
                title.as_ptr(),
                MB_OK
                    | if error {
                        MB_ICONWARNING
                    } else {
                        MB_ICONINFORMATION
                    },
            );
        }
    }

    pub fn yes_no(text: &str, title: &str) -> bool {
        ask_yes_no(std::ptr::null_mut(), text, title)
    }

    pub fn yes_no_for_window(window: &slint::Window, text: &str, title: &str) -> bool {
        ask_yes_no(
            crate::window::hwnd(window).unwrap_or(std::ptr::null_mut()),
            text,
            title,
        )
    }

    fn ask_yes_no(owner: windows_sys::Win32::Foundation::HWND, text: &str, title: &str) -> bool {
        let text = wide(text);
        let title = wide(title);
        unsafe {
            MessageBoxW(
                owner,
                text.as_ptr(),
                title.as_ptr(),
                MB_YESNO | MB_ICONINFORMATION,
            ) == IDYES
        }
    }

    pub fn save_discard_cancel(text: &str) -> Choice {
        let text = wide(text);
        let title = wide("FlyPPTTimer");
        let result = unsafe {
            MessageBoxW(
                std::ptr::null_mut(),
                text.as_ptr(),
                title.as_ptr(),
                MB_YESNOCANCEL | MB_ICONWARNING | MB_DEFBUTTON1,
            )
        };
        match result {
            IDYES => Choice::Yes,
            IDNO => Choice::No,
            _ => Choice::Cancel,
        }
    }

    #[derive(PartialEq, Eq)]
    pub enum Choice {
        Yes,
        No,
        Cancel,
    }

    pub fn open_url(value: &str) {
        let _ = crate::remote::open_url(value);
    }

    fn wide(value: &str) -> Vec<u16> {
        value.encode_utf16().chain(Some(0)).collect()
    }
}

pub fn create(
    applied: Rc<RefCell<AppConfig>>,
    config_path: PathBuf,
    on_applied: Rc<dyn Fn(&AppConfig)>,
    _on_closed: Rc<dyn Fn()>,
    exit_on_close: bool,
    remote: Rc<RemoteServer>,
) -> Result<SettingsWindow, slint::PlatformError> {
    let window = SettingsWindow::new()?;
    let draft = Rc::new(RefCell::new(applied.borrow().clone()));
    let ui_language = Language::from_config(&applied.borrow().language);
    let page = Rc::new(RefCell::new(0usize));
    let selected_rule = Rc::new(RefCell::new(-1i32));
    let selected_rules = Rc::new(RefCell::new(BTreeSet::new()));
    let addresses = Rc::new(crate::remote::lan_addresses());
    refresh(
        &window,
        &draft.borrow(),
        0,
        -1,
        &selected_rules.borrow(),
        false,
        ui_language,
        &remote,
        &addresses,
    );

    {
        let weak = window.as_weak();
        let draft = draft.clone();
        let page = page.clone();
        let remote_for_nav = Rc::clone(&remote);
        let addresses_for_nav = Rc::clone(&addresses);
        let selected_rule = selected_rule.clone();
        let selected_rules = selected_rules.clone();
        window.on_navigate(move |next| {
            *page.borrow_mut() = next as usize;
            if let Some(w) = weak.upgrade() {
                refresh(
                    &w,
                    &draft.borrow(),
                    next as usize,
                    *selected_rule.borrow(),
                    &selected_rules.borrow(),
                    w.get_dirty(),
                    ui_language,
                    &remote_for_nav,
                    &addresses_for_nav,
                );
            }
        });
    }
    {
        let weak = window.as_weak();
        let draft = draft.clone();
        let page = page.clone();
        let remote_for_field = Rc::clone(&remote);
        let addresses_for_field = Rc::clone(&addresses);
        let selected_rule = selected_rule.clone();
        let selected_rules = selected_rules.clone();
        window.on_field_edited(move |index, value, checked, selected| {
            if let Some(w) = weak.upgrade() {
                let rows = rows_for(
                    &draft.borrow(),
                    ui_language,
                    *page.borrow(),
                    &remote_for_field,
                    &addresses_for_field,
                );
                if let Some(row) = rows.get(index as usize) {
                    update_field(
                        &mut draft.borrow_mut(),
                        &row.key,
                        value.as_str(),
                        checked,
                        selected,
                    );
                    if row.key == "appearance.scheme" {
                        refresh(
                            &w,
                            &draft.borrow(),
                            *page.borrow(),
                            *selected_rule.borrow(),
                            &selected_rules.borrow(),
                            true,
                            ui_language,
                            &remote_for_field,
                            &addresses_for_field,
                        );
                    } else {
                        w.set_dirty(true);
                    }
                }
            }
        });
    }
    {
        let weak = window.as_weak();
        let draft = draft.clone();
        let page = page.clone();
        let remote_for_action = Rc::clone(&remote);
        let addresses_for_action = Rc::clone(&addresses);
        let selected_rules = selected_rules.clone();
        let action_config_path = config_path.clone();
        window.on_field_action(move |index| {
            let rows = rows_for(
                &draft.borrow(),
                ui_language,
                *page.borrow(),
                &remote_for_action,
                &addresses_for_action,
            );
            if let Some(row) = rows.get(index as usize) {
                handle_action(&row.key, &mut draft.borrow_mut(), &action_config_path);
            }
            if let Some(w) = weak.upgrade() {
                refresh(
                    &w,
                    &draft.borrow(),
                    *page.borrow(),
                    w.get_selected_rule(),
                    &selected_rules.borrow(),
                    true,
                    ui_language,
                    &remote_for_action,
                    &addresses_for_action,
                );
            }
        });
    }
    {
        let weak = window.as_weak();
        let draft = draft.clone();
        let page = page.clone();
        let remote_for_rule = Rc::clone(&remote);
        let addresses_for_rule = Rc::clone(&addresses);
        let selected_rule = selected_rule.clone();
        let selected_rules = selected_rules.clone();
        window.on_rule_selected(move |index| {
            let index_usize = index as usize;
            let mut selected = selected_rules.borrow_mut();
            if !selected.insert(index_usize) {
                selected.remove(&index_usize);
            }
            *selected_rule.borrow_mut() = selected
                .iter()
                .next_back()
                .map_or(-1, |value| *value as i32);
            drop(selected);
            if let Some(w) = weak.upgrade() {
                refresh(
                    &w,
                    &draft.borrow(),
                    *page.borrow(),
                    *selected_rule.borrow(),
                    &selected_rules.borrow(),
                    w.get_dirty(),
                    ui_language,
                    &remote_for_rule,
                    &addresses_for_rule,
                );
            }
        });
    }
    {
        let weak = window.as_weak();
        let draft = draft.clone();
        window.on_rule_edited(move |index, value, checked, code| {
            if let Some(rule) = draft.borrow_mut().rules.get_mut(index as usize) {
                match code {
                    0 => rule.enabled = checked,
                    1 => rule.duration = value.into(),
                    2.. => {
                        rule.mode = if code == 2 {
                            TimerMode::Countdown
                        } else {
                            TimerMode::CountUp
                        }
                    }
                    _ => {}
                }
            }
            if let Some(w) = weak.upgrade() {
                w.set_dirty(true);
            }
        });
    }
    {
        let weak = window.as_weak();
        let draft = draft.clone();
        let page = page.clone();
        let remote_for_rule_action = Rc::clone(&remote);
        let addresses_for_rule_action = Rc::clone(&addresses);
        let selected_rule = selected_rule.clone();
        let selected_rules = selected_rules.clone();
        window.on_rule_action(move |action| {
            match action {
                0 => {
                    let paths = native_open_presentations();
                    if !paths.is_empty() {
                        let cfg = draft.borrow();
                        let duration = cfg.timer.default_duration.clone();
                        let mode = cfg.timer.mode;
                        drop(cfg);
                        let mut added = Vec::new();
                        for path in paths {
                            let full = path.to_string_lossy().to_string();
                            if draft
                                .borrow()
                                .rules
                                .iter()
                                .any(|r| r.file_path.eq_ignore_ascii_case(&full))
                            {
                                continue;
                            }
                            draft.borrow_mut().rules.push(FileRule {
                                file_name: path
                                    .file_name()
                                    .unwrap_or_default()
                                    .to_string_lossy()
                                    .to_string(),
                                file_path: full,
                                duration: duration.clone(),
                                mode,
                                enabled: true,
                                ..FileRule::default()
                            });
                            added.push(draft.borrow().rules.len() - 1);
                        }
                        selected_rules.borrow_mut().clear();
                        for index in added {
                            selected_rules.borrow_mut().insert(index);
                            *selected_rule.borrow_mut() = index as i32;
                        }
                    }
                }
                1 => {
                    let mut indices = selected_rules.borrow().iter().copied().collect::<Vec<_>>();
                    indices.sort_unstable_by(|a, b| b.cmp(a));
                    for index in indices {
                        if index < draft.borrow().rules.len() {
                            draft.borrow_mut().rules.remove(index);
                        }
                    }
                    selected_rules.borrow_mut().clear();
                    *selected_rule.borrow_mut() = -1;
                }
                2 => {
                    draft.borrow_mut().rules.clear();
                    selected_rules.borrow_mut().clear();
                    *selected_rule.borrow_mut() = -1;
                }
                3 => {
                    if let Some(w) = weak.upgrade() {
                        let c = draft.borrow();
                        w.set_batch_duration(c.timer.default_duration.clone().into());
                        w.set_batch_mode(c.timer.mode as i32);
                        w.set_batch_open(true);
                        return;
                    }
                }
                _ => {}
            }
            if let Some(w) = weak.upgrade() {
                refresh(
                    &w,
                    &draft.borrow(),
                    *page.borrow(),
                    *selected_rule.borrow(),
                    &selected_rules.borrow(),
                    true,
                    ui_language,
                    &remote_for_rule_action,
                    &addresses_for_rule_action,
                );
            }
        });
    }
    {
        let weak = window.as_weak();
        let draft = draft.clone();
        let page = page.clone();
        let remote_for_batch = Rc::clone(&remote);
        let addresses_for_batch = Rc::clone(&addresses);
        let selected_rule = selected_rule.clone();
        let selected_rules = selected_rules.clone();
        window.on_batch_confirm(move |duration, mode| {
            if crate::config::is_valid_duration(duration.as_str()) {
                for (index, rule) in draft
                    .borrow_mut()
                    .rules
                    .iter_mut()
                    .enumerate()
                    .filter(|(index, _)| selected_rules.borrow().contains(index))
                {
                    let _ = index;
                    rule.duration = duration.to_string();
                    rule.mode = if mode == 0 {
                        TimerMode::Countdown
                    } else {
                        TimerMode::CountUp
                    };
                }
                if let Some(w) = weak.upgrade() {
                    w.set_batch_open(false);
                    refresh(
                        &w,
                        &draft.borrow(),
                        *page.borrow(),
                        *selected_rule.borrow(),
                        &selected_rules.borrow(),
                        true,
                        ui_language,
                        &remote_for_batch,
                        &addresses_for_batch,
                    );
                }
            }
        });
    }

    let apply_now: ApplyCallback = {
        let draft = draft.clone();
        let applied = applied.clone();
        let on_applied = on_applied.clone();
        let path = config_path.clone();
        let page = page.clone();
        let remote_for_apply = Rc::clone(&remote);
        let addresses_for_apply = Rc::clone(&addresses);
        let selected_rule = selected_rule.clone();
        let selected_rules = selected_rules.clone();
        Rc::new(move |weak, close| {
            let lang = ui_language;
            normalize_before_save(&mut draft.borrow_mut());
            if let Err(message) = validate(&draft.borrow(), lang) {
                native::message(&message, "FlyPPTTimer", false);
                return;
            }
            if let Err(error) = draft.borrow().save(&path) {
                native::message(&error.to_string(), "FlyPPTTimer", false);
                return;
            }
            let language_changed = applied.borrow().language != draft.borrow().language;
            commit_draft(&mut applied.borrow_mut(), &draft.borrow());
            let updated = applied.borrow().clone();
            on_applied(&updated);
            let restart_now = language_changed
                && weak.upgrade().is_some_and(|window| native::yes_no_for_window(
                    window.window(),
                    t(
                        lang,
                        "界面语言已更改。是否立即重启 FlyPPTTimer 以应用更改？\r\n\r\n选择“否”将在下次启动时应用。",
                        "The display language has changed. Restart FlyPPTTimer now to apply it?\r\n\r\nChoose No to apply it the next time the app starts.",
                    ),
                    t(lang, "需要重启", "Restart required"),
                ));
            if restart_now {
                if let Ok(exe) = std::env::current_exe() {
                    let _ = std::process::Command::new(exe)
                        .arg("--restart-after")
                        .arg(std::process::id().to_string())
                        .arg("--show-settings")
                        .spawn();
                }
                let _ = slint::quit_event_loop();
                return;
            }
            if let Some(w) = weak.upgrade() {
                refresh(
                    &w,
                    &draft.borrow(),
                    *page.borrow(),
                    *selected_rule.borrow(),
                    &selected_rules.borrow(),
                    false,
                    ui_language,
                    &remote_for_apply,
                    &addresses_for_apply,
                );
                if close {
                    let _ = w.hide();
                    if exit_on_close {
                        let _ = slint::quit_event_loop();
                    }
                }
            }
        })
    };
    {
        let weak = window.as_weak();
        let f = apply_now.clone();
        window.on_apply(move || f(&weak, false));
    }
    {
        let weak = window.as_weak();
        let f = apply_now.clone();
        window.on_accept(move || f(&weak, true));
    }
    {
        let weak = window.as_weak();
        let draft = draft.clone();
        let applied = applied.clone();
        let page = page.clone();
        let remote_for_cancel = Rc::clone(&remote);
        let addresses_for_cancel = Rc::clone(&addresses);
        let selected_rule = selected_rule.clone();
        let selected_rules = selected_rules.clone();
        window.on_cancel(move || {
            if let Some(w)=weak.upgrade() {
                if !w.get_dirty() { let _=w.hide(); if exit_on_close { let _=slint::quit_event_loop(); } return; }
                let lang=ui_language;
                match native::save_discard_cancel(t(lang, "设置中有未应用的更改。是：应用并关闭；否：放弃更改；取消：继续编辑。", "Settings contain unapplied changes. Yes: apply and close; No: discard changes; Cancel: continue editing.")) {
                    native::Choice::Yes => w.invoke_accept(),
                    native::Choice::No => { *draft.borrow_mut()=applied.borrow().clone(); refresh(&w, &draft.borrow(), *page.borrow(), *selected_rule.borrow(), &selected_rules.borrow(), false, ui_language, &remote_for_cancel, &addresses_for_cancel); let _=w.hide(); if exit_on_close { let _=slint::quit_event_loop(); } },
                    native::Choice::Cancel => {}
                }
            }
        });
    }
    {
        let weak = window.as_weak();
        window.window().on_close_requested(move || {
            if let Some(w) = weak.upgrade() {
                w.invoke_cancel();
            }
            slint::CloseRequestResponse::KeepWindowShown
        });
    }
    Ok(window)
}

type ApplyCallback = Rc<dyn Fn(&slint::Weak<SettingsWindow>, bool)>;

#[allow(clippy::too_many_arguments)]
fn refresh(
    window: &SettingsWindow,
    config: &AppConfig,
    page: usize,
    selected_rule: i32,
    selected_rules: &BTreeSet<usize>,
    dirty: bool,
    lang: Language,
    remote: &RemoteServer,
    addresses: &[String],
) {
    let pages = [
        "时长设置",
        "行为设置",
        "外观与显示",
        "远程控制",
        "控制设置",
        "其他设置",
    ]
    .into_iter()
    .map(|x| localize(lang, x).into())
    .collect::<Vec<SharedString>>();
    window.set_pages(ModelRc::new(VecModel::from(pages)));
    window.set_settings_title(t(lang, "演讲计时器设置", "FlyPPTTimer Settings").into());
    let rows = rows_for(config, lang, page, remote, addresses)
        .into_iter()
        .map(|r| r.localized(lang))
        .collect::<Vec<_>>();
    window.set_items(ModelRc::new(VecModel::from(rows)));
    let rules = config
        .rules
        .iter()
        .enumerate()
        .map(|(index, r)| RuleItem {
            file_name: r.file_name.clone().into(),
            file_path: r.file_path.clone().into(),
            duration: r.duration.clone().into(),
            mode: r.mode as i32,
            enabled: r.enabled,
            selected: selected_rules.contains(&index),
        })
        .collect::<Vec<_>>();
    window.set_rules(ModelRc::new(VecModel::from(rules)));
    window.set_current_page(page as i32);
    window.set_selected_rule(selected_rule);
    window.set_selected_rule_count(selected_rules.len() as i32);
    window.set_dirty(dirty);
    window.set_dirty_text(localize(lang, "有未应用的更改").into());
    window.set_ok_text(localize(lang, "确定").into());
    window.set_cancel_text(localize(lang, "取消").into());
    window.set_apply_text(localize(lang, "应用").into());
    window.set_rule_add_text(localize(lang, "添加文件").into());
    window.set_rule_delete_text(localize(lang, "删除").into());
    window.set_rule_clear_text(localize(lang, "清空").into());
    window.set_rule_batch_text(localize(lang, "批量设置").into());
    window.set_rule_file_label(localize(lang, "文件").into());
    window.set_rule_path_label(localize(lang, "路径").into());
    window.set_rule_duration_label(localize(lang, "时长").into());
    window.set_rule_mode_label(localize(lang, "模式").into());
    window.set_rule_status_label(localize(lang, "状态").into());
    window.set_timer_modes(ModelRc::new(VecModel::from(vec![
        SharedString::from(localize(lang, "倒计时")),
        SharedString::from(localize(lang, "正计时")),
    ])));
    window.set_batch_title(localize(lang, "批量设置").into());
    window.set_batch_count_text(
        t(
            lang,
            &format!("已选择 {} 条规则", selected_rules.len()),
            &format!("Selected {} rules", selected_rules.len()),
        )
        .into(),
    );
    window.set_batch_duration_label(localize(lang, "统一时长").into());
    window.set_batch_mode_label(localize(lang, "统一计时方式").into());
}

fn rows_for(
    c: &AppConfig,
    lang: Language,
    page: usize,
    remote: &RemoteServer,
    addresses: &[String],
) -> Vec<Row> {
    match page {
        0 => vec![
            Row::section("基础计时"),
            Row::text(
                "timer.duration",
                "默认时长 HH:mm:ss",
                &c.timer.default_duration,
            ),
            Row::combo(
                "timer.mode",
                "计时模式",
                vec!["倒计时", "正计时"],
                c.timer.mode as i32,
            ),
            Row::combo(
                "timer.overtime",
                "到达预设时间后",
                vec!["停止计时", "继续显示超时"],
                c.timer.continue_overtime as i32,
            ),
            Row::combo(
                "timer.end_action",
                "时间到后的操作",
                vec!["仅提示", "黑屏并显示“时间到”", "退出放映"],
                c.timer.end_action as i32,
            ),
            Row::section("文件规则"),
        ],
        1 => behavior_rows(c),
        2 => appearance_rows(c),
        3 => remote_rows(c, lang, remote, addresses),
        4 => vec![
            Row::section("快捷键"),
            Row::combo_owned(
                "controls.start",
                "开始/暂停快捷键",
                function_keys(),
                function_key_index(&c.controls.start_pause_hotkey),
            ),
            Row::combo_owned(
                "controls.stop",
                "停止/重置快捷键",
                function_keys(),
                function_key_index(&c.controls.stop_reset_hotkey),
            ),
            Row::combo_owned(
                "controls.toggle",
                "显示/隐藏快捷键",
                function_keys(),
                function_key_index(&c.controls.toggle_window_hotkey),
            ),
            Row::section("窗口行为"),
            Row::check("controls.click", "鼠标穿透", c.controls.click_through),
            Row::check("controls.lock", "锁定窗口", c.controls.lock_position),
            Row::check("controls.tray", "托盘最小化", c.controls.minimize_to_tray),
            Row::combo(
                "controls.close",
                "关闭按钮行为",
                vec!["退出程序", "最小化到托盘"],
                c.controls.close_button_behavior as i32,
            ),
        ],
        5 => other_rows(c, lang),
        _ => vec![],
    }
}

fn function_keys() -> Vec<String> {
    (1..=12).map(|n| format!("F{n}")).collect()
}

fn function_key_index(value: &str) -> i32 {
    value
        .strip_prefix('F')
        .and_then(|v| v.parse::<i32>().ok())
        .filter(|v| (1..=12).contains(v))
        .map(|v| v - 1)
        .unwrap_or(0)
}

fn behavior_rows(c: &AppConfig) -> Vec<Row> {
    let mut rows = vec![
        Row::section("全局与启动"),
        Row::check(
            "behavior.stop_fullscreen",
            "退出全屏时停止计时",
            c.behavior.stop_when_leaving_fullscreen,
        ),
        Row::check(
            "behavior.auto_start",
            "全屏白名单自动开始",
            c.behavior.auto_start_on_fullscreen,
        ),
        Row::check(
            "behavior.reset_fullscreen",
            "退出全屏时重置",
            c.behavior.reset_when_leaving_fullscreen,
        ),
        Row::check(
            "behavior.flash_paused",
            "暂停时闪烁当前时间",
            c.behavior.flash_paused_time,
        ),
    ];
    rows.extend(prompt_rows("提示 1", "p1", &c.behavior.prompt1, "提示1"));
    rows.extend(prompt_rows("提示 2", "p2", &c.behavior.prompt2, "提示2"));
    rows.extend(prompt_rows(
        "计时结束",
        "end",
        &c.behavior.end_prompt,
        "计时结束",
    ));
    rows.push(Row::text(
        "appearance.timeout_text",
        "超时文字颜色",
        &c.appearance.timeout_text_color,
    ));
    rows.push(Row::text(
        "appearance.timeout_background",
        "超时背景颜色",
        &c.appearance.timeout_background_color,
    ));
    rows.push(Row::text(
        "appearance.overtime_prefix",
        "超时前缀",
        &c.appearance.overtime_prefix,
    ));
    rows
}

fn prompt_rows(
    section: &'static str,
    prefix: &str,
    prompt: &crate::config::PromptSettings,
    label: &'static str,
) -> Vec<Row> {
    let enabled_key = format!("{prefix}.enabled");
    let before_key = format!("{prefix}.before");
    let speak_key = format!("{prefix}.speak");
    let speak_label = format!("{label}语音播报");
    let sound_key = format!("{prefix}.sound");
    let sound_label = format!("{label}提示音");
    let choose_key = format!("{prefix}.choose");
    let choose_label = format!("选择{label}提示音");
    let clear_key = format!("{prefix}.clear");
    let clear_label = format!("清除{label}提示音");
    let flash_style_key = format!("{prefix}.flash_style");
    let flash_style_label = format!("{label}闪烁样式");
    let flash_on_key = format!("{prefix}.flash_on");
    let flash_on_label = format!("{label}闪现时长（毫秒）");
    let flash_off_key = format!("{prefix}.flash_off");
    let flash_off_label = format!("{label}隐藏时长（毫秒）");
    let flash_seconds_key = format!("{prefix}.flash_seconds");
    let flash_seconds_label = format!("{label}闪烁持续（秒）");
    vec![
        Row::section(section),
        Row::check(enabled_key, label, prompt.enabled),
        Row::text(
            before_key,
            "距离预设时间还剩（秒）",
            prompt.trigger_before_end_seconds.to_string(),
        ),
        Row::check(speak_key, speak_label, prompt.speak),
        Row::text(sound_key, sound_label, &prompt.sound_file),
        Row::action(choose_key, choose_label, "选择文件"),
        Row::action(clear_key, clear_label, "恢复默认"),
        Row::combo(
            flash_style_key,
            flash_style_label,
            vec!["无", "闪烁文字", "闪烁背景", "实线边框", "边框加背景"],
            flash_style_index(&prompt.flash_style),
        ),
        Row::text(flash_on_key, flash_on_label, prompt.flash_on_ms.to_string()),
        Row::text(
            flash_off_key,
            flash_off_label,
            prompt.flash_off_ms.to_string(),
        ),
        Row::text(
            flash_seconds_key,
            flash_seconds_label,
            prompt.flash_seconds.to_string(),
        ),
    ]
}

fn flash_style_index(style: &str) -> i32 {
    match style {
        "无" => 0,
        "闪烁文字" => 1,
        "闪烁背景" => 2,
        "实线边框" => 3,
        _ => 4,
    }
}

fn appearance_rows(c: &AppConfig) -> Vec<Row> {
    let monitors = display::monitors();
    let single_screens = screen_names(&monitors);
    let extended = display::extended_monitors(&monitors);
    let has_extended = !extended.is_empty();
    let extended_names: Vec<String> = extended.iter().map(|m| m.device_name.clone()).collect();
    let big_screen_selected = if has_extended {
        extended
            .iter()
            .position(|m| {
                m.device_name
                    .eq_ignore_ascii_case(&c.placement.big_screen_device_name)
            })
            .unwrap_or(0) as i32
    } else {
        0
    };
    let single_selected = single_screens
        .iter()
        .position(|name| {
            !c.placement.target_screen_device_name.is_empty()
                && name.eq_ignore_ascii_case(&c.placement.target_screen_device_name)
        })
        .unwrap_or(0) as i32;
    vec![
        Row::section("计时器窗口"),
        Row::check("placement.visible", "显示计时器窗口", c.placement.visible),
        Row::section("配色"),
        Row::combo(
            "appearance.scheme",
            "配色方案",
            vec![
                "医疗卫生（蓝白）",
                "教育培训（深蓝金）",
                "商务会议（石墨蓝）",
                "科技发布（深色青蓝）",
                "高对比警示（黑红）",
                "自定义",
            ],
            scheme_index(&c.appearance.color_scheme),
        ),
        Row::text("appearance.text", "字体颜色", &c.appearance.text_color),
        Row::text(
            "appearance.background",
            "背景颜色",
            &c.appearance.background_color,
        ),
        Row::text(
            "appearance.flash_background",
            "闪烁背景颜色",
            &c.appearance.flash_background_color,
        ),
        Row::section("窗口尺寸与字号"),
        Row::text("appearance.width", "宽", c.appearance.width.to_string()),
        Row::text("appearance.height", "高", c.appearance.height.to_string()),
        Row::text(
            "appearance.font",
            "字号",
            c.appearance.font_size.to_string(),
        ),
        Row::combo(
            "appearance.shape",
            "外观形状",
            vec!["直角矩形", "圆角矩形（小）", "圆角矩形（大）"],
            shape_index(&c.appearance.shape),
        ),
        Row::text(
            "appearance.opacity",
            "背景不透明度",
            c.appearance.background_opacity.to_string(),
        ),
        Row::section("多屏显示"),
        Row::check(
            "placement.all_screens",
            "所有屏幕同时显示",
            c.placement.show_on_all_screens,
        ),
        Row::combo_owned(
            "placement.target",
            "单屏显示屏幕",
            single_screens,
            single_selected,
        ),
        Row::section("大屏计时模式"),
        Row::check(
            "placement.big",
            "启用大屏计时器",
            c.placement.big_screen_enabled,
        )
        .available(has_extended),
        Row::combo_owned(
            "placement.bigscreen",
            "大屏显示屏幕",
            if has_extended {
                extended_names
            } else {
                vec!["需要扩展屏".to_owned()]
            },
            big_screen_selected,
        )
        .available(has_extended),
        Row::section("默认位置"),
        Row::combo(
            "placement.anchor",
            "默认点位",
            vec![
                "左上", "上中", "右上", "左中", "正中", "右中", "左下", "下中", "右下",
            ],
            c.placement.anchor as i32,
        ),
        Row::text(
            "placement.xoff",
            "水平微调百分比",
            c.placement.offset_x_percent.to_string(),
        ),
        Row::text(
            "placement.yoff",
            "垂直微调百分比",
            c.placement.offset_y_percent.to_string(),
        ),
        Row::action("placement.resetpos", "窗口位置", "重置计时窗口位置"),
    ]
}

fn scheme_index(scheme: &str) -> i32 {
    match scheme {
        "医疗卫生（蓝白）" => 0,
        "教育培训（深蓝金）" => 1,
        "商务会议（石墨蓝）" => 2,
        "科技发布（深色青蓝）" => 3,
        "高对比警示（黑红）" => 4,
        _ => 5,
    }
}

fn shape_index(shape: &str) -> i32 {
    match shape {
        "直角矩形" => 0,
        "圆角矩形（小）" => 1,
        _ => 2,
    }
}

fn screen_names(monitors: &[display::DisplayMonitor]) -> Vec<String> {
    let mut names: Vec<String> = monitors.iter().map(|m| m.device_name.clone()).collect();
    if !names.iter().any(|n| n.contains("主屏幕")) {
        names.insert(0, "主屏幕".to_owned());
    }
    names
}

fn remote_rows(
    c: &AppConfig,
    lang: Language,
    remote: &RemoteServer,
    addresses: &[String],
) -> Vec<Row> {
    let note = t(
        lang,
        "服务运行中端口会保持固定。修改端口或随机端口设置后，请点击“重启远程服务并应用端口”，或下次启动后生效。",
        "The port remains fixed while the service is running. After changing the port or random-port option, restart the remote service or restart the app.",
    );
    let firewall = t(
        lang,
        "手机无法访问时，常见原因是 Windows 防火墙、IP 选错、端口被占用、手机和电脑不在同一网络。不要关闭防火墙；只为当前程序和当前端口添加入站规则。",
        "If a phone cannot connect, common causes include Windows Firewall, a wrong IP address, an occupied port, or devices being on different networks. Do not disable the firewall; add an inbound rule only for this app and port.",
    );
    let info = remote.info();
    let status = if info.status.is_empty() {
        t(lang, "未启动", "Not started").to_owned()
    } else {
        info.status.clone()
    };
    let current_port = if info.current_port > 0 {
        info.current_port.to_string()
    } else {
        c.remote_control.port.to_string()
    };
    let client_count = info.connected_clients.to_string();
    let recommended = if info.running {
        addresses
            .first()
            .map(|address| {
                let url = format!(
                    "http://{address}:{}/?token={}",
                    current_port, c.remote_control.token
                );
                crate::remote::mask_token(&url)
            })
            .unwrap_or_else(|| {
                t(
                    lang,
                    "未检测到可供手机访问的局域网地址",
                    "No mobile-accessible LAN address found",
                )
                .to_owned()
            })
    } else {
        t(
            lang,
            "未检测到可供手机访问的局域网地址",
            "No mobile-accessible LAN address found",
        )
        .to_owned()
    };
    let all_addresses = if addresses.is_empty() {
        t(
            lang,
            "未检测到可供手机访问的局域网地址",
            "No mobile-accessible LAN address found",
        )
        .to_owned()
    } else {
        addresses.join("\n")
    };
    vec![
        Row::section("本地网页遥控"),
        Row::check("remote.enabled", "启用远程控制", c.remote_control.enabled),
        Row::text("", "当前服务状态", status).disabled(),
        Row::text("", "本次启动端口", current_port).disabled(),
        Row::text("remote.port", "下次服务端口", c.remote_control.port.to_string()),
        Row::check("remote.random", "使用随机端口", c.remote_control.use_random_port),
        Row::info("端口生效说明", note),
        Row::text("", "连接设备数量", client_count).disabled(),
        Row::section("访问地址"),
        Row::text("", "推荐访问地址", recommended).disabled(),
        Row::info("手机可用局域网地址", all_addresses).tall(150),
        Row::section("操作"),
        Row::action("remote.restart", "重启远程服务", "重启远程服务并应用端口"),
        Row::action("remote.token", "重新生成令牌", "重新生成令牌"),
        Row::action("remote.disconnect", "断开所有设备", "断开所有远程设备"),
        Row::action("remote.copy", "复制访问地址", "复制推荐 URL"),
        Row::action("remote.open", "打开本机控制页", "打开本机控制页"),
        Row::section("防火墙排障"),
        Row::info("防火墙说明", firewall).tall(110),
        Row::info(
            "修复命令",
            format!(
                "netsh advfirewall firewall add rule name=\"FlyPPTTimer Remote\" dir=in action=allow protocol=TCP localport={}",
                c.remote_control.port
            ),
        )
        .tall(92),
        Row::action("remote.copyfirewall", "复制修复命令", "复制防火墙修复命令"),
        Row::info(
            "二维码显示",
            t(
                lang,
                "可从计时器或托盘右键菜单打开远程控制二维码。",
                "Open the remote-control QR code from the timer or tray context menu.",
            ),
        ),
    ]
}

fn other_rows(c: &AppConfig, lang: Language) -> Vec<Row> {
    vec![
        Row::section("语言"),
        Row::combo(
            "language",
            "界面语言",
            vec!["跟随系统", "English", "简体中文"],
            match c.language.as_str() {
                "en" => 1,
                "zh-CN" => 2,
                _ => 0,
            },
        ),
        Row::info(
            "下次启动生效",
            t(
                lang,
                "更改界面语言后，请退出并重新启动 FlyPPTTimer。安装版和便携版均会记住此选项。",
                "After changing the display language, exit and restart FlyPPTTimer. Both installed and portable editions remember this setting.",
            ),
        ),
        Row::section("软件更新"),
        Row::check(
            "update.start",
            "启动时检测新版本",
            c.update.check_on_startup,
        ),
        Row::action("update.check", "手动检测", "立即检测新版本"),
        Row::section("配置管理"),
        Row::action("config.import", "配置导入", "配置导入"),
        Row::action("config.export", "配置导出", "配置导出"),
        Row::action("config.reset", "恢复默认", "恢复默认"),
        Row::section("文件位置"),
        Row::action("path.config", "配置文件", "打开配置文件位置"),
        Row::action("path.logs", "日志文件", "打开日志文件位置"),
        Row::section("关于 FlyPPTTimer"),
        Row::text("", "当前版本", format!("FlyPPTTimer {} · Windows x64", env!("CARGO_PKG_VERSION"))).disabled(),
        Row::info(
            "项目介绍",
            t(
                lang,
                "FlyPPTTimer 是一款面向演讲、教学和会议场景的 Windows 演示计时工具，提供倒计时、正计时、多显示器悬浮显示、演示文稿规则，以及手机或浏览器局域网远程控制功能。软件配置、规则和日志默认保存在本机；远程控制仅在本地网络中运行，不依赖云端账户，也不会主动上传演示文稿内容。",
                "FlyPPTTimer is a Windows presentation timer for talks, teaching, and meetings. It provides countdown and count-up modes, multi-display overlays, presentation-specific rules, and LAN remote control from a phone or browser. Configuration, rules, and logs stay on this computer. Remote control runs only on the local network, requires no cloud account, and never uploads presentation content.",
            ),
        )
        .tall(190),
        Row::section("作者与协作"),
        Row::info(
            "作者的话",
            t(
                lang,
                "FlyPPTTimer 由曹虎男发起并从零开发。作者毕业于南京大学医学院护理专业，目前就职于江苏省人民医院宿迁医院。在工作实践中发现了演讲计时、演示控制和台下远程调整的实际需求，因此将这个想法逐步实现为本项目。希望它能让大家的演讲、教学和会议更加从容，也欢迎有兴趣的朋友参与测试、提出建议或共同开发。祝大家使用愉快！",
                "FlyPPTTimer was created and built from scratch by Hunan Cao, a nursing graduate of Nanjing University Medical School who currently works at Suqian Hospital of Jiangsu Province Hospital. Practical needs around talk timing, presentation control, and off-stage adjustments inspired the project. Contributions, testing, and suggestions are welcome.",
            ),
        )
        .tall(210),
        Row::text("", "联系邮箱", "caohunan@smail.nju.edu.cn").disabled(),
        Row::action(
            "url.github",
            "GitHub 项目主页",
            "打开 GitHub（可能需要网络工具）",
        ),
        Row::action(
            "url.gitee",
            "Gitee 项目主页",
            "打开 Gitee（中国大陆可直接访问）",
        ),
        Row::action("url.mail", "联系作者", "发送邮件"),
    ]
}

fn normalize_before_save(c: &mut AppConfig) {
    for (p, text) in [
        (&mut c.behavior.prompt1, "时间即将结束"),
        (&mut c.behavior.prompt2, "时间即将结束"),
        (&mut c.behavior.end_prompt, "预设时间到"),
    ] {
        p.text = text.into();
        p.beep = false;
        p.play_sound = !p.sound_file.is_empty();
    }
    c.behavior.prompt1.trigger_before_end_seconds = c
        .behavior
        .prompt1
        .trigger_before_end_seconds
        .clamp(0, 99999);
    c.behavior.prompt2.trigger_before_end_seconds = c
        .behavior
        .prompt2
        .trigger_before_end_seconds
        .clamp(0, 99999);
    for p in [
        &mut c.behavior.prompt1,
        &mut c.behavior.prompt2,
        &mut c.behavior.end_prompt,
    ] {
        p.flash_on_ms = p.flash_on_ms.clamp(50, 5000);
        p.flash_off_ms = p.flash_off_ms.clamp(50, 5000);
        p.flash_seconds = p.flash_seconds.clamp(0, 120);
    }
    c.appearance.width = c.appearance.width.clamp(1, 2000);
    c.appearance.height = c.appearance.height.clamp(1, 1000);
    c.appearance.font_size = c.appearance.font_size.clamp(8.0, 180.0);
    c.appearance.background_opacity = c.appearance.background_opacity.clamp(0, 100);
    c.placement.offset_x_percent = c.placement.offset_x_percent.clamp(-50.0, 50.0);
    c.placement.offset_y_percent = c.placement.offset_y_percent.clamp(-50.0, 50.0);
    c.remote_control.port = c.remote_control.port.clamp(1, 65535);
    c.controls.hotkeys.remove("openSettings");
}

fn update_field(c: &mut AppConfig, key: &str, value: &str, checked: bool, selected: i32) {
    let int = || value.parse::<i32>().unwrap_or_default();
    let float = || value.parse::<f64>().unwrap_or_default();
    match key {
        "timer.duration" => c.timer.default_duration = value.into(),
        "timer.mode" => {
            c.timer.mode = if selected == 0 {
                TimerMode::Countdown
            } else {
                TimerMode::CountUp
            }
        }
        "timer.overtime" => c.timer.continue_overtime = selected == 1,
        "timer.end_action" => {
            c.timer.end_action = match selected {
                1 => TimerEndAction::BlackScreen,
                2 => TimerEndAction::ExitSlideShow,
                _ => TimerEndAction::None,
            }
        }
        "behavior.stop_fullscreen" => c.behavior.stop_when_leaving_fullscreen = checked,
        "behavior.auto_start" => c.behavior.auto_start_on_fullscreen = checked,
        "behavior.reset_fullscreen" => c.behavior.reset_when_leaving_fullscreen = checked,
        "behavior.flash_paused" => c.behavior.flash_paused_time = checked,
        "p1.enabled" => c.behavior.prompt1.enabled = checked,
        "p1.before" => c.behavior.prompt1.trigger_before_end_seconds = int(),
        "p1.speak" => c.behavior.prompt1.speak = checked,
        "p1.sound" => c.behavior.prompt1.sound_file = value.into(),
        "p1.flash_style" => {
            c.behavior.prompt1.flash_style = if selected == 4 {
                "边框+背景".into()
            } else {
                value.into()
            };
            c.behavior.prompt1.flash_text = c.behavior.prompt1.flash_style.contains("文字");
            c.behavior.prompt1.flash_background = c.behavior.prompt1.flash_style != "无";
        }
        "p1.flash_on" => c.behavior.prompt1.flash_on_ms = int(),
        "p1.flash_off" => c.behavior.prompt1.flash_off_ms = int(),
        "p1.flash_seconds" => c.behavior.prompt1.flash_seconds = int(),
        "p2.enabled" => c.behavior.prompt2.enabled = checked,
        "p2.before" => c.behavior.prompt2.trigger_before_end_seconds = int(),
        "p2.speak" => c.behavior.prompt2.speak = checked,
        "p2.sound" => c.behavior.prompt2.sound_file = value.into(),
        "p2.flash_style" => {
            c.behavior.prompt2.flash_style = if selected == 4 {
                "边框+背景".into()
            } else {
                value.into()
            };
            c.behavior.prompt2.flash_text = c.behavior.prompt2.flash_style.contains("文字");
            c.behavior.prompt2.flash_background = c.behavior.prompt2.flash_style != "无";
        }
        "p2.flash_on" => c.behavior.prompt2.flash_on_ms = int(),
        "p2.flash_off" => c.behavior.prompt2.flash_off_ms = int(),
        "p2.flash_seconds" => c.behavior.prompt2.flash_seconds = int(),
        "end.enabled" => c.behavior.end_prompt.enabled = checked,
        "end.speak" => c.behavior.end_prompt.speak = checked,
        "end.sound" => c.behavior.end_prompt.sound_file = value.into(),
        "end.flash_style" => {
            c.behavior.end_prompt.flash_style = if selected == 4 {
                "边框+背景".into()
            } else {
                value.into()
            };
            c.behavior.end_prompt.flash_text = c.behavior.end_prompt.flash_style.contains("文字");
            c.behavior.end_prompt.flash_background = c.behavior.end_prompt.flash_style != "无";
        }
        "end.flash_on" => c.behavior.end_prompt.flash_on_ms = int(),
        "end.flash_off" => c.behavior.end_prompt.flash_off_ms = int(),
        "end.flash_seconds" => c.behavior.end_prompt.flash_seconds = int(),
        "appearance.text" => c.appearance.text_color = value.into(),
        "appearance.background" => c.appearance.background_color = value.into(),
        "appearance.flash_background" => c.appearance.flash_background_color = value.into(),
        "appearance.timeout_text" => c.appearance.timeout_text_color = value.into(),
        "appearance.timeout_background" => c.appearance.timeout_background_color = value.into(),
        "appearance.overtime_prefix" => c.appearance.overtime_prefix = value.into(),
        "appearance.width" => c.appearance.width = int(),
        "appearance.height" => c.appearance.height = int(),
        "appearance.font" => c.appearance.font_size = float() as f32,
        "appearance.shape" => {
            c.appearance.shape = value.into();
        }
        "appearance.opacity" => c.appearance.background_opacity = int(),
        "appearance.scheme" => {
            c.appearance.color_scheme = value.into();
            apply_scheme(c, value);
        }
        "placement.visible" => c.placement.visible = checked,
        "placement.all_screens" => c.placement.show_on_all_screens = checked,
        "placement.target" => {
            if value != "主屏幕" {
                c.placement.target_screen_device_name = value.into();
            }
        }
        "placement.big" => c.placement.big_screen_enabled = checked,
        "placement.bigscreen" => c.placement.big_screen_device_name = value.into(),
        "placement.anchor" => {
            c.placement.anchor = match selected {
                0 => OverlayAnchor::TopLeft,
                1 => OverlayAnchor::TopCenter,
                2 => OverlayAnchor::TopRight,
                3 => OverlayAnchor::MiddleLeft,
                4 => OverlayAnchor::Center,
                5 => OverlayAnchor::MiddleRight,
                6 => OverlayAnchor::BottomLeft,
                7 => OverlayAnchor::BottomCenter,
                _ => OverlayAnchor::BottomRight,
            };
        }
        "placement.xoff" => c.placement.offset_x_percent = float(),
        "placement.yoff" => c.placement.offset_y_percent = float(),
        "controls.start" => c.controls.start_pause_hotkey = value.into(),
        "controls.stop" => c.controls.stop_reset_hotkey = value.into(),
        "controls.toggle" => c.controls.toggle_window_hotkey = value.into(),
        "controls.click" => c.controls.click_through = checked,
        "controls.lock" => c.controls.lock_position = checked,
        "controls.tray" => c.controls.minimize_to_tray = checked,
        "controls.close" => {
            c.controls.close_button_behavior = if selected == 0 {
                CloseButtonBehavior::Exit
            } else {
                CloseButtonBehavior::MinimizeToTray
            }
        }
        "remote.enabled" => c.remote_control.enabled = checked,
        "remote.port" => {
            if let Ok(port) = value.parse::<u16>() {
                c.remote_control.port = port;
            }
        }
        "remote.random" => c.remote_control.use_random_port = checked,
        "language" => {
            c.language = match selected {
                1 => "en".to_owned(),
                2 => "zh-CN".to_owned(),
                _ => "auto".to_owned(),
            }
        }
        "update.start" => c.update.check_on_startup = checked,
        _ => {}
    }
}

fn apply_scheme(c: &mut AppConfig, scheme: &str) {
    let (text, background, flash) = match scheme {
        "教育培训（深蓝金）" => ("#1E3A5F", "#F5E9C8", "#D4A017"),
        "商务会议（石墨蓝）" => ("#2F3B4C", "#E8ECEF", "#5B7A9D"),
        "科技发布（深色青蓝）" => ("#0F4C5C", "#0B1B22", "#1B8FA8"),
        "高对比警示（黑红）" => ("#FFFFFF", "#111111", "#D32F2F"),
        _ => ("#0B3A66", "#F3F8FC", "#4EA3D8"),
    };
    c.appearance.text_color = text.into();
    c.appearance.background_color = background.into();
    c.appearance.flash_background_color = flash.into();
}

fn handle_action(key: &str, c: &mut AppConfig, config_path: &std::path::Path) {
    match key {
        "p1.choose" => {
            if let Some(path) = native_choose_sound() {
                c.behavior.prompt1.sound_file = path;
                c.behavior.prompt1.play_sound = true;
            }
        }
        "p1.clear" => {
            c.behavior.prompt1.sound_file.clear();
            c.behavior.prompt1.play_sound = false;
        }
        "p2.choose" => {
            if let Some(path) = native_choose_sound() {
                c.behavior.prompt2.sound_file = path;
                c.behavior.prompt2.play_sound = true;
            }
        }
        "p2.clear" => {
            c.behavior.prompt2.sound_file.clear();
            c.behavior.prompt2.play_sound = false;
        }
        "end.choose" => {
            if let Some(path) = native_choose_sound() {
                c.behavior.end_prompt.sound_file = path;
                c.behavior.end_prompt.play_sound = true;
            }
        }
        "end.clear" => {
            c.behavior.end_prompt.sound_file.clear();
            c.behavior.end_prompt.play_sound = false;
        }
        "placement.resetpos" => {
            c.placement.has_custom_placement = false;
        }
        "remote.token" => {
            let token = crate::remote::generate_token();
            c.remote_control.token = token;
        }
        "config.import" => native_import_config(c, config_path),
        "config.export" => native_export_config(c),
        "config.reset" => {
            *c = AppConfig::default();
        }
        "path.config" => {
            let _ = native_open_path(config_path);
        }
        "path.logs" => {
            let logs = crate::log::log_directory();
            if let Some(dir) = logs {
                let _ = native_open_path(&dir);
            }
        }
        "url.github" => {
            let _ = crate::remote::open_url("https://github.com/Hona-Cao/FlyPPTTimer");
        }
        "url.gitee" => {
            let _ = crate::remote::open_url("https://gitee.com/hona-cao/fly-ppttimer");
        }
        "url.mail" => {
            let _ = crate::remote::open_url("mailto:caohunan@smail.nju.edu.cn");
        }
        "update.check" => {
            // Handled by app layer through DesktopEvent::CheckUpdate; here just refresh.
        }
        _ => {}
    }
}

fn validate(c: &AppConfig, lang: Language) -> Result<(), String> {
    if !crate::config::is_valid_duration(&c.timer.default_duration) {
        return Err(t(
            lang,
            "默认时长必须是 HH:mm:ss 格式。",
            "Default duration must use HH:mm:ss format.",
        )
        .to_owned());
    }
    for rule in &c.rules {
        if !crate::config::is_valid_duration(&rule.duration) {
            return Err(t(
                lang,
                "文件规则“文件”的计时时长无效。",
                "A presentation rule has an invalid duration.",
            )
            .to_owned());
        }
    }
    Ok(())
}

fn commit_draft(applied: &mut AppConfig, draft: &AppConfig) {
    *applied = draft.clone();
}

fn native_open_presentations() -> Vec<PathBuf> {
    use windows_sys::Win32::UI::Controls::Dialogs::{
        GetOpenFileNameW, OFN_ALLOWMULTISELECT, OFN_EXPLORER, OFN_FILEMUSTEXIST, OFN_HIDEREADONLY,
        OPENFILENAMEW,
    };
    let mut filter =
        wide("演示文稿 (*.ppt;*.pptx;*.pptm)|*.ppt;*.pptx;*.pptm|所有文件 (*.*)|*.*\0");
    let mut file = [0u16; 32768];
    let mut ofn = unsafe { std::mem::zeroed::<OPENFILENAMEW>() };
    ofn.lStructSize = std::mem::size_of::<OPENFILENAMEW>() as u32;
    ofn.lpstrFilter = filter.as_mut_ptr();
    ofn.lpstrFile = file.as_mut_ptr();
    ofn.nMaxFile = file.len() as u32;
    ofn.Flags = OFN_EXPLORER | OFN_ALLOWMULTISELECT | OFN_FILEMUSTEXIST | OFN_HIDEREADONLY;
    if unsafe { GetOpenFileNameW(&mut ofn) } == 0 {
        return Vec::new();
    }
    let mut parts = Vec::new();
    let mut start = 0;
    for index in 0..file.len() {
        if file[index] == 0 {
            if index == start {
                break;
            }
            parts.push(String::from_utf16_lossy(&file[start..index]));
            start = index + 1;
        }
    }
    if parts.len() <= 1 {
        return parts.into_iter().map(PathBuf::from).collect();
    }
    let directory = PathBuf::from(&parts[0]);
    parts
        .into_iter()
        .skip(1)
        .map(|name| directory.join(name))
        .collect()
}
fn native_choose_sound() -> Option<String> {
    use windows_sys::Win32::UI::Controls::Dialogs::{
        GetOpenFileNameW, OFN_EXPLORER, OFN_FILEMUSTEXIST, OFN_HIDEREADONLY, OPENFILENAMEW,
    };
    let mut filter = wide("音频文件 (*.mp3;*.wav;*.wma;*.m4a)|*.mp3;*.wav;*.wma;*.m4a\0");
    let mut file = [0u16; 2048];
    let mut ofn = unsafe { std::mem::zeroed::<OPENFILENAMEW>() };
    ofn.lStructSize = std::mem::size_of::<OPENFILENAMEW>() as u32;
    ofn.lpstrFilter = filter.as_mut_ptr();
    ofn.lpstrFile = file.as_mut_ptr();
    ofn.nMaxFile = file.len() as u32;
    ofn.Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_HIDEREADONLY;
    let ok = unsafe { GetOpenFileNameW(&mut ofn) };
    if ok != 0 {
        let end = file.iter().position(|c| *c == 0).unwrap_or(file.len());
        Some(String::from_utf16_lossy(&file[..end]))
    } else {
        None
    }
}

fn native_import_config(c: &mut AppConfig, config_path: &std::path::Path) {
    use windows_sys::Win32::UI::Controls::Dialogs::{
        GetOpenFileNameW, OFN_EXPLORER, OFN_FILEMUSTEXIST, OFN_HIDEREADONLY, OPENFILENAMEW,
    };
    let mut filter = wide("配置文件 (*.json)|*.json|所有文件 (*.*)|*.*\0");
    let mut file = [0u16; 2048];
    let mut ofn = unsafe { std::mem::zeroed::<OPENFILENAMEW>() };
    ofn.lStructSize = std::mem::size_of::<OPENFILENAMEW>() as u32;
    ofn.lpstrFilter = filter.as_mut_ptr();
    ofn.lpstrFile = file.as_mut_ptr();
    ofn.nMaxFile = file.len() as u32;
    ofn.Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_HIDEREADONLY;
    if unsafe { GetOpenFileNameW(&mut ofn) } == 0 {
        return;
    }
    let end = file.iter().position(|c| *c == 0).unwrap_or(file.len());
    let path = PathBuf::from(String::from_utf16_lossy(&file[..end]));
    match AppConfig::load(&path) {
        Ok(imported) => {
            *c = imported;
            let _ = c.save(config_path);
        }
        Err(_) => native::message("配置导入失败：文件格式无效。", "FlyPPTTimer", true),
    }
}

fn native_export_config(c: &AppConfig) {
    if let Ok(json) = serde_json::to_string_pretty(c)
        && let Some(path) = native_save_path()
    {
        let _ = fs::write(path, json);
    }
}

fn native_save_path() -> Option<PathBuf> {
    use windows_sys::Win32::UI::Controls::Dialogs::{
        GetSaveFileNameW, OFN_EXPLORER, OFN_HIDEREADONLY, OFN_OVERWRITEPROMPT, OPENFILENAMEW,
    };
    let mut filter = wide("配置文件 (*.json)|*.json\0");
    let mut file = [0u16; 2048];
    for (i, ch) in "FlyPPTTimer.config.json".encode_utf16().enumerate() {
        file[i] = ch;
    }
    let mut ofn = unsafe { std::mem::zeroed::<OPENFILENAMEW>() };
    ofn.lStructSize = std::mem::size_of::<OPENFILENAMEW>() as u32;
    ofn.lpstrFilter = filter.as_mut_ptr();
    ofn.lpstrFile = file.as_mut_ptr();
    ofn.nMaxFile = file.len() as u32;
    ofn.Flags = OFN_EXPLORER | OFN_HIDEREADONLY | OFN_OVERWRITEPROMPT;
    if unsafe { GetSaveFileNameW(&mut ofn) } == 0 {
        return None;
    }
    let end = file.iter().position(|c| *c == 0).unwrap_or(file.len());
    Some(PathBuf::from(String::from_utf16_lossy(&file[..end])))
}

fn native_open_path(path: &std::path::Path) -> std::io::Result<()> {
    let _ = crate::remote::open_url(&path.display().to_string());
    Ok(())
}

fn wide(value: &str) -> Vec<u16> {
    value.encode_utf16().chain(Some(0)).collect()
}
