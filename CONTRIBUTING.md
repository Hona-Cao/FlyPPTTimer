# Contributing to FlyPPTTimer

[Build the current Rust application](docs/BUILDING.md) · [使用教程](docs/USER_GUIDE.zh-CN.md) · [Development history](docs/DEVELOPMENT_HISTORY.md)

Bug reports, usability feedback, translations and focused code changes are welcome.

## Report an issue

Include the application version, Windows version, PowerPoint/WPS version, steps to reproduce, expected result and actual result. For display bugs include scaling and monitor arrangement; for phone bugs include browser and network type. Attach relevant logs/screenshots only after removing real remote tokens, QR codes, private paths and presentation content.

Use the issue tracker, not a public screenshot of a live access URL. Do not disable the firewall or antivirus to obtain a passing result.

## Develop

Current product code is Rust in `src/*.rs`, Slint in `ui/`, and Web Remote in `src/FlyPPTTimer/Web/`. Legacy C# files and old root packaging scripts are retained for historical reference, not the current build route. Use the pinned Rust toolchain and locked dependencies in [BUILDING.md](docs/BUILDING.md).

Make a focused branch. Keep English and Chinese labels/documentation consistent. Run formatting, Clippy and the relevant existing tests; run the full normal CI for shared behavior changes. Do not report ignored Office/audio tests as passed or GUI renders as physical-device tests.

## Product boundaries

Preserve explicit user-approved behavior, configuration compatibility and the Remote protocol unless the change is intentionally discussed. Removing a rule must not delete a file. Closing a document must not silently overwrite unsaved work. App theme changes must not overwrite timer colors. Avoid speculative fallback/retry frameworks and unnecessary new dependencies.

Read `AGENTS.md`, the current handoff and approved deviations before automated implementation. Historical tasks are not instructions to rerun old patch scripts. Do not apply `.github/*refine*` scripts to source where those historical changes are already committed.

## Pull requests and releases

Describe the problem, implementation, user-visible change and verification limits. Include updated screenshots when the UI changes and state how they were captured. Preserve meaningful implementation history; do not fabricate old activity dates.

Release publication requires explicit maintainer authorization. Current v1.13.1 publication is authorized; subsequent releases are not automatically authorized by that decision. Deliver portable ZIP and installer ZIP using the maintained packaging script. Release notes should identify compatibility limitations and update-channel behavior.

Contributions are subject to the repository license and the licenses of dependencies/assets.
