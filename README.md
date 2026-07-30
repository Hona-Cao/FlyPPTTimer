# FlyPPTTimer

**English** | [简体中文](README.zh-CN.md)

<p align="center">
  <img src="src/FlyPPTTimer/Assets/app.png" width="88" alt="FlyPPTTimer logo">
</p>

<p align="center">
  <strong>A Windows presentation timer for talks, teaching, meetings, and clinical presentations</strong><br>
  PowerPoint / WPS integration · LAN phone remote · Countdown and count-up · Multi-display overlay
</p>

## Overview

FlyPPTTimer is a free, open-source Windows presentation timer that requires no cloud account. It combines a clear always-on-top timer, presentation-specific rules, time-up alerts, and local phone remote control in one application.

The current development version is **v0.30.2**.

## Download

The v0.30.2 test build is currently being validated and has not been published. Until it is approved, use the stable v0.20.2 packages:

| Edition | Best for | GitHub | Gitee mirror |
|---|---|---|---|
| Installer | Standard Windows installation | [Download v0.20.2 installer](https://github.com/Hona-Cao/FlyPPTTimer/releases/download/v0.20.2/FlyPPTTimer-v0.20.2-setup-win-x64.exe) | [Download from Gitee](https://gitee.com/hona-cao/fly-ppttimer/releases/download/v0.20.2/FlyPPTTimer-v0.20.2-setup-win-x64.exe) |
| Portable | Extract and run; configuration stays beside the app | [Download v0.20.2 portable ZIP](https://github.com/Hona-Cao/FlyPPTTimer/releases/download/v0.20.2/FlyPPTTimer-v0.20.2-portable-win-x64.zip) | [Download from Gitee](https://gitee.com/hona-cao/fly-ppttimer/releases/download/v0.20.2/FlyPPTTimer-v0.20.2-portable-win-x64.zip) |

[All GitHub releases](https://github.com/Hona-Cao/FlyPPTTimer/releases) · [Gitee releases](https://gitee.com/hona-cao/fly-ppttimer/releases)

Windows 10/11 x64 is currently supported. Packages are self-contained and do not require a separate .NET installation.

## What is new in v0.30.2

- Reduced the Settings selector corner radius and fully self-painted the closed selector surface so enabled and disabled controls have no native top/left edge.
- Replaced resize-time rounded-region rebuilding in Settings with one update at the end of a live resize.
- Deferred remote responsive reflow and presentation polling while the user is resizing the window.
- Restored the full lower corners of both remote navigation buttons while retaining the thin divider.
- Made the presentation list vertical-only; long names use ellipsis and can no longer create a horizontal scroll bar.

## What is new in v0.30.1

- Simplified the remote-control navigation: removed the navigation label and tinted container, and added one divider below the two module buttons.
- Removed the remaining native Windows border from both enabled and disabled Settings drop-downs.
- Increased the selector corner radius so the rounded shape is clearly visible.
- Removed the native border from the opened list window and applied rounded clipping to the complete list.

## What is new in v0.30.0

- The big-screen timer is now a standard resizable window with minimize and maximize controls.
- Big-screen mode is limited to extended displays; the primary display can no longer be selected.
- The big-screen display selector appears only when an extended display is connected and is disabled with its existing surface when the mode is off.
- Fixed a crash when opening Settings after the big-screen timer had disposed a shared WinForms font.
- Disabled fields now change the color of their original rounded surface instead of drawing a second mismatched layer.
- Settings drop-downs now use a borderless, shadow-free rounded style consistent with the main window.

## What is new in v0.20.9

- Placed the remote-control module buttons inside a dedicated navigation bar so they are clearly distinct from page actions.
- Consolidated the LAN guidance and documented phone/computer hotspots as supported ways to create the control network.
- The phone remote now follows the device language automatically; the manual language selector was removed.
- Reorganized Appearance & Display: the regular timer window comes first, and full-screen timer options have their own section.
- The single-display selector is disabled while **Show on all displays** is enabled.
- Reworked Settings drop-downs with centered content, a fully rounded surface, and responsive native wheel scrolling.

## What is new in v0.20.8

- Enlarged the four presentation toolbar buttons and added consistent spacing throughout the English presentation-details card.
- Stopped unchanged presentation titles from being reassigned during the one-second PowerPoint refresh, eliminating the **Not selected** flicker.
- Added a Settings switch for the regular timer window and a dedicated full-screen timer that can target a selected display.
- Unified rounded clipping for light-gray Settings controls.
- Added **Restart timer** to the phone remote. It immediately restarts from the selected presentation rule duration, falling back to the global duration.
- Added separate **Close current presentation** and **Close last-opened presentation** actions.

## What is new in v0.20.7

- Editable fields now use a light blue-tinted fill, clearly distinct from gray read-only and disabled controls.
- Presentation rows devote their full width to the file name and duration; per-row status and rule toggles were removed.
- The presentation toolbar now includes a larger **Clear list** action.
- The presentation feedback strip and quit-software card were removed.
- Top navigation buttons are narrower, farther apart, and use a borderless light-blue selected state.

## What is new in v0.20.6

- Recreated the bilingual remote-control window from the approved visual reference at a compact 700 × 510 logical client size.
- Matched the reference proportions for navigation, page headings, service status, QR/browser columns, presentation list, details, slide-show controls, and quit card.
- Added DPI-aware sizing for fixed layout rows so the composition remains visually consistent at Windows display scaling.
- Existing oversized remote-window placements are migrated to the new reference size.

## What is new in v0.20.5

- The remote-control window now uses two independent top navigation buttons instead of a left sidebar.
- Remote connection and presentation management were rebuilt with consistent spacing, aligned cards, clearer read-only and disabled states, and responsive local scrolling.
- Connection fields, presentation controls, and quit controls now use clearer bilingual wording and state-aware actions.
- The Settings navigation receives extra vertical room and inset painting so all four rounded corners remain visible.
- Layout calculations are covered at 100%, 125%, 150%, and 175% display scaling.

## What is new in v0.20.4

- The Settings navigation always stays on one line; the window expands automatically when required.
- The remote-connection header no longer exposes a manual port field and uses the default port `4080`; explicitly configured fixed ports are preserved.
- The browser address is selected automatically, so the obsolete address selector button has been removed.

## Language support introduced in v0.20.3

- English and Simplified Chinese desktop interfaces.
- A **Follow system** option, plus a manual language selector in Settings.
- The installer detects the Windows display language, selects it by default, and allows English or Simplified Chinese to be chosen before installation.
- The portable edition uses the Windows display language on first launch.
- The phone/browser remote follows the device language by default and has its own language selector.
- Existing timer settings and presentation rules are preserved when the language changes.

Language changes made inside the desktop app take effect after restarting FlyPPTTimer.

## Screenshots

| Timer and presentation rules | Appearance and display |
|---|---|
| <img src="docs/media/settings-duration.png" alt="Timer and presentation-rule settings" width="100%"> | <img src="docs/media/settings-appearance.png" alt="Appearance and display settings" width="100%"> |

<p align="center">
  <img src="docs/media/mobile-timer.jpg" width="310" alt="Mobile timer controls">
  <img src="docs/media/mobile-presentation.jpg" width="310" alt="Mobile presentation controls">
</p>

## Features

### Timer and alerts

- Countdown and count-up modes with start, pause, resume, stop, and reset.
- Configurable time-up behavior: alert only, stop at zero, continue into overtime, show a time-up blackout, or end the slide show.
- Two advance alerts and a time-up alert, each with optional speech, custom audio, and visual flashing.
- A compact, always-on-top timer overlay designed to stay readable over presentations.

### Presentation integration

- Automatically starts and stops with supported fullscreen presentations.
- Stores a separate duration, timer mode, and enabled state for each presentation file.
- Supports PowerPoint slide navigation, starting from the beginning or current slide, and ending a slide show.
- Detects available WPS Presentation capabilities and avoids presenting unsupported actions as reliable.

### Phone and browser remote

- No mobile app is required. Scan the QR code and use a browser on the same LAN.
- Adjust duration and timer mode, control the timer, mute the computer, and show or hide the overlay.
- Browse managed presentations, start a slide show, navigate slides, and use black/white screen controls.
- The remote service is local-only and uses a per-installation access token.

### Displays and controls

- Multi-monitor overlay support with nine anchor positions and percentage offsets.
- Configurable colors, opacity, shape, size, and overtime appearance.
- Global hotkeys for timer, visibility, flash, mute, timer mode, and duration presets.
- Atomic configuration saving with backup recovery.

## Quick start

1. Install FlyPPTTimer, or extract the portable ZIP.
2. Run `FlyPPTTimer.exe`.
3. Right-click the timer or tray icon and open **Settings**.
4. Set the duration, alerts, display position, and optional presentation rules.
5. For phone control, open **Remote Control**, scan the QR code, and keep both devices on the same trusted network.

Key local files:

- `FlyPPTTimer.config.json` — settings and presentation rules.
- `logs/` — local diagnostic logs.
- `alert-sounds/` — copies of user-selected alert sounds.

## Build from source

Requirements: Windows 10/11, PowerShell, and the .NET 8 SDK. Inno Setup 6 is also required to produce the installer.

```powershell
.\build.ps1
.\package_release.ps1
```

Run the tests:

```powershell
dotnet test tests\FlyPPTTimer.Tests\FlyPPTTimer.Tests.csproj -c Release
```

The release script creates versioned installer and portable artifacts with SHA-256 checksum files.

## Privacy and security

- FlyPPTTimer does not require a cloud account.
- Configuration, presentation rules, logs, and selected alert sounds remain local.
- The web remote binds to the local network and requires a token.
- Only enable firewall access on trusted private networks.
- Avoid publishing remote URLs or screenshots that contain the access token.

## Project and support

FlyPPTTimer was created by **Hunan Cao (曹虎男)**. Bug reports and contributions are welcome through [GitHub Issues](https://github.com/Hona-Cao/FlyPPTTimer/issues) and pull requests.

- [Changelog](CHANGELOG.md)
- [Contribution guide](CONTRIBUTING.md)
- License: [MIT](LICENSE)
- Contact: caohunan@smail.nju.edu.cn

## Support the project

If FlyPPTTimer is useful to you, a star, issue report, or pull request is greatly appreciated. Donation QR codes are available on the [Chinese README](README.zh-CN.md).
