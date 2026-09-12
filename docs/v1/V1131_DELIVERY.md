# FlyPPTTimer v1.13.1

Base: user-accepted RC3.4 ea123cda7f2e73778c2c1a3a5b04839fbd3afae4.

- Mobile list: horizontal swipes switch pages; native vertical list scrolling hands off to the outer page at either boundary, including short lists. Long-press reorder remains available.
- Percentage sliders: hover-wheel adjusts by one percentage point; adjacent editable numbers retain the existing ranges and precision. Valid input previews immediately; Enter/focus loss normalizes input. Apply persists and Cancel discards.
- PC Remote: File name / File path, Duration and Mode headings share the row column widths. Row height is 52 DIP, gap 4 DIP; file paths and enabled status remain.
- Settings after a language restart: OK or Cancel closes Settings, not the timer/tray/Remote service. The obsolete exit-on-settings-close mode has been removed.
- Every process startup shows the timer, even when the previous session saved Visible=false. Hiding remains available for the current session.
- Package, application, Remote API, manifest and Windows file/product versions are 1.13.1, without an RC/test suffix.

No dependency upgrade, main merge, tag or public GitHub Release. Previous accepted features remain unchanged. CI logs and BUILD.txt identify the validated product source. Physical Windows/phone interaction confirmation remains separate from automated checks.
