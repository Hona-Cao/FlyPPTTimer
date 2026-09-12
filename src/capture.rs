use std::{cell::RefCell, fs::File, path::PathBuf, rc::Rc};

use slint::{
    ComponentHandle, Model, PhysicalSize,
    platform::software_renderer::{MinimalSoftwareWindow, RepaintBufferType},
    platform::{Platform, PlatformError, WindowAdapter},
};

use crate::{config::AppConfig, remote::RemoteServer, settings};

thread_local! {
    static HEADLESS_WINDOW: Rc<MinimalSoftwareWindow> =
        MinimalSoftwareWindow::new(RepaintBufferType::NewBuffer);
}

struct HeadlessPlatform;

impl Platform for HeadlessPlatform {
    fn create_window_adapter(&self) -> Result<Rc<dyn WindowAdapter>, PlatformError> {
        Ok(HEADLESS_WINDOW.with(Clone::clone))
    }
}

pub fn capture_all(output: PathBuf) -> Result<(), Box<dyn std::error::Error>> {
    std::fs::create_dir_all(&output)?;
    slint::platform::set_platform(Box::new(HeadlessPlatform))?;
    HEADLESS_WINDOW.with(|window| window.set_size(PhysicalSize::new(900, 650)));

    for (language, language_name) in [("zh-CN", "zh-CN"), ("en", "en")] {
        let config = Rc::new(RefCell::new(AppConfig {
            language: language.to_owned(),
            ui_theme: std::env::var("FLYPPT_CAPTURE_THEME").unwrap_or_else(|_| "light".into()),
            ..AppConfig::default()
        }));
        let (remote_sender, _remote_receiver) = std::sync::mpsc::channel();
        let remote = Rc::new(RemoteServer::new(remote_sender));
        let preview = settings::create(
            Rc::clone(&config),
            output.join("preview.config.json"),
            Rc::new(|_| {}),
            Rc::new(|| {}),
            false,
            Rc::clone(&remote),
        )?;
        preview.show()?;

        for page in 0usize..6 {
            preview.invoke_navigate(page as i32);
            for (part, offset) in page_offsets(page).iter().copied().enumerate() {
                preview.set_preview_scroll_y(offset);
                slint::platform::update_timers_and_animations();
                HEADLESS_WINDOW.with(|window| window.request_redraw());
                let pixels = preview.window().take_snapshot()?;
                let file_name = format!(
                    "settings-{language_name}-{:02}-{}-part-{}.png",
                    page + 1,
                    page_name(page),
                    part + 1,
                );
                write_png(output.join(file_name), &pixels)?;
            }
        }
        preview.invoke_navigate(2);
        let rows = preview.get_items();
        for index in 0..rows.row_count() {
            if rows
                .row_data(index)
                .is_some_and(|row| row.key.as_str() == "appearance.opacity")
            {
                preview.invoke_field_edited(index as i32, "63".into(), false, 0);
                assert!(preview.get_dirty());
                assert_eq!(preview.get_timer_preview_opacity(), 63);
                preview.set_preview_scroll_y(-1100.0);
                slint::platform::update_timers_and_animations();
                HEADLESS_WINDOW.with(|window| window.request_redraw());
                write_png(
                    output.join(format!("rc34-unsaved-slider-{language_name}.png")),
                    &preview.window().take_snapshot()?,
                )?;
                preview.invoke_apply();
                assert!(!preview.get_dirty());
                assert_eq!(config.borrow().appearance.background_opacity, 63);
                break;
            }
        }
        // Reuse the production info callback, rather than a hand-written mock.
        preview.invoke_navigate(5);
        let rows = preview.get_items();
        for index in 0..rows.row_count() {
            if let Some(row) = rows.row_data(index)
                && (row.label.as_str() == "作者的话" || row.label.as_str() == "From the author")
            {
                preview.invoke_field_action(index as i32);
                slint::platform::update_timers_and_animations();
                HEADLESS_WINDOW.with(|window| window.request_redraw());
                let pixels = preview.window().take_snapshot()?;
                write_png(
                    output.join(format!("author-dialog-{language_name}.png")),
                    &pixels,
                )?;
                preview.set_dialog_open(false);
                break;
            }
        }
        preview.hide()?;
        drop(preview);
    }
    Ok(())
}

pub fn capture_windows(output: PathBuf) -> Result<(), Box<dyn std::error::Error>> {
    std::fs::create_dir_all(&output)?;
    slint::platform::set_platform(Box::new(HeadlessPlatform))?;

    // 计时窗口 (AppWindow)
    HEADLESS_WINDOW.with(|window| window.set_size(PhysicalSize::new(100, 35)));
    let timer_window = crate::app::AppWindow::new()?;
    timer_window.show()?;
    slint::platform::update_timers_and_animations();
    HEADLESS_WINDOW.with(|window| window.request_redraw());
    let pixels = timer_window.window().take_snapshot()?;
    write_png(output.join("timer-window.png"), &pixels)?;
    // Production Slint readout evidence, not a mock or a physical-display test.
    timer_window.set_page_text("1 / 23".into());
    timer_window.set_page_reserve("88 / 88".into());
    for (name, page_font, above, alignment, italic) in [
        ("timer-rc34-default", 12.0, false, 2, false),
        ("timer-rc32-page-above", 12.0, true, 0, true),
        ("timer-rc32-page-large", 36.0, false, 2, true),
    ] {
        timer_window.set_page_font_size(page_font);
        timer_window.set_page_above(above);
        timer_window.set_page_alignment(alignment);
        timer_window.set_page_italic(italic);
        timer_window.set_page_color(slint::Color::from_rgb_u8(164, 60, 30));
        let width = timer_window.get_required_text_width().ceil();
        let height = timer_window.get_required_text_height().ceil();
        timer_window.set_content_width(width);
        timer_window.set_content_height(height);
        HEADLESS_WINDOW
            .with(|window| window.set_size(PhysicalSize::new(width as u32, height as u32)));
        slint::platform::update_timers_and_animations();
        HEADLESS_WINDOW.with(|window| window.request_redraw());
        write_png(
            output.join(format!("{name}.png")),
            &timer_window.window().take_snapshot()?,
        )?;
    }
    timer_window.hide()?;
    drop(timer_window);

    // Remote PC 窗口两页 (700x620)
    HEADLESS_WINDOW.with(|window| window.set_size(PhysicalSize::new(700, 620)));
    let control = crate::app::PresentationWindow::new()?;
    control.set_next_port("4080".into());
    control.set_saved_port("4080".into());
    control.set_saved_text("已保存".into());
    control.set_unsaved_text("有未保存的修改".into());
    control.set_dark_theme(std::env::var("FLYPPT_CAPTURE_THEME").as_deref() == Ok("dark"));
    control.set_window_title("远程控制".into());
    control.set_connection_page_text("远程连接".into());
    control.set_presentation_page_text("演示文稿".into());
    control.set_connection_subtitle_text("通过手机或浏览器控制演示".into());
    control.set_presentation_subtitle_text("规则与放映".into());
    control.set_service_status_text("当前服务状态：已启动".into());
    control.set_current_port_text("本次启动端口：4080".into());
    control.set_client_count_text("连接设备数量：0".into());
    control.set_recommended_url("http://192.168.1.100:4080/?token=••••••".into());
    control.set_address_list_text("192.168.1.100\n192.168.1.101".into());
    control.set_firewall_text("如果手机无法连接，请在 Windows 防火墙中允许 TCP 端口 4080。FlyPPTTimer 只提供修复命令，不会主动提权修改防火墙。\nnetsh advfirewall firewall add rule name=\"FlyPPTTimer Remote\" dir=in action=allow protocol=TCP localport=4080".into());
    let items = slint::VecModel::<crate::app::PresentationItem>::default();
    items.push(crate::app::PresentationItem {
        name: "演示文稿.pptx".into(),
        path: "C:\\Decks\\演示文稿.pptx".into(),
        duration: "00:08:00".into(),
        mode: 0,
        enabled: true,
        is_rule: true,
        selected: false,
    });
    control.set_presentations(slint::ModelRc::new(items));
    control.show()?;
    control.set_current_page(0);
    slint::platform::update_timers_and_animations();
    HEADLESS_WINDOW.with(|window| window.request_redraw());
    let pixels = control.window().take_snapshot()?;
    write_png(output.join("remote-connection.png"), &pixels)?;
    control.set_current_page(1);
    slint::platform::update_timers_and_animations();
    HEADLESS_WINDOW.with(|window| window.request_redraw());
    let pixels = control.window().take_snapshot()?;
    write_png(output.join("remote-presentation.png"), &pixels)?;
    control.hide()?;
    drop(control);
    Ok(())
}

fn page_offsets(page: usize) -> &'static [f32] {
    match page {
        0 | 4 => &[0.0],
        1 | 2 => &[0.0, -500.0, -1_000.0, -1_500.0, -2_000.0],
        3 | 5 => &[0.0, -500.0, -1_000.0],
        _ => &[0.0],
    }
}

fn page_name(page: usize) -> &'static str {
    [
        "timer",
        "behavior",
        "appearance",
        "remote",
        "controls",
        "other",
    ]
    .get(page)
    .copied()
    .unwrap_or("unknown")
}

fn write_png(
    path: PathBuf,
    pixels: &slint::SharedPixelBuffer<slint::Rgba8Pixel>,
) -> Result<(), Box<dyn std::error::Error>> {
    let file = File::create(path)?;
    let mut encoder = png::Encoder::new(file, pixels.width(), pixels.height());
    encoder.set_color(png::ColorType::Rgba);
    encoder.set_depth(png::BitDepth::Eight);
    let mut writer = encoder.write_header()?;
    writer.write_image_data(pixels.as_bytes())?;
    writer.finish()?;
    Ok(())
}
