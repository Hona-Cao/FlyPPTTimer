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

主要技术债务：`FlyPPTTimerContext` 仍是 WinForms composition root；正式计时窗、大屏、远程电脑端和完整设置仍由 WinForms 承担；WPF Settings 直接引用主项目；Remote 尚未形成独立层。

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

配置采用“原 JSON DOM + typed projection”双层迁移：读取失败先保留损坏副本并尝试备份；按 `Version` 运行幂等迁移；缺失字段补默认、非法字段按 0.30.2 语义归一化；保存时只覆盖已知字段并保留未知字段；同目录临时写入、flush、时间戳备份、原子替换。token 不改写；自定义声音引用和 `alert-sounds/` 不因安装升级删除。

阶段 2 增加 `V4_CONFIGURATION_MIGRATION.md` 和真实 0.30.2 fixture，覆盖旧三快捷键字段与 `Hotkeys`、规则、token、窗口位置、显示器、提示声音及未知字段。

## 7. Presentation 迁移策略

保留 `IPresentationControlService` 为 Application 端口：

- `PresentationStaDispatcher` 承担 COM STA 与现有 15/5 秒超时。
- `PresentationStateMonitor` 保持 500ms 刷新并发布不可变副本。
- detector/terminator 集中进程名、能力与退出语义。
- activator 集中最大化、置前、TopMost 重试；调用方仍保留 20×100ms 放映窗口查找。
- 自动测试使用替身；Office/WPS 真机验收不由缺少 Office 的 CI 冒充。

受管文稿注册表区分程序只读打开与外部打开；关闭顺序、未保存状态、临时 `SlideShowSettings` 恢复和 COM 释放是不可回退契约。

## 8. Remote 兼容策略

保持默认 4080、随机端口选项、URL token、现有扁平 timer 字段和 `RemoteCommand`；新增字段必须可选。token 至少 128 bit，日志/诊断掩码；“断开全部”轮换 token 并增加 Revision。HTTP handler 仅解析、鉴权、映射 DTO，业务交给 command bus。静态网页继续嵌入，HTTP 集成测协议，真实浏览器测移动手势与布局。

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
