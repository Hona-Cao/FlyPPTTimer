# 演讲计时器 TEST_REPORT

测试时间：2026-07-02 上午（本机时钟记录）

## 环境

- 操作系统：Windows 10.0.26200 win-x64
- .NET SDK：本地 `.dotnet` 目录内 8.0.422
- PowerPoint：`C:\Program Files\Microsoft Office\Root\Office16\POWERPNT.EXE`
- 测试 PPT：`E:\快传\FlyPPTTimer_GUI\ppt\6月护士长例会内容.pptx`
- 主屏：`\\.\DISPLAY1`，物理坐标 `0,0,2560,1600`
- 副屏：`\\.\DISPLAY2`，物理坐标 `-2560,0,2560,1440`

## 构建

- 当前版本：v0.10.0
- 构建命令：`powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1`
- build：通过，0 warnings，0 errors
- publish：通过，win-x64 self-contained single-file
- exe 路径：`E:\快传\FlyPPTTimer_GUI\dist\FlyPPTTimer.exe`
- 配置路径：`E:\快传\FlyPPTTimer_GUI\dist\FlyPPTTimer.config.json`

## v0.10 本轮外观验证

- 构建结果：通过，0 warnings，0 errors。
- 顶部导航栏：已移除原生 `TabControl` 页签头，改为自绘 `SettingsNavButton` 色块导航，避免原生控件绘制黑色竖线、硬边框和阴影。
- 设置页容器：改为独立内容容器承载各页，保留原有滚动转发和页面切换。
- 启动当前测试版：通过，配置版本写回 `0.10.0`；本轮测试随机端口 `4301`。
- `/state`：通过，返回 `version=0.10.0`、`state=停止`、`displayText=08:00`。
- 本轮未修改远程控制核心逻辑、右键菜单逻辑、PPT 自动启动计时逻辑或文件规则逻辑。

## v0.9 外观记录

- 构建结果：通过，0 warnings，0 errors。
- 设置窗口外框：已改为 `FormBorderStyle.None` 自绘圆角窗体，并对 Form 本身应用圆角 Region，避免系统矩形边框和矩形阴影残留。
- 设置窗口标题栏：已改为自绘标题栏，保留最小化、最大化、关闭、标题栏拖动和边缘缩放。
- 圆角半径：主题层新增统一半径常量，设置窗口内外圆角从 v0.8 的较大半径整体收小约 30%。
- 启动当前测试版：通过，配置版本写回 `0.9.0`；本轮测试随机端口 `11594`。
- `/state`：通过，返回 `version=0.9.0`、`state=停止`、`displayText=08:00`。
- 本轮未修改远程控制核心逻辑、右键菜单逻辑、PPT 自动启动计时逻辑或文件规则逻辑。

## v0.8 回归记录

- 构建结果：通过，0 warnings，0 errors。
- 启动当前测试版：通过，配置版本写回 `0.8.0`。
- 默认位置：通过，首次启动配置为 `Anchor=1（上中）`、`OffsetYPercent=0.5`、`HasCustomPlacement=false`。
- 远程服务启动：通过，本轮测试随机端口 `6226` 启动后写回配置；运行中普通应用设置不会自动重启远程服务或更换本次端口。
- `/state`：通过，返回 `version=0.8.0`、`state=停止`、`displayText=08:00`。
- 文件规则界面：构建通过；支持添加多个 PPT 文件、删除、清空、表格编辑、路径 Tooltip 和下方编辑区。真实 PowerPoint 文件规则自动匹配未在本轮打开全屏放映实测，需用户环境复测。
- 顶部页签：已改为胶囊式 owner draw 页签；本轮未保存新的截图文件。

## v0.7 回归记录

- `/command timer.start`：通过，1.6 秒后再次读取 `/state` 返回 `state=运行中`、`displayText=07:58`、`elapsedMs=1649`，确认远程开始后后台计时持续推进。
- `/command timer.pause`：通过，返回 `state=暂停`，暂停后时间保持当前进度。
- `/command timer.resume`：通过，返回 `state=运行中`，从暂停位置继续。
- `/command timer.stop`：通过，返回 `state=停止`、`displayText=08:00`、`elapsedMs=0`，确认停止即重置。
- `/command window.flash`：通过，命令被接受并返回当前状态；远程闪烁命令已在 UI 线程执行。
- `/command prompt.test`：通过，返回 HTTP 400，确认“测试提示”远程入口已移除。
- 手机端 HTML：通过，包含“已连接/最后同步/已断开/停止并重置”，不包含 `prompt.test` 或“测试提示”。

## 截图问题排查

- 现象：之前多次截图只截到左上角局部，并混入其他窗口。
- 根因：截图进程没有先声明 DPI 感知，高 DPI 主屏会把 2560×1600 映射成缩放后的逻辑坐标，`CopyFromScreen` 按错误坐标取图。
- 处理：截图前调用 `SetProcessDpiAwarenessContext(-4)`，再读取 `Screen.AllScreens`。
- 验证：`tests\screen1_settings_final_v0.6.png` 尺寸为 2560×1600，完整覆盖主屏 1。

## 设置窗口验证

- 设置窗口支持拖拽调整大小：通过。
- 底部“应用、取消、确定”按钮完整显示：通过。
- 默认设置窗口截图：`tests\screen1_settings_rounded_final_v0.6.png`。
- 设置分组：已合并为“时长设置、行为设置、外观与显示、远程控制、控制设置、其他设置”。
- 不再需要的额外遥控设置入口：已删除。
- “其他设置”横向挤压：发现并修复，改为统一两列表单。
- 小圆角外观：通过。按钮、输入框、下拉框、表格外壳、分组条和右键菜单已应用小圆角。
- 右键菜单截图：`tests\screen1_context_menu_rounded_v0.6.png`。

## 默认显示验证

- 默认窗口尺寸：200×60。
- 默认字号：20。
- 默认显示：`08:00`，不显示小时。
- 规则：计时超过或设置到 1 小时后显示 `00:00:00`。

## 远程控制测试

- 测试端口：临时启用 `11835`。
- v0.6 `/state`：通过，返回 `displayText=08:00`、`state=停止`、`version=0.6.0`。
- `/command timer.start`：通过，返回 `state=运行中`、`displayText=07:59`。
- 修复点：后端 JSON 改为小驼峰字段，匹配手机页面脚本读取的 `displayText/state/windowVisible`。
- 当前默认配置为开启远程控制；启动后会监听随机端口并写回配置。
- 计时器右键菜单已接入“远程控制”，可打开二维码窗口。
- 二维码窗口生成本地二维码，并提供复制地址入口。

## PowerPoint 放映测试

- 启动方式：`POWERPNT.EXE /S E:\快传\FlyPPTTimer_GUI\ppt\6月护士长例会内容.pptx`
- 结果：通过。
- 日志关键记录：
  - `Fullscreen state changed: fullscreen=True, process=POWERPNT.EXE`
  - `Timer started.`
- 退出放映重置：通过。
- 退出放映日志关键记录：
  - `Fullscreen state changed: fullscreen=False, process=FlyPPTTimer.exe`
  - `Timer stopped and reset.`
- 说明：v0.6 使用 PowerPoint COM 检测 `SlideShowWindows.Count`，同时保留顶层全屏窗口扫描。

## 未完成项

- 真实手机同 Wi-Fi、热点、USB 共享网络测试仍需用户环境复测。
- 真实手机扫码访问仍需用户环境复测。
- 自动执行防火墙修复。
- 提示1/提示2/结束提示所有字段的完整 GUI。
