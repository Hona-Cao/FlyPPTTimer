use std::{
    cell::RefCell,
    collections::{HashMap, HashSet},
    io::{Read, Write},
    net::{IpAddr, Ipv4Addr, SocketAddr, TcpListener, TcpStream},
    process::Command,
    sync::{
        Arc, Mutex,
        atomic::{AtomicBool, AtomicI64, AtomicUsize, Ordering},
        mpsc,
    },
    thread,
    time::{Duration, Instant},
};

use serde::{Deserialize, Serialize};
use slint::{Image, Rgba8Pixel, SharedPixelBuffer};

use crate::{
    config::{AppConfig, FileRule},
    presentation::{PresentationApp, PresentationState},
    timer::{TimerMode, TimerSnapshot, TimerState},
};

const INDEX_HTML: &str = include_str!("FlyPPTTimer/Web/index.html");
const APP_CSS: &str = include_str!("FlyPPTTimer/Web/app.css");
const APP_JS: &str = include_str!("FlyPPTTimer/Web/app.js");
const MAX_HEADER_BYTES: usize = 16 * 1024;
const MAX_BODY_BYTES: usize = 64 * 1024;

#[derive(Debug, Clone, Default, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct TimerRemoteState {
    pub mode: String,
    pub state: String,
    pub running: bool,
    pub duration_ms: i64,
    pub elapsed_ms: i64,
    pub remaining_ms: i64,
    pub display_text: String,
    pub is_overtime: bool,
    pub continue_overtime: bool,
    pub window_visible: bool,
    pub muted: bool,
    pub time_up_blackout_active: bool,
    pub rule_count: usize,
}

#[derive(Debug, Clone, Default, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct PresentationOption {
    pub id: String,
    pub name: String,
    pub directory: String,
    pub is_open: bool,
    pub is_active: bool,
    pub is_slide_show_running: bool,
    pub is_managed: bool,
}

#[derive(Debug, Clone, Default, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct PresentationRemoteState {
    pub power_point_installed: bool,
    pub power_point_running: bool,
    pub has_presentation: bool,
    pub is_slide_show_running: bool,
    pub presentation_name: String,
    pub presentation_path: String,
    pub current_slide: i32,
    pub total_slides: i32,
    pub screen_mode: String,
    pub updated_at: String,
    pub error: String,
    pub presentations: Vec<PresentationOption>,
    pub operation: String,
    pub operation_message: String,
    pub operation_started_at: Option<String>,
    pub operation_id: String,
    pub is_operation_busy: bool,
    pub is_current_presentation_managed: bool,
    pub open_presentation_count: usize,
    pub wps_detected: bool,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct RemoteState {
    pub ok: bool,
    pub message: String,
    pub timer_state: TimerRemoteState,
    pub presentation_state: PresentationRemoteState,
    // v0.10 compatible flat timer fields.
    pub mode: String,
    pub state: String,
    pub running: bool,
    pub duration_ms: i64,
    pub elapsed_ms: i64,
    pub remaining_ms: i64,
    pub display_text: String,
    pub is_overtime: bool,
    pub window_visible: bool,
    pub muted: bool,
    pub time_up_blackout_active: bool,
    pub rule_count: usize,
    pub connected_clients: usize,
    pub version: String,
    pub revision: i64,
}

impl Default for RemoteState {
    fn default() -> Self {
        Self {
            ok: true,
            message: String::new(),
            timer_state: TimerRemoteState::default(),
            presentation_state: PresentationRemoteState::default(),
            mode: String::new(),
            state: String::new(),
            running: false,
            duration_ms: 0,
            elapsed_ms: 0,
            remaining_ms: 0,
            display_text: String::new(),
            is_overtime: false,
            window_visible: false,
            muted: false,
            time_up_blackout_active: false,
            rule_count: 0,
            connected_clients: 0,
            version: env!("CARGO_PKG_VERSION").to_owned(),
            revision: 0,
        }
    }
}

#[derive(Debug, Clone, Default, Deserialize)]
#[serde(default, rename_all = "camelCase")]
pub struct RemoteCommand {
    pub command: String,
    pub duration: Option<String>,
    pub duration_ms: Option<i64>,
    pub mode: Option<String>,
    pub slide_number: Option<i32>,
    pub presentation_id: Option<String>,
    pub confirmed: Option<bool>,
    pub sync_all_rules: Option<bool>,
    pub operation_id: Option<String>,
}

pub struct RemoteRequest {
    pub command: RemoteCommand,
    pub reply: mpsc::SyncSender<Result<(RemoteState, String), String>>,
}

#[derive(Debug, Clone, Default)]
pub struct RemoteInfo {
    pub running: bool,
    pub status: String,
    pub current_port: u16,
    pub connected_clients: usize,
    pub error: String,
}

struct ServerInstance {
    stop: Arc<AtomicBool>,
    thread: thread::JoinHandle<()>,
}

pub struct RemoteServer {
    instance: RefCell<Option<ServerInstance>>,
    shared_state: Arc<Mutex<RemoteState>>,
    info: Arc<Mutex<RemoteInfo>>,
    token: Arc<Mutex<String>>,
    clients: Arc<Mutex<HashMap<IpAddr, Instant>>>,
    revision: Arc<AtomicI64>,
    sender: mpsc::Sender<RemoteRequest>,
}

impl RemoteServer {
    pub fn new(sender: mpsc::Sender<RemoteRequest>) -> Self {
        Self {
            instance: RefCell::new(None),
            shared_state: Arc::new(Mutex::new(RemoteState::default())),
            info: Arc::new(Mutex::new(RemoteInfo::default())),
            token: Arc::new(Mutex::new(String::new())),
            clients: Arc::new(Mutex::new(HashMap::new())),
            revision: Arc::new(AtomicI64::new(0)),
            sender,
        }
    }

    pub fn start(&self, config: &mut AppConfig) -> Result<u16, String> {
        self.stop();
        *self.token.lock().unwrap() = config.remote_control.token.clone();
        if !config.remote_control.enabled {
            self.info.lock().unwrap().status = "未启动".to_owned();
            return Ok(0);
        }
        let requested = if config.remote_control.use_random_port || config.remote_control.port == 0
        {
            0
        } else {
            config.remote_control.port
        };
        let listener = TcpListener::bind((Ipv4Addr::UNSPECIFIED, requested)).map_err(|error| {
            let mut info = self.info.lock().unwrap();
            info.running = false;
            info.status = "启动失败".to_owned();
            info.error = error.to_string();
            format!("远程服务启动失败：{error}")
        })?;
        listener
            .set_nonblocking(true)
            .map_err(|error| error.to_string())?;
        let port = listener
            .local_addr()
            .map_err(|error| error.to_string())?
            .port();
        config.remote_control.port = port;
        let stop = Arc::new(AtomicBool::new(false));
        let stop_thread = Arc::clone(&stop);
        let state = Arc::clone(&self.shared_state);
        let info = Arc::clone(&self.info);
        let token = Arc::clone(&self.token);
        let clients = Arc::clone(&self.clients);
        let revision = Arc::clone(&self.revision);
        let sender = self.sender.clone();
        let active = Arc::new(AtomicUsize::new(0));
        let active_thread = Arc::clone(&active);
        {
            let mut current = self.info.lock().unwrap();
            *current = RemoteInfo {
                running: true,
                status: "已启动".to_owned(),
                current_port: port,
                connected_clients: 0,
                error: String::new(),
            };
        }
        self.revision.fetch_add(1, Ordering::Relaxed);
        let worker = thread::Builder::new()
            .name("flyppttimer-remote".to_owned())
            .spawn(move || {
                while !stop_thread.load(Ordering::Relaxed) {
                    match listener.accept() {
                        Ok((stream, address)) => {
                            if active_thread.fetch_add(1, Ordering::Relaxed) >= 16 {
                                active_thread.fetch_sub(1, Ordering::Relaxed);
                                continue;
                            }
                            let context = ConnectionContext {
                                state: Arc::clone(&state),
                                token: Arc::clone(&token),
                                clients: Arc::clone(&clients),
                                revision: Arc::clone(&revision),
                                sender: sender.clone(),
                            };
                            let active = Arc::clone(&active_thread);
                            let _ = thread::Builder::new()
                                .name("flyppttimer-remote-client".to_owned())
                                .spawn(move || {
                                    handle_connection(stream, address, context);
                                    active.fetch_sub(1, Ordering::Relaxed);
                                });
                        }
                        Err(error) if error.kind() == std::io::ErrorKind::WouldBlock => {
                            thread::sleep(Duration::from_millis(25))
                        }
                        Err(error) => {
                            let mut current = info.lock().unwrap();
                            current.error = error.to_string();
                            break;
                        }
                    }
                }
            })
            .map_err(|error| error.to_string())?;
        *self.instance.borrow_mut() = Some(ServerInstance {
            stop,
            thread: worker,
        });
        Ok(port)
    }

    pub fn stop(&self) {
        if let Some(instance) = self.instance.borrow_mut().take() {
            instance.stop.store(true, Ordering::Relaxed);
            let _ = instance.thread.join();
        }
        self.clients.lock().unwrap().clear();
        let mut info = self.info.lock().unwrap();
        info.running = false;
        info.status = "未启动".to_owned();
        info.connected_clients = 0;
        self.revision.fetch_add(1, Ordering::Relaxed);
    }

    pub fn apply_enabled(&self, config: &mut AppConfig) -> Result<(), String> {
        *self.token.lock().unwrap() = config.remote_control.token.clone();
        let running = self.info().running;
        if config.remote_control.enabled && !running {
            self.start(config)?;
        }
        if !config.remote_control.enabled && running {
            self.stop();
        }
        Ok(())
    }

    pub fn update_state(&self, mut state: RemoteState) {
        prune_clients(&self.clients);
        let count = self.clients.lock().unwrap().len();
        state.connected_clients = count;
        state.revision = self.revision.load(Ordering::Relaxed);
        *self.shared_state.lock().unwrap() = state;
        let mut info = self.info.lock().unwrap();
        info.connected_clients = count;
    }

    pub fn info(&self) -> RemoteInfo {
        self.info.lock().unwrap().clone()
    }

    pub fn regenerate_token(&self) -> String {
        let token = generate_token();
        *self.token.lock().unwrap() = token.clone();
        self.clients.lock().unwrap().clear();
        self.revision.fetch_add(1, Ordering::Relaxed);
        token
    }

    pub fn disconnect_all(&self) -> String {
        self.regenerate_token()
    }
}

impl Drop for RemoteServer {
    fn drop(&mut self) {
        self.stop();
    }
}

struct ConnectionContext {
    state: Arc<Mutex<RemoteState>>,
    token: Arc<Mutex<String>>,
    clients: Arc<Mutex<HashMap<IpAddr, Instant>>>,
    revision: Arc<AtomicI64>,
    sender: mpsc::Sender<RemoteRequest>,
}

fn handle_connection(mut stream: TcpStream, address: SocketAddr, context: ConnectionContext) {
    let _ = stream.set_read_timeout(Some(Duration::from_secs(8)));
    let _ = stream.set_write_timeout(Some(Duration::from_secs(8)));
    let result = read_request(&mut stream).and_then(|request| route(request, address, &context));
    let response = result.unwrap_or_else(|error| {
        HttpResponse::json(400, &serde_json::json!({"ok":false,"error":error}))
    });
    let _ = write_response(&mut stream, response);
}

struct HttpRequest {
    method: String,
    raw_url: String,
    body: Vec<u8>,
}
struct HttpResponse {
    status: u16,
    content_type: &'static str,
    body: Vec<u8>,
}
impl HttpResponse {
    fn text(status: u16, content_type: &'static str, value: String) -> Self {
        Self {
            status,
            content_type,
            body: value.into_bytes(),
        }
    }
    fn json<T: Serialize>(status: u16, value: &T) -> Self {
        Self::text(
            status,
            "application/json; charset=utf-8",
            serde_json::to_string(value).unwrap_or_else(|_| "{}".to_owned()),
        )
    }
}

fn read_request(stream: &mut TcpStream) -> Result<HttpRequest, String> {
    let mut data = Vec::new();
    let mut buffer = [0u8; 4096];
    let header_end;
    loop {
        let count = stream
            .read(&mut buffer)
            .map_err(|error| error.to_string())?;
        if count == 0 {
            return Err("请求不完整".to_owned());
        }
        data.extend_from_slice(&buffer[..count]);
        if let Some(position) = find_bytes(&data, b"\r\n\r\n") {
            header_end = position + 4;
            break;
        }
        if data.len() > MAX_HEADER_BYTES {
            return Err("请求头过大".to_owned());
        }
    }
    let header =
        std::str::from_utf8(&data[..header_end]).map_err(|_| "请求头编码无效".to_owned())?;
    let mut lines = header.lines();
    let mut request_line = lines.next().unwrap_or_default().split_whitespace();
    let method = request_line.next().unwrap_or_default().to_owned();
    let raw_url = request_line.next().unwrap_or_default().to_owned();
    let length = lines
        .find_map(|line| {
            line.split_once(':')
                .filter(|(name, _)| name.eq_ignore_ascii_case("content-length"))
                .and_then(|(_, value)| value.trim().parse::<usize>().ok())
        })
        .unwrap_or(0);
    if length > MAX_BODY_BYTES {
        return Err("请求体过大".to_owned());
    }
    while data.len() < header_end + length {
        let count = stream
            .read(&mut buffer)
            .map_err(|error| error.to_string())?;
        if count == 0 {
            break;
        }
        data.extend_from_slice(&buffer[..count]);
    }
    Ok(HttpRequest {
        method,
        raw_url,
        body: data[header_end..data.len().min(header_end + length)].to_vec(),
    })
}

fn route(
    request: HttpRequest,
    address: SocketAddr,
    context: &ConnectionContext,
) -> Result<HttpResponse, String> {
    let (path, supplied) = split_url(&request.raw_url);
    if !fixed_time_token_equals(&supplied, &context.token.lock().unwrap()) {
        return Ok(HttpResponse::json(
            403,
            &serde_json::json!({"ok":false,"error":"令牌无效或远程控制已关闭"}),
        ));
    }
    context
        .clients
        .lock()
        .unwrap()
        .insert(address.ip(), Instant::now());
    prune_clients(&context.clients);
    match (request.method.as_str(), path.as_str()) {
        ("GET", "/") | ("GET", "/index.html") => {
            let token = context
                .token
                .lock()
                .unwrap()
                .replace('\\', "\\\\")
                .replace('\'', "\\'")
                .replace(['\r', '\n'], "");
            Ok(HttpResponse::text(
                200,
                "text/html; charset=utf-8",
                INDEX_HTML.replace("__FLYPPT_TOKEN__", &token),
            ))
        }
        ("GET", "/assets/app.css") => Ok(HttpResponse::text(
            200,
            "text/css; charset=utf-8",
            APP_CSS.to_owned(),
        )),
        ("GET", "/assets/app.js") => Ok(HttpResponse::text(
            200,
            "application/javascript; charset=utf-8",
            APP_JS.to_owned(),
        )),
        ("GET", "/state") => {
            let mut state = context.state.lock().unwrap().clone();
            state.connected_clients = context.clients.lock().unwrap().len();
            state.revision = context.revision.load(Ordering::Relaxed);
            Ok(HttpResponse::json(200, &state))
        }
        ("POST", "/command") => {
            let command: RemoteCommand =
                serde_json::from_slice(&request.body).map_err(|error| error.to_string())?;
            let (reply, receiver) = mpsc::sync_channel(1);
            context
                .sender
                .send(RemoteRequest { command, reply })
                .map_err(|_| "远程命令服务已关闭".to_owned())?;
            match receiver.recv_timeout(Duration::from_secs(20)) {
                Ok(Ok((mut state, message))) => {
                    let revision = context.revision.fetch_add(1, Ordering::Relaxed) + 1;
                    state.ok = true;
                    state.message = message;
                    state.connected_clients = context.clients.lock().unwrap().len();
                    state.revision = revision;
                    Ok(HttpResponse::json(200, &state))
                }
                Ok(Err(error)) => {
                    let mut state = context.state.lock().unwrap().clone();
                    state.ok = false;
                    state.message = error;
                    Ok(HttpResponse::json(400, &state))
                }
                Err(_) => Ok(HttpResponse::json(
                    503,
                    &serde_json::json!({"ok":false,"message":"命令响应超时"}),
                )),
            }
        }
        _ => Ok(HttpResponse::json(
            404,
            &serde_json::json!({"ok":false,"error":"未找到"}),
        )),
    }
}

fn write_response(stream: &mut TcpStream, response: HttpResponse) -> std::io::Result<()> {
    let reason = if response.status == 200 {
        "OK"
    } else {
        "ERROR"
    };
    write!(
        stream,
        "HTTP/1.1 {} {}\r\nContent-Type: {}\r\nContent-Length: {}\r\nCache-Control: no-store\r\nContent-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self'; connect-src 'self'; img-src 'self' data:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'\r\nReferrer-Policy: no-referrer\r\nX-Content-Type-Options: nosniff\r\nConnection: close\r\n\r\n",
        response.status,
        reason,
        response.content_type,
        response.body.len()
    )?;
    stream.write_all(&response.body)
}

fn split_url(raw: &str) -> (String, String) {
    let (path, query) = raw.split_once('?').unwrap_or((raw, ""));
    let token = query
        .split('&')
        .find_map(|pair| {
            pair.split_once('=')
                .filter(|(key, _)| *key == "token")
                .map(|(_, value)| percent_decode(value))
        })
        .unwrap_or_default();
    (path.to_owned(), token)
}

fn percent_decode(value: &str) -> String {
    let bytes = value.as_bytes();
    let mut result = Vec::new();
    let mut index = 0;
    while index < bytes.len() {
        if bytes[index] == b'%'
            && index + 2 < bytes.len()
            && let Ok(byte) = u8::from_str_radix(&value[index + 1..index + 3], 16)
        {
            result.push(byte);
            index += 3;
        } else {
            result.push(if bytes[index] == b'+' {
                b' '
            } else {
                bytes[index]
            });
            index += 1;
        }
    }
    String::from_utf8_lossy(&result).into_owned()
}

fn find_bytes(data: &[u8], needle: &[u8]) -> Option<usize> {
    data.windows(needle.len())
        .position(|window| window == needle)
}
fn fixed_time_token_equals(left: &str, right: &str) -> bool {
    let left = left.as_bytes();
    let right = right.as_bytes();
    let mut difference = left.len() ^ right.len();
    for index in 0..left.len().max(right.len()) {
        difference |= usize::from(*left.get(index).unwrap_or(&0) ^ *right.get(index).unwrap_or(&0));
    }
    difference == 0
}
fn prune_clients(clients: &Arc<Mutex<HashMap<IpAddr, Instant>>>) {
    clients
        .lock()
        .unwrap()
        .retain(|_, seen| seen.elapsed() <= Duration::from_secs(30));
}

pub fn generate_token() -> String {
    let mut bytes = [0u8; 24];
    let status = unsafe {
        windows_sys::Win32::Security::Cryptography::BCryptGenRandom(
            std::ptr::null_mut(),
            bytes.as_mut_ptr(),
            bytes.len() as u32,
            2,
        )
    };
    assert_eq!(status, 0, "Windows random token generation failed");
    bytes.iter().map(|byte| format!("{byte:02x}")).collect()
}

pub fn lan_addresses() -> Vec<String> {
    let mut addresses = HashSet::new();
    if let Ok(output) = Command::new("ipconfig").output() {
        let text = String::from_utf8_lossy(&output.stdout);
        let mut adapter = String::new();
        for line in text.lines() {
            if !line.starts_with(char::is_whitespace) && line.trim_end().ends_with(':') {
                adapter = line.to_lowercase();
            }
            if [
                "virtual",
                "vmware",
                "hyper-v",
                "virtualbox",
                "clash",
                "tun",
                "wintun",
                "proxy",
            ]
            .iter()
            .any(|name| adapter.contains(name))
            {
                continue;
            }
            for candidate in
                line.split(|character: char| !character.is_ascii_digit() && character != '.')
            {
                if let Ok(IpAddr::V4(ip)) = candidate.parse::<IpAddr>()
                    && is_lan(ip)
                {
                    addresses.insert(ip);
                }
            }
        }
    }
    let mut result: Vec<_> = addresses.into_iter().map(|ip| ip.to_string()).collect();
    result.sort();
    result
}

pub fn mask_token(url: &str) -> String {
    let lower = url.to_ascii_lowercase();
    let Some(index) = lower.find("token=") else {
        return url.to_owned();
    };
    format!("{}••••••", &url[..index + "token=".len()])
}
fn is_lan(ip: Ipv4Addr) -> bool {
    let octets = ip.octets();
    octets[0] == 10
        || (octets[0] == 172 && (16..=31).contains(&octets[1]))
        || (octets[0] == 192 && octets[1] == 168)
}

pub fn qr_image(value: &str) -> Image {
    use qrcode::{Color as QrColor, QrCode};
    let Ok(code) = QrCode::new(value.as_bytes()) else {
        return Image::default();
    };
    let quiet = 4usize;
    let modules = code.width() + quiet * 2;
    let scale = (192usize / modules).max(2);
    let size = (modules * scale) as u32;
    let mut buffer = SharedPixelBuffer::<Rgba8Pixel>::new(size, size);
    for y in 0..size as usize {
        for x in 0..size as usize {
            let module_x = x / scale;
            let module_y = y / scale;
            let dark = module_x >= quiet
                && module_y >= quiet
                && module_x < modules - quiet
                && module_y < modules - quiet
                && code[(module_x - quiet, module_y - quiet)] == QrColor::Dark;
            buffer.make_mut_slice()[y * size as usize + x] = if dark {
                Rgba8Pixel {
                    r: 15,
                    g: 23,
                    b: 42,
                    a: 255,
                }
            } else {
                Rgba8Pixel {
                    r: 255,
                    g: 255,
                    b: 255,
                    a: 255,
                }
            };
        }
    }
    Image::from_rgba8(buffer)
}

pub fn copy_text(value: &str) -> Result<(), String> {
    use clipboard_win::{Clipboard, Setter, formats::Unicode};
    let _clipboard = Clipboard::new_attempts(10).map_err(|error| error.to_string())?;
    Unicode
        .write_clipboard(&value)
        .map(|_| ())
        .map_err(|error| error.to_string())
}

pub fn open_url(value: &str) -> Result<(), String> {
    use windows_sys::Win32::UI::Shell::ShellExecuteW;
    use windows_sys::Win32::UI::WindowsAndMessaging::SW_SHOWNORMAL;
    let operation: Vec<u16> = "open".encode_utf16().chain(Some(0)).collect();
    let target: Vec<u16> = value.encode_utf16().chain(Some(0)).collect();
    let result = unsafe {
        ShellExecuteW(
            std::ptr::null_mut(),
            operation.as_ptr(),
            target.as_ptr(),
            std::ptr::null(),
            std::ptr::null(),
            SW_SHOWNORMAL,
        )
    };
    if result as usize <= 32 {
        Err(format!("打开控制页失败（{result:?}）"))
    } else {
        Ok(())
    }
}

pub fn firewall_command(port: u16) -> String {
    let executable = std::env::current_exe()
        .map(|path| path.display().to_string())
        .unwrap_or_default();
    format!(
        "netsh advfirewall firewall add rule name=\"FlyPPTTimer Remote {port}\" dir=in action=allow program=\"{executable}\" protocol=TCP localport={port}"
    )
}

pub fn remote_state(
    snapshot: &TimerSnapshot,
    config: &AppConfig,
    presentation: &PresentationState,
    display_text: String,
    muted: bool,
    time_up: bool,
) -> RemoteState {
    let mode = if snapshot.mode == TimerMode::Countdown {
        "倒计时"
    } else {
        "正计时"
    }
    .to_owned();
    let state_text = match snapshot.state {
        TimerState::Running => "运行中",
        TimerState::Paused => "暂停",
        TimerState::Finished => "已结束",
        TimerState::Stopped => "停止",
    }
    .to_owned();
    let timer_state = TimerRemoteState {
        mode: mode.clone(),
        state: state_text.clone(),
        running: snapshot.state == TimerState::Running,
        duration_ms: snapshot.duration.as_millis() as i64,
        elapsed_ms: snapshot.elapsed.as_millis() as i64,
        remaining_ms: snapshot.remaining.as_millis() as i64,
        display_text: display_text.clone(),
        is_overtime: snapshot.is_overtime,
        continue_overtime: config.timer.continue_overtime,
        window_visible: config.placement.visible,
        muted,
        time_up_blackout_active: time_up,
        rule_count: config.rules.len(),
    };
    let presentation_state = presentation_remote_state(presentation, &config.rules);
    RemoteState {
        ok: true,
        message: String::new(),
        timer_state: timer_state.clone(),
        presentation_state,
        mode,
        state: state_text,
        running: timer_state.running,
        duration_ms: timer_state.duration_ms,
        elapsed_ms: timer_state.elapsed_ms,
        remaining_ms: timer_state.remaining_ms,
        display_text,
        is_overtime: timer_state.is_overtime,
        window_visible: timer_state.window_visible,
        muted,
        time_up_blackout_active: time_up,
        rule_count: config.rules.len(),
        connected_clients: 0,
        version: env!("CARGO_PKG_VERSION").to_owned(),
        revision: 0,
    }
}

fn presentation_remote_state(
    state: &PresentationState,
    rules: &[FileRule],
) -> PresentationRemoteState {
    let mut options = Vec::new();
    for item in &state.presentations {
        options.push(PresentationOption {
            id: id_for_path(&item.path),
            name: item.name.clone(),
            directory: std::path::Path::new(&item.path)
                .parent()
                .map(|p| p.display().to_string())
                .unwrap_or_default(),
            is_open: true,
            is_active: item.active,
            is_slide_show_running: item.slide_show_running,
            is_managed: item.managed,
        });
    }
    for rule in rules.iter().filter(|rule| rule.enabled) {
        if options
            .iter()
            .any(|option| option.id == id_for_path(&rule.file_path))
        {
            continue;
        }
        options.push(PresentationOption {
            id: id_for_path(&rule.file_path),
            name: std::path::Path::new(&rule.file_path)
                .file_name()
                .map(|v| v.to_string_lossy().into_owned())
                .unwrap_or_default(),
            directory: std::path::Path::new(&rule.file_path)
                .parent()
                .map(|p| p.display().to_string())
                .unwrap_or_default(),
            is_managed: true,
            ..PresentationOption::default()
        });
    }
    PresentationRemoteState {
        power_point_installed: state.powerpoint_installed,
        power_point_running: state.running,
        has_presentation: state.has_presentation,
        is_slide_show_running: state.slide_show_running,
        presentation_name: state.presentation_name.clone(),
        presentation_path: state.presentation_path.clone(),
        current_slide: state.current_slide,
        total_slides: state.total_slides,
        screen_mode: match state.screen_state {
            3 => "黑屏",
            4 => "白屏",
            _ => "正常",
        }
        .to_owned(),
        updated_at: utc_timestamp(),
        error: state.error.clone(),
        presentations: options,
        operation: "Idle".to_owned(),
        operation_message: state.message.clone(),
        operation_started_at: None,
        operation_id: String::new(),
        is_operation_busy: false,
        is_current_presentation_managed: state.managed,
        open_presentation_count: state.presentations.len(),
        wps_detected: state.application == Some(PresentationApp::Wps),
    }
}

fn utc_timestamp() -> String {
    use windows_sys::Win32::{Foundation::SYSTEMTIME, System::SystemInformation::GetSystemTime};
    let mut time = unsafe { std::mem::zeroed::<SYSTEMTIME>() };
    unsafe { GetSystemTime(&mut time) };
    format!(
        "{:04}-{:02}-{:02}T{:02}:{:02}:{:02}.{:03}Z",
        time.wYear,
        time.wMonth,
        time.wDay,
        time.wHour,
        time.wMinute,
        time.wSecond,
        time.wMilliseconds
    )
}

// Stable, case-insensitive path identity used consistently by state and commands.
pub fn id_for_path(path: &str) -> String {
    let normalized = std::path::absolute(path).unwrap_or_else(|_| std::path::PathBuf::from(path));
    normalized
        .to_string_lossy()
        .trim_end_matches(['\\', '/'])
        .to_uppercase()
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn token_comparison_rejects_wrong_values() {
        assert!(fixed_time_token_equals("abc", "abc"));
        assert!(!fixed_time_token_equals("abc", "abd"));
        assert!(!fixed_time_token_equals("abc", "abc0"));
    }
    #[test]
    fn parses_duration_and_mode_command() {
        let command: RemoteCommand = serde_json::from_str(
            r#"{"command":"timer.setDuration","durationMs":300000,"mode":"countup"}"#,
        )
        .unwrap();
        assert_eq!(command.duration_ms, Some(300000));
        assert_eq!(command.mode.as_deref(), Some("countup"));
    }
    #[test]
    fn path_identity_ignores_case() {
        assert_eq!(
            id_for_path(r"C:\Demo\Talk.pptx"),
            id_for_path(r"c:\demo\talk.PPTX")
        );
        assert_eq!(id_for_path(r"C:\Demo\Talk.pptx"), r"C:\DEMO\TALK.PPTX");
    }
    #[test]
    fn generated_token_matches_v0302_shape() {
        let token = generate_token();
        assert_eq!(token.len(), 48);
        assert!(
            token
                .bytes()
                .all(|byte| byte.is_ascii_hexdigit() && !byte.is_ascii_uppercase())
        );
    }
    #[test]
    fn qr_is_generated_for_remote_url() {
        let image = qr_image("http://192.168.1.2:4080/?token=abc");
        assert!(image.size().width > 0);
    }
    #[test]
    fn random_port_binds_and_is_saved_for_this_start() {
        let (sender, _receiver) = mpsc::channel();
        let server = RemoteServer::new(sender);
        let mut config = AppConfig::default();
        config.remote_control.token = generate_token();
        config.remote_control.use_random_port = true;
        let port = server.start(&mut config).unwrap();
        assert!(port > 0);
        assert_eq!(config.remote_control.port, port);
        server.stop();
    }
}
