use std::{
    collections::HashMap,
    ptr::{null, null_mut},
    sync::{Mutex, OnceLock, mpsc},
    thread,
};

use windows_sys::Win32::{
    Foundation::{HWND, LPARAM, LRESULT, POINT, WPARAM},
    System::LibraryLoader::GetModuleHandleW,
    UI::{
        Input::KeyboardAndMouse::{
            MOD_ALT, MOD_CONTROL, MOD_SHIFT, MOD_WIN, RegisterHotKey, UnregisterHotKey,
        },
        Shell::{
            NIF_ICON, NIF_MESSAGE, NIF_TIP, NIM_ADD, NIM_DELETE, NOTIFYICONDATAW, Shell_NotifyIconW,
        },
        WindowsAndMessaging::{
            AppendMenuW, CS_HREDRAW, CS_VREDRAW, CW_USEDEFAULT, CreateIconFromResourceEx,
            CreatePopupMenu, CreateWindowExW, DefWindowProcW, DestroyIcon, DestroyMenu,
            DestroyWindow, DispatchMessageW, GetCursorPos, GetMessageW, HMENU, IDI_APPLICATION,
            LR_DEFAULTCOLOR, LoadIconW, MB_ICONWARNING, MB_OK, MF_SEPARATOR, MF_STRING, MSG,
            MessageBoxW, PostMessageW, PostQuitMessage, RegisterClassW, SetForegroundWindow,
            TPM_RIGHTBUTTON, TPM_VERTICAL, TrackPopupMenu, TranslateMessage, WM_APP, WM_CLOSE,
            WM_COMMAND, WM_DESTROY, WM_HOTKEY, WM_LBUTTONDBLCLK, WM_RBUTTONUP, WNDCLASSW,
        },
    },
};

use crate::config::AppConfig;

const TRAY_MESSAGE: u32 = WM_APP + 1;
const RECONFIGURE_MESSAGE: u32 = WM_APP + 2;
const TIMER_MENU_MESSAGE: u32 = WM_APP + 3;
const TRAY_ID: u32 = 1;
const MENU_RESET_POSITION: usize = 1001;
const MENU_MUTE: usize = 1002;
const MENU_REMOTE: usize = 1003;
const MENU_SETTINGS: usize = 1004;
const MENU_UPDATE: usize = 1005;
const MENU_EXIT: usize = 1006;

static EVENT_SENDER: OnceLock<Mutex<Option<mpsc::Sender<DesktopEvent>>>> = OnceLock::new();
static HOTKEY_COMMANDS: OnceLock<Mutex<HashMap<i32, String>>> = OnceLock::new();
static DESKTOP_SETTINGS: OnceLock<Mutex<DesktopSettings>> = OnceLock::new();
static TRAY_ICON: OnceLock<Mutex<isize>> = OnceLock::new();

#[derive(Debug)]
pub enum DesktopEvent {
    Command(String),
    ResetPosition,
    OpenSettings,
    Remote,
    CheckUpdate,
    Exit,
}

#[derive(Clone, Default)]
struct DesktopSettings {
    hotkeys: Vec<(String, String)>,
    english: bool,
}

pub struct DesktopIntegration {
    hwnd: HWND,
    receiver: mpsc::Receiver<DesktopEvent>,
    thread: Option<thread::JoinHandle<()>>,
}

impl DesktopIntegration {
    pub fn start(config: &AppConfig) -> Result<Self, String> {
        let (event_sender, receiver) = mpsc::channel();
        let (ready_sender, ready_receiver) = mpsc::sync_channel(1);
        *EVENT_SENDER.get_or_init(Default::default).lock().unwrap() = Some(event_sender);
        *DESKTOP_SETTINGS
            .get_or_init(Default::default)
            .lock()
            .unwrap() = settings(config);
        let thread = thread::Builder::new()
            .name("flyppttimer-desktop".to_owned())
            .spawn(move || desktop_thread(ready_sender))
            .map_err(|error| error.to_string())?;
        let hwnd = ready_receiver.recv().map_err(|error| error.to_string())?? as HWND;
        Ok(Self {
            hwnd,
            receiver,
            thread: Some(thread),
        })
    }

    pub fn reconfigure(&self, config: &AppConfig) {
        *DESKTOP_SETTINGS
            .get_or_init(Default::default)
            .lock()
            .unwrap() = settings(config);
        unsafe { PostMessageW(self.hwnd, RECONFIGURE_MESSAGE, 0, 0) };
    }

    pub fn show_timer_menu(&self) {
        unsafe { PostMessageW(self.hwnd, TIMER_MENU_MESSAGE, 0, 0) };
    }

    pub fn try_recv(&self) -> Option<DesktopEvent> {
        self.receiver.try_recv().ok()
    }

    pub fn shutdown(&self) {
        unsafe { PostMessageW(self.hwnd, WM_CLOSE, 0, 0) };
    }
}

impl Drop for DesktopIntegration {
    fn drop(&mut self) {
        self.shutdown();
        if let Some(thread) = self.thread.take() {
            let _ = thread.join();
        }
    }
}

fn settings(config: &AppConfig) -> DesktopSettings {
    let mut hotkeys = config.controls.hotkeys.clone();
    hotkeys.insert(
        "startPause".to_owned(),
        config.controls.start_pause_hotkey.clone(),
    );
    hotkeys.insert(
        "stopReset".to_owned(),
        config.controls.stop_reset_hotkey.clone(),
    );
    hotkeys.insert(
        "toggleWindow".to_owned(),
        config.controls.toggle_window_hotkey.clone(),
    );
    DesktopSettings {
        hotkeys: hotkeys.into_iter().collect(),
        english: crate::config::ui_is_english(&config.language),
    }
}

fn desktop_thread(ready: mpsc::SyncSender<Result<isize, String>>) {
    unsafe {
        let instance = GetModuleHandleW(null());
        let class_name = wide("FlyPPTTimerDesktopWindow");
        let class = WNDCLASSW {
            style: CS_HREDRAW | CS_VREDRAW,
            lpfnWndProc: Some(window_proc),
            hInstance: instance,
            lpszClassName: class_name.as_ptr(),
            ..Default::default()
        };
        if RegisterClassW(&class) == 0 {
            let _ = ready.send(Err("failed to register desktop window class".to_owned()));
            return;
        }
        let hwnd = CreateWindowExW(
            0,
            class_name.as_ptr(),
            class_name.as_ptr(),
            0,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            0,
            0,
            null_mut(),
            null_mut(),
            instance,
            null(),
        );
        if hwnd.is_null() {
            let _ = ready.send(Err("failed to create desktop window".to_owned()));
            return;
        }
        add_tray_icon(hwnd);
        register_hotkeys(hwnd);
        let _ = ready.send(Ok(hwnd as isize));
        let mut message: MSG = std::mem::zeroed();
        while GetMessageW(&mut message, null_mut(), 0, 0) > 0 {
            TranslateMessage(&message);
            DispatchMessageW(&message);
        }
    }
}

unsafe extern "system" fn window_proc(
    hwnd: HWND,
    message: u32,
    wparam: WPARAM,
    lparam: LPARAM,
) -> LRESULT {
    match message {
        WM_HOTKEY => {
            if let Some(command) = HOTKEY_COMMANDS
                .get_or_init(Default::default)
                .lock()
                .unwrap()
                .get(&(wparam as i32))
                .cloned()
            {
                send(DesktopEvent::Command(command));
            }
            0
        }
        TRAY_MESSAGE => {
            match lparam as u32 {
                WM_LBUTTONDBLCLK => send(DesktopEvent::OpenSettings),
                WM_RBUTTONUP => unsafe { show_tray_menu(hwnd) },
                _ => {}
            }
            0
        }
        TIMER_MENU_MESSAGE => {
            unsafe { show_timer_menu(hwnd) };
            0
        }
        RECONFIGURE_MESSAGE => {
            unsafe {
                unregister_hotkeys(hwnd);
                register_hotkeys(hwnd)
            };
            0
        }
        WM_COMMAND => {
            match wparam & 0xffff {
                MENU_RESET_POSITION => send(DesktopEvent::ResetPosition),
                MENU_MUTE => send(DesktopEvent::Command("toggleMute".to_owned())),
                MENU_REMOTE => send(DesktopEvent::Remote),
                MENU_SETTINGS => send(DesktopEvent::OpenSettings),
                MENU_UPDATE => send(DesktopEvent::CheckUpdate),
                MENU_EXIT => send(DesktopEvent::Exit),
                _ => {}
            }
            0
        }
        WM_CLOSE => {
            unsafe { DestroyWindow(hwnd) };
            0
        }
        WM_DESTROY => {
            unsafe {
                unregister_hotkeys(hwnd);
                delete_tray_icon(hwnd);
                PostQuitMessage(0)
            };
            0
        }
        _ => unsafe { DefWindowProcW(hwnd, message, wparam, lparam) },
    }
}

fn send(event: DesktopEvent) {
    if let Some(sender) = EVENT_SENDER
        .get()
        .and_then(|slot| slot.lock().ok())
        .and_then(|slot| slot.clone())
    {
        let _ = sender.send(event);
    }
}

unsafe fn add_tray_icon(hwnd: HWND) {
    let mut data = NOTIFYICONDATAW {
        cbSize: std::mem::size_of::<NOTIFYICONDATAW>() as u32,
        hWnd: hwnd,
        uID: TRAY_ID,
        uFlags: NIF_MESSAGE | NIF_ICON | NIF_TIP,
        uCallbackMessage: TRAY_MESSAGE,
        hIcon: load_app_icon(),
        ..Default::default()
    };
    copy_wide(&mut data.szTip, "FlyPPTTimer");
    unsafe { Shell_NotifyIconW(NIM_ADD, &data) };
}

unsafe fn delete_tray_icon(hwnd: HWND) {
    let data = NOTIFYICONDATAW {
        cbSize: std::mem::size_of::<NOTIFYICONDATAW>() as u32,
        hWnd: hwnd,
        uID: TRAY_ID,
        ..Default::default()
    };
    unsafe { Shell_NotifyIconW(NIM_DELETE, &data) };
    let icon = std::mem::take(&mut *TRAY_ICON.get_or_init(Default::default).lock().unwrap());
    if icon != 0 {
        unsafe { DestroyIcon(icon as _) };
    }
}
unsafe fn show_timer_menu(hwnd: HWND) {
    let english = DESKTOP_SETTINGS
        .get_or_init(Default::default)
        .lock()
        .unwrap()
        .english;
    let menu: HMENU = unsafe { CreatePopupMenu() };
    append(
        menu,
        MENU_RESET_POSITION,
        if english {
            "Reset timer window position"
        } else {
            "重置计时窗口位置"
        },
    );
    append(
        menu,
        MENU_MUTE,
        if english {
            "Mute / Unmute"
        } else {
            "静音/取消静音"
        },
    );
    append(
        menu,
        MENU_REMOTE,
        if english {
            "Remote Control"
        } else {
            "远程控制"
        },
    );
    append(
        menu,
        MENU_SETTINGS,
        if english { "Settings" } else { "设置" },
    );
    unsafe { AppendMenuW(menu, MF_SEPARATOR, 0, null()) };
    append(menu, MENU_EXIT, if english { "Exit" } else { "退出" });
    let mut point = POINT::default();
    unsafe {
        GetCursorPos(&mut point);
        // Keep the menu above and to the left of the timer, matching v0.30.2.
        let width = 236;
        let height = 250;
        let left = windows_sys::Win32::UI::WindowsAndMessaging::GetSystemMetrics(
            windows_sys::Win32::UI::WindowsAndMessaging::SM_XVIRTUALSCREEN,
        );
        let top = windows_sys::Win32::UI::WindowsAndMessaging::GetSystemMetrics(
            windows_sys::Win32::UI::WindowsAndMessaging::SM_YVIRTUALSCREEN,
        );
        let right = left
            + windows_sys::Win32::UI::WindowsAndMessaging::GetSystemMetrics(
                windows_sys::Win32::UI::WindowsAndMessaging::SM_CXVIRTUALSCREEN,
            );
        let bottom = top
            + windows_sys::Win32::UI::WindowsAndMessaging::GetSystemMetrics(
                windows_sys::Win32::UI::WindowsAndMessaging::SM_CYVIRTUALSCREEN,
            );
        let x = (point.x - width).clamp(left, (right - width).max(left));
        let y = (point.y - height).clamp(top, (bottom - height).max(top));
        SetForegroundWindow(hwnd);
        TrackPopupMenu(menu, TPM_RIGHTBUTTON | TPM_VERTICAL, x, y, 0, hwnd, null());
        DestroyMenu(menu)
    };
}
unsafe fn show_tray_menu(hwnd: HWND) {
    let english = DESKTOP_SETTINGS
        .get_or_init(Default::default)
        .lock()
        .unwrap()
        .english;
    let menu: HMENU = unsafe { CreatePopupMenu() };
    append(
        menu,
        MENU_RESET_POSITION,
        if english {
            "Reset timer window position"
        } else {
            "重置计时窗口位置"
        },
    );
    append(
        menu,
        MENU_MUTE,
        if english {
            "Mute / Unmute"
        } else {
            "静音/取消静音"
        },
    );
    append(
        menu,
        MENU_REMOTE,
        if english {
            "Remote Control"
        } else {
            "远程控制"
        },
    );
    append(
        menu,
        MENU_SETTINGS,
        if english { "Settings" } else { "设置" },
    );
    append(
        menu,
        MENU_UPDATE,
        if english {
            "Check for Updates"
        } else {
            "检测新版本"
        },
    );
    unsafe { AppendMenuW(menu, MF_SEPARATOR, 0, null()) };
    append(menu, MENU_EXIT, if english { "Exit" } else { "退出" });
    let mut point = POINT::default();
    unsafe {
        GetCursorPos(&mut point);
        SetForegroundWindow(hwnd);
        TrackPopupMenu(menu, TPM_RIGHTBUTTON, point.x, point.y, 0, hwnd, null());
        DestroyMenu(menu)
    };
}

fn append(menu: HMENU, id: usize, text: &str) {
    let text = wide(text);
    unsafe { AppendMenuW(menu, MF_STRING, id, text.as_ptr()) };
}

unsafe fn register_hotkeys(hwnd: HWND) {
    let settings = DESKTOP_SETTINGS
        .get_or_init(Default::default)
        .lock()
        .unwrap()
        .clone();
    let mut commands = HOTKEY_COMMANDS
        .get_or_init(Default::default)
        .lock()
        .unwrap();
    commands.clear();
    let mut used = std::collections::HashSet::new();
    for (index, (command, binding)) in settings
        .hotkeys
        .iter()
        .filter(|(_, value)| !value.trim().is_empty())
        .enumerate()
    {
        let Some((modifiers, key)) = parse_hotkey(binding) else {
            continue;
        };
        if !used.insert((modifiers, key)) {
            continue;
        }
        let id = 2000 + index as i32;
        if unsafe { RegisterHotKey(hwnd, id, modifiers, key) } != 0 {
            commands.insert(id, command.clone());
        } else {
            eprintln!("failed to register global hotkey: {binding}");
            let message = if settings.english {
                format!("Failed to register hotkey: {binding}")
            } else {
                format!("快捷键注册失败：{binding}")
            };
            let message = wide(&message);
            let title = wide("FlyPPTTimer");
            unsafe {
                MessageBoxW(
                    hwnd,
                    message.as_ptr(),
                    title.as_ptr(),
                    MB_OK | MB_ICONWARNING,
                )
            };
        }
    }
}

unsafe fn unregister_hotkeys(hwnd: HWND) {
    let mut commands = HOTKEY_COMMANDS
        .get_or_init(Default::default)
        .lock()
        .unwrap();
    for id in commands.keys() {
        unsafe { UnregisterHotKey(hwnd, *id) };
    }
    commands.clear();
}

fn parse_hotkey(binding: &str) -> Option<(u32, u32)> {
    let parts = binding.split('+').map(str::trim).collect::<Vec<_>>();
    let mut modifiers = 0;
    let mut key = None;
    for part in parts {
        match part.to_ascii_lowercase().as_str() {
            "ctrl" | "control" => modifiers |= MOD_CONTROL,
            "alt" => modifiers |= MOD_ALT,
            "shift" => modifiers |= MOD_SHIFT,
            "win" => modifiers |= MOD_WIN,
            name => key = virtual_key(name),
        }
    }
    key.map(|key| (modifiers, key))
}

fn virtual_key(name: &str) -> Option<u32> {
    if let Some(number) = name
        .strip_prefix('f')
        .and_then(|value| value.parse::<u32>().ok())
        .filter(|value| (1..=24).contains(value))
    {
        return Some(0x6f + number);
    }
    match name {
        "up" => Some(0x26),
        "down" => Some(0x28),
        "left" => Some(0x25),
        "right" => Some(0x27),
        "space" => Some(0x20),
        _ if name.len() == 1 => name
            .as_bytes()
            .first()
            .map(|value| value.to_ascii_uppercase() as u32),
        _ => None,
    }
}

fn wide(value: &str) -> Vec<u16> {
    value.encode_utf16().chain(Some(0)).collect()
}

fn copy_wide<const N: usize>(destination: &mut [u16; N], value: &str) {
    for (target, source) in destination.iter_mut().zip(value.encode_utf16()) {
        *target = source;
    }
}

fn load_app_icon() -> windows_sys::Win32::UI::WindowsAndMessaging::HICON {
    let file = include_bytes!("FlyPPTTimer/Assets/app.ico");
    let custom = (|| {
        let entry = file.get(6..22)?;
        let size = u32::from_le_bytes(entry.get(8..12)?.try_into().ok()?) as usize;
        let offset = u32::from_le_bytes(entry.get(12..16)?.try_into().ok()?) as usize;
        let image = file.get(offset..offset.checked_add(size)?)?;
        let icon = unsafe {
            CreateIconFromResourceEx(
                image.as_ptr(),
                image.len() as u32,
                1,
                0x0003_0000,
                0,
                0,
                LR_DEFAULTCOLOR,
            )
        };
        (!icon.is_null()).then_some(icon)
    })();
    if let Some(icon) = custom {
        *TRAY_ICON.get_or_init(Default::default).lock().unwrap() = icon as isize;
        icon
    } else {
        unsafe { LoadIconW(null_mut(), IDI_APPLICATION) }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn parses_baseline_hotkeys() {
        assert_eq!(parse_hotkey("F3"), Some((0, 0x72)));
        assert_eq!(
            parse_hotkey("Ctrl+Alt+Up"),
            Some((MOD_CONTROL | MOD_ALT, 0x26))
        );
        assert_eq!(
            parse_hotkey("Ctrl+Alt+5"),
            Some((MOD_CONTROL | MOD_ALT, b'5' as u32))
        );
    }
}
