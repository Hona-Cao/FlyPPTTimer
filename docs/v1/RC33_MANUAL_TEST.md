# RC3.3 focused manual test

Exit the old EXE first; extract to a NEW writable folder and keep the DLLs beside the EXE. Use disposable PowerPoint documents. This version contains only the requested RC3.3 changes. Earlier unmentioned behavior remains provisionally accepted, not newly certified.

1. Open Settings and Remote together from the tray. Close/reopen either without losing the other or its uncommitted edits. Check content/scrollbar/footer spacing, taskbar/Alt-Tab/title icons. During ordinary repeated use, note any blank window; there is no need to stress it hundreds of times.
2. In Behavior and Appearance, click color swatches/Choose color. Select a standard color, a custom RGB color, and enter a HEX value. Cancel the chooser leaves the previous value. Apply saves; Settings Cancel rolls back preview. Page color follow-time behavior stays intact.
3. Add files from Settings and PC Remote. Only .ppt/.pptx/.pptm should be offered/accepted; typing a PDF or other file must not add it. Existing legacy entries are not silently deleted.
4. On the phone, each file's five actions should occupy one compact row. Sort by name/size/date, move up/down, and long-press drag. There must be only one transition, no new-old-new bounce during state refresh. Confirm order after refresh/restart. Reconnect after restarting the EXE should not freeze because revision numbers reset.

Blank-window root cause is not yet confirmed from the one reported incident. This change removes mixed native/Slint show/hide, invalidates stale delayed callbacks and logs open/close geometry. Hosted Windows repeated-open checks and browser emulation are evidence for those paths, not proof of all physical Windows11/GPU/DPI conditions. If a blank recurs, report which window, whether the frame/buttons remain, the last few operations and a time for matching the log.

Feedback: group number / pass or fail / steps / expected and actual / optional screenshot or video. Do not send remote tokens or private slide contents.
