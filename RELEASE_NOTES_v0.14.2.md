# FlyPPTTimer v0.14.2

## Remote presentation window

- Rebuilt the presentation page around a toolbar, an expandable PPT-list area, a rule editor card, and a separate action card.
- Buttons now size from their measured text and keep a single-line rendering mode; narrow layouts wrap controls between rows and the page scrolls rather than overlapping controls.
- The normal presentation actions and the destructive PowerPoint/WPS actions are visibly separated. Low-frequency actions are available through **More actions**.
- The visible access link masks its token. Copying the link and generating the QR code still use the complete valid URL.

## Delivery

- Version, assemblies, configuration protocol version and CI artifact names are updated to `0.14.2`.
- v0.14.1 release notes, audit evidence, acceptance evidence, artifacts and hash records are preserved unchanged.

## Manual verification still required

Verify the CI artifact at 100%, 125% and 150% DPI, including minimum-size behavior, real PowerPoint/WPS behavior and dual-display slideshow activation. Any screenshots must redact QR codes, tokens, full URLs, LAN IP addresses, and private file paths.
