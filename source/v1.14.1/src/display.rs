use slint::{PhysicalPosition, PhysicalSize};
use windows_sys::Win32::{
    Foundation::{LPARAM, RECT},
    Graphics::Gdi::{
        EnumDisplayMonitors, GetMonitorInfoW, HDC, HMONITOR, MONITORINFO, MONITORINFOEXW,
    },
};

use crate::config::{OverlayAnchor, WindowPlacement};

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct DisplayRect {
    pub x: i32,
    pub y: i32,
    pub width: i32,
    pub height: i32,
}

impl From<RECT> for DisplayRect {
    fn from(value: RECT) -> Self {
        Self {
            x: value.left,
            y: value.top,
            width: (value.right - value.left).max(1),
            height: (value.bottom - value.top).max(1),
        }
    }
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub struct DisplayMonitor {
    pub device_name: String,
    pub bounds: DisplayRect,
    pub work_area: DisplayRect,
    pub primary: bool,
    pub dpi: u32,
}

#[link(name = "shcore")]
unsafe extern "system" {
    fn GetDpiForMonitor(monitor: HMONITOR, dpi_type: i32, dpi_x: *mut u32, dpi_y: *mut u32) -> i32;
}

pub fn monitors() -> Vec<DisplayMonitor> {
    unsafe extern "system" fn collect(
        monitor: HMONITOR,
        _: HDC,
        _: *mut RECT,
        data: LPARAM,
    ) -> windows_sys::core::BOOL {
        let result = unsafe { &mut *(data as *mut Vec<DisplayMonitor>) };
        let mut info = MONITORINFOEXW::default();
        info.monitorInfo.cbSize = std::mem::size_of::<MONITORINFOEXW>() as u32;
        if unsafe { GetMonitorInfoW(monitor, &mut info.monitorInfo as *mut MONITORINFO) } == 0 {
            return 1;
        }
        let end = info
            .szDevice
            .iter()
            .position(|value| *value == 0)
            .unwrap_or(info.szDevice.len());
        let mut dpi_x = 96;
        let mut dpi_y = 96;
        if unsafe { GetDpiForMonitor(monitor, 0, &mut dpi_x, &mut dpi_y) } != 0 {
            dpi_x = 96;
        }
        result.push(DisplayMonitor {
            device_name: String::from_utf16_lossy(&info.szDevice[..end]),
            bounds: info.monitorInfo.rcMonitor.into(),
            work_area: info.monitorInfo.rcWork.into(),
            primary: info.monitorInfo.dwFlags & 1 != 0,
            dpi: dpi_x.max(96),
        });
        1
    }

    let mut result: Vec<DisplayMonitor> = Vec::new();
    unsafe {
        EnumDisplayMonitors(
            std::ptr::null_mut(),
            std::ptr::null(),
            Some(collect),
            &mut result as *mut _ as LPARAM,
        );
    }
    result.sort_by_key(|monitor| !monitor.primary);
    result
}

pub fn signature(monitors: &[DisplayMonitor]) -> String {
    monitors
        .iter()
        .map(|monitor| {
            format!(
                "{}:{}:{}:{}:{}:{}:{}:{}:{}:{}:{}",
                monitor.device_name,
                monitor.bounds.x,
                monitor.bounds.y,
                monitor.bounds.width,
                monitor.bounds.height,
                monitor.work_area.x,
                monitor.work_area.y,
                monitor.work_area.width,
                monitor.work_area.height,
                monitor.primary,
                monitor.dpi
            )
        })
        .collect::<Vec<_>>()
        .join("|")
}

pub fn big_screen_target<'a>(
    monitors: &'a [DisplayMonitor],
    placement: &WindowPlacement,
) -> Option<&'a DisplayMonitor> {
    if !placement.big_screen_enabled {
        return None;
    }
    let extended = extended_monitors(monitors);
    extended
        .iter()
        .copied()
        .find(|m| {
            m.device_name
                .eq_ignore_ascii_case(&placement.big_screen_device_name)
        })
        .or_else(|| extended.first().copied())
}

pub fn overlay_allowed(visible: bool, monitor: &str, excluded: &str) -> bool {
    visible && (excluded.is_empty() || !monitor.eq_ignore_ascii_case(excluded))
}

pub fn timer_targets<'a>(
    monitors: &'a [DisplayMonitor],
    placement: &WindowPlacement,
) -> Vec<&'a DisplayMonitor> {
    if placement.show_on_all_screens {
        return monitors.iter().collect();
    }
    vec![selected_monitor(
        monitors,
        &placement.target_screen_device_name,
    )]
}

pub fn selected_monitor<'a>(
    monitors: &'a [DisplayMonitor],
    device_name: &str,
) -> &'a DisplayMonitor {
    monitors
        .iter()
        .find(|monitor| {
            !device_name.is_empty() && monitor.device_name.eq_ignore_ascii_case(device_name)
        })
        .or_else(|| monitors.iter().find(|monitor| monitor.primary))
        .unwrap_or(&monitors[0])
}

pub fn extended_monitors(monitors: &[DisplayMonitor]) -> Vec<&DisplayMonitor> {
    monitors.iter().filter(|monitor| !monitor.primary).collect()
}

pub fn logical_size_physical(width: i32, height: i32, dpi: u32) -> PhysicalSize {
    let scale = dpi.max(96) as f64 / 96.0;
    PhysicalSize::new(
        (width.max(1) as f64 * scale).round().max(1.0) as u32,
        (height.max(1) as f64 * scale).round().max(1.0) as u32,
    )
}

pub fn timer_position(
    monitor: &DisplayMonitor,
    placement: &WindowPlacement,
    window_size: PhysicalSize,
) -> PhysicalPosition {
    let (origin_x, origin_y) = anchor_origin(monitor, placement.anchor, window_size);
    let center_x = origin_x
        + monitor.work_area.width as f64 * placement.offset_x_percent.clamp(-50.0, 50.0) / 100.0;
    let center_y = origin_y
        + monitor.work_area.height as f64 * placement.offset_y_percent.clamp(-50.0, 50.0) / 100.0;
    PhysicalPosition::new(
        (center_x - window_size.width as f64 / 2.0).round() as i32,
        (center_y - window_size.height as f64 / 2.0).round() as i32,
    )
}

pub fn capture_timer_position(
    placement: &mut WindowPlacement,
    position: PhysicalPosition,
    window_size: PhysicalSize,
    monitors: &[DisplayMonitor],
) {
    let center_x = position.x as f64 + window_size.width as f64 / 2.0;
    let center_y = position.y as f64 + window_size.height as f64 / 2.0;
    let monitor = monitors
        .iter()
        .find(|monitor| contains(monitor.bounds, center_x, center_y))
        .or_else(|| monitors.iter().find(|monitor| monitor.primary))
        .unwrap_or(&monitors[0]);
    let (origin_x, origin_y) = anchor_origin(monitor, placement.anchor, window_size);
    placement.offset_x_percent =
        ((center_x - origin_x) * 100.0 / monitor.work_area.width as f64).clamp(-50.0, 50.0);
    placement.offset_y_percent =
        ((center_y - origin_y) * 100.0 / monitor.work_area.height as f64).clamp(-50.0, 50.0);
    placement.x = position.x;
    placement.y = position.y;
    placement.screen_device_name = monitor.device_name.clone();
    placement.target_screen_device_name = monitor.device_name.clone();
    placement.has_custom_placement = true;
}

fn contains(rect: DisplayRect, x: f64, y: f64) -> bool {
    x >= rect.x as f64
        && x < (rect.x + rect.width) as f64
        && y >= rect.y as f64
        && y < (rect.y + rect.height) as f64
}

fn anchor_origin(
    monitor: &DisplayMonitor,
    anchor: OverlayAnchor,
    size: PhysicalSize,
) -> (f64, f64) {
    let area = monitor.bounds;
    let half_width = size.width as f64 / 2.0;
    let half_height = size.height as f64 / 2.0;
    let x = match anchor {
        OverlayAnchor::TopCenter | OverlayAnchor::Center | OverlayAnchor::BottomCenter => {
            area.x as f64 + area.width as f64 / 2.0
        }
        OverlayAnchor::TopRight | OverlayAnchor::MiddleRight | OverlayAnchor::BottomRight => {
            (area.x + area.width) as f64 - half_width
        }
        _ => area.x as f64 + half_width,
    };
    let y = match anchor {
        OverlayAnchor::MiddleLeft | OverlayAnchor::Center | OverlayAnchor::MiddleRight => {
            area.y as f64 + area.height as f64 / 2.0
        }
        OverlayAnchor::BottomLeft | OverlayAnchor::BottomCenter | OverlayAnchor::BottomRight => {
            (area.y + area.height) as f64 - half_height
        }
        _ => area.y as f64 + half_height,
    };
    (x, y)
}

#[cfg(test)]
mod tests {
    use super::*;

    fn monitor(dpi: u32) -> DisplayMonitor {
        DisplayMonitor {
            device_name: r"\\.\DISPLAY1".into(),
            bounds: DisplayRect {
                x: 0,
                y: 0,
                width: 1920,
                height: 1080,
            },
            work_area: DisplayRect {
                x: 0,
                y: 0,
                width: 1920,
                height: 1040,
            },
            primary: true,
            dpi,
        }
    }

    #[test]
    fn top_center_zero_offset_is_flush_with_the_screen() {
        let placement = WindowPlacement::default();
        let position = timer_position(&monitor(96), &placement, PhysicalSize::new(100, 35));
        assert_eq!(position, PhysicalPosition::new(910, 0));
    }

    #[test]
    fn right_bottom_uses_actual_size_and_saved_offsets() {
        let placement = WindowPlacement {
            anchor: OverlayAnchor::BottomRight,
            offset_x_percent: -10.0,
            offset_y_percent: -10.0,
            ..WindowPlacement::default()
        };
        let position = timer_position(&monitor(144), &placement, PhysicalSize::new(150, 53));
        assert_eq!(position, PhysicalPosition::new(1578, 923));
    }

    #[test]
    fn centers_each_display_with_negative_origin_dpi_and_a_side_taskbar() {
        for dpi in [96, 144, 192] {
            let mut screen = monitor(dpi);
            screen.bounds.x = -2560;
            screen.bounds.width = 2560;
            screen.work_area.x = -2480;
            screen.work_area.width = 2480;
            for width in [80, 137, 320] {
                let size = logical_size_physical(width, 50, dpi);
                let p = timer_position(&screen, &WindowPlacement::default(), size);
                let center = p.x as f64 + size.width as f64 / 2.0;
                assert!((center - (-1280.0)).abs() <= 0.5);
            }
        }
    }

    #[test]
    fn zero_offset_anchors_use_real_edges_for_all_sizes_and_monitor_origins() {
        for dpi in [96, 144, 192] {
            let mut m = monitor(dpi);
            m.bounds.x = -2560;
            m.bounds.y = -1440;
            m.bounds.width = 2560;
            m.bounds.height = 1440;
            m.work_area = DisplayRect {
                x: -2480,
                y: -1410,
                width: 2480,
                height: 1360,
            };
            for (w, h) in [(80, 28), (100, 59), (219, 113)] {
                let size = logical_size_physical(w, h, dpi);
                for anchor in [
                    OverlayAnchor::TopCenter,
                    OverlayAnchor::BottomCenter,
                    OverlayAnchor::TopLeft,
                    OverlayAnchor::BottomRight,
                ] {
                    let mut placement = WindowPlacement {
                        anchor,
                        ..Default::default()
                    };
                    let pos = timer_position(&m, &placement, size);
                    if matches!(anchor, OverlayAnchor::TopCenter | OverlayAnchor::TopLeft) {
                        assert_eq!(pos.y, m.bounds.y);
                    } else {
                        assert_eq!(pos.y + size.height as i32, m.bounds.y + m.bounds.height);
                    }
                    if anchor == OverlayAnchor::TopLeft {
                        assert_eq!(pos.x, m.bounds.x);
                    }
                    if anchor == OverlayAnchor::BottomRight {
                        assert_eq!(pos.x + size.width as i32, m.bounds.x + m.bounds.width);
                    }
                    capture_timer_position(&mut placement, pos, size, std::slice::from_ref(&m));
                    assert_eq!(timer_position(&m, &placement, size), pos);
                    assert!(placement.offset_y_percent.abs() < 0.00001);
                }
            }
        }
    }

    #[test]
    fn drag_round_trip_preserves_anchor_relative_position() {
        let screens = vec![monitor(96)];
        let mut placement = WindowPlacement::default();
        let position = PhysicalPosition::new(1100, 200);
        capture_timer_position(
            &mut placement,
            position,
            PhysicalSize::new(100, 35),
            &screens,
        );
        assert_eq!(
            timer_position(&screens[0], &placement, PhysicalSize::new(100, 35)),
            position
        );
        assert!(placement.has_custom_placement);
    }
}

#[cfg(test)]
mod rc32_tests {
    use super::*;
    #[test]
    fn fullscreen_display_exclusion_never_overrides_user_visibility() {
        assert!(!overlay_allowed(true, "DISPLAY2", "display2"));
        assert!(overlay_allowed(true, "DISPLAY1", "DISPLAY2"));
        assert!(overlay_allowed(true, "DISPLAY2", ""));
        assert!(!overlay_allowed(false, "DISPLAY1", "DISPLAY2"));
    }
}
