<div align="center">

# FlyPPTTimer

### Presentation timing, without breaking your flow.

A Windows presentation timer and control companion for **Microsoft PowerPoint** and **WPS Presentation**.

[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows11&logoColor=white)](#compatibility)
[![Architecture](https://img.shields.io/badge/Architecture-x64-555555)](#compatibility)
[![Rust](https://img.shields.io/badge/Built%20with-Rust-000000?logo=rust&logoColor=white)](#technology)
[![Slint](https://img.shields.io/badge/UI-Slint-2379F4)](#technology)
[![Release](https://img.shields.io/github/v/release/Hona-Cao/FlyPPTTimer?label=Release)](https://github.com/Hona-Cao/FlyPPTTimer/releases/latest)
[![Stars](https://img.shields.io/github/stars/Hona-Cao/FlyPPTTimer?style=flat&logo=github)](https://github.com/Hona-Cao/FlyPPTTimer/stargazers)

**[Download](https://github.com/Hona-Cao/FlyPPTTimer/releases/latest)** · **[User Guide](docs/USER_GUIDE.en.md)** · **[简体中文](README.zh-CN.md)**

</div>

---

FlyPPTTimer is built for presentations where **time actually matters**: thesis defenses, speech contests, pitches, training sessions, classes, meetings, live events, and any talk that has to land on time.

Instead of juggling a generic timer, PowerPoint, a second display, and a phone, FlyPPTTimer brings the presentation workflow together: **countdown/count-up timing, slide awareness, per-slide timing, reminders, multi-display output, presentation-specific rules, reusable scenarios, and browser-based remote control**.

<table>
<tr>
<td width="62%"><img src="docs/media/readme/settings-scenarios-en.png" alt="FlyPPTTimer desktop settings"></td>
<td width="38%" align="center"><img src="docs/media/readme/mobile-timer-en.png" alt="FlyPPTTimer browser remote timer" width="310"></td>
</tr>
<tr>
<td align="center"><strong>Desktop workflow</strong></td>
<td align="center"><strong>Phone browser remote</strong></td>
</tr>
</table>

<div align="center"><strong>Timing, presentation context, displays and remote control in one workflow.</strong></div>

## Why FlyPPTTimer?

A normal timer tells you the time. FlyPPTTimer is designed around the **presentation itself**.

| | What it means in a live presentation |
|---|---|
| **Stay on time** | Countdown, count up, unlimited elapsed timing, advance reminders, time-up actions, and overtime display. |
| **Know your slide** | Show the current slide / total slides and track how long you have spent on the current slide. |
| **Work with PowerPoint & WPS** | Detect and control compatible desktop presentations instead of running as an isolated stopwatch. |
| **Use every display well** | Keep a compact overlay where the speaker needs it, or put a dedicated large timer on another screen. |
| **Control from your phone** | Use a browser on the same local network for timing and presentation controls—no phone app installation required. |
| **Reuse your workflow** | Give different decks their own timing rules and save complete setups as scenarios. |

## Built for real presentations

### Keep the speaker focused

FlyPPTTimer can stay compact and out of the way while keeping the essential information visible.

The main timer can run as a **countdown**, **count up**, or an **unlimited elapsed timer**. During a PowerPoint/WPS slideshow, the lower row can show the current slide's dwell time and slide position, so you can answer two questions instantly:

> How much time do I have left?  
> How long have I been on this slide?

### PowerPoint & WPS are part of the workflow

FlyPPTTimer can work as a standalone timer, but its presentation integration is where it becomes much more useful.

When a compatible slideshow is active, FlyPPTTimer can show slide information and provide presentation controls. Per-slide timing tracks the current visit while the browser interface can show accumulated timing for slides during the current presentation session.

<table>
<tr>
<td width="50%" align="center"><img src="docs/media/readme/mobile-presentation-en.png" alt="Presentation controls in the phone browser" width="330"></td>
<td width="50%" align="center"><img src="docs/media/readme/mobile-slide-times-en.png" alt="Per-slide timing in the phone browser" width="330"></td>
</tr>
<tr>
<td align="center"><strong>Presentation controls</strong></td>
<td align="center"><strong>Per-slide timing</strong></td>
</tr>
</table>

## Your phone becomes the presentation remote

Open FlyPPTTimer Remote on the computer, connect from a phone on the same trusted local network, and control the presentation from a browser.

No dedicated mobile app is required.

<table>
<tr>
<td width="50%" align="center"><img src="docs/media/readme/mobile-timer-en.png" alt="FlyPPTTimer mobile timer controls" width="330"></td>
<td width="50%" align="center"><img src="docs/media/readme/mobile-presentation-en.png" alt="FlyPPTTimer mobile presentation controls" width="330"></td>
</tr>
<tr>
<td align="center"><strong>Timer controls</strong></td>
<td align="center"><strong>Presentation controls</strong></td>
</tr>
</table>

Depending on the enabled workflow, the browser remote can help you:

- start, pause, stop, and reset timing;
- move through presentation controls without returning to the keyboard;
- manage the controlled presentation list;
- review slide timing for the current show;
- switch saved scenarios;
- browse and add local PPT files when file browsing is explicitly enabled on the PC.

<p align="center">
  <img src="docs/media/readme/settings-remote-en.png" alt="FlyPPTTimer Remote settings" width="820">
</p>

The Remote interface is intended for trusted local networks. Keep live QR codes and remote-control URLs private.

## Designed for multiple displays

A presentation setup rarely has only one screen.

FlyPPTTimer supports compact floating timers as well as a dedicated large-screen timer. This lets you keep a discreet timer near the speaker while using a much larger readout for a stage display, confidence monitor, event staff, or another screen.

The display setup includes controls for timer appearance, opacity, placement, screen selection, and whether slide metadata appears on the big-screen timer. The dedicated large-screen view is intentionally separate from the compact overlay, so the speaker and the room do not have to use the same layout.

## Different deck, different timing

A five-minute opening, a fifteen-minute keynote, and an eight-minute defense should not require rebuilding the timer every time.

FlyPPTTimer supports **presentation-specific rules**. Conceptually, your setup can look like this:

| Presentation | Timing |
|---|---:|
| `Opening.pptx` | 05:00 countdown |
| `Product Demo.pptx` | 15:00 countdown |
| `Defense.pptx` | 08:00 countdown |
| Open discussion | Unlimited count up |

This is especially useful for competitions, defenses, multi-speaker meetings, event schedules, and repeated presentation workflows.

## Save an entire setup as a scenario

A scenario is more than a duration preset. It can preserve a complete working setup so you can switch presentation styles without rebuilding your configuration.

For example:

| Scenario | Example setup |
|---|---|
| **Defense** | 8-minute countdown · advance reminder · compact speaker overlay |
| **Competition** | 5-minute countdown · large-screen timer · prominent overtime state |
| **Classroom** | unlimited count up · multi-display · slide timing visible |

<p align="center">
  <img src="docs/media/readme/settings-scenarios-en.png" alt="FlyPPTTimer scenario settings" width="820">
</p>

Scenarios can be switched from the settings UI and other supported control surfaces, making repeat events much faster to prepare.

## Alerts that happen before it is too late

Presentation timing is most useful **before** the clock reaches zero.

FlyPPTTimer supports configurable prompt points, end-of-timer behavior, visual feedback, and overtime presentation. The goal is to give the speaker useful cues without forcing them to constantly watch the clock.

<p align="center">
  <img src="docs/media/readme/settings-behavior-en.png" alt="FlyPPTTimer behavior and alert settings" width="820">
</p>

## Where it fits

FlyPPTTimer is designed for situations such as:

- **Thesis defenses and academic presentations** — keep strict presentation and Q&A timing visible.
- **Speech contests and timed competitions** — combine clear countdowns with reminders and big-screen output.
- **Pitches and demos** — stay aware of total time and slide pacing while presenting.
- **Training and teaching** — use count-up or unlimited timing for open-ended sessions.
- **Meetings and multi-speaker events** — prepare presentation-specific durations and reusable setups.
- **Live event operation** — control timing and presentations from another position using the browser remote.

## Quick start

1. **[Download the latest release](https://github.com/Hona-Cao/FlyPPTTimer/releases/latest)** and extract the portable package or use the installer package.
2. Launch **FlyPPTTimer**. The timer overlay appears immediately.
3. Open **Settings**, choose a duration and timing mode, then apply it.
4. Use **F3** to start/pause and **F4** to stop/reset with the default shortcuts.
5. Start a compatible PowerPoint or WPS slideshow to use slide-aware features.
6. For phone control, open **Remote Control** on the PC and connect from a browser on the same local network.

For detailed setup, screenshots, controls, presentation rules, display placement, remote access, and troubleshooting, see the **[complete user guide](docs/USER_GUIDE.en.md)**.

## Feature overview

| Timing | Presentation | Display | Control |
|---|---|---|---|
| Countdown | PowerPoint integration | Floating timer | Global hotkeys |
| Count up | WPS Presentation integration | Multi-monitor output | Tray / timer menus |
| Unlimited elapsed time | Slide number display | Dedicated big-screen timer | Phone browser remote |
| Advance prompts | Per-slide stopwatch | Appearance & opacity | Presentation controls |
| Time-up / overtime display | Per-presentation rules | Position & screen selection | Scenario switching |
| Reusable scenarios | Current-session slide timing | Compact automatic sizing | Config import/export |

## Compatibility

- **OS:** Windows 10 / Windows 11
- **Architecture:** x64
- **Standalone timing:** works without Microsoft Office
- **Presentation integration:** requires a compatible desktop Microsoft PowerPoint or WPS Presentation installation
- **Phone remote:** modern browser on a device connected to the same reachable local network
- **Packages:** portable and installer editions are available from Releases

## Technology

FlyPPTTimer's current desktop application is built with **Rust** and **Slint**, with native Windows integration for the presentation and desktop workflow. The phone remote is delivered as a local browser interface.

The technology badges above describe the current application; GitHub's repository-language sidebar reflects files present in this public release/documentation repository and may not represent the application's complete implementation.

## Download & documentation

- **Latest version:** [GitHub Releases](https://github.com/Hona-Cao/FlyPPTTimer/releases/latest)
- **All releases:** [Release history](https://github.com/Hona-Cao/FlyPPTTimer/releases)
- **Mainland China mirror:** [Gitee Releases](https://gitee.com/hona-cao/fly-ppttimer/releases)
- **Complete English guide:** [docs/USER_GUIDE.en.md](docs/USER_GUIDE.en.md)
- **完整中文教程:** [docs/USER_GUIDE.zh-CN.md](docs/USER_GUIDE.zh-CN.md)
- **Bug reports & feature requests:** [GitHub Issues](https://github.com/Hona-Cao/FlyPPTTimer/issues)

## Star History

If FlyPPTTimer is useful in your presentation workflow, starring the repository makes it easier to find again and helps show how the project is growing.

<a href="https://www.star-history.com/?repos=Hona-Cao%2FFlyPPTTimer&type=date&legend=top-left">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=Hona-Cao/FlyPPTTimer&type=date&theme=dark&legend=top-left">
    <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=Hona-Cao/FlyPPTTimer&type=date&legend=top-left">
    <img alt="FlyPPTTimer Star History Chart" src="https://api.star-history.com/chart?repos=Hona-Cao/FlyPPTTimer&type=date&legend=top-left">
  </picture>
</a>

## Feedback

For bugs or feature requests, open a [GitHub Issue](https://github.com/Hona-Cao/FlyPPTTimer/issues). Include the FlyPPTTimer version, Windows version, PowerPoint/WPS version, reproduction steps, and relevant screenshots. Remove live QR codes, remote-access URLs, private paths, and sensitive presentation content before posting.
