# v1.13.1 GUI image provenance / 界面图片来源

These PNGs depict the current product GUI, not an AI-generated redesign. Names, paths, time and network state in fixtures are examples, not private user data. They are documentation images, not evidence of physical phone/Office acceptance.

这些图片来自本版真实界面代码的渲染，不是概念图。图中的文稿、时间、地址均为示例；不要扫描文档中的二维码尝试连接自己的电脑。

## Desktop

`ui/app-window.slint` and the production callbacks in `src/settings.rs` supply the desktop UI. `src/capture.rs` drives the Settings pages and Remote window with example data.

The hosted headless renderer did not resolve CJK fallback correctly. For documentation only, `scripts/prepare-doc-capture.py` selects an explicit Noto CJK font in a **disposable checkout**, then the application is built solely to execute `--capture-settings` / `--capture-windows`. Layouts and behavior are not redesigned. The font and temporary executable are **not** committed or shipped. The actual release still contains the accepted executable from source `e625c809f8cf7515f0143fab476bb4467d2fe1d7`.

Connection QR/URL uses `192.0.2.10` and `documentation-example`; it contains no live credential. Native window decorations and OS-owned dialogs are not part of these software snapshots. Typography can differ from a user's Windows font setup.

```powershell
# In a disposable checkout only, with the documentation font at doc-font.otf:
python scripts/prepare-doc-capture.py
cargo build --locked
$env:FLYPPT_CAPTURE_THEME = 'dark' # repeat for light
$p = Start-Process target/debug/FlyPPTTimer.exe -ArgumentList '--capture-settings','captures/dark/settings' -Wait -PassThru
$p = Start-Process target/debug/FlyPPTTimer.exe -ArgumentList '--capture-windows','captures/dark/windows' -Wait -PassThru
```

Never run release packaging against that temporary build. Close/discard the checkout after capture. The publication workflow downloads the separately validated executable for packaging.

## Mobile/browser

`scripts/capture-mobile.py` loads the unchanged shipping `index.html`, `app.css` and `app.js` into Chromium with an in-memory example HTTP state. The viewport is 390×844 CSS pixels, device scale 2, with touch/mobile mode. Both English/Simplified Chinese and light/dark pages are captured.

```shell
python -m pip install playwright==1.57.0
python -m playwright install chromium
python scripts/capture-mobile.py
```

Alternatively use `--browser /path/to/chromium`. There is no connection to a user's PC, cloud account or real presentation. A screenshot of “Connected” reflects the stated fixture, not a live networking test.

## Maintenance

Regenerate screenshots when visible controls change, keep sample paths/tokens non-private, and update the README/tutorial links together. Do not reuse v0.30.2 screenshots to illustrate current V1 controls.
