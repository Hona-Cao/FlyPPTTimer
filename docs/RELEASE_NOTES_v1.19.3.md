# FlyPPTTimer 1.19.3 Release Notes

This update fixes three issues that affected normal presentations and adjusts the phone-side close-deck and exit behaviour. Features and settings are unchanged from 1.19.2.

## 1. Phone remote could not open a deck after switching the default presentation app

- Before the fix: after changing the default from WPS to PowerPoint (or the reverse), opening a deck from the phone still called the previous app, so it failed or opened in the wrong program.
- After the fix: the current Windows default presentation app is always used.

## 2. Opening the next deck immediately after a slideshow could hang

- Before the fix: opening another deck right after the previous slideshow exited occasionally stayed in a loading state and required restarting the app.
- After the fix: the app retries automatically, falls back to the system default open method, and takes over the deck that actually opened. The previous deck is neither saved nor closed.

## 3. Timer could reset to zero during a running slideshow

- Before the fix: mid-slideshow, the timer was occasionally treated as a new run and restarted from zero.
- After the fix: the timer restarts only when a new slideshow actually begins.

## 4. Behaviour changes for closing decks and exiting the app from the phone

- Decks opened manually and not in the file list can now be closed remotely.
- "Exit presentation app" closes every deck in the current PowerPoint / WPS instance, including manually opened ones; other Office windows are unaffected. Confirmation is required and unsaved changes are discarded.

## Upgrading

- The installer can upgrade over 1.19.2 in place; personal settings and file rules are preserved.
