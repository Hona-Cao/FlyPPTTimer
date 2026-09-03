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
