from pathlib import Path
import re


def replace_once(path: str, old: str, new: str) -> None:
    file = Path(path)
    text = file.read_text(encoding="utf-8")
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{path}: expected one exact match, found {count}: {old[:80]!r}")
    file.write_text(text.replace(old, new), encoding="utf-8")


def regex_once(path: str, pattern: str, replacement: str) -> None:
    file = Path(path)
    text = file.read_text(encoding="utf-8")
    updated, count = re.subn(pattern, replacement, text, count=1, flags=re.S)
    if count != 1:
        raise RuntimeError(f"{path}: expected one regex match, found {count}: {pattern[:80]!r}")
    file.write_text(updated, encoding="utf-8")


# 1) Keep a clearer gutter between scrollable content and the scrollbar.
replace_once(
    "ui/app-window.slint",
    "width: connection-scroll.visible-width - 12px;",
    "width: connection-scroll.visible-width - 20px;",
)
replace_once(
    "ui/app-window.slint",
    "width: rules-scroll.visible-width - 10px;",
    "width: rules-scroll.visible-width - 18px;",
)
replace_once(
    "ui/app-window.slint",
    "width: settings-scroll.visible-width - 12px;",
    "width: settings-scroll.visible-width - 20px;",
)

# 2) Black-screen mode: ESC is a local presenter escape hatch even though the
# fullscreen overlay intentionally remains non-activating.
replace_once(
    "src/window.rs",
    "pub fn is_minimized(window: &slint::Window) -> bool {\n    hwnd(window).is_some_and(|hwnd| unsafe { IsIconic(hwnd) != 0 })\n}\n\npub fn show_time_up_window",
    "pub fn is_minimized(window: &slint::Window) -> bool {\n    hwnd(window).is_some_and(|hwnd| unsafe { IsIconic(hwnd) != 0 })\n}\n\npub fn escape_key_down() -> bool {\n    use windows_sys::Win32::UI::Input::KeyboardAndMouse::{GetAsyncKeyState, VK_ESCAPE};\n    unsafe { (GetAsyncKeyState(VK_ESCAPE as i32) as u16 & 0x8000) != 0 }\n}\n\npub fn show_time_up_window",
)
replace_once(
    "src/app.rs",
    "    let last_remote_update = Rc::new(Cell::new(Instant::now()));\n    let last_display_check = Rc::new(Cell::new(Instant::now()));\n\n    let refresh_timer = slint::Timer::default();",
    "    let last_remote_update = Rc::new(Cell::new(Instant::now()));\n    let last_display_check = Rc::new(Cell::new(Instant::now()));\n    let escape_was_down = Cell::new(false);\n\n    let refresh_timer = slint::Timer::default();",
)
replace_once(
    "src/app.rs",
    "        Duration::from_millis(100),\n        move || {\n            if let Some((old, port)) = remote_for_updates.take_port_change() {",
    "        Duration::from_millis(100),\n        move || {\n            let escape_down = window::escape_key_down();\n            let was_escape_down = escape_was_down.replace(escape_down);\n            if preserve_time_up_for_updates.get() && escape_down && !was_escape_down {\n                preserve_time_up_for_updates.set(false);\n                hide_time_up(&time_up_for_updates);\n            }\n            if let Some((old, port)) = remote_for_updates.take_port_change() {",
)

# 3) Any intentional mobile presentation action implicitly clears the app's
# time-up blackout before the requested presentation command runs. Refresh is
# observational and therefore does not change blackout state.
replace_once(
    "src/app.rs",
    "        return result;\n    }\n    match name {",
    "        return result;\n    }\n    if name.starts_with(\"ppt.\") && name != \"ppt.refresh\" && preserve_time_up.get() {\n        preserve_time_up.set(false);\n        hide_time_up(time_up_window);\n    }\n    match name {",
)

# 4) Prompt sound copies keep the user's original filename. A per-slot
# subdirectory prevents two prompts with the same basename from overwriting
# each other while preserving the basename itself.
replace_once(
    "src/settings.rs",
    "    fs::create_dir_all(directory)?;\n    let destination = directory.join(format!(\"{slot}.{extension}\"));",
    "    let file_name = source.file_name().ok_or_else(|| {\n        std::io::Error::new(std::io::ErrorKind::InvalidInput, \"提示音文件名无效。\")\n    })?;\n    let slot_directory = directory.join(slot);\n    fs::create_dir_all(&slot_directory)?;\n    let destination = slot_directory.join(file_name);",
)
replace_once(
    "src/settings.rs",
    "fn sound_import_copies_overwrites_same_slot_and_rejects_unsupported_extension()",
    "fn sound_import_preserves_filename_overwrites_same_slot_and_rejects_unsupported_extension()",
)
replace_once(
    "src/settings.rs",
    "assert_eq!(imported, destination.join(\"prompt1.wav\"));",
    "assert_eq!(imported, destination.join(\"prompt1\").join(\"chosen.WAV\"));",
)

# 5) Alert audio is capped at ten audible seconds. MCI no longer blocks on a
# full-length 'wait'; it polls the device mode and closes the alias at the cap.
replace_once(
    "src/audio.rs",
    "use crate::alerts::AlertEvent;\n\nenum Playback {",
    "use crate::alerts::AlertEvent;\n\nconst MAX_PROMPT_SOUND_DURATION: Duration = Duration::from_secs(10);\n\nenum Playback {",
)
regex_once(
    "src/audio.rs",
    r"fn play_sound\(path: &str\) -> Result<\(\), String> \{.*?\n\}\n\n// Keep the existing codec fallback",
    '''fn play_sound(path: &str) -> Result<(), String> {
    if !Path::new(path).is_file() {
        return Err(format!("Prompt sound file does not exist: {path}"));
    }
    let command_path = path.replace('"', "\\\"\\\"");
    let open = wide(&format!("open \\\"{command_path}\\\" alias FlyPPTTimerAlert"));
    let play = wide("play FlyPPTTimerAlert");
    let status = wide("status FlyPPTTimerAlert mode");
    let close = wide("close FlyPPTTimerAlert");
    let result = unsafe {
        let open_result = mci_send_string(open.as_ptr(), std::ptr::null_mut(), 0, 0);
        if open_result != 0 {
            open_result
        } else {
            let play_result = mci_send_string(play.as_ptr(), std::ptr::null_mut(), 0, 0);
            if play_result != 0 {
                play_result
            } else {
                let started = Instant::now();
                while started.elapsed() < MAX_PROMPT_SOUND_DURATION {
                    let mut mode = [0u16; 32];
                    let status_result =
                        mci_send_string(status.as_ptr(), mode.as_mut_ptr(), mode.len() as u32, 0);
                    if status_result != 0 {
                        break;
                    }
                    let end = mode.iter().position(|value| *value == 0).unwrap_or(mode.len());
                    if String::from_utf16_lossy(&mode[..end])
                        .trim()
                        .eq_ignore_ascii_case("stopped")
                    {
                        break;
                    }
                    thread::sleep(Duration::from_millis(25));
                }
                0
            }
        }
    };
    unsafe {
        let _ = mci_send_string(close.as_ptr(), std::ptr::null_mut(), 0, 0);
    }
    if result != 0 {
        play_wmp_native(path)
            .map_err(|error| format!("Prompt sound failed (MCI error {result}): {error}"))?;
    }
    Ok(())
}

// Keep the existing codec fallback''',
)
replace_once(
    "src/audio.rs",
    "        let mut started = false;\n        let mut last_progress = Instant::now();",
    "        let mut started = false;\n        let mut playback_started_at = None;\n        let mut last_progress = Instant::now();",
)
replace_once(
    "src/audio.rs",
    "            started |= matches!(state, 3..=5);\n            if started {",
    "            if matches!(state, 3..=5) && !started {\n                started = true;\n                playback_started_at = Some(Instant::now());\n            }\n            if playback_started_at\n                .is_some_and(|started| started.elapsed() >= MAX_PROMPT_SOUND_DURATION)\n            {\n                return Ok(());\n            }\n            if started {",
)
replace_once(
    "src/audio.rs",
    "            // Bound startup only: valid, long audio is not cut off.",
    "            // Bound startup separately; audible prompt playback is capped above.",
)

# 6) Mobile Remote: tolerate short LAN hiccups and immediately re-poll when a
# phone returns online/foreground. Commands are never automatically retried,
# avoiding duplicate non-idempotent presentation actions.
replace_once(
    "src/FlyPPTTimer/Web/app.js",
    "let connected=false,lastState=null,messageTimer=null,pollTimer=null,busy=false,pendingConfirmation=null,timerEditorDirty=false,selectedPresentationId=null;",
    "let connected=false,lastState=null,messageTimer=null,pollTimer=null,busy=false,pendingConfirmation=null,timerEditorDirty=false,selectedPresentationId=null,pollFailures=0;",
)
replace_once(
    "src/FlyPPTTimer/Web/app.js",
    "const controller=new AbortController(),timeout=setTimeout(()=>controller.abort(),20000);",
    "const controller=new AbortController(),timeout=setTimeout(()=>controller.abort(),6000);",
)
replace_once(
    "src/FlyPPTTimer/Web/app.js",
    "lastState=s;connection(true);const t=timerState(s),p=s.presentationState||{};",
    "lastState=s;pollFailures=0;connection(true);const t=timerState(s),p=s.presentationState||{};",
)
replace_once(
    "src/FlyPPTTimer/Web/app.js",
    "async function poll(){\n  try{paint(await api('/state'));schedulePoll(1000)}\n  catch(e){connection(false);setAvailability({},{});refreshPresentationButtons();notify('连接失败：'+e.message+'。Clash/TUN 请将本机局域网 IP 和端口设为 DIRECT。',true);schedulePoll(2500)}\n}",
    "async function poll(){\n  try{paint(await api('/state'));schedulePoll(1000)}\n  catch(e){\n    pollFailures+=1;\n    if(pollFailures>=3){\n      connection(false);setAvailability({},{});refreshPresentationButtons();\n      if(pollFailures===3)notify('连接失败：'+e.message+'。正在自动重连；Clash/TUN 请将本机局域网 IP 和端口设为 DIRECT。',true);\n    }\n    schedulePoll(Math.min(5000,500+pollFailures*750));\n  }\n}",
)
replace_once(
    "src/FlyPPTTimer/Web/app.js",
    "$('gotoSlide').addEventListener('click',()=>{const input=$('slideNumber'),value=Number(input.value),max=Number(input.max);if(!Number.isInteger(value)||value<1||value>max){notify(`请输入 1 到 ${max} 之间的页码`,true);input.focus();return}command('ppt.gotoSlide',{slideNumber:value})});\npoll();",
    "$('gotoSlide').addEventListener('click',()=>{const input=$('slideNumber'),value=Number(input.value),max=Number(input.max);if(!Number.isInteger(value)||value<1||value>max){notify(`请输入 1 到 ${max} 之间的页码`,true);input.focus();return}command('ppt.gotoSlide',{slideNumber:value})});\nwindow.addEventListener('online',()=>{if(!busy)schedulePoll(0)});\nwindow.addEventListener('focus',()=>{if(!busy)schedulePoll(0)});\ndocument.addEventListener('visibilitychange',()=>{if(!document.hidden&&!busy)schedulePoll(0)});\npoll();",
)

print("second-feedback direct patch prepared")
