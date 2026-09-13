[记录目录](../README.md) · [原始汇总](../../v1/CODEX_RESULT.md)

# 设置内容区、Remote 间距与文稿文件过滤修复

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`

源码提交：`62e1829`

## 本轮触发

用户反馈：设置页固定底栏虽然已透明，但滚动内容仍延伸到按钮下面；Remote 文件列表和时长/模式/保存编辑区需要留出明确间距；文件规则操作按钮过高、过宽且缺少间隔；添加相同文件应自动去重，文件类型只应允许幻灯片文件和 PDF。

## 实际修改

- 设置页滚动视口改为按窗口根高度计算，并预留固定底栏高度与下边距，内容不会再被“确定 / 取消 / 应用”按钮覆盖；底栏继续透明、无边框。
- 设置文件规则操作按钮固定为 34px 高，按文字长度使用紧凑宽度并保留 8px 间距；选中文件的时长/模式详情卡收紧为 64px 高。
- Remote 演示文稿规则列表底部与编辑卡之间增加 16px 间距，避免列表、编辑控件和底部操作区粘连。
- 添加文件对 `.ppt`、`.pptx`、`.pptm` 和 `.pdf` 建立统一白名单，移除“所有文件”过滤入口；设置页和 Remote 页都用规范化完整路径判断重复文件，重复选择不会再次添加。

## 验证

- `cargo fmt --all -- --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：41 passed、0 failed、1 ignored（既有 Office COM 手测测试；新增文件类型过滤测试通过）。
- `cargo build --release`：通过，生成 `target/release/FlyPPTTimer.exe`。
- Release `--capture-settings` / `--capture-windows`：通过；截图确认设置操作按钮已收紧并留有间距，设置底栏不再绘制背景或边框，Remote 页面列表布局保持稳定。

自动截图不能模拟真实文件拖放、重复选择和多屏鼠标操作；目标设备仍需手测确认格式过滤、重复文件和跨 DPI 窗口行为。

## 本地手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX10-LayoutFileFilter-20260909`。便携目录：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX10-LayoutFileFilter-20260909-win-x64/`，直接运行其中的 `FlyPPTTimer.exe`，不要求解压或安装；这是 review 手测版，不是正式 Release/Tag。完成推送后停止编码，等待审核。

---
