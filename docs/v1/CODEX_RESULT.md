# FlyPPTTimer V1 — 当前窗口右键遮罩审计结果

日期：2026-09-03
分支：`codex/v1-06-manual-test`
实现 commit：`e2c156395bbe154c7d768e4660028a35dadae341`
任务：只审计并修复计时器窗口右键后出现的系统标题栏/遮罩残影。

## 监测结果

直接在 Release 计时器窗口上执行了右键、菜单选择和菜单关闭。一次性诊断记录如下：

| 时刻 | Timer 状态 |
| --- | --- |
| 右键前 | HWND `0x2460fa4`；foreground 为 Timer；Style `0x16ca0000`；ExStyle `0x00040118`；WindowRect `(1205,18)-(1355,71)`；ClientRect `150x53` |
| `SetForegroundWindow(helper)` 后 | foreground 变为隐藏 Desktop helper `0x1d9001e`，Timer 的 Style、ExStyle 和几何尺寸未变；遮罩首次出现 |
| `TrackPopupMenu` 返回后 | foreground 可能变为 `0`/其他窗口，Timer 的 Style、ExStyle 和几何尺寸仍未变；系统标题栏残影持续存在 |

禁用整个菜单路径时连续右键均无异常；只调用 `SetForegroundWindow(helper)`（不显示菜单）即可复现。因此问题不是 Slint Timer 内容，也不是尺寸或 Style 被改写。

进一步记录到每次菜单路径都会向 Timer 发送 `0x00AE`（`WM_NCUAHDRAWCAPTION`，Windows UAH 主题标题栏绘制），并伴随 `WM_NCACTIVATE`。这两条绘制路径就是遮罩的直接来源。

## 最终修复

只修改两个源码文件：

- `src/window.rs`：在 Timer 原生 HWND 创建后安装一个轻量窗口子类；丢弃实际命中的 `WM_NCUAHDRAWCAPTION`，并将 `WM_NCACTIVATE` 交给 `DefSubclassProc` 时使用 `lParam=-1`，避免系统非客户区重绘。窗口样式、圆角、透明度、置顶和菜单项目均未改动。
- `src/app.rs`：在现有 80ms Timer 窗口初始化回调中安装上述处理。

没有保留 A/B 诊断日志、后台审计线程、第二套菜单、第二套 Timer 或新的窗口框架。`desktop.rs` 已恢复为原有菜单 owner/项目/命令路径。

## 实际验证

使用 `E:\快传\计时器\v1.0\target\release\FlyPPTTimer.exe`（临时将被忽略的 Release 配置 `Placement.Visible` 设为 `true`，测试后恢复原值）验证：

- 启动后 Timer 正常显示 `10:00`。
- 连续三次在真实 Timer 窗口右键；菜单显示正常。
- 两次选择菜单项、一次执行“重置计时窗口位置”；菜单关闭后每次都恢复为纯净的 `10:00`，没有留下标题栏、图标、系统按钮或渐变遮罩。
- 选择菜单项后的 Timer 截图均保持纯色背景和时间显示；弹出菜单覆盖 Timer 的瞬间属于菜单本身的正常覆盖。
- 测试结束后已关闭我启动的进程，没有遗留 FlyPPTTimer 进程。

## 构建

- `cargo fmt --check`：通过
- `cargo clippy --all-targets -- -D warnings`：通过
- `cargo test`：33 通过，0 失败，1 忽略（原有 Office 手工 COM 测试）
- `cargo build --release`：通过
- Release EXE：`E:\快传\计时器\v1.0\target\release\FlyPPTTimer.exe`，16,748,544 字节

本轮没有修改 `v0.30.2`、`agent/v4-foundation`、配置模型、Remote、PPT/WPS 或其他产品行为；没有创建 Release 或 Tag。应用服务器路径问题的本地修复（旧 `CODEX_CLI_PATH` 指向缺失版本目录）通过目录 junction 解决，不属于项目源码变更。

## 待用户手工复核

请使用同一个 Release EXE，在你的实际配置下重复：右键打开菜单 → 选择/关闭菜单 → 再右键数次，并确认 Timer 始终只有时间和背景。当前任务只处理这一遮罩回归；其他窗口、菜单和功能差异不在本轮扩大范围内。
