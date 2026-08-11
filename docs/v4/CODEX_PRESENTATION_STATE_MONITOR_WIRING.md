# Codex 任务：接入演示状态监控组件

## 目标

把 `PowerPointControlService` 中已经存在的状态缓存、500ms 后台刷新、刷新合并、放映开始/结束转换和状态深复制，委托给已验证的 `PresentationStateMonitor`。

本任务只做状态监控接线。不得修改 PowerPoint/WPS 的 COM 读取、命令实现、窗口激活、文稿管理、能力判断、超时或页面布局。

## 工作分支

在现有分支 `agent/v4-foundation` 上工作。不要修改或合并 `main`，不要创建新 PR。

开始前执行：

```powershell
git fetch origin
git checkout agent/v4-foundation
git pull --ff-only origin agent/v4-foundation
```

## 已存在且不得修改

- `src/FlyPPTTimer/Services/PresentationStaDispatcher.cs`
- `src/FlyPPTTimer/Services/PresentationStateMonitor.cs`
- `tests/FlyPPTTimer.Tests/PresentationStaDispatcherTests.cs`
- `tests/FlyPPTTimer.Tests/PresentationStateMonitorTests.cs`

如果现有 API 无法按本文完成接线，立即停止并报告原因。不得自行修改上述基础组件或扩大范围。

## 允许修改的文件

1. `src/FlyPPTTimer/Services/PowerPointControlService.cs`
2. `tests/FlyPPTTimer.Tests/PresentationControlAbstractionTests.cs`

除这两个文件外不得修改其他文件。

## 必须完成

### 1. 替换状态监控字段

在 `PowerPointControlService` 中新增：

```csharp
private readonly PresentationStateMonitor _stateMonitor;
```

删除以下由新组件接管的字段：

```csharp
private readonly System.Threading.Timer _refreshTimer;
private readonly object _stateSync = new();
private PresentationState _cachedState = new();
private bool _lastShowRunning;
private string _lastShowPath = "";
private int _refreshQueued;
private DateTime _lastRefreshFailureLog = DateTime.MinValue;
```

不得删除 `_operationSync`、`_operation`、`_dispatcher`、`_disposed` 或任何文稿管理字段。

### 2. 在构造函数创建状态监控组件

必须继续先创建 `_dispatcher`，然后创建 `_stateMonitor`：

```csharp
_stateMonitor = new PresentationStateMonitor(
    _dispatcher,
    ReadState,
    ApplyOperation,
    FriendlyError,
    _log.Warn);
```

删除旧 `_refreshTimer` 的创建。

把监控组件事件转发为原服务事件，并保持事件发送者为 `PowerPointControlService`：

```csharp
_stateMonitor.SlideShowStarted += (_, path) => SlideShowStarted?.Invoke(this, path);
_stateMonitor.SlideShowEnded += (_, _) => SlideShowEnded?.Invoke(this, EventArgs.Empty);
_stateMonitor.StateChanged += (_, _) => StateChanged?.Invoke(this, EventArgs.Empty);
```

不得修改 `SlideShowWindowActivated` 的现有触发位置。

### 3. 状态读取与刷新委托

把 `GetState()` 改为：

```csharp
public PresentationState GetState() => _stateMonitor.GetState();
```

把 `UpdateCachedState()` 改为仅委托：

```csharp
private PresentationState UpdateCachedState() => _stateMonitor.RefreshNow();
```

删除旧 `QueueRefresh()` 的完整实现。500ms 定时、刷新合并、COM Busy 重试和 30 秒日志节流由 `PresentationStateMonitor` 接管。

不得修改 `ReadState()`。

### 4. 操作状态更新

保留 `ApplyOperation(PresentationState state)` 和 `_operationSync` 原样。

把 `SetOperation` 中直接锁定 `_stateSync` 修改缓存的代码替换为：

```csharp
private void SetOperation(PresentationOperationInfo operation)
{
    lock (_operationSync) _operation = operation;
    _stateMonitor.MutateCurrent(ApplyOperation);
}
```

`Queue(RemoteCommand command)` 在刚写入 `_operation` 后的 `NotifyStateChanged()` 必须保留，避免改变命令接受时的通知时机。

`NotifyStateChanged()` 方法可以保留，且仍应直接触发原服务的 `StateChanged` 事件。

### 5. 异常时更新缓存错误

`RunQueuedOperation` 的 catch 中，不再直接访问 `_cachedState`。将：

```csharp
lock (_stateSync) _cachedState.Error = error;
```

替换为：

```csharp
_stateMonitor.MutateCurrent(state => state.Error = error, notify: false);
```

随后原有 `SetOperation(PresentationOperationInfo.Failed(...))` 保持不变，由它发送一次状态通知。

不得修改同步 `Execute` 的错误返回文案和逻辑。

### 6. 删除重复深复制实现

删除 `PowerPointControlService` 中的私有 `CloneState(PresentationState state)`。深复制只由 `PresentationStateMonitor.CloneState` 内部使用。

不要删除 `SamePath`，它仍被 PowerPoint 文稿和窗口匹配逻辑使用。

### 7. 释放顺序

把 `Dispose()` 改为以下顺序：

```csharp
public void Dispose()
{
    if (_disposed) return;
    _disposed = true;
    _stateMonitor.Dispose();
    _dispatcher.Dispose();
}
```

必须先停止状态监控定时器，再停止 STA 调度器。

### 8. 契约测试

扩展 `PresentationControlAbstractionTests.cs`，添加或更新测试，验证：

- `PowerPointControlService` 声明 `PresentationStateMonitor _stateMonitor`。
- 构造函数创建 `new PresentationStateMonitor(`。
- `GetState()` 使用 `_stateMonitor.GetState()`。
- `UpdateCachedState()` 使用 `_stateMonitor.RefreshNow()`。
- `SetOperation` 使用 `_stateMonitor.MutateCurrent(ApplyOperation)`。
- 错误路径使用 `notify: false`。
- 释放顺序中 `_stateMonitor.Dispose()` 位于 `_dispatcher.Dispose()` 之前。
- 不再包含 `_refreshTimer`、`_stateSync`、`_cachedState`、`_lastShowRunning`、`_lastShowPath`、`_refreshQueued`、`_lastRefreshFailureLog` 和私有 `CloneState`。

不得删除或弱化现有 STA 调度器契约测试。

## 严格禁止

- 不得修改 `ReadState()` 及其任何 COM 访问。
- 不得修改 `ExecuteCore()`、命令白名单、命令名称或提示文案。
- 不得修改 15 秒/5 秒同步命令超时。
- 不得修改 `PresentationStaDispatcher` 或 `PresentationStateMonitor`。
- 不得修改 PowerPoint/WPS 能力判断。
- 不得修改窗口激活、翻页节流、文稿打开/关闭和 COM 对象释放。
- 不得修改任何 WinForms/WPF 页面、布局、字体、颜色、DPI 或刷新显示逻辑。
- 不得升级 SDK、NuGet 包或 GitHub Actions。
- 不得提交 `artifacts/`、`bin/`、`obj/`。

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

- 三个项目构建均为 0 errors。
- 全部桌面测试和 Core 测试通过。
- win-x64 自包含单文件发布成功。
- 提交 diff 只包含两个允许修改的文件。
- 不合并 PR，不修改 `main`。
- 完成后报告提交 SHA、修改文件、测试数量、发布文件大小及 `git status --short`。
