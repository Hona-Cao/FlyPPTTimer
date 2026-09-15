# v1.14.1 GUI images / 图文来源

These 10 PNGs are actual Slint GUI renders from product source
`6e8bd3aeaec7ae8c247e09b535157db7f490047a`, captured by
[workflow 34994278753](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34994278753).
They are not redesigned mockups or physical-device/network acceptance evidence.

## Permission sequence

Each language (English / Simplified Chinese) and theme (light / dark) has:
- `connected`: production connection-guidance callback, checkbox initially unchecked.
- `saved`: production Apply callback saves permission and refreshes the notice/status.

The connection event, `192.0.2.10` address and Remote state are documentation examples. No live phone session or access token is photographed. Service-status rows in these controlled captures are not proof of a real network connection; use your own running Remote service and live QR code.

`timer-idle.png` and `timer-slide.png` show the compact v1.14.1 readout with example content and its actual content-based size. Display scaling can change physical pixel dimensions.

## Reproduction and unchanged release executable

The workflow checks out the exact product commit in a disposable workspace, selects a CJK font only to resolve missing characters on the hosted headless renderer, and drives existing field-edit/Apply callbacks in the capture driver. Layout and functional callbacks are not replaced. `--capture-settings` and `--capture-windows` generate the PNGs.

Neither the temporary font file nor documentation renderer executable is committed or distributed. Only selected PNGs reach the documentation branch. The public Release and delivered application EXE remain unchanged.

The phone UI did not change in v1.14.1. Its existing v1.14.0 images are reused intentionally and labeled in the guide; their source HTML/CSS/JS is not modified by this documentation task.
