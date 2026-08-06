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
| 0.30.2 迁移且未知字段 round-trip | config/installer tests; 0.20.x 报告 | 规则、token、声音、位置 | DOM + typed 版本迁移 | 真实旧配置 fixture、升级/回退 | 进行中 |
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
| 两次提前提醒：启用、时间、文字、效果 | `AlertService.cs`; SettingsForm; V0.18 tests | Prompt1/2 | Alert scheduler + WPF 页 | 假时钟边界/UIA | 兼容保留 |
| 结束提醒 | 同上 | EndPrompt | Alert scheduler | 每轮只触发一次 | 兼容保留 |
| 文本/背景/边框闪烁与节奏 | Overlay/Appearance; TEST_REPORT | flash 字段 | WPF animation | 三样式视觉/UIA | 兼容保留 |
| 后台语音队列不阻塞 UI | `WindowsAlertPlaybackEngine` | Speak/Text | speech adapter | 队列、取消、退出 | 兼容保留 |
| 自定义声音导入本地副本并播放 | `AlertSoundStorage.cs`; AlertService | PlaySound/SoundFile | sound store/player | 覆盖/缺失/格式实测 | 兼容保留 |
| 静音/取消静音和电脑音频控制 | `SystemAudioService.cs`; commands | 运行态 | audio adapter | 两次切换恢复/远程同步 | 兼容保留 |
| 全屏“时间到”画面 | `TimeUpBlackoutForm.cs` | EndAction | WPF overlay | 多屏关闭与释放 | 兼容保留 |
| 到时无动作/黑屏/结束放映 | TimerEndAction; context | EndAction | EndAction coordinator | adapter 替身 + 真机 | 兼容保留 |
| 提醒不重复、不漏发、重置后无残留 | `ResetTriggers`; 历史回归 | 运行态 | alert state machine | 跨阈值/暂停/重置 | 兼容保留 |

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
| 添加、编辑、删除、清空 | Settings/RuleRow | Rules[] | WPF Rules page | UIA CRUD/持久化 | 兼容保留 |
| 完整路径身份、文件名显示/匹配 | `PresentationRuleValidator.cs` | FileName/FilePath | Rule value object | 规范化/大小写测试 | 兼容保留 |
| 独立时长、模式、启用与批量修改 | BatchRule dialog | rule fields | WPF batch commands | 多选 UIA | 兼容保留 |
| 当前文稿匹配规则 | lifecycle controller | Rules | RuleResolver | adapter/path matrix | 兼容保留 |
| 改全局时长时确认是否同步规则 | SettingsForm; TEST_REPORT | DefaultDuration/Rules | confirmation service | 同步/不同步/取消 UIA | 兼容保留 |
| 不存在、重名、路径变化、重复规则 | validator/tests | Rules | validation collection | 文件系统替身 | 兼容保留 |
| 保存、语言切换、升级均保留规则 | config/localization/installer | Rules | migration pipeline | 0.30.2 round-trip | 兼容保留 |

## G. PowerPoint、WPS 与全屏联动

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 检测进程、文稿、放映、页码、能力 | `PowerPointControlService.cs`; presentation tests | — | interface/adapter/detector/monitor | 替身 + Office/WPS 真机 | 进行中 |
| 进入/离开全屏自动开始、停止、重置 | Fullscreen/Lifecycle | Behavior 自动字段 | coordinator | 状态序列 | 兼容保留 |
| 非 Office 全屏白名单 | FullscreenDetector/defaults | whitelist | fullscreen adapter | 浏览器/PDF/非白名单 | 兼容保留 |
| 打开文稿；受管 PowerPoint 只读 | control service; TEST_REPORT | rule path | presentation adapter | COM 替身/真机只读 | 进行中 |
| 从头/当前页放映，恢复临时 SlideShowSettings | service/control tests; 0.19.3 | — | adapter + STA | Saved/设置恢复 | 进行中 |
| 上一页/下一页/跳页 | Execute/remote tests | SlideNumber | typed command | 边界页码/真机 | 进行中 |
| 黑屏、白屏、恢复 | Execute | — | typed command | 状态/视觉真机 | 进行中 |
| 结束放映；20×100ms 找窗并激活最大化 | service; activator tests | — | adapter + activator | Win32 替身/真机 | 进行中 |
| 关闭当前、最后打开、外部/受管文稿顺序 | lifecycle/control tests | managed runtime list | ownership registry | 多文稿/未保存 | 进行中 |
| 明确确认后强制退出 PowerPoint/WPS | terminator tests | Confirmed | terminator | 假进程/真机确认 | 进行中 |
| 保留用户未保存状态并完整释放 COM | 0.19.3 报告/控制测试 | — | disposable COM scope | 异常注入/释放 | 进行中 |
| WPS 只启用真实能力并保留中文提示 | detector/control tests | — | capability model | 假进程 + WPS 真机 | 进行中 |

## H. 远程服务与电脑端

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| HTTP 启停/重启且不阻塞 UI | `RemoteControlService.cs`; HTTP tests | Enabled | Remote host | 端口占用/取消/退出 | 兼容保留 |
| 默认 4080 与随机端口 | config/network tests/README | Port/UseRandomPort | endpoint policy | 冲突与重启 | 兼容保留 |
| IPv4 选择、URL、二维码、复制、防火墙说明 | network/form | Window | WPF remote window | 多网卡/无网络/UIA | 兼容保留 |
| 强 token 鉴权与一键断开所有设备 | token/security tests | Token | auth/session service | 旧 token 失效、新 token 可用 | 兼容保留 |
| 启动失败、网卡变化和防火墙提示 | service/form | — | health state/notices | 占用/断网实测 | 兼容保留 |
| “远程连接/演示文稿”两模块 | RemoteControlForm/tests | Window | WPF dashboard | 响应式 UIA | 兼容保留 |
| 文稿列表/详情/规则编辑/危险操作确认 | form/validator | Rules | WPF dashboard | CRUD/确认/长文件名 | 兼容保留 |
| 窄宽布局、DPI、位置恢复 | layout service/V0302 tests | Remote Window | adaptive WPF layout | 700/900 宽、100–200% | 兼容保留 |

## I. 手机/浏览器

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 零安装；zh/en 跟随浏览器 | Web assets/tests | — | 嵌入 Web app/i18n | Playwright 移动视口 | 兼容保留 |
| token 缺失/错误/轮换失效 | security/HTTP tests | Token | auth middleware | HTTP 401/失效 | 兼容保留 |
| 计时状态、时长、模式和全套控制 | app.js/AppCommand/HTTP tests | RemoteCommand | versioned API | API + 浏览器 E2E | 兼容保留 |
| 窗口、闪烁、提示、静音 | 同上 | — | command bus | API/E2E | 兼容保留 |
| 文稿列表、打开、放映、导航、黑白屏、结束/关闭 | web/control | presentation DTO | presentation use cases | adapter HTTP + 真机 | 兼容保留 |
| “计时/演示”标签与能力反馈 | web assets/tests | — | Web state | Playwright | 兼容保留 |
| 横向方向锁、纵向滚动保护、吸附和反向抢占 | app.js; 0.19/0.20 tests | — | 手势状态机 | pointer/touch E2E | 兼容保留 |
| 移动布局、可访问性、常用浏览器 | HTML/CSS/TEST_REPORT | — | semantic Web UI | Chrome/Edge/Safari | 兼容保留 |

## J. 命令、托盘和快捷键

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 默认 F3/F4/F5 | README/config/hotkey tests | legacy + Hotkeys | command registry | 注册/触发实测 | 兼容保留 |
| 全部计时、窗口、闪烁、静音、模式、预设热键 | DefaultHotkeys/commands | Hotkeys | typed commands | 全映射测试 | 兼容保留 |
| 托盘、菜单、热键、远程共享命令 | `AppCommandService.cs` | — | application bus | 各入口同一用例 | 兼容保留 |
| 托盘远程/设置/位置/静音/退出 | context | — | WPF NotifyIcon host | UIA/人工 | 兼容保留 |
| 热键冲突、重注册、保存后即时应用 | HotkeyService/settings | Hotkeys | adapter/validation | 冲突进程 + 保存 | 兼容保留 |

## K. WPF 设置

| 子模块/行为 | 依据 | 配置 | 目标 | 验收 | 状态 |
|---|---|---|---|---|---|
| 时长、模式、超时、到时动作 | SettingsForm；当前 WPF VM | Timer | WPF settings | 真实文本/下拉/复选 UIA | 进行中 |
| 自动行为 | SettingsForm/WPF | Behavior basics | WPF settings | 保存后主程序即时应用 | 进行中 |
| 部分外观和通用热键/更新 | WPF XAML/VM | Appearance/Controls/Update | WPF settings | 数字 UIA/持久化 | 进行中 |
| 完整规则与批量操作 | SettingsForm/rows | Rules | WPF Rules page | CRUD/批量 UIA | 未开始 |
| 三组提醒、语音、声音、闪烁细节 | SettingsForm | Prompts | WPF Alerts page | 替身 + 真音频 | 未开始 |
| 完整外观、多屏定位、大屏 | SettingsForm | Appearance/Placement | WPF Display pages | 混合 DPI UIA | 未开始 |
| 全部快捷键与冲突 | SettingsForm | Controls.Hotkeys | WPF Hotkeys page | 注册替身/UIA | 未开始 |
| 完整远程设置 | Settings/Remote form | RemoteControl | WPF Remote page | HTTP + UIA | 未开始 |
| 未保存、校验、保存/放弃/取消、连续编辑不卡死 | 当前 WPF tests/smokes | all edited | MVVM + async boundary | 发布版四类控件 <3s；退出回归 | 已完成 |
| 经典设置回退直至完整覆盖 | context | 同一 config | 阶段 5 前保留 | 双入口互操作 | 兼容保留 |

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
| 矩阵/架构/配置迁移 | 本文、V4_ARCHITECTURE | 完整工程文档 | fixture/设计走查 | 进行中 |
| 中英文用户指南 | baseline docs | 阶段 6 指南 | 双语人工走查 | 未开始 |
| 人工测试计划 | TEST_REPORT 历史 | 可执行环境/步骤/预期/证据 | 全矩阵签收 | 未开始 |

## 阶段 0 结论

矩阵共 **99** 个行为单元。阶段 1 后达到“已完成”的是 **17** 项；其余多数功能仍可通过经典兼容实现使用，或正在抽取为 Core/Presentation/WPF 边界。这个比例衡量 WPF 正式迁移完成度，不是现有可用功能比例。

阶段 1 本地实测：三个 Release Build 均 0 warnings/0 errors、桌面 274/274、Core 4/4；双 EXE 发布、真实 WPF 设置、主程序退出及正式 WPF 计时窗 smoke 通过。当前机器只有主显示器，大屏热插拔人工步骤保留到具备扩展屏的验收环境，但真实 WPF Window、非主屏约束、共享格式化和显示拓扑均有自动化覆盖。
