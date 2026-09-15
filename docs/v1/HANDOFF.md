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

## Validated candidate

Executable source: 165591cdfe5e682f928c739d4a9ecc088aaa09f2.
Windows CI: 34952365172, successful (100 passed, 0 failed, 3 ignored).
Desktop GUI capture: 34953602785. Publication workflow: 34954515205.
The public release and main references must be verified after the publication job completes.
