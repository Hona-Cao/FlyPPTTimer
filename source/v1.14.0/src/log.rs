use std::{
    fs::{self, OpenOptions},
    io::Write,
    path::PathBuf,
    sync::{Mutex, OnceLock},
};

const MAX_LOG_BYTES: u64 = 2 * 1024 * 1024;
static LOCK: OnceLock<Mutex<()>> = OnceLock::new();

pub fn info(message: &str) {
    write("INFO", message);
}

pub fn error(message: &str) {
    write("ERROR", message);
}

fn write(level: &str, message: &str) {
    let Ok(_guard) = LOCK.get_or_init(|| Mutex::new(())).lock() else {
        return;
    };
    let Some(directory) = log_directory() else {
        return;
    };
    if fs::create_dir_all(&directory).is_err() {
        return;
    }
    let (date, timestamp) = local_timestamp();
    let mut path = directory.join(format!("app-{date}.log"));
    if path
        .metadata()
        .is_ok_and(|metadata| metadata.len() >= MAX_LOG_BYTES)
    {
        for index in 1.. {
            let candidate = directory.join(format!("app-{date}-{index}.log"));
            if candidate
                .metadata()
                .map_or(true, |metadata| metadata.len() < MAX_LOG_BYTES)
            {
                path = candidate;
                break;
            }
        }
    }
    if let Ok(mut file) = OpenOptions::new().create(true).append(true).open(path) {
        let _ = writeln!(file, "{timestamp} [{level}] {message}");
    }
}

pub fn log_directory() -> Option<PathBuf> {
    std::env::current_exe()
        .ok()?
        .parent()
        .map(|directory| directory.join("logs"))
}

fn local_timestamp() -> (String, String) {
    let mut value = windows_sys::Win32::Foundation::SYSTEMTIME::default();
    unsafe {
        windows_sys::Win32::System::SystemInformation::GetLocalTime(&mut value);
    }
    (
        format!("{:04}{:02}{:02}", value.wYear, value.wMonth, value.wDay),
        format!(
            "{:04}-{:02}-{:02} {:02}:{:02}:{:02}.{:03}",
            value.wYear,
            value.wMonth,
            value.wDay,
            value.wHour,
            value.wMinute,
            value.wSecond,
            value.wMilliseconds
        ),
    )
}
