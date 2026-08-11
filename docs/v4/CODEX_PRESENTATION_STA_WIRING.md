# Codex 任务：接入独立演示 STA 调度器

## 目标

把 `PowerPointControlService` 内部的队列、STA 线程、同步调用和 COM 忙重试接入已经存在的 `PresentationStaDispatcher`。

本任务只迁移线程调度基础设施，不拆分命令实现，不修改 COM 业务调用，不修改 PowerPoint/WPS 能力判断，不修改任何页面。

## 工作分支

在现有分支 `agent/v4-foundation` 上工作。

开始前执行：

```powershell
git fetch origin
git checkout agent/v4-foundation
git pull --ff-only origin agent/v4-foundation
```

不要修改 `main`，不要创建或合并新的 PR。

## 已存在的基础

- `src/FlyPPTTimer/Services/PresentationStaDispatcher.cs`
- `tests/FlyPPTTimer.Tests/PresentationStaDispatcherTests.cs`
- `src/FlyPPTTimer/Services/PowerPointControlService.cs`

`PresentationStaDispatcher` 已负责：

- 有界 FIFO 队列；
- 专用后台 STA 线程；
- 同步 `Invoke` 与超时；
- PowerPoint 常见 COM Busy HRESULT 重试；
- 停止与释放；
- 队列繁忙时沿用现有中文异常文案。

## 必须修改

### 1. `PowerPointControlService.cs` 字段与构造函数

- 删除：

```csharp
using System.Collections.Concurrent;
```

- 删除字段：

```csharp
private readonly BlockingCollection<Action> _queue = new(32);
private readonly Thread _thread;
```

- 新增字段：

```csharp
private readonly PresentationStaDispatcher _dispatcher;
```

- 构造函数中删除手工创建、设置和启动线程的代码。
- 在 `_log = log;` 后创建调度器：

```csharp
_dispatcher = new PresentationStaDispatcher(
    "FlyPPTTimer PowerPoint STA",
    warn: _log.Warn);
```

- 保留 500ms `_refreshTimer` 原样。

### 2. 异步命令队列

在 `Queue(RemoteCommand command)` 中，把：

```csharp
_queue.TryAdd(..., 200)
```

改为：

```csharp
_dispatcher.TryEnqueue(...)
```

必须保留：

- 命令白名单；
- 强制退出确认；
- 忙状态检查；
- 全部中文返回文案；
- `RunQueuedOperation` 的调用方式。

### 3. COM Busy 重试

在 `RunQueuedOperation` 中，把：

```csharp
RetryComBusy(() => ExecuteCore(command))
```

改为：

```csharp
_dispatcher.ExecuteWithBusyRetry(() => ExecuteCore(command))
```

在 `QueueRefresh` 中，把：

```csharp
RetryComBusy(UpdateCachedState)
```

改为：

```csharp
_dispatcher.ExecuteWithBusyRetry(UpdateCachedState)
```

不要修改重试次数、退避时间或 HRESULT；这些现在由调度器统一维护。

### 4. 同步命令调用

在 `Execute(RemoteCommand command)` 中，把原有私有 `Invoke` 调用改为：

```csharp
_dispatcher.Invoke(() =>
{
    // 保留原有函数体
}, timeout)
```

保留 5 秒和 15 秒超时规则以及所有异常处理和中文文案。

### 5. 后台刷新入队

在 `QueueRefresh()` 中，把 `_queue.TryAdd(...)` 改为 `_dispatcher.TryEnqueue(...)`。

必须保留：

- `_refreshQueued` 防重复入队；
- 30 秒错误日志节流；
- `finally` 中重置 `_refreshQueued`；
- 入队失败时立即重置 `_refreshQueued`。

### 6. 删除旧线程基础设施

从 `PowerPointControlService` 删除以下私有方法：

- `Run()`
- `Invoke<T>()`
- `RetryComBusy<T>()`

保留 `IsComBusy(COMException)`，因为 `ReadState()` 仍使用它判断是否重新抛出 COM Busy 异常。

### 7. 释放

`PowerPointControlService.Dispose()` 中：

- 保留 `_disposed` 幂等保护；
- 保留 `_refreshTimer.Dispose()`；
- 删除 `_queue.CompleteAdding()`、线程 `Join` 和 `_queue.Dispose()`；
- 改为调用：

```csharp
_dispatcher.Dispose();
```

不得改变应用上下文中的释放顺序。

### 8. 契约测试

扩展 `tests/FlyPPTTimer.Tests/PresentationControlAbstractionTests.cs`，增加一个测试读取 `PowerPointControlService.cs` 并验证：

- 包含 `PresentationStaDispatcher _dispatcher`；
- 包含 `_dispatcher.TryEnqueue`；
- 包含 `_dispatcher.Invoke`；
- 包含 `_dispatcher.ExecuteWithBusyRetry`；
- 不包含 `BlockingCollection<Action> _queue`；
- 不包含 `new Thread(Run)`；
- 不包含 `private T Invoke<T>`；
- 不包含 `private T RetryComBusy<T>`。

不要删除或放宽 `PresentationStaDispatcherTests`。

## 禁止事项

- 不得修改 `PresentationStaDispatcher.cs`，除非构建错误明确证明接口无法按本文接入；发生这种情况先停止并报告，不要自行扩展范围。
- 不得修改 `ExecuteCore` 及任何具体 PowerPoint 命令实现。
- 不得修改 `ReadState`、COM 对象释放、窗口激活、WPS 检测和能力声明。
- 不得修改远程 HTTP 路由、命令名称和响应字段。
- 不得修改 WinForms 或 WPF 布局、字体、颜色、尺寸、刷新间隔和提示文字。
- 不得升级 NuGet 包或 SDK。
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

- 三个项目 Release Build 均为 0 warnings、0 errors。
- 全部桌面测试和 Core 测试通过。
- win-x64 自包含单文件发布成功。
- 提交 diff 仅包含 `PowerPointControlService.cs`、契约测试和必要的任务完成说明。
- 提交到 `agent/v4-foundation` 后停止。
- 报告提交 SHA、修改文件、测试数量、发布结果和 `git status --short`。
