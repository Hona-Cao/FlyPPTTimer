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

const MAX_PROMPT_SOUND_DURATION: Duration = Duration::from_secs(10);

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
                                if let Err(error) = unsafe {
                                    voice.Speak(PCWSTR(text.as_ptr()), SPF_DEFAULT.0 as u32, None)
                                } {
                                    crate::log::error(&format!("Speech playback failed: {error}"));
                                }
                            } else {
                                crate::log::error(
                                    "Speech playback unavailable: SAPI voice could not be created",
                                );
                            }
                        }
                        Playback::Sound(path) => {
                            if let Err(error) = play_sound(&path) {
                                crate::log::error(&error);
                            }
                        }
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
        if let Some(sender) = &self.sender
            && let Err(error) = sender.try_send(playback)
        {
            crate::log::error(&format!("Audio queue rejected alert: {error}"));
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

fn play_sound(path: &str) -> Result<(), String> {
    if !Path::new(path).is_file() {
        return Err(format!("Prompt sound file does not exist: {path}"));
    }
    let command_path = path.replace('"', "\"\"");
    let open = wide(&format!("open \"{command_path}\" alias FlyPPTTimerAlert"));
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
                    let end = mode
                        .iter()
                        .position(|value| *value == 0)
                        .unwrap_or(mode.len());
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
        let mut playback_started_at = None;
        let mut last_progress = Instant::now();
        let mut last_position = 0.0;
        loop {
            pump_audio_messages();
            let value = player_invoke(&player, "playState", DISPATCH_PROPERTYGET, &[])?;
            let state = i32::try_from(&value).map_err(|e| e.to_string())?;
            if state == 8 || (started && matches!(state, 1 | 10)) {
                return Ok(());
            }
            if matches!(state, 3..=5) && !started {
                started = true;
                playback_started_at = Some(Instant::now());
            }
            if playback_started_at
                .is_some_and(|started| started.elapsed() >= MAX_PROMPT_SOUND_DURATION)
            {
                return Ok(());
            }
            if started {
                let position =
                    player_invoke(&controls, "currentPosition", DISPATCH_PROPERTYGET, &[])?;
                let position = f64::try_from(&position).map_err(|error| error.to_string())?;
                if position > last_position {
                    last_position = position;
                    last_progress = Instant::now();
                } else if last_progress.elapsed() >= Duration::from_secs(30) {
                    return Err("native media playback stalled for 30 seconds".into());
                }
            }
            // Bound startup separately; audible prompt playback is capped above.
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

#[cfg(test)]
mod native_feedback_tests {
    use super::*;

    #[test]
    #[ignore = "plays generated audio through the real Windows endpoint; set FLYPPT_AUDIO_TEST_DIR"]
    fn native_formats_repeat_fallback_speech_and_mute_restore() {
        let directory =
            std::env::var("FLYPPT_AUDIO_TEST_DIR").expect("temporary audio directory required");
        unsafe { CoInitializeEx(None, COINIT_APARTMENTTHREADED) }
            .ok()
            .unwrap();
        for extension in ["wav", "mp3", "wma", "m4a"] {
            let path = Path::new(&directory).join(format!("中文 提醒.{extension}"));
            let path = path.to_str().unwrap();
            for _ in 0..2 {
                play_sound(path).unwrap();
            }
            play_wmp_native(path).unwrap();
            println!("{extension}: first/repeat and direct native fallback completed");
        }
        assert!(play_sound(&Path::new(&directory).join("missing.wav").to_string_lossy()).is_err());
        let voice: ISpVoice = unsafe { CoCreateInstance(&SpVoice, None, CLSCTX_ALL) }.unwrap();
        let text = wide("音频测试。Audio test.");
        unsafe { voice.Speak(PCWSTR(text.as_ptr()), SPF_DEFAULT.0 as u32, None) }.unwrap();
        let original = system_mute().unwrap();
        let toggled = toggle_system_mute();
        // Always attempt restoration, even if querying after SetMute failed.
        let endpoint = system_volume().unwrap();
        unsafe { endpoint.SetMute(original, std::ptr::null()) }.unwrap();
        assert_eq!(toggled.unwrap(), !original);
        assert_eq!(system_mute().unwrap(), original);
    }
}
