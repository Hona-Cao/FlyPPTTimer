use std::{
    cell::{Cell, RefCell},
    ffi::c_void,
    fs,
    os::windows::process::CommandExt,
    path::{Path, PathBuf},
    ptr::{null, null_mut},
    sync::{Mutex, mpsc},
    thread,
};

use serde_json::Value;
use slint::ComponentHandle;
use windows_sys::Win32::Networking::WinHttp::{
    WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY, WINHTTP_FLAG_SECURE, WINHTTP_QUERY_FLAG_NUMBER,
    WINHTTP_QUERY_STATUS_CODE, WinHttpCloseHandle, WinHttpConnect, WinHttpOpen, WinHttpOpenRequest,
    WinHttpQueryHeaders, WinHttpReadData, WinHttpReceiveResponse, WinHttpSendRequest,
    WinHttpSetTimeouts,
};

pub const RELEASES_URL: &str = "https://gitee.com/hona-cao/fly-ppttimer/releases";
const LATEST_RELEASE_API: &str =
    "https://gitee.com/api/v5/repos/hona-cao/fly-ppttimer/releases/latest";
const GITHUB_API: &str = "https://api.github.com/repos/Hona-Cao/FlyPPTTimer/releases/latest";
const GITHUB_RELEASES: &str = "https://github.com/Hona-Cao/FlyPPTTimer/releases";
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
    Dismissed,
    Accepted(ReleaseInfo),
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
    window: RefCell<Option<crate::app::UpdateWindow>>,
}

impl UpdateService {
    pub fn new() -> Self {
        let (sender, receiver) = mpsc::channel();
        Self {
            sender,
            receiver: Mutex::new(receiver),
            busy: Cell::new(false),
            window: RefCell::new(None),
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

    pub fn refresh_theme(&self, dark: bool) {
        if let Some(window) = self.window.borrow().as_ref() {
            crate::theme::update(window, dark);
        }
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
    if service.check(user) && user {
        desktop.notify(
            text(
                config,
                "正在检查 Gitee / GitHub 新版本…",
                "Checking Gitee / GitHub for updates...",
            ),
            2000,
        );
    } else if user && let Some(window) = service.window.borrow().as_ref() {
        let _ = window.show();
        crate::window::foreground(window.window());
    }
}

pub fn handle_response_ui(
    response: Response,
    service: &UpdateService,
    config: &crate::config::AppConfig,
    desktop: &crate::desktop::DesktopIntegration,
) {
    match response {
        Response::Dismissed => {
            service.window.borrow_mut().take();
            service.busy.set(false);
        }
        Response::Accepted(release) => {
            service.window.borrow_mut().take();
            if is_installed_edition() && release.installer().is_some() {
                desktop.notify(
                    text(config, "正在下载更新...", "Downloading update..."),
                    3000,
                );
                service.download(release);
            } else {
                crate::settings::native::open_url(&release.release_url);
                service.busy.set(false);
            }
        }
        Response::Checked {
            user_initiated,
            result,
        } => handle_checked(result, user_initiated, service, config),
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
) {
    match result {
        Ok(CheckStatus::NoRelease) if user => crate::settings::native::message(
            text(
                config,
                "Gitee / GitHub \u{9879}\u{76ee}\u{76ee}\u{524d}\u{8fd8}\u{6ca1}\u{6709}\u{53d1}\u{5e03} Release\u{3002}",
                "No accessible release source has published a release yet.",
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
            if prompt_update(service, config, release) {
                return;
            }
        }
        Err(error) if user => show_error(config, &error.to_string()),
        _ => {}
    }
    service.busy.set(false);
}

fn prompt_update(
    service: &UpdateService,
    config: &crate::config::AppConfig,
    release: ReleaseInfo,
) -> bool {
    let window = match crate::app::UpdateWindow::new() {
        Ok(window) => window,
        Err(error) => {
            show_error(config, &error.to_string());
            return false;
        }
    };
    configure_update_window(&window, config, &release);
    let weak = window.as_weak();
    let sender = service.sender.clone();
    window.on_download(move || {
        if let Some(window) = weak.upgrade() {
            let _ = window.hide();
        }
        let _ = sender.send(Response::Accepted(release.clone()));
    });
    let weak = window.as_weak();
    let sender = service.sender.clone();
    window.on_later(move || {
        if let Some(window) = weak.upgrade() {
            let _ = window.hide();
        }
        let _ = sender.send(Response::Dismissed);
    });
    let sender = service.sender.clone();
    window.window().on_close_requested(move || {
        let _ = sender.send(Response::Dismissed);
        slint::CloseRequestResponse::HideWindow
    });
    if let Err(error) = window.show() {
        show_error(config, &error.to_string());
        return false;
    }
    crate::window::center_window_on_cursor(window.window(), window.window().size());
    crate::window::brand_native_window(window.window());
    crate::theme::update(&window, crate::theme::is_dark(&config.ui_theme));
    crate::window::foreground(window.window());
    *service.window.borrow_mut() = Some(window);
    true
}

pub(crate) fn configure_update_window(
    window: &crate::app::UpdateWindow,
    config: &crate::config::AppConfig,
    release: &ReleaseInfo,
) {
    window.set_dark_theme(crate::theme::is_dark(&config.ui_theme));
    window.set_heading(format!("FlyPPTTimer v{}", release.version).into());
    window.set_subtitle(
        format!(
            "{} v{}",
            text(config, "当前版本：", "Current version:"),
            env!("CARGO_PKG_VERSION")
        )
        .into(),
    );
    window.set_notes(release_notes(release).into());
    window.set_later_text(text(config, "稍后", "Later").into());
    window.set_download_text(text(config, "下载新版本", "Download update").into());
    window.set_hint(text(config, "滚动可阅读全部更新内容。ZIP 版本将打开发布页下载，请保留自己的配置。", "Scroll to read all release notes. ZIP editions open the release page for download; settings are kept.").into());
}
fn release_notes(release: &ReleaseInfo) -> String {
    // Do not truncate: even long multilingual release notes must remain readable to the end.
    if release.body.trim().is_empty() {
        return "No release notes were provided. / 此版本未提供更新说明。".into();
    }
    release.body.trim().to_owned()
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
    // Re-enter only the native handoff branch, before single-instance/UI setup.
    // The caller reaches this function only after explicit install confirmation.
    let executable = std::env::current_exe().map_err(|error| error.to_string())?;
    std::process::Command::new(executable)
        .creation_flags(windows_sys::Win32::System::Threading::CREATE_NO_WINDOW)
        .arg("--install-update-after")
        .arg(std::process::id().to_string())
        .arg(path)
        .spawn()
        .map_err(|error| error.to_string())?;
    Ok(())
}

fn check_latest() -> Result<CheckStatus, Box<dyn std::error::Error + Send + Sync>> {
    let (mirror, github) = thread::scope(|scope| {
        let mirror = scope.spawn(|| fetch_release(LATEST_RELEASE_API, RELEASES_URL, true));
        let github = fetch_release(GITHUB_API, GITHUB_RELEASES, false);
        (
            mirror
                .join()
                .unwrap_or_else(|_| Err("Gitee check worker failed".into())),
            github,
        )
    });
    choose_release([mirror, github])
}

type ReleaseResult = Result<Option<ReleaseInfo>, Box<dyn std::error::Error + Send + Sync>>;
fn choose_release(
    results: [ReleaseResult; 2],
) -> Result<CheckStatus, Box<dyn std::error::Error + Send + Sync>> {
    let mut reachable = false;
    let mut best: Option<ReleaseInfo> = None;
    let mut errors = Vec::new();
    for result in results {
        match result {
            Ok(release) => {
                reachable = true;
                if let Some(release) = release
                    && best.as_ref().is_none_or(|current| {
                        parse_version(&release.version) > parse_version(&current.version)
                    })
                {
                    best = Some(release);
                }
            }
            Err(error) => errors.push(error.to_string()),
        }
    }
    if !reachable {
        return Err(errors.join("; ").into());
    }
    match best {
        Some(release)
            if parse_version(&release.version) > parse_version(env!("CARGO_PKG_VERSION")) =>
        {
            Ok(CheckStatus::UpdateAvailable(release))
        }
        Some(_) => Ok(CheckStatus::UpToDate),
        None => Ok(CheckStatus::NoRelease),
    }
}
fn fetch_release(api: &str, releases_url: &str, gitee: bool) -> ReleaseResult {
    let response = http_get(api)?;
    if response.status == 404 {
        return Ok(None);
    }
    if !(200..300).contains(&response.status) {
        return Err(std::io::Error::other(format!("{api}: HTTP {}", response.status)).into());
    }
    let root: Value = serde_json::from_slice(&response.body)?;
    if root.get("draft").and_then(Value::as_bool) == Some(true)
        || root.get("prerelease").and_then(Value::as_bool) == Some(true)
    {
        return Ok(None);
    }
    let tag = json_string(&root, "tag_name");
    let version = parse_version(&tag).ok_or("Release version could not be read")?;
    let mut assets = parse_assets(&root);
    if gitee
        && assets.is_empty()
        && let Some(id) = root.get("id").and_then(Value::as_i64)
        && let Ok(response) = http_get(&format!("{RELEASE_API}/{id}/attach_files"))
        && (200..300).contains(&response.status)
        && let Ok(value) = serde_json::from_slice::<Value>(&response.body)
    {
        assets = parse_assets(&value);
    }
    let url = json_string(&root, "html_url");
    Ok(Some(ReleaseInfo {
        version: format_version(version),
        body: json_string(&root, "body"),
        release_url: if url.is_empty() {
            format!("{releases_url}/tag/{tag}")
        } else {
            url
        },
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
        WinHttpSetTimeouts(session.0, 5_000, 5_000, 8_000, 8_000);
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

    fn release(version: &str, body: &str) -> ReleaseInfo {
        ReleaseInfo {
            version: version.into(),
            body: body.into(),
            release_url: GITHUB_RELEASES.into(),
            assets: Vec::new(),
        }
    }
    #[test]
    fn full_release_notes_and_newest_accessible_source_are_preserved() {
        let long = "A release note line.\n".repeat(500) + "FINAL NOTE";
        let newer = release("9.0.0", &long);
        assert_eq!(release_notes(&newer), long);
        assert_eq!(
            choose_release([Ok(Some(release("1.13.1", "old"))), Ok(Some(newer.clone()))]).unwrap(),
            CheckStatus::UpdateAvailable(newer.clone())
        );
        assert_eq!(
            choose_release([Err("mirror unavailable".into()), Ok(Some(newer))]).unwrap(),
            CheckStatus::UpdateAvailable(release("9.0.0", &long))
        );
        assert_eq!(
            choose_release([
                Ok(None),
                Ok(Some(release(env!("CARGO_PKG_VERSION"), "current")))
            ])
            .unwrap(),
            CheckStatus::UpToDate
        );
        assert!(choose_release([Err("offline".into()), Err("offline".into())]).is_err());
    }

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
