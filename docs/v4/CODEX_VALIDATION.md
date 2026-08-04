# Codex 验证指令（FlyPPTTimer 4.0）

在分支 `agent/v4-foundation` 上执行。不要切换到 `main`，不要合并 PR，不要修改功能或页面。

## 目标

验证当前 4.0 基础代码，包括：

- 现有 WinForms 兼容入口。
- 平台无关 `FlyPPTTimer.Core`。
- 新增 WPF 项目 `FlyPPTTimer.Desktop`。
- 桌面与 Core 自动化测试。
- 现有正式入口的 Windows x64 自包含单文件发布。

## 命令

```powershell
dotnet --info

dotnet restore tests/FlyPPTTimer.Tests/FlyPPTTimer.Tests.csproj
dotnet restore tests/FlyPPTTimer.Core.Tests/FlyPPTTimer.Core.Tests.csproj
dotnet restore src/FlyPPTTimer.Desktop/FlyPPTTimer.Desktop.csproj
dotnet restore src/FlyPPTTimer/FlyPPTTimer.csproj -r win-x64

dotnet build src/FlyPPTTimer/FlyPPTTimer.csproj -c Release --no-restore
dotnet build src/FlyPPTTimer.Core/FlyPPTTimer.Core.csproj -c Release --no-restore
dotnet build src/FlyPPTTimer.Desktop/FlyPPTTimer.Desktop.csproj -c Release --no-restore

dotnet test tests/FlyPPTTimer.Tests/FlyPPTTimer.Tests.csproj -c Release --no-restore
dotnet test tests/FlyPPTTimer.Core.Tests/FlyPPTTimer.Core.Tests.csproj -c Release --no-restore

dotnet publish src/FlyPPTTimer/FlyPPTTimer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  --no-restore `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o artifacts/publish
```

## 必须检查

1. 三个项目均为 `0 errors`。
2. 所有桌面测试通过；当前基线为 201 项或更多。
3. 所有 Core 测试通过。
4. `artifacts/publish/FlyPPTTimer.exe` 存在。
5. 不启动或自动操作本机 PowerPoint/WPS。
6. 不修改文件，不运行格式化器，不提交代码。
7. 报告每条命令的退出码、测试数量和最终 EXE 大小。

## 说明

- 当前发布命令生成的仍是 WinForms 兼容入口。
- WPF 项目目前只要求成功编译，不是可交付测试版。
- 真正的 PowerPoint/WPS、投影仪、多显示器和 DPI 验证由人工测试阶段完成。
