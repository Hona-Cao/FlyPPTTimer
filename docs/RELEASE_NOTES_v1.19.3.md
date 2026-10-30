# FlyPPTTimer 1.19.3 Release Notes

This release fixes two problems that could stop a deck from opening. Features and settings are the same as 1.19.2; nothing new was added.

## 1. Opening a deck from your phone failed after changing the default presentation app

- Before: if WPS was the default program and you later switched back to PowerPoint (or the other way round), opening a deck from your phone still went through the previous program, so it failed or opened in the wrong one.
- Now: it uses the presentation app currently set as your Windows default.

## 2. Opening the next deck right after a slideshow ended could hang

- Before: starting another deck immediately after the previous slideshow closed sometimes stayed on loading, and the only way out was restarting the app.
- Now: it retries automatically; if the deck still will not open, it uses the Windows default open method and takes over the deck that actually opened. The previous deck is never saved or closed.

## Upgrading

- The installer updates over 1.19.2 in place and keeps your settings and file rules.
- If you have not hit either problem, staying on 1.19.2 is fine.
