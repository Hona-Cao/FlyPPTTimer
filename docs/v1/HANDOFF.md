# v1.14.1 public-release handoff

The user explicitly authorized GitHub and Gitee publication of the existing v1.14.1 ZIPs on 2026-09-16, current download links, and repair of release synchronization. This supersedes prior follow-up/documentation-only release holds. Do not rebuild or repackage the application.

Executable source: 6e8bd3aeaec7ae8c247e09b535157db7f490047a. Original package run: 34992459836. Validation: 102 passed, 0 failed, 3 ignored. Main integrates that real implementation ancestry and the later illustrated documentation. Existing tags/releases remain unchanged.

Public assets must be the original portable/setup ZIPs. No checksum assets or font files. Online documentation can be newer than the documentation inside these unchanged packages.

Gitee requires separate release metadata and attachment uploads; git mirroring does not copy them. The previous workflow was manual-only and expected an installer EXE. The repaired workflow accepts the actual two ZIPs and publication invokes it explicitly. GITEE_TOKEN stays in Actions secrets, not repository files or logs.

Read artifacts/publication and current remote release state for the actual GitHub/Gitee result. A prepared script does not establish successful uploading. No new runtime testing is claimed for this publication-only task.
