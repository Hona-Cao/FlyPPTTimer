[记录目录](../README.md) · [原始汇总](../../v1/CODEX_RESULT.md)

# RC3.2 - ChatGPT direct implementation

Implemented the 2026-09-12 user feedback on automatic/custom sizing, page typography/position/alignment, large-screen overlay exclusion, metadata sorting, controlled-file removal, animated mobile reordering/long press and non-selectable mobile text. Requirements and scope: USER_FEEDBACK_20260912_3.md. Codex did not implement this round and remains paused.

This source note does not predeclare CI success. The final Windows run must pass fmt, clippy -D warnings, production regression tests, node --check and release build before packaging. BUILD.txt records the exact patched-source commit and EXE SHA256. Chromium mock-HTTP interaction checks and software Slint captures are separate from physical mobile/Office/mixed-DPI acceptance.

Earlier unmentioned items are provisionally passed according to the user, not newly retested by ChatGPT. Only RC32_MANUAL_TEST.md items require focused user feedback.

---
## Historical results below
