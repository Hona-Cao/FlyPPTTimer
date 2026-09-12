# RC3.4 user feedback (2026-09-13)

Base: tested RC3.3.1 product commit 19bd8d13e98851223a8290cb052b9de4f8e4a09f.

1. Fresh defaults: independent page font 12, below time, right aligned. Preserve saved explicit preferences.
2. Small/medium/large corners: 3/7/14 DIP radii. Legacy Small retains its 7 DIP radius and displays as Medium. New Small is persisted as RoundedSmall to avoid an ambiguous migration.
3. All exposed percentages use draggable sliders: opacity 0-100%; horizontal/vertical offsets -50 to 50%. Update value and preview while dragging; Apply persists and discard restores.
4. Enter ends single-line editing; clicking blank form areas, tabs, buttons or other controls releases the previous editor focus. Ending edit is not silently saving settings.
5. Persistent footer: unsaved/saved settings. PC Remote also reports pending rule/port edits; mobile duration editor reports pending changes.
6. System/light/dark app theme, including standard Slint controls, Settings, PC Remote and in-app overlays. Browser follows the app with a local light/dark override. Timer custom colors are intentionally retained. Windows-owned file/color/message dialogs use the OS theme; no unsupported UxTheme patching.
7. Opening a controlled presentation shows and activates the exact document before maximizing its COM and native top-level window. No focus polling or input injection; repeated open of an already running slideshow does not obscure that show.

No main merge, release/tag, dependency upgrade or expanded broad regression campaign.
