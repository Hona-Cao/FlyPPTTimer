use std::{
    path::Path,
    sync::mpsc::{self, SyncSender},
    thread,
};

use windows::{
    Win32::{
        Media::{
            Audio::{
                Endpoints::IAudioEndpointVolume, IMMDeviceEnumerator, MMDeviceEnumerator,
                eMultimedia, eRender,
            },
            Speech::{ISpVoice, SPF_DEFAULT, SpVoice},
        },
        System::Com::{CLSCTX_ALL, COINIT_APARTMENTTHREADED, CoCreateInstance, CoInitializeEx},
    },
    core::PCWSTR,
};

use crate::alerts::AlertEvent;

enum Playback {
    Speech(String),
    Sound(String),
}

pub struct AudioService {
    sender: Option<SyncSender<Playback>>,
    thread: Option<thread::JoinHandle<()>>,
}

impl AudioService {
    pub fn new() -> Self {
        let (sender, receiver) = mpsc::sync_channel(32);
        let thread = thread::Builder::new()
            .name("flyppttimer-audio".to_owned())
            .spawn(move || {
                let _ = unsafe { CoInitializeEx(None, COINIT_APARTMENTTHREADED) };
                let voice =
                    unsafe { CoCreateInstance::<_, ISpVoice>(&SpVoice, None, CLSCTX_ALL) }.ok();
                while let Ok(playback) = receiver.recv() {
                    match playback {
                        Playback::Speech(text) => {
                            if let Some(voice) = &voice {
                                let text = wide(&text);
                                let _ = unsafe {
                                    voice.Speak(PCWSTR(text.as_ptr()), SPF_DEFAULT.0 as u32, None)
                                };
                            }
                        }
                        Playback::Sound(path) => play_sound(&path),
                    }
                }
            })
            .ok();
        Self {
            sender: Some(sender),
            thread,
        }
    }

    pub fn play(&self, event: &AlertEvent) {
        let playback = if event.prompt.play_sound && !event.prompt.sound_file.trim().is_empty() {
            Playback::Sound(event.prompt.sound_file.clone())
        } else if event.prompt.speak && !event.speech.trim().is_empty() {
            Playback::Speech(event.speech.clone())
        } else {
            return;
        };
        if let Some(sender) = &self.sender {
            let _ = sender.try_send(playback);
        }
    }
}

impl Drop for AudioService {
    fn drop(&mut self) {
        self.sender.take();
        if let Some(thread) = self.thread.take()
            && thread.is_finished()
        {
            let _ = thread.join();
        }
    }
}

pub fn toggle_system_mute() -> Result<bool, String> {
    let volume = system_volume()?;
    let muted = unsafe { volume.GetMute() }
        .map_err(|error| error.to_string())?
        .as_bool();
    unsafe { volume.SetMute(!muted, std::ptr::null()) }.map_err(|error| error.to_string())?;
    Ok(!muted)
}

pub fn system_mute() -> Result<bool, String> {
    let volume = system_volume()?;
    unsafe { volume.GetMute() }
        .map(|value| value.as_bool())
        .map_err(|error| error.to_string())
}

fn system_volume() -> Result<IAudioEndpointVolume, String> {
    unsafe {
        let _ = CoInitializeEx(None, COINIT_APARTMENTTHREADED);
        let enumerator: IMMDeviceEnumerator =
            CoCreateInstance(&MMDeviceEnumerator, None, CLSCTX_ALL)
                .map_err(|error| error.to_string())?;
        let device = enumerator
            .GetDefaultAudioEndpoint(eRender, eMultimedia)
            .map_err(|error| error.to_string())?;
        let volume: IAudioEndpointVolume = device
            .Activate(CLSCTX_ALL, None)
            .map_err(|error| error.to_string())?;
        Ok(volume)
    }
}

fn play_sound(path: &str) {
    if !Path::new(path).is_file() {
        eprintln!("prompt sound file does not exist: {path}");
        return;
    }
    let path = path.replace('"', "\"\"");
    let open = wide(&format!("open \"{path}\" alias FlyPPTTimerAlert"));
    let play = wide("play FlyPPTTimerAlert wait");
    let close = wide("close FlyPPTTimerAlert");
    unsafe {
        let _ = mci_send_string(open.as_ptr(), std::ptr::null_mut(), 0, 0);
        let result = mci_send_string(play.as_ptr(), std::ptr::null_mut(), 0, 0);
        let _ = mci_send_string(close.as_ptr(), std::ptr::null_mut(), 0, 0);
        if result != 0 {
            play_wmp_fallback(path.as_str(), result);
        }
    }
}

fn play_wmp_fallback(path: &str, mci_error: u32) {
    const SCRIPT: &str = r#"$player = New-Object -ComObject WMPlayer.OCX
$player.settings.autoStart = $false
$player.URL = $args[0]
$player.controls.play()
while ($player.playState -notin 1, 8, 10) { Start-Sleep -Milliseconds 50 }
"#;
    let result =
        std::process::Command::new(r"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe")
            .args([
                "-NoProfile",
                "-NonInteractive",
                "-WindowStyle",
                "Hidden",
                "-Command",
                SCRIPT,
                path,
            ])
            .status();
    if !result.is_ok_and(|status| status.success()) {
        eprintln!("failed to play prompt sound: {path} (MCI error {mci_error})");
    }
}

#[link(name = "winmm")]
unsafe extern "system" {
    #[link_name = "mciSendStringW"]
    fn mci_send_string(
        command: *const u16,
        return_text: *mut u16,
        return_length: u32,
        callback: usize,
    ) -> u32;
}

fn wide(value: &str) -> Vec<u16> {
    value.encode_utf16().chain(Some(0)).collect()
}
