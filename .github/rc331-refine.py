from pathlib import Path
import re


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{label}: expected 1 match, found {count}")
    return text.replace(old, new, 1)

# Add only the Win32 namespaces required by the existing windows 0.62 dependency.
p = Path("Cargo.toml")
s = p.read_text(encoding="utf-8")
old = 'windows = { version = "0.62", features = ["Win32_Media_Audio", "Win32_Media_Audio_Endpoints", "Win32_Media_Speech", "Win32_System_Com", "Win32_System_Com_StructuredStorage", "Win32_System_Ole", "Win32_System_Variant"] }'
new = 'windows = { version = "0.62", features = ["Win32_Foundation", "Win32_Media_Audio", "Win32_Media_Audio_Endpoints", "Win32_Media_Speech", "Win32_System_Com", "Win32_System_Com_StructuredStorage", "Win32_System_Ole", "Win32_System_Variant", "Win32_UI_Shell", "Win32_UI_Shell_Common"] }'
s = replace_once(s, old, new, "Cargo windows features")
p.write_text(s, encoding="utf-8", newline="\n")

# Accepted sockets must be blocking with bounded timeouts. The listener remains nonblocking.
p = Path("src/remote.rs")
s = p.read_text(encoding="utf-8")
old = '''fn handle_connection(mut stream: TcpStream, address: SocketAddr, context: ConnectionContext) {
    let _ = stream.set_read_timeout(Some(Duration::from_secs(8)));
    let _ = stream.set_write_timeout(Some(Duration::from_secs(8)));
    let result = read_request(&mut stream).and_then(|request| route(request, address, &context));
'''
new = '''fn configure_client_stream(stream: &TcpStream) -> Result<(), String> {
    // The listening socket is non-blocking so the server thread can observe stop requests.
    // Winsock may propagate that mode to an accepted socket. HTTP parsing is intentionally
    // blocking with bounded timeouts; otherwise the first read can race the request bytes and
    // surface WSAEWOULDBLOCK (10035) to a freshly scanned phone.
    stream
        .set_nonblocking(false)
        .map_err(|error| error.to_string())?;
    stream
        .set_read_timeout(Some(Duration::from_secs(8)))
        .map_err(|error| error.to_string())?;
    stream
        .set_write_timeout(Some(Duration::from_secs(8)))
        .map_err(|error| error.to_string())?;
    Ok(())
}

fn handle_connection(mut stream: TcpStream, address: SocketAddr, context: ConnectionContext) {
    let result = configure_client_stream(&stream)
        .and_then(|()| read_request(&mut stream))
        .and_then(|request| route(request, address, &context));
'''
s = replace_once(s, old, new, "remote accepted stream")
needle = '''#[cfg(test)]
mod tests {
    use super::*;
'''
insert = '''#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn client_stream_is_restored_to_blocking_before_http_read() {
        use std::io::Write as _;

        let listener = TcpListener::bind((Ipv4Addr::LOCALHOST, 0)).unwrap();
        let address = listener.local_addr().unwrap();
        let writer = thread::spawn(move || {
            let mut stream = TcpStream::connect(address).unwrap();
            thread::sleep(Duration::from_millis(40));
            stream
                .write_all(b"GET /state?token=test HTTP/1.1\\r\\nHost: localhost\\r\\n\\r\\n")
                .unwrap();
        });
        let (mut stream, _) = listener.accept().unwrap();
        // Make the regression deterministic even where accept does not inherit nonblocking mode.
        stream.set_nonblocking(true).unwrap();
        configure_client_stream(&stream).unwrap();
        let request = read_request(&mut stream).unwrap();
        assert_eq!(request.method, "GET");
        assert_eq!(request.raw_url, "/state?token=test");
        writer.join().unwrap();
    }
'''
s = replace_once(s, needle, insert, "remote regression test insertion")
p.write_text(s, encoding="utf-8", newline="\n")

# Replace the legacy GetOpenFileNameW dialog with the Vista+ Common Item Dialog.
p = Path("src/settings.rs")
s = p.read_text(encoding="utf-8")
start = s.find('const PRESENTATION_FILTER: &str')
end_marker = '\npub(crate) fn is_supported_presentation_path'
end = s.find(end_marker, start)
if start < 0 or end < 0:
    raise SystemExit("presentation picker block not found")
modern_picker = r'''const PRESENTATION_FILTER_PATTERN: windows::core::PCWSTR =
    windows::core::w!("*.ppt;*.pptx;*.pptm");

pub(crate) fn native_open_presentations() -> Vec<PathBuf> {
    use windows::{
        Win32::{
            Foundation::HWND,
            System::Com::{
                CLSCTX_INPROC_SERVER, COINIT_APARTMENTTHREADED, CoCreateInstance, CoInitializeEx,
                CoTaskMemFree, CoUninitialize,
            },
            UI::Shell::{
                Common::COMDLG_FILTERSPEC, FOS_ALLOWMULTISELECT, FOS_FILEMUSTEXIST,
                FOS_FORCEFILESYSTEM, FOS_PATHMUSTEXIST, FileOpenDialog, IFileOpenDialog,
                SIGDN_FILESYSPATH,
            },
        },
        core::w,
    };

    struct ComApartment;
    impl Drop for ComApartment {
        fn drop(&mut self) {
            unsafe { CoUninitialize() };
        }
    }

    if unsafe { CoInitializeEx(None, COINIT_APARTMENTTHREADED) }.is_err() {
        return Vec::new();
    }
    let _apartment = ComApartment;
    let dialog: IFileOpenDialog = match unsafe {
        CoCreateInstance(&FileOpenDialog, None, CLSCTX_INPROC_SERVER)
    } {
        Ok(dialog) => dialog,
        Err(_) => return Vec::new(),
    };
    let mut options = match unsafe { dialog.GetOptions() } {
        Ok(options) => options,
        Err(_) => return Vec::new(),
    };
    options |= FOS_ALLOWMULTISELECT | FOS_FILEMUSTEXIST | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST;
    if unsafe { dialog.SetOptions(options) }.is_err() {
        return Vec::new();
    }
    let filters = [COMDLG_FILTERSPEC {
        pszName: w!("PowerPoint (*.ppt;*.pptx;*.pptm)"),
        pszSpec: PRESENTATION_FILTER_PATTERN,
    }];
    if unsafe { dialog.SetFileTypes(&filters) }.is_err() {
        return Vec::new();
    }
    let _ = unsafe { dialog.SetFileTypeIndex(1) };
    let _ = unsafe { dialog.SetTitle(w!("选择 PPT 文件")) };

    let owner = HWND(crate::window::app_dialog_owner());
    if unsafe { dialog.Show(Some(owner)) }.is_err() {
        return Vec::new();
    }
    let items = match unsafe { dialog.GetResults() } {
        Ok(items) => items,
        Err(_) => return Vec::new(),
    };
    let count = match unsafe { items.GetCount() } {
        Ok(count) => count,
        Err(_) => return Vec::new(),
    };
    let mut paths = Vec::with_capacity(count as usize);
    for index in 0..count {
        let Ok(item) = (unsafe { items.GetItemAt(index) }) else {
            continue;
        };
        let Ok(display_name) = (unsafe { item.GetDisplayName(SIGDN_FILESYSPATH) }) else {
            continue;
        };
        let value = unsafe { display_name.to_string() }.ok();
        unsafe { CoTaskMemFree(Some(display_name.0.cast())) };
        if let Some(value) = value {
            let path = PathBuf::from(value);
            if is_supported_presentation_path(&path) {
                paths.push(path);
            }
        }
    }
    paths
}
'''
s = s[:start] + modern_picker + s[end:]
pattern = re.compile(
    r'    #\[test\]\n    fn powerpoint_dialog_filter_has_separate_label_pattern_and_double_terminator\(\) \{.*?\n    \}\n',
    re.S,
)
replacement = '''    #[test]
    fn powerpoint_dialog_filter_is_strict_and_has_no_all_files_fallback() {
        let pattern = unsafe { PRESENTATION_FILTER_PATTERN.to_string() }.unwrap();
        assert_eq!(pattern, "*.ppt;*.pptx;*.pptm");
    }
'''
s, count = pattern.subn(replacement, s, count=1)
if count != 1:
    raise SystemExit(f"presentation picker test: expected 1 match, found {count}")
p.write_text(s, encoding="utf-8", newline="\n")

print("RC3.3.1 refinements applied")
