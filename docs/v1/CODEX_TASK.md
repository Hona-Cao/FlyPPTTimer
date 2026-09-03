# FlyPPTTimer V1 — 当前 Codex 任务

状态：**待执行**  
基线：`docs/v1/V1_BASELINE_CHECKLIST.md`  
当前分支：`codex/v1-06-manual-test`

本轮只处理计时窗口右键后出现遮罩/残影的问题，不推进其它功能。

## 已知复现条件

用户确认：**异常是在计时器窗口上右键时出现的。**

因此不要再把重点放在“启动后窗口 Style 是否正确”。必须直接复现并观察这条路径：

`计时窗口右键 → 弹出右键菜单 → 菜单关闭/选择 → 计时窗口异常`

当前实现的右键路径是：

`AppWindow TouchArea → desktop.show_timer_menu() → 隐藏的 Desktop helper HWND → SetForegroundWindow(helper hwnd) → TrackPopupMenu(..., helper hwnd, ...)`

这条路径是本轮重点审查对象，尤其是 `SetForegroundWindow`、菜单 owner HWND、前台窗口切换以及菜单结束后的窗口状态。

## 1. 直接控制桌面复现

使用当前可用的桌面控制能力启动 Release，并在**真正的计时器窗口上右键**。

先确认：

- 不右键时窗口是否正常显示时间和纯色背景。
- 第一次右键后是否出现遮罩/残影。
- 是菜单弹出时出现，还是菜单消失后出现。
- 点击菜单项与点击空白处关闭菜单，结果是否相同。

不要用启动截图代替这个复现过程。

## 2. 监测右键前后发生了什么变化

只做临时的一次性诊断，比较三个时刻：

1. 右键前
2. `TrackPopupMenu` 正在显示时
3. `TrackPopupMenu` 返回后

至少记录普通 Timer HWND 的：

- `GWL_STYLE`
- `GWL_EXSTYLE`
- `GetWindowRect`
- `GetClientRect`
- 当前 foreground HWND

同时记录 Desktop helper HWND 和 Timer HWND，明确菜单实际由哪个 HWND 拥有、`SetForegroundWindow` 前后 foreground HWND 变成了谁。

如果圆角 Region 也发生变化，再记录 Region 边界；如果没有变化，不继续扩大诊断范围。

临时日志只用于定位，最终提交前删除。

## 3. 优先隔离右键菜单实现

不要先继续修改 Timer 的 Style。

做最小 A/B：

### A

保持计时窗口其它实现不变，临时让右键回调**不弹菜单**。

如果反复右键不再出现异常，说明问题就在原生菜单路径，而不是 Timer 本身。

### B

恢复菜单，然后分别确认以下哪一步首次导致异常：

- `SetForegroundWindow(helper hwnd)`
- `TrackPopupMenu` 使用隐藏 helper HWND 作为 owner
- `TrackPopupMenu` 返回后的处理

以实际复现结果决定修法，不预设根因。

重点检查：当前菜单由隐藏 Desktop helper window 所有，而不是被右键的 Timer window；这是否导致 foreground/activation/non-client 状态在菜单结束后发生异常。

## 4. 修复要求

只修实际定位出的右键菜单问题。

最终 Timer 仍必须：

- 只有时间和纯色背景
- 无标题栏、图标和系统按钮
- 不占任务栏
- 右键菜单保留 v0.30.2 的项目和功能
- 右键菜单弹出和关闭后窗口外观完全不变

如果需要调整菜单 owner、foreground 处理或菜单命令返回方式，可以直接改 `desktop.rs` / 右键接线；不要为了修右键问题继续反复改 Timer `GWL_STYLE`。

不要留下新的诊断入口或专用测试工具。

## 5. 完成后

运行现有：

- `cargo fmt --check`
- `cargo clippy --all-targets -- -D warnings`
- `cargo test`
- `cargo build --release`

然后再次用桌面控制实际执行：

`启动 → 右键 Timer → 关闭菜单 → 再右键数次`

确认结果。

更新 `docs/v1/CODEX_RESULT.md`，重点写清：

1. 右键前 / 菜单中 / 菜单后的关键窗口状态变化
2. 首个导致异常的具体调用或状态变化
3. 最终修复方式
4. 实际右键复现结果
5. Release EXE 本地路径和大小
6. 最终 commit SHA

提交并推送当前 review 分支后停止，等待用户手工测试和 ChatGPT 审核。不要创建 Release 或 Tag。
