"""Promote the existing v1.14.1 packages; update online docs only."""
from pathlib import Path
import re

TAG = 'v1.14.1'
SOURCE = '6e8bd3aeaec7ae8c247e09b535157db7f490047a'
GH = 'https://github.com/Hona-Cao/FlyPPTTimer'
GITEE = 'https://gitee.com/hona-cao/fly-ppttimer'


def write(path, text):
    Path(path).write_text(text.rstrip() + '\n', encoding='utf-8', newline='\n')


for name in ['README.md', 'README.zh-CN.md']:
    s = Path(name).read_text(encoding='utf-8')
    s = s.replace('**Guide updated for v1.14.1; latest public Release remains v1.14.0.**', '**Current public release: v1.14.1.**')
    s = s.replace('**教程已更新至 v1.14.1；公开 Release 仍为 v1.14.0。**', '**当前正式版：v1.14.1。**')
    if name.endswith('zh-CN.md'):
        s = re.sub(r'(?m)^以下直链.*$', '以下下载均为 **v1.14.1**：直接发布之前已交付的便携版和安装版 ZIP，没有重新编译或重新打包。网上图文教程已更新；为保持安装包原样，包内文档保留构建时的版本。', s)
        s = s.replace('**已经拿到 v1.14.1 的用户，按下面的新流程操作。**', '**v1.14.1 用户按下面的新流程操作。**')
    else:
        s = re.sub(r'(?m)^The public links below.*$', 'Both downloads provide **v1.14.1**: the exact previously delivered portable/setup ZIPs, without rebuilding or repackaging. Online guides are current; documentation inside the unchanged archives reflects their original build.', s)
        s = s.replace('**These changes apply to the delivered v1.14.1 build.**', '**These changes are included in v1.14.1.**')
    s = s.replace('/releases/download/v1.14.0/', '/releases/download/v1.14.1/')
    s = s.replace('FlyPPTTimer-v1.14.0-', 'FlyPPTTimer-v1.14.1-')
    s = s.replace(GITEE + '/releases)', GITEE + '/releases/tag/v1.14.1)')
    write(name, s)

for name in ['docs/USER_GUIDE.en.md', 'docs/USER_GUIDE.zh-CN.md']:
    s = Path(name).read_text(encoding='utf-8')
    if name.endswith('zh-CN.md'):
        s = re.sub(r'(?m)^教程对应.*$', '教程对应 **v1.14.1 正式版 · Windows 10 / 11 x64**。[下载与安装](../README.zh-CN.md#download)。', s)
        s = re.sub(r'(?m)^这一节对应.*$', '本节对应 **v1.14.1 正式版**。在“设置 → 其他设置”确认实际运行版本；旧版用户可以从首页的 GitHub 或 Gitee 发布入口升级。', s)
        s = re.sub(r'(?m)^先看.*公开.*v1\.14\.0.*$', '先从[下载与安装](../README.zh-CN.md#download)选择当前 v1.14.1 的便携版或安装版。', s)
    else:
        s = re.sub(r'(?m)^For the \*\*delivered v1.14.1.*$', 'For **v1.14.1 public release / Windows 10 and 11 x64**. [Download and install](../README.md#download).', s)
        s = re.sub(r'(?m)^This section describes.*$', 'This section describes the public v1.14.1 release. Check your actual version in Settings before following the new permission workflow.', s)
        s = re.sub(r'(?m)^Check the \[version note\].*$', 'Download the current v1.14.1 portable or setup ZIP from the [download links](../README.md#download).', s)
        s = s.replace('Latest public Release remains v1.14.0.', 'The public release is v1.14.1.')
    write(name, s)

p = Path('docs/RELEASE_NOTES_v1.14.1.md')
s = p.read_text(encoding='utf-8')
s = re.sub(r'^#.*', '# FlyPPTTimer v1.14.1', s, count=1)
s = re.sub(r'(?m)^\*\*状态.*$', '**v1.14.1 正式发布。** 直接发布之前已交付的两个 ZIP，不重新编译、不重新打包。 The exact previously delivered portable/setup ZIPs are published unchanged.', s)
s = re.sub(r'(?m)^This build is delivered on the review branch.*$', '', s)
s += f'''

## Downloads / 下载

- [Portable ZIP / 便携版]({GH}/releases/download/{TAG}/FlyPPTTimer-{TAG}-portable-win-x64.zip)
- [Setup ZIP / 安装版]({GH}/releases/download/{TAG}/FlyPPTTimer-{TAG}-setup-win-x64.zip)
- [Gitee release / 国内下载]({GITEE}/releases/tag/{TAG})

完整解压便携包后运行程序；安装版先解压，再运行其中的安装器。升级前退出旧版，保留自己的配置和提示音。只上传这两个 ZIP，不上传 SHA256 附件。GitHub 自动生成的 Source code 是源码，不是运行程序。

Extract the entire portable archive, or extract the setup archive and run its installer. Exit the old program and preserve personal configuration/sounds. Only these two ZIP assets are uploaded, without checksum attachments.

Application source: `{SOURCE}`. Existing validation: 102 passed, 0 failed, 3 ignored; packaging run 34992459836. Publication changes no runtime and performs no new compilation. Package documentation is retained byte-for-byte; use the online guides for current illustrated instructions.

[中文图文教程]({GH}/blob/{TAG}/docs/USER_GUIDE.zh-CN.md) · [English guide]({GH}/blob/{TAG}/docs/USER_GUIDE.en.md)
'''
write(str(p), s)

p = Path('CHANGELOG.md'); s = p.read_text(encoding='utf-8')
s = re.sub(r'(?m)^## 1\.14\.1 .*$', '## 1.14.1 — 2026-09-16（正式发布 / public release）', s)
s = re.sub(r'(?m)^- 本次仓库文档同步.*$', '- 发布之前已交付的原始便携／安装 ZIP，更新下载链接，并修复独立的 Gitee Release 附件同步；不重新构建、不改写历史标签。 Publish the existing ZIPs unchanged and repair Gitee release synchronization.', s)
write(str(p), s)
p = Path('docs/DEVELOPMENT_HISTORY.md'); s = p.read_text(encoding='utf-8')
s = s.replace('This is a delivered branch build, not a new public Release. The documentation refresh preserves the public v1.14.0 tag/packages and main\'s application source. Source dates and original validation results are retained.', 'Delivered on 2026-09-15 and authorized for public release on 2026-09-16. The same ZIPs are published without rebuilding. Implementation and later illustrated documentation retain their real ancestry; previous releases stay unchanged.')
write(str(p), s)
p = Path('docs/BUILDING.md'); s = p.read_text(encoding='utf-8')
s = s.replace('v1.14.0', 'v1.14.1').replace('v1140-default-config', 'v1141-default-config').replace('34709344878', '34992459836').replace('e625c809f8cf7515f0143fab476bb4467d2fe1d7', SOURCE)
s = s.replace('For the first public v1.14.1 release, packaging deliberately reuses', 'For public v1.14.1, publication reuses the existing ZIPs without repackaging. Their original packaging reused')
write(str(p), s)
write('docs/v1/HANDOFF.md', f'''# v1.14.1 public-release handoff

The user explicitly authorized GitHub and Gitee publication of the existing v1.14.1 ZIPs on 2026-09-16, current download links, and repair of release synchronization. This supersedes prior follow-up/documentation-only release holds. Do not rebuild or repackage the application.

Executable source: {SOURCE}. Original package run: 34992459836. Validation: 102 passed, 0 failed, 3 ignored. Main integrates that real implementation ancestry and the later illustrated documentation. Existing tags/releases remain unchanged.

Public assets must be the original portable/setup ZIPs. No checksum assets or font files. Online documentation can be newer than the documentation inside these unchanged packages.

Gitee requires separate release metadata and attachment uploads; git mirroring does not copy them. The previous workflow was manual-only and expected an installer EXE. The repaired workflow accepts the actual two ZIPs and publication invokes it explicitly. GITEE_TOKEN stays in Actions secrets, not repository files or logs.

Read artifacts/publication and current remote release state for the actual GitHub/Gitee result. A prepared script does not establish successful uploading. No new runtime testing is claimed for this publication-only task.
''')
write('docs/v1/CODEX_TASK.md', '# Current task: publish existing v1.14.1 packages\n\nExplicit user authorization covers GitHub and Gitee release publication and current links. Do not change runtime code or rebuild. See HANDOFF.md.\n')
for name in ['docs/v1/APPROVED_PRODUCT_DEVIATIONS.md', 'docs/v1/CODEX_RESULT.md']:
    s = Path(name).read_text(encoding='utf-8')
    s += '\n\n## 2026-09-16: existing v1.14.1 publication\n\nThe user authorizes publishing the previously delivered v1.14.1 ZIPs to GitHub/Gitee, repairing release synchronization, and updating download links. No runtime change, new compilation or repackaging. Actual outcomes are recorded by the publication workflow.\n'
    write(name, s)
Path('artifacts/publication').mkdir(parents=True, exist_ok=True)
body = Path('docs/RELEASE_NOTES_v1.14.1.md').read_text(encoding='utf-8')
for lang in ['en', 'zh-CN']:
    body = body.replace(f'](USER_GUIDE.{lang}.md', f']({GH}/blob/{TAG}/docs/USER_GUIDE.{lang}.md')
write('artifacts/publication/release-body.md', body)
print('Updated release links and bilingual notes; no ZIP files modified.')
