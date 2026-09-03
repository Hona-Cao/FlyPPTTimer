use std::{
    cell::{Cell, RefCell},
    path::PathBuf,
    rc::Rc,
    time::{Duration, Instant},
};

use slint::{Brush, Color, ComponentHandle, LogicalSize, Model, ModelRc, VecModel};

use crate::{
    alerts::AlertTracker,
    audio::AudioService,
    config::{AppConfig, CloseButtonBehavior, TimerEndAction, TimerMode},
    desktop::{DesktopEvent, DesktopIntegration},
    display,
    flash::{FlashController, FlashFrame},
    presentation::{
        PresentationCommand, PresentationLifecycle, PresentationService, PresentationTimerAction,
        fullscreen_whitelist_match,
    },
    remote::{RemoteRequest, RemoteServer},
    settings,
    timer::{SystemClock, Timer, TimerSnapshot, TimerState},
    updater::{UpdateService, handle_response_ui, start_check_ui},
    window,
};

pub use slint::include_modules;
include_modules!();

#[derive(Default)]
pub struct DisplayWindows {
    pub overlays: Vec<AppWindow>,
    pub big_screen: Option<BigScreenWindow>,
    pub signature: String,
}

pub fn run() -> Result<(), Box<dyn std::error::Error>> {
    let Some(_instance) = crate::single_instance::acquire()? else {
        return Ok(());
    };
    let config_path = config_path()?;
    let config = Rc::new(RefCell::new(load_config(&config_path)?));
    crate::log::info("FlyPPTTimer starting");

    let timer = Rc::new(RefCell::new(timer_from_config(&config.borrow())?));
    let alerts = Rc::new(RefCell::new(AlertTracker::default()));
    let flash = Rc::new(RefCell::new(FlashController::new()));
    let audio = Rc::new(AudioService::new());
    let desktop = Rc::new(DesktopIntegration::start(&config.borrow())?);
    let presentation = Rc::new(PresentationService::start()?);
    let presentation_lifecycle = Rc::new(RefCell::new(PresentationLifecycle::default()));
    let (remote_sender, remote_receiver) = std::sync::mpsc::channel();
    let remote = Rc::new(RemoteServer::new(remote_sender));
    let remote_receiver = Rc::new(RefCell::new(remote_receiver));
    let update_service = Rc::new(UpdateService::new());

    let window = AppWindow::new()?;
    apply_config(&window, &config.borrow());
    update_window(
        &window.as_weak(),
        &timer.borrow().snapshot(),
        &config.borrow(),
        FlashFrame {
            text_visible: true,
            ..FlashFrame::default()
        },
    );
    connect_drag(&window, Rc::clone(&config), config_path.clone());
    connect_timer_menu(&window, Rc::clone(&desktop));
    connect_close(&window, Rc::clone(&config), config_path.clone());

    let display_windows = Rc::new(RefCell::new(DisplayWindows::default()));
    let display_rebuild = Rc::new(Cell::new(true));
    let time_up_window = Rc::new(RefCell::new(None));
    let preserve_time_up = Rc::new(Cell::new(false));
    let settings_window = Rc::new(RefCell::new(None));
    let presentation_window = Rc::new(RefCell::new(None));

    // Initialize remote token and start service.
    {
        let mut cfg = config.borrow_mut();
        if cfg.remote_control.token.is_empty() {
            cfg.remote_control.token = crate::remote::generate_token();
            save_config(&cfg, &config_path);
        }
        if let Err(error) = remote.start(&mut cfg) {
            eprintln!("{error}");
        }
    }

    // Optional startup update check.
    if config.borrow().update.check_on_startup {
        start_check_ui(&update_service, &config.borrow(), &desktop, false);
    }

    // Show settings window on request.
    let show_settings = std::env::args().any(|arg| arg == "--show-settings");
    if show_settings {
        let settings = create_settings(
            Rc::clone(&config),
            config_path.clone(),
            Rc::clone(&timer),
            window.as_weak(),
            Rc::clone(&desktop),
            Rc::clone(&remote),
            Rc::clone(&display_rebuild),
            true,
        )?;
        settings.show()?;
        *settings_window.borrow_mut() = Some(settings);
    }

    let weak_window = window.as_weak();
    let timer_for_updates = Rc::clone(&timer);
    let config_for_updates = Rc::clone(&config);
    let alerts_for_updates = Rc::clone(&alerts);
    let flash_for_updates = Rc::clone(&flash);
    let audio_for_updates = Rc::clone(&audio);
    let desktop_for_updates = Rc::clone(&desktop);
    let settings_for_updates = Rc::clone(&settings_window);
    let presentation_for_updates = Rc::clone(&presentation);
    let lifecycle_for_updates = Rc::clone(&presentation_lifecycle);
    let time_up_for_updates = Rc::clone(&time_up_window);
    let preserve_time_up_for_updates = Rc::clone(&preserve_time_up);
    let presentation_window_for_updates = Rc::clone(&presentation_window);
    let remote_for_updates = Rc::clone(&remote);
    let remote_receiver_for_updates = Rc::clone(&remote_receiver);
    let update_for_updates = Rc::clone(&update_service);
    let display_windows_for_updates = Rc::clone(&display_windows);
    let display_rebuild_for_updates = Rc::clone(&display_rebuild);
    let config_path_for_updates = config_path.clone();
    let last_fullscreen_check = Rc::new(Cell::new(Instant::now()));
    let fullscreen_match = Rc::new(RefCell::new(None::<String>));
    let last_remote_update = Rc::new(Cell::new(Instant::now()));
    let last_display_check = Rc::new(Cell::new(Instant::now()));

    let refresh_timer = slint::Timer::default();
    refresh_timer.start(
        slint::TimerMode::Repeated,
        Duration::from_millis(100),
        move || {
            while let Some(event) = desktop_for_updates.try_recv() {
                handle_desktop_event(
                    event,
                    &weak_window,
                    &timer_for_updates,
                    &config_for_updates,
                    &alerts_for_updates,
                    &flash_for_updates,
                    &desktop_for_updates,
                    &settings_for_updates,
                    &presentation_window_for_updates,
                    &presentation_for_updates,
                    &remote_for_updates,
                    &update_for_updates,
                    &display_rebuild_for_updates,
                    &config_path_for_updates,
                );
            }
            while let Ok(request) = remote_receiver_for_updates.borrow().try_recv() {
                handle_remote_request(
                    request,
                    &weak_window,
                    &timer_for_updates,
                    &config_for_updates,
                    &alerts_for_updates,
                    &flash_for_updates,
                    &presentation_for_updates,
                    &time_up_for_updates,
                    &preserve_time_up_for_updates,
                    &config_path_for_updates,
                );
            }
            while let Some(response) = update_for_updates.try_recv() {
                let config = config_for_updates.borrow().clone();
                handle_response_ui(response, &update_for_updates, &config, &desktop_for_updates);
            }
            if config_for_updates.borrow().controls.minimize_to_tray
                && let Some(settings) = settings_for_updates.borrow().as_ref()
                && window::is_minimized(settings.window())
            {
                let _ = settings.hide();
            }
            let presentation_state = presentation_for_updates.state();
            if let Some(control) = presentation_window_for_updates.borrow().as_ref() {
                update_presentation_window(
                    control,
                    &presentation_state,
                    &config_for_updates.borrow(),
                );
                update_remote_connection_window(
                    control,
                    &remote_for_updates,
                    &config_for_updates.borrow(),
                );
            }
            if last_fullscreen_check.get().elapsed() >= Duration::from_millis(500) {
                last_fullscreen_check.set(Instant::now());
                *fullscreen_match.borrow_mut() = fullscreen_whitelist_match(
                    &config_for_updates
                        .borrow()
                        .behavior
                        .fullscreen_process_whitelist,
                );
            }
            let action = lifecycle_for_updates.borrow_mut().observe(
                presentation_state.slide_show_running || fullscreen_match.borrow().is_some(),
                &presentation_state.presentation_path,
                &config_for_updates.borrow(),
            );
            if matches!(&action, PresentationTimerAction::Start(_))
                && let Some(settings) = settings_for_updates.borrow().as_ref()
            {
                let _ = settings.hide();
            }
            apply_presentation_timer_action(
                action,
                &timer_for_updates,
                &alerts_for_updates,
                &config_for_updates.borrow(),
            );
            let update = timer_for_updates.borrow_mut().update();
            if last_display_check.get().elapsed() >= Duration::from_millis(1500) {
                last_display_check.set(Instant::now());
                let monitors = display::monitors();
                if !monitors.is_empty()
                    && display::signature(&monitors)
                        != display_windows_for_updates.borrow().signature
                {
                    display_rebuild_for_updates.set(true);
                }
            }
            let config = config_for_updates.borrow();
            let mut events = alerts_for_updates
                .borrow_mut()
                .check(&update.snapshot, &config);
            if update.just_finished
                && let Some(event) = alerts_for_updates
                    .borrow_mut()
                    .end(&update.snapshot, &config)
            {
                events.push(event);
            }
            if update.just_finished {
                match config.timer.end_action {
                    TimerEndAction::BlackScreen => {
                        if let Err(error) =
                            presentation_for_updates.queue(PresentationCommand::EndShow)
                        {
                            eprintln!("time-up slideshow exit was not accepted: {error}");
                        }
                        timer_for_updates.borrow_mut().stop_and_reset();
                        preserve_time_up_for_updates.set(true);
                        show_time_up(
                            &time_up_for_updates,
                            &preserve_time_up_for_updates,
                            &config.language,
                        );
                    }
                    TimerEndAction::ExitSlideShow => {
                        if let Err(error) =
                            presentation_for_updates.queue(PresentationCommand::EndShow)
                        {
                            eprintln!("time-up slideshow exit was not accepted: {error}");
                        }
                        timer_for_updates.borrow_mut().stop_and_reset();
                        preserve_time_up_for_updates.set(false);
                        hide_time_up(&time_up_for_updates);
                    }
                    TimerEndAction::None => {}
                }
            }
            for event in events {
                audio_for_updates.play(&event);
                flash_for_updates.borrow_mut().start_prompt(&event.prompt);
            }
            if update.snapshot.state == TimerState::Paused && config.behavior.flash_paused_time {
                flash_for_updates.borrow_mut().ensure_pause(
                    &config.appearance.flash_style,
                    config.appearance.flash_on_ms,
                    config.appearance.flash_off_ms,
                );
            } else {
                flash_for_updates.borrow_mut().stop_pause();
            }
            let frame = flash_for_updates.borrow_mut().frame();
            if display_rebuild_for_updates.replace(false)
                && let Some(root) = weak_window.upgrade()
                && let Err(error) = rebuild_display_windows(
                    &root,
                    &display_windows_for_updates,
                    Rc::clone(&config_for_updates),
                    config_path_for_updates.clone(),
                    Rc::clone(&desktop_for_updates),
                    &update.snapshot,
                    &config,
                    frame,
                )
            {
                eprintln!("failed to rebuild display windows: {error}");
            }
            update_display_windows(
                &weak_window,
                &display_windows_for_updates,
                &update.snapshot,
                &config,
                frame,
            );
            if last_remote_update.get().elapsed() >= Duration::from_secs(1) {
                last_remote_update.set(Instant::now());
                remote_for_updates.update_state(crate::remote::remote_state(
                    &update.snapshot,
                    &config,
                    &presentation_state,
                    format_snapshot(&update.snapshot, &config.appearance.overtime_prefix),
                    crate::audio::system_mute().unwrap_or(false),
                    preserve_time_up_for_updates.get(),
                ));
            }
            if update.snapshot.state == TimerState::Running && preserve_time_up_for_updates.get() {
                preserve_time_up_for_updates.set(false);
                hide_time_up(&time_up_for_updates);
            }
            drop(config);
            if let Some(root) = weak_window.upgrade() {
                expand_timer_windows_if_needed(
                    &root,
                    &display_windows_for_updates.borrow(),
                    &config_for_updates,
                    &config_path_for_updates,
                );
            }
        },
    );

    rebuild_display_windows(
        &window,
        &display_windows,
        Rc::clone(&config),
        config_path.clone(),
        Rc::clone(&desktop),
        &timer.borrow().snapshot(),
        &config.borrow(),
        FlashFrame {
            text_visible: true,
            ..FlashFrame::default()
        },
    )?;
    display_rebuild.set(false);

    slint::run_event_loop_until_quit()?;
    desktop.shutdown();
    remote.stop();
    settings_window.borrow_mut().take();
    if let Some(control) = presentation_window.borrow().as_ref() {
        let mut config = config.borrow_mut();
        window::capture_remote_window(control.window(), &mut config.remote_control.window);
        save_config(&config, &config_path);
    }
    presentation_window.borrow_mut().take();
    {
        let mut displays = display_windows.borrow_mut();
        for overlay in displays.overlays.drain(..) {
            let _ = overlay.hide();
        }
        if let Some(big_screen) = displays.big_screen.take() {
            let _ = big_screen.hide();
        }
    }
    let _ = window.hide();
    crate::log::info("FlyPPTTimer stopped");
    Ok(())
}

fn show_time_up(
    holder: &Rc<RefCell<Option<TimeUpWindow>>>,
    preserve: &Rc<Cell<bool>>,
    language: &str,
) {
    hide_time_up(holder);
    let Ok(window) = TimeUpWindow::new() else {
        return;
    };
    window.set_message(
        if crate::config::ui_is_english(language) {
            "TIME'S UP"
        } else {
            "时间到"
        }
        .into(),
    );
    let weak = window.as_weak();
    let preserve_for_dismiss = Rc::clone(preserve);
    window.on_dismiss(move || {
        preserve_for_dismiss.set(false);
        if let Some(window) = weak.upgrade() {
            let _ = window.hide();
        }
    });
    if window.show().is_ok() {
        window::show_time_up_window(window.window());
        *holder.borrow_mut() = Some(window);
    }
}

fn hide_time_up(holder: &Rc<RefCell<Option<TimeUpWindow>>>) {
    if let Some(window) = holder.borrow_mut().take() {
        let _ = window.hide();
    }
}

fn apply_presentation_timer_action(
    action: PresentationTimerAction,
    timer: &Rc<RefCell<Timer<SystemClock>>>,
    alerts: &Rc<RefCell<AlertTracker>>,
    config: &AppConfig,
) {
    match action {
        PresentationTimerAction::None => {}
        PresentationTimerAction::Start(path) => {
            let (duration, mode) = crate::presentation::timer_settings_for(config, &path);
            let _ = timer.borrow_mut().set_duration(duration);
            timer.borrow_mut().set_mode(timer_mode(mode));
            alerts.borrow_mut().reset();
            timer.borrow_mut().start();
        }
        PresentationTimerAction::Stop { reset } => {
            timer.borrow_mut().stop();
            if reset {
                timer.borrow_mut().stop_and_reset();
            }
            alerts.borrow_mut().reset();
        }
        PresentationTimerAction::Reset => {
            timer.borrow_mut().stop_and_reset();
            alerts.borrow_mut().reset();
        }
    }
}

fn timer_mode(mode: TimerMode) -> crate::timer::TimerMode {
    match mode {
        TimerMode::Countdown => crate::timer::TimerMode::Countdown,
        TimerMode::CountUp => crate::timer::TimerMode::CountUp,
    }
}

#[allow(clippy::too_many_arguments)]
fn create_settings(
    config: Rc<RefCell<AppConfig>>,
    config_path: PathBuf,
    timer: Rc<RefCell<Timer<SystemClock>>>,
    timer_window: slint::Weak<AppWindow>,
    desktop: Rc<DesktopIntegration>,
    remote: Rc<RemoteServer>,
    display_rebuild: Rc<Cell<bool>>,
    exit_on_close: bool,
) -> Result<SettingsWindow, slint::PlatformError> {
    let config_for_remote = Rc::clone(&config);
    let config_path_for_remote = config_path.clone();
    let remote_for_applied = Rc::clone(&remote);
    let on_applied = Rc::new(move |updated: &AppConfig| {
        apply_timer_config(&mut timer.borrow_mut(), updated);
        desktop.reconfigure(updated);
        {
            let mut current = config_for_remote.borrow_mut();
            if let Err(error) = remote_for_applied.apply_enabled(&mut current) {
                eprintln!("{error}");
            }
            save_config(&current, &config_path_for_remote);
        }
        if let Some(window) = timer_window.upgrade() {
            apply_config(&window, updated);
            window::apply_native_window(
                window.window(),
                updated.controls.click_through,
                updated.appearance.always_on_top,
                updated.appearance.background_opacity,
                &updated.appearance.shape,
            );
            set_window_visible(&window, updated.placement.visible);
        }
        display_rebuild.set(true);
    });
    settings::create(
        config,
        config_path,
        on_applied,
        Rc::new(|| {}),
        exit_on_close,
        Rc::clone(&remote),
    )
}

#[allow(clippy::too_many_arguments)]
fn handle_desktop_event(
    event: DesktopEvent,
    window: &slint::Weak<AppWindow>,
    timer: &Rc<RefCell<Timer<SystemClock>>>,
    config: &Rc<RefCell<AppConfig>>,
    alerts: &Rc<RefCell<AlertTracker>>,
    flash: &Rc<RefCell<FlashController>>,
    desktop: &Rc<DesktopIntegration>,
    settings_window: &Rc<RefCell<Option<SettingsWindow>>>,
    presentation_window: &Rc<RefCell<Option<PresentationWindow>>>,
    presentation: &Rc<PresentationService>,
    remote: &Rc<RemoteServer>,
    update_service: &Rc<UpdateService>,
    display_rebuild: &Rc<Cell<bool>>,
    config_path: &std::path::Path,
) {
    match event {
        DesktopEvent::Command(command) => {
            handle_command(&command, window, timer, config, alerts, flash, config_path)
        }
        DesktopEvent::ResetPosition => {
            let mut config = config.borrow_mut();
            config.placement.has_custom_placement = false;
            save_config(&config, config_path);
            display_rebuild.set(true);
        }
        DesktopEvent::OpenSettings => {
            if let Some(settings) = settings_window.borrow().as_ref() {
                if let Err(error) = settings.show() {
                    eprintln!("failed to show settings: {error}");
                }
                return;
            }
            if let Some(control) = presentation_window.borrow().as_ref()
                && let Err(error) = control.hide()
            {
                eprintln!("failed to hide presentation control: {error}");
            }
            match create_settings(
                Rc::clone(config),
                config_path.to_path_buf(),
                Rc::clone(timer),
                window.clone(),
                Rc::clone(desktop),
                Rc::clone(remote),
                Rc::clone(display_rebuild),
                false,
            ) {
                Ok(settings) => {
                    if let Err(error) = settings.show() {
                        eprintln!("failed to show settings: {error}");
                    }
                    *settings_window.borrow_mut() = Some(settings);
                }
                Err(error) => eprintln!("failed to create settings: {error}"),
            }
        }
        DesktopEvent::Remote => {
            if let Some(settings) = settings_window.borrow().as_ref()
                && let Err(error) = settings.hide()
            {
                eprintln!("failed to hide settings: {error}");
            }
            if let Some(control) = presentation_window.borrow().as_ref() {
                if let Err(error) = control.show() {
                    eprintln!("failed to show presentation control: {error}");
                }
                populate_remote_connection_window(control, remote, &config.borrow(), true);
                return;
            }
            match create_presentation_window(config, presentation, remote, config_path) {
                Ok(control) => {
                    if let Err(error) = control.show() {
                        eprintln!("failed to show presentation control: {error}");
                    }
                    *presentation_window.borrow_mut() = Some(control);
                }
                Err(error) => eprintln!("failed to create presentation control: {error}"),
            }
        }
        DesktopEvent::CheckUpdate => {
            let config = config.borrow().clone();
            start_check_ui(update_service, &config, desktop, true);
        }
        DesktopEvent::Exit => {
            if let Some(window) = window.upgrade() {
                let mut config = config.borrow_mut();
                let monitors = display::monitors();
                if !monitors.is_empty() {
                    display::capture_timer_position(
                        &mut config.placement,
                        window.window().position(),
                        window.window().size(),
                        &monitors,
                    );
                }
                save_config(&config, config_path);
            }
            let _ = slint::quit_event_loop();
        }
    }
}

fn create_presentation_window(
    config: &Rc<RefCell<AppConfig>>,
    service: &Rc<PresentationService>,
    remote: &Rc<RemoteServer>,
    config_path: &std::path::Path,
) -> Result<PresentationWindow, slint::PlatformError> {
    let window = PresentationWindow::new()?;
    window::restore_remote_window(window.window(), &config.borrow().remote_control.window);
    let english = crate::config::ui_is_english(&config.borrow().language);
    window.set_window_title(
        if english {
            "Remote Control"
        } else {
            "远程控制"
        }
        .into(),
    );
    window.set_connection_page_text(
        if english {
            "Remote connection"
        } else {
            "远程连接"
        }
        .into(),
    );
    window.set_presentation_page_text(
        if english {
            "Presentations"
        } else {
            "演示文稿"
        }
        .into(),
    );
    window.set_connection_subtitle_text(
        if english {
            "Mobile or browser access"
        } else {
            "通过手机或浏览器控制演示"
        }
        .into(),
    );
    window.set_presentation_subtitle_text(
        if english {
            "Rules and slide show"
        } else {
            "规则与放映"
        }
        .into(),
    );
    window.set_next_port_label(
        if english {
            "Port on next start"
        } else {
            "下次服务端口"
        }
        .into(),
    );
    window.set_random_port_text(
        if english {
            "Use a random port"
        } else {
            "使用随机端口"
        }
        .into(),
    );
    window.set_restart_service_text(
        if english {
            "Restart remote service"
        } else {
            "重启远程服务"
        }
        .into(),
    );
    window.set_apply_port_text(
        if english {
            "Restart service and apply port"
        } else {
            "重启远程服务并应用端口"
        }
        .into(),
    );
    window.set_regenerate_token_text(
        if english {
            "Regenerate token"
        } else {
            "重新生成令牌"
        }
        .into(),
    );
    window.set_disconnect_text(
        if english {
            "Disconnect all devices"
        } else {
            "断开所有设备"
        }
        .into(),
    );
    window.set_copy_address_text(
        if english {
            "Copy access address"
        } else {
            "复制访问地址"
        }
        .into(),
    );
    window.set_open_local_text(
        if english {
            "Open local control page"
        } else {
            "打开本机控制页"
        }
        .into(),
    );
    window.set_copy_firewall_text(
        if english {
            "Copy repair command"
        } else {
            "复制修复命令"
        }
        .into(),
    );
    window.set_open_text(
        if english {
            "Open presentation"
        } else {
            "打开演示文稿"
        }
        .into(),
    );
    window.set_beginning_text(
        if english {
            "Start from beginning"
        } else {
            "从头放映"
        }
        .into(),
    );
    window.set_current_text(
        if english {
            "Start from current slide"
        } else {
            "当前页放映"
        }
        .into(),
    );
    window.set_previous_text(if english { "Previous" } else { "上一页" }.into());
    window.set_next_text(if english { "Next" } else { "下一页" }.into());
    window.set_goto_text(if english { "Go" } else { "跳转" }.into());
    window.set_black_text(if english { "Black screen" } else { "黑屏" }.into());
    window.set_white_text(if english { "White screen" } else { "白屏" }.into());
    window.set_restore_text(if english { "Restore" } else { "恢复" }.into());
    window.set_end_text(
        if english {
            "End slide show"
        } else {
            "结束放映"
        }
        .into(),
    );
    window.set_close_active_text(
        if english {
            "Close current presentation"
        } else {
            "关闭当前文档"
        }
        .into(),
    );
    window.set_close_managed_text(
        if english {
            "Close last-opened presentation"
        } else {
            "关闭最后打开的文稿"
        }
        .into(),
    );
    window.set_exit_text(
        if english {
            "Quit presentation software"
        } else {
            "退出演示软件"
        }
        .into(),
    );
    window.set_confirm_title(
        if english {
            "Confirm quit"
        } else {
            "确认退出软件"
        }
        .into(),
    );
    window.set_confirm_message(
        if english {
            "This force-closes all PowerPoint/WPS presentation processes. Unsaved work will be lost."
        } else {
            "将强制关闭全部 PowerPoint/WPS 演示进程，未保存内容会丢失。"
        }
        .into(),
    );
    window.set_cancel_command_text(if english { "Cancel" } else { "取消" }.into());
    update_presentation_window(&window, &service.state(), &config.borrow());
    let weak = window.as_weak();
    let service = Rc::clone(service);
    window.on_command(move |code, value| {
        let selected_path = weak.upgrade().and_then(|window| {
            let index = window.get_selected_presentation();
            (index >= 0)
                .then(|| window.get_presentations().row_data(index as usize))
                .flatten()
                .map(|item| PathBuf::from(item.path.as_str()))
        });
        let command = match code {
            0 => selected_path.map(PresentationCommand::Open),
            1 => Some(PresentationCommand::StartFromBeginning(selected_path)),
            2 => Some(PresentationCommand::StartFromCurrent(selected_path)),
            3 => Some(PresentationCommand::Previous),
            4 => Some(PresentationCommand::Next),
            5 => value
                .parse::<i32>()
                .ok()
                .map(PresentationCommand::GoToSlide),
            6 => Some(PresentationCommand::ToggleBlackScreen),
            7 => Some(PresentationCommand::ToggleWhiteScreen),
            8 => Some(PresentationCommand::RestoreScreen),
            9 => Some(PresentationCommand::EndShow),
            10 => Some(PresentationCommand::CloseActive),
            11 => Some(PresentationCommand::CloseLastOpened),
            12 => Some(PresentationCommand::ExitApplication),
            13 => Some(PresentationCommand::ForceQuitAll { confirmed: true }),
            _ => None,
        };
        if let Some(command) = command
            && let Err(error) = service.queue(command)
        {
            eprintln!("presentation command failed: {error}");
        }
    });
    let weak = window.as_weak();
    let config_for_remote = Rc::clone(config);
    let remote_for_actions = Rc::clone(remote);
    let config_path_for_actions = config_path.to_path_buf();
    window.on_remote_action(move |action, value, random| {
        match action {
            0 => {
                let mut config = config_for_remote.borrow_mut();
                if let Err(error) = remote_for_actions.start(&mut config) {
                    eprintln!("{error}");
                }
                save_config(&config, &config_path_for_actions);
            }
            1 => {
                let Ok(port) = value.parse::<u16>() else {
                    return;
                };
                let mut config = config_for_remote.borrow_mut();
                config.remote_control.enabled = true;
                config.remote_control.use_random_port = random;
                config.remote_control.port = port;
                if let Err(error) = remote_for_actions.start(&mut config) {
                    eprintln!("{error}");
                }
                save_config(&config, &config_path_for_actions);
            }
            2 => {
                let token = remote_for_actions.regenerate_token();
                let mut config = config_for_remote.borrow_mut();
                config.remote_control.token = token;
                save_config(&config, &config_path_for_actions);
            }
            3 => {
                let token = remote_for_actions.disconnect_all();
                let mut config = config_for_remote.borrow_mut();
                config.remote_control.token = token;
                save_config(&config, &config_path_for_actions);
            }
            4 => {
                let config = config_for_remote.borrow();
                if let Some(url) = preferred_remote_url(&remote_for_actions, &config) {
                    let _ = crate::remote::copy_text(&url);
                }
            }
            5 => {
                let config = config_for_remote.borrow();
                let port = effective_remote_port(&remote_for_actions, &config);
                let _ = crate::remote::open_url(&format!(
                    "http://127.0.0.1:{port}/?token={}",
                    config.remote_control.token
                ));
            }
            6 => {
                let _ = crate::remote::copy_text(&crate::remote::firewall_command(
                    effective_remote_port(&remote_for_actions, &config_for_remote.borrow()),
                ));
            }
            7 => {
                let mut config = config_for_remote.borrow_mut();
                if remote_for_actions.info().running {
                    config.remote_control.enabled = false;
                    remote_for_actions.stop();
                } else {
                    config.remote_control.enabled = true;
                    if let Err(error) = remote_for_actions.start(&mut config) {
                        eprintln!("{error}");
                    }
                }
                save_config(&config, &config_path_for_actions);
            }
            _ => {}
        }
        if let Some(window) = weak.upgrade() {
            populate_remote_connection_window(
                &window,
                &remote_for_actions,
                &config_for_remote.borrow(),
                true,
            );
        }
    });
    let weak = window.as_weak();
    let config_for_close = Rc::clone(config);
    let config_path_for_close = config_path.to_path_buf();
    window.window().on_close_requested(move || {
        if let Some(window) = weak.upgrade() {
            let mut config = config_for_close.borrow_mut();
            window::capture_remote_window(window.window(), &mut config.remote_control.window);
            save_config(&config, &config_path_for_close);
        }
        slint::CloseRequestResponse::HideWindow
    });
    populate_remote_connection_window(&window, remote, &config.borrow(), true);
    Ok(window)
}

fn update_presentation_window(
    window: &PresentationWindow,
    state: &crate::presentation::PresentationState,
    config: &AppConfig,
) {
    let english = crate::config::ui_is_english(&config.language);
    let status = if !state.operation.message.is_empty() {
        state.operation.message.clone()
    } else if !state.error.is_empty() {
        state.error.clone()
    } else if !state.message.is_empty() {
        state.message.clone()
    } else if state.slide_show_running {
        if english {
            "Presenting"
        } else {
            "正在放映"
        }
        .to_owned()
    } else if state.running {
        if english {
            "Presentation software is running"
        } else {
            "演示软件已运行"
        }
        .to_owned()
    } else {
        if english { "Not started" } else { "未启动" }.to_owned()
    };
    window.set_status_text(settings::ui_text(&status, english).into());
    window.set_document_text(state.presentation_name.clone().into());
    window.set_slide_text(
        if state.total_slides > 0 {
            format!("{}/{}", state.current_slide, state.total_slides)
        } else {
            String::new()
        }
        .into(),
    );
    let mut seen = std::collections::HashSet::new();
    let mut items = Vec::new();
    for presentation in &state.presentations {
        seen.insert(presentation.path.to_lowercase());
        items.push(PresentationItem {
            name: presentation.name.clone().into(),
            path: presentation.path.clone().into(),
        });
    }
    for rule in config
        .rules
        .iter()
        .filter(|rule| rule.enabled && !rule.file_path.trim().is_empty())
    {
        if seen.insert(rule.file_path.to_lowercase()) {
            items.push(PresentationItem {
                name: rule.file_name.clone().into(),
                path: rule.file_path.clone().into(),
            });
        }
    }
    window.set_presentations(ModelRc::new(VecModel::from(items)));
}

fn update_remote_connection_window(
    window: &PresentationWindow,
    remote: &RemoteServer,
    config: &AppConfig,
) {
    populate_remote_connection_window(window, remote, config, false);
}

fn populate_remote_connection_window(
    window: &PresentationWindow,
    remote: &RemoteServer,
    config: &AppConfig,
    refresh_addresses: bool,
) {
    let english = crate::config::ui_is_english(&config.language);
    let info = remote.info();
    window.set_service_status_text(
        if english {
            format!(
                "Service status: {}",
                if info.running {
                    "Running"
                } else {
                    "Not started"
                }
            )
        } else {
            format!("当前服务状态：{}", info.status)
        }
        .into(),
    );
    window.set_current_port_text(
        if english {
            format!("Current port: {}", info.current_port)
        } else {
            format!("本次启动端口：{}", info.current_port)
        }
        .into(),
    );
    window.set_next_port(config.remote_control.port.to_string().into());
    window.set_random_port(config.remote_control.use_random_port);
    window.set_client_count_text(
        if english {
            format!("Connected devices: {}", info.connected_clients)
        } else {
            format!("连接设备数量：{}", info.connected_clients)
        }
        .into(),
    );
    window.set_service_toggle_text(
        if english {
            if info.running {
                "Stop service"
            } else {
                "Start service"
            }
        } else if info.running {
            "停止服务"
        } else {
            "启动服务"
        }
        .into(),
    );
    let effective_port = effective_remote_port(remote, config);
    window.set_firewall_text(if english {
        format!("If the phone cannot connect, allow TCP port {} in Windows Firewall. FlyPPTTimer only provides the repair command and does not elevate automatically.\n{}", effective_port, crate::remote::firewall_command(effective_port))
    } else {
        format!("如果手机无法连接，请在 Windows 防火墙中允许 TCP 端口 {}。FlyPPTTimer 只提供修复命令，不会主动提权修改防火墙。\n{}", effective_port, crate::remote::firewall_command(effective_port))
    }.into());
    if refresh_addresses || window.get_recommended_url().is_empty() {
        let addresses = crate::remote::lan_addresses();
        window.set_address_list_text(
            if addresses.is_empty() {
                if english {
                    "No mobile-accessible LAN address found".to_owned()
                } else {
                    "未找到可用的手机局域网地址".to_owned()
                }
            } else {
                addresses.join("\n")
            }
            .into(),
        );
        if let Some(recommended) = preferred_remote_url(remote, config) {
            window.set_recommended_url(crate::remote::mask_token(&recommended).into());
            window.set_qr_image(crate::remote::qr_image(&recommended));
        } else {
            window.set_recommended_url(
                if english {
                    "No mobile-accessible LAN address found"
                } else {
                    "未检测到可供手机访问的局域网地址"
                }
                .into(),
            );
            window.set_qr_image(slint::Image::default());
        }
    }
}

fn preferred_remote_url(remote: &RemoteServer, config: &AppConfig) -> Option<String> {
    let address = crate::remote::lan_addresses().into_iter().next()?;
    let port = effective_remote_port(remote, config);
    Some(format!(
        "http://{address}:{port}/?token={}",
        config.remote_control.token
    ))
}

fn effective_remote_port(remote: &RemoteServer, config: &AppConfig) -> u16 {
    let current = remote.info().current_port;
    if current > 0 {
        current
    } else {
        config.remote_control.port.max(1)
    }
}

#[allow(clippy::too_many_arguments)]
fn handle_remote_request(
    request: RemoteRequest,
    window: &slint::Weak<AppWindow>,
    timer: &Rc<RefCell<Timer<SystemClock>>>,
    config: &Rc<RefCell<AppConfig>>,
    alerts: &Rc<RefCell<AlertTracker>>,
    flash: &Rc<RefCell<FlashController>>,
    presentation: &Rc<PresentationService>,
    time_up_window: &Rc<RefCell<Option<TimeUpWindow>>>,
    preserve_time_up: &Rc<Cell<bool>>,
    config_path: &std::path::Path,
) {
    let result = execute_remote_command(
        &request.command,
        window,
        timer,
        config,
        alerts,
        flash,
        presentation,
        time_up_window,
        preserve_time_up,
        config_path,
    );
    let response = result.map(|message| {
        let snapshot = timer.borrow().snapshot();
        let config = config.borrow();
        (
            crate::remote::remote_state(
                &snapshot,
                &config,
                &presentation.state(),
                format_snapshot(&snapshot, &config.appearance.overtime_prefix),
                crate::audio::system_mute().unwrap_or(false),
                preserve_time_up.get(),
            ),
            message,
        )
    });
    let _ = request.reply.send(response);
}

#[allow(clippy::too_many_arguments)]
fn execute_remote_command(
    command: &crate::remote::RemoteCommand,
    window: &slint::Weak<AppWindow>,
    timer: &Rc<RefCell<Timer<SystemClock>>>,
    config: &Rc<RefCell<AppConfig>>,
    alerts: &Rc<RefCell<AlertTracker>>,
    flash: &Rc<RefCell<FlashController>>,
    presentation: &Rc<PresentationService>,
    time_up_window: &Rc<RefCell<Option<TimeUpWindow>>>,
    preserve_time_up: &Rc<Cell<bool>>,
    config_path: &std::path::Path,
) -> Result<String, String> {
    let name = command.command.as_str();
    match name {
        "timer.start" => {
            let mut timer = timer.borrow_mut();
            timer.start();
            alerts.borrow_mut().reset();
            Ok("已开始".to_owned())
        }
        "timer.pause" => {
            timer.borrow_mut().pause();
            Ok("已暂停".to_owned())
        }
        "timer.resume" => {
            timer.borrow_mut().resume();
            Ok("已继续".to_owned())
        }
        "timer.stop" => {
            timer.borrow_mut().stop();
            Ok("已停止".to_owned())
        }
        "timer.reset" => {
            let mut timer = timer.borrow_mut();
            timer.stop_and_reset();
            alerts.borrow_mut().reset();
            Ok("已重置".to_owned())
        }
        "timer.restart" => {
            let mut timer = timer.borrow_mut();
            let path = presentation.state().presentation_path.clone();
            let (duration, mode) = if path.is_empty() {
                (timer.duration(), timer.mode())
            } else {
                let (duration, mode) =
                    crate::presentation::timer_settings_for(&config.borrow(), &path);
                (duration, timer_mode(mode))
            };
            let _ = timer.set_duration(duration);
            timer.set_mode(mode);
            timer.restart();
            alerts.borrow_mut().reset();
            Ok("已重新计时".to_owned())
        }
        "timer.setDuration" => {
            let duration: String = command
                .duration
                .clone()
                .or_else(|| {
                    command.duration_ms.map(|ms| {
                        let d = Duration::from_millis(ms as u64);
                        let total = d.as_secs();
                        format!(
                            "{:02}:{:02}:{:02}",
                            total / 3600,
                            (total % 3600) / 60,
                            total % 60
                        )
                    })
                })
                .ok_or("缺少时长参数")?;
            if !crate::config::is_valid_duration(&duration) {
                return Err("计时时长无效".to_owned());
            }
            let mut config = config.borrow_mut();
            config.timer.default_duration = duration.to_owned();
            let _ = timer.borrow_mut().set_duration(config.timer.duration());
            save_config(&config, config_path);
            Ok("时长已设置".to_owned())
        }
        "timer.setMode" => {
            let mode = match command.mode.as_deref() {
                Some("倒计时") | Some("countdown") => TimerMode::Countdown,
                Some("正计时") | Some("countup") => TimerMode::CountUp,
                _ => return Err("模式无效".to_owned()),
            };
            let mut config = config.borrow_mut();
            config.timer.mode = mode;
            timer.borrow_mut().set_mode(timer_mode(mode));
            save_config(&config, config_path);
            Ok("模式已设置".to_owned())
        }
        "window.show" => {
            if let Some(w) = window.upgrade() {
                set_window_visible(&w, true);
            }
            config.borrow_mut().placement.visible = true;
            Ok("已显示".to_owned())
        }
        "window.hide" => {
            if let Some(w) = window.upgrade() {
                set_window_visible(&w, false);
            }
            config.borrow_mut().placement.visible = false;
            Ok("已隐藏".to_owned())
        }
        "window.toggle" => {
            let visible = !config.borrow().placement.visible;
            if let Some(w) = window.upgrade() {
                set_window_visible(&w, visible);
            }
            config.borrow_mut().placement.visible = visible;
            Ok(if visible { "已显示" } else { "已隐藏" }.to_owned())
        }
        "window.flash" => {
            let prompt = flash_prompt_from_config(&config.borrow());
            flash.borrow_mut().start_prompt(&prompt);
            Ok("已触发闪烁".to_owned())
        }
        "mute.toggle" => {
            let muted = crate::audio::toggle_system_mute().map_err(|error| error.to_string())?;
            Ok(if muted {
                "电脑已静音"
            } else {
                "电脑声音已恢复"
            }
            .to_owned())
        }
        "timeup.dismiss" => {
            preserve_time_up.set(false);
            hide_time_up(time_up_window);
            Ok("已退出“时间到”黑屏".to_owned())
        }
        "ppt.refresh" => presentation.queue(PresentationCommand::Refresh),
        "ppt.openPresentation" => {
            let path = command
                .presentation_id
                .as_deref()
                .and_then(remote_path)
                .map(PathBuf::from);
            let command = path
                .map(PresentationCommand::Open)
                .ok_or("请先选择演示文稿。")?;
            presentation.queue(command)
        }
        "ppt.startFromBeginning" => {
            let path = command
                .presentation_id
                .as_deref()
                .and_then(remote_path)
                .map(PathBuf::from);
            presentation.queue(PresentationCommand::StartFromBeginning(path))
        }
        "ppt.startFromCurrent" => {
            let path = command
                .presentation_id
                .as_deref()
                .and_then(remote_path)
                .map(PathBuf::from);
            presentation.queue(PresentationCommand::StartFromCurrent(path))
        }
        "ppt.previous" => presentation.queue(PresentationCommand::Previous),
        "ppt.next" => presentation.queue(PresentationCommand::Next),
        "ppt.gotoSlide" => {
            let slide = command.slide_number.ok_or("请输入有效页码。")?;
            presentation.queue(PresentationCommand::GoToSlide(slide))
        }
        "ppt.blackScreenToggle" => presentation.queue(PresentationCommand::ToggleBlackScreen),
        "ppt.whiteScreenToggle" => presentation.queue(PresentationCommand::ToggleWhiteScreen),
        "ppt.endShow" => presentation.queue(PresentationCommand::EndShow),
        "ppt.closeActivePresentation" => presentation.queue(PresentationCommand::CloseActive),
        "ppt.closeCurrentPresentation" => presentation.queue(PresentationCommand::CloseLastOpened),
        "ppt.forceQuitAll" => {
            if command.confirmed != Some(true) {
                return Err("强制退出会丢失所有未保存内容，请再次确认。".to_owned());
            }
            presentation.queue(PresentationCommand::ForceQuitAll { confirmed: true })
        }
        _ => Err(format!("命令不被允许: {name}")),
    }
}

fn flash_prompt_from_config(config: &AppConfig) -> crate::config::PromptSettings {
    crate::config::PromptSettings {
        flash_style: config.appearance.flash_style.clone(),
        flash_on_ms: config.appearance.flash_on_ms,
        flash_off_ms: config.appearance.flash_off_ms,
        flash_seconds: 3,
        ..crate::config::PromptSettings::default()
    }
}

fn remote_path(id: &str) -> Option<String> {
    // presentation ids are normalized full paths (case-insensitive);
    // the presentation service re-validates the file on disk.
    Some(id.to_owned())
}

fn handle_command(
    command: &str,
    window: &slint::Weak<AppWindow>,
    timer: &Rc<RefCell<Timer<SystemClock>>>,
    config: &Rc<RefCell<AppConfig>>,
    alerts: &Rc<RefCell<AlertTracker>>,
    flash: &Rc<RefCell<FlashController>>,
    config_path: &std::path::Path,
) {
    match command {
        "startPause" => {
            let mut timer = timer.borrow_mut();
            if timer.state() == TimerState::Running {
                timer.pause();
            } else {
                alerts.borrow_mut().reset();
                timer.start();
            }
        }
        "start" => {
            alerts.borrow_mut().reset();
            timer.borrow_mut().start();
        }
        "pause" => timer.borrow_mut().pause(),
        "resume" => timer.borrow_mut().resume(),
        "stopReset" => {
            timer.borrow_mut().stop_and_reset();
            alerts.borrow_mut().reset();
        }
        "stop" => timer.borrow_mut().stop(),
        "reset" => {
            timer.borrow_mut().stop_and_reset();
            alerts.borrow_mut().reset();
        }
        "toggleWindow" => {
            let visible = !config.borrow().placement.visible;
            set_visibility_and_save(window, config, config_path, visible);
        }
        "showWindow" => set_visibility_and_save(window, config, config_path, true),
        "hideWindow" => set_visibility_and_save(window, config, config_path, false),
        "flash" => flash
            .borrow_mut()
            .start_prompt(&flash_prompt_from_config(&config.borrow())),
        "toggleMute" => {
            if let Err(error) = crate::audio::toggle_system_mute() {
                eprintln!("failed to toggle system mute: {error}");
            }
        }
        "toggleMode" => {
            let mut config = config.borrow_mut();
            config.timer.mode = match config.timer.mode {
                TimerMode::Countdown => TimerMode::CountUp,
                TimerMode::CountUp => TimerMode::Countdown,
            };
            timer.borrow_mut().set_mode(timer_mode(config.timer.mode));
            save_config(&config, config_path);
        }
        "addMinute" => change_duration(timer, config, config_path, 60),
        "subtractMinute" => change_duration(timer, config, config_path, -60),
        "preset3" => set_duration_minutes(timer, config, config_path, 3),
        "preset5" => set_duration_minutes(timer, config, config_path, 5),
        "preset8" => set_duration_minutes(timer, config, config_path, 8),
        "preset10" => set_duration_minutes(timer, config, config_path, 10),
        "preset15" => set_duration_minutes(timer, config, config_path, 15),
        _ => {}
    }
}

fn change_duration(
    timer: &Rc<RefCell<Timer<SystemClock>>>,
    config: &Rc<RefCell<AppConfig>>,
    config_path: &std::path::Path,
    delta_seconds: i64,
) {
    let seconds = (timer.borrow().duration().as_secs() as i64 + delta_seconds).max(60) as u64;
    set_duration(timer, config, config_path, Duration::from_secs(seconds));
}

fn set_duration_minutes(
    timer: &Rc<RefCell<Timer<SystemClock>>>,
    config: &Rc<RefCell<AppConfig>>,
    config_path: &std::path::Path,
    minutes: u64,
) {
    set_duration(
        timer,
        config,
        config_path,
        Duration::from_secs(minutes * 60),
    );
}

fn set_duration(
    timer: &Rc<RefCell<Timer<SystemClock>>>,
    config: &Rc<RefCell<AppConfig>>,
    config_path: &std::path::Path,
    duration: Duration,
) {
    let _ = timer.borrow_mut().set_duration(duration);
    let mut config = config.borrow_mut();
    config.timer.default_duration = duration_config_text(duration);
    save_config(&config, config_path);
}

fn duration_config_text(duration: Duration) -> String {
    let seconds = duration.as_secs();
    format!(
        "{:02}:{:02}:{:02}",
        seconds / 3_600,
        (seconds % 3_600) / 60,
        seconds % 60
    )
}

fn set_visibility_and_save(
    window: &slint::Weak<AppWindow>,
    config: &Rc<RefCell<AppConfig>>,
    config_path: &std::path::Path,
    visible: bool,
) {
    if let Some(window) = window.upgrade() {
        set_window_visible(&window, visible);
    }
    let mut config = config.borrow_mut();
    config.placement.visible = visible;
    save_config(&config, config_path);
}

fn save_config(config: &AppConfig, path: &std::path::Path) {
    if let Err(error) = config.save(path) {
        eprintln!("failed to save configuration: {error}");
    }
}

fn apply_timer_config(timer: &mut Timer<SystemClock>, config: &AppConfig) {
    let _ = timer.set_duration(config.timer.duration());
    timer.set_mode(timer_mode(config.timer.mode));
    timer.set_continue_overtime(config.timer.effective_continue_overtime());
}

fn timer_from_config(
    config: &AppConfig,
) -> Result<Timer<SystemClock>, crate::timer::InvalidDuration> {
    Timer::new(
        SystemClock::new(),
        config.timer.duration(),
        timer_mode(config.timer.mode),
        config.timer.effective_continue_overtime(),
    )
}

#[allow(clippy::too_many_arguments)]
fn rebuild_display_windows(
    root: &AppWindow,
    holder: &Rc<RefCell<DisplayWindows>>,
    config_state: Rc<RefCell<AppConfig>>,
    config_path: PathBuf,
    desktop: Rc<DesktopIntegration>,
    snapshot: &TimerSnapshot,
    config: &AppConfig,
    frame: FlashFrame,
) -> Result<(), slint::PlatformError> {
    let monitors = display::monitors();
    if monitors.is_empty() {
        return Ok(());
    }
    let mut displays = holder.borrow_mut();
    for overlay in displays.overlays.drain(..) {
        let _ = overlay.hide();
    }
    if let Some(big_screen) = displays.big_screen.take() {
        let _ = big_screen.hide();
    }

    let targets = display::timer_targets(&monitors, &config.placement);
    configure_timer_window(root, targets[0], config)?;
    update_window(&root.as_weak(), snapshot, config, frame);
    connect_timer_visibility(root, config.placement.visible);

    for monitor in targets.into_iter().skip(1) {
        let overlay = AppWindow::new()?;
        apply_config(&overlay, config);
        update_window(&overlay.as_weak(), snapshot, config, frame);
        connect_drag(&overlay, Rc::clone(&config_state), config_path.clone());
        connect_timer_menu(&overlay, Rc::clone(&desktop));
        configure_timer_window(&overlay, monitor, config)?;
        connect_timer_visibility(&overlay, config.placement.visible);
        displays.overlays.push(overlay);
    }

    let extended = display::extended_monitors(&monitors);
    if config.placement.big_screen_enabled && !extended.is_empty() {
        let monitor = extended
            .iter()
            .copied()
            .find(|monitor| {
                monitor
                    .device_name
                    .eq_ignore_ascii_case(&config.placement.big_screen_device_name)
            })
            .unwrap_or(extended[0]);
        let big_screen = BigScreenWindow::new()?;
        update_big_screen(&big_screen, snapshot, config);
        big_screen.show()?;
        let scale = monitor.dpi.max(96) as f32 / 96.0;
        big_screen
            .window()
            .set_position(slint::PhysicalPosition::new(
                monitor.work_area.x,
                monitor.work_area.y,
            ));
        big_screen.window().set_size(LogicalSize::new(
            monitor.work_area.width as f32 / scale,
            monitor.work_area.height as f32 / scale,
        ));
        big_screen.window().set_maximized(true);
        displays.big_screen = Some(big_screen);
    }
    displays.signature = display::signature(&monitors);
    Ok(())
}

fn configure_timer_window(
    window: &AppWindow,
    monitor: &display::DisplayMonitor,
    config: &AppConfig,
) -> Result<(), slint::PlatformError> {
    apply_config(window, config);
    let size = display::logical_size_physical(
        config.appearance.width,
        config.appearance.height,
        monitor.dpi,
    );
    window
        .window()
        .set_position(display::timer_position(monitor, &config.placement, size));
    window.show()?;
    window::apply_native_window(
        window.window(),
        config.controls.click_through,
        config.appearance.always_on_top,
        config.appearance.background_opacity,
        &config.appearance.shape,
    );
    let weak = window.as_weak();
    let shape = config.appearance.shape.clone();
    slint::Timer::single_shot(Duration::from_millis(80), move || {
        if let Some(window) = weak.upgrade() {
            window::refresh_shape(window.window(), &shape);
        }
    });
    Ok(())
}

fn update_display_windows(
    root: &slint::Weak<AppWindow>,
    holder: &Rc<RefCell<DisplayWindows>>,
    snapshot: &TimerSnapshot,
    config: &AppConfig,
    frame: FlashFrame,
) {
    update_window(root, snapshot, config, frame);
    if let Some(root) = root.upgrade() {
        connect_timer_visibility(&root, config.placement.visible);
    }
    let displays = holder.borrow();
    for overlay in &displays.overlays {
        update_window(&overlay.as_weak(), snapshot, config, frame);
        connect_timer_visibility(overlay, config.placement.visible);
    }
    if let Some(big_screen) = displays.big_screen.as_ref() {
        update_big_screen(big_screen, snapshot, config);
    }
}

fn update_big_screen(window: &BigScreenWindow, snapshot: &TimerSnapshot, config: &AppConfig) {
    let overtime = snapshot.state == TimerState::Finished || snapshot.is_overtime;
    window.set_display_text(format_snapshot(snapshot, &config.appearance.overtime_prefix).into());
    window.set_foreground_color(if overtime {
        parse_color(
            &config.appearance.timeout_text_color,
            Color::from_rgb_u8(0xFF, 0xFF, 0xFF),
        )
    } else {
        parse_color(
            &config.appearance.text_color,
            Color::from_rgb_u8(0x0B, 0x3A, 0x66),
        )
    });
    window.set_surface_color(Brush::SolidColor(if overtime {
        parse_color(
            &config.appearance.timeout_background_color,
            Color::from_rgb_u8(0xB0, 0x00, 0x20),
        )
    } else {
        parse_color(
            &config.appearance.background_color,
            Color::from_rgb_u8(0xF3, 0xF8, 0xFC),
        )
    }));
    window.set_timer_font_family(config.appearance.font_family.clone().into());
    let logical_height =
        window.window().size().height as f32 / window.window().scale_factor().max(0.1);
    window.set_timer_font_size((logical_height * 0.30).clamp(48.0, 360.0));
    window.set_keep_on_top(config.appearance.always_on_top);
}

fn connect_timer_visibility(window: &AppWindow, visible: bool) {
    if window::is_visible(window.window()) != visible {
        set_window_visible(window, visible);
    }
}

fn expand_timer_windows_if_needed(
    root: &AppWindow,
    displays: &DisplayWindows,
    config: &Rc<RefCell<AppConfig>>,
    path: &std::path::Path,
) {
    let mut config = config.borrow_mut();
    let width = (root.get_required_text_width().ceil() as i32)
        .max(config.appearance.width)
        .min(2000);
    let height = (root.get_required_text_height().ceil() as i32)
        .max(config.appearance.height)
        .min(1000);
    if width <= config.appearance.width && height <= config.appearance.height {
        return;
    }
    config.appearance.width = width;
    config.appearance.height = height;
    save_config(&config, path);
    for overlay in std::iter::once(root).chain(displays.overlays.iter()) {
        let old_position = overlay.window().position();
        let old_size = overlay.window().size();
        let scale = overlay.window().scale_factor();
        overlay
            .window()
            .set_size(LogicalSize::new(width as f32, height as f32));
        overlay.window().set_position(slint::PhysicalPosition::new(
            old_position.x + (old_size.width as i32 - (width as f32 * scale).round() as i32) / 2,
            old_position.y + (old_size.height as i32 - (height as f32 * scale).round() as i32) / 2,
        ));
        let weak = overlay.as_weak();
        let shape = config.appearance.shape.clone();
        slint::Timer::single_shot(Duration::from_millis(80), move || {
            if let Some(window) = weak.upgrade() {
                window::refresh_shape(window.window(), &shape);
            }
        });
    }
    let english = crate::config::ui_is_english(&config.language);
    drop(config);
    // Deliver the baseline notification after the refresh callback releases its borrows.
    slint::Timer::single_shot(Duration::ZERO, move || {
        settings::native::message(
            &if english {
                format!(
                    "The current timer text needs more space. The window was resized to {width} × {height}."
                )
            } else {
                format!("当前时间文字需要更大的显示区域，窗口已自动调整为 {width} × {height}。")
            },
            if english {
                "Presentation Timer"
            } else {
                "演讲计时器"
            },
            false,
        );
    });
}

pub(crate) fn apply_config(window: &AppWindow, config: &AppConfig) {
    let appearance = &config.appearance;
    window.window().set_size(LogicalSize::new(
        appearance.width.max(1) as f32,
        appearance.height.max(1) as f32,
    ));
    window.set_surface_color(Brush::SolidColor(parse_color(
        &appearance.background_color,
        Color::from_rgb_u8(0xF3, 0xF8, 0xFC),
    )));
    window.set_foreground_color(parse_color(
        &appearance.text_color,
        Color::from_rgb_u8(0x0B, 0x3A, 0x66),
    ));
    window.set_timer_font_size(appearance.font_size.max(1.0));
    window.set_timer_font_family(appearance.font_family.clone().into());
    window.set_timer_font_weight(if appearance.font_style.contains("Bold") {
        700
    } else {
        400
    });
    window.set_timer_font_italic(appearance.font_style.contains("Italic"));
    window.set_corner_radius(shape_radius(&appearance.shape));
    window.set_flash_color(parse_color(
        &appearance.flash_background_color,
        Color::from_rgb_u8(0x4E, 0xA3, 0xD8),
    ));
    window.set_keep_on_top(appearance.always_on_top);
    window.set_drag_enabled(!config.controls.lock_position && !config.controls.click_through);
}

fn update_window(
    window: &slint::Weak<AppWindow>,
    snapshot: &TimerSnapshot,
    config: &AppConfig,
    flash: FlashFrame,
) {
    let Some(window) = window.upgrade() else {
        return;
    };
    let overtime = snapshot.state == TimerState::Finished || snapshot.is_overtime;
    window.set_display_text(format_snapshot(snapshot, &config.appearance.overtime_prefix).into());
    let color = if overtime {
        parse_color(
            &config.appearance.timeout_text_color,
            Color::from_rgb_u8(0xFF, 0xFF, 0xFF),
        )
    } else {
        parse_color(
            &config.appearance.text_color,
            Color::from_rgb_u8(0x0B, 0x3A, 0x66),
        )
    };
    let background = if overtime {
        parse_color(
            &config.appearance.timeout_background_color,
            Color::from_rgb_u8(0xB0, 0x00, 0x20),
        )
    } else {
        parse_color(
            &config.appearance.background_color,
            Color::from_rgb_u8(0xF3, 0xF8, 0xFC),
        )
    };
    window.set_foreground_color(color);
    window.set_surface_color(Brush::SolidColor(background));
    window.set_flash_text_visible(flash.text_visible);
    window.set_flash_background_active(flash.background_active);
    window.set_flash_border_active(flash.border_active);
}

fn connect_drag(window: &AppWindow, config: Rc<RefCell<AppConfig>>, path: PathBuf) {
    let drag_start = Rc::new(RefCell::new(
        None::<(slint::PhysicalPosition, slint::PhysicalPosition)>,
    ));

    let weak_for_start = window.as_weak();
    let drag_start_for_start = Rc::clone(&drag_start);
    window.on_begin_drag(move || {
        if let (Some(window), Some(cursor)) = (weak_for_start.upgrade(), window::cursor_position())
        {
            *drag_start_for_start.borrow_mut() = Some((window.window().position(), cursor));
        }
    });

    let weak_for_move = window.as_weak();
    let drag_start_for_move = Rc::clone(&drag_start);
    window.on_drag_move(move || {
        let Some((window_start, cursor_start)) = *drag_start_for_move.borrow() else {
            return;
        };
        if let (Some(window), Some(cursor)) = (weak_for_move.upgrade(), window::cursor_position()) {
            let position = slint::PhysicalPosition::new(
                window_start.x + cursor.x - cursor_start.x,
                window_start.y + cursor.y - cursor_start.y,
            );
            if position != window.window().position() {
                window.window().set_position(position);
            }
        }
    });

    let weak_for_end = window.as_weak();
    let drag_start_for_end = Rc::clone(&drag_start);
    window.on_end_drag(move || {
        if drag_start_for_end.borrow_mut().take().is_none() {
            return;
        }
        if let Some(window) = weak_for_end.upgrade() {
            let monitors = display::monitors();
            if !monitors.is_empty() {
                display::capture_timer_position(
                    &mut config.borrow_mut().placement,
                    window.window().position(),
                    window.window().size(),
                    &monitors,
                );
                save_config(&config.borrow(), &path);
            }
        }
    });
}
fn connect_timer_menu(window: &AppWindow, desktop: Rc<DesktopIntegration>) {
    let weak = window.as_weak();
    window.on_show_menu(move || {
        if weak.upgrade().is_some() {
            desktop.show_timer_menu();
        }
    });
}

fn connect_close(window: &AppWindow, config: Rc<RefCell<AppConfig>>, path: PathBuf) {
    let weak_window = window.as_weak();
    window.window().on_close_requested(move || {
        if let Some(window) = weak_window.upgrade() {
            let mut config = config.borrow_mut();
            let monitors = display::monitors();
            if !monitors.is_empty() {
                display::capture_timer_position(
                    &mut config.placement,
                    window.window().position(),
                    window.window().size(),
                    &monitors,
                );
            }
            config.placement.visible = window::is_visible(window.window());
            if let Err(error) = config.save(&path) {
                eprintln!("failed to save configuration: {error}");
            }
        }
        if config.borrow().controls.close_button_behavior == CloseButtonBehavior::Exit {
            let _ = slint::quit_event_loop();
        } else if let Some(window) = weak_window.upgrade() {
            window::set_visible(window.window(), false);
            config.borrow_mut().placement.visible = false;
            save_config(&config.borrow(), &path);
        }
        slint::CloseRequestResponse::HideWindow
    });
}

fn set_window_visible(window: &AppWindow, visible: bool) {
    if visible && let Err(error) = window.show() {
        eprintln!("failed to show timer window: {error}");
    }
    window::set_visible(window.window(), visible);
}

fn format_snapshot(snapshot: &TimerSnapshot, overtime_prefix: &str) -> String {
    let show_hours = snapshot.duration.as_secs() >= 3_600
        || snapshot.elapsed.as_secs() >= 3_600
        || snapshot.display.as_secs() >= 3_600;
    if snapshot.mode == crate::timer::TimerMode::Countdown
        && (snapshot.state == TimerState::Finished || snapshot.is_overtime)
    {
        if snapshot.is_overtime {
            return format!(
                "{overtime_prefix}{}",
                format_duration(
                    snapshot.elapsed.saturating_sub(snapshot.duration),
                    show_hours
                )
            );
        }
        return format_duration(Duration::ZERO, show_hours);
    }
    format_duration(snapshot.display, show_hours)
}

fn format_duration(duration: Duration, force_hours: bool) -> String {
    let seconds = duration.as_secs();
    let hours = seconds / 3_600;
    if force_hours || hours > 0 {
        format!(
            "{hours:02}:{:02}:{:02}",
            (seconds % 3_600) / 60,
            seconds % 60
        )
    } else {
        format!("{:02}:{:02}", seconds / 60, seconds % 60)
    }
}

fn parse_color(value: &str, fallback: Color) -> Color {
    let hex = value.strip_prefix('#').unwrap_or(value);
    if hex.len() != 6 {
        return fallback;
    }
    u32::from_str_radix(hex, 16).map_or(fallback, |rgb| {
        Color::from_rgb_u8((rgb >> 16) as u8, (rgb >> 8) as u8, rgb as u8)
    })
}

fn shape_radius(shape: &str) -> f32 {
    if !shape.contains("圆角") {
        0.0
    } else if shape.contains('大') {
        14.0
    } else {
        7.0
    }
}

fn load_config(path: &std::path::Path) -> Result<AppConfig, Box<dyn std::error::Error>> {
    let mut config = if path.exists() {
        AppConfig::load(path)?
    } else {
        AppConfig::default()
    };
    let marker = path.with_file_name("install-language.txt");
    if marker.exists() {
        let mut apply = || -> Result<(), Box<dyn std::error::Error>> {
            let language = std::fs::read_to_string(&marker)?;
            config.language = match language.trim() {
                "en" => "en",
                "zh-CN" => "zh-CN",
                _ => "auto",
            }
            .into();
            config.save(path)?;
            std::fs::remove_file(&marker)?;
            Ok(())
        };
        if let Err(error) = apply() {
            crate::log::error(&format!("Unable to apply installer language: {error}"));
        }
    }
    Ok(config)
}

fn config_path() -> Result<PathBuf, std::io::Error> {
    let executable = std::env::current_exe()?;
    Ok(executable
        .parent()
        .unwrap_or_else(|| std::path::Path::new("."))
        .join("FlyPPTTimer.config.json"))
}
