#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod alerts;
mod app;
mod audio;
mod capture;
mod color_picker;
pub mod config;
mod desktop;
mod display;
mod file_browser;
mod flash;
mod log;
mod mobile_rules;
mod presentation;
mod remote;
mod scenario;
mod settings;
mod single_instance;
mod slide_timer;
mod theme;
mod timer;
mod updater;
mod window;
use windows_sys::Win32::Foundation::CloseHandle;
use windows_sys::Win32::System::Threading::{
    INFINITE, OpenProcess, PROCESS_SYNCHRONIZE, WaitForSingleObject,
};

fn wait_for_restart_parent() {
    let arguments = std::env::args().collect::<Vec<_>>();
    let Some(index) = arguments
        .iter()
        .position(|argument| argument == "--restart-after")
    else {
        return;
    };
    let Ok(pid) = arguments
        .get(index + 1)
        .map(String::as_str)
        .unwrap_or_default()
        .parse::<u32>()
    else {
        return;
    };
    unsafe {
        let handle = OpenProcess(PROCESS_SYNCHRONIZE, 0, pid);
        if !handle.is_null() {
            WaitForSingleObject(handle, INFINITE);
            CloseHandle(handle);
        }
    }
}
fn main() -> Result<(), Box<dyn std::error::Error>> {
    if let Some(result) = install_update_handoff() {
        return result.map_err(Into::into);
    }
    if let Some(result) = portable_update_handoff() {
        return result.map_err(Into::into);
    }
    wait_for_restart_parent();
    let arguments = std::env::args().collect::<Vec<_>>();
    if let Some(index) = arguments
        .iter()
        .position(|argument| argument == "--capture-settings")
    {
        let output = arguments
            .get(index + 1)
            .ok_or("--capture-settings requires an output directory")?;
        return capture::capture_all(output.into());
    }
    if arguments
        .iter()
        .any(|argument| argument == "--capture-windows")
    {
        let output = arguments
            .iter()
            .position(|argument| argument == "--capture-windows")
            .and_then(|index| arguments.get(index + 1))
            .ok_or("--capture-windows requires an output directory")?;
        return capture::capture_windows(output.into());
    }
    let result = app::run();
    if let Err(error) = &result {
        log::error(&format!("FlyPPTTimer terminated: {error}"));
    }
    result
}

fn install_update_handoff() -> Option<Result<(), String>> {
    let arguments: Vec<_> = std::env::args_os().collect();
    if arguments
        .get(1)
        .is_none_or(|arg| arg != "--install-update-after")
    {
        return None;
    }
    Some((|| {
        if arguments.len() != 5 {
            return Err("Invalid installer handoff arguments".into());
        }
        let pid = arguments[2]
            .to_str()
            .and_then(|v| v.parse::<u32>().ok())
            .filter(|pid| *pid != 0 && *pid != std::process::id())
            .ok_or("Invalid installer parent PID")?;
        let path = std::path::Path::new(&arguments[3]);
        if !path.is_absolute() || !path.is_file() {
            return Err("Installer path must be an existing absolute file".into());
        }
        unsafe {
            let handle = OpenProcess(PROCESS_SYNCHRONIZE, 0, pid);
            if handle.is_null() {
                // ERROR_INVALID_PARAMETER means the parent has already exited.
                let error = windows_sys::Win32::Foundation::GetLastError();
                if error != windows_sys::Win32::Foundation::ERROR_INVALID_PARAMETER {
                    return Err(format!("Cannot wait for installer parent: {error}"));
                }
            } else {
                let wait = WaitForSingleObject(handle, INFINITE);
                CloseHandle(handle);
                if wait != windows_sys::Win32::Foundation::WAIT_OBJECT_0 {
                    return Err(format!("Installer parent wait failed: {wait}"));
                }
            }
        }
        let restart = std::path::PathBuf::from(&arguments[4]);
        let status = std::process::Command::new(path)
            .args([
                "/VERYSILENT",
                "/SUPPRESSMSGBOXES",
                "/NORESTART",
                "/SP-",
                "/CLOSEAPPLICATIONS",
            ])
            .status()
            .map_err(|e| format!("Cannot launch installer: {e}"))?;
        if !status.success() {
            return Err(format!("Installer exited with {status}"));
        }
        std::process::Command::new(restart)
            .spawn()
            .map_err(|e| format!("Cannot restart FlyPPTTimer: {e}"))?;
        Ok(())
    })())
}

fn portable_update_handoff() -> Option<Result<(), String>> {
    let args: Vec<_> = std::env::args_os().collect();
    if args.get(1).is_none_or(|v| v != "--portable-update-after") {
        return None;
    }
    Some((|| {
        if args.len() != 6 {
            return Err("Invalid portable updater handoff arguments".into());
        }
        let pid = args[2]
            .to_str()
            .and_then(|v| v.parse::<u32>().ok())
            .filter(|v| *v != 0)
            .ok_or("Invalid updater parent PID")?;
        let staging = std::path::PathBuf::from(&args[3]);
        let target = std::path::PathBuf::from(&args[4]);
        let restart = std::path::PathBuf::from(&args[5]);
        unsafe {
            let handle = OpenProcess(PROCESS_SYNCHRONIZE, 0, pid);
            if !handle.is_null() {
                WaitForSingleObject(handle, INFINITE);
                CloseHandle(handle);
            }
        }
        fn copy_tree(src: &std::path::Path, dst: &std::path::Path) -> Result<(), String> {
            std::fs::create_dir_all(dst).map_err(|e| e.to_string())?;
            for entry in std::fs::read_dir(src).map_err(|e| e.to_string())?.flatten() {
                let from = entry.path();
                let name = entry.file_name();
                let text = name.to_string_lossy();
                if text.eq_ignore_ascii_case("FlyPPTTimer.config.json")
                    || text.eq_ignore_ascii_case("alert-sounds")
                {
                    continue;
                }
                let to = dst.join(&name);
                if from.is_dir() {
                    copy_tree(&from, &to)?;
                } else {
                    std::fs::copy(&from, &to).map_err(|e| e.to_string())?;
                }
            }
            Ok(())
        }
        copy_tree(&staging, &target)?;
        std::process::Command::new(restart)
            .current_dir(target)
            .spawn()
            .map_err(|e| format!("Cannot restart FlyPPTTimer: {e}"))?;
        Ok(())
    })())
}
