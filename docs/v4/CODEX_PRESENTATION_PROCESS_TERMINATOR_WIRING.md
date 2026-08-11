# Codex task: wire presentation process terminator

Work on branch `agent/v4-foundation`.

## Allowed files

Only modify:

- `src/FlyPPTTimer/Services/PowerPointControlService.cs`
- `tests/FlyPPTTimer.Tests/PresentationControlAbstractionTests.cs`

Do not modify the terminator, detector, dispatcher, monitor, window activator, native methods, UI, project files, dependencies, SDK, or CI.

## Required change

Wire the existing `PresentationProcessTerminator` into `PowerPointControlService`.

1. Add one readonly `PresentationProcessTerminator` field.
2. Construct it once with `warn: _log.Warn`.
3. Replace only the implementation of `ForceQuitAll()` with delegation to `_processTerminator.TerminateAll()`.
4. Return `result.Message` when no presentation process is detected.
5. Clear `_managedPresentations` only when `result.AnyDetected` is true, then return `result.Message`.
6. Add a source contract test covering the field, constructor, delegation, conditional clear, and removal of direct `process.Kill(true)` from `PowerPointControlService`.

Expected service shape:

```csharp
private string ForceQuitAll()
{
    var result = _processTerminator.TerminateAll();
    if (!result.AnyDetected) return result.Message;
    _managedPresentations.Clear();
    return result.Message;
}
```

## Preserve

Do not change:

- command names or confirmation flow
- returned Chinese messages
- process-name policy
- `ReadState` or WPS capability detection
- PowerPoint/WPS COM behavior and release
- timeouts and 500ms refresh
- STA dispatch, state monitoring, window lookup/activation, or events
- WinForms/WPF UI

Do not commit `artifacts/`, `bin/`, or `obj/`.

## Validation

Run all restores, all three Release builds, desktop tests, Core tests, and the `win-x64` self-contained single-file publish. Require zero warnings/errors and all tests passing.

Commit and push to `agent/v4-foundation`. Do not create a PR, merge `main`, or force-push.

Report commit SHA, changed files, build/test results, publish path and size, and final `git status --short`.
