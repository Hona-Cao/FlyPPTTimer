# FlyPPTTimer v0.13.0 测试报告

## 构建环境

- 系统：Windows 11 x64
- SDK：.NET 8.0.422、Windows SDK 10.0.26100
- UI：WinUI 3、Windows App SDK 1.8 稳定版
- 分支：`feature/v0.13-winui-rebuild`

## 自动化测试

执行：

```powershell
.\.dotnet\dotnet.exe test .\tests\FlyPPTTimer.Tests\FlyPPTTimer.Tests.csproj -c Release
```

结果：28 项通过，0 项失败，0 项跳过。

新增覆盖包括 1/10/50 个 PPT 文件规则、重复路径排除、排序、批量时长、重新定位、放映中删除保护、关闭自动开始、四种退出放映组合和 Finished 最终快照。既有 HTTP 字节读取、请求限制、token、网络过滤、配置原子保存与移动网页状态测试继续通过。

## 本机构建与启动

- Core、Infrastructure.Windows、WinUI：Release/Debug 构建通过，0 warning，0 error。
- 非打包 WinUI 主窗口：真实启动通过。
- 主屏 2560x1600、副屏 2560x1440：分别创建悬浮计时窗口通过。
- 本地 HTTP 服务、托盘窗口及单实例组件：启动通过。
- 本地远控回环测试：`/state`、`/command`、计时实时刷新、CSP 与 `nosniff` 响应头通过；重复启动进程数保持 1。
- 文件规则首张幻灯片预览：实现为 PowerPoint STA 队列异步导出并按文件修改时间缓存。
- 使用 `ppt\6月护士长例会内容.pptx` 完成真实 Microsoft PowerPoint 测试：打开并从头放映、下一页、上一页、跳转第 3 页、黑屏/恢复、白屏/恢复、结束放映均通过。
- 放映开始后计时器立即运行；结束放映后按默认设置停止并重置为 `08:00`，联动通过。

## 需人工验收

- Microsoft PowerPoint：从当前页放映、多文稿、忙碌/受保护文稿仍需补充人工组合测试。
- 文件选择器、外部拖放、列表拖动排序及首张缩略图的完整鼠标交互。
- 浅色、深色、系统主题和 Windows 高对比度。
- 100%、125%、150%、200% DPI 的逐项视觉检查。
- 托盘菜单、快捷键、穿透、锁定及投影模式切换。
- Clash/TUN 条件下手机断联恢复和手机竖屏/横屏。
- 连续运行 8 小时内存趋势。

PowerPoint COM 与上述交互项目无法在无 Office 交互桌面的 GitHub Actions 中可靠执行，因此不伪造为自动化通过。
