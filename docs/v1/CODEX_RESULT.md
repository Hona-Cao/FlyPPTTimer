# FlyPPTTimer V1 — 窗口修复结果

日期：2026-09-03。状态：**窗口 Style 修复和构建完成；启动目视确认受工具故障阻塞，等待用户手工测试。**

## 版本与分支

- 本地目录：`E:\快传\计时器\v1.0`
- 当前 review 分支：`codex/v1-06-manual-test`（没有创建新分支）。
- 拉取的任务起点：`b19f4fca51983d1c66668ef355a753991e0b21c5`。
- 最终实现 commit SHA：`b5ecf5a0f54d8555944e67056ea0516c44a32087`。
- 本报告作为其后的独立文档提交；实现 SHA 对应 Release 的全部代码。包含本报告的最终 HEAD 可用 `git rev-parse origin/codex/v1-06-manual-test` 获取。
- 按用户要求顺延候选版本为 **1.07（1.7.0）**，不创建 Release 或 Tag。

## 修改文件

1. `src/window.rs`：仅修改普通 Timer 的原生 Style 设置及对应 SetWindowPos 标志。
2. `Cargo.toml`、`Cargo.lock`、`resources/app.manifest`：版本标识由 1.6.0 顺延到 1.7.0，无依赖变更。
3. `docs/v1/CODEX_RESULT.md`：记录本轮结果。

没有修改 Slint UI、Timer、设置、Remote、PPT/WPS、多屏、更新、Installer 或打包脚本。没有新增测试、验证工具或窗口框架。

## 实际 Style 修复

在 `apply_native_window()` 中：

- 读取当前 `GWL_STYLE`。
- 目标 Style 为：移除 `WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX`，加入 `WS_POPUP`，保留其他原有位。
- 默认位置标志仍为 `SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE`。
- **仅当目标 Style 与当前值不同**，写入 `GWL_STYLE`，并为本次 `SetWindowPos` 附加 `SWP_FRAMECHANGED`。
- Style 已符合目标时，透明度、置顶、穿透或设置应用引起的后续调用不会由此再次触发 `SWP_FRAMECHANGED`。
- 扩展样式继续保留 `WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`，移除 `WS_EX_APPWINDOW`，按配置切换 `WS_EX_TRANSPARENT`。

现有 Region 圆角、透明度及 TopMost 实现原样保留；`configure_timer_window()` 的 80ms 回调仍只刷新圆角。不修改 renderer，不向窗口添加内容或按钮。

## 构建和现有测试

| 命令 | 结果 |
| --- | --- |
| `cargo fmt --check` | 通过 |
| `cargo clippy --all-targets -- -D warnings` | 通过，无警告 |
| `cargo test` | 33 通过，0 失败，1 跳过 |
| `cargo build --release` | 通过 |

跳过项是原有 `manual_powerpoint_and_wps_com_connection`。没有启动 Office 或扩展测试。编译及测试进程已结束。

## Release EXE

- 路径：`E:\快传\计时器\v1.0\target\release\FlyPPTTimer.exe`
- 大小：**16,747,008 字节**。
- 这是本轮 **1.7.0** EXE。此前 `artifacts/release/v1.6.0` 的 Portable/Installer 未更新，不能用旧包检查本轮修复。
- 没有生成新的安装器或 ZIP，没有上传二进制，没有修改用户配置或删除旧产物。

## 启动检查与阻塞

本轮使用 Computer Use 技能初始化窗口检查。读取应用列表时，底层运行时返回：

```text
failed to launch codex app-server: 系统找不到指定的路径。 (os error 3)
```

因此**未能启动 Release 并目视确认时间显示、无标题栏和无图标**。没有把源码判断写成实测通过；没有用额外截图/GUI 工具绕过故障。本轮没有遗留启动的 Timer 窗口。

## 用户手测

先退出旧版本，再启动上面的 `target/release/FlyPPTTimer.exe`：

1. 确认只有时间与配置背景，无标题栏、程序图标及最小化/最大化/关闭按钮，不出现在任务栏。
2. 点击、拖动、弹出并收起右键菜单，检查遮罩/残影。
3. 应用透明度、置顶相关配置、锁定和鼠标穿透后，检查窗口框架没有重新出现。
4. 在此前复现的 DPI 下检查圆角及时间显示。

本轮仅处理当前窗口回归。其他已知差异不在本轮推进。推送当前 review 分支后停止，等待手测和审核。
