# FlyPPTTimer v0.20.2

本版针对手机遥控页面的滑动误触进行精确调整，并完成 Gitee 更新下载链路验证。

## 手机端滑动

- “计时 / 演示”仍支持整页任意位置起手、跟随手指平滑移动和松手吸附。
- 手势移动达到方向判定距离后，只有与水平线夹角不超过 35° 才触发横向切页。
- 向上、向下滚动以及大角度斜向手势不会切换模块。
- 已经开始的横向切换仍可被下一次滑动或标签点击立即打断和接管。

## 更新与下载

- 安装版继续支持从 Gitee Release 检测新版本、下载安装包并校验 SHA-256。
- 便携版检测到新版本后打开 Gitee 发行版页面，由用户自行选择文件。
- GitHub 与 Gitee 均提供安装版、便携版和对应校验文件。

## 下载文件

- `FlyPPTTimer-v0.20.2-setup-win-x64.exe`
- `FlyPPTTimer-v0.20.2-setup-win-x64.exe.sha256`
- `FlyPPTTimer-v0.20.2-portable-win-x64.zip`
- `FlyPPTTimer-v0.20.2-portable-win-x64.zip.sha256`
