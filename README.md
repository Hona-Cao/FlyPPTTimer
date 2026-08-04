# FlyPPTTimer

**English** | [简体中文](README.zh-CN.md)

<p align="center">
  <img src="src/FlyPPTTimer/Assets/app.png" width="88" alt="FlyPPTTimer logo">
</p>

<p align="center">
  <strong>A presentation timer and remote-control toolkit for Windows</strong><br>
  PowerPoint / WPS · Phone and browser remote · Countdown and count-up · Multi-display timer
</p>

<p align="center">
  <a href="https://github.com/Hona-Cao/FlyPPTTimer/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/Hona-Cao/FlyPPTTimer?display_name=tag&sort=semver"></a>
  <a href="https://github.com/Hona-Cao/FlyPPTTimer/actions/workflows/windows-ci.yml"><img alt="Windows CI" src="https://github.com/Hona-Cao/FlyPPTTimer/actions/workflows/windows-ci.yml/badge.svg"></a>
  <img alt="Windows 10/11" src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows">
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet">
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/License-MIT-green.svg"></a>
</p>

FlyPPTTimer combines an always-on-top timer, presentation-specific timing rules, local-network remote control, alerts, and multi-display output in one desktop application. It is free, open source, and works without a cloud account.

The latest release is **v0.30.2**. See [CHANGELOG.md](CHANGELOG.md) for the version history.

## Where it fits

- Talks, conferences, defenses, and public speaking
- Classroom teaching, training, and workshops
- Meetings, ceremonies, debates, and timed activities
- Medical, nursing, and clinical case presentations
- Interviews, recruitment sessions, and assessment rooms using a dedicated large-screen timer
- Events where an assistant needs to control timing or slides from a phone

## Download

| Edition | Best for | GitHub | Gitee |
|---|---|---|---|
| Installer | Regular Windows installation, shortcuts, and future in-app updates | [Download v0.30.2 installer](https://github.com/Hona-Cao/FlyPPTTimer/releases/download/v0.30.2/FlyPPTTimer-v0.30.2-setup-win-x64.exe) | [Gitee v0.30.2 release](https://gitee.com/hona-cao/fly-ppttimer/releases/tag/v0.30.2) |
| Portable | Extract and run; settings remain beside the application | [Download v0.30.2 portable ZIP](https://github.com/Hona-Cao/FlyPPTTimer/releases/download/v0.30.2/FlyPPTTimer-v0.30.2-portable-win-x64.zip) | [Gitee v0.30.2 release](https://gitee.com/hona-cao/fly-ppttimer/releases/tag/v0.30.2) |

[All GitHub releases](https://github.com/Hona-Cao/FlyPPTTimer/releases) · [All Gitee releases](https://gitee.com/hona-cao/fly-ppttimer/releases)

The current packages support Windows 10/11 x64 and include the required .NET runtime.

- The installer detects the Windows display language and lets you choose English or Simplified Chinese before installation.
- The portable edition follows the Windows display language on first launch.
- Upgrading the installer edition preserves the existing configuration.

## Screenshots

<p align="center">
  <img src="docs/media/presentation-timer-overlay.webp" alt="Timer overlay during a presentation" width="100%">
</p>

The timer remains visible over the presentation so the speaker and event staff can track the allotted time without interrupting the slide show.

<p align="center">
  <img src="docs/media/interface-overview.webp" alt="FlyPPTTimer interface overview" width="100%">
</p>

The overview contains all six Settings pages and both desktop remote-control pages. Sensitive file paths, access tokens, QR codes, presentation names, and organization details have been removed or blurred.

| Page | What it configures |
|---|---|
| **Duration Settings** | Default duration, count-up or countdown mode, behavior after zero, and independent rules for multiple PowerPoint or WPS files. |
| **Behavior Settings** | Automatic start, stop, and reset around full-screen presentations, plus two advance alerts and the final time-up alert. |
| **Appearance & Display** | Timer visibility, colors, size, opacity, shape, multi-display position, and large-screen timing. |
| **Remote Control Settings** | Local browser remote service, port and LAN address, access token, and connected-device management. |
| **Control Settings** | Global hotkeys, mouse click-through, window locking, tray behavior, and closing behavior. |
| **Other Settings** | Language, update checks, configuration import/export, reset, local paths, version information, and diagnostics. |
| **Remote Connection** | Service state, QR code, and browser address. A phone can connect over the same LAN or a phone hotspot without installing an app. |
| **Presentation Control** | Multiple presentation files and their timer rules, plus open, start, end, and close operations from off stage. |

<p align="center">
  <img src="docs/media/mobile-timer.jpg" width="310" alt="Mobile timer controls">
  &nbsp;&nbsp;
  <img src="docs/media/mobile-presentation.jpg" width="310" alt="Mobile presentation controls">
</p>

## Features

### Timer and alerts

- Countdown and count-up modes
- Start, pause, resume, stop, reset, and immediate restart
- Configurable duration presets and presentation-specific durations
- Continue into overtime with a separate color, or stop at zero
- Two advance alerts and one time-up alert
- Optional speech, custom audio, flashing, full-screen time-up display, or automatic slide-show ending
- Compact always-on-top timer with configurable size, font, colors, opacity, and shape
- A switch to run timing tasks without showing the regular timer window

### PowerPoint, WPS, and presentation rules

- Automatic timer behavior when a supported presentation enters or leaves full screen
- Independent duration, timer mode, and enabled state for each presentation
- Batch editing for multiple presentation rules
- Open a presentation, start from the beginning or current slide, navigate slides, show black/white screens, and end the slide show
- Separate actions for closing the current presentation and the last-opened presentation
- Read-only opening for presentations managed by FlyPPTTimer
- Capability detection for WPS Presentation, so unavailable actions remain disabled

### Phone and browser remote

- No mobile app is required
- Scan the QR code or open the local address from a phone, tablet, or another computer
- Control the timer, duration, mode, visibility, flashing, and computer mute state
- Restart timing immediately from the current presentation rule, falling back to the global duration
- Browse presentations, start a slide show, change slides, use black/white screens, end the show, and close the current document
- Automatic phone/browser language based on the device language
- Per-installation access token and a command to disconnect all remote devices

### Displays and large-screen timing

- Show the regular timer on one display or all displays
- Nine anchor positions with horizontal and vertical percentage adjustment
- Per-monitor DPI support for common Windows scaling levels
- A separate resizable large-screen timer window with minimize and maximize controls
- Large-screen timing is available on extended displays and never takes over the primary display
- Useful for interviews, recruitment, examinations, training rooms, and stage countdowns

### Desktop controls and reliability

- English, Simplified Chinese, and **Follow system**
- Language changes take effect after restart without overwriting timing rules
- Global hotkeys for timer operations, visibility, flashing, mute, timer mode, and duration presets
- Responsive Settings and Remote Control windows for different sizes and display scaling
- Atomic configuration writes, backup recovery, rotating local logs, and single-instance operation
- Optional update checks; automatic checking is off by default

## Quick start

### Basic timing

1. Install FlyPPTTimer or extract the portable ZIP.
2. Run `FlyPPTTimer.exe`.
3. Right-click the timer or tray icon and open **Settings**.
4. Set the default duration, timer mode, alerts, colors, and display position.
5. Use `F3` to start or pause and `F4` to stop and reset.

### Use a presentation rule

1. Open **Settings → Duration Settings**.
2. Add a PowerPoint or WPS presentation.
3. Set its duration and timer mode, then enable the rule.
4. Open or start the presentation from the Remote Control window, or start it normally in PowerPoint/WPS.

### Control from a phone

1. Open **Remote Control** from the tray menu.
2. Keep the phone and computer on the same Wi-Fi, Ethernet LAN, or phone/computer hotspot.
3. Scan the QR code.
4. Use the browser page to control timing and presentations.

Windows may ask for network access the first time remote control is enabled. Allow private-network access only when remote control is needed.

### Use the large-screen timer

1. Connect an extended display.
2. Open **Settings → Appearance & Display → Large-screen timer mode**.
3. Enable the large-screen timer and choose an extended display.
4. Move, resize, minimize, or maximize the large-screen window as needed.

## Compatibility

| Capability | Microsoft PowerPoint | WPS Presentation | Other full-screen applications |
|---|---:|---:|---:|
| Automatic full-screen timer behavior | Supported | Top-level window detection | Optional allowlist detection |
| Open a presentation | Supported | Supported | Not applicable |
| Start from beginning/current slide | Supported | Depends on the available WPS interface | Not applicable |
| Navigation, jump, black/white screen | Supported | Depends on the available WPS interface | Not applicable |
| Read-only managed files and controlled closing | Supported | Provided when detected | Not applicable |

WPS capabilities differ between versions. FlyPPTTimer enables only the operations detected on the current computer.

## Local files

- `FlyPPTTimer.config.json` — settings and presentation rules
- `logs/` — local diagnostic logs
- `alert-sounds/` — copies of selected custom alert sounds

Installer upgrades keep the existing configuration. Before an important event, test the presentation, display placement, audio, and remote connection on the actual equipment.

## Default hotkeys

| Key | Action |
|---|---|
| `F3` | Start or pause |
| `F4` | Stop and reset |
| `F5` | Show or hide the regular timer |

Additional hotkeys can be viewed and changed in Settings.

## Privacy and network safety

- No cloud account is required.
- Presentation contents are not uploaded by FlyPPTTimer.
- Settings, rules, selected sounds, and logs remain on the local computer.
- Remote control is intended for a trusted local network and requires an access token.
- Do not forward the remote-control port to the public Internet.
- Do not publish an active QR code, full remote URL, or token.
- Check file paths and other local information before sharing logs or screenshots.

## Build from source

Requirements: Windows 10/11, PowerShell, and the .NET 8 SDK. Inno Setup 6 is required to build the installer.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\package_release.ps1
```

Run the test suite:

```powershell
dotnet test tests\FlyPPTTimer.Tests\FlyPPTTimer.Tests.csproj -c Release
```

## Project

FlyPPTTimer was created by **Hunan Cao (曹虎男)** after seeing the practical need for reliable timing and remote presentation control in teaching, meetings, and clinical work.

- Contact: [caohunan@smail.nju.edu.cn](mailto:caohunan@smail.nju.edu.cn)
- Bugs and feature requests: [GitHub Issues](https://github.com/Hona-Cao/FlyPPTTimer/issues)
- Contribution guide: [CONTRIBUTING.md](CONTRIBUTING.md)
- Version history: [CHANGELOG.md](CHANGELOG.md)

Stars, issue reports, testing feedback, documentation improvements, and pull requests are all welcome.

## Support

If FlyPPTTimer saves you preparation or stage-management time, you can support its continued testing and maintenance through the donation options in the [Chinese README](README.zh-CN.md). The application remains free and open source whether or not you donate.

## License

FlyPPTTimer is available under the [MIT License](LICENSE).

Copyright © 2026 Cao Hunan（曹虎男）
