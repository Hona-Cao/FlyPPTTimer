"""Index original development stages without changing their dates or reports."""
from pathlib import Path
import re

root = Path('docs/development')
stages = root / 'stages'
records = root / 'records'
stages.mkdir(parents=True, exist_ok=True)
records.mkdir(parents=True, exist_ok=True)
history = Path('docs/DEVELOPMENT_HISTORY.md').read_text(encoding='utf-8')
index = [
    '# 开发与测试阶段文档 / Development-stage records',
    '',
    '[中文使用教程](../USER_GUIDE.zh-CN.md) · [English guide](../USER_GUIDE.en.md) · [完整时间线](../DEVELOPMENT_HISTORY.md)',
    '',
    '这里保存各阶段的问题、修改和验证记录。用户操作说明见上方教程。原始记录保持其所属阶段的范围和日期，不代表当前待办任务。',
    '',
    '## 逐阶段索引',
    '',
]
count = 0
for line in history.splitlines():
    if not re.match(r'^\| .+ \| 20\d\d-\d\d-\d\d \|', line):
        continue
    stage, date, commit, change = [part.strip() for part in line.strip('|').split('|')]
    count += 1
    name = f'stage-{count:02}.md'
    text = '\n'.join([
        f'# {stage}', '', '[阶段目录](../README.md)', '',
        f'日期（UTC）：{date}', '', f'实际提交：{commit}', '',
        '## 本阶段变化 / Changes', '', change, '',
        '## 记录入口', '',
        '[原始逐轮报告](../README.md#原始逐轮报告) · [完整时间线](../../DEVELOPMENT_HISTORY.md) · [提交索引](../../development-commits.tsv)', '',
    ])
    (stages / name).write_text(text, encoding='utf-8', newline='\n')
    index.append(f'- [{stage}](stages/{name}) — {date}')

index.extend(['', '## 原始逐轮报告', ''])
source = Path('docs/v1/CODEX_RESULT.md').read_text(encoding='utf-8')
parts = [part for part in re.split(r'(?m)(?=^# )', source) if part.strip()]
for number, part in enumerate(parts, 1):
    title = part.splitlines()[0].removeprefix('# ')
    name = f'report-{number:02}.md'
    text = '[记录目录](../README.md) · [原始汇总](../../v1/CODEX_RESULT.md)\n\n' + part.strip() + '\n'
    (records / name).write_text(text, encoding='utf-8', newline='\n')
    index.append(f'- [{title}](records/{name})')

index.extend([
    '', '## 独立交付与手测资料', '',
    '- [RC3.1](../v1/RC31_MANUAL_TEST.md)',
    '- [RC3.2](../v1/RC32_DELIVERY.md)',
    '- [RC3.3](../v1/RC33_MANUAL_TEST.zh-CN.md)',
    '- [RC3.4](../v1/RC34_SCOPE.md)',
    '- [v1.13.1](../v1/V1131_DELIVERY.md)',
    '- [本次教程整理](v1.13.1-guide-refresh.md)', '',
])
(root / 'README.md').write_text('\n'.join(index), encoding='utf-8', newline='\n')
print(f'Organized {count} original stages and {len(parts)} original reports.')
