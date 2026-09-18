# FlyPPTTimer v1.14.0

## 中文

### 更新检查与完整说明
- 更新窗口支持调整大小和纵向滚动，不再截断更新内容。
- 默认启用启动检查，仅发现新版本时弹窗；无更新或离线时不干扰使用。升级至 1.14 时启用一次，此后你仍可在设置中关闭并保存。
- 同时检查 Gitee 和 GitHub，选择可访问源中的最新正式版。ZIP 更新打开发布页下载，不会在未确认时自动安装。

### 计时器与桌面界面
- 水平和垂直默认偏移改为 0；自动尺寸变化后重新定位，水平居中按各显示器实际边界计算。
- 设置与电脑 Remote 垂直排版更紧凑。
- 新增逐页正计时秒表：设置→外观→显示逐页秒表。只在实际放映时计时，换页后本页秒数从 00 重新开始。
- 逐页秒表和页数固定在主计时器下方同一行，分别左对齐和右对齐。秒表字号默认跟随页数，可独立设置字号和颜色。移除页数上下位置和对齐选项。
- 启用页数或秒表时，空闲状态也保留完整布局，显示 -/- 和 00，不再因未读到页数而收缩。

### 手机遥控
- 翻页、放映和关闭按钮移至文稿列表上方。
- 新增浏览电脑 PPT 文件：先在电脑设置→远程控制中允许手机浏览，再在手机演示页点浏览按钮。支持本地磁盘、文件夹、上一级和名称筛选，仅允许 .ppt/.pptx/.pptm 加入受控列表。
- 浏览默认关闭，仍需有效遥控令牌。不提供下载、删除、修改任意文件或网络共享访问。
- 手机显示本页秒数和本轮逐页累计用时，回到同一页会累计，结束放映后保留到下一轮或退出软件。秒表与主计时器独立，暂停主计时不暂停当页停留时间。
- 主题、排序和已打开文件下拉框使用统一圆角、颜色、动画和键盘交互，适配深色与浅色。

## English

- Update notes are no longer truncated. A resizable, scrollable window displays the full release body. Startup checking is enabled by default (and once when upgrading from versions before 1.14); later opt-out remains available. Automatic checks are quiet unless a newer stable version is found.
- Gitee and GitHub are checked together; the newest reachable stable release is used. ZIP updates open the release page and do not install without confirmation.
- Both placement offsets default to zero. Content-size changes re-anchor the overlay, and horizontal centering uses each monitor's full bounds including side-taskbar layouts and mixed DPI.
- Settings and desktop Remote have tighter vertical spacing.
- The optional per-slide stopwatch counts elapsed seconds during a slideshow, restarting the visit counter on slide changes. It sits below the main timer on the left, with slide numbers on the right. Font defaults to the slide-number size; custom font/color or time-color following are supported. Idle metadata keeps its row with 00 and -/-.
- Phone presentation commands now precede the file list. Computer PPT browsing is opt-in on the PC, token-protected and read-only: navigate local drives/folders, filter names and add .ppt/.pptx/.pptm to the controlled list. No arbitrary downloads, deletion or network-share access.
- The phone includes accumulated per-slide times for the current/last show; revisits accumulate. A new show starts a new record. This clock is independent of pausing the main timer. Records are session-only.
- All phone dropdowns use consistent rounded styling, theme colors, motion and keyboard navigation.

## Downloads / 下载

- `FlyPPTTimer-v1.14.0-portable-win-x64.zip`: extract everything and run FlyPPTTimer.exe.
- `FlyPPTTimer-v1.14.0-setup-win-x64.zip`: extract and run the installer.

Exit the previous version and keep your configuration and sounds when upgrading. No SHA256 attachments. GitHub's automatic source archives are not application packages.
