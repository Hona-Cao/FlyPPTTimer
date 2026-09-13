# v1.13.1 public-release handoff

## Published on 2026-09-13

The user accepted v1.13.1 and authorized publication. The public GitHub Release is now published, marked Latest, not a draft and not a prerelease:
https://github.com/Hona-Cao/FlyPPTTimer/releases/tag/v1.13.1

Release/tag source: d96dd975e6fc1a10358d219cb0379940efc54706.
Main was fast-forwarded to that commit through the authorized GitHub connector, preserving the accepted development ancestry. Subsequent documentation-only commits do not change the released executable or move the tag.

Accepted executable source: e625c809f8cf7515f0143fab476bb4467d2fe1d7.
Accepted Windows CI: 34709344878 (90 passed, 0 failed, 3 explicitly ignored).
Publication workflow: 34729800120, both documentation and package-and-publish jobs succeeded.

## Assets and installation

Exactly two manually uploaded Release assets:
- FlyPPTTimer-v1.13.1-portable-win-x64.zip
- FlyPPTTimer-v1.13.1-setup-win-x64.zip

No checksum files were uploaded. GitHub's automatic Source code archives are not application packages. Both editions retain the exact accepted EXE and include the required application-local VC runtime DLLs.

The hosted Windows publication job successfully installed the setup edition, checked the installed EXE against the accepted application, verified all three runtime DLLs, reinstalled over an existing configuration without changing it, and started the installed application. These are automated installation checks, not claims of new physical-phone or Office acceptance.

## Documentation and history

README.md and README.zh-CN.md now describe v1.13.1. The English and Chinese complete guides, build instructions, changelog and screenshot provenance are included in the repository and packages.

25 current GUI PNGs are checked in: 17 desktop renders and eight mobile browser renders, covering Chinese/English and light/dark examples. The desktop documentation-only CJK renderer is disposable and does not replace the released program. No font files or temporary renderer executable are distributed.

The development history links 29 genuine milestones. docs/development-commits.tsv indexes 198 actual accepted-chain development/review/build commits, retaining original authors and timestamps. Historical RC notes are preserved and are not recast as previously published Releases.

## Discovery metadata: permission-limited

The rewritten bilingual titles, feature descriptions, image alt text, guides and navigational links are published. Updating the repository About description/homepage and Topics was attempted but both API writes returned HTTP 403, Resource not accessible by integration. They were NOT applied.

The repository owner can copy the proposed values from .github/repository-discovery.json into the repository About editor. Managed connector/workflow tokens do not have repository administration access; do not request secrets or invent successful metadata updates. Search-engine indexing and ranking have not been promised or verified.

## Boundaries for the next task

Read current GitHub state before modifying it. Do not rerun obsolete RC refinement scripts, squash the preserved history, merge unrelated experimental branches or move the v1.13.1 tag.

The application updater still uses Gitee. This task published GitHub only; no Gitee Release or update-channel change was made. Future product changes require a new task and version.
