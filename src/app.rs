use std::{
    cell::{Cell, RefCell},
    collections::BTreeSet,
    path::PathBuf,
    rc::Rc,
    time::{Duration, Instant},
};

use slint::{Brush, Color, ComponentHandle, LogicalSize, Model, ModelRc, SharedString, VecModel};

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
    // A new process opens management windows on the monitor where the user
    // invokes the tray command. Keep the saved size, but treat the previous
    // position as this-run state so a stale monitor never wins on startup.
    {
        let mut startup_config = config.borrow_mut();
        startup_config.remote_control.window.has_value = false;
        startup_config
            .remote_control
            .window
            .screen_device_name
            .clear();
    }
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
    let time_up_window = Rc::new(RefCell::new(Vec::new()));
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
        save_config(&cfg, &config_path);
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
        show_settings_ready(&settings)?;
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
    let escape_was_down = Cell::new(false);

    let refresh_timer = slint::Timer::default();
    refresh_timer.start(
        slint::TimerMode::Repeated,
        Duration::from_millis(100),
        move || {
            let escape_down = window::escape_key_down();
            let was_escape_down = escape_was_down.replace(escape_down);
            if preserve_time_up_for_updates.get() && escape_down && !was_escape_down {
                preserve_time_up_for_updates.set(false);
                hide_time_up(&time_up_for_updates);
            }
            if let Some((old, port)) = remote_for_updates.take_port_change() {
                let english = crate::config::ui_is_english(&config_for_updates.borrow().language);
                desktop_for_updates.notify(&if english {
                    format!("Port {old} is unavailable. Remote now uses port {port}; this port is saved for future starts.")
                } else {
                    format!("端口 {old} 不可用，远程服务已切换到 {port}，下次启动将继续使用此端口。")
                }, 10000);
            }
            while let Some(event) = desktop_for_updates.try_recv() {
                if let DesktopEvent::Command(command) = &event
                    && matches!(command.as_str(), "startPause" | "start" | "resume" | "stopReset" | "reset")
                {
                    preserve_time_up_for_updates.set(false);
                    hide_time_up(&time_up_for_updates);
                }
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
            if last_remote_update.get().elapsed() >= Duration::from_secs(1)
                && let Some(settings) = settings_for_updates.borrow().as_ref()
            {
                crate::settings::refresh_remote_status(settings, &remote_for_updates, &config_for_updates.borrow());
            }
            if let Some(control) = presentation_window_for_updates.borrow().as_ref() {
                update_presentation_window(
                    control,
                    &presentation_state,
                    &config_for_updates.borrow(),
                );
                if last_remote_update.get().elapsed() >= Duration::from_secs(1) { update_remote_connection_window(
                    control,
                    &remote_for_updates,
                    &config_for_updates.borrow(),
                ); }
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
            let mut action = lifecycle_for_updates.borrow_mut().observe_sample(
                (!presentation_state.state_unavailable || fullscreen_match.borrow().is_some())
                    .then_some(presentation_state.slide_show_running || fullscreen_match.borrow().is_some()),
                &presentation_state.presentation_path,
                &config_for_updates.borrow(),
            );
            if preserve_time_up_for_updates.get() && matches!(action, PresentationTimerAction::Start(_)) {
                action = PresentationTimerAction::None;
            }
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
            let preview = settings::preview_config(&config, settings_for_updates.borrow().as_ref());
            let label = page_label(preview.appearance.show_slide_numbers, presentation_state.current_slide, presentation_state.total_slides);
            if let Some(root) = weak_window.upgrade() { root.set_page_text(label.clone().into());
                root.set_page_reserve(page_reserve(presentation_state.total_slides).into()); }
            {
                let displays = display_windows_for_updates.borrow();
                for overlay in &displays.overlays { overlay.set_page_text(label.clone().into());
                    overlay.set_page_reserve(page_reserve(presentation_state.total_slides).into()); }
                if let Some(big) = &displays.big_screen { big.set_page_text(label.into()); }
            }
            update_display_windows(
                &weak_window,
                &display_windows_for_updates,
                &update.snapshot,
                &preview,
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
    hide_time_up(&time_up_window);
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
    holder: &Rc<RefCell<Vec<TimeUpWindow>>>,
    _preserve: &Rc<Cell<bool>>,
    language: &str,
) {
    hide_time_up(holder);
    for monitor in display::monitors() {
        let Ok(window) = TimeUpWindow::new() else {
            crate::log::error("Failed to create time-up cover");
            continue;
        };
        window.set_message(
            if crate::config::ui_is_english(language) {
                "TIME'S UP"
            } else {
                "时间到"
            }
            .into(),
        );
        if window.show().is_ok() {
            window::show_time_up_window(window.window(), monitor.bounds);
            holder.borrow_mut().push(window);
        }
    }
}

fn hide_time_up(holder: &Rc<RefCell<Vec<TimeUpWindow>>>) {
    for window in holder.borrow_mut().drain(..) {
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
        DesktopEvent::CloseBigScreen => {
            config.borrow_mut().placement.big_screen_enabled = false;
            save_config(&config.borrow(), config_path);
            if let Some(settings) = settings_window.borrow().as_ref() {
                settings.invoke_big_screen_disabled();
            }
            display_rebuild.set(true);
        }
        DesktopEvent::ResetPosition => {
            let mut config = config.borrow_mut();
            config.placement.has_custom_placement = false;
            save_config(&config, config_path);
            display_rebuild.set(true);
        }
        DesktopEvent::OpenSettings => {
            let existing_visible = settings_window
                .borrow()
                .as_ref()
                .is_some_and(|settings| window::is_visible(settings.window()));
            if existing_visible {
                if let Some(settings) = settings_window.borrow().as_ref()
                    && let Err(error) = show_settings_ready(settings)
                {
                    eprintln!("failed to show settings: {error}");
                }
                return;
            }
            let previous_settings_geometry = settings_window.borrow().as_ref().map(|settings| {
                let (position, size) = window::normal_window_geometry(settings.window());
                (position, Some(size))
            });
            // A hidden settings adapter can retain a stale native surface after
            // repeated close/reopen cycles. Recreate only a clean, non-dirty
            // hidden instance so an in-progress edit is never discarded.
            let can_recreate = settings_window
                .borrow()
                .as_ref()
                .is_some_and(|settings| !settings.get_dirty());
            if can_recreate {
                settings_window.borrow_mut().take();
            }
            if let Some(settings) = settings_window.borrow().as_ref() {
                if let Err(error) = show_settings_ready_at(settings, previous_settings_geometry) {
                    eprintln!("failed to show settings: {error}");
                }
                return;
            }
            if let Some(control) = presentation_window.borrow().as_ref() {
                if window::is_visible(control.window()) {
                    let mut current = config.borrow_mut();
                    window::capture_remote_window(
                        control.window(),
                        &mut current.remote_control.window,
                    );
                    save_config(&current, config_path);
                }
                if let Err(error) = control.hide() {
                    eprintln!("failed to hide presentation control: {error}");
                }
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
                    if let Err(error) =
                        show_settings_ready_at(&settings, previous_settings_geometry)
                    {
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
            let existing_visible = presentation_window
                .borrow()
                .as_ref()
                .is_some_and(|control| window::is_visible(control.window()));
            if existing_visible && let Some(control) = presentation_window.borrow().as_ref() {
                let (width, height) =
                    window::remote_window_size(&config.borrow().remote_control.window);
                if let Err(error) = show_presentation_ready(
                    control,
                    width,
                    height,
                    config.borrow().remote_control.window.clone(),
                ) {
                    eprintln!("failed to show presentation control: {error}");
                }
                populate_remote_connection_window(control, remote, &config.borrow(), true);
                return;
            }
            // A closed Slint window can retain a hidden adapter whose backing
            // surface is no longer drawable.  Drop that hidden instance and
            // create the next remote window afresh; the saved placement is
            // restored by `create_presentation_window`.
            if let Some(control) = presentation_window.borrow().as_ref() {
                let mut current = config.borrow_mut();
                window::capture_remote_window(control.window(), &mut current.remote_control.window);
                save_config(&current, config_path);
            }
            presentation_window.borrow_mut().take();
            match create_presentation_window(config, presentation, remote, config_path) {
                Ok(control) => {
                    let (width, height) =
                        window::remote_window_size(&config.borrow().remote_control.window);
                    if let Err(error) = show_presentation_ready(
                        &control,
                        width,
                        height,
                        config.borrow().remote_control.window.clone(),
                    ) {
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
    window.set_add_file_text(if english { "Add" } else { "添加" }.into());
    window.set_delete_file_text(if english { "Delete" } else { "删除" }.into());
    window.set_refresh_list_text(if english { "Refresh" } else { "刷新" }.into());
    window.set_clear_list_text(
        if english {
            "Clear list"
        } else {
            "清空列表"
        }
        .into(),
    );
    window.set_rule_duration_text(if english { "Duration" } else { "时长" }.into());
    window.set_rule_mode_text(if english { "Mode" } else { "模式" }.into());
    window.set_rule_status_text(if english { "Status" } else { "状态" }.into());
    window.set_rule_enabled_text(if english { "Enabled" } else { "启用规则" }.into());
    window.set_rule_disabled_text(if english { "Disabled" } else { "禁用规则" }.into());
    window.set_save_rule_text(if english { "Save" } else { "保存" }.into());
    window.set_batch_rule_text(
        if english {
            "Batch edit"
        } else {
            "批量设置"
        }
        .into(),
    );
    window.set_batch_cancel_text(if english { "Cancel" } else { "取消" }.into());
    window.set_batch_confirm_text(if english { "Apply" } else { "应用" }.into());
    window.set_batch_title(
        if english {
            "Batch edit"
        } else {
            "批量设置"
        }
        .into(),
    );
    window.set_batch_count_text(
        if english {
            "Selected 0 rules"
        } else {
            "已选择 0 条规则"
        }
        .into(),
    );
    window.set_batch_duration_text(
        if english {
            "Set duration"
        } else {
            "统一时长"
        }
        .into(),
    );
    window.set_batch_mode_text(
        if english {
            "Set timer mode"
        } else {
            "统一计时方式"
        }
        .into(),
    );
    window.set_timer_modes(ModelRc::new(VecModel::from(vec![
        SharedString::from(if english { "Countdown" } else { "倒计时" }),
        SharedString::from(if english { "Count up" } else { "正计时" }),
    ])));
    update_presentation_window(&window, &service.state(), &config.borrow());
    let weak = window.as_weak();
    let config_for_rules = Rc::clone(config);
    let service_for_rules = Rc::clone(service);
    let config_path_for_rules = config_path.to_path_buf();
    let selection_anchor = Rc::new(Cell::new(-1_i32));
    let selection_anchor_for_click = Rc::clone(&selection_anchor);
    let service_for_selection = Rc::clone(service);
    let config_for_selection = Rc::clone(config);
    window.on_presentation_selected(move |index, control, shift| {
        let Some(window) = weak.upgrade() else {
            return;
        };
        if index < 0 {
            return;
        }
        let index = index as usize;
        let mut selected = presentation_selection(&window);
        let anchor = selection_anchor_for_click.get();
        if shift && anchor >= 0 {
            if !control {
                selected.clear();
            }
            let anchor = anchor as usize;
            let start = anchor.min(index);
            let end = anchor.max(index);
            selected.extend(start..=end);
        } else if control {
            if !selected.insert(index) {
                selected.remove(&index);
            }
            selection_anchor_for_click.set(index as i32);
        } else {
            selected.clear();
            selected.insert(index);
            selection_anchor_for_click.set(index as i32);
        }
        window.set_selected_presentation(index as i32);
        let config = config_for_selection.borrow();
        update_presentation_window_with_selection(
            &window,
            &service_for_selection.state(),
            &config,
            &selected,
        );
        sync_presentation_editor(&window);
    });
    let weak = window.as_weak();
    let selection_anchor_for_actions = Rc::clone(&selection_anchor);
    window.on_rule_action(move |action, value| {
        let Some(window) = weak.upgrade() else {
            return;
        };
        match action {
            0 => {
                let paths = settings::native_open_presentations();
                if paths.is_empty() {
                    return;
                }
                let mut config = config_for_rules.borrow_mut();
                let default_duration = config.timer.default_duration.clone();
                let default_mode = config.timer.mode;
                for path in paths {
                    if !settings::is_supported_presentation_path(&path) {
                        continue;
                    }
                    let full_path = path.canonicalize().unwrap_or(path);
                    let identity = settings::presentation_identity(&full_path);
                    let full = full_path.to_string_lossy().into_owned();
                    if config.rules.iter().any(|rule| {
                        settings::presentation_identity(std::path::Path::new(&rule.file_path))
                            == identity
                    }) {
                        continue;
                    }
                    let mobile_order = crate::config::next_mobile_order(&config.rules);
                    config.rules.push(crate::config::FileRule {
                        mobile_order,
                        file_name: std::path::Path::new(&full)
                            .file_name()
                            .map(|name| name.to_string_lossy().into_owned())
                            .unwrap_or_else(|| full.clone()),
                        file_path: full,
                        duration: default_duration.clone(),
                        mode: default_mode,
                        enabled: true,
                        ..crate::config::FileRule::default()
                    });
                }
                save_config(&config, &config_path_for_rules);
                update_presentation_window(&window, &service_for_rules.state(), &config);
            }
            1 => {
                let paths = presentation_selection(&window)
                    .into_iter()
                    .filter_map(|index| window.get_presentations().row_data(index))
                    .filter(|item| item.is_rule)
                    .map(|item| item.path.to_lowercase())
                    .collect::<BTreeSet<_>>();
                if paths.is_empty() {
                    return;
                }
                let mut config = config_for_rules.borrow_mut();
                config
                    .rules
                    .retain(|rule| !paths.contains(&rule.file_path.to_lowercase()));
                save_config(&config, &config_path_for_rules);
                window.set_selected_presentation(-1);
                selection_anchor_for_actions.set(-1);
                update_presentation_window_with_selection(
                    &window,
                    &service_for_rules.state(),
                    &config,
                    &BTreeSet::new(),
                );
            }
            2 => {
                let _ = service_for_rules.queue(PresentationCommand::Refresh);
            }
            3 => {
                let mut config = config_for_rules.borrow_mut();
                config.rules.clear();
                save_config(&config, &config_path_for_rules);
                window.set_selected_presentation(-1);
                selection_anchor_for_actions.set(-1);
                update_presentation_window_with_selection(
                    &window,
                    &service_for_rules.state(),
                    &config,
                    &BTreeSet::new(),
                );
            }
            4 => {
                if !crate::config::is_valid_duration(value.as_str()) {
                    return;
                }
                let paths = presentation_selection(&window)
                    .into_iter()
                    .filter_map(|index| window.get_presentations().row_data(index))
                    .filter(|item| item.is_rule)
                    .map(|item| item.path.to_lowercase())
                    .collect::<BTreeSet<_>>();
                if paths.is_empty() {
                    return;
                }
                let mut config = config_for_rules.borrow_mut();
                for rule in config
                    .rules
                    .iter_mut()
                    .filter(|rule| paths.contains(&rule.file_path.to_lowercase()))
                {
                    rule.duration = value.to_string();
                    rule.mode = if window.get_rule_mode() == 0 {
                        TimerMode::Countdown
                    } else {
                        TimerMode::CountUp
                    };
                }
                save_config(&config, &config_path_for_rules);
                update_presentation_window(&window, &service_for_rules.state(), &config);
            }
            5 => {
                let selected = presentation_selection(&window);
                if selected.is_empty() {
                    return;
                }
                if let Some(item) = selected
                    .iter()
                    .filter_map(|index| window.get_presentations().row_data(*index))
                    .find(|item| item.is_rule)
                {
                    window.set_batch_duration(item.duration);
                    window.set_batch_mode(item.mode);
                }
                window.set_batch_count_text(
                    if config_for_rules.borrow().language == "en" {
                        format!("Selected {} rules", selected.len())
                    } else {
                        format!("已选择 {} 条规则", selected.len())
                    }
                    .into(),
                );
                window.set_batch_error("".into());
                window.set_batch_open(true);
            }
            _ => {}
        }
    });
    let weak = window.as_weak();
    let config_for_batch = Rc::clone(config);
    let service_for_batch = Rc::clone(service);
    let config_path_for_batch = config_path.to_path_buf();
    window.on_batch_confirm(move |duration, mode| {
        let Some(window) = weak.upgrade() else {
            return;
        };
        if !crate::config::is_valid_duration(duration.as_str()) {
            window.set_batch_error(
                if crate::config::ui_is_english(&config_for_batch.borrow().language) {
                    "A presentation rule has an invalid duration."
                } else {
                    "文件规则“文件”的计时时长无效。"
                }
                .into(),
            );
            return;
        }
        let selected = presentation_selection(&window);
        if selected.is_empty() {
            window.set_batch_open(false);
            return;
        }
        let paths = selected
            .iter()
            .filter_map(|index| window.get_presentations().row_data(*index))
            .filter(|item| item.is_rule)
            .map(|item| item.path.to_lowercase())
            .collect::<BTreeSet<_>>();
        let mut config = config_for_batch.borrow_mut();
        for rule in config
            .rules
            .iter_mut()
            .filter(|rule| paths.contains(&rule.file_path.to_lowercase()))
        {
            rule.duration = duration.to_string();
            rule.mode = if mode == 0 {
                TimerMode::Countdown
            } else {
                TimerMode::CountUp
            };
        }
        save_config(&config, &config_path_for_batch);
        update_presentation_window_with_selection(
            &window,
            &service_for_batch.state(),
            &config,
            &selected,
        );
        sync_presentation_editor(&window);
        window.set_batch_error("".into());
        window.set_batch_open(false);
    });
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
    window.on_remote_action(move |action, value, _random| {
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
                config.remote_control.use_random_port = false;
                config.remote_control.port = port;
                if let Err(error) = remote_for_actions.start(&mut config) {
                    eprintln!("{error}");
                }
                save_config(&config, &config_path_for_actions);
                if let Some(window) = weak.upgrade() {
                    window.set_next_port(config.remote_control.port.to_string().into());
                }
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
                if let Some(url) = preferred_remote_url(&remote_for_actions, &config) {
                    let _ = crate::remote::open_url(&url);
                }
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
    window.set_next_port(config.borrow().remote_control.port.to_string().into());
    populate_remote_connection_window(&window, remote, &config.borrow(), true);
    Ok(window)
}

fn update_presentation_window(
    window: &PresentationWindow,
    _state: &crate::presentation::PresentationState,
    config: &AppConfig,
) {
    let selected = presentation_selection(window);
    update_presentation_window_with_selection(window, _state, config, &selected);
}

fn presentation_selection(window: &PresentationWindow) -> BTreeSet<usize> {
    let model = window.get_presentations();
    let mut selected = BTreeSet::new();
    let mut index = 0;
    while let Some(item) = model.row_data(index) {
        if item.selected {
            selected.insert(index);
        }
        index += 1;
    }
    selected
}

fn update_presentation_window_with_selection(
    window: &PresentationWindow,
    _state: &crate::presentation::PresentationState,
    config: &AppConfig,
    selected: &BTreeSet<usize>,
) {
    // The PC remote page mirrors the settings file-rule list. Runtime
    // presentation state is intentionally kept out of this editor so that
    // selecting a row always edits the same persisted rule.
    let items = config
        .rules
        .iter()
        .filter(|rule| !rule.file_path.trim().is_empty())
        .enumerate()
        .map(|(index, rule)| PresentationItem {
            name: rule.file_name.clone().into(),
            path: rule.file_path.clone().into(),
            duration: rule.duration.clone().into(),
            mode: match rule.mode {
                TimerMode::Countdown => 0,
                TimerMode::CountUp => 1,
            },
            enabled: rule.enabled,
            is_rule: true,
            selected: selected.contains(&index),
        })
        .collect::<Vec<_>>();
    let current = window.get_selected_presentation();
    let valid_current = current >= 0
        && items
            .get(current as usize)
            .is_some_and(|item| item.selected);
    let next = if valid_current {
        current
    } else {
        items
            .iter()
            .position(|item| item.selected)
            .map_or(-1, |i| i as i32)
    };
    let previous_path = (current >= 0)
        .then(|| window.get_presentations().row_data(current as usize))
        .flatten()
        .map(|item| item.path);
    let next_path = (next >= 0).then(|| items[next as usize].path.clone());
    window.set_presentations(ModelRc::new(VecModel::from(items)));
    window.set_selected_presentation(next);
    // Periodic list updates must not overwrite an unfinished duration edit.
    if current != next || previous_path != next_path {
        sync_presentation_editor(window);
    }
}

fn sync_presentation_editor(window: &PresentationWindow) {
    let index = window.get_selected_presentation();
    if index >= 0
        && let Some(item) = window.get_presentations().row_data(index as usize)
    {
        window.set_rule_duration(item.duration);
        window.set_rule_mode(item.mode);
    }
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
    let recommended = preferred_remote_url(remote, config).unwrap_or_default();
    if refresh_addresses || window.get_connection_url().as_str() != recommended {
        window.set_connection_url(recommended.clone().into());
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
        if !recommended.is_empty() {
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
    time_up_window: &Rc<RefCell<Vec<TimeUpWindow>>>,
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
    time_up_window: &Rc<RefCell<Vec<TimeUpWindow>>>,
    preserve_time_up: &Rc<Cell<bool>>,
    config_path: &std::path::Path,
) -> Result<String, String> {
    let name = command.command.as_str();
    if name.starts_with("timer.") || name == "state.get" {
        let result = execute_remote_timer_command(command, timer, config, alerts, config_path);
        if result.is_ok()
            && matches!(
                name,
                "timer.start" | "timer.restart" | "timer.reset" | "timer.resume"
            )
        {
            preserve_time_up.set(false);
            hide_time_up(time_up_window);
        }
        return result;
    }
    if name.starts_with("ppt.") && name != "ppt.refresh" && preserve_time_up.get() {
        preserve_time_up.set(false);
        hide_time_up(time_up_window);
    }
    if name.starts_with("rules.") {
        let mut updated = config.borrow().clone();
        crate::mobile_rules::execute(&mut updated, &presentation.state(), command)?;
        updated
            .save(config_path)
            .map_err(|error| error.to_string())?;
        *config.borrow_mut() = updated;
        return Ok("列表已更新".into());
    }
    match name {
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

fn execute_remote_timer_command(
    command: &crate::remote::RemoteCommand,
    timer: &Rc<RefCell<Timer<SystemClock>>>,
    config: &Rc<RefCell<AppConfig>>,
    alerts: &Rc<RefCell<AlertTracker>>,
    config_path: &std::path::Path,
) -> Result<String, String> {
    let presentation_id = command
        .presentation_id
        .as_deref()
        .filter(|id| !id.trim().is_empty());
    match command.command.as_str() {
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
            timer.borrow_mut().stop_and_reset();
            alerts.borrow_mut().reset();
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
            let config = config.borrow();
            let rule = presentation_id.and_then(|id| {
                config.rules.iter().find(|rule| {
                    !rule.file_path.trim().is_empty()
                        && crate::remote::id_for_path(&rule.file_path) == id
                })
            });
            let mut settings = config.timer.clone();
            if let Some(rule) = rule {
                settings.default_duration = rule.duration.clone();
                settings.mode = rule.mode;
            }
            timer
                .set_duration(settings.duration())
                .map_err(|error| error.to_string())?;
            timer.set_mode(timer_mode(settings.mode));
            timer.restart();
            alerts.borrow_mut().reset();
            Ok(match rule {
                Some(rule) => format!("已按 {} 的规则时长重新计时", rule.file_name),
                None => "已按全局时长重新计时".to_owned(),
            })
        }
        "timer.setDuration" => {
            let seconds = if let Some(ms) = command.duration_ms.filter(|ms| *ms > 0) {
                (ms as f64 / 1000.0).round_ties_even()
            } else {
                crate::config::parse_duration(command.duration.as_deref().ok_or("缺少时长参数")?)
                    .ok_or("计时时长无效")?
                    .as_secs_f64()
            };
            let total = seconds.clamp(1.0, 86399.0) as u64;
            let duration = format!(
                "{:02}:{:02}:{:02}",
                total / 3600,
                (total % 3600) / 60,
                total % 60
            );
            let mut config = config.borrow_mut();
            config.timer.default_duration = duration.clone();
            for rule in &mut config.rules {
                if command.sync_all_rules == Some(true)
                    || (!rule.file_path.trim().is_empty()
                        && presentation_id
                            == Some(crate::remote::id_for_path(&rule.file_path).as_str()))
                {
                    rule.duration = duration.clone();
                }
            }
            timer
                .borrow_mut()
                .set_duration(config.timer.duration())
                .map_err(|error| error.to_string())?;
            config
                .save(config_path)
                .map_err(|error| error.to_string())?;
            Ok("时长已设置".to_owned())
        }
        "timer.setMode" => {
            let mode = match command.mode.as_deref() {
                Some("正计时") | Some("countup") => TimerMode::CountUp,
                _ => TimerMode::Countdown,
            };
            let mut config = config.borrow_mut();
            config.timer.mode = mode;
            if let Some(rule) = presentation_id.and_then(|id| {
                config.rules.iter_mut().find(|rule| {
                    !rule.file_path.trim().is_empty()
                        && crate::remote::id_for_path(&rule.file_path) == id
                })
            }) {
                rule.mode = mode;
            }
            timer.borrow_mut().set_mode(timer_mode(mode));
            config
                .save(config_path)
                .map_err(|error| error.to_string())?;
            Ok("模式已设置".to_owned())
        }
        "state.get" => Ok(String::new()),
        _ => Err("命令不被允许".to_owned()),
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
            match timer.state() {
                TimerState::Running => timer.pause(),
                TimerState::Paused => timer.resume(),
                TimerState::Stopped | TimerState::Finished => {
                    alerts.borrow_mut().reset();
                    timer.start();
                }
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
        big_screen.window().on_close_requested(|| {
            crate::desktop::request_close_big_screen();
            slint::CloseRequestResponse::HideWindow
        });
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
    // Apply the tool-window style before the first visible frame so timer windows
    // never acquire an AppWindow/taskbar button during startup.
    window::apply_native_window(
        window.window(),
        config.controls.click_through,
        config.appearance.always_on_top,
        config.appearance.background_opacity,
        &config.appearance.shape,
    );
    window.show()?;
    // The native handle is finalized by the first event-loop turn. Keep the
    // timer hidden until that handle has received its popup/tool-window flags.
    window::set_visible(window.window(), false);
    window::apply_native_window(
        window.window(),
        config.controls.click_through,
        config.appearance.always_on_top,
        config.appearance.background_opacity,
        &config.appearance.shape,
    );
    let weak = window.as_weak();
    let shape = config.appearance.shape.clone();
    let placement = config.placement.clone();
    let monitor = monitor.clone();
    let click_through = config.controls.click_through;
    let always_on_top = config.appearance.always_on_top;
    let opacity = config.appearance.background_opacity;
    let visible = config.placement.visible;
    let logical_size = LogicalSize::new(
        config.appearance.width as f32,
        config.appearance.height as f32,
    );
    slint::Timer::single_shot(Duration::from_millis(80), move || {
        if let Some(window) = weak.upgrade() {
            // Once the window has crossed a per-monitor-DPI boundary, Slint has
            // the target scale factor. Re-apply the logical size and physical
            // anchor then, keeping the configured dimensions and rounded region
            // in sync with the new monitor.
            window.window().set_size(logical_size);
            let size = display::logical_size_physical(
                logical_size.width as i32,
                logical_size.height as i32,
                monitor.dpi,
            );
            window
                .window()
                .set_position(display::timer_position(&monitor, &placement, size));
            // Winit may expose the native HWND only after the first event-loop
            // turn. Repeat the native flags here so the first visible timer
            // frame is also a tool window and cannot leave a taskbar button.
            window::apply_native_window(
                window.window(),
                click_through,
                always_on_top,
                opacity,
                &shape,
            );
            window::refresh_shape(window.window(), &shape);
            window::handle_timer_frame_paint(window.window());
            window::set_visible(window.window(), visible);
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
    if let Some(root_window) = root.upgrade() {
        sync_timer_window_scale(&root_window, config);
    }
    if let Some(root) = root.upgrade() {
        connect_timer_visibility(&root, config.placement.visible);
    }
    let displays = holder.borrow();
    for overlay in &displays.overlays {
        update_window(&overlay.as_weak(), snapshot, config, frame);
        sync_timer_window_scale(overlay, config);
        connect_timer_visibility(overlay, config.placement.visible);
    }
    if let Some(big_screen) = displays.big_screen.as_ref() {
        update_big_screen(big_screen, snapshot, config);
    }
}

fn sync_timer_window_scale(window: &AppWindow, config: &AppConfig) {
    let (width, height) = timer_content_size(
        config,
        window.get_required_text_width(),
        window.get_required_text_height(),
    );
    window.set_content_width(width as f32);
    window.set_content_height(height as f32);
    window::resize_for_dpi(window.window(), width, height);
    // Region dimensions are physical and can change after WM_DPICHANGED.
    // Refreshing it with the current client rect keeps the configured corners
    // aligned while a timer is dragged between monitors.
    window::refresh_shape(window.window(), &config.appearance.shape);
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
    window.set_timer_font_size(
        (logical_height * 0.30 * config.appearance.font_size / 18.0).clamp(24.0, 360.0),
    );
    window.set_keep_on_top(config.appearance.always_on_top);
}

fn connect_timer_visibility(window: &AppWindow, visible: bool) {
    if window::is_visible(window.window()) != visible {
        set_window_visible(window, visible);
    }
}

fn timer_content_size(config: &AppConfig, required_width: f32, required_height: f32) -> (i32, i32) {
    (
        config
            .appearance
            .width
            .max(required_width.ceil() as i32)
            .clamp(1, 2000),
        config
            .appearance
            .height
            .max(required_height.ceil() as i32)
            .clamp(1, 1000),
    )
}

fn page_reserve(total: i32) -> String {
    let digits = "8".repeat(total.max(1).to_string().len());
    format!("{digits}/{digits}")
}

fn page_label(enabled: bool, current: i32, total: i32) -> String {
    if enabled && current > 0 && total > 0 {
        format!("{current}/{total}")
    } else {
        String::new()
    }
}

pub(crate) fn apply_config(window: &AppWindow, config: &AppConfig) {
    let appearance = &config.appearance;
    window.set_content_width(appearance.width.max(1) as f32);
    window.set_content_height(appearance.height.max(1) as f32);
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
    window.set_timer_font_size(config.appearance.font_size);
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

fn show_settings_ready(window: &SettingsWindow) -> Result<(), slint::PlatformError> {
    show_settings_ready_at(window, None)
}

fn show_settings_ready_at(
    window: &SettingsWindow,
    geometry: Option<(slint::PhysicalPosition, Option<slint::PhysicalSize>)>,
) -> Result<(), slint::PlatformError> {
    // Defer native creation until the current desktop-event callback returns.
    // Keep one Slint show/hide lifecycle for the native surface: nested
    // show/hide timers can leave a stale white surface after a close/reopen.
    if window::is_visible(window.window()) {
        if let Err(error) = window.show() {
            eprintln!("failed to refresh settings: {error}");
            return Ok(());
        }
        window::foreground(window.window());
        window.window().request_redraw();
        return Ok(());
    }
    // Keep the actual client pixels when reopening a resized window.  Using
    // Slint's cached size here would apply the monitor scale factor twice on
    // mixed-DPI desktops, so this path deliberately stays in physical pixels.
    let previous_size = geometry.as_ref().and_then(|(_, size)| *size);
    let position = geometry.map(|(position, _)| position);
    let weak = window.as_weak();
    slint::Timer::single_shot(Duration::from_millis(1), move || {
        if let Some(window) = weak.upgrade() {
            if let Err(error) = window.show() {
                eprintln!("failed to show settings: {error}");
                return;
            }
            // Wait for Winit to deliver the initial resize/DPI event before
            // applying saved geometry. Otherwise the first reopen uses a
            // fallback scale and the window drifts on every cycle.
            let weak = window.as_weak();
            slint::Timer::single_shot(Duration::from_millis(50), move || {
                if let Some(window) = weak.upgrade() {
                    // The HWND has received its DPI by this turn, so reuse
                    // the saved physical client size directly. Converting it
                    // through Slint's logical scale here would apply DPI a
                    // second time and shrink on every reopen.
                    if let Some(position) = position {
                        window.window().set_position(position);
                    } else {
                        window::center_window_on_cursor(window.window(), window.window().size());
                    }
                    // Move to the saved monitor first so Winit finishes its DPI
                    // change before receiving the saved physical client size.
                    let physical_size = previous_size.unwrap_or_else(|| {
                        window::logical_size_to_physical(window.window(), 900, 650)
                    });
                    window.window().set_size(physical_size);
                    window::install_settings_dpi_stabilizer(window.window());
                    window::set_visible(window.window(), true);
                    window::foreground(window.window());
                    window.window().request_redraw();
                }
            });
        }
    });
    Ok(())
}

fn show_presentation_ready(
    window: &PresentationWindow,
    logical_width: i32,
    logical_height: i32,
    placement: crate::config::RemoteWindowPlacement,
) -> Result<(), slint::PlatformError> {
    if window::is_visible(window.window()) {
        if let Err(error) = window.show() {
            eprintln!("failed to refresh remote control: {error}");
            return Ok(());
        }
        window::foreground(window.window());
        window.window().request_redraw();
        return Ok(());
    }
    let weak = window.as_weak();
    slint::Timer::single_shot(Duration::from_millis(1), move || {
        if let Some(window) = weak.upgrade() {
            if let Err(error) = window.show() {
                eprintln!("failed to show remote control: {error}");
                return;
            }
            window::set_visible(window.window(), false);
            window::restore_remote_window_position(window.window(), &placement);
            // Remote placement is stored in 96-DPI logical pixels.  Submit
            // the corresponding physical client size after the HWND knows
            // its monitor DPI; sending LogicalSize here would apply the
            // monitor scale a second time.
            let physical_size =
                window::logical_size_to_physical(window.window(), logical_width, logical_height);
            window.window().set_size(physical_size);
            window::install_settings_dpi_stabilizer(window.window());
            window.window().set_maximized(placement.maximized);
            if let Err(error) = window.show() {
                eprintln!("failed to reveal remote control: {error}");
                return;
            }
            window::foreground(window.window());
            window.window().request_redraw();
        }
    });
    Ok(())
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

#[cfg(test)]
mod remote_parity_tests {
    use super::*;
    use crate::{config::FileRule, remote::RemoteCommand};

    #[test]
    fn start_pause_command_resumes_progress_and_keeps_alerts() {
        let config = Rc::new(RefCell::new(AppConfig::default()));
        let timer = Rc::new(RefCell::new(
            Timer::new(
                SystemClock::new(),
                Duration::from_secs(60),
                crate::timer::TimerMode::Countdown,
                false,
            )
            .unwrap(),
        ));
        let alerts = Rc::new(RefCell::new(AlertTracker::default()));
        let flash = Rc::new(RefCell::new(FlashController::new()));
        let command = || {
            handle_command(
                "startPause",
                &slint::Weak::default(),
                &timer,
                &config,
                &alerts,
                &flash,
                std::path::Path::new("unused.json"),
            )
        };
        command();
        assert_eq!(timer.borrow().state(), TimerState::Running);
        std::thread::sleep(Duration::from_millis(20));
        assert_eq!(
            alerts
                .borrow_mut()
                .check(&timer.borrow().snapshot(), &config.borrow())
                .len(),
            1
        );
        command();
        let paused = timer.borrow().snapshot();
        assert_eq!(paused.state, TimerState::Paused);
        assert!(paused.elapsed > Duration::ZERO);
        std::thread::sleep(Duration::from_millis(20));
        assert_eq!(timer.borrow().snapshot().elapsed, paused.elapsed);
        command();
        assert_eq!(timer.borrow().state(), TimerState::Running);
        assert!(timer.borrow().snapshot().elapsed >= paused.elapsed);
        assert!(
            alerts
                .borrow_mut()
                .check(&timer.borrow().snapshot(), &config.borrow())
                .is_empty()
        );
        std::thread::sleep(Duration::from_millis(20));
        assert!(timer.borrow().snapshot().elapsed > paused.elapsed);
        timer
            .borrow_mut()
            .set_duration(Duration::from_nanos(1))
            .unwrap();
        timer.borrow_mut().update();
        assert_eq!(timer.borrow().state(), TimerState::Finished);
        assert!(
            alerts
                .borrow_mut()
                .end(&timer.borrow().snapshot(), &config.borrow())
                .is_some()
        );
        command();
        assert_eq!(timer.borrow().state(), TimerState::Running);
        assert!(
            alerts
                .borrow_mut()
                .end(&timer.borrow().snapshot(), &config.borrow())
                .is_some()
        );
    }

    #[test]
    fn page_visibility_and_content_size_preserve_baseline() {
        assert_eq!(page_label(true, 1, 23), "1/23");
        for (enabled, current, total) in [(false, 1, 23), (true, 0, 23), (true, 1, 0)] {
            assert!(page_label(enabled, current, total).is_empty());
        }
        let config = AppConfig::default();
        let before = serde_json::to_value(&config).unwrap();
        assert_eq!(timer_content_size(&config, 85.0, 30.0), (100, 35));
        assert_eq!(timer_content_size(&config, 120.2, 54.1), (121, 55));
        assert_eq!(timer_content_size(&config, 85.0, 30.0), (100, 35));
        assert_eq!(serde_json::to_value(&config).unwrap(), before);
    }

    #[test]
    fn settings_and_remote_callbacks_preserve_edits_and_saved_data() {
        // Use Slint's existing software window to invoke production callbacks;
        // this verifies data flow, not native mouse/focus/DPI behavior.
        struct SoftwarePlatform;
        impl slint::platform::Platform for SoftwarePlatform {
            fn create_window_adapter(
                &self,
            ) -> Result<Rc<dyn slint::platform::WindowAdapter>, slint::PlatformError> {
                Ok(
                    slint::platform::software_renderer::MinimalSoftwareWindow::new(
                        slint::platform::software_renderer::RepaintBufferType::NewBuffer,
                    ),
                )
            }
        }
        slint::platform::set_platform(Box::new(SoftwarePlatform)).unwrap();
        // Exercise the real Slint measurements/root sizing, not just arithmetic.
        let overlay = AppWindow::new().unwrap();
        let baseline = AppConfig::default();
        overlay.set_timer_font_size(60.0);
        sync_timer_window_scale(&overlay, &baseline);
        assert!(overlay.get_content_width() > 100.0);
        assert!(overlay.get_content_height() > 35.0);
        overlay.set_timer_font_size(18.0);
        overlay.set_page_text("1/23".into());
        overlay.set_page_reserve(page_reserve(23).into());
        sync_timer_window_scale(&overlay, &baseline);
        assert!(overlay.get_content_height() > 35.0);
        overlay.set_page_text("".into());
        sync_timer_window_scale(&overlay, &baseline);
        assert_eq!(overlay.get_content_width(), 100.0);
        assert_eq!(overlay.get_content_height(), 35.0);
        assert_eq!(baseline.appearance.width, 100);
        assert_eq!(baseline.appearance.height, 35);

        let root = std::env::temp_dir().join(format!(
            "flyppttimer-ux13-regression-{}",
            std::process::id()
        ));
        let path = root.join("config.json");
        let config = Rc::new(RefCell::new(AppConfig {
            language: "en".into(),
            rules: vec![
                FileRule {
                    file_path: r"C:\A.pptx".into(),
                    duration: "00:08:00".into(),
                    enabled: true,
                    ..FileRule::default()
                },
                FileRule {
                    file_path: r"C:\B.pptx".into(),
                    duration: "00:05:00".into(),
                    enabled: false,
                    ..FileRule::default()
                },
            ],
            ..AppConfig::default()
        }));
        let (sender, _receiver) = std::sync::mpsc::channel();
        let remote = Rc::new(RemoteServer::new(sender));
        let service = Rc::new(PresentationService::start().unwrap());
        let control = create_presentation_window(&config, &service, &remote, &path).unwrap();
        let settings = settings::create(
            config.clone(),
            path.clone(),
            Rc::new(|_| {}),
            Rc::new(|| {}),
            false,
            remote.clone(),
        )
        .unwrap();
        settings.invoke_navigate(2);
        let width_row = settings
            .get_items()
            .iter()
            .position(|item| item.key == "appearance.width")
            .unwrap() as i32;
        settings.invoke_field_edited(width_row, "150".into(), false, 0);
        assert_eq!(settings.get_timer_preview_width(), 150);
        assert_eq!(config.borrow().appearance.width, 100);
        assert!(settings.get_dirty());

        control.invoke_presentation_selected(0, false, false);
        control.invoke_presentation_selected(1, true, false);
        control.invoke_presentation_selected(1, true, false);
        assert_eq!(presentation_selection(&control), BTreeSet::from([0]));
        assert_eq!(control.get_selected_presentation(), 0);
        assert_eq!(control.get_rule_duration(), "00:08:00");
        control.invoke_presentation_selected(1, true, false);
        control.set_rule_duration("00:09:00".into());
        control.invoke_rule_action(4, control.get_rule_duration());
        let saved = AppConfig::load(&path).unwrap();
        assert_eq!(
            saved.rules.iter().map(|r| r.enabled).collect::<Vec<_>>(),
            [true, false]
        );
        assert!(saved.rules.iter().all(|r| r.duration == "00:09:00"));
        control.invoke_rule_action(5, "".into());
        control.invoke_batch_confirm("00:10:00".into(), 1);
        assert_eq!(control.get_rule_duration(), "00:10:00");
        assert_eq!(control.get_rule_mode(), 1);
        control.invoke_rule_action(4, control.get_rule_duration());
        let saved = AppConfig::load(&path).unwrap();
        assert!(
            saved
                .rules
                .iter()
                .all(|r| r.duration == "00:10:00" && r.mode == TimerMode::CountUp)
        );
        assert_eq!(
            saved.rules.iter().map(|r| r.enabled).collect::<Vec<_>>(),
            [true, false]
        );
        control.invoke_rule_action(5, "".into());
        let before = std::fs::read(&path).unwrap();
        control.invoke_batch_confirm("invalid".into(), 0);
        assert!(control.get_batch_open());
        assert!(!control.get_batch_error().is_empty());
        assert_eq!(std::fs::read(&path).unwrap(), before);
        assert!(
            config
                .borrow()
                .rules
                .iter()
                .all(|r| r.duration == "00:10:00")
        );
        control.set_batch_open(false);
        control.invoke_presentation_selected(1, true, false);
        control.invoke_presentation_selected(0, true, false);
        assert_eq!(control.get_selected_presentation(), -1);
        control.invoke_rule_action(4, "00:01:00".into());
        control.invoke_rule_action(1, "".into());
        assert_eq!(std::fs::read(&path).unwrap(), before);

        // Neither periodic refresh nor unrelated Remote actions reset input.
        control.set_next_port("49".into());
        for _ in 0..5 {
            update_remote_connection_window(&control, &remote, &config.borrow());
        }
        assert_eq!(control.get_next_port(), "49");
        control.invoke_remote_action(2, "".into(), false);
        assert_eq!(control.get_next_port(), "49");
        let probe = std::net::TcpListener::bind("127.0.0.1:0").unwrap();
        let requested_port = probe.local_addr().unwrap().port();
        drop(probe);
        control.set_next_port(requested_port.to_string().into());
        control.invoke_remote_action(1, control.get_next_port(), false);
        assert!(remote.info().running);
        assert_eq!(
            config.borrow().remote_control.port,
            remote.info().current_port
        );
        assert_eq!(
            control.get_next_port(),
            config.borrow().remote_control.port.to_string()
        );
        assert_eq!(
            AppConfig::load(&path).unwrap().remote_control.port,
            config.borrow().remote_control.port
        );
        remote.stop();
        let token = config.borrow().remote_control.token.clone();
        config.borrow_mut().remote_control.port = 49123;
        config.borrow_mut().remote_control.window.width_dip = 810;
        config.borrow_mut().rules.push(FileRule {
            file_path: r"C:\New.pptx".into(),
            ..FileRule::default()
        });
        config.borrow().save(&path).unwrap();
        // Reverting the Settings edit clears dirty despite external changes.
        settings.invoke_field_edited(width_row, "100".into(), false, 0);
        assert!(!settings.get_dirty());
        settings.invoke_field_edited(width_row, "150".into(), false, 0);
        settings.invoke_apply();
        assert!(!settings.get_dirty());
        let saved = AppConfig::load(&path).unwrap();
        assert_eq!(saved.appearance.width, 150);
        assert_eq!(saved.rules.len(), 3);
        assert_eq!(saved.rules[0].duration, "00:10:00");
        assert_eq!(saved.remote_control.token, token);
        assert_eq!(saved.remote_control.port, 49123);
        assert_eq!(saved.remote_control.window.width_dip, 810);
        // A second Apply must use a refreshed baseline.
        config.borrow_mut().remote_control.port = 49124;
        settings.invoke_field_edited(width_row, "160".into(), false, 0);
        settings.invoke_apply();
        assert_eq!(AppConfig::load(&path).unwrap().remote_control.port, 49124);
        assert_eq!(config.borrow().appearance.width, 160);
        // Reverted edits + Cancel leave the external configuration untouched.
        settings.invoke_field_edited(width_row, "170".into(), false, 0);
        settings.invoke_field_edited(width_row, "160".into(), false, 0);
        assert!(!settings.get_dirty());
        let before = std::fs::read(&path).unwrap();
        settings.invoke_cancel();
        assert_eq!(std::fs::read(&path).unwrap(), before);
        drop(settings);
        drop(control);
        drop(service);
        std::fs::remove_dir_all(root).unwrap();
    }

    #[test]
    fn remote_timer_commands_apply_selected_rules_and_reset_alerts() {
        let root =
            std::env::temp_dir().join(format!("flyppttimer-remote-parity-{}", std::process::id()));
        std::fs::create_dir_all(&root).unwrap();
        let path = root.join("config.json");
        let initial = AppConfig {
            rules: vec![
                FileRule {
                    file_path: r"C:\Talk.pptx".into(),
                    file_name: "Talk.pptx".into(),
                    duration: "00:03:00".into(),
                    ..FileRule::default()
                },
                FileRule {
                    file_path: r"C:\Other.pptx".into(),
                    duration: "00:05:00".into(),
                    ..FileRule::default()
                },
            ],
            ..AppConfig::default()
        };
        let id = crate::remote::id_for_path(&initial.rules[0].file_path);
        let timer = Rc::new(RefCell::new(
            Timer::new(
                SystemClock::new(),
                initial.timer.duration(),
                timer_mode(initial.timer.mode),
                true,
            )
            .unwrap(),
        ));
        let config = Rc::new(RefCell::new(initial));
        let alerts = Rc::new(RefCell::new(AlertTracker::default()));
        let execute = |command| {
            execute_remote_timer_command(&command, &timer, &config, &alerts, &path).unwrap()
        };
        execute(RemoteCommand {
            command: "timer.setDuration".into(),
            duration: Some("00:01:00".into()),
            duration_ms: Some(120000),
            presentation_id: Some(id.clone()),
            ..RemoteCommand::default()
        });
        let saved = AppConfig::load(&path).unwrap();
        assert_eq!(saved.timer.default_duration, "00:02:00");
        assert_eq!(
            saved
                .rules
                .iter()
                .map(|rule| rule.duration.as_str())
                .collect::<Vec<_>>(),
            ["00:02:00", "00:05:00"]
        );
        assert_eq!(timer.borrow().duration(), Duration::from_secs(120));
        execute(RemoteCommand {
            command: "timer.setMode".into(),
            mode: Some("countup".into()),
            presentation_id: Some(id.clone()),
            ..RemoteCommand::default()
        });
        let saved = AppConfig::load(&path).unwrap();
        assert_eq!(saved.timer.mode, TimerMode::CountUp);
        assert_eq!(
            saved.rules.iter().map(|rule| rule.mode).collect::<Vec<_>>(),
            [TimerMode::CountUp, TimerMode::Countdown]
        );
        assert_eq!(
            timer.borrow().snapshot().mode,
            crate::timer::TimerMode::CountUp
        );
        execute(RemoteCommand {
            command: "timer.setDuration".into(),
            duration_ms: Some(240000),
            sync_all_rules: Some(true),
            ..RemoteCommand::default()
        });
        assert!(
            AppConfig::load(&path)
                .unwrap()
                .rules
                .iter()
                .all(|rule| rule.duration == "00:04:00")
        );
        // Restart chooses the requested rule even when disabled, as the legacy command does.
        config.borrow_mut().rules[0].duration = "00:01:00".into();
        config.borrow_mut().rules[0].enabled = false;
        alerts
            .borrow_mut()
            .end(&timer.borrow().snapshot(), &config.borrow())
            .unwrap();
        assert_eq!(
            execute(RemoteCommand {
                command: "timer.restart".into(),
                presentation_id: Some(id),
                ..RemoteCommand::default()
            }),
            "已按 Talk.pptx 的规则时长重新计时"
        );
        assert_eq!(timer.borrow().duration(), Duration::from_secs(60));
        assert_eq!(timer.borrow().state(), TimerState::Running);
        assert!(
            alerts
                .borrow_mut()
                .end(&timer.borrow().snapshot(), &config.borrow())
                .is_some()
        );
        execute(RemoteCommand {
            command: "timer.stop".into(),
            ..RemoteCommand::default()
        });
        let snapshot = timer.borrow().snapshot();
        assert_eq!(snapshot.state, TimerState::Stopped);
        assert_eq!(snapshot.elapsed, Duration::ZERO);
        assert!(
            alerts
                .borrow_mut()
                .end(&snapshot, &config.borrow())
                .is_some()
        );
        execute(RemoteCommand {
            command: "timer.setMode".into(),
            mode: Some("unknown".into()),
            ..RemoteCommand::default()
        });
        assert_eq!(config.borrow().timer.mode, TimerMode::Countdown);
        assert_eq!(
            execute(RemoteCommand {
                command: "timer.restart".into(),
                presentation_id: Some("unknown".into()),
                ..RemoteCommand::default()
            }),
            "已按全局时长重新计时"
        );
        assert_eq!(timer.borrow().duration(), Duration::from_secs(240));
        assert_eq!(
            timer.borrow().snapshot().mode,
            crate::timer::TimerMode::Countdown
        );
        execute(RemoteCommand {
            command: "state.get".into(),
            ..RemoteCommand::default()
        });
        execute(RemoteCommand {
            command: "timer.setDuration".into(),
            duration_ms: Some(-1),
            duration: Some("0:1:2".into()),
            ..RemoteCommand::default()
        });
        assert_eq!(config.borrow().timer.default_duration, "00:01:02");
        execute(RemoteCommand {
            command: "timer.setDuration".into(),
            duration_ms: Some(90000000),
            ..RemoteCommand::default()
        });
        assert_eq!(config.borrow().timer.default_duration, "23:59:59");
        execute(RemoteCommand {
            command: "timer.setDuration".into(),
            duration_ms: Some(2500),
            ..RemoteCommand::default()
        });
        assert_eq!(config.borrow().timer.default_duration, "00:00:02");
        std::fs::remove_dir_all(root).unwrap();
    }
}
