# FlyPPTTimer V1 — Codex Result Handoff

日期：2026-09-03。状态：**1.06 手工测试候选已构建；等待审核和手测。不是“遮罩已实测消失”或“所有基线差异已清零”的声明。**

## Review 版本

- 本地目录：`E:\快传\计时器\v1.0`。
- 起点：拉取 GitHub `codex/v1-05-window-audit`，快进到 `4660ae624fb2ccf7568388f8db96f25f186f6823`。
- Review 分支：`codex/v1-06-manual-test`。
- 最终实现 commit SHA：`68d9090636227401cd9a97008f57cb141deb54c4`。
- 本报告是后续独立文档提交，review HEAD 比上述实现提交多一个仅修改本报告的提交。上述 SHA 对应候选的全部源码、依赖和构建脚本；可用 `git rev-parse origin/codex/v1-06-manual-test` 获取包括本报告的最终分支 SHA。
- 版本顺延为 **1.06 / 1.6.0**；Cargo、配置保存版本、manifest、安装器和打包名称使用 `1.6.0`。
- 不创建 Release、Tag 或正式发布页，不上传二进制或本地测试输出。

## 主要修改文件

- `src/window.rs`、`src/app.rs`：普通 Timer 框架最小修复、文字过宽扩展、单实例句柄存活期、安装语言读取。
- `ui/app-window.slint`：文字所需尺寸；Remote 三列按钮及跳页控件坐标修正。
- `src/presentation.rs`、`src/remote.rs`：operation/busy 及受理/完成/失败状态。
- `src/settings.rs`：手动更新和 Remote 按钮连接；英文下拉框配置值及配色修正。
- `src/updater.rs`、`src/desktop.rs`：原更新通知、提示和忙碌流程，安装辅助进程无控制台启动。
- `Cargo.toml`、`Cargo.lock`、`src/config.rs`、`resources/app.manifest`、`installer/FlyPPTTimer.iss`：版本顺延，无新增依赖。
- `scripts/build-release.ps1`、`.gitignore`：产物按版本存放，不再删除整个 release 目录，不提交 artifacts/output。

## 1. Timer 遮罩最小修复

严格执行任务指定的四项修改：

1. 普通 Timer 的 `apply_native_window()` 删除全部 `GWL_STYLE` 读写，框架交给 Slint `no-frame: true`。
2. 保留原扩展样式，加入 `WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`，移除 `WS_EX_APPWINDOW`，按配置切换 `WS_EX_TRANSPARENT`。
3. 普通 Timer 的 `SetWindowPos()` 保留原 TopMost 选择及 `SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE`，移除 `SWP_FRAMECHANGED`。
4. `configure_timer_window()` 保留首次原生设置；80ms 回调只做 `refresh_shape()`。

没有改变 renderer、透明度实现、圆角算法或 Timer。`window.rs` 中仍有一个 `SWP_FRAMECHANGED`，属于“时间到”辅助窗口，不是普通 Timer，本轮未改。

### 本机检查的实际结果

按 Computer Use 技能尝试直接读取窗口，工具在启动阶段返回：

```text
failed to launch codex app-server: 系统找不到指定的路径。 (os error 3)
```

同一入口重试仍失败。**没有完成启动、点击、拖动及右键的本机人工检查**，没有认定遮罩已消失或仍稳定复现。没有编写 GUI 驱动、截图脚本或替代验收工具，也没有继续叠加遮罩补丁。请优先手测；若同一遮罩仍稳定复现，将此提交交给 ChatGPT 继续审核。

## 2. 明确基线差异的处理

### 文字过宽

对照 `TimerOverlayForm.cs` 和 `FlyPPTTimerContext.cs` 的尺寸处理：当前文字需要的宽/高超过配置时扩展，不主动缩小；新尺寸写回现有配置，沿用宽 2000、高 1000 上限；普通多屏窗口同步调整并保持中心。

原提示和标题分别保留为：

- `当前时间文字需要更大的显示区域，窗口已自动调整为 {宽} × {高}。` / `演讲计时器`
- `The current timer text needs more space. The window was resized to {宽} × {高}.` / `Presentation Timer`

提示在刷新回调释放配置借用后显示。文字尺寸使用 Slint preferred size；旧版使用 GDI/TextRenderer，两者像素边界仍需手测，不宣称像素完全一致。

### Remote operation/busy

旧 Web 确实依赖 `isOperationBusy` 禁用命令按钮；旧 PC 窗口显示 `OperationMessage`。之前 V1 恒返回 Idle/false，现恢复：

- 原操作名称、受理提示、操作 ID、开始时间和 busy 状态。
- 打开文稿、开始/结束放映、关闭文稿、强制退出期间的 busy；忙碌时沿用原提示拒绝新的演示命令。
- 完成后 Idle + 成功消息，失败后 Failed + 错误消息；轮询不会覆盖正在进行的操作状态。
- HTTP 返回受理消息，不再提前宣称 COM 操作成功；恢复 `ppt.refresh`；PC 显示实际操作消息。
- 不另建 Timer 或 COM 实现，不修改 Web。文稿仍以大小写不敏感的完整路径标识；操作 ID 沿用原版 GUID，仅标识一次操作，不是文稿身份。无哈希扩展。

### 更新 / Portable / Installer

- 设置“立即检测新版本”原来只有占位代码，现连接与托盘相同的入口。
- 补回检测/下载安装时的托盘通知；检查、询问、下载属于同一次忙碌流程，重复检查沿用原提示。
- 修正英文更新主句；按便携版/缺少安装包/有安装包分别使用信息、警告、问号提示。
- 启动检查对网络失败静默；手动检查及非网络解析错误显示失败。
- 便携版仍打开 Gitee 引导下载，不覆盖自身；安装版仍下载后安排安装并退出，无 SHA、完整性校验或额外更新框架。
- 安装辅助 PowerShell 进程无控制台启动；读取安装器的 `install-language.txt`，保存原有语言选项后移除标记。
- 配置、日志仍相对于 EXE；默认安装目录仍为 `%LOCALAPPDATA%\FlyPPTTimer`；已有配置不被安装覆盖，卸载保留配置。
- 修正单实例句柄原来在局部作用域立即析构的问题，保留到退出。
- 现有打包脚本完成编译；使用机器已有 Inno Setup，未安装或移动系统工具。取消删除整个 release 目录，ZIP 仅收录原有打包文件清单。

## 3. 快速基线对照

- 导航仍仅六页且顺序不变：时长设置、行为设置、外观与显示、远程控制、控制设置、其他设置。
- 原默认配置未修改，“全部默认字段与 v0.30.2 等价”测试通过；保存版本号随 V1 顺延。
- 英文 ComboBox 改为保存原始选项，避免圆角/配色等配置被翻译文字破坏。
- 配色正常/超时/闪烁值恢复 `AppearancePresetService` 原值，“自定义”不再重置颜色；修正教育培训、商务会议、科技发布三个英文预设名。
- 设置 Remote 页的重启、令牌、断开、复制地址、本机页面、防火墙命令复制接入已有服务。重启先应用设置；令牌/断开立即生效并保存，不提前保存其他编辑内容。
- Timer 右键与托盘菜单未增加项目；托盘英文 `Check for updates` 大小写对齐原版。
- Remote 三列按钮间距公式及跳页输入框/按钮有明确位置错误，已修正。
- `git diff v0.30.2 -- src/FlyPPTTimer` 为空，原版源码、资源和 Web 未改；未修改 v4 分支。

这是快速源码对照，不是完整双语逐屏验收；额外发现的未完成差异列在文末。

## 4. 检查与构建

| 检查 | 最终实现提交结果 |
| --- | --- |
| `cargo fmt --check` | 通过 |
| `cargo clippy --all-targets -- -D warnings` | 通过，无警告 |
| `cargo test` | 33 通过，0 失败，1 跳过 |
| `cargo build --release` | 通过；最终打包脚本亦执行此命令 |
| `scripts/build-release.ps1` | 通过，生成 Portable ZIP 和 Installer |

仅新增一个 operation 完成/失败的纯逻辑测试。跳过的是原有 `manual_powerpoint_and_wps_com_connection`，没有自动启动或退出用户的 Office；没有扩展 GUI、声音、设备或发布验收框架。

## 5. 本地产物

| 产物 | 绝对路径 | 字节 |
| --- | --- | ---: |
| Release EXE | `E:\快传\计时器\v1.0\target\release\FlyPPTTimer.exe` | 16,747,008 |
| Portable ZIP | `E:\快传\计时器\v1.0\artifacts\release\v1.6.0\FlyPPTTimer-v1.6.0-portable-win-x64.zip` | 7,577,286 |
| Installer | `E:\快传\计时器\v1.0\artifacts\release\v1.6.0\FlyPPTTimer-v1.6.0-setup-win-x64.exe` | 7,209,444 |

解压式目录：`E:\快传\计时器\v1.0\artifacts\release\v1.6.0\FlyPPTTimer-v1.6.0-portable-win-x64`。

旧 artifacts/output 内容未删除。构建及测试进程已经结束；本轮没有启动图形版候选，没有遗留本轮启动的 Timer 窗口。二进制仅保存在本地。

## 6. 用户手测清单

1. 退出旧版，从上述 **v1.6.0 Portable** 启动；优先在此前的 150% DPI 下点击、拖动、打开/收起右键菜单，检查遮罩、圆角、透明度及任务栏隐藏。同一遮罩若仍稳定复现，停止试补丁并回传现象。
2. 设置与 Remote 连续打开，检查前置闪窗、闪退；应用尺寸、字体和配色，使用较长超时前缀检查自动扩展提示；英文下选择圆角/配色，测试语言重启及保存。
3. 倒/正计时、暂停/恢复、Restart、超时、提醒 1/2、声音/TTS、闪烁、静音、快捷键。
4. PowerPoint / WPS 用测试文稿验证放映联动、规则、翻页/跳页、黑白屏及结束；不要用未保存的重要文稿试强制退出。
5. 手机扫码，测试 Timer/PPT 控制、状态同步、busy 及失败后恢复；设置和 PC Remote 两个入口测试重启、令牌和断开。
6. 双屏、所有屏幕、大屏、九宫格/微调、热插拔、非 100% DPI、拖动后重启位置。
7. Portable 启动/退出与配置；Installer 安装、语言、保留配置及卸载；设置/托盘手动更新及启动检查。确认候选基本可用后再测试会改变实际安装的更新流程。

## 7. 尚存差异 / 阻塞

- **本机 GUI 操作工具无法启动**：遮罩、前置闪窗、窗口稳定性、语言重启和布局没有本轮实测结论，不能写成已经解决。
- **PC Remote 演示文稿页仍是前序实现的简化列表**：缺少 v0.30.2 原页的添加、删除、刷新、清空列表及内联规则操作；规则目前可在设置页编辑。本轮恢复 operation/busy，未扩展为整页重新迁移。这仍是明确的产品差异，需后续审核确定修正范围。
- 新增 operation 固定消息按旧资源连接；既有动态 COM 错误及整个 PC UI 的中英文未逐项实机复核，不能声称全量文字已验收。
- Slint 与 GDI 的文字度量像素差异需手测；达到原版尺寸上限后不继续无限扩窗。
- 手机、PPT/WPS、多屏、声音/TTS、安装/卸载及实际在线更新安装均未做本轮人工验证；安装器编译成功不等于安装/卸载通过。
- 编译、测试及打包无阻塞。没有 Release/Tag，没有进入下一阶段。

推送 review 分支后停止，等待 ChatGPT 审核及用户反馈。
