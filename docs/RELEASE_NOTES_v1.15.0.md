# FlyPPTTimer v1.15.0

## 中文

### 默认逐页计时与大屏时间模式
- 全新配置默认开启逐页秒表；普通悬浮窗继续在主时间下方左侧显示当前页秒数，右侧显示页数。
- 大屏计时器默认只显示主时间，不显示页数或逐页秒表。若确实需要，可在“外观与显示 → 大屏显示页数与逐页秒表”单独开启。

### 应用内自动更新
- “其他设置 → 软件更新”可选择 **Gitee** 或 **GitHub**。首次使用时，Windows 地理区域为中国大陆（CN）默认 Gitee，其他区域默认 GitHub；之后以保存的选择为准。
- 发现新版本后，更新窗口继续显示完整 Release 说明。点击“下载并安装”后由 FlyPPTTimer 自动下载对应 ZIP、解压、更新并重启，不再要求手工打开网页。
- 安装版自动运行新安装器；便携版在退出后原地替换程序文件，同时保留 `FlyPPTTimer.config.json` 与 `alert-sounds`。

### 情景模式
- 设置左侧在“控制设置”和“其他设置”之间新增 **情景模式**，最多保存 8 个配置快照。
- 模式名称、切换快捷键和图标角标颜色都可自定义；前 8 个模式自动使用易区分的默认颜色。
- “保存当前配置”会记录计时、行为、外观、控制、显示和文件规则。可用于竞赛、招聘、大屏教学、演讲者提醒等不同场景。
- 当前情景的颜色以右下角圆点显示在托盘图标上；托盘和计时器右键菜单都可直接切换情景模式。
- 手机计时页新增情景模式下拉框，局域网 Remote 也可切换当前模式。

### 无限制计时
- “时长设置”的默认时长右侧新增 **无限制**。启用后固定为正计时，不再使用预设到时点。
- 无限制时隐藏“到达预设时间后”“时间到后的操作”、提前提醒、结束提醒和超时样式；适合不限时授课、开放讨论等场景。
- 文件规则保留，关闭无限制后可继续使用原来的每文件时长和模式。

## English

### Per-slide timing defaults and big-screen time-only mode
- Fresh configurations enable the per-slide stopwatch by default. The ordinary overlay keeps current-slide seconds on the left and slide numbers on the right below the main time.
- Fullscreen/big-screen timing is time-only by default. Slide numbers and the per-slide stopwatch can be enabled separately under Appearance & Display when needed.

### In-app automatic updates
- Choose **Gitee** or **GitHub** under Other → Software Updates. First use defaults to Gitee when the Windows geographic region is mainland China (CN), otherwise GitHub; a saved choice is retained.
- The full release notes remain visible. Choosing **Download and install** downloads the matching ZIP, extracts it, updates the application and restarts without sending the user to a browser.
- Installed editions run the extracted setup package. Portable editions replace application files after exit while preserving `FlyPPTTimer.config.json` and `alert-sounds`.

### Scenarios
- A new **Scenarios** main settings page, between Controls and Other, stores up to eight configuration snapshots.
- Each scenario has a custom name, switch hotkey and badge color; eight distinct colors are supplied as defaults.
- Snapshots cover timer, behavior, appearance, controls, display placement and presentation rules. They can represent competition, recruiting, teaching, speaker-reminder and other workflows.
- The active scenario appears as a colored dot on the tray icon. Switch from the tray/timer context menus, a global hotkey, or the phone Remote timer page.

### Unlimited timer
- **Unlimited** sits beside the default duration. When enabled, the timer is fixed to count-up and has no effective target time.
- Target/overtime/time-up controls and end reminders disappear while unlimited timing is active. Existing presentation rules are retained for later use.

## Downloads
The public release contains exactly the portable ZIP and setup ZIP. No separate checksum attachment is published. GitHub's automatically generated source archives are developer source, not application packages.
