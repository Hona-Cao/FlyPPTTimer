# FlyPPTTimer 1.19.6 Release Notes (vs. 1.19.4)

## Bug Fixes
- **Persistent Always-on-Top**: Fixed an issue where the timer window, multi-display overlays, and rehearsal control strip could fall behind fullscreen slideshows, the taskbar, or other windows; they now stay on top without stealing focus.
- **WPS PDF Compatibility**: Improved handling of both WPS Prometheus integrated mode (`wps.exe` + `wpspdf.exe`) and standalone mode, fixing total timer, per-slide timing, and page number readouts during WPS PDF playback.
- **Rapid Page-Turn Stability**: Fixed page readout flicker, accidental clearing of the per-slide stopwatch, and repeated window resizing during fast mouse-wheel scrolling in PowerPoint and WPS.
- **Cross-App Presentation Switching**: Fixed switching from mobile to a presentation temporarily opened in WPS when PowerPoint is the system default, and matched target windows strictly by application process type.
- **Mobile Quit Presentation App**: Fixed an issue where "Quit presentation software" from the mobile controller failed to close PowerPoint/WPS in certain states.
- **Runtime Stability Hardening**: Hardened internal presentation and remote-service mutex locks against poison-induced cascading crashes.

## New Features
- **PPT / PDF Visual Distinction**: Added subtle background tints, left accent bars, and `PPT` / `PDF` pill badges across Desktop Presentation Control, Settings Rules, and the Mobile presentation list (light and dark theme adaptive).
- **Document List Search & Type Filter**: Added real-time filename search and `All / PPT / PDF` filtering in both the Desktop Presentation Control and Mobile presentation list.
- **"Close All Presentations"**: Replaced the rarely used "Close last-opened presentation" action with "Close all presentations" (with confirmation, keeping the presentation app open) on both Desktop and Mobile.
- **Cross-App Confirmation Awareness**: Mobile confirmation prompts for "Close all presentations" and "Quit presentation software" now explicitly display the active target app (PowerPoint or WPS).
- **Generic Fullscreen Timing & Mobile PDF Upload**: Added fullscreen timing for external viewers and mobile PDF upload with original filename preservation (PDF playback navigation remains manual on desktop).
- **Overtime Count-Up Styling**: Unified overtime behavior settings with configurable negative prefix, timeout text/background colors, and elapsed overtime display.

## Improvements & Performance
- **Adaptive Layout & Remote UX**: Dynamically adapts between 1-line and 2-line timer/rehearsal layouts based on page-readout availability; unified "Open / Switch / Current" states and extended mobile Office command timeout to 23s.
- **Less Frequent Fullscreen Scanning**: Throttled main-thread desktop fullscreen window enumeration from 50ms to 150ms, reducing the number of scheduled scans.
- **Sane New-Install Defaults & Keybindings**: Voice alerts default to off on fresh installs, file browsing/receiving default to on without password, and F1–F12 scenario hotkeys enforce unique assignment.
- **Licensing & Binary Hardening**: Kept the existing online/offline activation protocol while hardening entitlement checks and enabling Release symbol stripping and LTO optimization.
