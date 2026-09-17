# RC3.3 user feedback - focused direct implementation

Date: 2026-09-12. Baseline: 359a09364fa8c36310084236ec8d3fa3a0382160 (RC3.2 source plus delivery note). Version stays 1.13.0; dependencies pinned. Codex remains paused.

Requested: simultaneous Settings/PC Remote; wider scroll and footer gutters; product icons on app-owned windows/dialogs; visual standard/custom color chooser alongside HEX; PowerPoint-only additions (.ppt/.pptx/.pptm); compact one-row five-button mobile file actions; fix sorting/reordering new-old-new flicker; investigate one unrepeatable blank-on-reopen report.

Confirmed source findings:
- Desktop OpenSettings/Remote explicitly hid the sibling window.
- Command acknowledgements used fresh configuration but did not publish the shared GET state cache. GET attached the latest counter to old data, allowing the displayed order to revert until the periodic producer refreshed it.
- Mobile rendering cancelled/restarted FLIP animation even for unchanged order on status polls.
- The native picker used a pipe between description and extensions instead of the Win32 NUL-separated filter pair, and explicitly admitted PDF.
- Native Remote show used Slint show, raw SW_HIDE, then Slint show; Settings' delayed callback could re-show a recently closed window. These are lifecycle risks; they do NOT prove the sole cause of the reported blank incident.

Implementation boundaries:
- Keep separate reusable management-window lifecycles, invalidate obsolete deferred shows, use Slint show/hide, request redraw and log geometry. Do not guess-change mixed-DPI stabilization.
- UI producer publishes each command snapshot before replying. Snapshot owns a monotonic revision and process-instance id; GET never forges revisions; client ignores stale snapshots/polls but handles EXE restart. Unchanged order never restarts an animation. No automatic retry of destructive or Next commands.
- Add native ChooseColor palette/custom RGB with existing HEX input and swatch. Dialog creation is off the Slint callback thread; completion posts back. Cancel leaves drafts unchanged. No scripts, new dependencies or persistent services.
- Existing product assets are used for all application Windows; app-owned native dialogs and balloons are branded. Do not alter Explorer or Office icons outside this program.
- Validate additions as .ppt/.pptx/.pptm, fix filters and prevent common-dialog current-directory changes. Do not delete legacy configured files or their disk contents.

Validation must report actual fmt/clippy/test/JS/release results separately from browser emulation, hosted Windows native checks, and actual user devices. Provide one portable ZIP with exact source commit and binary hashes after successful checks. Do not create Release/Tag or modify main.
