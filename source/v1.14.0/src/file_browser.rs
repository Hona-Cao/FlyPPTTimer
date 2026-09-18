//! Token-protected, read-only directory navigation. No file bytes or shell commands.
use serde::{Deserialize, Serialize};
use std::path::PathBuf;
use windows_sys::Win32::Storage::FileSystem::{GetDriveTypeW, GetLogicalDrives};

#[derive(Default, Deserialize)]
#[serde(default)]
pub struct BrowseRequest {
    pub path: String,
}
#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct Entry {
    pub name: String,
    pub path: String,
    pub is_directory: bool,
}
#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct Listing {
    pub ok: bool,
    pub path: String,
    pub parent: String,
    pub entries: Vec<Entry>,
}

fn local_syntax(path: &str) -> bool {
    let p = path.as_bytes();
    p.len() >= 3
        && p[0].is_ascii_alphabetic()
        && p[1] == b':'
        && matches!(p[2], b'\\' | b'/')
        && !path[2..].contains([':', '\0'])
}
fn local_drive(path: &str) -> bool {
    let root: Vec<u16> = format!("{}:\\", &path[..1])
        .encode_utf16()
        .chain(Some(0))
        .collect();
    // Removable, fixed and RAM disks. Never connect to a UNC or mapped network drive.
    matches!(unsafe { GetDriveTypeW(root.as_ptr()) }, 2 | 3 | 6)
}
pub fn local_path(value: &str) -> Result<PathBuf, String> {
    if !local_syntax(value) || !local_drive(value) {
        return Err("Only local computer drives are available.".into());
    }
    let resolved =
        std::fs::canonicalize(value).map_err(|e| format!("Cannot access folder or file: {e}"))?;
    let path = resolved.to_string_lossy();
    let path = path.strip_prefix(r"\\?\").unwrap_or(&path);
    if !local_syntax(path) || !local_drive(path) {
        return Err("Only local computer drives are available.".into());
    }
    Ok(PathBuf::from(path))
}
pub fn presentation_file(value: &str) -> Result<PathBuf, String> {
    let path = local_path(value)?;
    if !path.is_file() || !crate::settings::is_supported_presentation_path(&path) {
        return Err("Choose a PowerPoint file (.ppt, .pptx or .pptm).".into());
    }
    Ok(path)
}
pub fn browse(value: &str) -> Result<Listing, String> {
    if value.is_empty() {
        let mut entries = Vec::new();
        if let Some(home) = std::env::var_os("USERPROFILE") {
            let home = home.to_string_lossy();
            if let Ok(path) = local_path(&home) {
                entries.push(Entry {
                    name: "Home".into(),
                    path: path.to_string_lossy().into_owned(),
                    is_directory: true,
                });
            }
        }
        let mask = unsafe { GetLogicalDrives() };
        for index in 0..26 {
            let path = format!("{}:\\", char::from(b'A' + index));
            if mask & (1 << index) != 0 && local_drive(&path) {
                entries.push(Entry {
                    name: path.clone(),
                    path,
                    is_directory: true,
                });
            }
        }
        return Ok(Listing {
            ok: true,
            path: String::new(),
            parent: String::new(),
            entries,
        });
    }
    let path = local_path(value)?;
    let read = std::fs::read_dir(&path).map_err(|e| format!("Cannot read this folder: {e}"))?;
    let mut entries = Vec::new();
    for entry in read.flatten() {
        let Ok(meta) = entry.metadata() else { continue };
        if !(meta.is_dir()
            || meta.is_file() && crate::settings::is_supported_presentation_path(&entry.path()))
        {
            continue;
        }
        entries.push(Entry {
            name: entry.file_name().to_string_lossy().into_owned(),
            path: entry.path().to_string_lossy().into_owned(),
            is_directory: meta.is_dir(),
        });
    }
    entries.sort_by(|a, b| {
        b.is_directory
            .cmp(&a.is_directory)
            .then_with(|| crate::mobile_rules::logical_compare(&a.name, &b.name))
    });
    let parent = path
        .parent()
        .map(|p| p.to_string_lossy().into_owned())
        .unwrap_or_default();
    Ok(Listing {
        ok: true,
        path: path.to_string_lossy().into_owned(),
        parent,
        entries,
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn rejects_remote_device_relative_and_alternate_stream_paths() {
        for p in [
            r"\\server\share\file.pptx",
            r"\\.\C:\file.pptx",
            r"C:relative.pptx",
            r"file.pptx",
            r"C:\file.pptx:stream",
            r"\\?\C:\file.pptx",
        ] {
            assert!(!local_syntax(p), "{p}");
        }
        assert!(local_syntax(r"C:\Talks\report.pptx"));
    }
    #[test]
    fn lists_only_directories_and_powerpoint_files_without_reading_contents() {
        let dir = std::env::temp_dir().join(format!("flyppt-browser-{}", std::process::id()));
        std::fs::create_dir_all(dir.join("folder")).unwrap();
        std::fs::write(dir.join("sample.PPTX"), b"example").unwrap();
        std::fs::write(dir.join("secret.txt"), b"private").unwrap();
        let listing = browse(dir.to_str().unwrap()).unwrap();
        assert_eq!(listing.entries.len(), 2);
        assert!(listing.entries[0].is_directory);
        assert_eq!(listing.entries[1].name, "sample.PPTX");
        assert!(presentation_file(dir.join("secret.txt").to_str().unwrap()).is_err());
        std::fs::remove_dir_all(dir).unwrap();
    }
}
