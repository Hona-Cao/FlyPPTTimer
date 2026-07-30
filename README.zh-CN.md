# FlyPPTTimer

[English](README.md) | **简体中文**

<p align="center">
  <img src="src/FlyPPTTimer/Assets/app.png" width="88" alt="FlyPPTTimer 图标">
</p>

<p align="center">
  <strong>为演讲、教学、会议和医疗汇报准备的 Windows 演示计时器</strong><br>
  PowerPoint / WPS 联动 · 手机局域网遥控 · 正计时与倒计时 · 多显示器悬浮显示
</p>

<p align="center">
  <a href="https://github.com/Hona-Cao/FlyPPTTimer/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/Hona-Cao/FlyPPTTimer?display_name=tag&sort=semver"></a>
  <a href="https://github.com/Hona-Cao/FlyPPTTimer/actions/workflows/windows-ci.yml"><img alt="Windows CI" src="https://github.com/Hona-Cao/FlyPPTTimer/actions/workflows/windows-ci.yml/badge.svg"></a>
  <img alt="Windows 10/11" src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows">
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet">
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/License-MIT-green.svg"></a>
</p>

FlyPPTTimer 是一款开源、免费、无需云端账户的 Windows 演示计时工具。它把清晰的悬浮时间、演示文稿规则、到时提醒和手机遥控集中在一个应用中，适合 PPT 汇报、培训授课、课堂演示、会议发言以及护理和医疗科室例会等场景。

当前开发版本为 **v0.30.2**，正在进行本地实机测试，尚未发布。当前公开稳定版仍为 **v0.20.2**。

### v0.30.2 窗口缩放与控件细节修复

- 缩小设置下拉框圆角；收起状态改为完全自绘，清除启用和禁用状态残留的上边线、左边线。
- 设置页禁用色块改为无边框的实色圆角绘制，不再出现半透明接缝。
- 设置窗口拖动缩放期间不再逐像素重建窗口圆角区域，结束拖动后统一恢复。
- 远程控制窗口拖动缩放期间暂停整页响应式重排和演示文稿轮询，结束后一次性同步。
- 恢复远程导航按钮完整的下半部圆角。
- 演示文稿列表禁止横向滚动，长文件名保持省略号显示。

### v0.30.1 导航与下拉框视觉修复

- 远程控制导航删除“页面导航”文字和浅蓝圆角背景，只在两个模块按钮下方保留一条横向分隔线。
- 从 Windows 原生窗口样式中移除启用和禁用下拉框残留的黑色实线边框。
- 加大下拉框圆角，使收起状态的圆角轮廓更清晰。
- 展开的下拉列表同样移除原生边框，并对整个列表窗口应用圆角裁剪。

### v0.30.0 大屏窗口与设置控件修复

- 大屏计时器改为可调整大小的标准窗口，支持最小化和最大化。
- 大屏计时器仅允许使用扩展屏，禁止选择主屏幕；没有扩展屏时禁用该功能并隐藏屏幕下拉框。
- 关闭大屏模式时，目标屏幕选项直接改变原圆角表面的颜色，不再叠加尺寸不同的灰色色块。
- 修复大屏计时器误释放 WinForms 共享字体后，右键打开“设置”导致程序崩溃退出的问题。
- 所有动态禁用控件统一直接切换原容器颜色。
- 下拉框改为无实线边框、无阴影的圆角样式，与主窗口保持一致。

### v0.20.9 导航与显示设置优化

- 远程控制的两个模块入口放入独立导航区域，与页面内功能按钮明确区分。
- 网络说明合并为一处，并补充可用手机热点或电脑热点建立局域网。
- 手机端删除语言切换，始终根据手机系统语言自动显示中文或英文。
- “外观与显示”把“计时器窗口”调整为首个功能，并新增“大屏计时模式”二级分组。
- 启用“所有屏幕同时显示”时，“单屏显示屏幕”自动禁用，避免两个选项产生歧义。
- 下拉框统一完整圆角色块、内容居中，并恢复跟手的原生滚轮滚动。

### v0.20.8 计时与远程控制增强

- 加宽、加高演示文稿页的四个工具按钮，并统一英文详情区的字段间距。
- PowerPoint 状态每秒刷新时不再重复写入未变化的标题，修复 “Not selected” 闪烁。
- 设置页新增普通计时器窗口显示开关，以及可指定显示器的独立大屏计时器。
- 设置页浅灰控件统一使用圆角裁剪。
- 手机遥控新增“重新计时”：优先按当前文稿规则时长重置，否则使用全局默认时长，并立即开始。
- 新增“关闭当前文稿”，与“关闭最后打开的文稿”分别提供。

### v0.20.7 远程控制界面精简

- 可编辑控件恢复浅蓝色背景，与浅灰色只读、禁用控件明确区分。
- 文稿列表行只显示文件名和时长，删除“待打开”和行内“禁用规则”。
- 工具栏新增“清空列表”，并放大添加、删除、刷新、清空列表四个按钮的文字。
- 删除文稿动作提示条和整个退出软件卡片。
- 顶部导航按钮缩窄并加大间距，选中时仅显示浅蓝背景与蓝色文字，不再显示框线。

### v0.20.6 远程控制界面一比一重制

- 按确认的中英文效果图，将远程控制窗口重制为约 700 × 510 逻辑客户区的紧凑布局。
- 对齐顶部导航、标题、状态栏、二维码与浏览器分栏，以及文稿列表、详情、放映和退出卡片的比例与留白。
- 固定布局行改为按当前 DPI 换算，避免 Windows 缩放后纵向尺寸被压缩。
- 升级时将旧版过大的远程窗口尺寸迁移到新效果图尺寸。

### v0.20.5 远程控制界面重构

- 删除左侧栏，改为顶部两个相互独立的圆角模块按钮。
- 按统一间距重构远程连接与演示文稿页面，卡片边线对齐，并在窗口缩小时提供局部滚动。
- 只读与禁用控件采用整块灰色状态；保存、删除、放映和结束放映按钮按实际状态启用。
- 更新连接字段、文稿控制和退出软件区域的中英文用语。
- 设置导航栏增加垂直空间并使用内缩绘制，四个圆角均完整显示。
- 自动验证 100%、125%、150%、175% 显示缩放下的逻辑布局。

### v0.20.4 界面精简

- 设置导航栏始终单行显示，空间不足时自动扩宽设置窗口。
- 远程连接页不再显示或编辑端口，新安装和旧版随机端口配置默认使用固定端口 `4080`，已设置的固定端口继续保留。
- 浏览器访问地址自动选择，删除不再需要的“选择”按钮。

### v0.20.3 多语言支持

- 桌面端支持 English、简体中文和“跟随系统”。
- 安装器默认读取 Windows 显示语言，并允许安装前手动选择语言。
- 便携版首次启动按本机显示语言显示。
- 手机/浏览器遥控页默认跟随设备语言，并提供独立语言选择。
- 切换语言不会覆盖现有计时设置或文件规则；桌面端更改语言后需重启软件生效。

代码仓库：[GitHub（主仓库）](https://github.com/Hona-Cao/FlyPPTTimer) · [Gitee（中国大陆镜像）](https://gitee.com/hona-cao/fly-ppttimer)。两个平台均提供正式版下载；中国大陆用户可优先使用 Gitee。

## 下载

| 版本 | 适合人群 | GitHub | Gitee（中国大陆推荐） |
|---|---|---|---|
| 安装版 | 希望通过安装程序完成部署的普通用户 | [下载 v0.20.2 安装版](https://github.com/Hona-Cao/FlyPPTTimer/releases/download/v0.20.2/FlyPPTTimer-v0.20.2-setup-win-x64.exe) | [下载 v0.20.2 安装版](https://gitee.com/hona-cao/fly-ppttimer/releases/download/v0.20.2/FlyPPTTimer-v0.20.2-setup-win-x64.exe) |
| 便携版 | 希望解压即用、配置随程序保存的用户 | [下载 v0.20.2 便携版](https://github.com/Hona-Cao/FlyPPTTimer/releases/download/v0.20.2/FlyPPTTimer-v0.20.2-portable-win-x64.zip) | [下载 v0.20.2 便携版](https://gitee.com/hona-cao/fly-ppttimer/releases/download/v0.20.2/FlyPPTTimer-v0.20.2-portable-win-x64.zip) |

校验文件：[安装版 SHA-256](https://github.com/Hona-Cao/FlyPPTTimer/releases/download/v0.20.2/FlyPPTTimer-v0.20.2-setup-win-x64.exe.sha256) · [便携版 SHA-256](https://github.com/Hona-Cao/FlyPPTTimer/releases/download/v0.20.2/FlyPPTTimer-v0.20.2-portable-win-x64.zip.sha256) · [GitHub Release](https://github.com/Hona-Cao/FlyPPTTimer/releases) · [Gitee 发行版](https://gitee.com/hona-cao/fly-ppttimer/releases)

> v0.20.2 限制手机端切页手势的水平触发角度，纵向滚动不再容易误切换“计时 / 演示”模块；继续使用压缩自包含单文件，无需预装 .NET。

> 目前提供 Windows x64 版本。Windows 首次运行或启用远程控制时，可能询问是否允许访问网络；如需手机遥控，请仅允许“专用网络”并确保手机和电脑连接同一局域网。

## 界面预览

| 计时与文件规则 | 外观与显示 |
|---|---|
| <img src="docs/media/settings-duration.png" alt="计时与文件规则设置" width="100%"> | <img src="docs/media/settings-appearance.png" alt="外观与显示设置" width="100%"> |

| 电脑端远程连接 | 电脑端演示控制 |
|---|---|
| <img src="docs/media/remote-connect.png" alt="电脑端远程连接与二维码" width="100%"> | <img src="docs/media/remote-presentations.png" alt="电脑端演示文稿控制" width="100%"> |

<p align="center">
  <img src="docs/media/mobile-timer.jpg" width="310" alt="手机端计时控制">
  &nbsp;&nbsp;
  <img src="docs/media/mobile-presentation.jpg" width="310" alt="手机端演示控制">
</p>

<details>
<summary>查看更多：提示与行为设置</summary>

<p align="center">
  <img src="docs/media/settings-alerts.png" width="760" alt="提示与行为设置">
</p>

</details>

## 能做什么

### 计时与到时处理

- 支持倒计时和正计时，以及开始、暂停、继续、停止和重置。
- 默认计时窗口为 100×35、微软雅黑 18 号粗体；文字始终在窗口中水平、垂直居中。
- 窗口尺寸或文字宽度变化时，以“默认点位 + 微调”为固定中心原点向四周调整，避免时间显示不全。
- 倒计时到零后可选择停止，或继续以另一种颜色显示已超出的时间。
- 到时后可仅提示、全屏黑屏显示“时间到”，或退出当前放映。
- 提示 1、提示 2 和计时结束可分别配置语音/提示音与闪烁；自选音频会复制到应用自己的存储目录。

### PowerPoint、WPS 与文件规则

- 可为不同演示文稿保存独立时长、计时方式和启用状态。
- 支持勾选多条文件规则后批量修改时长和正/倒计时方式。
- FlyPPTTimer 打开的受控文稿使用只读方式；外部打开的文稿也可按“最后打开优先”逐个静默关闭。
- 支持从头放映、从当前页放映、上一页、下一页、跳页、黑屏、白屏和结束放映。
- 放映结束、黑屏到时动作和计时状态之间保持同步。
- 针对 WPS 外层演示窗口进行识别和首次显示最大化处理。

### 手机或浏览器遥控

- 手机无需安装 App，在浏览器中扫描二维码即可使用。
- 手机端可以调整计时时长、切换正计时/倒计时，并同步保存到电脑；修改全局时长时可选择是否同步全部文件规则。
- “计时”和“演示”页面既可点击标签切换，也可跟随手指平滑左右滑动；只有明确的横向手势才切页，纵向滚动保持原页面。
- 支持显示/隐藏计时窗口、触发闪烁、控制电脑主音量静音。
- “计时”和“演示”页面都可以退出“时间到”黑屏。
- 手机端实时显示连接、计时、静音、演示和命令执行状态。
- 远程链接使用随机 token；“断开所有设备”后旧链接和旧二维码立即失效。

### 显示、配色与可靠性

- 使用 Per-Monitor V2 DPI，适配 100%、125%、150% 等缩放比例。
- 支持多显示器、九宫格默认点位、百分比微调和计时窗口位置重置。
- 远程控制窗口支持标准/紧凑响应式布局，并记忆显示器、位置、大小和最大化状态。
- 默认采用医疗卫生蓝白配色，同时提供教育、商务、科技和高对比预设。
- 配置采用原子写入和备份恢复；日志按日期及大小轮转。
- 可在设置中选择是否在启动时检测新版本，也可从系统托盘手动检测；自动检测默认关闭。
- 安装版确认后可下载安装更新并保留现有配置，绿色便携版则打开 Release 页面供用户自行选择文件。
- 单实例运行，并捕获 UI、后台任务和未处理异常。

## 三步开始使用

1. 下载并安装，或解压便携版后运行 `FlyPPTTimer.exe`。
2. 右键计时窗口或托盘图标打开“设置”，按需要调整时长、提示、配色和显示位置。
3. 需要手机遥控时打开“远程控制”，让手机与电脑连接同一网络，然后扫码访问。

首次启动会在程序目录生成：

- `FlyPPTTimer.config.json`：设置与文件规则。
- `logs/app-日期.log`：运行与错误日志。

升级安装版不会主动覆盖现有用户配置。重要活动前仍建议提前打开演示文稿、扫码连接并进行一次完整彩排。

## 兼容性说明

| 能力 | Microsoft PowerPoint | WPS 演示 | 其他全屏程序 |
|---|---:|---:|---:|
| 全屏时自动联动计时 | 支持 | 支持顶层窗口识别 | 可通过白名单识别 |
| 打开演示文稿 | 支持 | 支持 | 不适用 |
| 从头/当前页放映 | 支持 | 取决于 WPS 兼容接口 | 不适用 |
| 翻页、跳页、黑白屏 | 支持 | 取决于 WPS 兼容接口 | 不适用 |
| 只读受控与逐个静默关闭 | 支持，包含外部打开的文稿 | 按实际检测能力提供 | 不适用 |

WPS 不同版本暴露的兼容接口可能不同，程序会显示实际检测到的能力，不会把未确认的操作伪装为可用。

## 局域网与隐私

- FlyPPTTimer 不依赖云端账户，不会主动上传演示文稿内容。
- 配置、文件规则、自选提示音和日志默认保存在本机。
- 远程控制仅用于同一局域网；请勿将端口转发到公网，也不要公开仍然有效的二维码或完整 token。
- Clash、TUN、代理和常见虚拟网卡地址不会作为推荐扫码地址；优先选择真实 Wi-Fi 或以太网局域网地址。
- 日志、截图和 Issue 中可能包含演示文稿路径，公开前请先检查并移除敏感信息。

## 快捷键

默认快捷键包括：

- `F3`：开始/暂停。
- `F4`：停止/重置。
- `F5`：显示/隐藏计时窗口。

其余控制可在设置窗口中查看和调整。设置窗口本身通过鼠标从计时窗口或托盘菜单打开。

## 从源码构建

环境要求：Windows 10/11、.NET 8 SDK、PowerShell。生成正式安装包还需要 Inno Setup 6（`winget install --id JRSoftware.InnoSetup --exact`）。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

也可以直接使用仓库本地 SDK：

```powershell
.\.dotnet\dotnet.exe restore
.\.dotnet\dotnet.exe build src\FlyPPTTimer\FlyPPTTimer.csproj -c Release
.\.dotnet\dotnet.exe test tests\FlyPPTTimer.Tests\FlyPPTTimer.Tests.csproj -c Release
```

## 项目故事

FlyPPTTimer 由 **曹虎男** 发起并从零开发。作者毕业于南京大学医学院护理专业，目前就职于江苏省人民医院宿迁医院。在工作实践中发现了演讲计时、演示控制和台下远程调整的实际需求，因此逐步将这个想法实现为本项目。

希望它能让大家的演讲、教学、会议和医疗汇报更加从容，也欢迎有兴趣的朋友参与测试、提出建议或共同开发。祝大家使用愉快！

- 联系邮箱：[`caohunan@smail.nju.edu.cn`](mailto:caohunan@smail.nju.edu.cn)
- 问题与建议：[GitHub Issues](https://github.com/Hona-Cao/FlyPPTTimer/issues)
- 参与开发：[CONTRIBUTING.md](CONTRIBUTING.md)
- 完整版本记录：[CHANGELOG.md](CHANGELOG.md)

## 赞赏与支持

如果 FlyPPTTimer 帮你节省了准备和控场时间，欢迎在**完全自愿、量力而行**的前提下请我喝杯咖啡。你的支持将用于持续测试、适配和维护；无论是否赞赏，软件的免费使用和开源功能都不会受到影响。

<p align="center">
  <img src="docs/media/donate-alipay.jpg" width="260" alt="支付宝赞赏码">
  &nbsp;&nbsp;&nbsp;
  <img src="docs/media/donate-wechat.png" width="260" alt="微信赞赏码">
</p>

<p align="center">支付宝 · 微信</p>

## 参与贡献

欢迎提交 Issue 和 Pull Request。UI 问题请附上系统版本、屏幕分辨率、缩放比例和脱敏截图；PowerPoint/WPS 问题请同时说明软件版本及复现步骤。完整指南见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## License

FlyPPTTimer 使用 [MIT License](LICENSE) 开源。你可以在保留版权和许可证声明的前提下使用、修改和分发本项目。

Copyright © 2026 Cao Hunan（曹虎男）
