[记录目录](../README.md) · [原始汇总](../../v1/CODEX_RESULT.md)

# RC-2 最终集成验收与代码冻结候选版

日期：2026-09-11；分支：`codex/v1-06-manual-test`。

本轮拉取并验证 HEAD：`952226d4c114b59334b13d0a7b44c60d89b9f030`。没有修改源码、依赖、版本或发布脚本，没有发现稳定复现的 P0/P1。候选记录见 [RC_CANDIDATE.md](RC_CANDIDATE.md)，最终体验清单压缩为 [五个场景](RC_MANUAL_TEST.md)。未验证的项目没有标为通过。

## 工作区与最终自动验证

- 外层 `E:\快传\计时器` 位于 `agent/v4-foundation`，有用户未跟踪文件，保持原样。本轮使用已独立存在且开始时干净的 `E:\快传\计时器\v1.0` review 工作区，fast-forward 拉取目标分支。
- 已读 AGENTS、HANDOFF、批准覆盖项、基线、当前任务、已有结果，并核对 v0.30.2 默认配置/AppCommandService、外层 v4 单调 Timer 技术参考和相关 Rust 实现。两个用户批准的 Remote 覆盖项保持不变。
- 本机工具路径与旧交接不同。最初 rustup 下载遇 TLS EOF，fmt 尚未执行；后续官方工具链下载成功，实际 `rustc 1.92.0 (ded5c06cf 2025-12-08)`。未改默认工具链、项目锁文件或依赖版本。
- 环境恢复后统一执行一次：fmt 通过；clippy 全 targets/features 且 `-D warnings` 通过；test **55 passed、0 failed、1 ignored**；locked Release build 通过（3m25s）。既有 ignored 项是 Office COM 手测。
- 现有 `scripts/build-release.ps1` 通过。脚本内部的 Release build 复用已构建产物，没有建立第二套脚本。Portable / ZIP / Installer 名称均为 `1.13.0`；Installer ProductVersion `1.13.0`，FileVersion `1.13.0.0`。主程序版本另经真实 `/state` 核对。
- 原始本地日志：`target/rc-temp/fmt.log`、`clippy.log`、`test.log`、`build.log`、`package.log`。临时脚本、配置、声音和截图均在 `target/rc-temp/`，不提交新测试框架。
- 最终证据来自本机 Rust RC 验证，旧 CI 不作为本轮验收依据；未改旧 CI，未创建 Release / Tag，未增加哈希流程。

## 真实 Windows 集成结果

| 项目 | 结果 | 实际方式与边界 |
|---|---|---|
| Portable / Release 启动 | 通过 | 直接运行本轮 Portable EXE；确认 starting 日志、实际 HTTP 服务、版本 1.13.0。未用旧二进制替代。 |
| 第二实例 | 通过 | 同 EXE 再启动，5 秒内 exit 0；仍只有原测试主进程。 |
| F3 / F4 / F5 | 通过 | computer-use 向真实设置窗口发送全局功能键，再经 HTTP 读状态。F3 Running→Paused→Running；暂停两次 elapsed 都是 12126ms，恢复后增长至 23165ms；F4 running=false/elapsed=0；F5 隐藏后 windowVisible=false，再切回 true。Timer 浮窗外观不据此判定。 |
| Timer / 托盘菜单 | 无法可靠验证 | `sky.list_apps/list_windows` 两次没有枚举无标题 Timer/托盘入口，不能可靠定位点击。未建立 Win32/GUI 自动化替代框架。 |
| Settings 打开/关闭/重开 | 通过（限定入口） | 现有 `--show-settings` 打开真实窗口，关闭后重新启动相同入口，窗口完整显示。此参数本身设置 exit_on_close=true；不是同一进程托盘重开测试。 |
| Settings Apply | 通过 | 原生输入 00:04:00，点击应用，配置落盘；导入/恢复默认另有独立运行态核对。 |
| Settings Cancel | 通过 | 修改暂停闪烁开关未应用，点击取消出现基线三选一文案，选择“否”，窗口关闭、磁盘 FlashPausedTime=false；下一实例仍显示正确持久配置。 |
| Import / Reset | 通过 | 仅导入 `target/rc-temp/RC-TEMP-import.json`：3 分钟、3 条临时路径规则。UI、磁盘和 HTTP 分别为 00:03:00/3、180000ms/3；恢复默认后三者为 00:08:00/0、480000ms/0。未打开占位文稿或覆盖用户配置。 |
| PC Remote 两页、重开、端口停顿、Ctrl/Shift 与批量 | 无法可靠验证原生交互 | 托盘入口不可定位，未把既有回调单测算作真机通过。现有 `--capture-windows` 成功退出；检查其 headless 演示文稿页只显示规则管理，无放映控制按钮，仅作为渲染证据。 |
| 固定端口被占用 | 通过 | 停止测试实例后用临时 TcpListener 占住 4080，再启动 Portable；改用 5785 并写回配置，curl 读得版本 1.13.0、持久时长 120000ms。临时 listener 已停止，没有改防火墙。 |
| Remote HTTP 鉴权 | 通过 | 真实进程 localhost `/state`：无 token 403、错 token 403、正确 token 成功；版本 1.13.0。未输出真实 token 到报告。 |
| Remote HTTP Timer | 通过 | `/command` Start/Pause/Resume/Reset，等刷新后读 `/state`；暂停等待 2 秒 elapsed 不变，恢复增长，重置为 0。另测 setDuration/setMode、正计时跨预设时间、倒计时超时、window.hide/show。日志 `target/rc-temp/http.log`。 |
| LAN / 手机 | 本机通过；手机无法可靠验证 | 使用本机可用 IPv4 的 LAN 地址访问同一服务成功。不代表手机跨设备、防火墙或无线网络已通过。 |
| PowerPoint / WPS 与多放映 | 无法安全自动验证 | 开始前发现 WPS 已有用户文稿，未证明其为临时文件，不操作/保存/关闭/强退。按任务安全规则不执行 Office 控制，包括可能选中用户文稿的跨应用命令；未生成测试 Office 文稿，未将软件已安装当作验收通过。 |
| 双屏 / DPI | Settings 当前屏通过；其他无法可靠验证 | WinForms 确认双屏，GetDpiForMonitor 为 144/120（150%/125%）。Settings 截图完整可用。跨屏 drag 被工具拒绝：`point (-950, 70) is outside window bounds`；未改系统缩放。Timer/PC Remote/大屏定位和混合 DPI 移动仍待确认。 |
| 声音 / TTS | 调用与存活检查通过；听感无法验证 | ffmpeg 生成 0.3 秒 WAV/MP3；MCI open/play/close 对两者均返回 0，SAPI Speak 成功返回。安装版启用默认结束提示，2 秒倒计时到超时后仍响应 HTTP/Reset、日志无崩溃。未改系统音量/静音；不宣称听感或应用 WAV/MP3 UI 选取通过。 |
| Portable 配置持久化 | 通过 | HTTP 保存 2 分钟至程序同目录，退出/重启后读得 120000ms，后续 Settings 又完成回读。前两次重启清理使用仅限测试 PID 的 Stop-Process，不能据此声称正常退出。 |
| 正常退出 / 进程清理 | 通过（设置启动模式）；托盘退出未验收 | 后续 Portable 和 Installer 均通过 `--show-settings` 的正常 Cancel/Discard 路径退出，日志 stopped，进程消失。早期 CloseMainWindow 在默认关闭到托盘配置下没有退出，随后清理测试 PID；不将该行为判为卡死。最终无测试主进程。 |
| Installer | 隔离新装/覆盖/启动/卸载通过；旧版升级未验收 | 检查 HKCU/HKLM/WOW6432Node 卸载项及常见目录，无现有安装。原安装器定向 `target/rc-temp/installer`，静默、不建图标、不关闭其他应用。安装 exit 0；本轮配置改为 7 分钟，同版本重装保留；安装版 UI/HTTP 为 7 分钟/420000ms。正常退出后卸载 exit 0，EXE 和安装登记移除，测试配置/日志按设计保留。未覆盖用户正式安装。 |

## 执行中遇到的工具限制与复核

- 最初 HTTP 脚本在命令后 250ms 读取上一轮缓存状态，稍后状态正常；临时脚本改为等待 1100ms，再运行全部 HTTP 验证通过。只改临时脚本，不改产品刷新周期。
- 端口回退/后续实例上 PowerShell Invoke-RestMethod 曾报连接被中止；同一端点使用 curl 鉴权、读状态和执行命令成功。未据此推断产品根因或修改服务器；通过证据包含 curl 实际结果，不把失败的 PowerShell 调用记为成功。
- 文件对话框 UIA set_value 返回 CacheRequest 属性错误，刷新后用观察到的文件名框输入并成功导入；未操作原目录里的用户文件。
- 捕获被其他窗口遮挡时重新激活目标窗口观察，没有将其他应用画面作为计时器证据。不提交桌面截图，避免包含用户其他窗口或路径。

## 收尾与候选冻结

- Portable 配置已恢复仓库默认值：8 分钟、空 token、0 条规则；日志移至 `target/rc-temp/portable-logs/`。ZIP/Installer 在测试前生成，未打入测试配置。
- 临时安装已卸载，临时 listener 停止，测试主进程退出；WPS 用户文稿未受影响。
- 只提交 `CODEX_RESULT.md`、`RC_MANUAL_TEST.md`、`RC_CANDIDATE.md`。候选源码仍是 `952226d4c114b59334b13d0a7b44c60d89b9f030`；不因纯文档变更重复全部构建。
- **代码冻结候选版，除最终验收发现 P0/P1 外不再修改源码**。推送后停止，等待 ChatGPT 最终审核。

---
