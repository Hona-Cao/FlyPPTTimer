# FlyPPTTimer v0.13.1 测试报告

## 发布范围

- 分支：`feature/v0.13.1-compatibility`
- 基线：v0.12.1 稳定 WinForms 版本（提交 `a75c740`）
- 本版仅恢复兼容性并保留既有美工，不引入 WinUI 运行时，不重构计时、远控、PPT 检测或设置功能。

## 自动化测试

执行命令：

```powershell
.\.dotnet\dotnet.exe test .\tests\FlyPPTTimer.Tests\FlyPPTTimer.Tests.csproj -c Release -p:NuGetAudit=false
```

结果：18 项通过，0 项失败，0 项跳过。

## 桌面交互回归

- 原设置窗口已恢复，六个原有页签及其设置内容完整保留。
- 按 `F6` 能打开设置窗口，窗口保持响应。
- 双屏显示两个计时悬浮窗时，任务栏中只有设置窗口；两个计时窗口均不生成任务栏预览。
- 实际鼠标右键点击计时窗口能弹出原功能菜单。
- 点击菜单外空白处，右键菜单立即关闭。
- 在右键菜单选择“退出”后，进程正常结束，无需任务管理器强制关闭。
- 连续启动两次最终 EXE，仅保留 1 个进程，单实例保护有效。
- v0.13 WinUI 页面及动画未进入 v0.13.1 运行路径。
- 使用原始 `Assets/app.ico`，未替换软件标志。

验收截图：`tests/acceptance/v0.13.1/settings-restored.png`。

## 构建验证

- 目标版本：v0.13.1
- 目标平台：Windows x64、.NET 8、自包含单文件。
- `dist\v0.13.1` 和 `releases\v0.13.1` 均为独立目录，不覆盖旧版本。
- Release 构建及自包含单文件发布：0 个警告，0 个错误。
- EXE SHA-256：`7D3D1A2CDEEDC81AA5FE40DF5D1607811D16C28105CB240DE471FB6EA9F78330`。
- 安装程序 SHA-256：`96973CE1629508BD823AD4AE88645B95F46765A09EC75AD504DD95A1C0942E62`。
- 绿色版 ZIP SHA-256：`8F31222E69AF92A5822F884F21E344CB9473337EFDBFA2854A78B7A0EF35D159`。

## 仍需人工验证

- 托盘溢出区图标在用户当前任务栏布局下的鼠标右键操作。
- Microsoft PowerPoint COM 全流程及手机真机远控。
- 100%、125%、150% DPI 下的长期使用体验。
