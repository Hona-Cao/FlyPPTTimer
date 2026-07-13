# WinUI 3 现代化原型评估

分支：`ui-modernization-winui`

## 已安装环境

- .NET SDK 10.0.301
- Microsoft.WindowsAppSDK.WinUI.CSharp.Templates 0.0.6-alpha
- Windows App SDK 2.2.0（模板 NuGet 依赖）

## 原型边界

- 仅原型化设置窗口和远程控制二维码窗口。
- v0.11 WinForms 悬浮计时窗、托盘、全屏检测和 PowerPoint COM 保持不变。
- 后续共享 Core 项目应先提取纯模型、配置验证、计时状态和远控 DTO；WinForms/WinUI 均通过接口调用。

## 初步结论

- Fluent 控件、明暗主题、Mica、NavigationView 和轻量过渡由 WinUI 3 原生支持。
- PowerPoint COM 可继续由独立 STA 服务提供，与 UI 框架无关。
- 托盘菜单、透明置顶、鼠标穿透和无边框悬浮窗仍需 Win32 互操作专项验证，当前不迁移。
- WinUI 3 非打包单文件发布有限制；便携版通常需要自包含 Windows App SDK 文件，体积和部署复杂度高于当前 WinForms 单文件。
- 在托盘、透明悬浮窗、多屏 DPI 和便携发布全部实测通过前，不建议全量迁移；若其中任一项不稳定，应保留 WinForms 悬浮层，并考虑 WPF + Fluent 作为桌面设置界面的备选。
