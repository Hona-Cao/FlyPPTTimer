use slint::{Color, ComponentHandle};

pub fn parse_hex(value: &str) -> Option<Color> {
    let value = value.trim().strip_prefix('#').unwrap_or(value.trim());
    if value.len() != 6 || !value.bytes().all(|c| c.is_ascii_hexdigit()) {
        return None;
    }
    let rgb = u32::from_str_radix(value, 16).ok()?;
    Some(Color::from_rgb_u8(
        (rgb >> 16) as u8,
        (rgb >> 8) as u8,
        rgb as u8,
    ))
}
fn color_ref(color: Color) -> u32 {
    u32::from(color.red()) | (u32::from(color.green()) << 8) | (u32::from(color.blue()) << 16)
}
fn hex_from_ref(rgb: u32) -> String {
    format!(
        "#{:02X}{:02X}{:02X}",
        rgb & 255,
        (rgb >> 8) & 255,
        (rgb >> 16) & 255
    )
}

pub fn open(window: &crate::app::SettingsWindow, row: i32, current: &str) {
    if window.get_native_dialog_open() {
        return;
    }
    let initial = color_ref(parse_hex(current).unwrap_or(Color::from_rgb_u8(0, 0, 0)));
    let owner = crate::window::hwnd(window.window()).unwrap_or(std::ptr::null_mut()) as isize;
    window.set_native_dialog_open(true);
    let weak = window.as_weak();
    if let Err(error) = std::thread::Builder::new()
        .name("flyppttimer-color-dialog".into())
        .spawn(move || {
            let selected = choose(owner, initial);
            let _ = slint::invoke_from_event_loop(move || {
                if let Some(window) = weak.upgrade() {
                    window.set_native_dialog_open(false);
                    if let Some(value) = selected {
                        window.invoke_field_edited(row, value.into(), false, 0);
                    }
                    window.window().request_redraw();
                }
            });
        })
    {
        window.set_native_dialog_open(false);
        crate::log::error(&format!("Color chooser failed: {error}"));
    }
}

fn choose(owner: isize, initial: u32) -> Option<String> {
    use std::sync::{Mutex, OnceLock};
    use windows_sys::Win32::UI::Controls::Dialogs::{
        CC_ENABLEHOOK, CC_FULLOPEN, CC_RGBINIT, CHOOSECOLORW, ChooseColorW,
    };
    static CUSTOM: OnceLock<Mutex<[u32; 16]>> = OnceLock::new();
    let palette = CUSTOM.get_or_init(|| {
        Mutex::new([
            0xFFFFFF, 0x000000, 0x663A0B, 0xFCF8F3, 0xD8A34E, 0x202080, 0x505050, 0xC0C0C0,
            0x0000FF, 0x0080FF, 0x00FFFF, 0x00AA00, 0xCCCC00, 0xFF6600, 0x800080, 0xFFC0CB,
        ])
    });
    let mut colors = *palette.lock().unwrap_or_else(|e| e.into_inner());
    let mut dialog = CHOOSECOLORW {
        lStructSize: std::mem::size_of::<CHOOSECOLORW>() as u32,
        hwndOwner: owner as _,
        rgbResult: initial,
        lpCustColors: colors.as_mut_ptr(),
        Flags: CC_ENABLEHOOK | CC_FULLOPEN | CC_RGBINIT,
        lpfnHook: Some(crate::window::brand_common_dialog),
        ..unsafe { std::mem::zeroed() }
    };
    let accepted = unsafe { ChooseColorW(&mut dialog) } != 0;
    *palette.lock().unwrap_or_else(|e| e.into_inner()) = colors;
    accepted.then(|| hex_from_ref(dialog.rgbResult))
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn rgb_and_hex_roundtrip_and_reject_invalid_color() {
        for value in ["#0B3A66", "#FFFFFF", "#000000", "#01ABF0"] {
            assert_eq!(hex_from_ref(color_ref(parse_hex(value).unwrap())), value);
        }
        assert_eq!(parse_hex(" 0b3a66 "), parse_hex("#0B3A66"));
        for value in ["#xyz123", "#12345", "#1234567", "red", "", "#中文颜色"] {
            assert!(parse_hex(value).is_none());
        }
    }
}
