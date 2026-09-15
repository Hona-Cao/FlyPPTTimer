# FlyPPTTimer v1.14.1 — 已交付构建说明 / Delivered-build notes

**状态：已交付使用，尚未创建 v1.14.1 公开 Release。** 本页是变更说明，不是新发布公告。当前公开下载仍为 [v1.14.0](https://github.com/Hona-Cao/FlyPPTTimer/releases/tag/v1.14.0)。[中文使用图解](USER_GUIDE.zh-CN.md#v1141) · [Illustrated English guide](USER_GUIDE.en.md#v1141)

Source: [`6e8bd3a`](https://github.com/Hona-Cao/FlyPPTTimer/commit/6e8bd3aeaec7ae8c247e09b535157db7f490047a) · [Build and package record](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34992459836).

## 连接后直接打开文件访问授权

手机成功连接后，如果还未允许文件浏览，电脑自动打开“设置→远程控制”，高亮“允许手机浏览电脑 PPT 文件”。勾选后点应用，再回手机点“浏览电脑 PPT 文件”。同一设备在本次服务运行中只提示一次，不会因持续刷新反复打断演示。已有权限时不重复打开设置。

连接本身不自动授权。此开关适用于持有当前遥控地址的设备，仍只允许浏览本地文件夹和加入 PPT，不提供任意文件下载、修改或删除。

## 更紧凑的计时浮窗

保持字号、左侧逐页秒表和右侧页数不变，减小两行之间的留白和窗口四周内边距。自动尺寸会同步收紧；手动设置的固定宽高不会被覆盖。

## 零偏移真正贴齐屏幕边缘

默认点位不再使用假定的 140×50 窗口计算，改用实际浮窗尺寸：上中 + 垂直 0% 时上边缘贴屏幕上边；下中 + 0% 时下边缘贴屏幕下边。九宫格其他边缘点位使用相同规则，支持负坐标副屏和不同 DPI。

这里的边缘是整块屏幕，不是任务栏上方的工作区。已保存的非零偏移继续保留，要贴边请设为 0%并应用。

## English

- A new authenticated phone connection opens desktop Settings directly at Remote control, highlights PPT file browsing and explains how to enable it and Apply. The same device is not announced again during polling/reconnects within the running service. Already enabled permission does not reopen Settings. Connecting never grants permission automatically.
- Tighter timer row spacing and outer padding, preserving font sizes, fixed left/right metadata and custom window sizes.
- Zero-offset anchors use the actual window dimensions and full monitor edges. Top-center is flush with the top; bottom-center is flush with the bottom. The same rule covers other edge anchors, negative-origin monitors, DPI scaling, resizing and drag capture.

This build is delivered on the review branch. Existing public release tags are unchanged; no public Release was requested for this follow-up.
