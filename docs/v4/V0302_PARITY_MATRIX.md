# FlyPPTTimer v0.30.2 → 4.0 功能等价矩阵

更新日期：2026-08-06

稳定基线：`v0.30.2` / `8921390ac99d574f99be46b7e08d36a191b3e483`

4.0 审计起点：`f051e90d217ca9c0b1f7a7d602128ed49a407b21`

## 判定规则

- **已完成**：目标实现、行为测试、关键 UI Automation 和文档均完成。
- **兼容保留**：0.30.2 行为仍由经典 WinForms 承担，迁移完成前不得删除。
- **进行中**：已有 Core、服务边界或 WPF 部分实现，正式 WPF 尚未覆盖完整行为。
- **未开始**：尚无正式 WPF 实现。源码字符串测试只算审计证据，不能冒充行为迁移。
- 每行自动化栏是最低验证要求；标为兼容保留不代表已完成 WPF 迁移。

## A. 应用基础与可靠性

| 子模块/行为 | v0.30.2 源码或测试依据 | 配置影响 | 4.0 目标实现 | 自动化 + 人工验收 | 状态 |
|---|---|---|---|---|---|
| 单实例；第二次启动不重复创建托盘/计时/HTTP | `Program.cs`; `VersionAndPresentationContractTests.cs` | — | WPF AppHost 单实例协调器 | 双进程启动、唤醒、仅一托盘 | 兼容保留 |
| 启动、托盘驻留、受控退出与资源释放 | `FlyPPTTimerContext.cs`; TEST_REPORT 0.18.9 | Controls 关闭行为 | WPF Host 生命周期 | UIA 启动/托盘/退出、无残留进程 | 进行中 |
| 配置默认、校验、归一化 | `AppConfig.cs`; `ConfigService.cs`; config tests | 全 Schema | 配置仓储/验证器 | 空配置与边界 fixture | 进行中 |
| 原子保存、时间戳备份、损坏恢复 | `ConfigService.cs`; package/config tests | config 与 backups | 原子仓储 | 中断写入/损坏/恢复 | 兼容保留 |
| 0.30.2 迁移且未知字段 round-trip | `Fixtures/v0.30.2-config.json`; config tests | 规则、token、声音、位置 | typed + JsonExtensionData 迁移 | 逐层未知字段往返；升级/回退文档 | 已完成 |
| 日志、轮转、异常记录、token 脱敏 | `LogService.cs`; `RemoteUrlPrivacy.cs` tests | `logs/` | Windows 日志 adapter | 轮转、URL/token 扫描 | 兼容保留 |
| 中文/英文/系统语言及切换重启 | `Localization.cs`; `V0203LocalizationTests.cs` | Language/install-language | WPF 资源服务 | zh/en UIA、重启后配置保留 | 兼容保留 |
| 安装/便携路径语义 | `AppPaths.cs`; package/installer scripts | config/logs/sounds | deployment path policy | 安装升级与 ZIP 实测 | 兼容保留 |
| Windows 10/11 x64、PerMonitorV2 | csproj/Program；布局测试 | Placement | WPF DPI/display adapter | 100–200% 混合 DPI | 兼容保留 |

## B. 计时核心

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 倒计时与正计时 | `TimerEngine`; `TimerServiceTests`; `WpfTimerDisplayTests` | Timer.Mode | Core `TimerEngine` + WPF 投影 | Core 方向/格式行为 | 已完成 |
| 开始、暂停、继续、停止、重置 | Timer/AppCommand tests；发布版 F3 UIA | — | Timer use cases + WPF 窗口 | 状态转换 + WPF UIA | 已完成 |
| 立即重新计时 | `AppCommandService.Restart`; web | Rules/DefaultDuration | Restart use case | 运行/暂停/规则三场景 | 兼容保留 |
| 默认时长和 3/5/8/10/15 预设 | config defaults; command tests | DefaultDuration/Hotkeys | typed preset commands | 本地/热键/远程一致 | 兼容保留 |
| 当前文稿规则优先，无规则用全局 | lifecycle/rule tests | Rules | RuleResolver | 路径/禁用/无匹配表驱动 | 兼容保留 |
| 到零停止或继续显示超时 | Timer/Alert/Display tests | ContinueOvertime | Core policy + WPF formatter | 跨零点与显示 UIA | 已完成 |
| 单调时钟抵抗系统时间变化/后台运行 | timer tests; TEST_REPORT | — | `IMonotonicClock` | 假时钟跳变/暂停 | 已完成 |
| 桌面、远程、普通窗、大屏状态一致 | `GetRemoteState`; WPF timer/big-screen | 运行态 | 单一快照/Revision | 多消费者投影 | 已完成 |

## C. 提醒、声音和到时动作

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 两次提前提醒：启用、时间、文字、效果 | `AlertService.cs`; WPF PromptDraft; V0.18 tests | Prompt1/2 | Alert scheduler + WPF 页 | 假时钟边界/跨页真实控件 UIA | 已完成 |
| 结束提醒 | 同上 | EndPrompt | Alert scheduler + WPF 页 | 每轮只触发一次/保存行为 | 已完成 |
| 文本/背景/边框闪烁与节奏 | WPF PromptDraft；Overlay/Appearance tests | flash 字段 | WPF 配置 + 现有显示 | 参数边界/发布版 UIA | 已完成 |
| 后台语音队列不阻塞 UI | `WindowsAlertPlaybackEngine`; WPF Speak | Speak/Text | speech adapter | 队列、取消、退出 | 已完成 |
| 自定义声音导入本地副本并播放 | WPF picker; `AlertSoundStorage`; tests | PlaySound/SoundFile | 异步 sound store/player | 覆盖/格式/路径持久化 | 已完成 |
| 静音/取消静音和电脑音频控制 | `SystemAudioService.cs`; commands | 运行态 | audio adapter | 两次切换恢复/远程同步 | 兼容保留 |
| 全屏“时间到”画面 | `WpfTimeUpOverlayWindow.cs` | EndAction | WPF overlay | 多屏关闭与释放 | 已完成 |
| 到时无动作/黑屏/结束放映 | TimerEndAction; context | EndAction | EndAction coordinator | adapter 替身 + 真机 | 兼容保留 |
| 提醒不重复、不漏发、重置后无残留 | `ResetTriggers`; 历史回归 | 运行态 | alert state machine | 跨阈值/暂停/重置 | 已完成 |

## D. 普通计时窗口

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 显示/隐藏且后台继续计时 | `WpfTimerOverlayWindow`; F3/F5 发布 smoke | Placement.Visible | WPF TimerWindow | UIA 隐藏/显示/后台 | 已完成 |
| 字体、字号、颜色、背景、超时色、透明度、尺寸 | WPF overlay/formatter；Appearance tests | Appearance.* | WPF style | 预设和视觉基线 | 已完成 |
| 置顶、无边框、形状、点击穿透、锁定 | WPF overlay/native | Appearance/Controls | Window/native adapter | 点击/焦点/置顶实测 | 已完成 |
| 拖动、右键菜单、位置重置 | WPF overlay；STA 真 MenuItem test | Placement | WPF behavior/commands | 真控件 + 人工拖动 | 已完成 |
| 单屏或所有屏幕显示 | context; placement tests | ShowOnAllScreens/Target | WPF window per display | 虚拟/真实双屏 | 已完成 |
| 九宫格锚点与 X/Y 百分比微调 | `OverlayPlacementService`; tests | Anchor/Offsets | placement calculator | 负坐标/144-DPI 表驱动 | 已完成 |
| 投影/DPI/断连后重建并保持状态 | display event + WPF rebuild | Placement | topology observer | 重建回归；热插拔人工 | 已完成 |

## E. 大屏计时

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 只列非主扩展屏；无扩展屏禁用 | context + `WpfBigScreenTimerWindow`; tests | BigScreen fields | WPF BigScreenWindow | 单屏拒绝；双屏人工计划 | 已完成 |
| 标准标题栏；移动、缩放、最小/最大/关闭 | WPF big-screen；STA 真 Window test | 运行态 | WPF 标准 Window | 真控件/系统按钮人工 | 已完成 |
| 与主计时实时同步 | context/snapshot/formatter | — | 共享状态投影 | 快照 + WPF 控件 | 已完成 |
| 设备变化安全关闭，释放后设置仍可打开 | WPF rebuild + 设置退出 smoke | DeviceName | display observer | 生命周期回归；热插拔人工 | 已完成 |

## F. 文稿规则

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 添加、编辑、删除、清空 | WPF Rules DataGrid/commands | Rules[] | WPF Rules page | 真实 DataGrid + 持久化 | 已完成 |
| 完整路径身份、文件名显示/匹配 | `PresentationRuleValidator`; FileRuleDraft | FileName/FilePath | 路径身份/去重 | 规范化/大小写测试 | 已完成 |
| 独立时长、模式、启用与批量修改 | WPF batch command/tests | rule fields | WPF batch commands | 选中/未选中行为 | 已完成 |
| 当前文稿匹配规则 | lifecycle controller | Rules | RuleResolver | adapter/path matrix | 兼容保留 |
| 改全局时长时确认是否同步规则 | WPF confirmation callback/tests | DefaultDuration/Rules | confirmation service | 同步/不同步两路径 | 已完成 |
| 不存在、重名、路径变化、重复规则 | validator + WPF validation/tests | Rules | validation collection | 重复/非法时长 | 已完成 |
| 保存、语言切换、升级均保留规则 | fixture/config/WPF tests | Rules | migration pipeline | 0.30.2 round-trip | 已完成 |

## G. PowerPoint、WPS 与全屏联动

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 检测进程、文稿、放映、页码、能力 | detector/monitor tests；PowerPoint/WPS 临时文稿真机 | — | interface/adapter/detector/monitor | 替身 + Office/WPS 真机 | 已完成 |
| 进入/离开全屏自动开始、停止、重置 | lifecycle 状态序列与重复/换稿测试 | Behavior 自动字段 | coordinator | 状态序列 | 已完成 |
| 非 Office 全屏白名单 | FullscreenDetector/defaults/tests | whitelist | fullscreen adapter | 浏览器/PDF/非白名单 | 已完成 |
| 打开文稿；受管 PowerPoint 只读 | ownership tests；PowerPoint/WPS 真机只读 | rule path | presentation adapter | COM 替身/真机只读 | 已完成 |
| 从头/当前页放映，恢复临时 SlideShowSettings | service tests；PowerPoint/WPS 真机 | — | adapter + STA | Saved/设置恢复 | 已完成 |
| 上一页/下一页/跳页 | typed command 表驱动；PowerPoint/WPS 真机 | SlideNumber | typed command | 边界页码/真机 | 已完成 |
| 黑屏、白屏、恢复 | typed command 表驱动；PowerPoint/WPS 真机 | — | typed command | 状态/视觉真机 | 已完成 |
| 结束放映；20×100ms 找窗并激活最大化 | activator/native-candidate tests；PowerPoint/WPS 真机 | — | adapter + activator | Win32 替身/真机 | 已完成 |
| 关闭当前、最后打开、外部/受管文稿顺序 | ownership/lifecycle tests；临时文稿真机 | managed runtime list | ownership registry | 多文稿/未保存 | 已完成 |
| 明确确认后强制退出 PowerPoint/WPS | terminator/command tests；不终止用户进程 | Confirmed | terminator | 假进程/明确确认 | 已完成 |
| 保留用户未保存状态并完整释放 COM | managed/external close tests；共享 RCW 回归；真机 | — | disposable COM scope | 异常注入/释放 | 已完成 |
| WPS 只启用真实能力并保留中文提示 | detector/control tests；本机 WPS 检测与兼容 COM | — | capability model | 假进程 + WPS 真机 | 已完成 |

## H. 远程服务与电脑端

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| HTTP 启停/重启且不阻塞 UI | `RemoteControlService.cs`; real-listener HTTP tests | Enabled | `RemoteControlService` + dashboard facade | 随机端口/连续请求/取消/退出 | 已完成 |
| 默认 4080 与随机端口 | config/network/dashboard tests/README | Port/UseRandomPort | endpoint policy | 校验、随机监听与重启 | 已完成 |
| IPv4 选择、URL、二维码、复制、防火墙说明 | network/WPF dashboard/UIA | Window | `WpfRemoteControlWindow` | 多网卡/无网络/真实控件 | 已完成 |
| 强 token 鉴权与一键断开所有设备 | token/security/real HTTP tests | Token | auth/session service | 旧 token 403、新 token 200 | 已完成 |
| 启动失败、网卡变化和防火墙提示 | service/WPF dashboard | — | health state/notices | 状态快照、刷新与人工网络步骤 | 已完成 |
| “远程连接/演示文稿”两模块 | WPF dashboard/STA/UIA | Window | formal WPF dashboard | 响应式真实控件 UIA | 已完成 |
| 文稿列表/详情/规则编辑/危险操作确认 | dashboard/validator/command tests | Rules | WPF dashboard | CRUD/确认/能力禁用/长文件名 | 已完成 |
| 窄宽布局、DPI、位置恢复 | layout service/WPF UIA | Remote Window | adaptive WPF layout | 780px、断点、持久化 | 已完成 |

## I. 手机/浏览器

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 零安装；zh/en 跟随浏览器 | Web assets/tests/Chromium smoke | — | 嵌入 Web app/i18n | 390×844 zh/en 真浏览器 | 已完成 |
| token 缺失/错误/轮换失效 | security/real HTTP tests | Token | auth middleware | HTTP 403/轮换失效 | 已完成 |
| 计时状态、时长、模式和全套控制 | app.js/AppCommand/real HTTP | RemoteCommand | stable API | API + Chromium POST E2E | 已完成 |
| 窗口、闪烁、提示、静音 | command/asset/browser tests | — | command bus | API/控件可用性 | 已完成 |
| 文稿列表、打开、放映、导航、黑白屏、结束/关闭 | web/typed presentation control | presentation DTO | presentation use cases | adapter HTTP + 阶段3真机 | 已完成 |
| “计时/演示”标签与能力反馈 | web assets/Chromium smoke | — | Web state | 真浏览器切页与状态渲染 | 已完成 |
| 横向方向锁、纵向滚动保护、吸附和反向抢占 | app.js; 0.19/0.20 + Chromium | — | 手势状态机 | 40ms 连续反向 touch E2E | 已完成 |
| 移动布局、可访问性、常用浏览器 | semantic HTML/CSS/Chromium | — | semantic Web UI | 390px 无溢出；Edge/Safari 阶段6真机 | 已完成 |

## J. 命令、托盘和快捷键

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 默认 F3/F4/F5 | WPF VM/config/hotkey tests | legacy + Hotkeys | command registry | 保存/注册/触发实测 | 已完成 |
| 全部计时、窗口、闪烁、静音、模式、预设热键 | WPF HotkeyDraft/commands | Hotkeys | typed commands | 20 项映射 + UIA | 已完成 |
| 托盘、菜单、热键、远程共享命令 | `AppCommandService.cs` | — | application bus | 各入口同一用例 | 兼容保留 |
| 托盘远程/设置/位置/静音/退出 | context | — | WPF NotifyIcon host | UIA/人工 | 兼容保留 |
| 热键冲突、重注册、保存后即时应用 | WPF validation; HotkeyService | Hotkeys | adapter/validation | 重复拒绝 + 主程序重载 | 已完成 |

## K. WPF 设置

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 时长、模式、超时、到时动作 | WPF Settings VM/XAML | Timer | WPF settings | 发布版文本/下拉/复选 UIA | 已完成 |
| 自动行为 | WPF Settings VM/XAML | Behavior basics | WPF settings | 保存后主程序即时应用 | 已完成 |
| 外观、通用与更新字段 | WPF Display/Other 页 | Appearance/Controls/Update | WPF settings | 数字 UIA/持久化 | 已完成 |
| 完整规则与批量操作 | WPF Rules DataGrid/commands | Rules | WPF Rules page | CRUD/批量/同步选择 | 已完成 |
| 三组提醒、语音、声音、闪烁细节 | WPF PromptDraft/file picker | Prompts | WPF Alerts page | 行为测试 + 发布版跨页 UIA | 已完成 |
| 完整外观、多屏定位、大屏 | WPF Display 页/Screen 组合 | Appearance/Placement | WPF Display pages | 保存/拓扑/真实控件 | 已完成 |
| 全部快捷键与冲突 | WPF Hotkeys page | Controls.Hotkeys | WPF Hotkeys page | 20 项/重复拒绝/UIA | 已完成 |
| 完整远程设置 | WPF Remote/Other 页 | RemoteControl | WPF Remote settings | 保存/token 轮换/UIA | 已完成 |
| 未保存、校验、保存/放弃/取消、连续编辑不卡死 | 当前 WPF tests/smokes | all edited | MVVM + async boundary | 发布版四类控件 <3s；退出回归 | 已完成 |
| 经典设置回退（阶段 5 已移除，WPF 单入口） | context | 同一 config | 阶段 5 移除 | WPF 单入口 | 已完成 |

## L. 更新、安装与发布

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 启动检查默认关闭；托盘手动检查 | `GiteeUpdateService.cs`; README | CheckOnStartup | update use case | HTTP fixture/版本比较 | 兼容保留 |
| Gitee 查询、资产选择、错误处理 | update service/tests | — | provider | API fixture/错误注入 | 兼容保留 |
| 安装版下载与可选 SHA-256 | service/0.20.2 报告 | temp | installer use case | 本地 HTTP/错误哈希 | 兼容保留 |
| 便携版不覆盖自身而打开下载页 | service | — | deployment policy | portable 实测 | 兼容保留 |
| Inno x64 升级前备份并保留配置 | package/installer/tests | config/backups | 双 EXE installer | 静默升级/回退 | 兼容保留 |
| 自包含单文件 win-x64、ZIP/Setup/SHA | scripts/workflow | — | WPF 唯一入口发布链 | clean VM/hash | 进行中 |
| Actions 双 EXE/UI smoke/Artifact | windows-ci.yml | — | 每阶段 CI | 运行成功、Artifact 可下载 | 进行中 |

## M. 文档

| 子模块 | 依据 | 4.0 目标 | 验收 | 状态 |
|---|---|---|---|---|
| 中英文 README | baseline README | 完整 WPF 使用/安全/升级 | 链接/命令/截图 | 兼容保留 |
| CHANGELOG/TEST_REPORT | 历史文档 | 每阶段证据 | TRX/CI/hash 对照 | 进行中 |
| 矩阵/架构/配置迁移 | 本文、V4_ARCHITECTURE、V4_CONFIGURATION_MIGRATION | 完整工程文档 | fixture/设计走查 | 已完成 |
| 中英文用户指南 | baseline docs | 阶段 6 指南 | 双语人工走查 | 未开始 |
| 人工测试计划 | TEST_REPORT 历史 | 可执行环境/步骤/预期/证据 | 全矩阵签收 | 未开始 |

## 阶段 2 结论

矩阵共 **99** 个行为单元。阶段 2 后达到“已完成”的是 **42** 项；40 项继续由兼容实现承担，15 项正在重构，2 项文档验收尚未开始。这个比例衡量正式迁移和验收完成度，不是现有可用功能比例。

阶段 2 本地实测：三个 Release Build 均 0 warnings/0 errors、桌面 279/279、Core 4/4；双 EXE 发布、六页 WPF 设置 UI Automation、主程序退出重载及正式 WPF 计时窗回归通过。WPF 正式配置入口不再需要经典设置补齐字段；经典窗口继续保留为阶段 5 前的兼容入口。
