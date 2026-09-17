# FlyPPTTimer v1.16.0

## 中文

### 情景模式
- 设置底栏新增“保存模式”，可直接填写名称并按 Enter/确认保存当前已应用配置。
- 情景列表默认折叠、最新创建优先；选择、删除确认、展开/收起集中在列表行。
- 展开后保留名称、切换快捷键、角标颜色和“用当前配置覆盖”等维护项。
- 底栏显示当前情景与颜色；切换后计时器边框短暂以该颜色提示。图标情景色圆点更醒目，右键菜单始终保留情景入口。

### PowerPoint / WPS 打开方式
- 默认使用 Windows 对 `.ppt/.pptx/.pptm` 的系统默认应用。
- 可指定 Microsoft PowerPoint 或 WPS 演示程序；选择时验证可执行文件，非支持程序会被拒绝。
- 电脑端与手机端发起的“打开演示文稿”使用同一打开方式设置。

### 手机遥控与演示退出
- 只要检测到已知 PowerPoint/WPS 演示程序仍在运行，手机端即可使用“退出演示程序”，不再要求当前必须有文稿打开。
- 仍保留受控文稿检查和确认，避免误退出。
- 缩短 Remote 命令分发、服务器接收轮询、演示状态刷新和已连接手机轮询间隔，改善局域网控制反馈。

### 下载、升级与许可
- Windows 10 / 11 x64；公开发布仅提供便携版 ZIP 与安装版 ZIP。
- 安装版升级不覆盖已有 `FlyPPTTimer.config.json`；便携更新继续保留个人配置与 `alert-sounds`。
- v1.15.0 及以前的历史 MIT 权利保持不变；v1.16.0 适用随当前版本提供的 LICENSE。
- GitHub 自动生成的 `Source code` 压缩包只包含公开发布/用户文档仓库的内容，不包含当前 v1.16.0 开发源码；历史已经公开的源码快照仍按其原许可保留。

## English

### Scenario workflow
- Settings adds **Save mode** in the footer. Enter a name and confirm (or press Enter) to save the currently applied configuration.
- Scenario rows start collapsed and newest first. Selection, confirmed deletion, and expand/collapse live directly on each row.
- Expanded details focus on name, switch hotkey, badge color, and updating the stored snapshot from the current configuration.
- The footer shows the active scenario and color; switching briefly highlights the timer border. Badge dots are larger and the scenario submenu stays available even before the first scenario is saved.

### PowerPoint / WPS opener preference
- The default remains the Windows system association for `.ppt/.pptx/.pptm`.
- Microsoft PowerPoint or WPS Presentation can be selected explicitly. Unsupported executables are rejected.
- Desktop and phone-initiated presentation opens use the same preference.

### Remote responsiveness and presentation exit
- The phone **Exit presentation application** action is available whenever a known PowerPoint/WPS presentation process is running, even when no deck is currently open. Controlled-deck checks and confirmation remain.
- Remote dispatch, server accept polling, presentation-state refresh, and connected-phone polling intervals are reduced for faster LAN feedback.

### Downloads, upgrading, and license
- Windows 10 / 11 x64. The public release contains exactly the portable ZIP and setup ZIP.
- Installer upgrades preserve an existing `FlyPPTTimer.config.json`; portable updates preserve personal configuration and `alert-sounds`.
- Historical MIT rights for v1.15.0 and earlier remain unchanged. v1.16.0 is governed by the LICENSE shipped with this release.
- GitHub-generated `Source code` archives contain only the public release/user-documentation repository, not the current v1.16.0 development source. Historical source snapshots already published remain available under their original licenses.
