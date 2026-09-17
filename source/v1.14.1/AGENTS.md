# FlyPPTTimer V1 Agent Notes

Before changing V1 code, read these files in order:

1. `docs/v1/HANDOFF.md` — current project status and new-session handoff.
2. `docs/v1/APPROVED_PRODUCT_DEVIATIONS.md` — explicit user-approved product changes that override conflicting older baseline items.
3. `docs/v1/V1_BASELINE_CHECKLIST.md` — permanent v0.30.2 product baseline for everything not overridden above.
4. `docs/v1/CODEX_TASK.md` — current task from ChatGPT; this is the only current implementation instruction.
5. `docs/v1/CODEX_RESULT.md` — latest implementation/result report.
6. The actual `v0.30.2` code and assets.
7. `agent/v4-foundation` only for proven technical lessons.
8. The current V1 implementation.

When `APPROVED_PRODUCT_DEVIATIONS.md` conflicts with `V1_BASELINE_CHECKLIST.md`, the approved-deviations document wins. Do not restore an older baseline behavior that the user explicitly changed later.

After completing the current task, update `docs/v1/CODEX_RESULT.md`, commit all source/document changes, and push the review branch so ChatGPT can audit the exact result from GitHub.

V1 must preserve the v0.30.2 feature set, options, defaults, behavior, Chinese and English text, Remote protocol, and PowerPoint/WPS behavior except for the explicit user-approved deviations recorded above. Do not add product features. Keep the implementation direct and lightweight, and add only the code and tests needed for real product behavior.

Do not modify the v0.30.2 tag or `agent/v4-foundation`. Do not create a Release or tag unless the user explicitly asks.
