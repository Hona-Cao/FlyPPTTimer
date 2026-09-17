# FlyPPTTimer — PPT 计时器、PowerPoint / WPS 演讲倒计时与手机遥控

[English](README.md) · **简体中文** · [完整使用教程](docs/USER_GUIDE.zh-CN.md)

FlyPPTTimer 是一款面向 Windows 的演示计时工具，支持倒计时、正计时、PPT 页数、逐页秒表、多屏显示以及手机浏览器遥控。

> **FlyPPTTimer 是免费开源软件，并将继续保持免费开源。** 项目采用 MIT License。当前已将 **v1.14.1 及之前的历史源码**重新整理并公开到本仓库，更新版本的源码也会继续在这里发布。

## 下载

- [GitHub 最新 Release](https://github.com/Hona-Cao/FlyPPTTimer/releases/latest)
- [GitHub Releases 列表](https://github.com/Hona-Cao/FlyPPTTimer/releases)
- 中国大陆历史下载入口：[Gitee v1.15.0](https://gitee.com/hona-cao/fly-ppttimer/releases/tag/v1.15.0)

当前公开正式版为 **v1.15.0**，支持 Windows 10 / 11 x64。独立计时不需要 Office；页数识别和放映控制需要桌面版 PowerPoint 或 WPS 演示。手机端无需安装 App。

建议只从作者标明的官方发布渠道下载。非官方镜像、重新打包版本或修改版不代表 FlyPPTTimer 官方版本。

## 源码

1.14 系列及之前的历史源码现在可以直接在本仓库查看。

`main` 分支上的可浏览源码快照：

- [v1.14.1](source/v1.14.1/)：对应产品提交 [`6e8bd3a`](https://github.com/Hona-Cao/FlyPPTTimer/commit/6e8bd3aeaec7ae8c247e09b535157db7f490047a) 的完整源码树。
- [v1.14.0](source/v1.14.0/)：保留的 v1.14.0 源码状态。
- [v1.13.1](source/v1.13.1/)：保留的 v1.13.1 源码状态，以及进入 1.14 系列之前的更早开发历史。

另外提供保持原始仓库根目录结构和 Git 祖先链的源码分支：[`source/v1.14.1`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.14.1)、[`source/v1.14.0`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.14.0) 和 [`source/v1.13.1`](https://github.com/Hona-Cao/FlyPPTTimer/tree/source/v1.13.1)。

构建说明、源码来源和版本关系见 [source/README.md](source/README.md)。历史源码与当前发布/文档文件分开存放，避免把旧版本源码误认为当前二进制版本的源码。

## 使用说明

- [完整中文教程](docs/USER_GUIDE.zh-CN.md)
- [English user guide](docs/USER_GUIDE.en.md)

教程包含安装与升级、计时模式、PowerPoint / WPS 联动、手机遥控、情景模式、多显示器、文件规则和故障排查等内容。

## 主要功能

- 倒计时、正计时与无限制正计时
- PowerPoint / WPS 页数识别与放映控制
- 当前页逐页秒表
- 多显示器与大屏计时
- 提前提醒、到时提醒、超时显示
- 每份 PPT 独立时长规则
- 最多 8 个情景模式
- 手机浏览器局域网遥控
- GitHub / Gitee 更新源与应用内更新
- 安装版与便携版

## 开源许可与后续版本

- FlyPPTTimer 采用 [MIT License](LICENSE) 开源。
- 后续正式版本将继续 **免费提供并保持开源**，源码将在本仓库发布。
- 软件许可证不授予对 FlyPPTTimer 名称、图标或品牌进行误导性使用的权利。品牌使用规则见 [TRADEMARKS.md](TRADEMARKS.md)。

第三方组件及致谢见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

## 反馈问题

可以通过 [GitHub Issues](https://github.com/Hona-Cao/FlyPPTTimer/issues) 提交问题。请提供 FlyPPTTimer 版本、Windows 版本、PowerPoint/WPS 版本、复现步骤和必要截图，并遮挡二维码、远程访问地址、私人路径和演示文稿中的敏感内容。
