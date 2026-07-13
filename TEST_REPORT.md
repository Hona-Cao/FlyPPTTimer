# FlyPPTTimer v0.12.1 测试报告

## 构建环境

- 系统：Windows 11 x64
- 运行时：.NET 8
- 当前版本：v0.12.1
- 分支：`feature/v0.12-stability-ui`

## 自动化测试

执行命令：

```powershell
.\.dotnet\dotnet.exe test .\tests\FlyPPTTimer.Tests\FlyPPTTimer.Tests.csproj -c Release
```

结果：18 项通过，0 项失败。

覆盖范围：

- `AutoStartOnFullscreen=false` 时放映不启动或重置计时器。
- 退出放映的停止/重置四种组合。
- 倒计时结束时 Updated 和 Finished 获得同一 Finished 最终快照。
- UTF-8 请求体字节长度、超大请求体和不完整请求体。
- token 固定时间比较、日志 URL 脱敏和旧 token 失效。
- 私有局域网与 Clash/TUN 地址过滤。
- 手机演示列表 busy 结束后的按钮恢复路径。
- 配置原子保存、用户值保留、损坏配置备份恢复。

## 本机回归

- Release 构建：通过，0 warnings，0 errors。
- 单文件 win-x64 自包含发布：通过。
- `/state` 和 `/command`：使用本地服务验证响应、安全头和旧 token 拒绝。
- 单实例：重复启动不会创建第二套托盘、快捷键或远程服务。
- PowerPoint 自动开始开关：使用 `ppt\6月护士长例会内容.pptx` 通过同一手机接口从头放映；`AutoStartOnFullscreen=false` 时放映状态为运行中，计时器保持“停止”，测试通过。
- `releases\v0.12.1`：新目录创建成功，未覆盖 v0.12。

## 需人工验证

- Microsoft PowerPoint COM：从当前页放映、翻页、跳页、黑白屏和结束放映的完整交互复测。
- PowerPoint 忙碌、受保护文稿、多个已打开文稿及非活动放映窗口。
- 手机在 Clash 规则模式、电脑在 Clash TUN 模式下的断联与恢复。
- 100%、125%、150% DPI，以及手机竖屏和横屏触控体验。

上述 PowerPoint COM 项目不能在无 Office 交互桌面的 GitHub Actions 中可靠执行，明确作为本机集成测试，不标记为 CI 通过。
