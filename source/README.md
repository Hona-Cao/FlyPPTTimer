# Historical source code

This directory contains versioned FlyPPTTimer source snapshots from releases that were published under the MIT License. It is a historical archive; it is not the current v1.16.0 development source.

## Versions

- [`v1.14.1/`](v1.14.1/)
- [`v1.14.0/`](v1.14.0/)
- [`v1.13.1/`](v1.13.1/)

You can also use the matching historical source branches if you want the project files at the repository root:

- [`source/v1.14.1`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.14.1)
- [`source/v1.14.0`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.14.0)
- [`source/v1.13.1`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.13.1)

## Build a historical snapshot

For v1.14.1 on Windows x64, install the Rust MSVC toolchain and Windows SDK, then run:

```powershell
git clone https://github.com/Hona-Cao/FlyPPTTimer.git
cd FlyPPTTimer
git switch source/v1.14.1
rustup toolchain install 1.92.0 --profile minimal --component rustfmt,clippy
cargo build --locked --release
```

The executable will be generated at `target/release/FlyPPTTimer.exe`.

## License

The archived releases and source snapshots shown here retain the MIT License that accompanied those historical versions. Their existing MIT rights are not changed by the later v1.16.0 license boundary. Refer to the license file contained in the relevant historical snapshot or release for those terms.

The repository-root [LICENSE](../LICENSE) governs v1.16.0 and later original material unless a specific file or release states otherwise.
