# RC3.2 - ChatGPT direct implementation

Implemented the 2026-09-12 user feedback on automatic/custom sizing, page typography/position/alignment, large-screen overlay exclusion, metadata sorting, controlled-file removal, animated mobile reordering/long press and non-selectable mobile text. Requirements and scope: USER_FEEDBACK_20260912_3.md. Codex did not implement this round and remains paused.

This source note does not predeclare CI success. The final Windows run must pass fmt, clippy -D warnings, production regression tests, node --check and release build before packaging. BUILD.txt records the exact patched-source commit and EXE SHA256. Chromium mock-HTTP interaction checks and software Slint captures are separate from physical mobile/Office/mixed-DPI acceptance.

Earlier unmentioned items are provisionally passed according to the user, not newly retested by ChatGPT. Only RC32_MANUAL_TEST.md items require focused user feedback.

---
## Historical results below

# 第二轮反馈：家用 Windows 整合与交付

日期：2026-09-12。分支：`codex/v1-06-manual-test`，版本保持 `1.13.0`。本轮唯一源码基点为从 GitHub 拉取的 `b193befab4d67f0f7dbec0cd84ca0ebe61b445ff`；未使用办公室工作区、未提交状态、构建产物或日志。下方旧日期内容仅为历史记录，不是本机验证证据。用户已确认的最终尺寸、滚动条、作者说明、手机基本控制、音频和全屏黑屏继续视为通过。

## 实现与验证结论

| 项目 | 本轮状态 | 证据与边界 |
| --- | --- | --- |
| U01 混合 DPI 拖动 | 环境限制，残余问题未解决 | 本机两台物理屏均为 2560×1440、120 DPI（125%），没有真实混合 DPI 条件。保留已通过的管理窗口 DPI 稳定器，未凭猜测修改拖动流程；不能把同 DPI 双屏当作 150%/125% 验收。 |
| U03 弹窗 | 部分真实桌面通过；其余未逐一验证 | 125% 中英文未应用修改原生对话框文字/按钮完整，取消继续编辑、放弃还原正常；原生按钮跟随中文 Windows。PC Remote 批量无效时长错误文字可见，modal 背景禁用。真实 Edge 长确认框发现 Tab 未限制、ESC 未关闭，已补焦点循环、背景 inert、ESC 和焦点恢复；最终英文删除确认框 Tab/ESC 通过。本机深色系统主题令固定浅色管理窗口的标准控件白字白底，实际截图证实后仅将这两个窗口的标准控件 Palette 固定为浅色。导入/恢复默认/更新的每种错误分支未穷举，150% 视觉没有本机证据。 |
| U07 原子切换 | WPS 真实桌面通过；原生 PowerPoint 环境限制 | 单 STA 操作先结束异目标放映，再打开/激活/启动指定目标；不经两个 HTTP 请求。可丢弃三页 A/B 验证 Open、从头、从当前页（编辑页为第 1 页）切换，A 停止、B 接管 300 秒规则，A 原规则 180 秒。dirty A 切换后仍 Saved=0，B 保持 clean，两文稿仍打开。无放映 Next 明确失败，不自动开映。同目标重复启动短路，关闭路径传播结束放映错误。末尾另尝试从已清理文稿的状态重新打开 A 再确认关闭，worker 超时，未取得本轮真实关闭通过证据；不将此追加尝试算作成功。自动回归覆盖无中间 stop 采样的 A→B 生命周期和隐藏规则计时。 |
| U08 手机列表 | 配置/HTTP/桌面浏览器通过；真实触控环境限制 | 隐藏独立于 enabled；隐藏入口可恢复；删除须确认，仅改规则；只添加已打开文稿；上下移动持久保存。真实 EXE 验证隐藏/恢复/排序、删除后文稿仍打开且磁盘字节不变、重新加入成功。修复真实 Office 返回 `\\?\` 路径造成同文稿重复身份、addOpen 失败的问题，兼容普通路径和 UNC。旧配置默认、Settings 合并、排序和不删磁盘文件自动测试通过。手机触控尚无实际设备证据。 |
| U09 断线恢复 | 客户端自动验证及普通浏览器恢复通过；真实手机环境限制 | 对实际轮询函数做受控验证：前两次失败容错、第三次离线、后续 GET 恢复；online/focus/visibility 事件保留，不重放 POST。普通 Edge 在同 token/port 程序重启后恢复。真实 GET 曾出现 WinError 10053，下一 GET 可用，临时工具只重试 GET，未重发 POST；不据此宣称长时稳定，也未另改服务端网络架构。未完成手机约 10 分钟、锁屏、后台、Wi-Fi 开关验收。 |
| U10 页码 | 自动及真实桌面通过 | 默认开启，旧配置亦默认开启；无有效页码隐藏。普通/镜像及大屏实际显示 1/3，字号层级和居中正常。自动测试覆盖 1/23、无效页码和稳定预留宽度；翻页文本不写配置、不重启 timer。 |
| U11 自适应与预览 | 自动及同 DPI 双屏真实桌面通过；混合 DPI 环境限制 | 替换只增大且写回基准的旧逻辑，用同一测量结果同步 Slint 内容和物理窗口，可缩回基准。实际发现原生框变大而 Slint 根仍 100×35，导致大字号裁切，已用显式 content-width/height 修复。60 字号实际 223×106 完整显示，32 字号含页码 126×93、关页码回 126×61；默认单行 125×44。宽高预览 220×100 对应 275×125，Apply 前磁盘仍 100/35/18，Apply 后 220/100/60；放弃恢复应用值，页码开关放弃后磁盘仍 true。仅这四字段进入预览，其他 draft 不提前应用。真实 Slint 测量测试覆盖大字号、1/23 双行、回缩、不改基准；混合 DPI 与全部超长时间视觉组合不冒充实测。 |
| 直接小修：ESC / ppt.* | 真实桌面通过 | 最终 EXE 产生两屏 2560×1440 遮罩；真实键盘按住 ESC 250ms 后状态清除；重新到时后 `ppt.next` 清除遮罩，仍无放映。瞬时 SendKeys 脉冲首次未被轮询捕获，不将该次标为通过；正常持续按键验证通过。 |
| 直接小修：音频 | 保留源码/既有通过结论 | 10 秒上限及按原 basename、slot 子目录复制保留 fdadde3 实现，未重复实现。完整自动检查通过；本轮未另做长音频可听时长测量，不冒充真实听感验证。 |

## 环境与检查

- 仅快速确认已安装 Rust/Cargo 1.92.0、现有 VS/Windows SDK 和已缓存依赖；使用现有 Windows SDK 的 rc.exe。未安装、升级、修复或重新注册 Rust、VS、SDK、依赖或 Office。
- `PowerPoint.Application` 的 COM LocalServer32 实际指向 `wpp.exe`，虽然自动化 Name 返回 Microsoft PowerPoint、旧检测字段 wpsDetected=false，本轮成功记录均归为 **WPS**。显式启动本机 POWERPNT.EXE 未获得可用原生窗口/独立 COM 会话，只清理自己启动的临时进程，没有修改注册。原生 Microsoft PowerPoint 不标通过。
- 最终统一检查全部通过：`cargo fmt --all -- --check`；`cargo clippy --locked --all-targets --all-features -- -D warnings`；`cargo test --locked` **72 passed / 0 failed / 3 ignored**；`cargo build --locked --release`。另外 `node --check` 和临时实际轮询函数验证通过。未把 ignored 测试冒充本轮运行。
- 桌面输入、截图、临时 A/B 文稿及测试配置均新建于本机 `target/home-regression-20260912/`。只操作可丢弃文稿；证据不进入 Git/ZIP，避免夹带 token、私人桌面或本机路径。测试配置和日志不作为运行默认值。
- 自动审批拒绝了启动带远程调试端口的 Edge，返回理由仅为 blocked by policy；没有绕过，改用普通浏览器窗口与原生键鼠完成可做的检查。

## 交付

仅生成一个本轮最终绿色包：`artifacts/review/FlyPPTTimer-v1.13.0-home-feedback-20260912-win-x64.zip`。包含最终 Release EXE、现有微软签名 x64 CRT、干净默认配置（无规则/空 token、页码默认开启）、LICENSE、BUILD.txt 与最小体验清单。源码提交 SHA 和 EXE 哈希见包内 BUILD.txt；包不含测试文稿、日志或测试 token。

剩余体验边界见 `RC_MANUAL_TEST.md`。完成 commit + push 后停止等待审核；未创建 Release/Tag，未合并默认分支。

---

# 2026-09-11 用户反馈整改结果（1.13.0）

本节为当前结果；后面的 RC-2 记录仅作历史证据。基点为 `5c1cfa9fdd3e3c9c5af0e3c69ac28639a02f625b`，已核对 User feedback fixes 工作流成功并接收 F02～F08 的直接修复。最终源码 SHA 见绿色包 `BUILD.txt`。未升级 Rust 1.92.0、依赖版本或 Cargo.lock；未发布 Release、Tag 或合并默认分支。

## 分项状态

| 项目 | 状态 | 本轮证据与边界 |
|---|---|---|
| F01 跨屏尺寸 | 已修且真机验证 | 150%/125% 双屏；Settings、PC Remote 各绕边 3 圈，再跨屏反向往返 10 次。Settings 返回主屏始终 1350×975 客户区；副屏 1125×813，关闭后同进程重开不变。Remote 875×638（125%）/1050×766（150%），无累计漂移。主动扩大至 1200×856 后跨屏和最大化还原保持；贴边吸附后重开 1200×857，仅首次逻辑取整 1 px。Timer 跨屏后为 150×53/125×44。 |
| F02 底栏留白 | 已修且真机验证 | 接收布局调整；原生检查发现滚动内容仍可能进入底栏，进一步限定 viewport 高度并增加裁剪矩形。底栏三按钮保持等高和固定外边距。 |
| F03 规则编辑卡 | 已修且真机验证 | 保留 54 DIP 编辑卡；用可丢弃 PPTX 的规则检查 Settings 和 PC Remote，窗口增高不拉伸编辑卡。 |
| F04 滚动区域 | 已修且真机验证 | 保留滚动条独立通道；给四个 ScrollView 添加明确裁剪边界，防止内容越过标题、编辑区和底栏。 |
| F05 作者说明 | 已修且真机验证 | 中文首段、段落和“祝大家使用愉快”末句完整；确定按钮可见。中英文布局及两种 DPI 的实际截图在临时证据目录。 |
| F06 蓝条/键盘 | 已修且真机验证 | 无顶部装饰蓝条；修正操作按钮组未遵守弹窗禁用状态。作者弹窗 Tab 后焦点仍在确定，Space/Enter 正常关闭，背景按钮禁用。 |
| F07 单一入口/热点 | 仅源码或自动验证；手机环境限制 | UI、QR、复制、打开采用实际网卡地址、当前端口和 token；网络/令牌变化每秒更新，Settings 仅刷新只读状态，不覆盖未应用端口。当前 Wi-Fi 192.168.36.125（网关 .1）与 Meta/StarVPN 并存，本机 LAN 正确 token/page 200，无/错 token 403。没有真实手机扫码验收；未修改防火墙。 |
| F08 原生音频/更新 | 已修且真机验证；听感/杀软归因环境限制 | WAV/MP3/WMA/M4A 中文空格路径首次、重复及直接原生 WMP 回退均完成；SAPI、静音切换及恢复通过。音频失败入日志，队列有界，回退启动及停止进展有超时。更新安装交接改为本 EXE 的原生等待分支，真实无害测试安装器在父进程退出 236ms 后启动，helper exit 0，无 ps1。未能取得用户所报杀软拦截记录，不能断言根因或保证所有杀软放行。 |
| F09 全屏到时 | 已修且真机验证；单屏/现场投影确认待补 | 复现旧遮罩仅 324×138；改为每屏独立 Slint fullscreen，连续三轮测得 2560×1600/2560×1440，所有四角纯黑。到时后保持；翻页命令、普通点击不解除；Remote reset 和本地 F4 清除两屏。退出前 2 层、退出后 0 层且进程正常退出。“仅提示/退出放映”模式用 F3 启动短计时，均无黑色遮罩（`non-black-modes.json`；本次未运行实际放映）。PowerPoint 联动补测在创建 COM 实例时返回 80080005，尚未打开临时文稿，故实际放映结束/下一页与遮罩联动仍留现场确认；不能用翻页 HTTP 命令代替实际放映证据。没有改变系统分辨率或安全组合键。 |
| B1 放映范围清理 | 已修且真机验证 | 完整快照后才写入；每个 put/Run 失败都尝试全部恢复；恢复失败不标 Saved=true，原本 dirty 不标 clean。4 个定向回归覆盖写入/恢复/Run 失败、阻塞 worker 和未知状态；PowerPoint、WPS 临时三页文稿 clean/dirty 各一轮，范围及 Saved 均正确。 |
| B2 COM 错误与退出 | 仅源码或自动验证 | 明确区分未知采样，未知不触发自动结束，后续确定结束正常生效；Drop 不等待仍卡在 COM 的线程。用受控阻塞 worker 证明退出不等待，不杀 Office，不保留完整旧状态缓存。真实 Office 拒绝响应的所有场景未穷举。 |

## F01 根因与实现

临时消息日志实际记录了嵌套 DPI：外层准备 144 DPI，内部又回到 120 DPI，外层随后仍按旧比例写入。旧校正会放大；单纯去掉校正也不足以覆盖 Winit 的嵌套尺寸计算。最终只在 DPI 消息范围内改写待应用的 `WINDOWPOS`，从最后一次用户主动调整后的逻辑客户区尺寸计算；不在 DPI 回调里调用 SetWindowPos，不从中间物理尺寸继续乘比例。WM_SIZING/WM_EXITSIZEMOVE 区分主动拉伸与移动，最大化和吸附不更新普通尺寸基准。保存 WINDOWPLACEMENT 时扣除窗口装饰，并先定位目标屏再恢复物理客户区尺寸。

未使用固定窗口大小、禁用缩放、循环延迟校正或递归 SetWindowPos。曾尝试的 Winit 事件接入和 GetWindowSubclass 均已移除；后者在本机 Comctl32 下缺少导出，已用兼容窗口属性替代，失败中间 EXE 不交付。

## 验证与交付

- 最终统一检查：`cargo fmt --all -- --check`、`cargo clippy --all-targets --locked -- -D warnings`、`cargo test --locked`、`cargo build --locked --release`。普通测试 66 passed / 0 failed / 3 ignored；另显式执行音频与可丢弃文稿两个真实 Windows ignored 测试均通过。旧 COM 连接 smoke 不擅自运行，避免影响现有文稿。
- 临时数据仅在 `target/feedback-temp/`：DPI 前后几何/消息日志、`settings-native-size.jsonl`、`remote-native-size.jsonl`、`remote-resize-reopen.jsonl`、`black-rounds.json`、`black-click-f4.json`、`black-exit.json`、`lan-http.json`、`audio-test.log`、`native-range-test.log`、`native-handoff-result.json` 及应用窗口截图。测试 token、规则、日志不放入 ZIP。
- 最终绿色包：`artifacts/feedback/FlyPPTTimer-v1.13.0-feedback-win-x64.zip`，包含当前编译 EXE、微软签名运行库、干净配置、LICENSE、BUILD.txt、运行库说明和一页测试说明。源码 SHA 由提交后打包时写入 BUILD。两个 CRT 均为 Microsoft 签名 Valid；最终 EXE SHA256 `DEF866A5D0CA15FC9992B04825D8C7F40F69E7984F997B7244DE3BEDF0008906`。副本真实启动并加载同目录 VCRUNTIME140.dll，完成中文 125%/150% UI 检查后正常退出（`final-loader.json`）。
- 剩余体验确认仅用 `RC_MANUAL_TEST.md`：手机热点扫码、真实听感/安全软件、自己的跨屏路径和单屏/投影放映效果。电脑侧回归不再整包交给用户重做。

---

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

# RC-1.1 多放映状态误判收口

日期：2026-09-11

Review 分支：`codex/v1-06-manual-test`

审核基点：`988358cef27c93b696fb65915fc7e86ecd7c0fe8`

## 本轮修改

- `read_application_state()` 先记录 `SlideShowWindows.Count > 0`，再解析目标窗口；多窗口目标不明确时返回 `slide_show_running=true`、清晰错误和空的当前文稿/页码，不再让状态层把放映误报为已停止。
- `with_show_view()` 继续在目标不明确时直接返回错误，因此 Previous/Next/Goto/黑屏/白屏/恢复/结束命令不会发送到任意窗口。
- 增加纯逻辑状态回归，覆盖 0 窗口、唯一窗口回退、乱序目标匹配、多窗口歧义保持运行事实；未修改 Timer、Remote、UI、DPI、配置、发布脚本或 Office ownership/退出逻辑。

## 验证

- `cargo fmt --all -- --check` 通过；`cargo clippy --locked --all-targets --all-features -- -D warnings` 通过；`cargo test --locked` 为 **55 passed、0 failed、1 ignored**（既有 Office COM 手测）。
- `cargo build --locked --release` 通过，程序版本仍为 `1.13.0`；按任务要求未重复 Inno Setup、未重新生成截图。
- RC-1.1 定向静态审阅未发现新的 P0/P1；真实 PowerPoint/WPS 多窗口、手机局域网、混合 DPI、声音/TTS 与安装升级仍由 [RC_MANUAL_TEST.md](RC_MANUAL_TEST.md) 手工完成。

## 待用户手测

本轮仅收口多放映状态误判，完成定向单元测试和 Release 构建。请按 [RC_MANUAL_TEST.md](RC_MANUAL_TEST.md) 在目标 Windows 机器上完成最终人工验收；不创建 Release/Tag。

---

# UX13 审核后稳定性修复与新电脑环境预检

日期：2026-09-10

Review 分支：`codex/v1-06-manual-test`

本轮起始 HEAD：`ec30b3179ac7154d34f95e4f7cd18a868d110fdf`（已 fetch 远端最新提交）。

## 新电脑环境预检

按 AGENTS、HANDOFF、V1_BASELINE_CHECKLIST、CODEX_TASK、CODEX_RESULT 顺序读取；并核对 v0.30.2 的 AppCommandService、RemoteControlForm、AppConfig 和默认配置，以及 v4 的单调计时技术经验。

- 原仓库 `J:\codex2\FlyPPTTimer_GUI` 位于 main，存在用户未提交的 RELEASE_NOTES 删除及测试目录。保留原状，在 `J:\codex2\FlyPPTTimer-v1-06-manual-test` 建立独立 worktree，跟踪目标 review 分支；新工作区开始时干净。
- Git 可用；origin 为 `https://github.com/Hona-Cao/FlyPPTTimer.git`，保留原 gitee remote。
- 原工具链：`rustc 1.86.0 (05f9846f8 2025-03-31)`、`cargo 1.86.0 (adf9b6ad1 2025-02-28)`；已有 x86_64-pc-windows-msvc target。
- 第一次 `cargo check --locked` 原文：`error: rustc 1.86.0 is not supported by the following packages:`，其中 `slint@1.17.1 requires rustc 1.92`，同版本 Slint backend/compiler/core/build 等也要求 1.92。
- 安装官方 `1.92.0-x86_64-pc-windows-msvc` 工具链及 rustfmt、clippy：`rustup toolchain install 1.92.0 --profile minimal --component rustfmt --component clippy`。最终为 `rustc 1.92.0 (ded5c06cf 2025-12-08)` / `cargo 1.92.0 (344c4567c 2025-10-21)`。通过 `cargo +1.92.0` 或进程级 `RUSTUP_TOOLCHAIN=1.92.0` 使用，不改变用户默认工具链，不升级项目依赖。
- Visual Studio Community 2022 17.13.6 已安装；MSVC 为 `C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.43.34808`。cl.exe 存在但不在普通 shell PATH。
- Windows SDK 已安装在 `F:\Windows Kits\10`，有 10.0.22000.0 / 10.0.22621.0；本轮设置进程级 `RC=F:\Windows Kits\10\bin\10.0.22621.0\x64\rc.exe`，资源编译器可执行。未重新安装 VS/SDK，未修改 build.rs 绕过环境问题。
- 修改源码前，`cargo +1.92.0 check --locked` 已通过（55.19s）。Cargo.toml / Cargo.lock 保持不变，程序版本仍为 1.13.0。
- PowerPoint：有，注册路径及 `C:\Program Files\Microsoft Office\Root\Office16\POWERPNT.EXE` 文件存在。WPS：有，wpp 注册入口指向 `I:\APP\wps person\WPS Office\12.1.0.28488\office6\wps.exe`，文件存在；实际放映能力本轮未验收。
- 双屏：检测到 DISPLAY1 / DISPLAY2，WinForms 报告各 2048×1152 逻辑区域；实际缩放比例和混合 DPI 拖动效果尚未验收。
- 手机 Remote 同局域网设备：暂不可确认，待用户用手机连接验证；不能把本机 HTTP 测试当作手机连接验收。
- Inno Setup：有，6.7.3，`C:\Users\mf203\AppData\Local\Programs\Inno Setup 6\ISCC.exe` 可执行。
- ffmpeg：有，`I:\APP\ffmpeg-master\bin\ffmpeg.exe`。
- Windows PowerShell 默认禁止脚本执行；发布脚本验证仅使用进程级执行策略参数，未修改系统执行策略。

## 根因与实际修改

1. **F3**：旧 startPause 把 Paused 路由到 start 并重置提醒。改为显式匹配四种状态：Running 暂停、Paused 恢复、Stopped/Finished 新开始；恢复保留累计 elapsed 和本轮提醒记录。Timer 模块未改。
2. **Settings**：旧 Apply 将完整旧 draft 保存并替换共享配置，dirty 也错误比较实时 applied。增加会话 baseline，以 baseline/draft 差异合并到最新 applied；对象按字段合并，列表按实际编辑的领域合并（包括 Rules，未编辑规则时全部采用外部最新值；若双方同时修改同一规则领域，则本次 Settings Apply 的领域修改优先）。双方快照先用原规则规范化，避免把未编辑字段的历史清理误认为本次修改。校验和落盘都使用合并结果，保存成功后才替换 applied，并同步 draft/baseline；Discard 仍只丢弃本窗口草稿。直接保存 token/重启服务的设置操作同步自己的 baseline，防止再次被当成未应用修改。未增加配置框架或 revision 系统。
3. **Remote 端口**：周期刷新不再写 next-port；创建窗口和应用端口回调才同步编辑框。刷新服务状态、更新地址/令牌等操作保留正在输入的文本。原服务启动、端口占用 fallback 逻辑未变。
4. **Remote 规则**：普通保存只写可编辑的 duration/mode，保留各规则 enabled；更新列表时确保当前行属于实际选中集合，空集合为 -1；Ctrl 取消后、批量保存后同步当前编辑器。周期刷新同一规则不覆写正在编辑的时长。删除/保存不再在无选择时退回第一行。非法批量时长保持弹窗并显示现有中英文错误文案，不修改共享配置或磁盘。
5. **发布版本**：build-release.ps1 用 cargo metadata 读取当前 Cargo.toml 对应的根 package version，统一 Portable 目录、zip、installer 参数和名称；安装器取消旧 1.6.0 fallback，要求从脚本传入 MyVersion。不改程序版本，不创建 Release/Tag。

## 验证

- 定向命令/回调测试通过：F3 真实命令入口，Pause 等待期间 elapsed 固定，Resume 继续原进度且不重复提醒，Finished 后可重新开始。
- 使用 Slint 自带 MinimalSoftwareWindow 调用生产窗口回调验证数据路径，不新增 GUI 自动化框架：A→Ctrl B→Ctrl 取消 B 回到 A；启用/禁用混合保存保持 enabled；batch 后普通保存保持 10 分钟与模式；非法 batch 时长有错误反馈且配置文件字节不变；无选择保存/删除不修改第一条规则。
- Settings 回调测试覆盖未应用外观修改 + 外部规则批量修改/新增规则/token/port/窗口尺寸落盘 + Apply，验证两边更改均保留；第二次 Apply 使用新 baseline；改回原值清除 dirty；无 dirty Cancel 不落盘。
- 补充同一配置对象内不同字段的合并测试：Settings 修改 Remote enabled、外观宽度，外部 port/token/Remote 窗口宽度/外观高度/提醒文字仍保留。
- Windows PowerShell 版本解析最小验证通过，输出 `Parsed package version: 1.13.0`。
- 端口回调测试覆盖半截输入经多次 refresh / 更新令牌仍保留，以及完整端口输入应用后服务启动、编辑框和持久配置与实际端口一致。
- 提交前统一检查：`cargo fmt --all -- --check` 通过；`cargo clippy --locked --all-targets --all-features -- -D warnings` 通过；`cargo test --locked` 为 **44 passed、0 failed、1 ignored**（既有 Office COM 手测测试）；`cargo build --locked --release` 通过。开发时测试代码曾出现整数/浮点类型不匹配，已修正后重新编译验证，未删除或跳过任何回归测试。
- 使用 Windows PowerShell 实际运行 build-release.ps1 成功（脚本内部 build 复用缓存，0.72s）；Inno Setup 生成 installer 成功，读取安装器版本资源为 ProductVersion `1.13.0` / FileVersion `1.13.0.0`。
- 便携目录：`J:\codex2\FlyPPTTimer-v1-06-manual-test\artifacts\release\v1.13.0\FlyPPTTimer-v1.13.0-portable-win-x64`，可直接运行其中 FlyPPTTimer.exe；同级 zip 为 7,655,303 字节，setup-win-x64.exe 为 7,380,002 字节，主程序为 17,668,096 字节。均为本地 review 产物，使用仓库默认配置，未执行安装器，未发布 GitHub Release/Tag。

## 待用户真实手测

本轮自动回调/数据测试不等于原生鼠标、键盘、视觉体验通过。请在实际 Windows 桌面复核 F3 快捷键注册与暂停恢复、Settings 未应用草稿与 PC Remote 来回切换、端口输入停顿数秒、Ctrl/Shift 选择、批量弹窗错误提示及保存、双屏混合 DPI 拖动、PowerPoint/WPS 放映、声音/TTS 与手机局域网连接。软件和双屏已检测到，并非缺失；这些项目未被本轮源码测试实际验收。既有 Office COM 手测测试继续保留。

完成本轮源码和文档提交、推送后停止编码，等待 ChatGPT 下一轮审核。

---

# 设置窗口显示问题定向修复（源码审核通过，待真机复核）

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`；审阅基点：`4825b97`。

## 本轮依据与范围

用户在 UX-04 手测包上报告设置窗口大小异常、文字缺失、跨屏异常，并授权继续排查、无人值守跟进审阅。本轮只处理该反馈；不进入 Timer / 大屏外观重做，不修改设置选项、默认值、Remote 协议或 Office 逻辑。拉取了最新 `4825b97`：确认上一包就绪，没有新增整改要求。

## 证据与修复

- 旧包实际窗口截图边界约 603×465 逻辑像素，内容区域约 600×433，与 900×650 物理像素在 150% 缩放下的结果相符；明显小于界面声明的 760×520 最小逻辑尺寸。
- 删除设置模型创建阶段强制 `PhysicalSize(900, 650)`；首次显示后根据 HWND 所在屏幕 DPI 把 900×650 逻辑尺寸转换为物理尺寸。重新打开仍保留已存在窗口的客户区尺寸。沿用现有延后显示流程，没有新增计时器或重试链。
- 设置窗口在原生显示及最终尺寸同步后请求框架重绘，与现有 Remote 路径一致。标签使用整行扣除上下留白后的绘制区域，保留原有自动换行和由首选文本高度撑开的行高，避免把绘制区域压到字体首选高度。
- 原 DPI 子类处理器用 `GetDpiForWindow` 当作旧 DPI，并在其等于消息的新 DPI 时直接拦截消息。Windows 此时报告的是当前 DPI；因此该比较无法判断上一次框架处理的 DPI。改为在已有 subclass reference data 中保存上一次处理值，收到变化先更新该值，并始终转发给 Winit，使框架收到缩放事件。保留原有客户区尺寸校正；最大化时不额外改客户区大小。这段处理器由设置和 PC Remote 共用，因此两者均在审核范围。
- 依据：[Microsoft WM_DPICHANGED 文档](https://learn.microsoft.com/en-us/windows/win32/hidpi/wm-dpichanged)；本地依赖 `winit-0.30.13/src/platform_impl/windows/event_loop.rs` 的 WM_DPICHANGED 分支本身已按缓存比例去重并向 Slint 发送 ScaleFactorChanged。

## 实际验证与未验收项

本轮使用 computer-use 操作了旧包设置窗口，观察到偏小布局；后续工具对重新启动的设置窗口只返回桌面背景，激活失败。重新获取窗口并重新启动独立调试实例后仍不能可靠操作，所以没有把背景截图当作软件白屏，也没有声称新版本已通过真实跨屏验证。调试配置只位于 target/debug，未覆盖用户原包配置。

修复过程执行了两次用于诊断运行的 debug build。进入新本地手测包阶段后，cargo fmt --check、cargo clippy --all-targets --all-features -- -D warnings、cargo test、cargo build --release 均通过；37 passed、0 failed、1 ignored（既有 Office 真机测试）。未新增镜像实现的单元测试或 GUI 测试矩阵。

仍需真实显示验收：设置首次打开/关闭重开是否保持合理尺寸，六页中英文标签是否完整，150% 与 125% 屏之间拖动是否稳定，最大化/还原是否正确；共享 DPI 处理器需要同时复核 PC Remote。文字缺失修复效果与跨屏稳定性尚未真机确认。此前 UX-04 键盘/焦点验收也仍保留。

本地手测包标识：`v1.13.0-SettingsFix01-20260909`；目录及同名 zip 位于 `E:/快传/计时器/tests/`，内部程序版本仍为 1.13.0，包内说明记录本轮源码提交。使用默认配置打包，不携带临时调试配置、用户配置或日志。这是待真机复核的定向修复包，不是正式发布。

审阅跟进频率按用户实际要求为每小时一次：无变化保持安静，有明确新任务则按仓库顺序读取并继续已授权整改，再更新结果、提交和推送；不创建 Release/Tag。

---
# UX-04 已有交互可用性结果

日期：2026-09-08

Review 分支：`codex/v1-06-manual-test`

任务基点：8d11f63。保留 UX-01～UX-03 已通过的配色和布局。

## 实际修改

本轮仅修改 `ui/app-window.slint`。

- `RemoteButton` 使用 Slint 1.17.1 的 FocusScope 与 forward-focus，接入框架 Tab 导航。参考本地依赖中 Fluent Button 的焦点实现，没有引入另一套组件库。
- 获得焦点时显示内侧 2px 焦点边框：蓝色主按钮使用白色，其余按钮使用蓝色。保留既有鼠标 hover/pressed/clicked。
- Enter/Space 按下时接受事件，松开时调用现有 clicked；不在按下重复事件中发送命令。FocusScope 的 enabled 绑定原按钮 enabled，禁用按钮不接受键盘激活。
- PC Remote 退出确认层创建时聚焦取消按钮；现有背景按钮、输入框、列表点击和页面切换均叠加 `!confirm-exit` 启用条件。保留遮罩、取消行为和 command(13, "") 确认退出语义。
- 设置批量层创建时聚焦已有时长输入框；背景导航、普通设置字段、规则选择/编辑/操作与底部三个按钮叠加 `!batch-open`。FieldRow 只增加一个 Slint 层 interactive 输入，将现有弹层布尔值传给标准控件；没有修改 Rust 设置模型。
- 关闭弹层后依原有布尔值重新启用背景，不增加焦点恢复对象、历史栈或恢复链。后续焦点移动由框架处理。

只读路径仍绑定 LineEdit.read-only，标准 LineEdit/ComboBox/CheckBox/Button 保留其键盘和输入行为；本轮未增加滚轮行为、复制按钮、Tooltip、Esc 全局语义或新确认步骤。

## 实际验证与限制

`cargo check`：通过，仅运行一次。没有修改 Rust，未运行全量测试、Clippy、Release build 或新增焦点/视觉快照测试。

本轮所需 FocusScope、forward-focus、焦点边框、键盘回调和弹层初始化聚焦均可由当前 Slint 编译。不建立自定义 Tab 顺序或底层事件系统；Tab 的真实顺序、关闭弹层后的焦点落点，以及按键按住/松开行为仍需用户验收。

## 仍需用户手测

1. Remote 通过 Tab/Shift+Tab 移动时可交互按钮焦点是否清晰，顺序是否基本符合视觉顺序。
2. 聚焦按钮后 Enter/Space 按下并松开是否只执行一次原命令；禁用按钮是否不接受鼠标/键盘操作。
3. 退出确认打开后焦点是否落在取消按钮，Tab 是否只到达弹层内操作，背景不能误操作；取消后继续 Tab 是否可用。
4. 批量设置打开后时长框是否聚焦，背景设置/规则/底部按钮不可操作；批量保存/取消保持原有行为。
5. 非弹层状态下只读路径仍可选取复制但不可编辑，禁用设置控件仍清晰可辨。
6. UX-01～UX-03 的配色、布局和窗口缩放效果没有回退。

编译通过不代表真实键盘、焦点和窗口交互已经验收。本轮未生成新手测包，未创建 Release/Tag。推送后停止等待审核，不进入 UX-05。

## UX-04 本地手测包交付

用户要求提供手测版，进入本地打包阶段后统一执行一次：`cargo fmt --check`、`cargo clippy --all-targets --all-features -- -D warnings`、`cargo test`、`cargo build --release`，均通过；测试为 37 passed、0 failed、1 ignored（既有 Office 真机测试）。

手测包：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX04-401f650-win-x64.zip`；同名目录内可直接运行 EXE。程序内部版本仍为 1.13.0，包标识为 v1.13.0-UX04-401f650，包含 UX-01～UX-04。附版本和手测说明。等待真实手测与审核，未创建 GitHub Release/Tag。

---

# 设置 / Remote / 大屏回归修复

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`

源码提交：`46eac11`

## 本轮触发

用户在上一版手测后报告：设置窗口拖拽跨屏时尺寸再次异常放大；时长设置的文件规则按钮过高并与边框重叠；规则单击默认出现多选；大屏缺少右上角关闭按钮且关闭后设置没有取消启用；Remote 仍暴露随机端口；说明文字占用过多页面长度；切换语言后弹窗/重启会卡死；打开设置或 Remote 后窗口可能只出现在任务栏。

## 实际修改

- 删除设置和 PC Remote 共用的自定义 DPI 尺寸校正 subclass，以及首帧后的二次尺寸恢复。窗口首次显示按当前 HWND 的 DPI 将 900×650 逻辑尺寸换算为物理尺寸，已有窗口继续使用实际客户区尺寸；跨屏时只保留 Winit 自身的 `WM_DPICHANGED` / Slint scale-factor 流程，避免两套尺寸调整互相叠加导致逐次放大。
- 设置窗口和 Remote 窗口在已经创建时使用 Win32 `ShowWindow` / `SetForegroundWindow` 恢复并置前；首次延后显示也在明确尺寸后置前，避免从托盘打开后只出现在任务栏。
- 时长设置的文件规则列表改为按规则数量紧凑计算高度，操作按钮固定为 34px 高并置于独立 48px 操作栏，避免按钮与规则框线重叠。
- 规则普通左键点击清空其他选择并选中当前规则；只有 Ctrl+左键才追加或取消多选，批量设置继续只作用于选中规则。
- 大屏窗口恢复标准标题栏，提供右上角关闭按钮。窗口关闭后通过桌面事件取消 `BigScreenEnabled`、保存配置、刷新设置页并重建显示窗口。
- Remote 设置页删除随机端口控件和相关展示。旧配置中的随机端口字段仍可读取，但启动时强制固定端口；保存端口被占用或无权限时直接绑定系统分配的可用端口，写回配置并通过托盘通知旧端口与新端口。
- 说明性内容（端口生效说明、局域网地址、防火墙说明、修复命令、二维码说明、语言提示、项目介绍等）改为单行按钮，点击后在设置窗口内弹出滚动说明，减少页面长度。
- 语言选择后立即显示“需要重启”弹窗；点击确定保存设置、启动带 `--restart-after` 的新实例并退出旧实例，重启后直接显示设置窗口。取消不会丢失草稿，后续应用仍会再次提示。

## 验证

- `cargo fmt --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：37 passed、0 failed、1 ignored（既有 Office COM 手测测试），新增固定端口占用切换与复用测试通过。
- 已用 Windows 原生窗口检查确认设置窗口为标准可调整窗口，规则列表与操作栏不再重叠；说明行可打开居中滚动弹窗；切换 English 后点击确定能够保存并自动重启到英文设置窗口。Windows 防火墙安全提示属于系统授权界面，未在测试中代替用户点击。

## 手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX05-SettingsRemote-20260909`，目录和 zip 位于 `E:/快传/计时器/tests/`。包内使用默认配置，不携带临时调试配置、用户配置或日志；这是 review 手测包，不是正式 Release/Tag。

## 待真实复核

需要用户在实际 150% / 125% 双屏环境复核设置和 PC Remote 拖拽跨屏后尺寸是否保持稳定，以及大屏关闭按钮、端口占用切换、批量选择和托盘置前行为。源码和自动测试通过不替代真实多屏交互验收。完成本轮提交和推送后停止，等待审核。

---
# 设置与 Remote 规则页第二轮手测反馈

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`

源码提交：`46eac11`

## 本轮触发

用户继续手测后确认：设置和 Remote 窗口在鼠标指针跨屏瞬间仍可能异常放大；文件规则需要明确的普通单选、Ctrl 追加/取消和 Shift 范围选择；选中文件后的详情应只保留时长和模式；说明弹窗出现文字拥挤；Remote 的“演示文稿”页只需要复刻设置页文件规则，并删除运行状态及放映控制按钮。

## 实际修改

- 设置和 PC Remote 继续共用窗口 DPI 处理，但改为记录上一次 DPI，先把 `WM_DPICHANGED` 转发给 Winit / Slint，再只在实际客户区尺寸与 DPI 比例不一致时校正客户区；最大化窗口不强行改尺寸。正常路径不会额外调整，跨屏发生重复缩放时只修正可测出的偏差。
- 文件规则行把 Ctrl 和 Shift 修饰键传入 Rust：普通左键清空其它选择并单选，Ctrl+左键追加或取消当前项，Shift+左键按上一次选中项到当前项建立范围；Ctrl+Shift 可在已有选择上追加范围。选中项继续用色块提示。
- 文件规则详情卡片压缩为时长和模式两项，移除文件名、路径和启用复选框的重复展示；启用状态仍保留在规则列表中。
- 说明弹窗增加隐藏测量文本，以实际换行后的首选高度计算弹窗高度和滚动区域，并扩大正文宽度、固定顶部对齐和留白，避免长说明挤成一团或被裁切。
- Remote 的“演示文稿”页数据源改为配置中的文件规则，移除“演示软件已运行”状态区以及从“打开演示文稿”到“退出演示软件”的放映控制按钮，只保留规则列表、时长/模式编辑、添加、删除、清空和保存。

## 验证

- `cargo fmt --all`：通过。
- `cargo check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：37 passed、0 failed、1 ignored（既有 Office COM 手测测试）。
- `cargo build --release`：通过，生成 `target/release/FlyPPTTimer.exe`。
- 本轮在当前桌面启动了 Release `--show-settings` 实例；Computer Use 窗口捕获随后返回“foreground window did not report a process id”，无法可靠取得该实例截图，因此未把本机单屏操作当作跨 DPI 真机验收。150% / 125% 双屏拖拽仍需用户实际设备复核。

## 本地手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX06-SettingsRules-20260909`。便携目录：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX06-SettingsRules-20260909-win-x64/`，直接运行其中的 `FlyPPTTimer.exe`，不要求解压或安装；包内使用默认配置，不携带临时调试配置、用户配置或日志。这是 review 手测版，不是正式 Release/Tag。

本轮完成后提交并推送 `codex/v1-06-manual-test`，停止编码等待审核；跨屏尺寸、Ctrl/Shift 选择和弹窗视觉仍以用户手测结果为准。

---

# 设置窗口显示与 Remote 文件规则修复

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`

源码提交：`7c039a8`

## 本轮触发

用户反馈：说明文字收进按钮后没有分段和首行缩进；设置窗口反复打开和关闭后偶尔只剩空白内容；不同页面的色块圆角和背景边界不统一；Remote 文件规则列表无法可靠选中文件并编辑参数。

## 实际修改

- 说明弹窗统一规范化段落：按空行分段、每段首行缩进，并按实际换行后的首选高度调整弹窗和滚动区域，长文本不会挤成一团。
- 设置窗口在已存在时强制恢复显示、置前并请求重绘；隐藏且没有未保存修改的旧设置实例会被安全重建，避免反复打开/关闭后复用失效的原生 surface。Remote 窗口也在显示前后执行同样的置前和重绘流程。
- 桌面管理窗口统一使用浅蓝灰画布、白色面板、统一边框和 8px/12px 圆角；设置侧栏、底部操作栏、字段行、规则卡片和 Remote 规则行都明确设置圆角和边界，避免纯白背景与相邻区域融合。
- Remote“演示文稿”规则行补齐整行点击区域和选中边框，点击后可编辑对应规则的时长与模式；列表继续复刻设置页的规则数据和选中状态。

## 验证

- `cargo fmt --all -- --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：37 passed、0 failed、1 ignored（既有 Office COM 手测测试）。
- `cargo build --release`：通过。
- `cargo run -- --capture-settings target/capture-settings-current2` 和 `cargo run -- --capture-windows target/capture-current2`：通过；设置页显示浅蓝灰画布、白色圆角侧栏/字段/底部栏，Remote 规则页显示圆角列表和选中边框，旧演示控制按钮未回归。

自动截图验证了排版和重绘路径，但当前 Computer Use 无法稳定取得 Release 原生窗口的进程句柄，因此反复开关、跨屏瞬间以及真实鼠标 Ctrl/Shift 选择仍需用户在目标设备上复核。

## 本地手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX07-SettingsDisplay-20260909`。便携目录：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX07-SettingsDisplay-20260909-win-x64/`，直接运行其中的 `FlyPPTTimer.exe`，不要求解压或安装；包内使用默认配置，不携带临时调试配置或用户日志。这是 review 手测版，不是正式 Release/Tag。完成推送后停止编码，等待审核。

---

# 设置密度、脏状态与 Remote 缩放修复

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`

源码提交：`cbc140d`

## 本轮触发

用户反馈：设置导航栏和底部固定操作栏不需要圆角；同层级的长按钮应改为多列；不可修改内容需要灰色显示；文件规则详情的时长和模式过于宽松；重复选择相同值却提示未应用修改；Remote 窗口调整大小会先跳到固定尺寸；演示文稿规则仍无法稳定选中并编辑时长、计时方式。

## 实际修改

- 设置左侧导航栏、导航项和底部固定操作栏改为直角，保留内容面板的圆角层级。
- Remote 操作、配置管理、文件位置和关于链接等同层级按钮改为两列或三列布局，长按钮不再独占整行。
- 禁用设置项的标签和只读值使用灰色层级；只读路径仍保留可选取能力，视觉上降低对比度。
- 文件规则详情把时长和计时方式放在同一行的两列中，减少空白高度。
- 脏状态改为比较草稿与已应用配置的实际序列化内容；普通设置、文件规则编辑、批量设置、恢复默认和文件操作改回原值时都会清除“有未应用的更改”。相同值的重复选择不会产生脏状态或语言重启提示。
- Remote 窗口默认回到 700×510 DIP，最小尺寸降为 560×460，并保存真实调整后的尺寸，不再强制 700×620；演示文稿列表使用整行指针释放事件选中规则，选中后可以编辑并保存时长和倒计时/正计时。

## 验证

- `cargo fmt --all -- --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：40 passed、0 failed、1 ignored（既有 Office COM 手测测试）。
- `cargo build --release`：通过。
- `cargo run -- --capture-settings target/capture-settings-ux08` 和 `cargo run -- --capture-windows target/capture-ux08`：通过；截图确认设置页操作按钮已按多列排列，导航和底栏为直角，Remote 规则页保留规则列表与编辑区。

真实无级拖拽、Remote 行点击及重复选择需要用户在 Windows 目标设备上复核；自动截图不替代多屏和鼠标交互验收。

## 本地手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX08-SettingsDensity-20260909`。便携目录：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX08-SettingsDensity-20260909-win-x64/`，直接运行其中的 `FlyPPTTimer.exe`，不要求解压或安装；这是 review 手测版，不是正式 Release/Tag。完成推送后停止编码，等待审核。

---

# Remote 规则多选、居中编辑与按当前屏幕打开

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`

源码提交：`f6ee291`

## 本轮触发

用户反馈：Remote“规则与放映”页仍不能按设置页习惯使用 Ctrl/Shift 选中文件；时长、模式和保存控件未在编辑卡中统一居中；从计时器右键打开管理窗口时可能出现在另一块屏幕；重复打开后窗口大小和位置记忆不稳定；设置底部操作栏的背景和边框会遮挡内容。

## 实际修改

- Remote 规则项增加 `selected` 状态和修饰键事件：普通点击单选，Ctrl 点击切换单项，Shift 从最近锚点选择范围，Ctrl+Shift 可在现有选择上追加范围；保存与删除操作对当前选中规则集合生效。
- Remote 编辑卡改成居中的单行布局，时长标签、输入框、模式标签、下拉框和保存按钮按统一间距水平排列。
- 新进程首次打开设置或 Remote 时根据当前鼠标所在显示器的工作区居中；本次运行内再次打开会保留手动调整的位置和窗口大小，Remote 的真实尺寸继续写入已有配置字段。
- 设置窗口复用和重建路径保留当前几何信息；底部固定区域改为透明、无边框，只保留“确定 / 取消 / 应用”三个按钮。

## 验证

- `cargo fmt --all -- --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：40 passed、0 failed、1 ignored（既有 Office COM 手测测试）。
- `cargo run -- --capture-settings target/capture-settings-ux09` 和 `cargo run -- --capture-windows target/capture-ux09`：通过；截图确认设置底栏为透明、Remote 规则页保留规则列表及三列底部操作布局。
- `cargo build --release`：通过，生成 `target/release/FlyPPTTimer.exe`。

自动截图不能覆盖真实鼠标修饰键、跨屏瞬间和拖拽后的原生窗口几何，双屏 Ctrl/Shift 选择和跨 DPI 拖拽仍需用户在目标设备手测确认。

## 本地手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX09-RemoteSelection-20260909`。便携目录：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX09-RemoteSelection-20260909-win-x64/`，直接运行其中的 `FlyPPTTimer.exe`，不要求解压或安装；这是 review 手测版，不是正式 Release/Tag。完成推送后停止编码，等待审核。

---

# 设置内容区、Remote 间距与文稿文件过滤修复

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`

源码提交：`62e1829`

## 本轮触发

用户反馈：设置页固定底栏虽然已透明，但滚动内容仍延伸到按钮下面；Remote 文件列表和时长/模式/保存编辑区需要留出明确间距；文件规则操作按钮过高、过宽且缺少间隔；添加相同文件应自动去重，文件类型只应允许幻灯片文件和 PDF。

## 实际修改

- 设置页滚动视口改为按窗口根高度计算，并预留固定底栏高度与下边距，内容不会再被“确定 / 取消 / 应用”按钮覆盖；底栏继续透明、无边框。
- 设置文件规则操作按钮固定为 34px 高，按文字长度使用紧凑宽度并保留 8px 间距；选中文件的时长/模式详情卡收紧为 64px 高。
- Remote 演示文稿规则列表底部与编辑卡之间增加 16px 间距，避免列表、编辑控件和底部操作区粘连。
- 添加文件对 `.ppt`、`.pptx`、`.pptm` 和 `.pdf` 建立统一白名单，移除“所有文件”过滤入口；设置页和 Remote 页都用规范化完整路径判断重复文件，重复选择不会再次添加。

## 验证

- `cargo fmt --all -- --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：41 passed、0 failed、1 ignored（既有 Office COM 手测测试；新增文件类型过滤测试通过）。
- `cargo build --release`：通过，生成 `target/release/FlyPPTTimer.exe`。
- Release `--capture-settings` / `--capture-windows`：通过；截图确认设置操作按钮已收紧并留有间距，设置底栏不再绘制背景或边框，Remote 页面列表布局保持稳定。

自动截图不能模拟真实文件拖放、重复选择和多屏鼠标操作；目标设备仍需手测确认格式过滤、重复文件和跨 DPI 窗口行为。

## 本地手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX10-LayoutFileFilter-20260909`。便携目录：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX10-LayoutFileFilter-20260909-win-x64/`，直接运行其中的 `FlyPPTTimer.exe`，不要求解压或安装；这是 review 手测版，不是正式 Release/Tag。完成推送后停止编码，等待审核。

---

# 设置与 Remote 布局隔离优化

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`

源码提交：`e962010`

## 本轮触发

用户反馈上一版仍存在底栏与设置内容视觉重叠，Remote 编辑区与文件列表的留白不足，设置文件规则操作区的控件仍显得过高、过宽，要求兼顾问题修复和整体观感。

## 实际修改

- 设置主内容面板启用裁剪，并把滚动视口明确限制在固定底栏上方，额外保留 6px 安全间距；滚动中的规则行不会再绘制到确定/取消/应用按钮区域。
- 设置页字段和文件规则区域统一使用 6px 外部间距；文件规则操作按钮固定为 32px 高、按文字长度设置 52/68px 宽度并使用 6px 间隔；选中规则详情卡压缩为 60px 高。
- Remote 文件列表和编辑卡之间保持 16px 空隙，列表、编辑和底部操作区形成独立层次。

## 验证

- `cargo fmt --all -- --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：41 passed、0 failed、1 ignored。
- `cargo build --release`：通过；Release 截图确认设置底栏与内容分区清晰、文件规则按钮紧凑且有间距，Remote 规则页保持独立卡片层次。

真实多屏拖拽、文件拖放和鼠标交互仍需用户在目标设备上手测；静态截图不代替真机验收。

## 本地手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX11-LayoutIsolation-20260909`。便携目录：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX11-LayoutIsolation-20260909-win-x64/`，直接运行其中的 `FlyPPTTimer.exe`，不要求解压或安装；这是 review 手测版，不是正式 Release/Tag。完成推送后停止编码，等待审核。

---

# 真实 UX10 便携版复现：设置窗口 DPI 递减与底栏隔离

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`

源码提交：`c1da90b`

## 本轮触发

用户指出此前只看开发期静态截图，没有先打开实际测试版本。本轮改为直接启动并操作 `E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX10-LayoutFileFilter-20260909-win-x64/FlyPPTTimer.exe`，使用该包内带有真实文件规则的配置复现问题。

## 实际复现与修改

- 通过实际右键菜单打开设置后，连续关闭/打开五次，确认窗口客户区从约 900×650 逐次缩小，最终只剩白色区域；根因是隐藏窗口的 HWND 客户区读取受到 DPI 虚拟化，并在下一次打开时再次当作物理尺寸设置。
- 设置几何记忆改用 Slint/winit 的物理窗口尺寸；创建 native surface 后等待一次 Winit DPI/resize 事件，再直接恢复保存的物理尺寸，移除旧的嵌套显示/隐藏流程。修复后在同一真实配置上连续三次关闭/打开，窗口外框稳定为约 915×687，没有递减或空白表面。
- 设置内容面板高度和滚动视口进一步预留固定底栏的安全区（内容面板减少 18px、滚动视口减少 20px），底栏仍无背景和边框，文件规则最后一行与“确定 / 取消 / 应用”之间保留可见间隙。
- 删除不再使用的 HWND 客户区尺寸读取函数，避免后续代码重新引入 DPI 虚拟化尺寸。

## 验证

- `cargo fmt -- --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：41 passed、0 failed、1 ignored（既有 Office COM 手测测试）。
- `cargo build --release`：通过。
- 真实 UX12 便携包使用 UX10 的文件规则配置启动；设置窗口实际打开后截图确认内容、滚动区和底栏分离，连续三次关闭/打开尺寸保持稳定。

## 本地手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX12-SettingsDpiLayout-20260909`。便携目录：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX12-SettingsDpiLayout-20260909-win-x64/`，直接运行其中的 `FlyPPTTimer.exe`，不要求解压或安装；包内保留了 UX10 的文件规则配置，便于复现本轮真实问题。这是 review 手测版，不是正式 Release/Tag。完成推送后停止编码，等待审核。

# 设置说明弹窗与 Remote 规则页 UX13

日期：2026-09-10

Review 分支：`codex/v1-06-manual-test`

## 本轮触发

用户要求基于上一版真实便携包继续检查说明文字弹窗、设置页红框区域和 Remote“规则与放映”列表，并增加 Remote 批量设置。用户同时要求验证截图覆盖完整桌面和任务栏。

## 真实复现

直接启动上一版 `FlyPPTTimer-v1.13.0-UX12-SettingsDpiLayout-20260909-win-x64`，通过 Win32 实际打开设置和 Remote 窗口，并使用 `ffmpeg gdigrab -i desktop` 捕获 5120×1600 完整虚拟桌面。设置页选中规则后，规则列表、添加/删除/清空/批量设置、时长/模式编辑和底部确定/取消/应用挤在同一段纵向空间；Remote 规则列表缺少列层级，文件路径、状态和编辑区之间的关系不清楚。

## 实际修改

- 设置页将规则列表、操作工具栏、选中规则编辑卡和固定底部操作栏分成独立层级；工具栏使用统一浅色面板和间距，编辑卡增加高度，时长和模式改为居中的固定窄列，滚动视口和底栏保留安全间隔。
- 说明弹窗增加主题色顶边、统一边框、正文卡片、滚动内边距和自适应高度；正文使用更清晰的字号与次级文字色，保留现有说明内容和重启确认语义。
- Remote“规则与放映”列表增加列标题、文件名/路径二级排版、时长/模式右侧列和状态行；列表行、编辑卡和底部操作区有明确留白。
- Remote 规则页保留普通、Ctrl 和 Shift 选择，并新增“批量设置”按钮及弹窗，对所选规则统一写入时长和计时方式；校验时长后保存配置并刷新选中状态。
- 便携包沿用上一版真实规则配置，便于复现用户反馈；未修改 v0.30.2 的 Timer、Remote HTTP 协议或 Office 控制行为。

## 验证

- `cargo fmt --all -- --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：41 passed、0 failed、1 ignored。
- `cargo build --release`：通过。
- Release `--capture-windows target/capture-ux13`：通过，Remote 规则页静态渲染通过。
- 真实便携版全屏截图（5120×1600，包含两侧任务栏）确认设置编辑区分层、说明弹窗排版和 Remote 规则列表布局：`target/ux13-settings-large-selected-full.png`、`target/ux13-settings-front2-full.png`、`target/ux13-remote-presentation-full.png`。

批量弹窗的鼠标点击最终一次落到另一块屏幕后台窗口，未把该次误点击作为通过证据；代码编译、控件可见性和静态渲染已通过，实际 Ctrl/Shift 选择及批量保存仍请用户在手测包中确认。

## 本地手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX13-DialogsRemoteBatch-20260910`。便携目录：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX13-DialogsRemoteBatch-20260910-win-x64/`，直接运行其中的 `FlyPPTTimer.exe`，不需要压缩包、安装或解压。这是 review 手测版，不是正式 Release/Tag。

完成提交并推送后停止编码，等待审核。
