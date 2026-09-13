# Building and packaging FlyPPTTimer v1.13.1

[Home](../README.md) · [中文用法](USER_GUIDE.zh-CN.md)

## Current code, not the legacy .NET application

The current executable is built from `src/main.rs`, the Rust modules in `src/`, and `ui/app-window.slint`. `src/FlyPPTTimer/Web/` supplies the actual embedded browser UI. The old C# project and historical scripts remain as provenance/baseline material; do not use `dotnet publish`, root `build.ps1` or root `package_release.ps1` to create a current V1 release.

Requirements: Windows x64; Rust **1.92.0** with the MSVC toolchain, rustfmt and Clippy; Visual Studio Build Tools with C++/Windows SDK (`rc.exe` available); Node.js for the JavaScript syntax check. The UI dependency is locked to Slint **1.17.1**. Installer packaging additionally uses **Inno Setup 6**. Network access is needed to obtain tools/crates unless cached.

```powershell
git clone https://github.com/Hona-Cao/FlyPPTTimer.git
cd FlyPPTTimer
git checkout v1.13.1
rustup toolchain install 1.92.0 --profile minimal --component rustfmt,clippy
cargo fmt --all -- --check
cargo clippy --locked --all-targets --all-features -- -D warnings
cargo test --locked
node --check src/FlyPPTTimer/Web/app.js
cargo build --locked --release
```

Run these in an environment with the Windows SDK resource compiler on PATH. A C++ developer PowerShell is a convenient starting point. The result is `target/release/FlyPPTTimer.exe`. Avoid dependency upgrades as part of reproducing an accepted build.

## Package both editions

```powershell
./scripts/build-release.ps1
```

Run from a PowerShell development session where your local build scripts are permitted. This script is a developer packaging tool, not a runtime dependency; the application does not launch PowerShell for audio.

The script uses `docs/v1131-default-config.json` for clean current defaults, includes app-local VC runtime DLLs, license and current documentation, compiles the per-user installer, and emits:

- `artifacts/release/v1.13.1/FlyPPTTimer-v1.13.1-portable-win-x64.zip`
- `artifacts/release/v1.13.1/FlyPPTTimer-v1.13.1-setup-win-x64.zip`

Use `-IsccPath 'C:\...\ISCC.exe'` when Inno Setup is not in its conventional location. The installer keeps the existing AppId and does not replace a user's existing configuration with defaults. `docs/default-config.json` remains the **v0.30.2 compatibility test fixture**; do not repurpose it as current packaging defaults.

For the first public v1.13.1 release, packaging deliberately reuses the exact application accepted by the user, from Actions run `34709344878`, product source `e625c809f8cf7515f0143fab476bb4467d2fe1d7`:

```powershell
./scripts/build-release.ps1 -ApplicationDirectory C:\path\to\accepted\app
```

The optional directory must contain `FlyPPTTimer.exe` with matching version metadata and may contain its runtime DLLs. Documentation and installer metadata can change without rebuilding that executable. The isolated screenshot renderer is never a release input.

## CI and history

`windows-ci.yml` builds the current Rust app, runs formatting/lint/tests and checks the embedded JavaScript; it no longer publishes the old C# v0.20.2 artifact. Native Office/audio tests remain explicitly ignored unless their documented device/Office prerequisites are supplied. A green unit-test result is not a physical phone or projector test.

The publication workflow prepares the two ZIP assets, smoke-tests installation, and publishes only on the authorized `main` branch. It creates no separate checksum assets. Repository commits, Actions provenance and the package `BUILD.txt` retain source traceability.

[Real development timeline](DEVELOPMENT_HISTORY.md) · [Screenshot reproduction](media/v1.13.1/README.md)
