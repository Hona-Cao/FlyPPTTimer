use raw_window_handle::{HasWindowHandle, RawWindowHandle};
use slint::{PhysicalPosition, PhysicalSize};
use windows_sys::Win32::{
    Foundation::{HWND, POINT, RECT},
    Graphics::Gdi::{CreateRoundRectRgn, SetWindowRgn},
    UI::HiDpi::GetDpiForWindow,
    UI::WindowsAndMessaging::{
        GWL_EXSTYLE, GWL_STYLE, GetClientRect, GetCursorPos, GetSystemMetrics, GetWindowLongPtrW,
        GetWindowRect, HWND_NOTOPMOST, HWND_TOPMOST, IsIconic, IsWindowVisible, LWA_ALPHA,
        SM_CXVIRTUALSCREEN, SM_CYVIRTUALSCREEN, SM_XVIRTUALSCREEN, SM_YVIRTUALSCREEN,
        SPI_GETWORKAREA, SW_HIDE, SW_SHOWNOACTIVATE, SWP_FRAMECHANGED, SWP_NOACTIVATE, SWP_NOMOVE,
        SWP_NOSIZE, SWP_NOZORDER, SetLayeredWindowAttributes, SetWindowLongPtrW, SetWindowPos,
        ShowWindow, SystemParametersInfoW, WS_CAPTION, WS_EX_APPWINDOW, WS_EX_LAYERED,
        WS_EX_NOACTIVATE, WS_EX_TOOLWINDOW, WS_EX_TRANSPARENT, WS_MAXIMIZEBOX, WS_MINIMIZEBOX,
        WS_POPUP, WS_SYSMENU, WS_THICKFRAME,
    },
};

use crate::config::RemoteWindowPlacement;

pub fn handle_timer_frame_paint(window: &slint::Window) {
    if let Some(hwnd) = hwnd(window) {
        unsafe {
            windows_sys::Win32::UI::Shell::SetWindowSubclass(hwnd, Some(timer_frame_proc), 1, 0)
        };
    }
}

unsafe extern "system" fn timer_frame_proc(
    hwnd: HWND,
    message: u32,
    wparam: usize,
    lparam: isize,
    id: usize,
    _: usize,
) -> isize {
    use windows_sys::Win32::UI::{
        Shell::{DefSubclassProc, RemoveWindowSubclass},
        WindowsAndMessaging::*,
    };
    // Prevent Windows' legacy UAH caption painting from covering the frameless timer.
    const WM_NCUAHDRAWCAPTION: u32 = 0x00ae;
    if message == WM_NCUAHDRAWCAPTION {
        return 0;
    }
    if message == WM_NCACTIVATE {
        return unsafe { DefSubclassProc(hwnd, message, wparam, -1) };
    }
    if message == WM_NCDESTROY {
        unsafe { RemoveWindowSubclass(hwnd, Some(timer_frame_proc), id) };
    }
    unsafe { DefSubclassProc(hwnd, message, wparam, lparam) }
}

pub fn apply_native_window(
    window: &slint::Window,
    click_through: bool,
    always_on_top: bool,
    opacity_percent: i32,
    shape: &str,
) {
    let Some(hwnd) = hwnd(window) else {
        return;
    };

    unsafe {
        let current_style = GetWindowLongPtrW(hwnd, GWL_STYLE) as u32;
        let window_style = (current_style
            & !(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX))
            | WS_POPUP;
        let mut position_flags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE;
        if window_style != current_style {
            SetWindowLongPtrW(hwnd, GWL_STYLE, window_style as isize);
            position_flags |= SWP_FRAMECHANGED;
        }

        let current_exstyle = GetWindowLongPtrW(hwnd, GWL_EXSTYLE) as u32;
        let mut style = current_exstyle;
        style |= WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        style &= !WS_EX_APPWINDOW;
        if click_through {
            style |= WS_EX_TRANSPARENT;
        } else {
            style &= !WS_EX_TRANSPARENT;
        }
        if style != current_exstyle {
            SetWindowLongPtrW(hwnd, GWL_EXSTYLE, style as isize);
            position_flags |= SWP_FRAMECHANGED;
        }

        let alpha = ((opacity_percent.clamp(10, 100) * 255) / 100) as u8;
        SetLayeredWindowAttributes(hwnd, 0, alpha, LWA_ALPHA);

        SetWindowPos(
            hwnd,
            if always_on_top {
                HWND_TOPMOST
            } else {
                HWND_NOTOPMOST
            },
            0,
            0,
            0,
            0,
            position_flags,
        );

        apply_shape(hwnd, shape);
    }
}

pub fn refresh_shape(window: &slint::Window, shape: &str) {
    let Some(hwnd) = hwnd(window) else {
        return;
    };
    unsafe { apply_shape(hwnd, shape) };
}

pub fn resize_physical(window: &slint::Window, size: PhysicalSize) {
    let Some(hwnd) = hwnd(window) else {
        return;
    };
    unsafe {
        let mut rect = RECT::default();
        if GetWindowRect(hwnd, &mut rect) == 0 {
            return;
        }
        if rect.right - rect.left == size.width as i32
            && rect.bottom - rect.top == size.height as i32
        {
            return;
        }
        SetWindowPos(
            hwnd,
            std::ptr::null_mut(),
            0,
            0,
            size.width.max(1) as i32,
            size.height.max(1) as i32,
            SWP_NOMOVE | SWP_NOACTIVATE | SWP_NOZORDER,
        );
    }
}

pub fn resize_for_dpi(window: &slint::Window, width_dip: i32, height_dip: i32) {
    let Some(hwnd) = hwnd(window) else {
        return;
    };
    let dpi = unsafe { GetDpiForWindow(hwnd) }.max(96);
    let scale = dpi as f64 / 96.0;
    resize_physical(
        window,
        PhysicalSize::new(
            (width_dip.max(1) as f64 * scale).round() as u32,
            (height_dip.max(1) as f64 * scale).round() as u32,
        ),
    );
}

pub fn cursor_position() -> Option<PhysicalPosition> {
    let mut point = POINT::default();
    if unsafe { GetCursorPos(&mut point) } == 0 {
        return None;
    }
    Some(PhysicalPosition::new(point.x, point.y))
}

pub fn set_visible(window: &slint::Window, visible: bool) {
    if let Some(hwnd) = hwnd(window) {
        unsafe {
            ShowWindow(hwnd, if visible { SW_SHOWNOACTIVATE } else { SW_HIDE });
        }
    }
}

pub fn is_minimized(window: &slint::Window) -> bool {
    hwnd(window).is_some_and(|hwnd| unsafe { IsIconic(hwnd) != 0 })
}

pub fn show_time_up_window(window: &slint::Window) {
    let Some(hwnd) = hwnd(window) else {
        return;
    };
    unsafe {
        let mut style = GetWindowLongPtrW(hwnd, GWL_EXSTYLE) as u32;
        style |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        style &= !WS_EX_APPWINDOW;
        SetWindowLongPtrW(hwnd, GWL_EXSTYLE, style as isize);
        let x = GetSystemMetrics(SM_XVIRTUALSCREEN);
        let y = GetSystemMetrics(SM_YVIRTUALSCREEN);
        let width = GetSystemMetrics(SM_CXVIRTUALSCREEN).max(1);
        let height = GetSystemMetrics(SM_CYVIRTUALSCREEN).max(1);
        SetWindowPos(
            hwnd,
            HWND_TOPMOST,
            x,
            y,
            width,
            height,
            SWP_NOACTIVATE | SWP_FRAMECHANGED,
        );
        ShowWindow(hwnd, SW_SHOWNOACTIVATE);
    }
}

pub fn is_visible(window: &slint::Window) -> bool {
    hwnd(window).is_some_and(|hwnd| unsafe { IsWindowVisible(hwnd) != 0 })
}

pub fn restore_remote_window(window: &slint::Window, placement: &RemoteWindowPlacement) {
    let width = placement.width_dip.max(700) as f32;
    // The presentation page includes the rule editor and its full control
    // group. Keep legacy 510-DIP placements usable without clipping it.
    let height = placement.height_dip.max(620) as f32;
    window.set_size(slint::LogicalSize::new(width, height));
    let size = window.size();
    let work = monitor_work_area(&placement.screen_device_name).unwrap_or_else(primary_work_area);
    let available_x = (work.right - work.left - size.width as i32).max(0);
    let available_y = (work.bottom - work.top - size.height as i32).max(0);
    let (left_ratio, top_ratio) = if placement.has_value {
        (
            placement.left_ratio.clamp(0.0, 1.0),
            placement.top_ratio.clamp(0.0, 1.0),
        )
    } else {
        (0.5, 0.5)
    };
    window.set_position(PhysicalPosition::new(
        work.left + (available_x as f64 * left_ratio).round() as i32,
        work.top + (available_y as f64 * top_ratio).round() as i32,
    ));
    window.set_maximized(placement.maximized);
}

pub fn capture_remote_window(window: &slint::Window, placement: &mut RemoteWindowPlacement) {
    let maximized = window.is_maximized();
    let (position, size) = if maximized {
        hwnd(window)
            .and_then(normal_window_bounds)
            .unwrap_or_else(|| (window.position(), window.size()))
    } else {
        (window.position(), window.size())
    };
    let monitor =
        monitor_for_window(window).unwrap_or_else(|| (String::new(), primary_work_area()));
    let work = monitor.1;
    let available_x = (work.right - work.left - size.width as i32).max(0);
    let available_y = (work.bottom - work.top - size.height as i32).max(0);
    placement.has_value = true;
    placement.screen_device_name = monitor.0;
    placement.left_ratio = if available_x == 0 {
        0.0
    } else {
        ((position.x - work.left) as f64 / available_x as f64).clamp(0.0, 1.0)
    };
    placement.top_ratio = if available_y == 0 {
        0.0
    } else {
        ((position.y - work.top) as f64 / available_y as f64).clamp(0.0, 1.0)
    };
    let scale = window.scale_factor().max(0.1);
    placement.width_dip = ((size.width as f32 / scale).round() as i32).max(700);
    placement.height_dip = ((size.height as f32 / scale).round() as i32).max(510);
    placement.maximized = maximized;
}

fn monitor_work_area(device_name: &str) -> Option<RECT> {
    if device_name.is_empty() {
        return None;
    }
    monitor_entries()
        .into_iter()
        .find(|(name, _)| name.eq_ignore_ascii_case(device_name))
        .map(|(_, work)| work)
}

fn monitor_for_window(window: &slint::Window) -> Option<(String, RECT)> {
    use windows_sys::Win32::Graphics::Gdi::{
        GetMonitorInfoW, MONITOR_DEFAULTTONEAREST, MONITORINFO, MONITORINFOEXW, MonitorFromWindow,
    };
    let monitor = unsafe { MonitorFromWindow(hwnd(window)?, MONITOR_DEFAULTTONEAREST) };
    if monitor.is_null() {
        return None;
    }
    let mut info = MONITORINFOEXW::default();
    info.monitorInfo.cbSize = std::mem::size_of::<MONITORINFOEXW>() as u32;
    if unsafe { GetMonitorInfoW(monitor, &mut info.monitorInfo as *mut MONITORINFO) } == 0 {
        return None;
    }
    let end = info
        .szDevice
        .iter()
        .position(|value| *value == 0)
        .unwrap_or(info.szDevice.len());
    Some((
        String::from_utf16_lossy(&info.szDevice[..end]),
        info.monitorInfo.rcWork,
    ))
}

fn monitor_entries() -> Vec<(String, RECT)> {
    use windows_sys::Win32::{
        Foundation::LPARAM,
        Graphics::Gdi::{
            EnumDisplayMonitors, GetMonitorInfoW, HDC, HMONITOR, MONITORINFO, MONITORINFOEXW,
        },
    };
    unsafe extern "system" fn collect(
        monitor: HMONITOR,
        _: HDC,
        _: *mut RECT,
        data: LPARAM,
    ) -> windows_sys::core::BOOL {
        let entries = unsafe { &mut *(data as *mut Vec<(String, RECT)>) };
        let mut info = MONITORINFOEXW::default();
        info.monitorInfo.cbSize = std::mem::size_of::<MONITORINFOEXW>() as u32;
        if unsafe { GetMonitorInfoW(monitor, &mut info.monitorInfo as *mut MONITORINFO) } != 0 {
            let end = info
                .szDevice
                .iter()
                .position(|value| *value == 0)
                .unwrap_or(info.szDevice.len());
            entries.push((
                String::from_utf16_lossy(&info.szDevice[..end]),
                info.monitorInfo.rcWork,
            ));
        }
        1
    }
    let mut entries = Vec::new();
    unsafe {
        EnumDisplayMonitors(
            std::ptr::null_mut(),
            std::ptr::null(),
            Some(collect),
            &mut entries as *mut _ as LPARAM,
        );
    }
    entries
}

fn normal_window_bounds(hwnd: HWND) -> Option<(PhysicalPosition, PhysicalSize)> {
    use windows_sys::Win32::UI::WindowsAndMessaging::{GetWindowPlacement, WINDOWPLACEMENT};
    let mut placement = unsafe { std::mem::zeroed::<WINDOWPLACEMENT>() };
    placement.length = std::mem::size_of::<WINDOWPLACEMENT>() as u32;
    if unsafe { GetWindowPlacement(hwnd, &mut placement) } == 0 {
        return None;
    }
    let bounds = placement.rcNormalPosition;
    Some((
        PhysicalPosition::new(bounds.left, bounds.top),
        PhysicalSize::new(
            (bounds.right - bounds.left).max(1) as u32,
            (bounds.bottom - bounds.top).max(1) as u32,
        ),
    ))
}

pub fn hwnd(window: &slint::Window) -> Option<HWND> {
    let handle_provider = window.window_handle();
    let handle = handle_provider.window_handle().ok()?;
    match handle.as_raw() {
        RawWindowHandle::Win32(handle) => Some(handle.hwnd.get() as HWND),
        _ => None,
    }
}

fn primary_work_area() -> RECT {
    let mut area = RECT::default();
    let succeeded =
        unsafe { SystemParametersInfoW(SPI_GETWORKAREA, 0, &mut area as *mut RECT as *mut _, 0) };
    if succeeded == 0 || area.right <= area.left || area.bottom <= area.top {
        RECT {
            left: 0,
            top: 0,
            right: 1920,
            bottom: 1080,
        }
    } else {
        area
    }
}

#[cfg(test)]
#[allow(clippy::items_after_test_module)]
mod tests {
    use super::*;

    #[test]
    fn remote_placement_defaults_match_v0302_window_size() {
        let placement = RemoteWindowPlacement::default();
        assert!(!placement.has_value);
        assert_eq!((placement.width_dip, placement.height_dip), (700, 510));
    }

    #[test]
    fn remote_placement_round_trip_uses_existing_config_fields() {
        let json = serde_json::to_string(&RemoteWindowPlacement {
            has_value: true,
            left_ratio: 0.25,
            top_ratio: 0.75,
            width_dip: 760,
            height_dip: 540,
            maximized: true,
            ..RemoteWindowPlacement::default()
        })
        .unwrap();
        let restored: RemoteWindowPlacement = serde_json::from_str(&json).unwrap();

        assert!(restored.has_value);
        assert_eq!((restored.left_ratio, restored.top_ratio), (0.25, 0.75));
        assert_eq!((restored.width_dip, restored.height_dip), (760, 540));
        assert!(restored.maximized);
    }
}

fn window_dpi(hwnd: HWND) -> u32 {
    use windows_sys::Win32::Graphics::Gdi::{GetDC, GetDeviceCaps, LOGPIXELSX, ReleaseDC};
    let dc = unsafe { GetDC(hwnd) };
    if dc.is_null() {
        return 96;
    }
    let dpi = unsafe { GetDeviceCaps(dc, LOGPIXELSX as i32) }.max(96) as u32;
    unsafe { ReleaseDC(hwnd, dc) };
    dpi
}

unsafe fn apply_shape(hwnd: HWND, shape: &str) {
    if !shape.contains("圆角") {
        unsafe { SetWindowRgn(hwnd, std::ptr::null_mut(), 1) };
        return;
    }

    let mut client = RECT::default();
    if unsafe { GetClientRect(hwnd, &mut client) } == 0 {
        return;
    }
    let diameter = if shape.contains('大') { 28 } else { 14 };
    let diameter = (diameter as f32 * window_dpi(hwnd) as f32 / 96.0).round() as i32;
    let region = unsafe {
        CreateRoundRectRgn(
            0,
            0,
            client.right - client.left + 1,
            client.bottom - client.top + 1,
            diameter,
            diameter,
        )
    };
    if !region.is_null() {
        unsafe { SetWindowRgn(hwnd, region, 1) };
    }
}
