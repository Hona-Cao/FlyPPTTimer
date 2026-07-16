# v0.14.2 acceptance checklist

Use the **CI artifact** `FlyPPTTimer-v0.14.2-windows-x64` for acceptance.

- [ ] 100% DPI: default and minimum window, empty list and at least three PPT entries.
- [ ] 125% DPI: no clipped text, button wrapping, overlap, or overflow.
- [ ] 150% DPI: toolbar, list, editor and action cards remain usable; narrow layout scrolls or reflows.
- [ ] Normal and destructive actions are clearly separated and clickable.
- [ ] Navigation is rounded/custom rather than a classic raised TabControl.
- [ ] Visible URL masks the token; copy and QR flows still work with the full URL.
- [ ] Real PowerPoint, WPS limitations, and dual-display behavior are recorded separately.

Do not commit screenshots containing QR codes, tokens, full URLs, LAN IPs, filenames, or private paths.
