# FlyPPTTimer v1.13.1

## 中文

FlyPPTTimer 是免费的 Windows PPT 计时器与手机演示遥控工具，支持 PowerPoint / WPS、正计时和倒计时、文稿独立时长、页数显示、提醒及多显示器。当前桌面程序使用 Rust + Slint，不再需要 .NET。

### 下载与安装

本版提供两个文件，没有单独的 SHA256 附件：

- **FlyPPTTimer-v1.13.1-portable-win-x64.zip**：全部解压，运行 `FlyPPTTimer.exe`，保留旁边的运行库 DLL。
- **FlyPPTTimer-v1.13.1-setup-win-x64.zip**：解压后运行里面的安装 EXE，支持中文／英文、当前用户安装和桌面快捷方式。

两个包使用相同的已验收应用 EXE，包含所需 VC 运行库。升级前退出旧版并备份自己的配置、提示音；安装器不以默认配置覆盖既有配置。

### 本版和此前 RC 的累计变化

- 手机列表内也能横滑切换模块，长列表滚动到底后接续页面滚动；短列表不再阻挡页面。
- 百分比滑块支持拖拽、悬停滚轮每格1个百分点、右侧精确输入，以及实时预览、应用保存、取消恢复。
- 电脑 Remote 的文件名／文件路径、时长、模式对齐，列表更紧凑。
- 修改语言重启后，设置的确定／取消不再导致软件退出；每次启动计时器重新显示，隐藏只对本次运行有效。
- 累积引入现代 PPT 多选文件选择器、首次 Remote 10035 修正、深浅主题、页数独立排版、小中大圆角、自动／自定义尺寸、大屏计时和受控文稿排序管理。
- 中英文首页与完整教程重写，电脑／手机图片来自当前 GUI 渲染；真实 UX/RC 提交及更新记录保留。

### 使用说明

[中文首页](https://github.com/Hona-Cao/FlyPPTTimer/blob/v1.13.1/README.zh-CN.md) · [完整教程](https://github.com/Hona-Cao/FlyPPTTimer/blob/v1.13.1/docs/USER_GUIDE.zh-CN.md) · [迭代记录](https://github.com/Hona-Cao/FlyPPTTimer/blob/v1.13.1/docs/DEVELOPMENT_HISTORY.md)

F3开始／暂停，F4停止重置，F5显示／隐藏。手机和电脑同网，打开电脑Remote后扫描自己的实时二维码。文档截图里的二维码是示例，不可用于连接。

**渠道说明**：已验收 EXE 的内置更新器仍使用 Gitee，本次是 GitHub 发布，没有自动同步 Gitee。获取本版请直接下载本页 ZIP；不要期待内置更新器自动安装 ZIP。

## English

FlyPPTTimer is a free Windows presentation timer and phone remote for PowerPoint/WPS: countdown/count-up, per-file durations, slide numbers, alerts and multiple displays. The current Rust + Slint desktop application does not need .NET.

### Downloads

- **Portable ZIP**: extract everything and run `FlyPPTTimer.exe`; keep its accompanying runtime DLLs.
- **Setup ZIP**: extract and run the installer EXE; per-user English/Chinese installation and optional shortcut.

Both contain the same accepted executable and app-local VC runtimes. Exit the old version and back up personal settings/sounds before upgrading. No separate checksum assets are uploaded.

### Highlights

Mobile list swipes now switch modules and chain vertical scrolling; percentage sliders gain one-point wheel steps and exact entry; Remote rows are aligned and compact. Closing Settings after a language restart no longer exits the app, and every process launch restores timer visibility.

This release also includes the preceding RC work: modern PPT-only file picking, the first-request 10035 fix, light/dark themes, independent slide-number typography, smaller corners, automatic/custom sizing, fullscreen timing and controlled-list management. READMEs, detailed bilingual tutorials, current GUI renders and the real development history are included.

[English guide](https://github.com/Hona-Cao/FlyPPTTimer/blob/v1.13.1/docs/USER_GUIDE.en.md) · [Development history](https://github.com/Hona-Cao/FlyPPTTimer/blob/v1.13.1/docs/DEVELOPMENT_HISTORY.md)

**Update-channel note**: the accepted executable still checks Gitee and recognizes standalone installer EXEs. This GitHub release is not a Gitee publication; download/extract these ZIPs manually.

## Provenance

Executable source: `e625c809f8cf7515f0143fab476bb4467d2fe1d7`.
[Accepted Windows CI](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34709344878): formatting, Clippy, 90 passed / 0 failed / 3 ignored, web syntax, Release build and startup/Remote checks. The user subsequently approved publication. Documentation renders are not claimed as physical phone/Office testing.
