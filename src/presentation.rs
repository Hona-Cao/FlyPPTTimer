use std::{
    collections::{HashMap, HashSet},
    path::{Path, PathBuf},
    sync::{Arc, Mutex, mpsc},
    thread,
    time::Duration,
};

use windows::{
    Win32::System::{
        Com::{
            CLSCTX_LOCAL_SERVER, CLSIDFromProgID, COINIT_APARTMENTTHREADED, CoCreateGuid,
            CoCreateInstance, CoInitializeEx, DISPATCH_METHOD, DISPATCH_PROPERTYGET,
            DISPATCH_PROPERTYPUT, DISPPARAMS, IDispatch,
        },
        Ole::GetActiveObject,
        Variant::VARIANT,
    },
    core::{BSTR, Interface, PCWSTR},
};

use crate::config::{AppConfig, FileRule, TimerMode};

const SLIDE_SHOW_RUNNING: i32 = 1;
const SLIDE_SHOW_BLACK: i32 = 3;
const SLIDE_SHOW_WHITE: i32 = 4;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum PresentationApp {
    PowerPoint,
    Wps,
}

#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct PresentationState {
    pub powerpoint_installed: bool,
    pub wps_installed: bool,
    pub application: Option<PresentationApp>,
    pub running: bool,
    pub has_presentation: bool,
    pub slide_show_running: bool,
    pub presentation_name: String,
    pub presentation_path: String,
    pub current_slide: i32,
    pub total_slides: i32,
    pub screen_state: i32,
    pub managed: bool,
    pub message: String,
    pub error: String,
    pub presentations: Vec<OpenPresentation>,
    pub operation: PresentationOperation,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct PresentationOperation {
    pub name: String,
    pub message: String,
    pub started_at: Option<String>,
    pub id: String,
    pub busy: bool,
}

impl Default for PresentationOperation {
    fn default() -> Self {
        Self {
            name: "Idle".into(),
            message: String::new(),
            started_at: None,
            id: String::new(),
            busy: false,
        }
    }
}

impl PresentationOperation {
    fn finish(&mut self, result: &Result<String, String>) {
        match result {
            Ok(message) => {
                *self = Self {
                    message: message.clone(),
                    ..Self::default()
                }
            }
            Err(error) => {
                self.name = "Failed".into();
                self.message = error.clone();
                self.busy = false;
            }
        }
    }
}

#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct OpenPresentation {
    pub name: String,
    pub path: String,
    pub active: bool,
    pub slide_show_running: bool,
    pub managed: bool,
}

#[allow(dead_code)]
pub enum PresentationCommand {
    Refresh,
    Open(PathBuf),
    StartFromBeginning(Option<PathBuf>),
    StartFromCurrent(Option<PathBuf>),
    Previous,
    Next,
    GoToSlide(i32),
    ToggleBlackScreen,
    ToggleWhiteScreen,
    RestoreScreen,
    EndShow,
    CloseActive,
    CloseLastOpened,
    ExitApplication,
    ForceQuitAll { confirmed: bool },
}

impl PresentationCommand {
    fn operation(&self) -> (&'static str, &'static str) {
        match self {
            Self::Open(_) => ("OpeningPresentation", "正在打开演示文稿"),
            Self::StartFromBeginning(_) | Self::StartFromCurrent(_) => {
                ("StartingSlideshow", "正在启动放映")
            }
            Self::EndShow => ("StoppingSlideshow", "正在结束放映"),
            Self::CloseActive => ("ClosingPresentation", "正在关闭当前文稿"),
            Self::CloseLastOpened => ("ClosingPresentation", "正在关闭最后打开的文稿"),
            Self::ForceQuitAll { .. } => ("ForceExitingApplication", "正在强制退出演示程序"),
            _ => ("Idle", "正在执行演示命令"),
        }
    }
}

enum Request {
    Execute(
        PresentationCommand,
        String,
        mpsc::SyncSender<Result<String, String>>,
    ),
    Stop,
}

pub struct PresentationService {
    sender: mpsc::Sender<Request>,
    state: Arc<Mutex<PresentationState>>,
    thread: Option<thread::JoinHandle<()>>,
}

impl PresentationService {
    pub fn start() -> Result<Self, String> {
        let (sender, receiver) = mpsc::channel();
        let state = Arc::new(Mutex::new(PresentationState::default()));
        let state_for_thread = Arc::clone(&state);
        let thread = thread::Builder::new()
            .name("flyppttimer-presentation-sta".to_owned())
            .spawn(move || presentation_thread(receiver, state_for_thread))
            .map_err(|error| error.to_string())?;
        Ok(Self {
            sender,
            state,
            thread: Some(thread),
        })
    }

    pub fn state(&self) -> PresentationState {
        self.state.lock().unwrap().clone()
    }

    #[allow(dead_code)]
    pub fn execute(&self, command: PresentationCommand) -> Result<String, String> {
        let (sender, receiver) = mpsc::sync_channel(1);
        self.enqueue(command, sender)?;
        receiver
            .recv_timeout(Duration::from_secs(15))
            .map_err(|_| "PowerPoint 响应超时，计时遥控仍可继续使用。".to_owned())?
    }

    pub fn queue(&self, command: PresentationCommand) -> Result<String, String> {
        let (sender, _receiver) = mpsc::sync_channel(1);
        self.enqueue(command, sender)
    }

    fn enqueue(
        &self,
        command: PresentationCommand,
        reply: mpsc::SyncSender<Result<String, String>>,
    ) -> Result<String, String> {
        if matches!(
            command,
            PresentationCommand::ForceQuitAll { confirmed: false }
        ) {
            return Err("强制退出会丢失所有未保存内容，请再次确认。".into());
        }
        let mut state = self.state.lock().unwrap();
        if state.operation.busy {
            return Err("演示操作正在进行，请等待当前操作完成。".into());
        }
        let (name, message) = command.operation();
        let id = format!(
            "{:032x}",
            unsafe { CoCreateGuid() }
                .map_err(|e| e.to_string())?
                .to_u128()
        );
        state.operation = PresentationOperation {
            name: name.into(),
            message: message.into(),
            started_at: Some(crate::remote::utc_timestamp()),
            id: id.clone(),
            busy: name != "Idle",
        };
        if self
            .sender
            .send(Request::Execute(command, id, reply))
            .is_err()
        {
            let result = Err("演示控制服务已关闭。".to_owned());
            state.operation.finish(&result);
            return result;
        }
        Ok(message.into())
    }
}

impl Drop for PresentationService {
    fn drop(&mut self) {
        let _ = self.sender.send(Request::Stop);
        if let Some(thread) = self.thread.take() {
            let _ = thread.join();
        }
    }
}

fn presentation_thread(receiver: mpsc::Receiver<Request>, state: Arc<Mutex<PresentationState>>) {
    let _ = unsafe { CoInitializeEx(None, COINIT_APARTMENTTHREADED) };
    let mut session = Session::default();
    loop {
        match receiver.recv_timeout(Duration::from_millis(500)) {
            Ok(Request::Execute(command, operation_id, reply)) => {
                let result = session.execute(command);
                match &result {
                    Ok(message) => session.message = message.clone(),
                    Err(error) => session.message = error.clone(),
                }
                let mut current = session.read_state();
                if result.is_err() {
                    current.error = session.message.clone();
                }
                let mut shared = state.lock().unwrap();
                if shared.operation.id == operation_id {
                    shared.operation.finish(&result);
                }
                current.operation = shared.operation.clone();
                *shared = current;
                let _ = reply.send(result);
            }
            Ok(Request::Stop) | Err(mpsc::RecvTimeoutError::Disconnected) => break,
            Err(mpsc::RecvTimeoutError::Timeout) => {
                let mut current = session.read_state();
                let mut shared = state.lock().unwrap();
                current.operation = shared.operation.clone();
                *shared = current;
            }
        }
    }
}

#[derive(Default)]
struct Session {
    managed_paths: HashSet<String>,
    opened_order: Vec<String>,
    created_application: Option<PresentationApp>,
    message: String,
}

impl Session {
    fn read_state(&self) -> PresentationState {
        let (powerpoint_installed, wps_installed) = installed_applications();
        let Some((kind, app)) = running_application() else {
            let process = running_process();
            return PresentationState {
                powerpoint_installed,
                wps_installed,
                application: process,
                running: process.is_some(),
                message: self.message.clone(),
                ..PresentationState::default()
            };
        };
        match read_application_state(kind, &app, &self.managed_paths) {
            Ok(mut state) => {
                state.powerpoint_installed = powerpoint_installed;
                state.wps_installed = wps_installed;
                state.message = self.message.clone();
                state
            }
            Err(error) => PresentationState {
                powerpoint_installed,
                wps_installed,
                application: Some(kind),
                running: true,
                message: self.message.clone(),
                error,
                ..PresentationState::default()
            },
        }
    }

    fn execute(&mut self, command: PresentationCommand) -> Result<String, String> {
        match command {
            PresentationCommand::Refresh => Ok("状态已刷新".into()),
            PresentationCommand::Open(path) => self.open(path),
            PresentationCommand::StartFromBeginning(path) => self.start_show(path, false),
            PresentationCommand::StartFromCurrent(path) => self.start_show(path, true),
            PresentationCommand::Previous => with_show_view(|view| {
                call(view, "Previous", &[]).map(|_| "已切换到上一页".to_owned())
            }),
            PresentationCommand::Next => {
                with_show_view(|view| call(view, "Next", &[]).map(|_| "已切换到下一页".to_owned()))
            }
            PresentationCommand::GoToSlide(slide) if slide > 0 => with_show_view(|view| {
                call(view, "GotoSlide", &[VARIANT::from(slide)])?;
                Ok(format!("已跳转到第 {slide} 页"))
            }),
            PresentationCommand::GoToSlide(_) => Err("请输入有效页码。".to_owned()),
            PresentationCommand::ToggleBlackScreen => toggle_screen(SLIDE_SHOW_BLACK, "黑屏"),
            PresentationCommand::ToggleWhiteScreen => toggle_screen(SLIDE_SHOW_WHITE, "白屏"),
            PresentationCommand::RestoreScreen => with_show_view(|view| {
                put(view, "State", VARIANT::from(SLIDE_SHOW_RUNNING))?;
                Ok("已恢复放映画面".to_owned())
            }),
            PresentationCommand::EndShow => with_show_view(|view| {
                call(view, "Exit", &[])?;
                Ok("已结束放映".to_owned())
            }),
            PresentationCommand::CloseActive => self.close_active(),
            PresentationCommand::CloseLastOpened => self.close_last_opened(),
            PresentationCommand::ExitApplication => self.exit_application(),
            PresentationCommand::ForceQuitAll { confirmed: false } => {
                Err("强制退出会丢失所有未保存内容，请再次确认。".to_owned())
            }
            PresentationCommand::ForceQuitAll { confirmed: true } => force_quit_all(),
        }
    }

    fn open(&mut self, path: PathBuf) -> Result<String, String> {
        let path = normalized_existing_path(&path)?;
        let (_kind, app) = match running_application() {
            Some(value) => value,
            None => {
                let value = create_application()?;
                self.created_application = Some(value.0);
                value
            }
        };
        let presentations = dispatch(get(&app, "Presentations")?)?;
        let presentation = if let Some(presentation) = find_presentation(&presentations, &path)? {
            presentation
        } else {
            let presentation = dispatch(call(
                &presentations,
                "Open",
                &[
                    VARIANT::from(path.as_str()),
                    VARIANT::from(true),
                    VARIANT::from(false),
                    VARIANT::from(true),
                ],
            )?)?;
            let key = normalize_path(&path);
            self.managed_paths.insert(key.clone());
            self.opened_order.push(key);
            presentation
        };
        let _ = put(&app, "WindowState", VARIANT::from(3));
        if let Ok(windows) = get(&presentation, "Windows").and_then(dispatch)
            && int(get(&windows, "Count")?).unwrap_or(0) > 0
            && let Ok(window) = call(&windows, "Item", &[VARIANT::from(1)]).and_then(dispatch)
        {
            let _ = put(&window, "WindowState", VARIANT::from(3));
            let _ = call(&window, "Activate", &[]);
        }
        let _ = put(&app, "Visible", VARIANT::from(true));
        Ok(format!(
            "已打开 {}",
            Path::new(&path)
                .file_name()
                .unwrap_or_default()
                .to_string_lossy()
        ))
    }

    fn start_show(
        &mut self,
        requested: Option<PathBuf>,
        from_current: bool,
    ) -> Result<String, String> {
        if let Some(path) = requested {
            self.open(path)?;
        }
        let (_, app) = running_application().ok_or("PowerPoint 或 WPS 演示未运行。")?;
        let windows = dispatch(get(&app, "SlideShowWindows")?)?;
        if int(get(&windows, "Count")?)? > 0 {
            return Ok("放映已经在运行，本次重复启动已忽略".to_owned());
        }
        let presentation = dispatch(get(&app, "ActivePresentation")?)?;
        let settings = dispatch(get(&presentation, "SlideShowSettings")?)?;
        let total = int(get(&dispatch(get(&presentation, "Slides")?)?, "Count")?)?;
        let start = if from_current {
            current_edit_slide(&app).unwrap_or(1).clamp(1, total)
        } else {
            1
        };
        let original_range = int(get(&settings, "RangeType")?).ok();
        let original_start = int(get(&settings, "StartingSlide")?).ok();
        let original_end = int(get(&settings, "EndingSlide")?).ok();
        let was_saved = int(get(&presentation, "Saved")?)
            .ok()
            .is_some_and(|value| value != 0);
        put(
            &settings,
            "RangeType",
            VARIANT::from(if from_current { 2 } else { 1 }),
        )?;
        put(&settings, "StartingSlide", VARIANT::from(start))?;
        put(&settings, "EndingSlide", VARIANT::from(total))?;
        let result = call(&settings, "Run", &[]);
        if let Some(value) = original_start {
            let _ = put(&settings, "StartingSlide", VARIANT::from(value));
        }
        if let Some(value) = original_end {
            let _ = put(&settings, "EndingSlide", VARIANT::from(value));
        }
        if let Some(value) = original_range {
            let _ = put(&settings, "RangeType", VARIANT::from(value));
        }
        if was_saved {
            let _ = put(&presentation, "Saved", VARIANT::from(true));
        }
        result?;
        Ok(if from_current {
            format!("已从第 {start} 页开始放映")
        } else {
            "已从头开始放映".to_owned()
        })
    }

    fn close_active(&mut self) -> Result<String, String> {
        let (_, app) = running_application().ok_or("PowerPoint 或 WPS 演示未运行。")?;
        let presentation = dispatch(get(&app, "ActivePresentation")?)?;
        let path = string(get(&presentation, "FullName")?)?;
        end_show_for(&app, &path);
        if self.managed_paths.contains(&normalize_path(&path)) {
            let _ = put(&presentation, "Saved", VARIANT::from(true));
        }
        call(&presentation, "Close", &[])?;
        self.remove_managed(&path);
        Ok(format!("已关闭当前文稿：{}。", file_name(&path)))
    }

    fn close_last_opened(&mut self) -> Result<String, String> {
        while let Some(path) = self.opened_order.pop() {
            let Some((_, app)) = running_application() else {
                break;
            };
            let presentations = dispatch(get(&app, "Presentations")?)?;
            if let Some(presentation) = find_presentation(&presentations, &path)? {
                end_show_for(&app, &path);
                let _ = put(&presentation, "Saved", VARIANT::from(true));
                call(&presentation, "Close", &[])?;
                self.managed_paths.remove(&path);
                return Ok(format!("已关闭最后打开的文稿：{}。", file_name(&path)));
            }
            self.managed_paths.remove(&path);
        }
        Err("当前没有 FlyPPTTimer 打开的文稿。".to_owned())
    }

    fn exit_application(&mut self) -> Result<String, String> {
        let (kind, app) =
            running_application().ok_or("未发现正在运行的 PowerPoint 或 WPS 演示进程。")?;
        if self.created_application != Some(kind) {
            return Err("演示软件不是由 FlyPPTTimer 启动，未执行退出。".to_owned());
        }
        let presentations = dispatch(get(&app, "Presentations")?)?;
        let count = int(get(&presentations, "Count")?)?;
        for index in 1..=count {
            let item = dispatch(call(&presentations, "Item", &[VARIANT::from(index)])?)?;
            let path = string(get(&item, "FullName")?)?;
            if !self.managed_paths.contains(&normalize_path(&path)) {
                return Err("检测到用户原先打开的文稿，未执行退出。".to_owned());
            }
        }
        call(&app, "Quit", &[])?;
        self.managed_paths.clear();
        self.opened_order.clear();
        self.created_application = None;
        Ok("已退出演示软件。".to_owned())
    }

    fn remove_managed(&mut self, path: &str) {
        let key = normalize_path(path);
        self.managed_paths.remove(&key);
        self.opened_order.retain(|candidate| candidate != &key);
    }
}

fn read_application_state(
    kind: PresentationApp,
    app: &IDispatch,
    managed: &HashSet<String>,
) -> Result<PresentationState, String> {
    let presentations = dispatch(get(app, "Presentations")?)?;
    let running = int(get(&presentations, "Count")?)? > 0;
    let windows = dispatch(get(app, "SlideShowWindows")?)?;
    let slide_show_running = int(get(&windows, "Count")?)? > 0;
    let presentation = if slide_show_running {
        let window = dispatch(call(&windows, "Item", &[VARIANT::from(1)])?)?;
        dispatch(get(&window, "Presentation")?)?
    } else if running {
        dispatch(get(app, "ActivePresentation")?)?
    } else {
        return Ok(PresentationState {
            application: Some(kind),
            running: true,
            ..PresentationState::default()
        });
    };
    let path = string(get(&presentation, "FullName")?).unwrap_or_default();
    let name = string(get(&presentation, "Name")?).unwrap_or_else(|_| file_name(&path));
    let slides = dispatch(get(&presentation, "Slides")?)?;
    let total_slides = int(get(&slides, "Count")?).unwrap_or(0);
    let (current_slide, screen_state) = if slide_show_running {
        let window = dispatch(call(&windows, "Item", &[VARIANT::from(1)])?)?;
        let view = dispatch(get(&window, "View")?)?;
        let slide = dispatch(get(&view, "Slide")?)?;
        (
            int(get(&slide, "SlideIndex")?).unwrap_or(0),
            int(get(&view, "State")?).unwrap_or(SLIDE_SHOW_RUNNING),
        )
    } else {
        (current_edit_slide(app).unwrap_or(0), SLIDE_SHOW_RUNNING)
    };
    let count = int(get(&presentations, "Count")?).unwrap_or(0);
    let mut open_presentations = Vec::new();
    for index in 1..=count {
        let Ok(item) = call(&presentations, "Item", &[VARIANT::from(index)]).and_then(dispatch)
        else {
            continue;
        };
        let item_path = string(get(&item, "FullName")?).unwrap_or_default();
        let item_name = string(get(&item, "Name")?).unwrap_or_else(|_| file_name(&item_path));
        open_presentations.push(OpenPresentation {
            name: item_name,
            active: same_path(&item_path, &path),
            slide_show_running: slide_show_running && same_path(&item_path, &path),
            managed: managed.contains(&normalize_path(&item_path)),
            path: item_path,
        });
    }
    Ok(PresentationState {
        powerpoint_installed: kind == PresentationApp::PowerPoint,
        wps_installed: kind == PresentationApp::Wps,
        application: Some(kind),
        running: true,
        has_presentation: true,
        slide_show_running,
        presentation_name: name,
        presentation_path: path.clone(),
        current_slide,
        total_slides,
        screen_state,
        managed: managed.contains(&normalize_path(&path)),
        message: String::new(),
        error: String::new(),
        presentations: open_presentations,
        operation: PresentationOperation::default(),
    })
}

fn running_application() -> Option<(PresentationApp, IDispatch)> {
    for (kind, prog_id) in application_prog_ids() {
        if let Ok(clsid) = clsid(prog_id) {
            let mut unknown = None;
            if unsafe { GetActiveObject(&clsid, None, &mut unknown) }.is_ok()
                && let Some(unknown) = unknown
                && let Ok(dispatch) = unknown.cast::<IDispatch>()
            {
                return Some((kind, dispatch));
            }
        }
    }
    None
}

fn create_application() -> Result<(PresentationApp, IDispatch), String> {
    for (kind, prog_id) in application_prog_ids() {
        let Ok(clsid) = clsid(prog_id) else { continue };
        if let Ok(app) =
            unsafe { CoCreateInstance::<_, IDispatch>(&clsid, None, CLSCTX_LOCAL_SERVER) }
        {
            return Ok((kind, app));
        }
    }
    Err("未安装 Microsoft PowerPoint 或 WPS 演示。".to_owned())
}

fn application_prog_ids() -> [(PresentationApp, &'static str); 3] {
    [
        (PresentationApp::PowerPoint, "PowerPoint.Application"),
        (PresentationApp::Wps, "KWPP.Application"),
        (PresentationApp::Wps, "WPP.Application"),
    ]
}

fn installed_applications() -> (bool, bool) {
    let powerpoint = clsid("PowerPoint.Application").is_ok();
    let wps = clsid("KWPP.Application").is_ok() || clsid("WPP.Application").is_ok();
    (powerpoint, wps)
}

fn running_process() -> Option<PresentationApp> {
    process_names().values().find_map(|name| {
        if name.eq_ignore_ascii_case("POWERPNT.EXE") {
            Some(PresentationApp::PowerPoint)
        } else if ["WPSOffice.exe", "wpp.exe", "wps.exe"]
            .iter()
            .any(|candidate| name.eq_ignore_ascii_case(candidate))
        {
            Some(PresentationApp::Wps)
        } else {
            None
        }
    })
}

fn process_names() -> HashMap<u32, String> {
    use windows_sys::Win32::{
        Foundation::{CloseHandle, INVALID_HANDLE_VALUE},
        System::Diagnostics::ToolHelp::{
            CreateToolhelp32Snapshot, PROCESSENTRY32W, Process32FirstW, Process32NextW,
            TH32CS_SNAPPROCESS,
        },
    };
    unsafe {
        let snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if snapshot == INVALID_HANDLE_VALUE {
            return HashMap::new();
        }
        let mut entry = PROCESSENTRY32W {
            dwSize: std::mem::size_of::<PROCESSENTRY32W>() as u32,
            ..Default::default()
        };
        let mut processes = HashMap::new();
        if Process32FirstW(snapshot, &mut entry) != 0 {
            loop {
                let length = entry
                    .szExeFile
                    .iter()
                    .position(|value| *value == 0)
                    .unwrap_or(entry.szExeFile.len());
                let name = String::from_utf16_lossy(&entry.szExeFile[..length]);
                processes.insert(entry.th32ProcessID, name);
                if Process32NextW(snapshot, &mut entry) == 0 {
                    break;
                }
            }
        }
        CloseHandle(snapshot);
        processes
    }
}

pub fn fullscreen_whitelist_match(whitelist: &[String]) -> Option<String> {
    use windows_sys::Win32::{
        Foundation::{HWND, LPARAM, RECT},
        Graphics::Gdi::{
            GetMonitorInfoW, MONITOR_DEFAULTTONEAREST, MONITORINFO, MonitorFromWindow,
        },
        UI::WindowsAndMessaging::{
            EnumWindows, GetWindowRect, GetWindowThreadProcessId, IsWindowVisible,
        },
    };
    struct Context<'a> {
        whitelist: &'a [String],
        processes: HashMap<u32, String>,
        matched: Option<String>,
    }
    unsafe extern "system" fn callback(hwnd: HWND, parameter: LPARAM) -> i32 {
        let context = unsafe { &mut *(parameter as *mut Context<'_>) };
        if unsafe { IsWindowVisible(hwnd) } == 0 {
            return 1;
        }
        let mut process_id = 0;
        unsafe { GetWindowThreadProcessId(hwnd, &mut process_id) };
        let Some(name) = context.processes.get(&process_id) else {
            return 1;
        };
        if name.eq_ignore_ascii_case("POWERPNT.EXE")
            || !context
                .whitelist
                .iter()
                .any(|candidate| name.eq_ignore_ascii_case(candidate))
        {
            return 1;
        }
        let mut window = RECT::default();
        if unsafe { GetWindowRect(hwnd, &mut window) } == 0 {
            return 1;
        }
        let monitor = unsafe { MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST) };
        let mut info = MONITORINFO {
            cbSize: std::mem::size_of::<MONITORINFO>() as u32,
            ..Default::default()
        };
        if unsafe { GetMonitorInfoW(monitor, &mut info) } == 0 {
            return 1;
        }
        if window.left <= info.rcMonitor.left + 2
            && window.top <= info.rcMonitor.top + 2
            && window.right >= info.rcMonitor.right - 2
            && window.bottom >= info.rcMonitor.bottom - 2
        {
            context.matched = Some(name.clone());
            return 0;
        }
        1
    }
    let mut context = Context {
        whitelist,
        processes: process_names(),
        matched: None,
    };
    unsafe { EnumWindows(Some(callback), (&mut context as *mut Context<'_>) as LPARAM) };
    context.matched
}

fn clsid(prog_id: &str) -> windows::core::Result<windows::core::GUID> {
    let value = BSTR::from(prog_id);
    unsafe { CLSIDFromProgID(PCWSTR(value.as_ptr())) }
}

fn with_show_view(
    operation: impl FnOnce(&IDispatch) -> Result<String, String>,
) -> Result<String, String> {
    let (_, app) = running_application().ok_or("PowerPoint 或 WPS 演示未运行。")?;
    let windows = dispatch(get(&app, "SlideShowWindows")?)?;
    if int(get(&windows, "Count")?)? <= 0 {
        return Err("当前没有正在运行的 PowerPoint 放映。".to_owned());
    }
    let window = dispatch(call(&windows, "Item", &[VARIANT::from(1)])?)?;
    let view = dispatch(get(&window, "View")?)?;
    operation(&view)
}

fn toggle_screen(target: i32, label: &str) -> Result<String, String> {
    with_show_view(|view| {
        let current = int(get(view, "State")?)?;
        put(
            view,
            "State",
            VARIANT::from(if current == target {
                SLIDE_SHOW_RUNNING
            } else {
                target
            }),
        )?;
        Ok(format!("已切换{label}状态"))
    })
}

fn current_edit_slide(app: &IDispatch) -> Result<i32, String> {
    let window = dispatch(get(app, "ActiveWindow")?)?;
    let view = dispatch(get(&window, "View")?)?;
    let slide = dispatch(get(&view, "Slide")?)?;
    int(get(&slide, "SlideIndex")?)
}

fn find_presentation(presentations: &IDispatch, path: &str) -> Result<Option<IDispatch>, String> {
    let count = int(get(presentations, "Count")?)?;
    for index in 1..=count {
        let item = dispatch(call(presentations, "Item", &[VARIANT::from(index)])?)?;
        if same_path(&string(get(&item, "FullName")?)?, path) {
            return Ok(Some(item));
        }
    }
    Ok(None)
}

fn end_show_for(app: &IDispatch, path: &str) {
    let Ok(windows) = get(app, "SlideShowWindows").and_then(dispatch) else {
        return;
    };
    let Ok(count) = get(&windows, "Count").and_then(int) else {
        return;
    };
    for index in 1..=count {
        let Ok(window) = call(&windows, "Item", &[VARIANT::from(index)]).and_then(dispatch) else {
            continue;
        };
        let Ok(presentation) = get(&window, "Presentation").and_then(dispatch) else {
            continue;
        };
        let Ok(showing_path) = get(&presentation, "FullName").and_then(string) else {
            continue;
        };
        if same_path(&showing_path, path)
            && let Ok(view) = get(&window, "View").and_then(dispatch)
        {
            let _ = call(&view, "Exit", &[]);
            return;
        }
    }
}

fn get(object: &IDispatch, name: &str) -> Result<VARIANT, String> {
    invoke(object, name, DISPATCH_PROPERTYGET, &[], None)
}

fn call(object: &IDispatch, name: &str, args: &[VARIANT]) -> Result<VARIANT, String> {
    invoke(
        object,
        name,
        DISPATCH_METHOD | DISPATCH_PROPERTYGET,
        args,
        None,
    )
}

fn put(object: &IDispatch, name: &str, value: VARIANT) -> Result<(), String> {
    invoke(object, name, DISPATCH_PROPERTYPUT, &[value], Some(-3)).map(|_| ())
}

fn invoke(
    object: &IDispatch,
    name: &str,
    flags: windows::Win32::System::Com::DISPATCH_FLAGS,
    args: &[VARIANT],
    named: Option<i32>,
) -> Result<VARIANT, String> {
    let wide = name.encode_utf16().chain(Some(0)).collect::<Vec<_>>();
    let name_ptr = PCWSTR(wide.as_ptr());
    let mut id = 0;
    unsafe {
        object
            .GetIDsOfNames(&windows::core::GUID::zeroed(), &name_ptr, 1, 0, &mut id)
            .map_err(|error| format!("COM 成员 {name} 不可用：{error}"))?;
    }
    let mut reversed = args.iter().rev().cloned().collect::<Vec<_>>();
    let mut named_id = named.unwrap_or_default();
    let params = DISPPARAMS {
        rgvarg: if reversed.is_empty() {
            std::ptr::null_mut()
        } else {
            reversed.as_mut_ptr()
        },
        rgdispidNamedArgs: if named.is_some() {
            &mut named_id
        } else {
            std::ptr::null_mut()
        },
        cArgs: reversed.len() as u32,
        cNamedArgs: u32::from(named.is_some()),
    };
    let mut result = VARIANT::default();
    unsafe {
        object
            .Invoke(
                id,
                &windows::core::GUID::zeroed(),
                0,
                flags,
                &params,
                Some(&mut result),
                None,
                None,
            )
            .map_err(|error| format!("COM 调用 {name} 失败：{error}"))?;
    }
    Ok(result)
}

fn dispatch(value: VARIANT) -> Result<IDispatch, String> {
    IDispatch::try_from(&value).map_err(|error| error.to_string())
}

fn int(value: VARIANT) -> Result<i32, String> {
    i32::try_from(&value).map_err(|error| error.to_string())
}

fn string(value: VARIANT) -> Result<String, String> {
    BSTR::try_from(&value)
        .map(|value| value.to_string())
        .map_err(|error| error.to_string())
}

fn normalized_existing_path(path: &Path) -> Result<String, String> {
    if !path.is_file() {
        return Err("演示文稿文件不存在。".to_owned());
    }
    path.canonicalize()
        .map(|path| path.to_string_lossy().into_owned())
        .map_err(|error| error.to_string())
}

fn normalize_path(path: &str) -> String {
    Path::new(path)
        .canonicalize()
        .unwrap_or_else(|_| PathBuf::from(path))
        .to_string_lossy()
        .trim_end_matches(['\\', '/'])
        .to_lowercase()
}

fn same_path(left: &str, right: &str) -> bool {
    !left.is_empty() && !right.is_empty() && normalize_path(left) == normalize_path(right)
}

fn file_name(path: &str) -> String {
    Path::new(path)
        .file_name()
        .unwrap_or_default()
        .to_string_lossy()
        .into_owned()
}

fn force_quit_all() -> Result<String, String> {
    let status = std::process::Command::new("taskkill")
        .args(["/F", "/T", "/IM", "POWERPNT.EXE"])
        .status();
    let wps = ["WPSOffice.exe", "wpp.exe", "wps.exe"]
        .iter()
        .filter_map(|name| {
            std::process::Command::new("taskkill")
                .args(["/F", "/T", "/IM", name])
                .status()
                .ok()
        })
        .any(|status| status.success());
    if status.is_ok_and(|status| status.success()) || wps {
        Ok("已请求退出演示软件。未保存内容不会恢复。".to_owned())
    } else {
        Ok("未发现正在运行的 PowerPoint 或 WPS 演示进程。".to_owned())
    }
}

pub fn matching_rule<'a>(config: &'a AppConfig, presentation_path: &str) -> Option<&'a FileRule> {
    config
        .rules
        .iter()
        .find(|rule| rule.enabled && same_path(&rule.file_path, presentation_path))
}

pub fn timer_settings_for(config: &AppConfig, presentation_path: &str) -> (Duration, TimerMode) {
    matching_rule(config, presentation_path)
        .map(|rule| {
            let duration =
                parse_rule_duration(&rule.duration).unwrap_or_else(|| config.timer.duration());
            (duration, rule.mode)
        })
        .unwrap_or_else(|| (config.timer.duration(), config.timer.mode))
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum PresentationTimerAction {
    None,
    Start(String),
    Stop { reset: bool },
    Reset,
}

#[derive(Default)]
pub struct PresentationLifecycle {
    showing: bool,
    path: String,
    automation_active: bool,
}

impl PresentationLifecycle {
    pub fn observe(
        &mut self,
        showing: bool,
        path: &str,
        config: &AppConfig,
    ) -> PresentationTimerAction {
        if showing {
            if self.showing && same_path(&self.path, path) {
                return PresentationTimerAction::None;
            }
            self.showing = true;
            self.path = path.to_owned();
            if config.behavior.auto_start_on_fullscreen {
                self.automation_active = true;
                PresentationTimerAction::Start(path.to_owned())
            } else {
                self.automation_active = false;
                PresentationTimerAction::None
            }
        } else {
            if !self.showing {
                return PresentationTimerAction::None;
            }
            self.showing = false;
            self.path.clear();
            if !std::mem::take(&mut self.automation_active) {
                return PresentationTimerAction::None;
            }
            if config.behavior.stop_when_leaving_fullscreen {
                PresentationTimerAction::Stop {
                    reset: config.behavior.reset_when_leaving_fullscreen,
                }
            } else if config.behavior.reset_when_leaving_fullscreen {
                PresentationTimerAction::Reset
            } else {
                PresentationTimerAction::None
            }
        }
    }
}

fn parse_rule_duration(value: &str) -> Option<Duration> {
    let parts = value
        .split(':')
        .map(str::parse::<u64>)
        .collect::<Result<Vec<_>, _>>()
        .ok()?;
    (parts.len() == 3 && parts[1] < 60 && parts[2] < 60)
        .then(|| Duration::from_secs(parts[0] * 3_600 + parts[1] * 60 + parts[2]))
        .filter(|duration| !duration.is_zero())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn operation_tracks_busy_completion_and_failure() {
        let (name, message) = PresentationCommand::Open(PathBuf::from("deck.pptx")).operation();
        let mut operation = PresentationOperation {
            name: name.into(),
            message: message.into(),
            id: "operation-1".into(),
            started_at: Some("2026-09-03T00:00:00Z".into()),
            busy: name != "Idle",
        };
        assert!(operation.busy);
        assert_eq!(operation.name, "OpeningPresentation");
        operation.finish(&Err("打开失败。".into()));
        assert_eq!(operation.name, "Failed");
        assert!(!operation.busy);
        assert_eq!(operation.id, "operation-1");
        operation.finish(&Ok("已打开演示文稿".into()));
        assert_eq!(
            operation,
            PresentationOperation {
                message: "已打开演示文稿".into(),
                ..PresentationOperation::default()
            }
        );
        assert_eq!(
            PresentationCommand::Next.operation(),
            ("Idle", "正在执行演示命令")
        );
        assert_eq!(
            PresentationCommand::EndShow.operation().0,
            "StoppingSlideshow"
        );
    }

    #[test]
    fn enabled_full_path_rule_overrides_global_timer() {
        let mut config = AppConfig::default();
        config.rules.push(FileRule {
            file_path: r"C:\Decks\Talk.pptx".to_owned(),
            duration: "00:03:30".to_owned(),
            mode: TimerMode::CountUp,
            enabled: true,
            ..FileRule::default()
        });
        assert_eq!(
            timer_settings_for(&config, r"c:\decks\TALK.pptx"),
            (Duration::from_secs(210), TimerMode::CountUp)
        );
        assert_eq!(
            timer_settings_for(&config, r"C:\Decks\Other.pptx"),
            (Duration::from_secs(480), TimerMode::Countdown)
        );
    }

    #[test]
    fn disabled_rule_does_not_override_global_timer() {
        let mut config = AppConfig::default();
        config.rules.push(FileRule {
            file_path: r"C:\Talk.pptx".to_owned(),
            duration: "00:01:00".to_owned(),
            enabled: false,
            ..FileRule::default()
        });
        assert_eq!(
            timer_settings_for(&config, r"c:\talk.pptx"),
            (Duration::from_secs(480), TimerMode::Countdown)
        );
    }

    #[test]
    fn slideshow_transitions_follow_v0302_stop_reset_order() {
        let config = AppConfig::default();
        let mut lifecycle = PresentationLifecycle::default();
        assert_eq!(
            lifecycle.observe(true, r"C:\Talk.pptx", &config),
            PresentationTimerAction::Start(r"C:\Talk.pptx".to_owned())
        );
        assert_eq!(
            lifecycle.observe(true, r"c:\TALK.pptx", &config),
            PresentationTimerAction::None
        );
        assert_eq!(
            lifecycle.observe(false, "", &config),
            PresentationTimerAction::Stop { reset: true }
        );
    }

    #[test]
    fn slideshow_without_auto_start_does_not_stop_user_timer_on_exit() {
        let mut config = AppConfig::default();
        config.behavior.auto_start_on_fullscreen = false;
        let mut lifecycle = PresentationLifecycle::default();
        assert_eq!(
            lifecycle.observe(true, r"C:\Talk.pptx", &config),
            PresentationTimerAction::None
        );
        assert_eq!(
            lifecycle.observe(false, "", &config),
            PresentationTimerAction::None
        );
    }

    #[test]
    #[ignore = "manual Windows COM smoke test"]
    fn manual_powerpoint_and_wps_com_connection() {
        let _ = unsafe { CoInitializeEx(None, COINIT_APARTMENTTHREADED) };
        for prog_id in ["PowerPoint.Application", "KWPP.Application"] {
            let clsid = clsid(prog_id).unwrap_or_else(|error| panic!("{prog_id}: {error}"));
            let app =
                unsafe { CoCreateInstance::<_, IDispatch>(&clsid, None, CLSCTX_LOCAL_SERVER) }
                    .unwrap_or_else(|error| panic!("{prog_id}: {error}"));
            let version =
                string(get(&app, "Version").expect("Version property")).expect("Version string");
            assert!(!version.is_empty(), "{prog_id} returned no version");
            call(&app, "Quit", &[]).unwrap_or_else(|error| panic!("{prog_id} Quit: {error}"));
            println!("{prog_id} {version}");
        }
    }
}
