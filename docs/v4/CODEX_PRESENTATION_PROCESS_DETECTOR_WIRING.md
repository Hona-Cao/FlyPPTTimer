# Codex task: wire presentation process detection

## Goal

Wire the existing `PresentationProcessDetector` into `PowerPointControlService` only for WPS capability population.

This is a narrow mechanical refactor. It must not change presentation commands, COM behavior, window behavior, force-exit behavior, messages, timeouts, refresh cadence, or UI.

## Allowed files

Modify only:

- `src/FlyPPTTimer/Services/PowerPointControlService.cs`
- `tests/FlyPPTTimer.Tests/PresentationControlAbstractionTests.cs`

Do not modify:

- `src/FlyPPTTimer/Services/PresentationProcessDetector.cs`
- `tests/FlyPPTTimer.Tests/PresentationProcessDetectorTests.cs`
- `src/FlyPPTTimer/Services/PresentationStateMonitor.cs`
- `src/FlyPPTTimer/Services/PresentationStaDispatcher.cs`
- `src/FlyPPTTimer/Services/PresentationWindowActivator.cs`
- `src/FlyPPTTimer/Native/NativeMethods.cs`
- any WinForms or WPF view
- project, package, SDK, or workflow files

## Required implementation

In `PowerPointControlService`:

1. Add one readonly `PresentationProcessDetector` field.
2. Construct it once in the service constructor with its default system source.
3. Keep both existing `PopulateWpsCapabilities(state)` call sites in `ReadState` unchanged.
4. Change `PopulateWpsCapabilities` from static to instance scope.
5. Replace only its direct `Process.GetProcesses()` detection logic with:
   - one `_processDetector.Detect()` call;
   - `state.WpsDetected` from the returned snapshot;
   - `state.WpsCapabilities` from `PresentationProcessDetector.CreateWpsCapabilities(snapshot.WpsDetected)`.
6. Preserve the exact existing WPS capability message and capability values through the detector helper.

## Explicitly forbidden

Do not modify any of the following:

- `ForceQuitAll`, including its process enumeration, names, messages, kill behavior, confirmation flow, and disposal
- `ReadState` COM access, exception flow, return points, or rule-option population
- PowerPoint/WPS process names or aliases
- command names or command dispatch
- COM creation, access, retry, or release logic
- 15-second and 5-second command timeouts
- 500ms state refresh cadence
- STA queue behavior
- window discovery or activation
- the WPS first-frame hook
- event timing or event forwarding
- Chinese messages
- WinForms/WPF layout or styling

Do not add dependencies or upgrade SDK, packages, or GitHub Actions.

## Contract test

Extend `PresentationControlAbstractionTests.cs` with a source contract test that verifies:

- `PowerPointControlService` has a `PresentationProcessDetector` field;
- the constructor creates it once;
- `PopulateWpsCapabilities` is an instance method;
- it calls `_processDetector.Detect()`;
- it assigns `state.WpsDetected` from the snapshot;
- it calls `PresentationProcessDetector.CreateWpsCapabilities`;
- the existing `ForceQuitAll` method still exists and still contains its current process enumeration and `process.Kill(true)` behavior.

Do not replace the existing detector behavior tests.

## Validation

Run all commands from the repository root:

```powershell
dotnet restore tests/FlyPPTTimer.Tests/FlyPPTTimer.Tests.csproj
dotnet restore tests/FlyPPTTimer.Core.Tests/FlyPPTTimer.Core.Tests.csproj
dotnet restore src/FlyPPTTimer.Desktop/FlyPPTTimer.Desktop.csproj
dotnet restore src/FlyPPTTimer/FlyPPTTimer.csproj -r win-x64

dotnet build src/FlyPPTTimer/FlyPPTTimer.csproj -c Release --no-restore
dotnet build src/FlyPPTTimer.Core/FlyPPTTimer.Core.csproj -c Release --no-restore
dotnet build src/FlyPPTTimer.Desktop/FlyPPTTimer.Desktop.csproj -c Release --no-restore

dotnet test tests/FlyPPTTimer.Tests/FlyPPTTimer.Tests.csproj -c Release --no-restore
dotnet test tests/FlyPPTTimer.Core.Tests/FlyPPTTimer.Core.Tests.csproj -c Release --no-restore

dotnet publish src/FlyPPTTimer/FlyPPTTimer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true --no-restore -o artifacts/publish
```

All builds must report zero warnings and zero errors. All tests must pass and the single-file publish must succeed.

## Commit rules

- Commit and push only to `agent/v4-foundation`.
- Do not create another PR.
- Do not modify, merge, or push to `main`.
- Do not force-push.
- Do not commit `artifacts/`, `bin/`, or `obj/`.

## Completion report

Report:

- commit SHA;
- exact changed files;
- warning/error counts for all three builds;
- desktop and Core test totals;
- published executable path and byte size;
- final `git status --short`;
- confirmation that generated output was not committed.
