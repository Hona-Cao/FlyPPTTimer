# Codex 任务：接入演示窗口激活策略

## 目标

将 `PowerPointControlService` 中 `ActivateNativeWindow` 的 Win32 最大化、置前、TopMost 脉冲重试和诊断逻辑委托给已经验证的 `PresentationWindowActivator`。

本任务只做依赖接线。不得修改窗口查找、PowerPoint/WPS COM、放映启动、WPS 首帧最大化钩子、提示文字或页面布局。

## 工作分支

在现有分支 `agent/v4-foundation` 上工作。不要修改或合并 `main`，不要创建新 PR。

开始前执行：

```powershell
git fetch origin
git checkout agent/v4-foundation
git pull --ff-only origin agent/v4-foundation
```

## 已存在且禁止修改的基础

- `src/FlyPPTTimer/Services/PresentationWindowActivator.cs`
- `tests/FlyPPTTimer.Tests/PresentationWindowActivatorTests.cs`

`PresentationWindowActivator` 已覆盖：

- 空 HWND 失败。
- 正常最大化并置前。
- 首次置前失败后的 TopMost/NoTopMost 脉冲重试。
- Windows 拒绝强制置前但窗口已最大化时仍视为成功。
- 未成功最大化时的完整 Win32 诊断。

## 只允许修改

1. `src/FlyPPTTimer/Services/PowerPointControlService.cs`
2. `tests/FlyPPTTimer.Tests/PresentationControlAbstractionTests.cs`

## 必须修改

### 1. 增加激活器字段

在 `PowerPointControlService` 中增加：

```csharp
private readonly PresentationWindowActivator _windowActivator;
```

### 2. 构造激活器

在构造函数中，在 `_log` 已赋值后创建：

```csharp
_windowActivator = new PresentationWindowActivator(warn: _log.Warn);
```

不要向激活器传入 PowerPoint/WPS COM 对象。

### 3. 只替换 `ActivateNativeWindow` 的实现

保留原方法签名：

```csharp
private WindowActivationResult ActivateNativeWindow(
    IntPtr hwnd,
    string path,
    string label,
    bool maximized,
    string failurePrefix = "；文稿已打开但最大化或置前失败")
```

`maximized` 参数目前为兼容参数，保留它，不改调用点。

将方法体替换为对 `_windowActivator` 的委托。结果转换必须保留现有私有 `WindowActivationResult` 类型和现有消息内容：

```csharp
var result = _windowActivator.Activate(hwnd, path, label, failurePrefix);
return new WindowActivationResult(
    result.Success,
    result.Message,
    result.Path,
    result.Hwnd);
```

不要调用 `WindowActivationResult.Failed(...)` 转换失败结果，因为新结果的 `Message` 已包含现有前导分号，再次调用会产生双分号。

### 4. 删除旧方法体中的重复 Win32 策略

`ActivateNativeWindow` 方法体内不再直接调用：

- `NativeMethods.ShowWindow`
- `NativeMethods.BringWindowToTop`
- `NativeMethods.SetForegroundWindow`
- `NativeMethods.SetWindowPos`
- `NativeMethods.IsZoomed`
- `Marshal.GetLastWin32Error`

这些调用已经由 `PresentationWindowActivator` 负责。

不要删除 `using System.Runtime.InteropServices;`，文件其他位置仍用于 COM 释放和 P/Invoke。

### 5. 扩展契约测试

在 `PresentationControlAbstractionTests.cs` 增加测试，验证：

- `PowerPointControlService` 声明 `PresentationWindowActivator _windowActivator`。
- 构造函数创建 `PresentationWindowActivator` 并传入 `_log.Warn`。
- `ActivateNativeWindow` 调用 `_windowActivator.Activate(hwnd, path, label, failurePrefix)`。
- 转换结果时直接调用 `new WindowActivationResult(...)`。
- 旧诊断字符串 `PowerPoint window activation incomplete` 不再出现在 `PowerPointControlService.cs`。

不要把测试写成全文件禁止 `NativeMethods.ShowWindow`，因为 `WpsFirstFrameMaximizer` 仍必须使用它。

## 必须保持不变

- `ActivatePresentationWindow` 的 COM 激活、最大化和回退流程。
- `ActivatePresentationProcessWindow` 的进程名、标题匹配与句柄释放。
- `FindWpsPresentationFrame`、`IsWpsPresentationFrame` 和 `WpsFirstFrameMaximizer`。
- `ActivateSlideShowWindow` 的 20 次、每次 100ms 查找逻辑。
- `SlideShowWindowActivated` 事件触发位置。
- 私有 `WindowActivationResult` 类型及其现有辅助方法。
- 所有中文提示文字。
- PowerPoint/WPS COM 读取与 `Release(...)`。
- 15 秒/5 秒命令超时、500ms 状态刷新和 STA 调度。
- WinForms/WPF 页面、尺寸、字体、颜色和布局。

## 禁止事项

- 不得修改 `PresentationWindowActivator.cs`。
- 不得修改 `PresentationWindowActivatorTests.cs`。
- 不得修改 `NativeMethods.cs`。
- 不得改变任何远程命令名称或 HTTP 路由。
- 不得升级 SDK、NuGet 包或 GitHub Actions。
- 不得提交 `artifacts/`、`bin/` 或 `obj/`。

## 验证命令

```powershell
dotnet restore tests/FlyPPTTimer.Tests/FlyPPTTimer.Tests.csproj
dotnet restore tests/FlyPPTTimer.Core.Tests/FlyPPTTimer.Core.Tests.csproj
dotnet restore src/FlyPPTTimer.Desktop/FlyPPTTimer.Desktop.csproj
dotnet build src/FlyPPTTimer/FlyPPTTimer.csproj -c Release --no-restore
dotnet build src/FlyPPTTimer.Core/FlyPPTTimer.Core.csproj -c Release --no-restore
dotnet build src/FlyPPTTimer.Desktop/FlyPPTTimer.Desktop.csproj -c Release --no-restore
dotnet test tests/FlyPPTTimer.Tests/FlyPPTTimer.Tests.csproj -c Release --no-restore
dotnet test tests/FlyPPTTimer.Core.Tests/FlyPPTTimer.Core.Tests.csproj -c Release --no-restore
dotnet restore src/FlyPPTTimer/FlyPPTTimer.csproj -r win-x64
dotnet publish src/FlyPPTTimer/FlyPPTTimer.csproj -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o artifacts/publish
```

## 完成标准

- 三个项目构建均为 0 warnings、0 errors。
- 全部桌面测试通过。
- 全部 Core 测试通过。
- win-x64 自包含单文件发布成功。
- Git diff 只包含两处允许修改的文件。
- `artifacts/` 可保持未跟踪，但不得暂存或提交。
- 提交到 `agent/v4-foundation` 后停止。
- 报告提交 SHA、修改文件列表、构建结果、测试数量、发布文件路径和大小、`git status --short`。
