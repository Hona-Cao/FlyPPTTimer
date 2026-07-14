# FlyPPTTimer v0.13 架构

## 项目边界

- `FlyPPTTimer.Core`：配置模型、计时状态机、放映生命周期、文件规则、远控命令和唯一应用状态源；不引用 WinUI 或 WinForms。
- `FlyPPTTimer.Infrastructure.Windows`：PowerPoint COM STA、HTTP 服务、配置原子保存/恢复、日志轮转、网络地址过滤、全局热键和提示输出。
- `FlyPPTTimer.WinUI`：窗口、页面、MVVM、托盘、悬浮窗、多屏布局、二维码和嵌入式手机资源。
- `FlyPPTTimer`：保留的 v0.12.1 WinForms 回退项目，不参与 v0.13 运行时组合。

## 状态与线程

`ApplicationStateStore` 是桌面、悬浮窗、二维码页和手机网页的共同状态源。`PowerPointControlService` 使用容量为 32 的专用 STA 队列，带命令超时、COM 忙碌重试、状态缓存和日志节流。WinUI ViewModel 不直接持有 COM 对象。

计时使用 `Stopwatch` 与后台脉冲，不依赖 UI 定时器；所有窗口通过同一快照更新。放映开始和结束只进入 `PresentationLifecycleController`，按自动开始、文件规则和退出组合决定计时行为。

## 发布方式

`package_winui_release.ps1` 从 WinUI csproj 读取版本，拒绝覆盖既有 `releases\v*` 和 `dist\v*`。绿色版为非打包、自包含 win-x64 文件夹及 ZIP；安装版写入 `%LOCALAPPDATA%\FlyPPTTimer`，升级前备份配置且不覆盖既有配置。
