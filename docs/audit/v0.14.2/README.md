# v0.14.2 audit record

## Scope

Remote presentation layout and privacy refresh on top of commit `3c33152f508c59c91c1eda316914e998ed29e20b`.

## Evidence

- Automated tests are published by the v0.14.2 Windows CI workflow.
- The workflow emits a distinct `FlyPPTTimer-v0.14.2-windows-x64` artifact with an EXE SHA-256 file.
- No v0.14.1 audit material or delivery file is replaced.

## Screenshot privacy checklist

Before committing screenshots, redact the QR code, token, complete URL, LAN IP address, private filenames, and private paths.
