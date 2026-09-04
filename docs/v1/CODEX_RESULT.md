# V1.06 窗口入口续修结果

日期：2026-09-04  
分支：`codex/v1-06-manual-test`

## 1. 本轮范围

本轮只处理用户反馈的“点击设置/远程控制时闪退或闪窗”问题，保留上一轮已经完成的 Timer 任务栏样式、跨屏 DPI 和 Remote“演示文稿”页面修复，不扩展其它功能。

## 2. 闪退 / 闪窗实际原因

旧实现从 100 ms 的桌面事件轮询回调里同步调用窗口显示辅助函数。辅助函数又同步调用 `slint::platform::update_timers_and_animations()`，造成 Slint 事件循环重入；这正是点击设置或远程控制后进程直接退出、窗口轮廓短暂出现的高风险路径。

同时，窗口尚未创建 HWND 时先调用 Win32 隐藏是无效的；随后 `show()` 会立即让首个原生窗口可见，桌面合成器可能先显示未完成绘制的表面。

当前分支的修复为：

- 删除入口路径中的同步 `update_timers_and_animations()`；
- 设置和远程控制窗口的 `show()` 延迟到当前桌面回调返回之后；
- 原生窗口创建后先隐藏，下一次 Slint 定时器回调再显示；
- 已存在且可见的窗口只恢复可见，不再重复隐藏/显示，避免再次打开时闪烁。

对应提交：

- `02fb47cd54020c924756a58e8ac97ac43dd4117a` — 移除入口重入路径；
- `ec69f063f1515be3aad815c314317d986506f247` — 延后创建和首帧显示；
- `93f8f3a2f10714efd0751d3d73f7630c4be1dbae` — 已显示窗口不再重复闪烁。

## 3. Remote“演示文稿”页（上一轮保留）

仍复用现有 `PresentationService` 和 `PresentationCommand`，包含：

- 添加、删除、刷新、清空文稿列表；
- 多选文件添加；
- 文件名、规范化完整路径和大小写不敏感去重；
- 时长、倒计时/正计时、启用/禁用规则编辑与保存；
- 打开、从头放映、当前页放映、上一页、下一页、跳页；
- 黑屏、白屏、恢复、结束放映、关闭文稿和退出演示软件。

本轮没有建立第二套演示控制逻辑。

## 4. Timer 任务栏和跨屏修复（上一轮保留）

Timer/overlay/时间到覆盖窗口继续使用 ToolWindow/Popup 样式，不应进入任务栏；设置和远程控制窗口保持普通任务栏窗口。跨屏路径继续按实际 HWND DPI 重新同步物理尺寸和圆角 Region。

## 5. 主要修改文件

本轮新增修改：

- `src/app.rs`：设置/远程控制入口的非重入、延后显示和可见窗口复用。
- `docs/v1/CODEX_RESULT.md`：更新本轮原因、提交和验证状态。

上一轮窗口/Remote 修复涉及的文件仍见前一版报告：`src/window.rs`、`src/settings.rs`、`ui/app-window.slint`、`src/capture.rs`、`Cargo.toml`。

## 6. 构建与桌面验证状态

执行宿主恢复后，本机已重新完成：

- `cargo fmt --check`：通过；
- `cargo clippy --all-targets -- -D warnings`：通过；
- `cargo test`：通过，33 passed、1 ignored（既有 Office 真机 smoke test）；
- `cargo build --release`：通过。

新的 Release 可执行文件为：

`E:\快传\计时器\v1.0\target\release\FlyPPTTimer.exe`

版本为 `1.9.0`，文件大小 16,836,608 字节。

使用桌面控制和 Win32 窗口轮询直接启动 `--show-settings` 进行了验证。启动期间只观察到 Slint/Winit 的隐藏初始化窗口（`Window Class`，1280×745，Visible=False）和透明的 `Winit Thread Event Target`（15×15）；没有观察到可见的空白设置中间窗口。可见窗口为：

- Timer：类名 `Window Class`、无标题、100×35；
- 设置：标题“演讲计时器设置”、类名 `Window Class`、915×687。

两个 Timer HWND 的扩展样式均包含 `WS_EX_TOOLWINDOW`、`WS_EX_NOACTIVATE`、`WS_EX_LAYERED`，且不含 `WS_EX_APPWINDOW`；实测 DPI 120/144 时客户区仍为 100×35。设置窗口保留普通任务栏窗口样式。

使用 Release 的 headless capture 检查了 Remote 两页，`remote-connection.png` 与 `remote-presentation.png` 均正常绘制；演示文稿页包含文稿列表、完整路径、规则时长/模式/启用状态、添加/删除/刷新/清空和现有放映控制按钮。

## 7. 留给用户的下一步手测

- 从 Timer 右键打开设置和远程控制，确认真实托盘入口下不出现可见空白轮廓；
- 关闭后再次打开两个窗口，确认没有闪退或重复闪烁；
- 确认 Timer、overlay 和时间到覆盖窗口不出现在任务栏，设置/远程控制正常出现在任务栏；
- 在实际不同 DPI 显示器之间拖动 Timer，确认绘制和圆角保持正常。

本轮没有创建 Release 或 Tag，也没有上传 EXE/ZIP/Installer。

## 8. 1.9.0 测试版本编号

源码包版本已从 1.8.0 递增为 1.9.0（`Cargo.toml` 与 `Cargo.lock` 已同步）。本轮 Release 已在本机生成并完成启动、窗口样式和截图验证。

## 9. 当前 review 分支提交

本次报告更新后将提交并推送到 `codex/v1-06-manual-test`；不创建 Release 或 Tag。
