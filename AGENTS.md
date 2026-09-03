# FlyPPTTimer V1 Agent Notes

Before changing V1 code, read these files in order:

1. `docs/v1/V1_BASELINE_CHECKLIST.md` — permanent product baseline.
2. `docs/v1/CODEX_TASK.md` — current task from ChatGPT; this is the only current implementation instruction.
3. The actual `v0.30.2` code and assets.
4. `agent/v4-foundation` only for proven technical lessons.
5. The current V1 implementation.

After completing the current task, update `docs/v1/CODEX_RESULT.md`, commit all source/document changes, and push the review branch so ChatGPT can audit the exact result from GitHub.

V1 must preserve the v0.30.2 feature set, options, defaults, behavior, Chinese and English text, Remote protocol, and PowerPoint/WPS behavior. Do not add product features. Keep implementation direct and lightweight; no SHA-256/integrity framework, no defensive architecture, and only small tests for critical regression-prone logic.

Do not modify the v0.30.2 tag or `agent/v4-foundation`. Do not create a Release or tag unless the user explicitly asks.
