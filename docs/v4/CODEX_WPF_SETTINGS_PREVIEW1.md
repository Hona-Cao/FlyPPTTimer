# Codex 任务：WPF 设置窗口 Preview 1

## 目标

一次性交付可供用户实际测试的 WPF 设置程序，不再继续拆分内部基础组件。

最终 CI Artifact 的 `artifacts/publish/` 根目录必须同时包含：

- `FlyPPTTimer.exe`
- `FlyPPTTimer.Settings.exe`

用户从现有主程序托盘菜单打开 WPF 设置，保存并关闭后，主程序自动重新加载配置并应用。现有 WinForms 设置窗口必须继续作为“经典设置”保留。

## 允许修改

- `src/FlyPPTTimer/FlyPPTTimerContext.cs`
- `src/FlyPPTTimer/FlyPPTTimer.csproj`（仅在确有必要时）
- `src/FlyPPTTimer.Desktop/**`
- `.github/workflows/windows-ci.yml`
- `tests/FlyPPTTimer.Tests/FlyPPTTimer.Tests.csproj`
- `tests/FlyPPTTimer.Tests/**` 中本任务新增或更新的测试
- `TEST_REPORT.md`（仅更新本里程碑验证说明）

不得修改 PowerPoint/WPS、计时状态机、状态监控、STA、窗口激活、进程检测/终止、远程控制协议和配置 Schema。

不得升级 SDK、NuGet 包或版本号。

## 架构要求

### WPF 设置程序

1. 将 `FlyPPTTimer.Desktop` 的程序集名改为 `FlyPPTTimer.Settings`。
2. `FlyPPTTimer.Desktop` 引用现有 `FlyPPTTimer` 项目，直接复用：
   - `AppConfig`
   - `ConfigService`
   - `LogService`
   - `AppPaths`
3. WPF 程序从与 EXE 同目录的 `FlyPPTTimer.config.json` 加载和保存配置。
4. 保存时必须通过 `ConfigService.Save`，不得自行序列化覆盖配置。
5. 未在 WPF 页面暴露的配置字段必须原样保留。

### 主程序集成

1. 保留现有 `SettingsForm` 和全部事件接线。
2. 将现有经典设置打开逻辑整理为 `ShowClassicSettings()`。
3. 新增 WPF 设置启动逻辑：
   - 路径：`Path.Combine(AppContext.BaseDirectory, "FlyPPTTimer.Settings.exe")`
   - 同一时间只允许一个设置进程。
   - 已运行时尝试激活现有窗口，不重复启动。
   - 文件不存在或启动失败时记录警告并回退到经典设置。
4. WPF 设置进程退出后，在 WinForms UI 线程调用 `_configService.Load()`，再通过现有 `ApplyConfig(...)` 应用。
5. 托盘和计时窗口菜单同时提供：
   - `设置（WPF 预览）`
   - `经典设置`
6. 托盘双击和现有 `AppCommandService` 的“打开设置”动作进入 WPF 预览。
7. 不改变现有配置应用、热键注册、远程服务和窗口重建语义。

## WPF 页面范围

将当前占位壳替换成真正的设置窗口，至少包含以下分区。

### 计时

- 默认时长 `Timer.DefaultDuration`
- 模式 `Timer.Mode`
- 到零后继续超时 `Timer.ContinueOvertime`
- 到时动作 `Timer.EndAction`

### 自动行为

- `Behavior.AutoStartOnFullscreen`
- `Behavior.StopWhenLeavingFullscreen`
- `Behavior.ResetWhenLeavingFullscreen`
- `Behavior.FlashOnPauseResume`
- `Behavior.FlashPausedTime`

### 外观

- `Appearance.FontSize`
- `Appearance.Width`
- `Appearance.Height`
- `Appearance.BackgroundOpacity`
- `Appearance.TextOpacity`
- `Appearance.AlwaysOnTop`
- `Appearance.Borderless`

### 控制与其他

- `Controls.StartPauseHotkey`
- `Controls.StopResetHotkey`
- `Controls.ToggleWindowHotkey`
- `Controls.MinimizeToTray`
- `Update.CheckOnStartup`

页面必须明确提示：文件规则、远程控制、提示音和高级外观暂在“经典设置”中编辑。

## 交互要求

- 使用现有 WPF 主题资源。
- 使用 ViewModel 和命令，不把配置读写逻辑堆在 XAML code-behind。
- 提供“保存并关闭”和“取消”。
- 显示未保存状态。
- 关闭存在未保存修改的窗口时给出保存、放弃、取消选择。
- 默认时长必须验证为有效且大于零的 `hh:mm:ss`/`TimeSpan` 值。
- 数字字段必须限制在现有模型可接受的合理范围：
  - FontSize：8–96
  - Width：40–2000
  - Height：20–1000
  - 两个透明度：0–100
- 保存错误必须在窗口中显示，不允许静默失败。

## 测试要求

至少增加以下自动化验证：

1. ViewModel 能从配置加载上述所有字段。
2. 保存会更新上述字段并保留未暴露字段（例如远程控制 Token 和 Rules）。
3. 无效时长和越界数字不能保存。
4. 主程序源码契约确认：
   - WPF 设置 EXE 路径正确。
   - 设置进程退出后重新加载并调用 `ApplyConfig`。
   - 经典设置入口仍存在。
   - WPF 缺失/失败时回退经典设置。
5. CI Artifact 契约确认两个 EXE 均存在。

测试不得依赖真实 PowerPoint、WPS、网络或用户配置文件。

## CI 与发布

1. Restore 两个 `win-x64` 发布运行时：
   - `FlyPPTTimer`
   - `FlyPPTTimer.Desktop`
2. 保持三个项目 Release Build。
3. 保持桌面/Core 全部测试。
4. 将两个项目都发布为 `win-x64`、self-contained、single-file。
5. 最终复制到同一目录：
   - `artifacts/publish/FlyPPTTimer.exe`
   - `artifacts/publish/FlyPPTTimer.Settings.exe`
6. 为两个 EXE 生成并验证 SHA-256。
7. Artifact 继续使用现有名称。

## 不得改变

- `main` 分支
- PR 状态
- PowerPoint/WPS 行为
- 计时逻辑
- 配置 Schema 和默认值
- 现有 WinForms 设置功能
- 现有中文配置值
- 版本 `4.0.0-alpha.1`

不得提交 `artifacts/`、`bin/`、`obj/`。

## 完整验证

执行：

- 三个项目 Restore
- 两个发布运行时 Restore
- 三个项目 Release Build，0 warnings / 0 errors
- 全部桌面测试
- 全部 Core 测试
- 两个 self-contained single-file Publish
- 确认两个 EXE 位于 `artifacts/publish/`
- 计算两个 EXE 字节大小和 SHA-256

## 提交

提交并推送到 `agent/v4-foundation`。

不要创建新 PR，不要修改或合并 `main`，不要强制推送。

完成报告只需包含：

- 提交 SHA
- 修改文件
- 三个 Build 结果
- 桌面/Core 测试数量
- 两个 EXE 的路径、大小、SHA-256
- 最终 `git status --short`
- 简短人工测试步骤
