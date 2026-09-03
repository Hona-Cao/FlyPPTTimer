# V1.06 手工测试修复结果

日期：2026-09-03
分支：`codex/v1-06-manual-test`

## 1. 设置 / 远程控制闪窗

使用 Release 测试版在真实桌面启动 `--show-settings`，直接观察到的设置窗口只有一个：

- 标题：`演讲计时器设置`
- 类名：`Window Class`
- 最终 HWND 为普通 Slint/Winit 窗口，尺寸随当前 DPI 正常调整

窗口创建后，旧路径会先 `show()`，首帧尚未完成就被桌面合成器显示，随后才绘制完整内容。现在设置和远程控制入口都先创建窗口、隐藏原生 HWND、让 Slint 完成首帧更新，再显示窗口。实桌面检查中设置窗口只出现完整表面，没有可见中间空白窗口；远程入口使用同一显示顺序，未增加额外窗口或验证入口。

## 2. 远程控制“演示文稿”页

对照 v0.30.2 `RemoteControlForm` 恢复了原有页面内容：

- 添加、删除、刷新、清空列表
- 使用原有 Windows 多选文件对话框，一次可添加多个文稿
- 列表显示文件名和规范化完整路径，路径大小写不敏感去重
- 文稿规则的时长、倒计时/正计时、启用/禁用编辑和保存
- 打开文稿、从头放映、当前页放映、上一页、下一页、跳转
- 黑屏、白屏、恢复、结束放映、关闭当前文稿、关闭最后打开的文稿、退出演示软件

仍复用 `PresentationService` 和现有 `PresentationCommand`，没有建立第二套演示状态或命令协议。页面改为 620 DIP 高，以免规则编辑器和放映控制被旧的 510 DIP 保存尺寸裁切。

## 3. Timer 任务栏行为

原因是原生 Winit 窗口在 `show()` 后仍保留 overlapped/AppWindow 样式，任务栏据此创建按钮；仅修改扩展样式但不通知窗口管理器也可能继续保留旧状态。

现在在首个可用 HWND 上应用 `WS_POPUP | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`，移除 `WS_EX_APPWINDOW`，并在 Style/ExStyle 发生变化时带 `SWP_FRAMECHANGED`。显示器 overlay、时间到窗口都沿用这条路径。

Release 实测 Win32 结果：

- Timer：类 `Window Class`，Style `0x96000000`，ExStyle `0x08080098`，无 `WS_EX_APPWINDOW`
- 设置：类 `Window Class`，Style `0x16ca0000`，ExStyle `0x00040110`，保持普通任务栏窗口

## 4. 跨屏空白 / 尺寸异常

原因是多屏使用不同 DPI 时，Slint 的逻辑尺寸、Winit 的物理客户区和圆角 Region 可能不同步。计时器窗口现在每次显示更新都读取实际 HWND 的 `GetDpiForWindow`，按当前 DPI 将配置逻辑宽高换算为物理像素，并重新应用圆角 Region；初始化和跨屏后的首个事件循环也会重复定位、调整样式和 Region。

真实桌面在 144 DPI 屏幕上测得配置 `100×35` 的 Timer HWND 物理矩形为 `150×53`，与 144/96 比例一致；窗口样式、定位和圆角刷新均在同一更新路径完成。跨屏热插拔和从高 DPI 屏拖回低 DPI 屏仍请在用户环境手工复核。

## 5. 主要修改文件

- `src/app.rs`：窗口首帧显示顺序、Remote 文稿规则管理、Timer DPI 同步
- `src/window.rs`：ToolWindow/Popup 样式、`SWP_FRAMECHANGED`、DPI 物理尺寸、Remote 窗口最小高度
- `src/settings.rs`：复用原有多选文稿文件对话框
- `ui/app-window.slint`：Remote“演示文稿”页的规则工具栏、编辑区和完整控制布局
- `src/capture.rs`：Remote 页面捕获尺寸及示例规则字段
- `Cargo.toml`：启用 `windows-sys` HiDPI API

未修改 `v0.30.2`、`agent/v4-foundation`，没有新增 SHA-256、哈希校验或额外测试框架。

## 6. 构建与测试

- `cargo fmt --check`：通过
- `cargo clippy --all-targets -- -D warnings`：通过
- `cargo test`：33 通过，0 失败，1 忽略（原有 Office COM 手工测试）
- `cargo build --release`：通过
- Remote 页面捕获：通过；中文页面 700×620，列表、路径、规则信息和放映控制无重叠

## 7. 本地测试版

- 版本：`1.8.0`（测试文件名 `v1.08`）
- Release EXE：`E:\快传\计时器\v1.0\target\release\FlyPPTTimer.exe`
- 手工测试副本：`E:\快传\计时器\v1.0\artifacts\test-v1.08\FlyPPTTimer-v1.08-test.exe`
- 大小：16,837,120 字节

测试副本未上传 GitHub，没有创建 Release 或 Tag。

## 8. 留给用户的手工复核

请在同一测试副本上复核：

- 从 Timer 右键菜单打开设置、远程控制，确认没有空白/半绘制闪窗
- Timer、overlay 和时间到覆盖窗口均不出现在任务栏；设置和远程控制正常出现在任务栏
- 在不同 DPI 的两块屏幕之间拖动 Timer，确认文字、背景、圆角和位置正常
- 远程“演示文稿”页的多选添加、规则编辑保存、放映控制

最终提交 SHA 将在本文件对应提交完成后补入 Git 历史，不创建 Release 或 Tag。
