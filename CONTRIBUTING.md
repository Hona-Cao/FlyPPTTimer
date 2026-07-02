# 参与维护

欢迎一起改进 FlyPPTTimer。

## 开发环境

- Windows 10/11
- .NET 8 SDK，仓库内可使用 `.dotnet` 目录
- PowerShell

## 构建

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

构建产物会输出到 `dist\`。

## 提交建议

- 一个 PR 尽量只解决一个问题。
- UI 问题请附截图，说明屏幕分辨率和缩放比例。
- 远程控制、PPT 自动检测、计时逻辑相关改动请写清测试步骤。

## 发布

发布包由维护者从当前 `dist\` 生成：

- `FlyPPTTimer_Portable_v*.zip`：绿色版。
- `FlyPPTTimer_Setup_v*.exe`：安装版。
