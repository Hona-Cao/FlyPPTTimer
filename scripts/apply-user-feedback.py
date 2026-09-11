"""One-shot reviewed transformations; fail rather than modify an unexpected source."""
from pathlib import Path
import subprocess

EXPECTED = {
    'ui/app-window.slint': 'ca93abbec20035cc009f69c5cfb23bca5e509f32',
    'src/audio.rs': 'e13b98fd2d1df0f4639d420757ce4c70cbe8b0b4',
    'src/remote.rs': '6f3be902d8a447a7ff554869c3d138ddd77c1294',
    'Cargo.toml': 'bc8febbdfc66e2c8624fa2741923c196ab0ac759',
}
for path, expected in EXPECTED.items():
    actual = subprocess.check_output(['git', 'rev-parse', 'HEAD:' + path], text=True).strip()
    if actual != expected:
        raise SystemExit(f'Reviewed input changed: {path} {actual}')

def one(text, old, new):
    if text.count(old) != 1:
        raise SystemExit(f'Expected one exact match: {old[:100]!r}; found {text.count(old)}')
    return text.replace(old, new, 1)

def write(path, text):
    Path(path).write_text(text, encoding='utf-8', newline='\n')

ui = Path('ui/app-window.slint').read_text(encoding='utf-8')
ui = one(ui, 'height: parent.height - 92px;', 'height: parent.height - 76px;')
ui = one(ui, '''        ScrollView {
            x: 18px;
            y: 56px;
            width: parent.width - 36px;''', '''        settings-scroll := ScrollView {
            x: 18px;
            y: 56px;
            width: parent.width - 26px;
            viewport-width: self.visible-width;''')
ui = one(ui, 'height: root.height - 164px;', 'height: parent.height - 64px;')
ui = one(ui, '''            VerticalLayout {
                width: parent.width;
                spacing: 10px;
                padding-bottom: 12px;''', '''            VerticalLayout {
                width: settings-scroll.visible-width - 12px;
                alignment: start;
                spacing: 10px;
                padding-bottom: 12px;''')
ui = one(ui, '''                if root.current-page == 0 && root.selected-rule >= 0: Rectangle {
                    height: 76px;''', '''                if root.current-page == 0 && root.selected-rule >= 0: Rectangle {
                    height: 54px;
                    min-height: 54px;
                    max-height: 54px;''')
ui = one(ui, 'x: 16px; y: 21px; width: parent.width - 32px; height: 34px;', 'x: 16px; y: 10px; width: parent.width - 32px; height: 34px;')
ui = one(ui, '''        y: parent.height - 68px;
        width: parent.width - 196px;
        height: 68px;''', '''        y: parent.height - 76px;
        width: parent.width - 196px;
        height: 76px;''')
ui = one(ui, '''            x: parent.width - 290px;
            y: 16px;
            width: 290px;
            height: 36px;
            spacing: 6px;''', '''            x: parent.width - 324px;
            y: 20px;
            width: 300px;
            height: 36px;
            spacing: 12px;''')
ui = one(ui, '''        ScrollView {
            x: 16px; y: 16px; width: parent.width - 32px; height: parent.height - 32px;
            VerticalLayout {
                width: parent.width;
                spacing: 12px;''', '''        connection-scroll := ScrollView {
            x: 16px; y: 16px; width: parent.width - 24px; height: parent.height - 32px;
            viewport-width: self.visible-width;
            VerticalLayout {
                width: connection-scroll.visible-width - 12px;
                alignment: start;
                spacing: 12px;''')
ui = one(ui, '''                Text { text: root.address-list-text; color: DesktopTheme.muted; font-size: 12px; wrap: word-wrap; }
''', '')
ui = one(ui, '''        ScrollView {
            x: 8px; y: 34px; width: parent.width - 16px; height: parent.height - 42px;
            VerticalLayout {
                width: parent.width;
                spacing: 6px;''', '''        rules-scroll := ScrollView {
            x: 8px; y: 34px; width: parent.width - 12px; height: parent.height - 42px;
            viewport-width: self.visible-width;
            VerticalLayout {
                width: rules-scroll.visible-width - 10px;
                alignment: start;
                spacing: 6px;''')
ui = one(ui, 'height: root.height - 266px;', 'height: root.height - 252px;')
ui = one(ui, 'y: root.height - 176px; width: parent.width - 48px; height: 72px;', 'y: root.height - 154px; width: parent.width - 48px; height: 54px;')
ui = one(ui, 'y: 18px; width: parent.width; height: 34px; alignment: center; spacing: 10px;', 'x: 12px; y: 10px; width: parent.width - 24px; height: 34px; alignment: center; spacing: 10px;')
start = ui.index('    if root.dialog-open: Rectangle {')
assert ui[start:].rstrip().endswith('}')
ui = ui[:start] + '''    if root.dialog-open: Rectangle {
        init => { dialog-ok.focus(); }
        x: 0px; y: 0px; width: parent.width; height: parent.height;
        background: DesktopTheme.scrim;
        TouchArea { }
        dialog-card := Rectangle {
            width: min(660px, parent.width - 48px);
            // Measure the actual visible text at its actual font and width.
            // Long content scrolls inside the available window, never clips.
            height: min(parent.height - 48px, max(190px, dialog-body.preferred-height + 158px));
            x: (parent.width - self.width) / 2;
            y: (parent.height - self.height) / 2;
            background: DesktopTheme.surface; border-radius: 12px; border-width: 1px; border-color: DesktopTheme.border;
            Text { x: 24px; y: 18px; width: parent.width - 48px; height: 34px; text: root.dialog-title; font-size: 18px; font-weight: 700; color: DesktopTheme.text; vertical-alignment: center; }
            Rectangle { x: 24px; y: 62px; width: parent.width - 48px; height: parent.height - 132px; background: DesktopTheme.subtle; border-radius: 8px; border-width: 1px; border-color: DesktopTheme.border; }
            dialog-scroll := ScrollView {
                x: 28px; y: 66px; width: parent.width - 56px; height: parent.height - 140px;
                viewport-width: self.visible-width;
                viewport-height: dialog-body.preferred-height + 20px;
                viewport-x: 0px;
                viewport-y: 0px;
                dialog-body := Text {
                    x: 12px; y: 10px;
                    width: dialog-card.width - 104px;
                    height: self.preferred-height;
                    text: root.dialog-message;
                    wrap: word-wrap;
                    vertical-alignment: top;
                    color: DesktopTheme.secondary;
                    font-size: 14px;
                }
            }
            Button { visible: root.dialog-restart; x: parent.width - 212px; y: parent.height - 52px; width: 88px; height: 34px; text: root.cancel-text; clicked => { root.dialog-open = false; } }
            dialog-ok := Button { x: parent.width - 112px; y: parent.height - 52px; width: 88px; height: 34px; text: root.ok-text; clicked => { root.dialog-open = false; root.dialog-confirm(); } }
        }
    }

}
'''
write('ui/app-window.slint', ui)

audio = Path('src/audio.rs').read_text(encoding='utf-8')
audio = one(audio, '    thread,\n', '    thread,\n    time::{Duration, Instant},\n')
audio = one(audio, '        System::Com::{CLSCTX_ALL, COINIT_APARTMENTTHREADED, CoCreateInstance, CoInitializeEx},', '''        System::{
            Com::{CLSCTX_ALL, CLSCTX_INPROC_SERVER, CLSIDFromProgID, COINIT_APARTMENTTHREADED,
                CoCreateInstance, CoInitializeEx, DISPATCH_FLAGS, DISPATCH_METHOD,
                DISPATCH_PROPERTYGET, DISPATCH_PROPERTYPUT, DISPPARAMS, IDispatch},
            Variant::VARIANT,
        },''')
audio = one(audio, '    core::PCWSTR,', '    core::{GUID, PCWSTR},')
a = audio.index('fn play_sound(path: &str) {')
b = audio.index('#[link(name = "winmm")]', a)
audio = audio[:a] + r'''fn play_sound(path: &str) {
    if !Path::new(path).is_file() {
        eprintln!("prompt sound file does not exist: {path}");
        return;
    }
    let command_path = path.replace('"', "\"\"");
    let open = wide(&format!("open \"{command_path}\" alias FlyPPTTimerAlert"));
    let play = wide("play FlyPPTTimerAlert wait");
    let close = wide("close FlyPPTTimerAlert");
    let result = unsafe {
        let open_result = mci_send_string(open.as_ptr(), std::ptr::null_mut(), 0, 0);
        if open_result == 0 {
            let play_result = mci_send_string(play.as_ptr(), std::ptr::null_mut(), 0, 0);
            let _ = mci_send_string(close.as_ptr(), std::ptr::null_mut(), 0, 0);
            play_result
        } else { open_result }
    };
    if result != 0 && let Err(error) = play_wmp_native(path) {
        eprintln!("failed to play prompt sound (MCI error {result}): {error}");
    }
}

// Keep the existing codec fallback, but host it on the audio STA directly.
// No shell, command interpreter, encoded script or process launch is involved.
fn play_wmp_native(path: &str) -> Result<(), String> {
    let prog_id = wide("WMPlayer.OCX");
    let class = unsafe { CLSIDFromProgID(PCWSTR(prog_id.as_ptr())) }.map_err(|e| e.to_string())?;
    let player: IDispatch = unsafe { CoCreateInstance(&class, None, CLSCTX_INPROC_SERVER) }
        .map_err(|e| e.to_string())?;
    let result = (|| {
        let settings = player_object(&player, "settings")?;
        player_invoke(&settings, "autoStart", DISPATCH_PROPERTYPUT, &[VARIANT::from(false)])?;
        player_invoke(&player, "URL", DISPATCH_PROPERTYPUT, &[VARIANT::from(path)])?;
        let controls = player_object(&player, "controls")?;
        player_invoke(&controls, "play", DISPATCH_METHOD, &[])?;
        let started_at = Instant::now();
        let mut started = false;
        loop {
            pump_audio_messages();
            let value = player_invoke(&player, "playState", DISPATCH_PROPERTYGET, &[])?;
            let state = i32::try_from(&value).map_err(|e| e.to_string())?;
            if state == 8 || (started && matches!(state, 1 | 10)) { return Ok(()); }
            started |= matches!(state, 3..=5);
            // Bound startup only: valid, long audio is not cut off.
            if !started && started_at.elapsed() >= Duration::from_secs(15) {
                return Err("native media player did not start the selected sound".to_owned());
            }
            thread::sleep(Duration::from_millis(25));
        }
    })();
    // close also stops playback; execute it on both success and error paths.
    let _ = player_invoke(&player, "close", DISPATCH_METHOD, &[]);
    result
}

fn pump_audio_messages() {
    use windows_sys::Win32::UI::WindowsAndMessaging::{DispatchMessageW, MSG, PM_REMOVE, PeekMessageW, TranslateMessage};
    unsafe {
        let mut message = MSG::default();
        while PeekMessageW(&mut message, std::ptr::null_mut(), 0, 0, PM_REMOVE) != 0 {
            TranslateMessage(&message);
            DispatchMessageW(&message);
        }
    }
}

fn player_object(player: &IDispatch, name: &str) -> Result<IDispatch, String> {
    let value = player_invoke(player, name, DISPATCH_PROPERTYGET, &[])?;
    IDispatch::try_from(&value).map_err(|e| e.to_string())
}

fn player_invoke(object: &IDispatch, name: &str, flags: DISPATCH_FLAGS, args: &[VARIANT]) -> Result<VARIANT, String> {
    let name = wide(name);
    let name_ptr = PCWSTR(name.as_ptr());
    let mut id = 0;
    unsafe { object.GetIDsOfNames(&GUID::zeroed(), &name_ptr, 1, 0, &mut id) }.map_err(|e| e.to_string())?;
    let mut reversed: Vec<_> = args.iter().rev().cloned().collect();
    let property_put = flags == DISPATCH_PROPERTYPUT;
    let mut named_id = -3;
    let params = DISPPARAMS {
        rgvarg: if reversed.is_empty() { std::ptr::null_mut() } else { reversed.as_mut_ptr() },
        rgdispidNamedArgs: if property_put { &mut named_id } else { std::ptr::null_mut() },
        cArgs: reversed.len() as u32,
        cNamedArgs: u32::from(property_put),
    };
    let mut value = VARIANT::default();
    unsafe { object.Invoke(id, &GUID::zeroed(), 0, flags, &params, Some(&mut value), None, None) }.map_err(|e| e.to_string())?;
    Ok(value)
}

''' + audio[b:]
assert 'powershell' not in audio.lower() and 'Command::' not in audio
write('src/audio.rs', audio)

remote = Path('src/remote.rs').read_text(encoding='utf-8')
a = remote.index('pub fn lan_addresses() -> Vec<String> {')
b = remote.index('pub fn mask_token', a)
remote = remote[:a] + r'''pub fn lan_addresses() -> Vec<String> {
    use windows_sys::Win32::{
        Foundation::{ERROR_BUFFER_OVERFLOW, NO_ERROR},
        NetworkManagement::{IpHelper::{GetAdaptersAddresses, IP_ADAPTER_ADDRESSES_LH,
            GAA_FLAG_INCLUDE_GATEWAYS, GAA_FLAG_SKIP_ANYCAST, GAA_FLAG_SKIP_MULTICAST, GAA_FLAG_SKIP_DNS_SERVER},
            Ndis::IfOperStatusUp},
        Networking::WinSock::{AF_INET, SOCKADDR_IN},
    };
    // An aligned buffer owns all records until traversal is complete.
    let mut bytes = 15_000u32;
    for _ in 0..3 {
        let mut buffer = vec![0u64; (bytes as usize).div_ceil(8)];
        let head = buffer.as_mut_ptr().cast::<IP_ADAPTER_ADDRESSES_LH>();
        let result = unsafe { GetAdaptersAddresses(u32::from(AF_INET),
            GAA_FLAG_INCLUDE_GATEWAYS | GAA_FLAG_SKIP_ANYCAST | GAA_FLAG_SKIP_MULTICAST | GAA_FLAG_SKIP_DNS_SERVER,
            std::ptr::null(), head, &mut bytes) };
        if result == ERROR_BUFFER_OVERFLOW {
            if bytes > 1_048_576 { return Vec::new(); }
            continue;
        }
        if result != NO_ERROR { return Vec::new(); }
        let mut candidates = Vec::new();
        let mut adapter_ptr = head;
        while let Some(adapter) = unsafe { adapter_ptr.as_ref() } {
            if adapter.OperStatus == IfOperStatusUp && matches!(adapter.IfType, 6 | 71) {
                let mut address_ptr = adapter.FirstUnicastAddress;
                while let Some(address) = unsafe { address_ptr.as_ref() } {
                    let socket = address.Address;
                    if !socket.lpSockaddr.is_null()
                        && socket.iSockaddrLength >= std::mem::size_of::<SOCKADDR_IN>() as i32
                        && unsafe { (*socket.lpSockaddr).sa_family } == AF_INET
                    {
                        let socket = unsafe { &*socket.lpSockaddr.cast::<SOCKADDR_IN>() };
                        let ip = Ipv4Addr::from(unsafe { socket.sin_addr.S_un.S_addr }.to_ne_bytes());
                        if is_lan(ip) {
                            candidates.push((!adapter.FirstGatewayAddress.is_null(), adapter.IfType == 71, adapter.Ipv4Metric, ip));
                        }
                    }
                    address_ptr = address.Next;
                }
            }
            adapter_ptr = adapter.Next;
        }
        return preferred_lan_address(candidates);
    }
    Vec::new()
}

// One connection address and QR: only assigned unicast IPs are candidates,
// never DNS servers, subnet masks, DHCP servers or default gateways.
fn preferred_lan_address(mut candidates: Vec<(bool, bool, u32, Ipv4Addr)>) -> Vec<String> {
    candidates.sort_by_key(|(gateway, wifi, metric, ip)| (!*gateway, !*wifi, *metric, *ip));
    candidates.into_iter().find(|(_, _, _, ip)| is_lan(*ip))
        .map(|(_, _, _, ip)| vec![ip.to_string()]).unwrap_or_default()
}

''' + remote[b:]
if 'Command::' not in remote:
    remote = one(remote, '    process::Command,\n', '')
# HashSet is still used elsewhere only if it remains referenced after removing ipconfig.
if remote.count('HashSet') == 1:
    remote = one(remote, 'collections::{HashMap, HashSet}', 'collections::HashMap')
remote += '''
#[cfg(test)]
mod feedback_address_tests {
    use super::*;
    #[test]
    fn phone_hotspot_wifi_is_one_address_not_the_gateway() {
        assert_eq!(preferred_lan_address(vec![
            (true, false, 10, Ipv4Addr::new(192,168,1,25)),
            (true, true, 30, Ipv4Addr::new(172,20,10,4)),
        ]), vec!["172.20.10.4"]);
    }
    #[test]
    fn disconnected_or_non_lan_candidates_do_not_create_a_phone_url() {
        assert!(preferred_lan_address(Vec::new()).is_empty());
        assert!(preferred_lan_address(vec![(true, true, 1, Ipv4Addr::LOCALHOST)]).is_empty());
        assert_eq!(preferred_lan_address(vec![(true, true, 10, Ipv4Addr::new(192,168,43,20))]), vec!["192.168.43.20"]);
    }
}
'''
write('src/remote.rs', remote)
cargo = Path('Cargo.toml').read_text(encoding='utf-8')
cargo = one(cargo, '"Win32_Networking_WinHttp"', '"Win32_Networking_WinHttp", "Win32_Networking_WinSock", "Win32_NetworkManagement_IpHelper", "Win32_NetworkManagement_Ndis"')
write('Cargo.toml', cargo)
print('Prepared only reviewed layout, native audio, and assigned-address fixes.')
