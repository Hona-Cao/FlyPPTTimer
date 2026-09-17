# RC3.2 focused manual test

Unzip to a NEW writable folder. Exit old FlyPPTTimer before starting the new EXE; otherwise the single-instance guard may leave the old EXE active. Do not overwrite your existing configuration. Use disposable copies of decks.

1. Appearance > Window Size & Font: Automatic hides Width/Height, changing time font or page font grows/shrinks the content window; no settings are written until Apply. Custom shows fields and uses exact dimensions. Returning to Custom retains saved dimensions; sizes too small intentionally clip. Cancel restores the applied mode/fonts.
2. With valid page numbers, verify equal top/bottom spacing. Default page size/color follow time; disable follow and test a different page size/color, italic, left/center/right, above/below, then save/reopen. Check both a small timer and the large timer.
3. Enable the large timer on an extended display with all-screen overlays enabled. That display must show ONLY the large timer; other displays keep small overlays. Test show/hide, change the large display, then disable/close it and check restoration.
4. Phone list: use names Deck1, Deck2, Deck10; check name/size/modified ascending/descending against the files. Switch to manual with Move Up/Down and verify movement animation and saved order. Refresh and restart, order persists.
5. Hold a file's title/drag handle for about 0.36 seconds, drag across neighbors, hold near the list edge to scroll, release. The ghost and neighbor transitions should be legible; one move is saved. A normal quick swipe scrolls. Cancel via switching app without release must not submit a move. Verify long press does not select text; number input still works.
6. Remove a disposable open file. It disappears from the controlled list and its remote close/start/flip controls are unavailable. Its disk content and open document remain. It can be explicitly re-added using Add open file (or desktop Add files) before remote control works again. Confirm the playing deck has a distinct background/badge.

Previous unmentioned features are provisionally accepted by the user. No full retest requested. Physical Android/iOS gestures, actual large-screen visibility and installed Office integration still require this check; automated CI and browser emulation do not prove them.

Feedback: item / pass or failure / operation / expected versus actual / screenshot or short recording. Do not share remote tokens or private deck content.
