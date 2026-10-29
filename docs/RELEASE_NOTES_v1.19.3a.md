# FlyPPTTimer 1.19.3a — Test Preview

- Fix remote-open failure after switching from WPS to PowerPoint or changing the Windows default presentation app; avoid reusing a stale Office/WPS COM connection.
- Retry opening the next presentation after the previous slideshow exits; if COM still fails, use the Windows file association and reconnect to the application that opened the file.
- Test preview only; this is not a stable release.

Automated tests: 666 passed, 0 failed, 10 ignored. Real PowerPoint/WPS acceptance is still pending.
