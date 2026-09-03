use std::{
    collections::BTreeMap,
    fs::{self, File},
    io::{self, Write},
    path::Path,
    time::Duration,
};

use serde::{Deserialize, Serialize};
use serde_repr::{Deserialize_repr, Serialize_repr};

pub fn ui_is_english(language: &str) -> bool {
    if language.eq_ignore_ascii_case("en") || language.eq_ignore_ascii_case("en-US") {
        return true;
    }
    if language.eq_ignore_ascii_case("zh-CN") {
        return false;
    }
    #[cfg(windows)]
    unsafe {
        windows_sys::Win32::Globalization::GetUserDefaultUILanguage() & 0x03ff != 0x0004
    }
    #[cfg(not(windows))]
    {
        true
    }
}

pub const V1_CONFIG_VERSION: &str = env!("CARGO_PKG_VERSION");
const DEFAULT_DURATION_SECONDS: u64 = 8 * 60;

#[derive(Debug)]
pub enum ConfigError {
    Io(io::Error),
    Json(serde_json::Error),
}

impl std::fmt::Display for ConfigError {
    fn fmt(&self, formatter: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::Io(error) => write!(formatter, "configuration I/O failed: {error}"),
            Self::Json(error) => write!(formatter, "configuration JSON is invalid: {error}"),
        }
    }
}

impl std::error::Error for ConfigError {}

impl From<io::Error> for ConfigError {
    fn from(value: io::Error) -> Self {
        Self::Io(value)
    }
}

impl From<serde_json::Error> for ConfigError {
    fn from(value: serde_json::Error) -> Self {
        Self::Json(value)
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(default, rename_all = "PascalCase")]
pub struct AppConfig {
    pub version: String,
    pub language: String,
    pub update: UpdateSettings,
    pub timer: TimerSettings,
    pub behavior: BehaviorSettings,
    pub appearance: AppearanceSettings,
    pub controls: ControlSettings,
    pub remote_control: RemoteControlSettings,
    pub placement: WindowPlacement,
    pub rules: Vec<FileRule>,
}

impl Default for AppConfig {
    fn default() -> Self {
        Self {
            version: V1_CONFIG_VERSION.to_owned(),
            language: "auto".to_owned(),
            update: UpdateSettings::default(),
            timer: TimerSettings::default(),
            behavior: BehaviorSettings::default(),
            appearance: AppearanceSettings::default(),
            controls: ControlSettings::default(),
            remote_control: RemoteControlSettings::default(),
            placement: WindowPlacement::default(),
            rules: Vec::new(),
        }
    }
}

impl AppConfig {
    pub fn from_json(json: &str) -> Result<Self, ConfigError> {
        Ok(serde_json::from_str(json)?)
    }

    pub fn load(path: impl AsRef<Path>) -> Result<Self, ConfigError> {
        let json = fs::read_to_string(path)?;
        Self::from_json(&json)
    }

    pub fn save(&self, path: impl AsRef<Path>) -> Result<(), ConfigError> {
        let path = path.as_ref();
        if let Some(parent) = path.parent() {
            fs::create_dir_all(parent)?;
        }

        let mut saved = self.clone();
        saved.version = V1_CONFIG_VERSION.to_owned();
        let json = serde_json::to_vec_pretty(&saved)?;
        serde_json::from_slice::<AppConfig>(&json)?;

        let temporary_path = path.with_extension("json.tmp");
        let mut temporary = File::create(&temporary_path)?;
        temporary.write_all(&json)?;
        temporary.write_all(b"\n")?;
        temporary.sync_all()?;

        replace_file(&temporary_path, path)?;
        Ok(())
    }
}

#[cfg(windows)]
fn replace_file(temporary_path: &Path, destination: &Path) -> io::Result<()> {
    use std::os::windows::ffi::OsStrExt;
    use windows_sys::Win32::Storage::FileSystem::{
        MOVEFILE_REPLACE_EXISTING, MOVEFILE_WRITE_THROUGH, MoveFileExW,
    };

    let temporary: Vec<u16> = temporary_path
        .as_os_str()
        .encode_wide()
        .chain(Some(0))
        .collect();
    let destination: Vec<u16> = destination
        .as_os_str()
        .encode_wide()
        .chain(Some(0))
        .collect();
    let replaced = unsafe {
        MoveFileExW(
            temporary.as_ptr(),
            destination.as_ptr(),
            MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH,
        )
    };
    if replaced == 0 {
        Err(io::Error::last_os_error())
    } else {
        Ok(())
    }
}

#[cfg(not(windows))]
fn replace_file(temporary_path: &Path, destination: &Path) -> io::Result<()> {
    if destination.exists() {
        fs::remove_file(destination)?;
    }
    fs::rename(temporary_path, destination)
}

#[derive(Debug, Clone, Default, Serialize, Deserialize)]
#[serde(default, rename_all = "PascalCase")]
pub struct UpdateSettings {
    pub check_on_startup: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(default, rename_all = "PascalCase")]
pub struct TimerSettings {
    pub default_duration: String,
    pub mode: TimerMode,
    pub enable_per_slide_timer: bool,
    pub continue_overtime: bool,
    pub end_action: TimerEndAction,
}

impl Default for TimerSettings {
    fn default() -> Self {
        Self {
            default_duration: "00:08:00".to_owned(),
            mode: TimerMode::Countdown,
            enable_per_slide_timer: false,
            continue_overtime: true,
            end_action: TimerEndAction::None,
        }
    }
}

impl TimerSettings {
    pub fn duration(&self) -> Duration {
        parse_duration(&self.default_duration)
            .unwrap_or_else(|| Duration::from_secs(DEFAULT_DURATION_SECONDS))
    }

    pub fn effective_continue_overtime(&self) -> bool {
        self.end_action == TimerEndAction::None && self.continue_overtime
    }
}

#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize_repr, Deserialize_repr)]
#[repr(u8)]
pub enum TimerMode {
    #[default]
    Countdown = 0,
    CountUp = 1,
}

#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize_repr, Deserialize_repr)]
#[repr(u8)]
pub enum TimerEndAction {
    #[default]
    None = 0,
    BlackScreen = 1,
    ExitSlideShow = 2,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(default, rename_all = "PascalCase")]
pub struct BehaviorSettings {
    pub auto_start_on_fullscreen: bool,
    pub stop_when_leaving_fullscreen: bool,
    pub reset_when_leaving_fullscreen: bool,
    pub flash_on_pause_resume: bool,
    pub flash_paused_time: bool,
    pub prompt1: PromptSettings,
    pub prompt2: PromptSettings,
    pub end_prompt: PromptSettings,
    pub fullscreen_process_whitelist: Vec<String>,
}

impl Default for BehaviorSettings {
    fn default() -> Self {
        Self {
            auto_start_on_fullscreen: true,
            stop_when_leaving_fullscreen: true,
            reset_when_leaving_fullscreen: true,
            flash_on_pause_resume: true,
            flash_paused_time: false,
            prompt1: PromptSettings {
                enabled: true,
                trigger_before_end_seconds: 120,
                text: "时间即将结束".to_owned(),
                speak: true,
                flash_background: true,
                ..PromptSettings::default()
            },
            prompt2: PromptSettings {
                enabled: false,
                trigger_before_end_seconds: 30,
                text: "时间即将结束".to_owned(),
                speak: true,
                flash_background: true,
                ..PromptSettings::default()
            },
            end_prompt: PromptSettings {
                enabled: true,
                text: "预设时间到".to_owned(),
                speak: true,
                flash_background: true,
                flash_seconds: 8,
                ..PromptSettings::default()
            },
            fullscreen_process_whitelist: vec![
                "POWERPNT.EXE".to_owned(),
                "WPSOffice.exe".to_owned(),
                "wpp.exe".to_owned(),
                "Acrobat.exe".to_owned(),
                "AcroRd32.exe".to_owned(),
                "chrome.exe".to_owned(),
                "msedge.exe".to_owned(),
            ],
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(default, rename_all = "PascalCase")]
pub struct PromptSettings {
    pub enabled: bool,
    pub trigger_before_end_seconds: i32,
    pub text: String,
    pub speak: bool,
    pub beep: bool,
    pub flash_text: bool,
    pub flash_background: bool,
    pub play_sound: bool,
    pub sound_file: String,
    pub flash_style: String,
    pub flash_on_ms: i32,
    pub flash_off_ms: i32,
    pub flash_seconds: i32,
}

impl Default for PromptSettings {
    fn default() -> Self {
        Self {
            enabled: false,
            trigger_before_end_seconds: 0,
            text: String::new(),
            speak: false,
            beep: false,
            flash_text: false,
            flash_background: false,
            play_sound: false,
            sound_file: String::new(),
            flash_style: "闪烁背景".to_owned(),
            flash_on_ms: 350,
            flash_off_ms: 350,
            flash_seconds: 3,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(default, rename_all = "PascalCase")]
pub struct AppearanceSettings {
    pub color_scheme: String,
    pub font_family: String,
    pub font_size: f32,
    pub font_style: String,
    pub text_color: String,
    pub background_color: String,
    pub timeout_text_color: String,
    pub timeout_background_color: String,
    pub flash_background_color: String,
    pub width: i32,
    pub height: i32,
    pub background_opacity: i32,
    pub text_opacity: i32,
    pub shape: String,
    pub flash_style: String,
    pub flash_on_ms: i32,
    pub flash_off_ms: i32,
    pub overtime_prefix: String,
    pub borderless: bool,
    pub always_on_top: bool,
}

impl Default for AppearanceSettings {
    fn default() -> Self {
        Self {
            color_scheme: "医疗卫生（蓝白）".to_owned(),
            font_family: "Microsoft YaHei UI".to_owned(),
            font_size: 18.0,
            font_style: "Bold".to_owned(),
            text_color: "#0B3A66".to_owned(),
            background_color: "#F3F8FC".to_owned(),
            timeout_text_color: "#FFFFFF".to_owned(),
            timeout_background_color: "#B00020".to_owned(),
            flash_background_color: "#4EA3D8".to_owned(),
            width: 100,
            height: 35,
            background_opacity: 88,
            text_opacity: 100,
            shape: "圆角矩形（小）".to_owned(),
            flash_style: "闪烁背景".to_owned(),
            flash_on_ms: 350,
            flash_off_ms: 350,
            overtime_prefix: "-".to_owned(),
            borderless: true,
            always_on_top: true,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(default, rename_all = "PascalCase")]
pub struct ControlSettings {
    pub start_pause_hotkey: String,
    pub stop_reset_hotkey: String,
    pub toggle_window_hotkey: String,
    pub hotkeys: BTreeMap<String, String>,
    pub click_through: bool,
    pub lock_position: bool,
    pub minimize_to_tray: bool,
    pub close_button_behavior: CloseButtonBehavior,
}

impl Default for ControlSettings {
    fn default() -> Self {
        Self {
            start_pause_hotkey: "F3".to_owned(),
            stop_reset_hotkey: "F4".to_owned(),
            toggle_window_hotkey: "F5".to_owned(),
            hotkeys: default_hotkeys(),
            click_through: false,
            lock_position: false,
            minimize_to_tray: true,
            close_button_behavior: CloseButtonBehavior::MinimizeToTray,
        }
    }
}

fn default_hotkeys() -> BTreeMap<String, String> {
    [
        ("startPause", "F3"),
        ("start", ""),
        ("pause", ""),
        ("resume", ""),
        ("stopReset", "F4"),
        ("stop", ""),
        ("reset", ""),
        ("toggleWindow", "F5"),
        ("showWindow", ""),
        ("hideWindow", ""),
        ("flash", "F7"),
        ("toggleMute", "F8"),
        ("toggleMode", ""),
        ("addMinute", "Ctrl+Alt+Up"),
        ("subtractMinute", "Ctrl+Alt+Down"),
        ("preset3", "Ctrl+Alt+1"),
        ("preset5", "Ctrl+Alt+2"),
        ("preset8", "Ctrl+Alt+3"),
        ("preset10", "Ctrl+Alt+4"),
        ("preset15", "Ctrl+Alt+5"),
    ]
    .into_iter()
    .map(|(key, value)| (key.to_owned(), value.to_owned()))
    .collect()
}

#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize_repr, Deserialize_repr)]
#[repr(u8)]
pub enum CloseButtonBehavior {
    Exit = 0,
    #[default]
    MinimizeToTray = 1,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(default, rename_all = "PascalCase")]
pub struct RemoteControlSettings {
    pub enabled: bool,
    pub use_random_port: bool,
    pub port: u16,
    pub token: String,
    pub window: RemoteWindowPlacement,
}

impl Default for RemoteControlSettings {
    fn default() -> Self {
        Self {
            enabled: true,
            use_random_port: false,
            port: 4080,
            token: String::new(),
            window: RemoteWindowPlacement::default(),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(default, rename_all = "PascalCase")]
pub struct RemoteWindowPlacement {
    pub has_value: bool,
    pub screen_device_name: String,
    pub left_ratio: f64,
    pub top_ratio: f64,
    pub width_dip: i32,
    pub height_dip: i32,
    pub maximized: bool,
}

impl Default for RemoteWindowPlacement {
    fn default() -> Self {
        Self {
            has_value: false,
            screen_device_name: String::new(),
            left_ratio: 0.0,
            top_ratio: 0.0,
            width_dip: 700,
            height_dip: 510,
            maximized: false,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(default, rename_all = "PascalCase")]
pub struct WindowPlacement {
    pub visible: bool,
    pub big_screen_enabled: bool,
    pub big_screen_device_name: String,
    pub show_on_all_screens: bool,
    pub target_screen_device_name: String,
    pub anchor: OverlayAnchor,
    pub offset_x_percent: f64,
    pub offset_y_percent: f64,
    pub x: i32,
    pub y: i32,
    pub screen_device_name: String,
    pub has_custom_placement: bool,
}

impl Default for WindowPlacement {
    fn default() -> Self {
        Self {
            visible: true,
            big_screen_enabled: false,
            big_screen_device_name: String::new(),
            show_on_all_screens: true,
            target_screen_device_name: String::new(),
            anchor: OverlayAnchor::TopCenter,
            offset_x_percent: 0.0,
            offset_y_percent: 0.5,
            x: 80,
            y: 80,
            screen_device_name: String::new(),
            has_custom_placement: false,
        }
    }
}

#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize_repr, Deserialize_repr)]
#[repr(u8)]
pub enum OverlayAnchor {
    TopLeft = 0,
    #[default]
    TopCenter = 1,
    TopRight = 2,
    MiddleLeft = 3,
    Center = 4,
    MiddleRight = 5,
    BottomLeft = 6,
    BottomCenter = 7,
    BottomRight = 8,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(default, rename_all = "PascalCase")]
pub struct FileRule {
    pub file_name: String,
    pub file_path: String,
    pub duration: String,
    pub mode: TimerMode,
    pub enabled: bool,
    pub title_pattern: String,
    pub feature: String,
}

impl Default for FileRule {
    fn default() -> Self {
        Self {
            file_name: String::new(),
            file_path: String::new(),
            duration: "00:08:00".to_owned(),
            mode: TimerMode::Countdown,
            enabled: true,
            title_pattern: String::new(),
            feature: String::new(),
        }
    }
}

fn parse_duration(value: &str) -> Option<Duration> {
    let mut fields = value.split(':');
    let hours: u64 = fields.next()?.parse().ok()?;
    let minutes: u64 = fields.next()?.parse().ok()?;
    let seconds: u64 = fields.next()?.parse().ok()?;
    if fields.next().is_some() || minutes >= 60 || seconds >= 60 {
        return None;
    }

    let total = hours
        .checked_mul(3_600)?
        .checked_add(minutes.checked_mul(60)?)?
        .checked_add(seconds)?;
    (total > 0).then(|| Duration::from_secs(total))
}

pub fn is_valid_duration(value: &str) -> bool {
    parse_duration(value).is_some()
}

#[cfg(test)]
mod tests {
    use serde_json::Value;

    use super::*;

    const V0302_DEFAULT_CONFIG: &str = include_str!("../docs/default-config.json");

    #[test]
    fn reads_real_v0302_default_configuration() {
        let config = AppConfig::from_json(V0302_DEFAULT_CONFIG).unwrap();

        assert_eq!(config.version, "0.30.2");
        assert_eq!(config.timer.duration(), Duration::from_secs(8 * 60));
        assert_eq!(config.timer.mode, TimerMode::Countdown);
        assert!(config.timer.effective_continue_overtime());
        assert_eq!(config.behavior.prompt1.trigger_before_end_seconds, 120);
        assert_eq!(config.behavior.prompt2.trigger_before_end_seconds, 30);
        assert_eq!(config.controls.start_pause_hotkey, "F3");
        assert_eq!(config.remote_control.port, 4080);
        assert_eq!(config.placement.anchor, OverlayAnchor::TopCenter);
    }

    #[test]
    fn new_configuration_matches_all_v0302_default_fields() {
        let mut expected: Value = serde_json::from_str(V0302_DEFAULT_CONFIG).unwrap();
        let config = AppConfig {
            version: "0.30.2".to_owned(),
            ..AppConfig::default()
        };
        let mut actual = serde_json::to_value(config).unwrap();

        normalize_integral_numbers(&mut expected);
        normalize_integral_numbers(&mut actual);

        assert_eq!(actual, expected);
    }

    #[test]
    fn save_updates_version_and_can_be_loaded_again() {
        let path = Path::new("target/tests/FlyPPTTimer.config.json");
        let mut config = AppConfig::from_json(V0302_DEFAULT_CONFIG).unwrap();
        config.language = "zh-CN".to_owned();
        config.save(path).unwrap();

        let loaded = AppConfig::load(path).unwrap();
        assert_eq!(loaded.version, V1_CONFIG_VERSION);
        assert_eq!(loaded.language, "zh-CN");
        assert_eq!(loaded.timer.duration(), Duration::from_secs(8 * 60));

        fs::remove_file(path).unwrap();
        let backup = path.with_extension("json.bak");
        if backup.exists() {
            fs::remove_file(backup).unwrap();
        }
    }

    fn normalize_integral_numbers(value: &mut Value) {
        match value {
            Value::Array(items) => items.iter_mut().for_each(normalize_integral_numbers),
            Value::Object(fields) => fields.values_mut().for_each(normalize_integral_numbers),
            Value::Number(number) => {
                if let Some(float) = number.as_f64()
                    && float.fract() == 0.0
                    && float >= i64::MIN as f64
                    && float <= i64::MAX as f64
                {
                    *number = (float as i64).into();
                }
            }
            _ => {}
        }
    }
}
