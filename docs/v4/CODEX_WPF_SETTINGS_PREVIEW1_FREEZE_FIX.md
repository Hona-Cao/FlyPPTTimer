# Codex 任务：修复 WPF 设置 Preview 1 交互卡死

## 用户实测

当前 Preview 1 存在阻断问题：

- WPF 设置窗口可以正常打开、移动和缩放，尺寸调整流畅。
- 修改默认时长、模式、外观尺寸或其他任意设置后，WPF 设置程序立即卡死、无响应。
- 因此无法测试输入校验和关闭时的保存/放弃/取消。
- “经典设置”正常。
- 移走 `FlyPPTTimer.Settings.exe` 后能正常回退经典设置。

本任务只修复 WPF 设置交互冻结并补齐真实交互回归测试，不推进其他功能。

## 开始前

同步并确认分支：

```powershell
git fetch origin
git checkout agent/v4-foundation
git pull --ff-only origin agent/v4-foundation
git status --short
git rev-parse HEAD
```

除生成目录外，如存在来源不明的修改，停止并报告，不要重置或覆盖。

## 允许修改

- `src/FlyPPTTimer.Desktop/App.xaml.cs`
- `src/FlyPPTTimer.Desktop/ViewModels/SettingsViewModel.cs`
- `src/FlyPPTTimer.Desktop/Views/MainWindow.xaml`
- `src/FlyPPTTimer.Desktop/Views/MainWindow.xaml.cs`
- `src/FlyPPTTimer.Desktop/Themes/Controls.xaml`（仅在确认样式/布局是根因时）
- `tests/FlyPPTTimer.Tests/WpfSettingsPreviewTests.cs`
- 本任务新增的 WPF 交互测试文件
- `.github/workflows/windows-ci.yml`（仅用于增加真实交互烟雾测试）
- `TEST_REPORT.md`（仅更新本次验证结果）

不得修改主程序的 PowerPoint/WPS、计时、远程控制、配置 Schema、经典设置或进程启动/回退逻辑。

不得升级 SDK、NuGet 包或版本号。

## 必须先复现和定位

不要先猜测修复。

1. 使用当前发布配置生成 `FlyPPTTimer.Settings.exe`。
2. 在独立临时目录放置设置 EXE 和测试配置。
3. 实际显示 WPF 窗口并依次完成：
   - 修改默认时长文本框；
   - 切换计时模式；
   - 勾选或取消一个复选框；
   - 修改一个外观数字字段。
4. 每一步后确认：
   - 进程仍存活；
   - 窗口仍能处理 Dispatcher 操作；
   - 操作能在限定时间内完成；
   - 不出现持续 CPU 占用或无响应。
5. 定位第一次输入后所有控件共用的执行路径。重点检查：
   - TwoWay Binding 和 `INotifyPropertyChanged` 是否形成反馈循环；
   - `Set`、`IsDirty`、`UnsavedStatus`、`ErrorMessage` 的通知链；
   - SharedSizeGroup、ScrollViewer 和底部状态区是否形成持续布局失效；
   - 动态资源或隐式控件样式是否导致重复重建；
   - Dispatcher 线程是否被同步阻塞。

最终报告必须说明确认的根因，不能只写“优化绑定”。

## 修复要求

- 第一次及后续任意字段修改都必须保持即时响应。
- 不得在字段编辑时读写配置文件；只在“保存并关闭”或关闭选择保存时调用 `ConfigService.Save`。
- 保留未保存状态和错误提示。
- 保留所有 Preview 1 字段、范围校验和未暴露字段保留语义。
- 保留保存、放弃、取消三种关闭选择。
- 不得通过禁用字段、移除双向编辑或退回经典设置来规避问题。
- 如增加诊断日志，必须简洁且不能在每次按键产生大量日志。

## 必须增加真实交互回归测试

现有 ViewModel 单元测试不足以覆盖此问题。

在不增加第三方包的前提下，增加 Windows 发布版交互烟雾测试。可使用 PowerShell/.NET UI Automation、WPF Dispatcher 测试或等效方式，但必须实际操作控件，而不是只启动进程。

建议给关键控件添加稳定的 `AutomationProperties.AutomationId`：

- `DefaultDuration`
- `TimerMode`
- `ContinueOvertime`
- `Width`
- `SaveAndClose`

测试至少完成：

1. 启动发布后的 `FlyPPTTimer.Settings.exe`。
2. 找到窗口和上述控件。
3. 输入新的默认时长。
4. 切换计时模式。
5. 切换复选框。
6. 修改宽度。
7. 每一步在 3 秒内完成，并确认窗口仍响应。
8. 验证“有未保存的更改”出现。
9. 使用取消或放弃正常关闭，不污染仓库配置。
10. 无论成功失败都清理进程和临时目录。

CI 中原有“只启动设置程序”的烟雾测试应替换或扩展为上述真实交互测试。

## 其他自动化验证

保留并通过现有测试，同时增加：

- 连续设置多个 ViewModel 属性不会递归通知或阻塞。
- `PropertyChanged` 事件数量有明确上限，不因一次字段修改无限增长。
- 无效输入仍不保存并显示错误。
- 保存后未暴露字段仍保留。

## 完整验证

执行并报告：

- 三个项目 Restore；
- 两个 `win-x64` 发布运行时 Restore；
- 三个 Release Build，全部 0 warnings / 0 errors；
- 全部桌面测试；
- 全部 Core 测试；
- 两个 self-contained single-file Publish；
- 发布版 WPF 设置真实交互烟雾测试；
- 两个 EXE 的大小和 SHA-256；
- 最终 `git status --short`。

## 提交

提交并推送到 `agent/v4-foundation`。

不要创建 PR，不要修改或合并 `main`，不要强制推送，不要提交 `artifacts/`、`bin/` 或 `obj/`。

完成报告只需包含：

- 确认的根因；
- 提交 SHA；
- 修改文件；
- 构建和测试结果；
- 真实交互烟雾测试结果；
- 两个 EXE 的大小和 SHA-256；
- 最终 `git status --short`。
