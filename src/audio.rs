use std::{
    path::Path,
    sync::mpsc::{self, SyncSender},
    thread,
    time::{Duration, Instant},
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
        System::{
            Com::{
                CLSCTX_ALL, CLSCTX_INPROC_SERVER, CLSIDFromProgID, COINIT_APARTMENTTHREADED,
                CoCreateInstance, CoInitializeEx, DISPATCH_FLAGS, DISPATCH_METHOD,
                DISPATCH_PROPERTYGET, DISPATCH_PROPERTYPUT, DISPPARAMS, IDispatch,
            },
            Variant::VARIANT,
        },
    },
    core::{GUID, PCWSTR},
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
        } else {
            open_result
        }
    };
    if result != 0
        && let Err(error) = play_wmp_native(path)
    {
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
        player_invoke(
            &settings,
            "autoStart",
            DISPATCH_PROPERTYPUT,
            &[VARIANT::from(false)],
        )?;
        player_invoke(&player, "URL", DISPATCH_PROPERTYPUT, &[VARIANT::from(path)])?;
        let controls = player_object(&player, "controls")?;
        player_invoke(&controls, "play", DISPATCH_METHOD, &[])?;
        let started_at = Instant::now();
        let mut started = false;
        loop {
            pump_audio_messages();
            let value = player_invoke(&player, "playState", DISPATCH_PROPERTYGET, &[])?;
            let state = i32::try_from(&value).map_err(|e| e.to_string())?;
            if state == 8 || (started && matches!(state, 1 | 10)) {
                return Ok(());
            }
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
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        DispatchMessageW, MSG, PM_REMOVE, PeekMessageW, TranslateMessage,
    };
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

fn player_invoke(
    object: &IDispatch,
    name: &str,
    flags: DISPATCH_FLAGS,
    args: &[VARIANT],
) -> Result<VARIANT, String> {
    let name = wide(name);
    let name_ptr = PCWSTR(name.as_ptr());
    let mut id = 0;
    unsafe { object.GetIDsOfNames(&GUID::zeroed(), &name_ptr, 1, 0, &mut id) }
        .map_err(|e| e.to_string())?;
    let mut reversed: Vec<_> = args.iter().rev().cloned().collect();
    let property_put = flags == DISPATCH_PROPERTYPUT;
    let mut named_id = -3;
    let params = DISPPARAMS {
        rgvarg: if reversed.is_empty() {
            std::ptr::null_mut()
        } else {
            reversed.as_mut_ptr()
        },
        rgdispidNamedArgs: if property_put {
            &mut named_id
        } else {
            std::ptr::null_mut()
        },
        cArgs: reversed.len() as u32,
        cNamedArgs: u32::from(property_put),
    };
    let mut value = VARIANT::default();
    unsafe {
        object.Invoke(
            id,
            &GUID::zeroed(),
            0,
            flags,
            &params,
            Some(&mut value),
            None,
            None,
        )
    }
    .map_err(|e| e.to_string())?;
    Ok(value)
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
