# FlyPPTTimer 1.19.3a — What's new

A test build after 1.19.2. It fixes two problems that could block normal use; features and settings are the same as in 1.19.2.

## 1. Opening a deck from your phone failed after you changed the default presentation app

- Before: if WPS was the default program and you later switched back to PowerPoint, opening a deck from your phone still went through WPS, so it failed or opened in the wrong program.
- Now: it always uses the presentation app currently set as your Windows default.

## 2. Opening the next deck right after a slideshow ended could hang

- Before: starting another deck immediately after the previous slideshow closed sometimes stayed on loading, and the only way out was restarting the app.
- Now: it retries automatically; if the deck still will not open, it uses the Windows default open method and then takes over the deck that actually opened. The previous deck is never saved or closed.

## Notes

- Test build for the two fixes above. If you have not hit either problem, stay on the stable 1.19.2.
- The installer upgrades in place and keeps your settings.
