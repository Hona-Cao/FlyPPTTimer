# FlyPPTTimer 1.19.3 Release Notes

This release fixes three problems that could interrupt a presentation, and makes the phone's close/exit actions explicit. Features and settings are the same as 1.19.2; nothing new was added.

## 1. Opening a deck from your phone failed after changing the default presentation app

- Before: if WPS was the default program and you later switched back to PowerPoint (or the other way round), opening a deck from your phone still went through the previous program, so it failed or opened in the wrong one.
- Now: it uses the presentation app currently set as your Windows default.

## 2. Opening the next deck right after a slideshow ended could hang

- Before: starting another deck immediately after the previous slideshow closed sometimes stayed on loading, and the only way out was restarting the app.
- Now: it retries automatically; if the deck still will not open, it uses the Windows default open method and takes over the deck that actually opened. The previous deck is never saved or closed.

## 3. The timer could reset to zero and restart in the middle of a slideshow

- Before: while the same slideshow was still running, the timer was occasionally treated as a new run and jumped back to the start.
- Now: the timer restarts only when a genuinely new slideshow begins; state wobble during the same show no longer interrupts it.

## 4. Clearer "close deck" and "exit presentation app" on the phone

- Decks you opened by hand, outside the file list, can now be closed remotely.
- "Exit presentation app" closes every deck in the current PowerPoint / WPS instance, including ones you opened by hand; other Office windows are untouched. It asks for confirmation first and states that unsaved changes will be discarded.

## Upgrading

- The installer updates over 1.19.2 in place and keeps your settings and file rules.
- If you have not hit these problems, staying on 1.19.2 is fine.
