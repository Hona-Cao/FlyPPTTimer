use slint::ComponentHandle;

pub fn is_dark(choice: &str) -> bool {
    match choice {
        "dark" => true,
        "light" => false,
        _ => unsafe {
            use windows_sys::Win32::System::Registry::{
                HKEY_CURRENT_USER, RRF_RT_REG_DWORD, RegGetValueW,
            };
            let mut light: u32 = 1;
            let mut size = std::mem::size_of::<u32>() as u32;
            RegGetValueW(
                HKEY_CURRENT_USER,
                windows_sys::w!(r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"),
                windows_sys::w!("AppsUseLightTheme"),
                RRF_RT_REG_DWORD,
                std::ptr::null_mut(),
                (&mut light as *mut u32).cast(),
                &mut size,
            );
            light == 0
        },
    }
}

fn titlebar(window: &slint::Window, dark: bool) -> bool {
    let Some(hwnd) = crate::window::hwnd(window) else {
        return false;
    };
    let enabled = i32::from(dark);
    unsafe {
        use windows_sys::Win32::Graphics::Dwm::{
            DWMWA_USE_IMMERSIVE_DARK_MODE, DwmSetWindowAttribute,
        };
        // Unsupported Windows versions keep their system-drawn title bar.
        DwmSetWindowAttribute(
            hwnd,
            DWMWA_USE_IMMERSIVE_DARK_MODE as u32,
            (&enabled as *const i32).cast(),
            std::mem::size_of::<i32>() as u32,
        );
    }
    true
}

pub fn settings(window: &crate::app::SettingsWindow, dark: bool) {
    if window.get_dark_theme() != dark || !window.get_theme_initialized() {
        window.set_dark_theme(dark);
        window.set_theme_initialized(titlebar(window.window(), dark));
    }
}

pub fn remote(window: &crate::app::PresentationWindow, dark: bool) {
    if window.get_dark_theme() != dark || !window.get_theme_initialized() {
        window.set_dark_theme(dark);
        window.set_theme_initialized(titlebar(window.window(), dark));
    }
}
