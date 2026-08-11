# FlyPPTTimer 4.0 架构与迁移方案

更新日期：2026-08-06

## 1. 目标与约束

4.0 以 WPF 为唯一正式桌面入口，同时对 `v0.30.2` 的用户行为、配置语义、远程协议、PowerPoint/WPS 控制和安装升级保持等价。迁移期间经典 WinForms 是兼容实现，不是可随意删除的旧代码；只有 `V0302_PARITY_MATRIX.md` 全部通过后才能移除相应入口。

- UI 线程不得同步等待子进程、COM、HTTP 或磁盘 I/O。
- ViewModel 不直接操作 COM、Win32、进程、`HttpListener`、文件系统或音频设备。
- 计时使用单调时钟；所有 UI 和远程端消费同一状态快照。
- 配置迁移先保留数据再归一化；未知字段必须 round-trip，token 和声音文件不得进入日志或 Artifact。
- PowerPoint/WPS 的 STA、500ms 状态监控、命令超时、窗口查找与激活语义以现有抽象和 v0.30.2 行为为准。
- 远程现有 JSON 字段和命令向后兼容；破坏性演进只能新增版本化端点。

## 2. 当前架构审计

可复用成果：

- `FlyPPTTimer.Core`：`TimerEngine`、单调时钟、明确的运行状态和方向模型。
- 配置：`ConfigSchema`、`ConfigService` 的默认值、校验、原子保存、备份和导入导出。
- Presentation：`IPresentationControlService`、PowerPoint adapter、STA dispatcher、500ms monitor、process detector/terminator、window activator。
- WPF：独立 `FlyPPTTimer.Settings.exe`、基础 MVVM 设置、未保存状态/验证/保存放弃取消、真实 UI Automation 与退出死锁回归。
- CI：三个项目构建、桌面/Core 测试、双单文件发布、WPF 控件和主程序退出 smoke、SHA-256 Artifact。

阶段 1 已将正式普通计时窗和大屏窗切换为同进程 WPF Window；阶段 2 完成正式 WPF Settings；阶段 4 又把正式远程电脑端切换为同进程 WPF dashboard；阶段 5 把“时间到”全屏窗口也切换为 WPF `WpfTimeUpOverlayWindow`，并删除了无兼容责任的旧 WinForms UI（计时窗/大屏/设置/远程/时间到 Form 及 6 个 helper 控件）与经典设置回退。主要技术债务：`FlyPPTTimerContext` 仍是兼容 composition root；托盘菜单仍复用少量 Forms 基础设施（`LocalizedMessageDialog`、`ModernTheme`）；WPF Settings 直接引用主项目；静音/结束放映/更新检查等少量行为仍标记为“兼容保留”待阶段 6 收敛。

## 3. 目标分层

```text
FlyPPTTimer.Desktop (WPF shell, views, view models, UI adapters)
        │
        ▼
FlyPPTTimer.Application (use cases, commands, state projections, orchestration)
   │           │             │
   ▼           ▼             ▼
Core       Presentation     Remote abstractions
(timer,    Integration      (protocol DTOs,
rules)     abstractions      auth, state sync)
   ▲           ▲             ▲
   └───────────┴─────────────┘
             │
FlyPPTTimer.Infrastructure.Windows
(config, files, logging, hotkeys, displays, audio, speech,
 processes, Win32, COM adapters, HTTP host, update/install)
```

项目数量可按阶段调整，但依赖方向固定：桌面层依赖 Application/抽象；基础设施实现抽象；Core 不依赖 Windows UI；ViewModel 不引用具体基础设施。

## 4. 运行时所有权

最终 WPF Host 是唯一 composition root：

| 所有者 | 生命周期 | 责任 |
|---|---|---|
| AppHost | 进程 | 单实例、配置、日志、组装、启动、受控退出 |
| TimerCoordinator | 进程 | TimerEngine、规则、提醒阈值、统一快照 |
| WindowCoordinator | 按需窗口 | 主计时、多屏、大屏、时间到、设置、远程面板 |
| PresentationCoordinator | 进程 | STA、monitor、COM adapter、窗口激活、进程检测/终止 |
| RemoteHost | 可启停 | HTTP、token、静态资源、Revision、命令桥接 |
| HotkeyCoordinator | 进程 | 注册、冲突、配置后的原子重注册 |
| UpdateCoordinator | 按需 | 查询、下载、校验、安装模式策略 |

退出顺序：停止接受命令 → 停止 HTTP/热键/检测 → 关闭窗口与提醒 → 释放 Presentation/COM → 刷新配置与日志 → 退出 Dispatcher。所有停止操作幂等；关闭设置子进程只异步通知 UI。

## 5. 状态与命令流

1. 托盘、WPF、快捷键、Remote API 和演示联动只发 typed command。
2. Application 用例校验命令并调用 Core 或端口。
3. TimerEngine/Presentation monitor 产生不可变快照和递增 Revision。
4. WPF Dispatcher、远程轮询和其他窗口订阅同一快照，不拥有业务状态。
5. 提醒调度器按轮次 ID 和阈值保证每种提醒每轮最多一次。

## 6. 配置兼容策略

配置采用 typed projection + 各持久化对象 `JsonExtensionData`：读取失败先保留损坏副本并尝试备份；按 `SchemaVersion` 运行幂等迁移；缺失字段补默认、非法字段按 0.30.2 语义归一化；保存时覆盖已知字段并逐层保留未知 JSON；同目录临时写入、复核、时间戳备份、原子替换。token 不改写；自定义声音引用和 `alert-sounds/` 不因安装升级删除。

阶段 2 增加 `V4_CONFIGURATION_MIGRATION.md` 和真实 0.30.2 fixture，覆盖旧三快捷键字段与 `Hotkeys`、规则、token、窗口位置、显示器、提示声音及未知字段。

阶段 2 的 WPF Settings 使用一个窗口级协调 ViewModel 和三个可观察 draft（Prompt、Rule、Hotkey）。属性 setter 只更新内存和脏状态；规则/声音选择由 WPF 对话框适配，声音复制、配置导入/导出在显式异步命令后执行。保存先整体校验，再一次性投影回 `AppConfig` 并原子写入。六个页面共享同一 draft，因此切页不会重新读取磁盘或丢失未保存状态。

## 7. Presentation 迁移策略

保留 `IPresentationControlService` 为 provider 端口，并由阶段 3 的 `PresentationCommandService` 提供正式 Application 用例边界。托盘计时到时、HTTP、电脑端兼容窗口和后续 WPF 电脑端只发送 `PresentationCommandKind`；仅该服务把强类型命令映射回稳定的 `ppt.*` 远程协议：

- `PresentationStaDispatcher` 承担 COM STA 与现有 15/5 秒超时。
- `PresentationStateMonitor` 保持 500ms 刷新并发布不可变副本。
- detector/terminator 集中进程名、能力与退出语义。
- activator 集中最大化、置前、TopMost 重试；调用方仍保留 20×100ms 放映窗口查找。
- COM 引用按每次取得的 RCW 引用平衡释放，禁止 `FinalReleaseComObject` 清空仍由别名使用的共享 Application RCW。
- PowerPoint/WPS 无法通过 COM 读取放映 HWND 时，从可见 `POWERPNT`/`wpp`/`wps` 窗口中按前台、放映标题、窗口类和面积选择目标，再交给同一 activator。
- 自动测试使用替身；Office/WPS 真机验收不由缺少 Office 的 CI 冒充。

受管文稿注册表区分程序只读打开与外部打开；只有受管只读文稿可抑制临时放映设置产生的伪保存提示，外部文稿必须保留 Office/WPS 原生未保存确认。关闭顺序、临时 `SlideShowSettings` 恢复和 COM 释放是不可回退契约。

## 8. Remote 兼容策略

保持默认 4080、随机端口选项、URL token、现有扁平 timer 字段和 `RemoteCommand`；新增字段必须可选。token 至少 128 bit，日志/诊断掩码；“断开全部”轮换 token 并增加 Revision。HTTP handler 仅解析、鉴权、映射 DTO，业务交给 command bus。静态网页继续嵌入，HTTP 集成测协议，真实浏览器测移动手势与布局。

阶段 4 以 `RemoteDashboardService` 作为 WPF 电脑端的 Application facade，统一配置副本、监听生命周期、地址发现、URL、规则 CRUD 和 typed Presentation command；`WpfRemoteControlWindow` 只绑定快照和发命令，不直接监听端口、写文件或操作 COM。正式托盘/启动参数入口均打开该 WPF 窗口，经典 `RemoteControlForm` 仅保留兼容源码，待阶段 5 按删除门槛清理。

远程监听器当前采用每连接一请求模型，响应显式声明 `Connection: close`；真实端口测试连续验证 token、`/state`、计时命令、演示命令和 token 轮换。`operationId` 穿过 Remote DTO、typed command 和 provider，保持既有去重/追踪语义。网页验证由 Playwright CLI 在真实 Chromium 中执行，覆盖 390×844、POST、演示状态、中英文和连续反向手势，不以静态字符串测试冒充浏览器行为。

## 9. WPF 窗口策略

- `App.xaml` 最终启动主 WPF Host，而非仅设置预览。
- 主计时、多屏副本、大屏和时间到窗口共享状态 ViewModel，各自拥有窗口/显示策略。
- 控件使用稳定 `AutomationId`；关键 UIA 操作仍须 <3 秒，冷启动单独允许 20 秒。
- 保存/放弃/取消通过异步边界，属性 setter 不做磁盘 I/O。
- 每迁移一个正式入口就切换命令路由；阶段 2 全覆盖前保留经典设置回退。

## 10. 阶段与删除门槛

| 阶段 | 正式入口切换 | 删除条件 |
|---|---|---|
| 1 | WPF shell、计时/多屏/大屏 | D/E 全通过后才可停用相关 Form，暂不删除 |
| 2 | 完整设置、规则、提醒、声音、热键 | K/F/C/J 全通过且不依赖经典设置后移除 SettingsForm |
| 3 | Presentation 状态与命令入口 | G 全通过；底层 COM/Win32 adapter 保留 |
| 4 | WPF 远程电脑端与新 Remote 层 | H/I 全通过后移除 RemoteControlForm |
| 5 | WPF 唯一进程入口、更新/安装/便携 | 全矩阵无“兼容保留”后移除 WinForms UI/root |
| 6 | 候选版 | 文档、人工验收、CI、配置升级/回退全部完成 |

## 11. 测试与质量门槛

Core 测假时钟/状态机/规则/提醒；Application 测命令/投影/取消；Infrastructure 测配置 fixture、HTTP/auth、显示计算、进程/Win32/COM wrapper；WPF 测 STA 真实控件与发布版 UIA；系统层人工覆盖多屏/混合 DPI、Office/WPS、音频、热键、安装升级。

每阶段必须保持 Release 0 warnings / 0 errors、既有测试全过、双 EXE 同目录可运行、SHA-256 可复核、GitHub Actions 成功且 Artifact 可下载。

## 12. 阶段 0 决策

- 基线固定到 tag `v0.30.2`，不随 `main` 漂移。
- 阶段 0 不迁移产品代码，只建立可审计基线和验证链。
- 当前 WPF Settings 可复用但不是 4.0 主应用；正式入口从阶段 1 建立。
- 经典 WinForms 仍承担大多数兼容功能，禁止提前删除。
