# FlyPPTTimer 4.0 User Guide (English)

> For version: `4.0.0-alpha.1` (working branch `agent/v4-foundation`)
> This is the preview guide for the 4.0 rebuild, covering the WPF single-entry experience. Behavior is parity-equivalent to v0.30.2.

## 1. What changed in 4.0

- **WPF is the only official entry point.** The regular timer, big-screen timer, remote desktop client, settings, and the "time's up" screen are all handled by WPF. The legacy WinForms UI is no longer the official entry.
- **Two executables ship in the same folder:**
  - `FlyPPTTimer.exe`: the main app (tray, timer, presentation integration, remote listener).
  - `FlyPPTTimer.Settings.exe`: a standalone WPF settings window, opened from the tray menu or the settings entry.
- **Settings apply on save.** Clicking "Save and close" in the settings window writes the configuration once and the main app reloads and applies it automatically — no manual timer restart needed.
- **"Time's up" full-screen** is now a WPF window with proper multi-screen close/release.
- **Classic settings fallback removed.** 4.0 no longer keeps the old WinForms settings window as a compatibility entry; the WPF settings fully cover the configuration.

## 2. Start and tray

1. Run `FlyPPTTimer.exe`.
2. The FlyPPTTimer icon appears in the system tray.
3. Right-click the tray icon to open the menu:
   - **Settings**: opens the WPF settings window (`FlyPPTTimer.Settings.exe`).
   - **Remote control**: opens the WPF remote desktop window (address, QR code, presentation control, disconnect devices).
   - Quick actions: **Start/Pause**, **Stop and reset**, **Show/Hide timer window**.
   - **Exit**.

## 3. Settings window (WPF)

The settings window has six pages. "Save and close" in the bottom-right writes all changes at once and makes the main app reload; unsaved work can be discarded with "Cancel". The window footer shows unsaved state and validation errors.

### Timer
- Default duration, timer mode (countdown / count-up), continue past zero, end action (stop / black screen / end slideshow / custom).
- Timer window width.
- Auto behavior: start on fullscreen, stop when leaving fullscreen, reset when leaving fullscreen, flash on pause/resume, flash paused time.

### File rules
- Per PowerPoint / WPS presentation: independent duration, timer mode, and enable state.
- Add file, delete selected, clear.
- Batch edit: select multiple rules, set batch duration / batch mode, apply to selected.
- Only one rule per path is kept.

### Alerts & sound
- Three reminders: Prompt 1, Prompt 2, and End prompt. Each can enable, speak, trigger-before-seconds, sound file, flash style, and rhythm.
- Overtime prefix text, overtime text color, overtime background color.

### Appearance & display
- Show timer window, always on top, borderless.
- Color scheme, shape, text color, background color, flash background.
- Font size, width, height, background opacity, text opacity.
- Multi-screen: show on all screens, or a specific single screen; big-screen timer (extended screens only, selectable target).
- Default anchor (nine-grid), horizontal / vertical percentage fine-tune.

### Hotkeys
- Click-through, lock window, minimize to tray, close-button behavior.
- All global hotkeys can be viewed and edited; duplicate keys are rejected with a message.

### Remote & other
- Remote control: enable, use random port, port, regenerate access token (token is never shown in the UI).
- Language: Simplified Chinese / English / Follow system (applies fully after the main app restarts).
- Check for updates on startup (off by default).
- Import / export / reset config, open config folder, open log folder.

## 4. Phone or browser remote control

1. Open "Remote control" from the tray menu.
2. Put the phone and PC on the same LAN (Wi-Fi / Ethernet / hotspot).
3. Scan the QR code in the window, or open the LAN address in a phone, tablet, or another PC browser.
4. Control timer, duration, timer mode, window visibility, flash, and PC mute; re-time per the current presentation rule.
5. Browse presentations, start slideshow, change slides, black/white screen, end slideshow, close current document.
6. "Disconnect all devices" rotates the access token and invalidates old links.

> Remote control is for trusted LANs only and is verified by an access token. Do not map the port to the public internet, and do not share a valid QR code, full remote address, or token.

## 5. Big-screen timer

1. Connect an extended display.
2. Settings → Appearance & display → enable the big-screen timer and pick the extended screen.
3. The big-screen window is a resizable standard window; move, scale, minimize, or maximize it. It only uses extended screens and never the primary.

## 6. Configuration and migration

- Config file: `FlyPPTTimer.config.json` (next to `FlyPPTTimer.exe`).
- Upgrading from v0.30.2: on first launch an idempotent migration runs by `SchemaVersion`, preserving user fields such as fonts, prompt text, sounds, and per-page rules; unknown fields round-trip.
- Custom sounds live in `alert-sounds/` and are not removed by upgrades.
- Config is written atomically with timestamped backups; "Reset" in the settings window restores factory defaults.

## 7. Hotkey quick reference

| Key | Action |
|---|---|
| `F3` | Start or pause |
| `F4` | Stop and reset |
| `F5` | Show / hide the regular timer window |

The full hotkey list is viewable and editable in Settings → Hotkeys.

## 8. Alpha notes

- 4.0.0-alpha.1 is a rebuild preview; no formal Release / Tag yet.
- Behavior is parity-equivalent to v0.30.2, but test fully on the target device before important events: presentation integration, display position, sound, phone connection, and multi-screen.
- If the preview does not fit, you can keep using the stable v0.30.2.

## 9. Local file locations

- `FlyPPTTimer.config.json`: settings and rules.
- `logs/`: runtime and error logs.
- `alert-sounds/`: local copies of custom alert sounds.

## 10. Feedback

- Issues: [GitHub Issues](https://github.com/Hona-Cao/FlyPPTTimer/issues)
- Changelog: [CHANGELOG.md](../CHANGELOG.md)
- Rebuild progress: [V4_PROGRESS.md](V4_PROGRESS.md)
- Behavior parity matrix: [V0302_PARITY_MATRIX.md](V0302_PARITY_MATRIX.md)
