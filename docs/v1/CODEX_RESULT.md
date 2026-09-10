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
