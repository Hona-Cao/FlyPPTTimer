# v1.14.0 GUI images

Desktop images come from current Slint layouts and production capture entry points. Mobile images render shipping HTML/CSS/JavaScript with example state in Chromium. Folder names, paths, QR tokens and page timings are examples, not the user's private data. These are not photographs of physical phone/Office testing.

The disposable desktop documentation renderer selects a CJK font explicitly because hosted headless rendering does not reliably select the Windows CJK fallback. Neither the font nor that temporary EXE is distributed. The release uses the separately validated production executable.

Generate mobile renders with `python scripts/capture-mobile.py`; run `python scripts/check-mobile-v1140.py` for interaction checks. Pass `--browser` to use a local Chromium binary. `scripts/select-doc-captures.py` selects the desktop images without changing their pixels.
