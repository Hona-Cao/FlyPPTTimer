# Codex 任务：接入统一演示控制接口

## 目标

把 FlyPPTTimer 的远程服务、远程控制窗口和应用上下文从具体的
`PowerPointControlService` 类型切换到 `IPresentationControlService`，但不得改变任何现有命令、提示文字、线程模型、COM 调用或页面布局。

本任务只做依赖接线。不要拆分 `PowerPointControlService` 内部代码，不要修改 PowerPoint 或 WPS，不要增加第三方依赖。

## 工作分支

在现有分支 `agent/v4-foundation` 上工作。不要修改 `main`，不要合并 PR。

## 已存在的基础

- `src/FlyPPTTimer/Services/IPresentationControlService.cs`
- `src/FlyPPTTimer/Services/PowerPointPresentationAdapter.cs`
- `tests/FlyPPTTimer.Tests/PresentationControlAbstractionTests.cs`

## 必须修改

### 1. `FlyPPTTimerContext.cs`

- 将字段 `_powerPoint` 的类型改为 `IPresentationControlService`。
- 构造时先创建原有服务，再用适配器包装：

```csharp
_powerPoint = new PowerPointPresentationAdapter(
    new PowerPointControlService(() => _config, _log));
```

- 保留全部事件订阅和 `GetState()` 调用原样。
- 保留现有释放顺序，确保底层服务只释放一次。

### 2. `RemoteControlService.cs`

- 将字段 `_powerPoint` 的类型改为 `IPresentationControlService?`。
- 将构造函数对应参数改为 `IPresentationControlService?`。
- 将 `PresentationController` 属性类型改为 `IPresentationControlService?`。
- 不改字段名称，不改 HTTP 路由，不改命令白名单，不改返回文案。

### 3. `RemoteControlForm.cs`

- 将字段 `_powerPoint` 的类型改为 `IPresentationControlService?`。
- 其他 `GetState()`、`Execute()`、`Queue()` 调用保持原样。
- 不改任何布局、尺寸、字体、颜色、刷新间隔或提示文字。

### 4. 测试

扩展 `PresentationControlAbstractionTests.cs`：

- 通过反射验证 `RemoteControlService` 构造函数使用 `IPresentationControlService?`。
- 验证 `RemoteControlService.PresentationController` 的属性类型为 `IPresentationControlService`。
- 读取 `FlyPPTTimerContext.cs`，验证它创建 `PowerPointPresentationAdapter`。
- 读取 `RemoteControlForm.cs`，验证字段不再声明为 `PowerPointControlService?`。

## 禁止事项

- 不得修改 `PowerPointControlService.cs` 的内部实现。
- 不得修改 COM 重试、STA 线程、500ms 状态刷新和命令超时。
- 不得修改 PowerPoint/WPS 功能或能力判断。
- 不得修改设置窗口和远程控制窗口布局。
- 不得升级 NuGet 包。
- 不得重命名远程命令。
- 不得删除兼容字段或旧客户端接口。

## 验证命令

```powershell
dotnet restore tests/FlyPPTTimer.Tests/FlyPPTTimer.Tests.csproj
dotnet restore tests/FlyPPTTimer.Core.Tests/FlyPPTTimer.Core.Tests.csproj
dotnet build src/FlyPPTTimer/FlyPPTTimer.csproj -c Release --no-restore
dotnet test tests/FlyPPTTimer.Tests/FlyPPTTimer.Tests.csproj -c Release --no-restore
dotnet test tests/FlyPPTTimer.Core.Tests/FlyPPTTimer.Core.Tests.csproj -c Release --no-restore
dotnet restore src/FlyPPTTimer/FlyPPTTimer.csproj -r win-x64
dotnet publish src/FlyPPTTimer/FlyPPTTimer.csproj -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o artifacts/publish
```

## 完成标准

- 构建 0 errors。
- 全部桌面测试通过。
- 全部 Core 测试通过。
- 单文件发布成功。
- Git diff 仅包含上述三处接线、测试和必要文档调整。
- 不合并 PR；提交到 `agent/v4-foundation` 后停止，并报告提交 SHA 与测试结果。
