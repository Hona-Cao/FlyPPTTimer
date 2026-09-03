# FlyPPTTimer V1 — 当前 Codex 任务

状态：**待执行**  
基线：`docs/v1/V1_BASELINE_CHECKLIST.md`  
当前分支：`codex/v1-06-manual-test`

本轮只修复计时窗口，不推进其他功能。

## 目标

计时窗口必须恢复为干净的悬浮计时器：

- 只显示时间文字和配置的纯色背景
- 无标题栏
- 无程序图标
- 无最小化 / 最大化 / 关闭按钮
- 不出现在任务栏
- 保留置顶、拖动、右键菜单、圆角、透明度和鼠标穿透

用户当前截图中的小程序图标/工具窗口框架是回归，不能保留。

## 根因与修复方向

当前 `AppWindow` 在 Slint 中已有 `no-frame: true`，但最新修复删除了普通 Timer 的 `GWL_STYLE` 清理，只保留 `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`。在当前 Windows/winit 组合下，底层 Style 仍可能带有非客户区，因此 Windows 显示出了工具窗口标题栏和程序图标。

恢复普通 Timer 的原生无框 Style，但不要恢复之前每次调用都强制 `SWP_FRAMECHANGED` 的做法。

在 `src/window.rs::apply_native_window()` 中：

1. 读取 `GWL_STYLE`。
2. 移除 `WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX`。
3. 加入 `WS_POPUP`。
4. 只有当 Style 实际发生变化时才 `SetWindowLongPtrW(GWL_STYLE, ...)`，并在这一次 `SetWindowPos` 中附加 `SWP_FRAMECHANGED`。
5. 后续仅因透明度、置顶、穿透或设置应用再次调用时，如果 Style 已经是目标值，不再触发 `SWP_FRAMECHANGED`。
6. 扩展样式继续保留 `WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`，移除 `WS_EX_APPWINDOW`，按配置切换 `WS_EX_TRANSPARENT`。
7. 保留现有圆角 Region、透明度和 TopMost 实现，不重做 renderer 或窗口架构。

核心原则：普通 Timer 最终必须是 `WS_POPUP` 风格的无框窗口，而不是普通窗口或工具窗口标题栏。

## 本轮范围

不要处理更新、Installer、Remote、PPT/WPS、多屏或其他基线差异。本轮只修这个窗口回归。

完成后只需：

- `cargo fmt --check`
- `cargo clippy --all-targets -- -D warnings`
- `cargo test`
- `cargo build --release`
- 启动一次 Release，确认窗口能显示时间且没有原生标题栏/图标

真实点击、拖动、右键、DPI 和残影由用户手工测试，不为此新增测试或工具。

## GitHub 结果

完成后更新 `docs/v1/CODEX_RESULT.md`，写明：

- 修改文件
- 实际 Style 修复方式
- 构建和现有测试结果
- Release EXE 本地路径与大小
- 最终 commit SHA

提交并推送当前 review 分支后停止，等待用户手工测试和 ChatGPT 审核。不要创建 Release 或 Tag。
