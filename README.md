# FlyPPTTimer — PowerPoint & WPS Presentation Timer / PPT 计时器

**English** · [简体中文](README.zh-CN.md) · [User guide](docs/USER_GUIDE.en.md) · [中文详细教程](docs/USER_GUIDE.zh-CN.md)

<p align="center">
  <img src="src/FlyPPTTimer/Assets/app.png" width="88" alt="FlyPPTTimer presentation timer application icon">
</p>

**A free, open-source Windows PPT timer with a floating countdown, slide numbers, phone remote control and multi-monitor output.** Use it for conference talks, thesis defenses, classroom teaching, clinical presentations, training and meetings—without adding a countdown to every PowerPoint slide.

[![Latest release](https://img.shields.io/github/v/release/Hona-Cao/FlyPPTTimer?sort=semver)](https://github.com/Hona-Cao/FlyPPTTimer/releases/latest)
[![Windows CI](https://github.com/Hona-Cao/FlyPPTTimer/actions/workflows/windows-ci.yml/badge.svg)](https://github.com/Hona-Cao/FlyPPTTimer/actions/workflows/windows-ci.yml)
[![Windows 10 / 11 x64](https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-blue)](#download-v1131)
[![Rust + Slint](https://img.shields.io/badge/Built%20with-Rust%20%2B%20Slint-orange)](docs/BUILDING.md)
[![MIT license](https://img.shields.io/badge/License-MIT-green)](LICENSE)

**Current version: v1.13.1.** The desktop app has been rebuilt in Rust + Slint; the old v0.30.2 .NET instructions and screenshots are not the instructions for this version. [What changed](CHANGELOG.md) · [Development history, including UX and RC builds](docs/DEVELOPMENT_HISTORY.md)

## Download v1.13.1

| Package | Download | How to use |
|---|---|---|
| Portable ZIP | [FlyPPTTimer-v1.13.1-portable-win-x64.zip](https://github.com/Hona-Cao/FlyPPTTimer/releases/download/v1.13.1/FlyPPTTimer-v1.13.1-portable-win-x64.zip) | Extract the entire ZIP to a writable folder, then run `FlyPPTTimer.exe`. Keep the DLLs beside it. |
| Installer ZIP | [FlyPPTTimer-v1.13.1-setup-win-x64.zip](https://github.com/Hona-Cao/FlyPPTTimer/releases/download/v1.13.1/FlyPPTTimer-v1.13.1-setup-win-x64.zip) | Extract it, run the included setup EXE, choose English or Simplified Chinese, and follow the wizard. |

[Release page](https://github.com/Hona-Cao/FlyPPTTimer/releases/tag/v1.13.1) · [All releases](https://github.com/Hona-Cao/FlyPPTTimer/releases)

Windows 10/11 x64 is the target platform. Both packages include the required application-local Microsoft VC runtime DLLs; **.NET is not required** for v1.13.1. Microsoft PowerPoint or WPS Presentation must be installed on the PC for supported presentation-control functions; the independent timer does not require Office. Your phone only needs a browser on the same trusted local network.

The release provides exactly two uploaded packages: portable ZIP and installer ZIP. GitHub also displays its automatically generated source-code archives.

## Start in three minutes

1. **Start the timer.** Run the app. Its small floating timer appears immediately. Press **F3** to start/pause and **F4** to stop and reset.
2. **Set the duration.** Right-click the timer or its notification-area icon → **Settings → Timer**. Enter `00:08:00` for eight minutes, choose countdown or count-up, and click **Apply**.
3. **Add your slides.** In the same page, click **Add file**, select `.ppt`, `.pptx` or `.pptm` files, set a duration/mode per file, and save. Files are identified by full path; adding a rule does not alter the slide deck.
4. **Connect a phone.** Right-click → **Remote Control**. Keep the PC and phone on the same Wi-Fi, or connect the PC to the phone's hotspot. Scan the displayed QR code. Do not replace the displayed PC address with the hotspot gateway.
5. **Present.** On the phone, open **Presentation**, choose a controlled file and tap **Open**, then **Start from beginning** or **Start from current slide**. Automatic timing on entering full screen is enabled by default.

For timing only, steps 3–5 are optional. Close a settings window with **OK**; use the tray's **Exit** command to quit the application.

## See the current interface

### Desktop: settings and precise percentage controls

![FlyPPTTimer v1.13.1 desktop timer settings with countdown, time-up actions and presentation rules](docs/media/v1.13.1/settings-en-timer.png)

![FlyPPTTimer v1.13.1 dark appearance settings with opacity slider and editable percentage](docs/media/v1.13.1/settings-en-dark-opacity.png)

### PC Remote: QR connection and compact file rules

![FlyPPTTimer v1.13.1 PC Remote local-network connection screen](docs/media/v1.13.1/pc-remote-connection.png)

![FlyPPTTimer v1.13.1 compact presentation rules with aligned file, duration and mode columns](docs/media/v1.13.1/pc-remote-rules.png)

### Phone: timer and PowerPoint controls

<p>
  <img src="docs/media/v1.13.1/mobile-en-light-timer.png" width="310" alt="FlyPPTTimer phone browser countdown timer, duration editor, pause, reset and mute controls">
  <img src="docs/media/v1.13.1/mobile-en-dark-presentation.png" width="310" alt="FlyPPTTimer dark phone browser presentation list, sorting, slide navigation and PowerPoint controls">
</p>

These are renders of the v1.13.1 GUI, not concept drawings. Desktop images use the production UI and callbacks in a disposable documentation build with an explicit CJK font to correct missing glyphs on the build host; that build is never shipped. Mobile images use the unchanged HTML/CSS/JavaScript at a phone-sized viewport. Document names, paths and connection data are examples. [Screenshot provenance and reproduction](docs/media/v1.13.1/README.md)

## What you can do

| Need | Feature and entry point |
|---|---|
| Keep a talk on time | Countdown/count-up, start/pause/resume, stop/reset, restart and overtime colors. See [timer controls](docs/USER_GUIDE.en.md#timer-controls). |
| Time different speakers | Presentation-specific duration, mode and enabled state; Ctrl/Shift selection and batch changes. See [file rules](docs/USER_GUIDE.en.md#presentation-rules). |
| See progress without looking away | Always-on-top floating timer, current/total slide numbers, independent page font, colors, opacity, shape and positioning. See [appearance](docs/USER_GUIDE.en.md#appearance-and-percentage-controls). |
| Receive advance warnings | Two configurable pre-end reminders and a time-up reminder; speech, imported audio, flashing, a full-screen time-up cover or ending the slide show. See [alerts](docs/USER_GUIDE.en.md#alerts-and-time-up-actions). |
| Control slides from a phone | Browser-based PowerPoint/WPS remote: open/switch controlled decks, start shows, previous/next, jump to slide, black/white screen and end show. See [phone control](docs/USER_GUIDE.en.md#phone-and-browser-remote). |
| Organize a session | Name/size/date sorting, long-press reordering, hide/restore and removal from the controlled list. See [mobile list](docs/USER_GUIDE.en.md#organize-the-mobile-presentation-list). |
| Use a projector or confidence monitor | All-screen or selected-screen overlays plus a dedicated large timer on an extended display. See [multiple displays](docs/USER_GUIDE.en.md#multiple-displays). |
| Match the room | System/light/dark theme on desktop and a local theme choice on the phone. See [themes and language](docs/USER_GUIDE.en.md#themes-and-language). |
| Keep preferences | Local configuration import/export, reset, logs and preserved settings on upgrade. See [configuration and upgrading](docs/USER_GUIDE.en.md#configuration-and-upgrading). |

**PC Remote and phone Remote have different roles.** The PC's **Presentation** page manages file rules; it is not a duplicate slide-control panel. Open, previous/next, black/white screen and close-document actions are on the phone/browser page.

## Defaults worth knowing

New configurations use an **eight-minute countdown**. Slide numbers are on, with an independent **12-point size**, **below** the time and **right-aligned**. The timer sizes itself to its content; custom size remains available. Theme follows the system.

**Every app launch shows the timer.** Hiding it only affects the current session. F5 toggles it again. Existing explicit appearance and timing preferences are preserved when a configuration is reused.

The three main shortcut selectors are in **Settings → Controls**:

| Shortcut | Default action |
|---|---|
| F3 | Start/pause; resume a paused timer |
| F4 | Stop and reset |
| F5 | Show/hide the ordinary timer |

Additional default shortcuts, including flash, mute and duration presets, are listed in the [guide](docs/USER_GUIDE.en.md#keyboard-shortcuts).

## Common questions

**Is this a PowerPoint add-in?** No. It is a separate Windows application. You do not need to insert timer objects or macros into every slide.

**Can I use it without PowerPoint?** Yes, as an independent countdown/count-up timer. Supported slide detection and controls require a compatible desktop PowerPoint/WPS installation. Browser slides or PDF viewers are not added as controlled PPT files.

**My phone cannot connect.** Use the current QR code, make sure both devices are on the same network, and check guest-network isolation, VPN/proxy routing and Windows firewall permission for the displayed port. Do not disable the firewall. [Step-by-step troubleshooting](docs/USER_GUIDE.en.md#troubleshooting)

**Will removing a file delete it?** No. Removing it revokes its membership in FlyPPTTimer's controlled list. It does not delete the disk file or close an open document. Closing a document is a separate action with a warning about unsaved work.

**Does a slider save as I drag?** It previews while dragging; the wheel changes it by one percentage point and the adjacent number accepts precise input. **Apply/OK saves; Cancel discards**. Finishing text entry is not the same as saving.

**Why is the file picker light when the app is dark?** Windows-owned dialogs use the operating system's theme. The app theme does not replace those dialogs.

**How do I upgrade from the older version?** Exit the old program and back up its configuration and `alert-sounds` folder. The installer preserves existing configuration; portable users should copy their configuration/sounds into the newly extracted folder. Old file paths must still be valid. [Full upgrade instructions](docs/USER_GUIDE.en.md#configuration-and-upgrading)

**Does “Check for updates” check this GitHub release?** In the accepted v1.13.1 executable it still checks **Gitee**. GitHub publishing does not imply a matching Gitee release. Use the GitHub download links above for this release; this installer ZIP is installed manually after extraction.

## Privacy and sensible use

Timer operation and LAN remote control require no cloud account. Configuration, rules, logs and imported sounds remain local; the app does not upload presentation content. Update checks and project links need internet access.

The Remote URL contains an access token. Use a **trusted local network**, keep the URL/QR private, and do not forward the control port to the public internet. Before posting logs, check them for local file paths. Close/quit presentation commands may discard unsaved work; save in Office first.

## Development has continued

The v1.13.1 release brings the real V1 development history onto `main`, including the UX iterations, RC1/RC2 fixes, RC3.1, RC3.2, RC3.3, RC3.3.1, RC3.4 and final v1.13.1 changes. Original commit authors and dates are retained.

[Readable development timeline](docs/DEVELOPMENT_HISTORY.md) · [Full changelog](CHANGELOG.md) · [Original implementation reports](docs/v1/CODEX_RESULT.md)

The accepted application build passed **90 automated tests**, with **3 environment-dependent tests ignored**. The ignored Office/audio tests are not counted as passes. Interface renders are not a substitute for physical device testing; the maintainer has approved v1.13.1 for publication after use.

## Build, contribute and contact

The current application uses **Rust 1.92.0, Slint 1.17.1 and the Windows MSVC toolchain**. The retained C# files describe the older implementation; do not use the old .NET packaging commands for V1.

```powershell
rustup toolchain install 1.92.0 --profile minimal --component rustfmt,clippy
cargo +1.92.0 test --locked
cargo +1.92.0 build --release --locked
```

See [complete build/package instructions](docs/BUILDING.md), [CONTRIBUTING.md](CONTRIBUTING.md) and [GitHub Issues](https://github.com/Hona-Cao/FlyPPTTimer/issues). When reporting a bug, include version, Windows/Office version, display scaling, reproduction steps and a redacted screenshot.

Created by **Hunan Cao (曹虎男)**. Contact: [caohunan@smail.nju.edu.cn](mailto:caohunan@smail.nju.edu.cn).

## Support the project

Stars, useful issue reports, documentation and contributions are welcome. Donations are optional and do not unlock features or change the free software.

<p>
  <img src="docs/media/donate-alipay.jpg" width="220" alt="Optional Alipay donation to the FlyPPTTimer author">
  <img src="docs/media/donate-wechat.png" width="220" alt="Optional WeChat donation to the FlyPPTTimer author">
</p>

## License

[MIT License](LICENSE). Copyright © 2026 Cao Hunan. Third-party components retain their respective licenses.
