# RC3.2 user feedback implementation / 2026-09-12

Base tested source: `5bc223bd7bea12339311bac4494f369bb3d363b1` (RC3.1).
Version stays 1.13.0, Rust 1.92.0, dependencies unchanged.
The user provisionally accepts all previous items not mentioned in this feedback.
That is user acceptance, NOT newly executed physical Office/DPI/mobile tests.

## Approved changes

1. Automatic sizing (default) measures current time/page content and fonts; width/height settings are hidden. Custom sizing uses the saved exact width/height, shows the fields, and does not silently grow. Too-small custom sizes can clip; switch to Automatic for full fit. Custom dimensions survive switching modes.
2. Time and page rows share one vertically centered block with equal top/bottom layout margins. Page size and color follow time by default, with independent overrides; italic, left/center/right alignment and above/below placement are configurable. These fields preview together; Apply saves and Cancel discards. The page-width reserve prevents jitter during pagination.
3. A display occupied by the full-screen timer is excluded from small overlays, including mirrored overlays, show/hide hotkeys and refresh. Disabling/closing the large timer restores eligible small overlays. Display selection still requires an extended screen.
4. The controlled mobile list includes only explicit file rules. Open unlisted documents are offered separately under Add open file. Remove file requires confirmation, removes control membership, never deletes/saves/closes the disk or Office document. Backend rejects subsequent direct remote controls for unlisted targets. Global quit refuses unknown/mixed/shared Office processes rather than kill removed files.
5. Name sorting uses Windows StrCmpLogicalW (numeric-aware, case-insensitive locale collation); size uses actual bytes; modified time uses actual file metadata. Both directions are available; unavailable metadata stays last. A manual move switches to manual sorting, persists compact order, and preserves PC list order. Hidden files remain controlled; removed files do not.
6. Keyed mobile rows animate movement, mark the moved row, and support stationary long-press drag, a drag ghost, neighbor transitions and edge scroll. One command is submitted on drop, never on cancel. Stale orders are rejected. Normal swipe scroll remains available. Text selection/copy/context menus are disabled; numeric fields remain editable. Reduced-motion preference is respected.

## Validation boundaries

Run Windows fmt/clippy/tests/JS syntax/release build before shipping. Automated checks cover production sizing decisions, row visibility, config persistence/stale merge, display exclusion, native name/metadata sorting, removal permission and stale dragging. Chromium integration checks use mocked HTTP state and exercise the real shipped frontend; they are NOT Office, network, physical phone or mixed-DPI validation. Existing software-renderer captures provide Slint layout evidence only.

Codex remains paused. No Release, tag, default-branch merge, or dependency/version upgrade.
