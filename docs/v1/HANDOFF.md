# v1.14.0 release handoff

The user requested eight changes and explicitly authorized GitHub publication of v1.14.0. Work starts from main 7c0f3a77d9dd9f82030b4def11df020cfea889aa, retaining the author's subsequent README/Gitee changes and all earlier development history.

## Product scope

- Complete resizable, scrollable release notes; update checking defaults on. Pre-1.14 configuration migration enables it once, and later user changes are respected. GitHub and Gitee stable releases are checked; the highest reachable version is selected. No automatic installation without user action.
- Fresh horizontal and vertical placement offsets are zero. Center anchors use the full target monitor bounds and are recalculated when automatic content width changes. Explicit saved offsets remain user preferences.
- More compact Settings and desktop Remote vertical spacing.
- Mobile presentation controls precede the controlled list.
- Authenticated mobile browsing of local folders/PPT files, gated by a desktop opt-in. It adds existing PPT/PPTX/PPTM files to rules; it does not upload, download, delete or modify files, or start presentations implicitly.
- Optional slide stopwatch in seconds, fixed below the main time at left, with page numbers at right. Font follows page numbers by default; font/color are customizable. Main timer pause does not pause real slide dwell time. Per-visit seconds reset on slide change; the mobile table accumulates revisits within the current show. History is memory-only and is replaced by a new show/document.
- Idle metadata reserves its normal space and displays 00 and -/- when enabled. Per-slide stopwatch is opt-in; page numbers retain their existing setting.
- Consistent themed mobile selectors with keyboard support, click-away dismissal and reduced-motion support.

## Validation and delivery

Product validation runs on Windows with Rust 1.92.0. Build/source identifiers and results are recorded by CI and in BUILD.txt. The release uses the validated application artifact, not the disposable font-enabled documentation renderer. Documentation captures use the actual GUI and public example data; no font files are distributed.

The user-facing release contains exactly the portable ZIP and setup ZIP, without separate checksum assets. GitHub's automatic source archives are not application packages. Preserve v1.13.1 and earlier tags. Fast-forward main without squashing or fabricated commit dates. Read current remote state before further writes.

Automated Rust, HTTP, browser interaction and installer checks do not constitute physical Office/WPS or phone acceptance. No Gitee publication is implied by a GitHub release.


## Published on 2026-09-15

GitHub Release v1.14.0 is public, not a prerelease or draft.
Release/tag source: af7ac108939d0c375800a49f64aa10926763a661.
Executable source: 165591cdfe5e682f928c739d4a9ecc088aaa09f2.
Product CI: 34952365172 (100 passed, 0 failed, 3 explicitly ignored).
Publication workflow: 34985358779, both documentation and package-and-publish jobs succeeded.
The interrupted publication was fixed by using Appearance.Width, the actual saved field, in the installer check; the application was not changed to bypass validation.

Exactly two uploaded assets: FlyPPTTimer-v1.14.0-portable-win-x64.zip and FlyPPTTimer-v1.14.0-setup-win-x64.zip. No checksum assets were uploaded.
Installation, runtime DLLs, preservation of existing configuration, pre-1.14 update-check migration, startup visibility and live Remote state passed on hosted Windows.
Eight focused English/Chinese browser checks passed. These are automated checks, not physical-phone or Office/WPS acceptance.

This documentation-only follow-up captures completed finite CSS animations so the file-browser images are not captured halfway through opening. Shipping HTML/CSS/JS and the executable remain unchanged. The release tag/assets remain fixed; corrected documentation screenshots are on the continuing branch/main, while the release ZIPs retain their publication-time documentation.
Fast-forward main to the finalized documentation commit without rewriting earlier history; read the remote ref before updating. Preserve v1.14.0 and earlier tags. No Gitee release was made in this task.

## v1.14.1 illustrated documentation synchronization

The user requested current GitHub explanations/tutorials with illustrations. The guides now describe delivered v1.14.1, whose runtime source is 6e8bd3aeaec7ae8c247e09b535157db7f490047a on review/v1.14.1. The latest public Release remains v1.14.0. This documentation task does not publish a release, move tags, replace packages or merge the review runtime into main.

Updated bilingual READMEs, full tutorials, changelog and development stage record. Ten new GUI images show connected/saved permission states in both languages and themes plus the compact timer. Captures use the production layouts in a disposable CJK-enabled documentation renderer (run 34994278753); fonts and the temporary executable are not shipped. Unchanged phone images retain their documented v1.14.0 provenance.

The documentation workflow validates local links, explicit anchors, image references and a documentation-only diff before fast-forwarding main. Read the final workflow result and remote main ref to confirm completion. No new runtime testing or physical-device acceptance is claimed.
