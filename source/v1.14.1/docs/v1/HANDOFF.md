# v1.14.1 completed follow-up

Branch: review/v1.14.1. Main and public release tags are unchanged. Deliver portable and setup ZIPs; do not create a public release for this follow-up without a further request.

A new authenticated phone connection opens Settings at Remote control with a highlighted PPT file-browsing permission and Apply instructions. It never grants permission automatically, preserves draft edits, and does not repeat on polling or reconnection during the service session. The existing permission still applies to devices holding the current Remote URL.

The timer keeps its font sizes and fixed left/right metadata arrangement. Row gap is 0 DIP, metadata separation 4 DIP, horizontal padding totals 12 DIP and vertical padding totals 4 DIP. Manual width/height remains unchanged.

Anchor placement and drag capture use actual window dimensions and full monitor bounds instead of a fixed 140x50 baseline. Top-center at zero aligns the real top edge; bottom-center aligns the real bottom edge. All nine anchors follow the same rule, including negative-origin screens and different DPI. Saved nonzero offsets are retained.

Validation run 34990592629: formatting, Clippy, 102 passed / 0 failed / 3 ignored, web syntax, optimized Windows build, real GUI captures and startup/Remote checks passed. Only its subsequent commit step failed because generated Markdown used CRLF. The exact cached executable is reused here without rebuilding or changing runtime inputs. Source is reconstructed from the identical patch and fixes and formatted with the same Rust 1.92.0 toolchain. Documentation is written with LF.

Package/source identity is in BUILD.txt and SOURCE.txt. Automated validation and software GUI renders are not physical phone/Office acceptance. This packaging workflow is the completed delivery record.
