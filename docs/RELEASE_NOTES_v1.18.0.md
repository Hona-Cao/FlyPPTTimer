# FlyPPTTimer v1.18.0

[简体中文](RELEASE_NOTES_v1.18.0.zh-CN.md) · [User guide](USER_GUIDE.en.md)

Changes from **v1.17.2**, the previous public stable release. This release includes the diagnostics and connection-recovery improvements from v1.17.3, which was not published separately.

## Added

- **Independent Simple / Custom reminder profiles**, with reminders before the target, at Timer end and after actual overtime. New installations use Simple; existing unmarked settings and scenarios retain Custom.
- **Optional rehearsal hover controls — Free**, off by default: Reset, Restart, Pause and Resume. The independent menu retracts about 700 ms after the pointer leaves, keeps a 2 DIP gap, and adapts to the timer without moving its readout. Mouse pass-through hides it completely.
- **Current-round extension — Remote Pro:** +30 seconds / +1 minute for Running or Paused finite rounds. Extensions accumulate without changing saved default duration, file rules or scenarios; Reset/Restart clears them.
- **Diagnostics Center — Free:** local status, Copy safe summary and Export diagnostic report. Reports exclude tokens, activation material, presentation paths/content, raw configuration and logs.

## Improved

- Running/Paused profile switches and every scenario application, including the current scenario, now require desktop fresh-round confirmation. Cancel preserves the round; confirmation clears timing feedback and retains running/paused intent.
- Desktop Remote setup emphasizes phone connection, QR and same-LAN guidance. Desktop connection technical settings are grouped under collapsed Advanced details.
- **Presentation Review — Free** summarizes effective accumulated slide time, comparison with the final target and the longest accumulated slide. Extended rounds show original plan, added time and final target; Unlimited omits target comparison.
- Review adds an effective-time comparison with an explicitly selected same-presentation **Pro rehearsal baseline**, alongside existing details and pace information.
- English/Chinese labels, light/dark readability and dialog presentation are more consistent. New configurations use a small rounded timer shape; saved shapes are preserved.
- Browser Display keeps the last valid picture during disconnection, shows reconnection feedback and retries automatically.

## Fixed

- Remote service status now reflects listener/connection failures so users can identify a stopped service and restart it.
- Browser Display request ordering, timeouts and stale-response handling are more reliable.
- Reminder-profile editing no longer overwrites the inactive profile. Free runs one nearest enabled Custom pre-end point, Timer end and one nearest post-overtime point without deleting extra saved points.
- Rehearsal-menu border, sizing, repaint and popup-cleanup defects have been addressed; configured reminder flashes remain available.
- Client-side license error classification, expiry handling and license-related Settings actions have been corrected.

## Known limitations

- An unexpired trial still requires online reconfirmation after app restart. Its original **168-hour server-defined expiry** does not reset.


**Upgrade:** back up configuration and custom alert sounds. Existing settings and Review history remain readable; Free retains its local workflow and one available scenario. **All Remote Control, including phone Timer/Presentation and Browser Display, requires Pro.** Extra saved Pro settings are retained while inactive. See the [guide](USER_GUIDE.en.md#pro) for edition limits.

Official packages: [GitHub v1.18.0](https://github.com/Hona-Cao/FlyPPTTimer/releases/tag/v1.18.0) · [Gitee releases](https://gitee.com/hona-cao/fly-ppttimer/releases).
