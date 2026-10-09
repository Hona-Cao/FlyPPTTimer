# FlyPPTTimer v1.18.0

Upgrade baseline: **v1.17.2**, the previous public release. v1.18.0 also includes the diagnostics and reliability changes from v1.17.3, which was not released separately.

## Reminders and fresh rounds

- **Simple / Custom:** new installations use Simple; existing configurations and scenario snapshots without a mode marker migrate to Custom. Both profiles are stored independently, including in scenarios. Editing or switching one never overwrites the other.
- Simple offers pre-end, Timer-end and post-overtime switches, two offsets and shared Sound / Flash / Speech effects. Custom retains detailed editors and adds post-overtime nodes. Post-overtime reminders require actual overtime and a policy that continues timing after the endpoint.
- While Running or Paused, applying any scenario (even the current one) or switching reminder profiles requires desktop confirmation of a fresh round. Cancel preserves the round; confirm clears elapsed time and reminder/slide feedback, retaining Running or Paused intent. Stopped rounds can switch directly. For active-round scenario changes from the phone, confirm on the desktop.

## Optional hover controls

**Free, OFF by default:** enable **Settings → Controls → Show rehearsal controls**. Hover near the timer to reveal an independent compact menu with its own corners; moving away from both timer and menu retracts it after about **700 ms**. A **2 DIP** gap separates it from the timer. It attaches on the right, or on the left near the monitor's right edge, without moving or shrinking the timer body.

Reset uses a rounded-square icon and clears/stops the round; Restart uses a circular-arrow icon and starts again; Pause / Resume use pause / play symbols. Pause is available while Running and Resume while Paused. The menu adapts to live timer colors, opacity, theme, shape and automatic/custom height. With **mouse pass-through enabled, the menu never appears and no clickable control window remains**.

Normal extra borders, native/render size synchronization and popup retirement have been repaired. Alert borders still appear when configured. These fixes address confirmed repaint/lifecycle defects; the intermittent field report of a double-visible region or light mask still needs physical confirmation. New configurations use **RoundedSmall**; upgrading does not forcibly replace existing saved shape choices.

## Phone control and Review

- **Remote (Pro):** +30 seconds / +1 minute cumulatively extend only the current Running or Paused finite round. Paused stays paused. Stopped, Finished and Unlimited rounds cannot be extended; reset/restart clears added time. Saved default duration, scenarios and presentation rules are unchanged by these buttons.
- Connection setup emphasizes phone control, QR and same-LAN guidance, with technical information in collapsed **Advanced details**. Editing and applying the saved duration is a separate action; applying it during Running/Paused starts a fresh round after the phone confirmation.
- **Presentation Review (Free):** summary shows effective accumulated recorded slide time, ahead/overtime against the final target and the longest accumulated slide. With extension, it shows **original plan + added time + final target**. Unlimited reports omit target comparison. Existing history and detailed slide statistics remain available.
- **Pro baseline comparison:** compares effective time with an explicitly selected review for the same presentation, separately from the target comparison. No baseline is selected automatically.

## Diagnostics and reliability since v1.17.2

Open **Settings → Other → Diagnostics Center** or the tray entry. Copy a safe summary or export a diagnostic report covering app, presentation observer, displays, Remote, Browser Display, license summary and updater status. Export omits tokens, activation data, presentation paths/content, raw configuration and logs. Refresh does not start a trial or contact licensing/update services.

Remote now recovers accurate stopped/failure status after listener or connection-thread failures. Browser Display serializes requests, bounds request/body timeouts, rejects stale responses, retains the last valid picture during disconnection and retries automatically with bilingual feedback. Client license error/expiry handling fixes are included in the executable.

## Free / Pro and upgrade care

Free includes core local timing, both reminder profiles, **one enabled Custom pre-end node nearest the endpoint + Timer end + one enabled post-overtime node nearest the endpoint**, one available scenario, hover controls, Diagnostics and complete base Review. Pro adds Remote (including extension), Browser Display, multiple Custom nodes/scenarios and rehearsal baseline/pace comparison. Extra stored Pro configuration is preserved when Pro is unavailable; it does not run in Free and becomes available again with Pro.

Back up your configuration and custom alert sounds before upgrading. Existing settings and history remain supported. The 7-day trial starts only by explicit user confirmation. **Restarting an unexpired trial still requires online reconfirmation; its original server-defined 168-hour period is not reset.** A broader offline-after-restart policy remains undecided. License-service repairs exist in source and local simulated tests only; they have **not been deployed to production**. These notes do not announce new live server revocation or trial behavior.

Real Office/WPS operation, audio, physical LAN/phone use, multi-monitor/fractional-DPI dragging/locking/pass-through and interactive confirmation still require manual acceptance. Isolated/software tests do not replace those checks. Official portable and installer packages are distributed through the [GitHub Releases](https://github.com/Hona-Cao/FlyPPTTimer/releases) and [Gitee Releases](https://gitee.com/hona-cao/fly-ppttimer/releases) pages.
