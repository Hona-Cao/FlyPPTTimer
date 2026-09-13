[记录目录](../README.md) · [原始汇总](../../v1/CODEX_RESULT.md)

# 设置窗口显示与 Remote 文件规则修复

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`

源码提交：`7c039a8`

## 本轮触发

用户反馈：说明文字收进按钮后没有分段和首行缩进；设置窗口反复打开和关闭后偶尔只剩空白内容；不同页面的色块圆角和背景边界不统一；Remote 文件规则列表无法可靠选中文件并编辑参数。

## 实际修改

- 说明弹窗统一规范化段落：按空行分段、每段首行缩进，并按实际换行后的首选高度调整弹窗和滚动区域，长文本不会挤成一团。
- 设置窗口在已存在时强制恢复显示、置前并请求重绘；隐藏且没有未保存修改的旧设置实例会被安全重建，避免反复打开/关闭后复用失效的原生 surface。Remote 窗口也在显示前后执行同样的置前和重绘流程。
- 桌面管理窗口统一使用浅蓝灰画布、白色面板、统一边框和 8px/12px 圆角；设置侧栏、底部操作栏、字段行、规则卡片和 Remote 规则行都明确设置圆角和边界，避免纯白背景与相邻区域融合。
- Remote“演示文稿”规则行补齐整行点击区域和选中边框，点击后可编辑对应规则的时长与模式；列表继续复刻设置页的规则数据和选中状态。

## 验证

- `cargo fmt --all -- --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：37 passed、0 failed、1 ignored（既有 Office COM 手测测试）。
- `cargo build --release`：通过。
- `cargo run -- --capture-settings target/capture-settings-current2` 和 `cargo run -- --capture-windows target/capture-current2`：通过；设置页显示浅蓝灰画布、白色圆角侧栏/字段/底部栏，Remote 规则页显示圆角列表和选中边框，旧演示控制按钮未回归。

自动截图验证了排版和重绘路径，但当前 Computer Use 无法稳定取得 Release 原生窗口的进程句柄，因此反复开关、跨屏瞬间以及真实鼠标 Ctrl/Shift 选择仍需用户在目标设备上复核。

## 本地手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX07-SettingsDisplay-20260909`。便携目录：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX07-SettingsDisplay-20260909-win-x64/`，直接运行其中的 `FlyPPTTimer.exe`，不要求解压或安装；包内使用默认配置，不携带临时调试配置或用户日志。这是 review 手测版，不是正式 Release/Tag。完成推送后停止编码，等待审核。

---
