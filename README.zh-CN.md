<div align="center">

# FlyPPTTimer

### 专注演讲，把时间交给 FlyPPTTimer。

面向 **Microsoft PowerPoint** 与 **WPS 演示** 的 Windows 演示计时与现场控制工具。

[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows11&logoColor=white)](#兼容性)
[![Architecture](https://img.shields.io/badge/Architecture-x64-555555)](#兼容性)
[![Rust](https://img.shields.io/badge/Built%20with-Rust-000000?logo=rust&logoColor=white)](#技术)
[![Slint](https://img.shields.io/badge/UI-Slint-2379F4)](#技术)
[![Release](https://img.shields.io/github/v/release/Hona-Cao/FlyPPTTimer?label=Release)](https://github.com/Hona-Cao/FlyPPTTimer/releases/latest)
[![Stars](https://img.shields.io/github/stars/Hona-Cao/FlyPPTTimer?style=flat&logo=github)](https://github.com/Hona-Cao/FlyPPTTimer/stargazers)

**[下载最新版](https://github.com/Hona-Cao/FlyPPTTimer/releases/latest)** · **[完整教程](docs/USER_GUIDE.zh-CN.md)** · **[English](README.md)**

</div>

---

FlyPPTTimer 面向那些**时间真的很重要**的现场：论文答辩、演讲比赛、路演、培训课堂、会议、活动现场，以及任何需要在限定时间内讲清楚的演示。

你不再需要在普通计时器、PowerPoint、第二块屏幕和手机之间来回切换。FlyPPTTimer 把 **倒计时/正计时、PPT 页码、逐页用时、提醒、多屏显示、文稿独立规则、情景模式和手机浏览器遥控** 放进同一套演示工作流。

<p align="center">
  <img src="docs/media/readme/timer-overlay.png" alt="FlyPPTTimer 悬浮计时器，显示主时间、逐页秒表和页码" width="500">
</p>

<div align="center">
<strong>一眼看到：整场还剩多久、这一页讲了多久、现在讲到第几页。</strong>
</div>

## 为什么是 FlyPPTTimer？

普通计时器只负责告诉你时间；FlyPPTTimer 围绕的是**整场演示本身**。

| | 在现场意味着什么 |
|---|---|
| **掌握节奏** | 倒计时、正计时、无限正计时、提前提醒、到时处理与超时显示。 |
| **知道讲到哪里** | 显示当前页 / 总页数，并记录当前幻灯片已经停留多久。 |
| **配合 PowerPoint / WPS** | 识别并控制兼容的桌面演示，而不是与 PPT 完全分离的独立秒表。 |
| **让每块屏幕各司其职** | 演讲者附近保留紧凑浮窗，另一块屏幕可以显示专门的大屏计时。 |
| **手机直接变遥控器** | 同一局域网中用浏览器控制计时和演示，手机无需额外安装 App。 |
| **重复工作不必重配** | 给不同 PPT 保存不同时间规则，并把整套设置保存成情景模式。 |

## 为真实演示现场设计

### 让演讲者只需要专注内容

FlyPPTTimer 可以保持紧凑，不遮挡演示，同时把真正需要的信息放在眼前。

<p align="center">
  <img src="docs/media/readme/timer-overlay.png" alt="紧凑的 FlyPPTTimer 演示计时浮窗" width="520">
</p>

主计时支持 **倒计时**、**正计时** 和 **无限正计时**。PowerPoint / WPS 放映时，下方还可以同时显示当前页停留秒数和 PPT 页码，让你随时回答两个问题：

> 我整场还剩多少时间？  
> 这一页我已经讲了多久？

### PowerPoint / WPS 不再只是计时器旁边的另一个窗口

FlyPPTTimer 可以独立计时，但真正体现价值的是它与演示文稿的联动。

兼容的幻灯片放映开始后，可以显示页码并提供演示控制；逐页秒表显示当前一次停留时间，手机端则可以查看本轮演示中每一页的累计用时。

<table>
<tr>
<td width="52%"><img src="docs/media/readme/remote-presentations.png" alt="电脑端演示文稿管理"></td>
<td width="48%"><img src="docs/media/readme/mobile-slide-times-zh.png" alt="手机端逐页用时"></td>
</tr>
<tr>
<td align="center"><strong>电脑端管理演示文稿</strong></td>
<td align="center"><strong>手机端查看逐页用时</strong></td>
</tr>
</table>

## 手机，就是你的演示遥控器

在电脑打开 FlyPPTTimer Remote，让手机与电脑处于同一可信局域网，然后直接用浏览器连接。

**手机无需安装专用 App。**

<table>
<tr>
<td width="50%" align="center"><img src="docs/media/readme/mobile-timer-zh.png" alt="FlyPPTTimer 手机计时控制" width="330"></td>
<td width="50%" align="center"><img src="docs/media/readme/mobile-presentation-zh.png" alt="FlyPPTTimer 手机演示控制" width="330"></td>
</tr>
<tr>
<td align="center"><strong>计时控制</strong></td>
<td align="center"><strong>演示控制</strong></td>
</tr>
</table>

根据启用的工作流，浏览器遥控可以帮助你：

- 开始、暂停、停止与重置计时；
- 不回到键盘前也能操作演示控制；
- 管理受控演示文稿列表；
- 查看本轮逐页用时；
- 切换已经保存的情景模式；
- 在电脑明确授权后，浏览并加入本机 PPT 文件。

<p align="center">
  <img src="docs/media/readme/remote-connection.png" alt="FlyPPTTimer 电脑端远程连接窗口" width="720">
</p>

Remote 面向可信的本地网络使用。不要公开真实二维码和完整远程控制地址。

## 为多显示器而生

真正的演示现场往往不只有一块屏幕。

FlyPPTTimer 既可以使用紧凑悬浮计时器，也可以开启专用大屏计时。你可以在演讲者附近保留低干扰的小浮窗，同时把醒目的大时间放到扩展屏、返看屏、舞台屏幕或工作人员显示器上。

<p align="center">
  <img src="docs/media/readme/big-screen-timer.png" alt="FlyPPTTimer 大屏计时器" width="820">
</p>

显示设置可以控制计时器外观、透明度、位置、目标显示器，以及大屏是否同时显示页码和逐页时间等信息。

<p align="center">
  <img src="docs/media/readme/settings-appearance-zh.png" alt="FlyPPTTimer 外观与显示设置" width="820">
</p>

## 不同 PPT，用不同时间

5 分钟的开场、15 分钟的主题汇报、8 分钟的答辩，不应该每次都重新改计时器。

FlyPPTTimer 支持**按演示文稿保存独立规则**。例如：

| 演示文稿 | 计时方式 |
|---|---:|
| `开场.pptx` | 05:00 倒计时 |
| `产品介绍.pptx` | 15:00 倒计时 |
| `答辩.pptx` | 08:00 倒计时 |
| 开放讨论 | 无限正计时 |

对于比赛、答辩、多人会议、活动流程和经常重复的演示任务，这会省掉大量临场重新设置。

## 一键切换整套演示配置

情景模式不是简单的“时长预设”，而是把一整套工作状态保存下来，需要时快速切换。

例如：

| 情景 | 示例配置 |
|---|---|
| **答辩** | 8 分钟倒计时 · 提前提醒 · 紧凑演讲者浮窗 |
| **比赛** | 5 分钟倒计时 · 大屏计时 · 醒目超时状态 |
| **课堂** | 无限正计时 · 多屏显示 · 显示逐页时间 |

<p align="center">
  <img src="docs/media/readme/settings-scenarios-zh.png" alt="FlyPPTTimer 情景模式设置" width="820">
</p>

情景可以从设置界面和其他支持的控制入口快速切换，非常适合重复举办的活动或固定演示流程。

## 提醒要发生在“来不及”之前

演示计时真正有价值的时刻，不只是数字归零的那一秒。

FlyPPTTimer 支持可配置的提示点、计时结束行为、视觉反馈和超时显示，让演讲者能提前调整节奏，而不是一直盯着时钟。

<p align="center">
  <img src="docs/media/readme/settings-behavior-zh.png" alt="FlyPPTTimer 行为与提醒设置" width="820">
</p>

## 适合这些场景

- **论文答辩 / 学术汇报** —— 严格掌握陈述时间与问答节奏。
- **演讲比赛 / 限时比赛** —— 倒计时、提前提示和大屏时间可以组合使用。
- **路演 / 产品 Demo** —— 同时关注总时长和每页节奏，不打断演示。
- **培训 / 课堂** —— 正计时或无限计时适合开放式课程与讨论。
- **会议 / 多位演讲者** —— 为不同 PPT 配置独立时长，并复用固定现场设置。
- **活动执行 / 会务** —— 工作人员可以在其他位置通过浏览器遥控计时与演示。

## 30 秒开始使用

1. **[下载最新版](https://github.com/Hona-Cao/FlyPPTTimer/releases/latest)**，选择便携版或安装版。
2. 启动 **FlyPPTTimer**，计时浮窗会直接出现。
3. 打开 **设置**，选择时长和计时方式并应用。
4. 默认快捷键下，按 **F3** 开始/暂停，按 **F4** 停止并重置。
5. 开始兼容的 PowerPoint / WPS 放映，即可使用页码等演示联动功能。
6. 需要手机控制时，在电脑打开 **远程控制**，让手机在同一局域网中通过浏览器连接。

更完整的设置、截图、控制方式、文件规则、多显示器定位、手机遥控与故障排查，请查看 **[完整中文教程](docs/USER_GUIDE.zh-CN.md)**。

## 功能总览

| 计时 | 演示文稿 | 显示 | 控制 |
|---|---|---|---|
| 倒计时 | PowerPoint 联动 | 悬浮计时器 | 全局快捷键 |
| 正计时 | WPS 演示联动 | 多显示器 | 托盘 / 计时器菜单 |
| 无限正计时 | PPT 页码显示 | 专用大屏计时 | 手机浏览器遥控 |
| 提前提示 | 逐页秒表 | 外观与透明度 | 演示控制 |
| 到时 / 超时显示 | 每份 PPT 独立规则 | 位置与屏幕选择 | 情景模式切换 |
| 情景模式 | 本轮逐页用时 | 自动紧凑尺寸 | 配置导入/导出 |

## 兼容性

- **系统：** Windows 10 / Windows 11
- **架构：** x64
- **独立计时：** 不安装 Microsoft Office 也可以使用
- **演示联动：** 需要兼容的桌面版 Microsoft PowerPoint 或 WPS 演示
- **手机遥控：** 使用现代浏览器，并与电脑处于可互通的本地网络
- **发布包：** Releases 提供便携版与安装版

## 技术

FlyPPTTimer 当前桌面应用使用 **Rust + Slint** 构建，并结合 Windows 原生能力完成演示与桌面工作流；手机遥控则由本机提供浏览器界面。

上方技术徽章描述的是当前应用使用的技术。GitHub 右侧的仓库语言统计只会分析这个公开发布/文档仓库中实际存在的文件，因此不一定代表应用完整实现的语言比例。

## 下载与文档

- **最新版：** [GitHub Releases](https://github.com/Hona-Cao/FlyPPTTimer/releases/latest)
- **全部版本：** [Release 历史](https://github.com/Hona-Cao/FlyPPTTimer/releases)
- **中国大陆：** [Gitee Releases](https://gitee.com/hona-cao/fly-ppttimer/releases)
- **完整中文教程：** [docs/USER_GUIDE.zh-CN.md](docs/USER_GUIDE.zh-CN.md)
- **Complete English guide:** [docs/USER_GUIDE.en.md](docs/USER_GUIDE.en.md)
- **Bug / 功能建议：** [GitHub Issues](https://github.com/Hona-Cao/FlyPPTTimer/issues)

## Star History

如果 FlyPPTTimer 对你的演示工作流有帮助，可以给仓库一个 Star，方便以后找到，也能直观看到项目一路成长的轨迹。

<a href="https://www.star-history.com/?repos=Hona-Cao%2FFlyPPTTimer&type=date&legend=top-left">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=Hona-Cao/FlyPPTTimer&type=date&theme=dark&legend=top-left">
    <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=Hona-Cao/FlyPPTTimer&type=date&legend=top-left">
    <img alt="FlyPPTTimer Star History Chart" src="https://api.star-history.com/chart?repos=Hona-Cao/FlyPPTTimer&type=date&legend=top-left">
  </picture>
</a>

## 反馈

发现 Bug 或有功能建议，可以提交 [GitHub Issue](https://github.com/Hona-Cao/FlyPPTTimer/issues)。建议附上 FlyPPTTimer 版本、Windows 版本、PowerPoint/WPS 版本、复现步骤和必要截图；发布截图前请移除真实二维码、远程访问地址、私人路径以及演示文稿中的敏感内容。
