# Historical source archive

FlyPPTTimer is MIT-licensed free and open-source software. This directory restores a browsable historical source snapshot for the 1.14 series while keeping the repository's current release/documentation layout clear.

## Available source

- [`v1.14.1/`](v1.14.1/) — exact source tree from product commit [`6e8bd3aeaec7ae8c247e09b535157db7f490047a`](https://github.com/Hona-Cao/FlyPPTTimer/commit/6e8bd3aeaec7ae8c247e09b535157db7f490047a).
- [`source/v1.14.1`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.14.1) — the same v1.14.1 source in its original repository layout, with its real commit ancestry preserved.
- [`source/v1.14.0`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.14.0) — v1.14.0 source state.
- [`source/v1.13.1`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.13.1) — v1.13.1 milestone and the earlier development ancestry leading into the 1.14 series.

The v1.14.1 `Cargo.toml` identifies the application as version `1.14.1`. For building or inspecting Git history, prefer the `source/v1.14.1` branch so the historical project layout is at the repository root.

## Build the v1.14.1 source

On Windows x64, with the Rust MSVC toolchain and Windows SDK available:

```powershell
git clone https://github.com/Hona-Cao/FlyPPTTimer.git
cd FlyPPTTimer
git switch source/v1.14.1
rustup toolchain install 1.92.0 --profile minimal --component rustfmt,clippy
cargo build --locked --release
```

The historical source also contains its original tests, UI sources, resources, packaging scripts, documentation, and development records. Historical workflow files are preserved inside the snapshot for provenance; workflows nested under `source/v1.14.1/.github/` are not active GitHub Actions workflows on `main`.

## Licensing

The source in this archive is covered by the repository's [MIT License](../LICENSE). FlyPPTTimer will continue to be free and open source, and source for later releases will be published in this repository as well.
