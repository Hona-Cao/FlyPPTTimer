# FlyPPTTimer V1 Agent Notes

Before changing V1 code, read `docs/v1/V1_BASELINE_CHECKLIST.md` in full.

Use this priority when resolving questions:

1. `docs/v1/V1_BASELINE_CHECKLIST.md`
2. The actual `v0.30.2` code and assets
3. Proven technical lessons from `agent/v4-foundation`
4. The current V1 implementation

V1 must preserve the v0.30.2 feature set, options, defaults, behavior, Chinese and English text, Remote protocol, and PowerPoint/WPS behavior. The v4 branch is technical reference only; do not inherit its product changes, WPF/WinForms structure, dual-EXE design, or heavy CI/GUI automation.

Build V1 with Rust + Slint for Windows 10+ x64, preferably as one `FlyPPTTimer.exe`. Keep the implementation direct, preserve v0.30.2 configuration compatibility, and add only small tests for critical regression-prone logic. Do not modify the v0.30.2 tag or `agent/v4-foundation` while implementing V1.

Keep all V1 source, assets, documentation, and build output inside this repository directory.
