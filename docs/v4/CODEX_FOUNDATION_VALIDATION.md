# 给 Codex 的指令：验证 FlyPPTTimer 4.0 基础分支

请在 Windows 10/11 环境中验证仓库 `Hona-Cao/FlyPPTTimer` 的分支 `agent/v4-foundation`。

## 任务范围

1. 检出 `agent/v4-foundation`。
2. 安装或使用 `global.json` 指定的 .NET 10 SDK。
3. 依次执行：

```powershell
dotnet --info
dotnet restore tests\FlyPPTTimer.Tests\FlyPPTTimer.Tests.csproj
dotnet restore tests\FlyPPTTimer.Core.Tests\FlyPPTTimer.Core.Tests.csproj
dotnet restore src\FlyPPTTimer\FlyPPTTimer.csproj -r win-x64
dotnet build src\FlyPPTTimer\FlyPPTTimer.csproj -c Release --no-restore
dotnet build src\FlyPPTTimer.Core\FlyPPTTimer.Core.csproj -c Release --no-restore
dotnet test tests\FlyPPTTimer.Tests\FlyPPTTimer.Tests.csproj -c Release --no-restore
dotnet test tests\FlyPPTTimer.Core.Tests\FlyPPTTimer.Core.Tests.csproj -c Release --no-restore
dotnet publish src\FlyPPTTimer\FlyPPTTimer.csproj -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o artifacts\publish
```

## 修复规则

- 若出现编译或测试错误，直接修复并补充对应测试。
- 只处理 4.0 基础构建、Core 计时引擎和 CI 相关问题。
- 不重写页面，不修改 PowerPoint/WPS 第三方软件，不改变现有用户功能。
- 不删除旧 `TimerService`；本阶段新 Core 与旧实现并存。
- 保持 `main` 不变，只向 `agent/v4-foundation` 提交。

## 交付内容

- 提交所有必要修复。
- 列出执行过的命令及结果。
- 若无法通过，说明准确错误、文件和行号，不要只给概括性描述。
