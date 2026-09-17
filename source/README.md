# Source code

This directory contains versioned FlyPPTTimer source code.

## Versions

- [`v1.14.1/`](v1.14.1/)
- [`v1.14.0/`](v1.14.0/)
- [`v1.13.1/`](v1.13.1/)

You can also use the matching source branches if you want the project files at the repository root:

- [`source/v1.14.1`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.14.1)
- [`source/v1.14.0`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.14.0)
- [`source/v1.13.1`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.13.1)

## Build

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

FlyPPTTimer is released under the [MIT License](../LICENSE).
