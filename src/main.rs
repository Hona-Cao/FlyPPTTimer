#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod alerts;
mod app;
mod audio;
mod capture;
pub mod config;
mod desktop;
mod display;
mod flash;
mod log;
mod mobile_rules;
mod presentation;
mod remote;
mod settings;
mod single_instance;
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
        if arguments.len() != 4 {
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
        use std::os::windows::ffi::OsStrExt;
        let file: Vec<u16> = path.as_os_str().encode_wide().chain(Some(0)).collect();
        let directory: Vec<u16> = path
            .parent()
            .unwrap()
            .as_os_str()
            .encode_wide()
            .chain(Some(0))
            .collect();
        let result = unsafe {
            windows_sys::Win32::UI::Shell::ShellExecuteW(
                std::ptr::null_mut(),
                std::ptr::null(),
                file.as_ptr(),
                std::ptr::null(),
                directory.as_ptr(),
                windows_sys::Win32::UI::WindowsAndMessaging::SW_SHOWNORMAL,
            )
        };
        if result as isize <= 32 {
            return Err(format!("Cannot launch installer: {}", result as isize));
        }
        Ok(())
    })())
}
