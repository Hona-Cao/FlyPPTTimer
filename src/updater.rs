use std::{
    cell::Cell,
    ffi::c_void,
    fs,
    os::windows::process::CommandExt,
    path::{Path, PathBuf},
    ptr::{null, null_mut},
    sync::{Mutex, mpsc},
    thread,
};

use serde_json::Value;
use windows_sys::Win32::Networking::WinHttp::{
    WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY, WINHTTP_FLAG_SECURE, WINHTTP_QUERY_FLAG_NUMBER,
    WINHTTP_QUERY_STATUS_CODE, WinHttpCloseHandle, WinHttpConnect, WinHttpOpen, WinHttpOpenRequest,
    WinHttpQueryHeaders, WinHttpReadData, WinHttpReceiveResponse, WinHttpSendRequest,
    WinHttpSetTimeouts,
};

pub const RELEASES_URL: &str = "https://gitee.com/hona-cao/fly-ppttimer/releases";
const LATEST_RELEASE_API: &str =
    "https://gitee.com/api/v5/repos/hona-cao/fly-ppttimer/releases/latest";
const RELEASE_API: &str = "https://gitee.com/api/v5/repos/hona-cao/fly-ppttimer/releases";

#[derive(Clone, Debug, PartialEq, Eq)]
pub struct ReleaseAsset {
    pub name: String,
    pub download_url: String,
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub struct ReleaseInfo {
    pub version: String,
    pub body: String,
    pub release_url: String,
    pub assets: Vec<ReleaseAsset>,
}

impl ReleaseInfo {
    pub fn installer(&self) -> Option<&ReleaseAsset> {
        self.assets.iter().find(|asset| {
            let name = asset.name.to_ascii_lowercase();
            name.ends_with(".exe") && name.contains("setup") && name.contains("win-x64")
        })
    }
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub enum CheckStatus {
    NoRelease,
    UpToDate,
    UpdateAvailable(ReleaseInfo),
}

#[derive(Debug)]
pub enum Response {
    Checked {
        user_initiated: bool,
        result: Result<CheckStatus, Box<dyn std::error::Error + Send + Sync>>,
    },
    Downloaded {
        result: Result<PathBuf, String>,
    },
}

pub struct UpdateService {
    sender: mpsc::Sender<Response>,
    receiver: Mutex<mpsc::Receiver<Response>>,
    busy: Cell<bool>,
}

impl UpdateService {
    pub fn new() -> Self {
        let (sender, receiver) = mpsc::channel();
        Self {
            sender,
            receiver: Mutex::new(receiver),
            busy: Cell::new(false),
        }
    }

    pub fn check(&self, user_initiated: bool) -> bool {
        if self.busy.replace(true) {
            return false;
        }
        let sender = self.sender.clone();
        thread::spawn(move || {
            let result = check_latest();
            match &result {
                Ok(status) => crate::log::info(&format!("Update check completed: {status:?}")),
                Err(error) => crate::log::error(&format!("Update check failed: {error}")),
            }
            let _ = sender.send(Response::Checked {
                user_initiated,
                result,
            });
        });
        true
    }

    pub fn download(&self, release: ReleaseInfo) -> bool {
        let sender = self.sender.clone();
        thread::spawn(move || {
            let result = download_installer(&release).map_err(|error| error.to_string());
            match &result {
                Ok(path) => {
                    crate::log::info(&format!("Update installer downloaded: {}", path.display()))
                }
                Err(error) => crate::log::error(&format!("Update download failed: {error}")),
            }
            let _ = sender.send(Response::Downloaded { result });
        });
        true
    }

    pub fn try_recv(&self) -> Option<Response> {
        self.receiver.lock().ok()?.try_recv().ok()
    }
}

pub fn start_check_ui(
    service: &UpdateService,
    config: &crate::config::AppConfig,
    desktop: &crate::desktop::DesktopIntegration,
    user: bool,
) {
    let started = service.check(user);
    if started && user {
        desktop.notify(
            text(
                config,
                "正在从 Gitee 检测新版本…",
                "Checking Gitee for updates…",
            ),
            2000,
        );
    } else if !started && user {
        crate::settings::native::message(
            text(
                config,
                "\u{6b63}\u{5728}\u{68c0}\u{6d4b}\u{65b0}\u{7248}\u{672c}\u{ff0c}\u{8bf7}\u{7a0d}\u{5019}\u{3002}",
                "Checking for updates. Please wait.",
            ),
            text(config, "FlyPPTTimer \u{66f4}\u{65b0}", "FlyPPTTimer Update"),
            false,
        );
    }
}

pub fn handle_response_ui(
    response: Response,
    service: &UpdateService,
    config: &crate::config::AppConfig,
    desktop: &crate::desktop::DesktopIntegration,
) {
    match response {
        Response::Checked {
            user_initiated,
            result,
        } => handle_checked(result, user_initiated, service, config, desktop),
        Response::Downloaded { result } => {
            match result {
                Ok(path) => match launch_installer_after_exit(&path) {
                    Ok(()) => {
                        let _ = slint::quit_event_loop();
                    }
                    Err(error) => show_error(config, &error),
                },
                Err(error) => show_error(config, &error),
            }
            service.busy.set(false);
        }
    }
}

fn handle_checked(
    result: Result<CheckStatus, Box<dyn std::error::Error + Send + Sync>>,
    user: bool,
    service: &UpdateService,
    config: &crate::config::AppConfig,
    desktop: &crate::desktop::DesktopIntegration,
) {
    match result {
        Ok(CheckStatus::NoRelease) if user => crate::settings::native::message(
            text(
                config,
                "Gitee \u{9879}\u{76ee}\u{76ee}\u{524d}\u{8fd8}\u{6ca1}\u{6709}\u{53d1}\u{5e03} Release\u{3002}",
                "The Gitee project has no releases yet.",
            ),
            text(config, "FlyPPTTimer \u{66f4}\u{65b0}", "FlyPPTTimer Update"),
            false,
        ),
        Ok(CheckStatus::UpToDate) if user => crate::settings::native::message(
            &format!(
                "{}v{}",
                text(
                    config,
                    "\u{5f53}\u{524d}\u{5df2}\u{662f}\u{6700}\u{65b0}\u{7248}\u{672c}\u{ff1a}",
                    "You already have the latest version: ",
                ),
                env!("CARGO_PKG_VERSION")
            ),
            text(config, "FlyPPTTimer \u{66f4}\u{65b0}", "FlyPPTTimer Update"),
            false,
        ),
        Ok(CheckStatus::UpdateAvailable(release)) => {
            if prompt_update(service, config, desktop, release) {
                return;
            }
        }
        Err(error) if user || !error.is::<std::io::Error>() => {
            show_error(config, &error.to_string())
        }
        _ => {}
    }
    service.busy.set(false);
}

fn prompt_update(
    service: &UpdateService,
    config: &crate::config::AppConfig,
    desktop: &crate::desktop::DesktopIntegration,
    release: ReleaseInfo,
) -> bool {
    let installed = is_installed_edition();
    let action = if installed && release.installer().is_some() {
        text(
            config,
            "\u{662f}\u{5426}\u{7acb}\u{5373}\u{4e0b}\u{8f7d}\u{5b89}\u{88c5}\u{ff1f}\u{5b89}\u{88c5}\u{65f6}\u{4f1a}\u{4fdd}\u{7559}\u{5f53}\u{524d}\u{914d}\u{7f6e}\u{ff0c}\u{65b0}\u{529f}\u{80fd}\u{4ecd}\u{4f7f}\u{7528}\u{9ed8}\u{8ba4}\u{8bbe}\u{7f6e}\u{ff0c}\u{4e4b}\u{540e}\u{53ef}\u{81ea}\u{884c}\u{9009}\u{62e9}\u{3002}",
            "Download and install now? Your current configuration will be preserved.",
        )
    } else if installed {
        text(
            config,
            "\u{6b64} Release \u{6682}\u{672a}\u{627e}\u{5230} Windows x64 \u{5b89}\u{88c5}\u{5305}\u{3002}\u{662f}\u{5426}\u{6253}\u{5f00} Gitee Release \u{9875}\u{9762}\u{ff1f}",
            "No Windows x64 installer was found in this release. Open the Gitee release page?",
        )
    } else {
        text(
            config,
            "\u{5f53}\u{524d}\u{4f7f}\u{7528}\u{7684}\u{662f}\u{7eff}\u{8272}\u{4fbf}\u{643a}\u{7248}\u{ff0c}\u{7a0b}\u{5e8f}\u{4e0d}\u{4f1a}\u{81ea}\u{52a8}\u{8986}\u{76d6}\u{6587}\u{4ef6}\u{3002}\u{662f}\u{5426}\u{6253}\u{5f00} Gitee Release \u{9875}\u{9762}\u{81ea}\u{884c}\u{9009}\u{62e9}\u{4e0b}\u{8f7d}\u{ff1f}",
            "You are using the portable edition, so the app will not overwrite its own files. Open the Gitee release page to download the update?",
        )
    };
    let prompt = update_prompt(config, &release, action);
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        MB_ICONINFORMATION, MB_ICONQUESTION, MB_ICONWARNING,
    };
    let icon = if !installed {
        MB_ICONINFORMATION
    } else if release.installer().is_some() {
        MB_ICONQUESTION
    } else {
        MB_ICONWARNING
    };
    if crate::settings::native::yes_no_with_icon(
        &prompt,
        text(
            config,
            "\u{53d1}\u{73b0}\u{65b0}\u{7248}\u{672c}",
            "Update available",
        ),
        icon,
    ) {
        if installed && release.installer().is_some() {
            desktop.notify(
                text(
                    config,
                    "正在下载更新，完成前请勿退出程序…",
                    "Downloading the update. Do not exit until it finishes…",
                ),
                3000,
            );
            return service.download(release);
        } else {
            crate::settings::native::open_url(&release.release_url);
        }
    }
    false
}

fn update_prompt(config: &crate::config::AppConfig, release: &ReleaseInfo, action: &str) -> String {
    let notes = release.body.trim().chars().take(600).collect::<String>();
    let notes = if notes.is_empty() {
        String::new()
    } else {
        format!(
            "\r\n\r\n{}\r\n{notes}",
            text(
                config,
                "\u{66f4}\u{65b0}\u{8bf4}\u{660e}\u{ff1a}",
                "Release notes:"
            )
        )
    };
    if crate::config::ui_is_english(&config.language) {
        return format!(
            "FlyPPTTimer v{} is available (current: v{}).{notes}\r\n\r\n{action}",
            release.version,
            env!("CARGO_PKG_VERSION")
        );
    }
    format!(
        "{} v{}\u{ff08}{} v{}\u{ff09}\u{3002}{}\r\n\r\n{}",
        text(
            config,
            "\u{53d1}\u{73b0}\u{65b0}\u{7248}\u{672c}",
            "Update available"
        ),
        release.version,
        text(config, "\u{5f53}\u{524d}", "current"),
        env!("CARGO_PKG_VERSION"),
        notes,
        action
    )
}

fn show_error(config: &crate::config::AppConfig, error: &str) {
    crate::log::error(&format!("Update check or download failed: {error}"));
    crate::settings::native::message(
        &format!(
            "{}\r\n{}\r\n\r\n{}",
            text(
                config,
                "\u{68c0}\u{6d4b}\u{6216}\u{4e0b}\u{8f7d}\u{65b0}\u{7248}\u{672c}\u{5931}\u{8d25}\u{ff1a}",
                "Update check or download failed:",
            ),
            error,
            text(
                config,
                "\u{53ef}\u{7a0d}\u{540e}\u{91cd}\u{8bd5}\u{ff0c}\u{6216}\u{524d}\u{5f80} Gitee Release \u{9875}\u{9762}\u{624b}\u{52a8}\u{4e0b}\u{8f7d}\u{3002}",
                "Try again later or download manually from the Gitee release page.",
            )
        ),
        text(config, "FlyPPTTimer \u{66f4}\u{65b0}", "FlyPPTTimer Update"),
        true,
    );
}

fn text<'a>(config: &crate::config::AppConfig, chinese: &'a str, english: &'a str) -> &'a str {
    if crate::config::ui_is_english(&config.language) {
        english
    } else {
        chinese
    }
}

pub fn is_installed_edition() -> bool {
    let Some(local_app_data) = std::env::var_os("LOCALAPPDATA") else {
        return false;
    };
    let expected = PathBuf::from(local_app_data).join("FlyPPTTimer");
    let Ok(executable) = std::env::current_exe() else {
        return false;
    };
    executable
        .parent()
        .is_some_and(|directory| same_windows_path(directory, &expected))
}

pub fn launch_installer_after_exit(path: &Path) -> Result<(), String> {
    if !path.is_file() {
        return Err("\u{4e0b}\u{8f7d}\u{7684}\u{5b89}\u{88c5}\u{7a0b}\u{5e8f}\u{4e0d}\u{5b58}\u{5728}\u{3002}".into());
    }
    let parent = path.parent().unwrap_or_else(|| Path::new("."));
    let script = parent.join("install-update.ps1");
    let escaped = path.to_string_lossy().replace('\'', "''");
    let process_id = std::process::id();
    fs::write(
        &script,
        format!(
            "$ErrorActionPreference = 'Stop'\r\ntry {{ Wait-Process -Id {process_id} -Timeout 30 -ErrorAction SilentlyContinue }} catch {{ }}\r\nStart-Process -FilePath '{escaped}' -WorkingDirectory (Split-Path -Parent '{escaped}')\r\nRemove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue\r\n"
        ),
    )
    .map_err(|error| error.to_string())?;
    std::process::Command::new("powershell.exe")
        .creation_flags(windows_sys::Win32::System::Threading::CREATE_NO_WINDOW)
        .args([
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-WindowStyle",
            "Hidden",
            "-File",
        ])
        .arg(&script)
        .spawn()
        .map_err(|error| error.to_string())?;
    Ok(())
}

fn check_latest() -> Result<CheckStatus, Box<dyn std::error::Error + Send + Sync>> {
    let response = http_get(LATEST_RELEASE_API)?;
    if response.status == 404 {
        return Ok(CheckStatus::NoRelease);
    }
    if !(200..300).contains(&response.status) {
        return Err(std::io::Error::other(format!("Gitee HTTP {}", response.status)).into());
    }
    let root: Value = serde_json::from_slice(&response.body)?;
    let tag = json_string(&root, "tag_name");
    let remote = parse_version(&tag).ok_or("Gitee Release \u{7684}\u{7248}\u{672c}\u{6807}\u{7b7e}\u{65e0}\u{6cd5}\u{8bc6}\u{522b}\u{3002}")?;
    let current = parse_version(env!("CARGO_PKG_VERSION")).ok_or(
        "\u{5f53}\u{524d}\u{7a0b}\u{5e8f}\u{7248}\u{672c}\u{65e0}\u{6cd5}\u{8bc6}\u{522b}\u{3002}",
    )?;
    if remote <= current {
        return Ok(CheckStatus::UpToDate);
    }

    let mut assets = parse_assets(&root);
    if assets.is_empty()
        && let Some(id) = root.get("id").and_then(Value::as_i64)
    {
        let response = http_get(&format!("{RELEASE_API}/{id}/attach_files"))?;
        if (200..300).contains(&response.status)
            && let Ok(value) = serde_json::from_slice::<Value>(&response.body)
        {
            assets = parse_assets(&value);
        }
    }
    let release_url = {
        let value = json_string(&root, "html_url");
        if value.is_empty() {
            format!("{RELEASES_URL}/tag/{tag}")
        } else {
            value
        }
    };
    Ok(CheckStatus::UpdateAvailable(ReleaseInfo {
        version: format_version(remote),
        body: json_string(&root, "body"),
        release_url,
        assets,
    }))
}

fn download_installer(
    release: &ReleaseInfo,
) -> Result<PathBuf, Box<dyn std::error::Error + Send + Sync>> {
    let asset = release
        .installer()
        .ok_or("\u{6b64} Release \u{4e2d}\u{672a}\u{627e}\u{5230} Windows x64 \u{5b89}\u{88c5}\u{7248}\u{3002}")?;
    let file_name = Path::new(&asset.name)
        .file_name()
        .ok_or("\u{5b89}\u{88c5}\u{5305}\u{6587}\u{4ef6}\u{540d}\u{65e0}\u{6548}\u{3002}")?;
    let directory = std::env::temp_dir()
        .join("FlyPPTTimer")
        .join("updates")
        .join(format!("v{}", release.version));
    fs::create_dir_all(&directory)?;
    let destination = directory.join(file_name);
    let temporary = destination.with_extension("download");
    if temporary.exists() {
        fs::remove_file(&temporary)?;
    }
    let response = http_get(&asset.download_url)?;
    if !(200..300).contains(&response.status) {
        return Err(format!(
            "\u{4e0b}\u{8f7d}\u{5b89}\u{88c5}\u{5305}\u{5931}\u{8d25}\u{ff1a}HTTP {}",
            response.status
        )
        .into());
    }
    fs::write(&temporary, response.body)?;
    if destination.exists() {
        fs::remove_file(&destination)?;
    }
    fs::rename(&temporary, &destination)?;
    Ok(destination)
}

fn parse_assets(root: &Value) -> Vec<ReleaseAsset> {
    let source = root
        .get("assets")
        .or_else(|| root.get("attach_files"))
        .unwrap_or(root);
    let Some(items) = source.as_array() else {
        return Vec::new();
    };
    items
        .iter()
        .filter_map(|item| {
            let name = ["name", "filename", "file_name"]
                .into_iter()
                .map(|key| json_string(item, key))
                .find(|value| !value.is_empty())?;
            let mut url = ["browser_download_url", "download_url"]
                .into_iter()
                .map(|key| json_string(item, key))
                .find(|value| !value.is_empty())
                .unwrap_or_default();
            if url.is_empty()
                && let Some(id) = item.get("id").and_then(Value::as_i64)
            {
                url = format!(
                    "https://gitee.com/hona-cao/fly-ppttimer/attach_files/{id}/download/{name}"
                );
            }
            (!url.is_empty()).then_some(ReleaseAsset {
                name,
                download_url: url,
            })
        })
        .collect()
}

fn json_string(value: &Value, key: &str) -> String {
    value
        .get(key)
        .and_then(Value::as_str)
        .unwrap_or_default()
        .to_owned()
}

fn parse_version(value: &str) -> Option<(u32, u32, u32)> {
    let normalized = value
        .trim()
        .trim_start_matches(['v', 'V'])
        .split(['-', '+'])
        .next()?;
    let mut fields = normalized.split('.');
    let major = fields.next()?.parse().ok()?;
    let minor = fields.next().unwrap_or("0").parse().ok()?;
    let patch = fields.next().unwrap_or("0").parse().ok()?;
    Some((major, minor, patch))
}

fn format_version(version: (u32, u32, u32)) -> String {
    format!("{}.{}.{}", version.0, version.1, version.2)
}

fn same_windows_path(left: &Path, right: &Path) -> bool {
    left.to_string_lossy()
        .trim_end_matches(['\\', '/'])
        .eq_ignore_ascii_case(right.to_string_lossy().trim_end_matches(['\\', '/']))
}

struct HttpResponse {
    status: u32,
    body: Vec<u8>,
}

struct InternetHandle(*mut c_void);

impl InternetHandle {
    fn new(value: *mut c_void, operation: &str) -> Result<Self, std::io::Error> {
        if value.is_null() {
            let error = std::io::Error::last_os_error();
            Err(std::io::Error::new(
                error.kind(),
                format!("{operation}: {error}"),
            ))
        } else {
            Ok(Self(value))
        }
    }
}

impl Drop for InternetHandle {
    fn drop(&mut self) {
        unsafe {
            WinHttpCloseHandle(self.0);
        }
    }
}

fn http_get(url: &str) -> Result<HttpResponse, Box<dyn std::error::Error + Send + Sync>> {
    let parsed = ParsedUrl::parse(url)?;
    let agent = wide(&format!("FlyPPTTimer/{}", env!("CARGO_PKG_VERSION")));
    let session = InternetHandle::new(
        unsafe {
            WinHttpOpen(
                agent.as_ptr(),
                WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY,
                null(),
                null(),
                0,
            )
        },
        "WinHttpOpen",
    )?;
    unsafe {
        WinHttpSetTimeouts(session.0, 20_000, 20_000, 20_000, 20_000);
    }
    let host = wide(&parsed.host);
    let connection = InternetHandle::new(
        unsafe { WinHttpConnect(session.0, host.as_ptr(), parsed.port, 0) },
        "WinHttpConnect",
    )?;
    let verb = wide("GET");
    let path = wide(&parsed.path);
    let request = InternetHandle::new(
        unsafe {
            WinHttpOpenRequest(
                connection.0,
                verb.as_ptr(),
                path.as_ptr(),
                null(),
                null(),
                null(),
                if parsed.secure {
                    WINHTTP_FLAG_SECURE
                } else {
                    0
                },
            )
        },
        "WinHttpOpenRequest",
    )?;
    check_bool(
        unsafe { WinHttpSendRequest(request.0, null(), 0, null(), 0, 0, 0) },
        "WinHttpSendRequest",
    )?;
    check_bool(
        unsafe { WinHttpReceiveResponse(request.0, null_mut()) },
        "WinHttpReceiveResponse",
    )?;
    let mut status = 0u32;
    let mut status_size = std::mem::size_of::<u32>() as u32;
    check_bool(
        unsafe {
            WinHttpQueryHeaders(
                request.0,
                WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
                null(),
                (&mut status as *mut u32).cast(),
                &mut status_size,
                null_mut(),
            )
        },
        "WinHttpQueryHeaders",
    )?;
    let mut body = Vec::new();
    let mut buffer = vec![0u8; 64 * 1024];
    loop {
        let mut read = 0u32;
        check_bool(
            unsafe {
                WinHttpReadData(
                    request.0,
                    buffer.as_mut_ptr().cast(),
                    buffer.len() as u32,
                    &mut read,
                )
            },
            "WinHttpReadData",
        )?;
        if read == 0 {
            break;
        }
        body.extend_from_slice(&buffer[..read as usize]);
    }
    Ok(HttpResponse { status, body })
}

fn check_bool(value: i32, operation: &str) -> Result<(), std::io::Error> {
    if value != 0 {
        return Ok(());
    }
    let error = std::io::Error::last_os_error();
    Err(std::io::Error::new(
        error.kind(),
        format!("{operation}: {error}"),
    ))
}

struct ParsedUrl {
    secure: bool,
    host: String,
    port: u16,
    path: String,
}

impl ParsedUrl {
    fn parse(url: &str) -> Result<Self, &'static str> {
        let (secure, rest, default_port) = if let Some(rest) = url.strip_prefix("https://") {
            (true, rest, 443)
        } else if let Some(rest) = url.strip_prefix("http://") {
            (false, rest, 80)
        } else {
            return Err(
                "\u{4e0d}\u{652f}\u{6301}\u{7684}\u{66f4}\u{65b0}\u{5730}\u{5740}\u{534f}\u{8bae}\u{3002}",
            );
        };
        let (authority, path) = rest
            .split_once('/')
            .map_or((rest, "/".to_owned()), |(host, path)| {
                (host, format!("/{path}"))
            });
        let (host, port) = authority
            .rsplit_once(':')
            .and_then(|(host, port)| port.parse().ok().map(|port| (host, port)))
            .unwrap_or((authority, default_port));
        if host.is_empty() {
            return Err(
                "\u{66f4}\u{65b0}\u{5730}\u{5740}\u{7f3a}\u{5c11}\u{670d}\u{52a1}\u{5668}\u{540d}\u{79f0}\u{3002}",
            );
        }
        Ok(Self {
            secure,
            host: host.to_owned(),
            port,
            path,
        })
    }
}

fn wide(value: &str) -> Vec<u16> {
    value.encode_utf16().chain(std::iter::once(0)).collect()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn version_parser_matches_release_tags() {
        assert_eq!(parse_version("v1.2.3"), Some((1, 2, 3)));
        assert_eq!(parse_version("1.2.3-beta+4"), Some((1, 2, 3)));
        assert_eq!(parse_version("invalid"), None);
    }

    #[test]
    fn installer_selection_matches_v0302_naming() {
        let release = ReleaseInfo {
            version: "1.5.0".into(),
            body: String::new(),
            release_url: RELEASES_URL.into(),
            assets: vec![
                ReleaseAsset {
                    name: "FlyPPTTimer-v1.5.0-portable-win-x64.zip".into(),
                    download_url: "https://example.invalid/portable.zip".into(),
                },
                ReleaseAsset {
                    name: "FlyPPTTimer-v1.5.0-setup-win-x64.exe".into(),
                    download_url: "https://example.invalid/setup.exe".into(),
                },
            ],
        };
        assert_eq!(
            release.installer().map(|asset| asset.name.as_str()),
            Some("FlyPPTTimer-v1.5.0-setup-win-x64.exe")
        );
    }
}
