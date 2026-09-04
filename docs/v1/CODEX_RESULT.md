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

本轮前一阶段报告提交为 `140936c`；本次布局修正提交为 `8d6eb16`，随后推送到 `codex/v1-06-manual-test`。不创建 Release 或 Tag。

## 10. 文件规则操作栏布局修正

后续手工复核发现，设置页“文件规则”的【添加文件】【删除】【清空】【批量设置】操作栏位于列表框上方。已将四个按钮移动到文件列表框下方，保持按钮顺序、启用条件和回调行为不变；选中规则的编辑区域仍位于操作栏之后。

本次测试版本递增为 `1.10.0`。Release 构建和设置页 headless 截图均已重新验证，截图显示列表框后紧接四个操作按钮。

## 11.1.0 设置窗口跨屏缩放修正

日期：2026-09-04
分支：`codex/v1-06-manual-test`

### 实际复现

在 1.10.0 Release 的真实双屏桌面上，设置窗口首次创建时客户区为 900×650；拖到低 DPI 屏后，Winit/Windows 会依据创建前的旧缩放状态重新换算尺寸，出现客户区变大、内容看起来被放大的现象。跨屏过程中窗口内容会被截断，容易误判为空白或布局损坏。

### 修正

- 设置窗口 `show()` 创建原生 HWND 后先保持隐藏；在下一次 Slint 定时器回调中重新提交 900×650 的逻辑客户区尺寸，再显示首帧。
- Remote 窗口沿用已有的 `WidthDip`/`HeightDip`（最小 700×620）计算，并在同一首帧时序中重新提交逻辑尺寸。
- 没有增加新的 DPI 抽象、固定物理像素或校验体系；Timer 核心、配置模型和现有多屏逻辑不变。

### 1.11.0 实测

使用 `target/release/FlyPPTTimer.exe --show-settings` 启动并拖动设置窗口经过主屏与低 DPI 扩展屏：

- 主屏首帧完整绘制，客户区 900×650；
- 完全移入扩展屏后仍完整绘制，截图保持约 900×650 的逻辑尺寸，Win32 客户区仍为 900×650、窗口 DPI 120；
- 返回/跨越边界过程中没有出现空白、比例跳变或内容遮挡。

测试时仅临时关闭 Release 配置中的 Remote 服务以避免 Windows 防火墙系统提示，测试结束后已恢复 `RemoteControl.Enabled=true`；没有修改用户源码、v0.30.2 或 `agent/v4-foundation`。

### 构建结果

- `cargo fmt --check`：通过；
- `cargo clippy --all-targets -- -D warnings`：通过；
- `cargo test`：33 passed、1 ignored（既有 Office 真机 smoke test）；
- `cargo build --release`：通过；
- Release EXE：`E:\快传\计时器\v1.0\target\release\FlyPPTTimer.exe`，16,837,632 字节；
- 版本：`1.11.0`（`Cargo.toml` 与 `Cargo.lock` 已同步）。

## 1.12.0 设置窗口可调整大小与混合 DPI 跨屏修复

日期：2026-09-04
分支：`codex/v1-06-manual-test`

### 修改内容

- 设置窗口改为 `preferred-width/height: 900px/650px`，并设置最小尺寸 `760px/520px`；保留原生装饰窗口的 `WS_THICKFRAME`，因此支持鼠标拖拽调整大小。
- 创建设置窗口时先提交明确的初始物理客户区尺寸，避免绑定设置模型后 Slint 的 preferred layout 异步调整覆盖原生尺寸。
- 重开设置窗口时读取当前 HWND 客户区物理尺寸并恢复，避免混合 DPI 下重复缩放。
- 增加轻量的设置窗口 `WM_DPICHANGED` 处理：忽略 Windows/Winit 在跨屏拖动时重复发送的同 DPI 消息；真实 DPI 切换时按 `旧物理尺寸 × 新 DPI / 旧 DPI` 调整客户区，同时保留 Winit 的窗口位置和原生边框。

### 实测结果

使用 Per-Monitor DPI aware 的 Win32 只读诊断和桌面拖动验证：

- 主屏：客户区 `900×650 @ 144 DPI`；
- 完全移到扩展屏：客户区 `750×541 @ 120 DPI`；
- 移回主屏：恢复为 `900×650 @ 144 DPI`；
- 未再出现此前跨屏反复拖动导致的客户区递增（曾复现到约 `2270×1700`）。
- 通过原生边框拖拽完成了尺寸变化测试；调整后的尺寸按用户设置在跨 DPI 屏幕间保持等比例，符合 Windows Per-Monitor DPI 逻辑。

本轮未修改 v0.30.2 或 `agent/v4-foundation`，未添加额外校验、哈希或测试基础设施。当前只在本机现有的 150% 主屏与 125% 扩展屏组合上人工验证，未声称覆盖所有 DPI 组合及热插拔场景。

### 构建结果

- `cargo fmt --check`：通过；
- `cargo clippy --all-targets -- -D warnings`：通过；
- `cargo test`：33 passed、1 ignored（既有 Office 真机 smoke test）；
- `cargo build --release`：通过；
- Release EXE：`E:\快传\计时器\v1.0\target\release\FlyPPTTimer.exe`，16,860,672 字节；
- 版本：`1.12.0`（`Cargo.toml` 与 `Cargo.lock` 已同步）；
- Release 配置已恢复 `RemoteControl.Enabled=true`，临时测试备份已清理。

## 1.13.0 远程控制窗口复用与 DPI 修正

日期：2026-09-04
分支：`codex/v1-06-manual-test`

### 实际问题与原因

- 远程窗口首帧在混合 DPI 下把保存的 96-DPI `WidthDip`/`HeightDip` 再次按错误时序换算，旧配置可能因此记录出 `1221×1099` 等异常尺寸，窗口内容只占左侧。
- 关闭后复用已经隐藏的 Slint 窗口时，原生 HWND 虽然恢复显示，但渲染表面可能只留下旧图像或空白，表现为远程窗口重开后内容消失。
- 远程窗口在首帧创建前用未缩放的客户区尺寸计算位置，保存的底部/居中比例在再次打开时会把窗口推到屏幕下方。

### 修正

- 增加直接的 96-DPI 逻辑尺寸到当前 HWND DPI 物理客户区转换；首帧保持隐藏，尺寸和 DPI 稳定后才显示，并请求一次重绘。
- 关闭后不再复用隐藏的远程 Slint 实例；下一次从托盘入口重新创建窗口，沿用已保存的窗口位置、尺寸和最大化状态。
- 首次显示后按当前显示器实际 DPI 用物理客户区尺寸重新计算保存位置，避免恢复时因预显示 DIP 尺寸造成越界。
- 未新增远程协议、设置项、哈希或测试框架；“远程控制 → 演示文稿”页面继续使用已有的规则列表、添加/删除/刷新/清空、规则编辑和放映控制。

### 本机验证

- 干净配置首次打开：远程窗口约 `700×620` DIP，页面完整绘制；没有可见的中间空白窗口。
- 关闭后再次打开：页面完整绘制，尺寸保持不变，保存位置恢复正常。
- 已切换到“演示文稿”页确认列表区域及添加、删除、刷新、清空、从头/当前页放映、上一页/下一页、跳页、黑屏/白屏/恢复、结束放映等控件均可见。
- 使用真实 Win32 窗口诊断确认主屏 `144 DPI` 下远程客户区为 `1050×930` 物理像素（对应 `700×620` DIP）。

### 本轮文件与构建

- 主要修改：`src/app.rs`、`src/window.rs`、`Cargo.toml`、`Cargo.lock`、本报告。
- 测试版本递增为 `1.13.0`；Release 可执行文件：`E:\快传\计时器\v1.0\target\release\FlyPPTTimer.exe`。
- 本轮最终检查继续执行 `cargo fmt --check`、`cargo clippy --all-targets -- -D warnings`、`cargo test` 和 `cargo build --release`；未创建 Release 或 Tag。
