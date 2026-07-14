# v0.13 开发环境记录

本机验证环境：Windows 11 x64、.NET SDK 8.0.422、Windows SDK 10.0.26100、WinUI 3 模板和 Windows App SDK 1.8 稳定通道。本轮没有安装 Preview、Experimental 或 nightly 组件，也没有要求管理员权限或系统重启。

主要 NuGet 依赖：

- `Microsoft.WindowsAppSDK` 1.8.260317003
- `Microsoft.Windows.SDK.BuildTools` 10.0.26100.7705
- `Microsoft.Windows.SDK.BuildTools.WinApp` 0.3.1
- `CommunityToolkit.Mvvm` 8.4.0
- `QRCoder` 1.6.0
- `H.NotifyIcon.WinUI` 2.3.0

测试依赖沿用稳定版 `Microsoft.NET.Test.Sdk` 17.12.0、xUnit 2.9.2 和 xUnit runner 2.8.2。仓库内 `.dotnet`、`.packages`、`bin`、`obj`、`dist` 和 `releases` 均被忽略，不提交 SDK、依赖缓存或生成产物。
