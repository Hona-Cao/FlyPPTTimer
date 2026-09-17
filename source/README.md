# Historical source archive

FlyPPTTimer is MIT-licensed free and open-source software. This directory restores browsable historical source snapshots for the 1.14 series and its immediate predecessor while keeping the repository's current release/documentation layout clear.

## Browsable snapshots

- [`v1.14.1/`](v1.14.1/) — exact tree from product commit [`6e8bd3aeaec7ae8c247e09b535157db7f490047a`](https://github.com/Hona-Cao/FlyPPTTimer/commit/6e8bd3aeaec7ae8c247e09b535157db7f490047a).
- [`v1.14.0/`](v1.14.0/) — preserved v1.14.0 source state (`Cargo.toml` version `1.14.0`).
- [`v1.13.1/`](v1.13.1/) — preserved v1.13.1 source state and the earlier development line leading into 1.14.

## Exact source branches

For cloning, building, or walking the original Git ancestry, use the version branches where the project files remain at repository root:

- [`source/v1.14.1`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.14.1)
- [`source/v1.14.0`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.14.0)
- [`source/v1.13.1`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.13.1)

The `source/v1.14.1` branch retains the real commit ancestry leading to the 1.14 series, so earlier implementation history remains inspectable instead of being replaced by a synthetic snapshot.

## Build the v1.14.1 source

On Windows x64, with the Rust MSVC toolchain and Windows SDK available:

```powershell
git clone https://github.com/Hona-Cao/FlyPPTTimer.git
cd FlyPPTTimer
git switch source/v1.14.1
rustup toolchain install 1.92.0 --profile minimal --component rustfmt,clippy
cargo build --locked --release
```

The historical snapshots retain their original tests, UI sources, resources, packaging scripts, documentation, and development records. Historical workflow files nested below `source/<version>/.github/` are retained only as provenance when browsing `main`; nested workflow directories are not active GitHub Actions workflows on `main`.

## Licensing

The source in this archive is covered by the repository's [MIT License](../LICENSE). FlyPPTTimer will continue to be free and open source, and source for later releases will be published in this repository as well.
