# FlyPPTTimer 4.0 连续重构进度

更新日期：2026-08-06

分支：`agent/v4-foundation`

基线：`v0.30.2` (`8921390ac99d574f99be46b7e08d36a191b3e483`)

审计起点：`f051e90d217ca9c0b1f7a7d602128ed49a407b21`

## 当前状态

- 当前阶段：**阶段 4 — 远程控制（已自动开始）**
- 阶段 0 完成度：**100%**
- 阶段 1 完成度：**100%**
- 阶段 2 完成度：**100%**
- 阶段 3 完成度：**100%**
- 阶段 4 完成度：**0%，入口与协议审计已开始**
- 全工程完成度：**约 55%**（矩阵 54/99 完成；阶段 0–3 完成）
- 阶段 0 提交：`3369825d8c983b4589a3f3814be86175a6210cf1`
- 阶段 0 CI：[Windows CI 31090670000](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/31090670000)，成功，含 Artifact 上传。
- 阶段 1 提交：`82712046f0bb56a67d964a3327232c809ca43421`
- 阶段 1 CI：[Windows CI 31093177173](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/31093177173)，成功，直接完成发布版 UI Automation 并上传 Artifact。
- 阶段 2 主提交：`288fbed578a63d05e832415254316bfae6ad4fa1`；UIA 可靠性修复至 `520af4586a270d46e4435b5352b3e147b2fdffff`。
- 阶段 2 CI：[Windows CI 31096841196](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/31096841196)，成功，含直接发布版 UI Automation 和 Artifact 上传。
- 阶段 3 提交：`069ce33a3a1246dd88eb470b372695a0ca78d66c`。
- 阶段 3 CI：[Windows CI 31099197824](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/31099197824)，成功，含 314+4 测试、三组直接发布版 UI Automation 和 Artifact 上传。

| 阶段 | 状态 | 交付 | 提交/CI |
|---|---|---|---|
| 0 审计与架构 | 完成 | 矩阵、架构/迁移、进度、双基准、Artifact | `3369825`; CI 31090670000 成功 |
| 1 WPF 外壳/计时/多屏/大屏 | 完成 | WPF 正式计时/大屏入口、拓扑、UIA | `8271204`; CI 31093177173 成功 |
| 2 完整设置/规则/提醒 | 完成 | 六页 WPF 设置、fixture、UIA | `288fbed` + `520af45`; CI 31096841196 成功 |
| 3 PowerPoint/WPS/联动 | 完成 | typed commands、所有权、RCW/窗口回退、真机 | `069ce33`; CI 31099197824 成功 |
| 4 远程控制 | 进行中 | WPF 电脑端、HTTP、手机网页 | — |
| 5 唯一入口/更新/安装/发布 | 未开始 | 清理无兼容责任的旧 UI | — |
| 6 全功能候选版 | 未开始 | 完整文档与候选 Artifact | — |

## 阶段 0 审计与验证

已审阅 v0.30.2 中英文 README、CHANGELOG、TEST_REPORT、全部产品/测试源码、远程网页、构建/安装/发布/更新脚本和 Actions。矩阵按 A–M 细化行为、证据、配置、目标和验收。

| 对象 | 结果 |
|---|---|
| v0.30.2 Release Build | 0 warnings / 0 errors |
| v0.30.2 桌面测试 | 188/188 通过，0 跳过 |
| 4.0 主程序/Core/WPF Release Build | 各 0 warnings / 0 errors |
| 4.0 桌面/Core 测试 | 264/264；4/4；0 跳过 |
| 发布 | win-x64、自包含、单文件、双 EXE |
| WPF UI smoke | 冷启动 1,332ms；文本 137ms；下拉 151ms；复选 15ms；数字 16ms |
| 主程序/设置退出 smoke | 集成启动 2,108ms；设置关闭后主程序 191ms 响应 |

基线测试首次因独立 .NET 10 SDK 目录不含 .NET 8 runtime 而无法启动 testhost；对同一生成物设置 runtime major roll-forward 后 188/188 通过。这是宿主环境差异，不是代码失败。

| 本地 Artifact（不提交） | 字节 | SHA-256 |
|---|---:|---|
| `artifacts/stage0-publish/FlyPPTTimer.exe` | 51,823,905 | `CA355E6AC3368807EEE1F639FDC304F1577D6D36C712C757C59C06F206F929C0` |
| `artifacts/stage0-publish/FlyPPTTimer.Settings.exe` | 75,353,968 | `6306B8B140DD0A1774C6D0440F7D8FF9738A344DDE3BF26D2539D2D7348B3ED9` |

## 阶段 1 本地验证

| 对象 | 结果 |
|---|---|
| 主程序/Core/WPF Settings Release Build | 各 0 warnings / 0 errors |
| 桌面/Core 测试 | 274/274；4/4；0 跳过 |
| WPF 设置 UI smoke | 启动 1,164ms；四类控件 88/83/23/17ms；取消 105ms |
| 设置退出 smoke | 集成启动 2,915ms；主程序 198ms 响应 |
| 正式 WPF 计时窗 smoke | 冷启动 1,467ms；F3 82ms；F5 隐藏/显示 1,183/14ms |
| 发布 | win-x64、自包含、单文件、双 EXE |

| 本地 Artifact（不提交） | 字节 | SHA-256 |
|---|---:|---|
| `artifacts/phase1-publish/FlyPPTTimer.exe` | 75,564,260 | `43A5D27B64185F7DAC460369F382BE142F3E00B0ABEEADC361D2EDE6269B618C` |
| `artifacts/phase1-publish/FlyPPTTimer.Settings.exe` | 75,577,569 | `CD681FC50B1750EE3253AD0BD44C54CC237A3F7B07CC5C93558B09DF0B8785C0` |

阶段 1 CI 直接发布版 UI Automation：设置启动 1,059ms，文本/下拉/复选/数字 59/77/118/13ms；正式计时窗启动 1,184ms，F3 84ms，F5 隐藏/显示 1,151/26ms。STA 回退未触发；其存在是为了 Runner 无窗口时仍以真实控件失败/通过，而不是跳过。

## 阶段 2 本地验证

WPF 正式设置现覆盖计时、规则、三组提醒/语音/声音/闪烁、全部外观与显示、多屏/大屏、全部快捷键、远程配置、语言、更新字段及配置管理。属性编辑只改变内存 draft；声音复制与配置导入/导出只在显式操作后执行。真实 v0.30.2 fixture 和逐层 `JsonExtensionData` 保证未知字段往返。

| 对象 | 结果 |
|---|---|
| 主程序/Core/WPF Settings Release Build | 各 0 warnings / 0 errors |
| 桌面/Core 测试 | 279/279；4/4；0 跳过 |
| 发布版六页 WPF 设置 UIA | 启动 1,302ms；基础 95/92/133/18ms；提醒 170/20ms；热键 220/18ms；远程 120/11/17ms；脏状态 100ms；取消 106ms |
| 设置退出 smoke | 集成启动 2,948ms；主程序 196ms 响应并重载 |
| WPF 计时回归 | 启动 1,492ms；F3 78ms；F5 隐藏/显示 1,166/14ms |
| 发布 | win-x64、自包含、单文件、双 EXE |

| 本地 Artifact（不提交） | 字节与 SHA-256 |
|---|---|
| `artifacts/phase2/publish/FlyPPTTimer.exe` | 75,684,704 bytes；SHA-256 `BEBEB044290404E9CC3DC6E8569BFEDC947144BFD68912A6132F9D70051BA75E` |
| `artifacts/phase2/publish/FlyPPTTimer.Settings.exe` | 75,715,003 bytes；SHA-256 `585B27CB92F8F9155B12BFB138A5D702E3867FA662CAEDC39A6581A57A8244D9` |

本机 Restore 曾因受限用户 NuGet 缓存指向缺失包失败；改用仓库内已忽略的 `.nuget/packages` 后完整 Restore 与测试通过，未修改依赖或项目文件。

阶段 2 CI 直接操作发布版窗口：设置启动 1,119ms；基础控件 138/156/10/10ms；提醒切页/编辑 317/113ms；热键 153/11ms；远程 135/6/10ms；脏状态 ValuePattern 95ms；取消 110ms。主程序设置集成启动 2,416ms、退出后 194ms 响应；计时窗启动 1,199ms、F3 88ms、F5 隐藏/显示 1,148/116ms。全部步骤和 Artifact 上传成功，未触发无窗口回退。

## 阶段 3 本地验证

`PresentationCommandService` 现在是演示控制的正式 Application 用例边界，统一计时到时、HTTP 和电脑端兼容窗口的 13 种命令，并保持既有 `ppt.*` 协议、15/5 秒超时、STA、500ms monitor、20×100ms 找窗、中文消息和事件位置。修复真机暴露的共享 RCW 被 `FinalReleaseComObject` 提前清空问题，并为 WPS/Office COM HWND 不可读增加原生窗口回退。关闭策略只对程序只读打开的受管文稿抑制伪保存提示，外部文稿保留原生未保存确认。

| 对象 | 结果 |
|---|---|
| 主程序/Core/WPF Settings Release Build | 各 0 warnings / 0 errors |
| 桌面/Core 测试 | 314/314；4/4；0 跳过 |
| Microsoft PowerPoint 64 位真机 | 临时三页文稿：只读打开、受管状态、从头放映、下一页、跳页、黑/白屏及恢复、结束、关闭全部通过；窗口最大化并置前 |
| WPS 演示真机 | `wpp.exe` 兼容 COM 同一非破坏链路通过；原生放映窗口回退成功；未执行会终止用户进程的 ForceQuitAll |
| 发布版六页设置 UIA | 启动 1,334ms；基础 93/113/22/17ms；提醒 133/15ms；热键 217/15ms；远程 129/12/17ms；脏状态 100ms；取消 132ms |
| 设置退出 smoke | 集成启动 3,242ms；主程序 252ms 响应 |
| WPF 计时回归 | 启动 1,557ms；F3 82ms；F5 隐藏/显示 1,142/26ms |
| 发布 | win-x64、自包含、单文件、双 EXE |

| 本地 Artifact（不提交） | 字节与 SHA-256 |
|---|---|
| `artifacts/phase3/publish/FlyPPTTimer.exe` | 75,687,921 bytes；SHA-256 `0AB5E03119803614063A24687E12AE2BB99ACB7767218ABCEE03381775EA2E75` |
| `artifacts/phase3/publish/FlyPPTTimer.Settings.exe` | 75,718,225 bytes；SHA-256 `19C712B734FC3A436E1B6F90CDF9E2577797028121CF4C8290E4DADC3D0C2E61` |

阶段 3 CI 直接发布版 UIA：设置启动 1,234ms，基础控件 64/69/16/13ms，提醒 208/13ms，热键 301/14ms，远程 242/7/13ms，脏状态 115ms，取消 107ms；设置集成启动 2,899ms、主程序 218ms 响应；计时窗启动 1,223ms、F3 90ms、F5 隐藏/显示 1,088/27ms。Restore、三个 Release Build、314+4 测试、双 EXE 发布、校验和与 Artifact 上传全部成功。

## 风险与下一步

- 主 composition root、托盘、远程电脑端、时间到窗口和完整设置仍有 WinForms 兼容实现，矩阵通过前不能删除；正式普通计时窗与大屏已是 WPF。
- WPF Settings 当前仍通过主项目引用配置、声音和显示基础设施；后续随 Application/Infrastructure 分层继续反转，但 ViewModel 属性编辑不直接执行磁盘 I/O。
- PowerPoint/WPS、多屏、音频、热键、安装升级需要实机验收，CI 替身不能冒充。
- 本机只有 `DISPLAY1`，无法完成物理扩展屏热插拔；阶段 1 已覆盖真实 WPF 大屏控件、主屏拒绝、负坐标/DPI/锚点计算和重建生命周期，物理双屏步骤列为阶段 6 人工验收项。
- GitHub Hosted Windows Runner 的 UI Automation 桌面可用性不稳定：31090670000 成功操作发布版窗口，31090929246 随后无法发现同一发布版设置窗口。发布版脚本继续保留且本机必跑；CI 不把环境缺窗计为通过/跳过，而以专用退出码转入 STA 线程内真实 WPF 控件绑定/Dispatcher 测试，回退测试失败仍使 CI 失败。

阶段 4 精确任务：审计矩阵 H/I 与现有 HTTP、token、Revision、状态投影、电脑端窗口和三份 Web 资源；建立可替换 Remote Application 边界，迁移正式 WPF 电脑端“远程连接/演示文稿”窗口，并以 HTTP 集成、真实浏览器手势和发布版 WPF UIA 覆盖全部协议与交互。

## 会话恢复指令

```text
继续 Hona-Cao/FlyPPTTimer 4.0 连续重构。同步 agent/v4-foundation，阅读 docs/v4/CODEX_V4_COMPLETE_REBUILD.md、V0302_PARITY_MATRIX.md、V4_ARCHITECTURE.md、V4_PROGRESS.md，核对最新提交和 CI；从 V4_PROGRESS 的下一步继续，不重做完成阶段，不删除仍为兼容保留的 WinForms 功能。每阶段完成构建、测试、真实 WPF UIA、双 EXE Artifact/SHA-256、文档、提交推送并等待 Actions。
```
