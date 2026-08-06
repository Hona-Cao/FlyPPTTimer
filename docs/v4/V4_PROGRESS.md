# FlyPPTTimer 4.0 连续重构进度

更新日期：2026-08-06

分支：`agent/v4-foundation`

基线：`v0.30.2` (`8921390ac99d574f99be46b7e08d36a191b3e483`)

审计起点：`f051e90d217ca9c0b1f7a7d602128ed49a407b21`

## 当前状态

- 当前阶段：**阶段 1 — WPF 应用外壳、计时与显示（已自动开始）**
- 阶段 0 完成度：**100%**
- 阶段 1 完成度：**本地实现与全部门槛验证完成；提交、推送和 CI 待完成**
- 全工程完成度：**约 20%**（矩阵 17/99 完成；阶段 0 完成，阶段 1 等待提交/CI）
- 阶段 0 提交：`3369825d8c983b4589a3f3814be86175a6210cf1`
- 阶段 0 CI：[Windows CI 31090670000](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/31090670000)，成功，含 Artifact 上传。

| 阶段 | 状态 | 交付 | 提交/CI |
|---|---|---|---|
| 0 审计与架构 | 完成 | 矩阵、架构/迁移、进度、双基准、Artifact | `3369825`; CI 31090670000 成功 |
| 1 WPF 外壳/计时/多屏/大屏 | 本地完成 | WPF 正式计时/大屏入口、拓扑、UIA | 待提交/CI |
| 2 完整设置/规则/提醒 | 未开始 | WPF 不再依赖经典设置 | — |
| 3 PowerPoint/WPS/联动 | 未开始 | Presentation 全能力 | — |
| 4 远程控制 | 未开始 | WPF 电脑端、HTTP、手机网页 | — |
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

## 风险与下一步

- 主 composition root、托盘、远程电脑端、时间到窗口和完整设置仍有 WinForms 兼容实现，矩阵通过前不能删除；正式普通计时窗与大屏已是 WPF。
- WPF Settings 当前引用 WinForms 主项目；阶段 1–2 需反转到 Application/Infrastructure 抽象。
- WPF 设置尚缺规则、提醒声音、完整显示、完整热键与远程设置。
- PowerPoint/WPS、多屏、音频、热键、安装升级需要实机验收，CI 替身不能冒充。
- 本机只有 `DISPLAY1`，无法完成物理扩展屏热插拔；阶段 1 已覆盖真实 WPF 大屏控件、主屏拒绝、负坐标/DPI/锚点计算和重建生命周期，物理双屏步骤列为阶段 6 人工验收项。
- GitHub Hosted Windows Runner 的 UI Automation 桌面可用性不稳定：31090670000 成功操作发布版窗口，31090929246 随后无法发现同一发布版设置窗口。发布版脚本继续保留且本机必跑；CI 不把环境缺窗计为通过/跳过，而以专用退出码转入 STA 线程内真实 WPF 控件绑定/Dispatcher 测试，回退测试失败仍使 CI 失败。

阶段 2 精确任务：把经典设置中尚未覆盖的规则 CRUD/批量操作、三组提醒、语音/声音、完整闪烁、全部外观与显示、全部快捷键、远程、语言/更新迁移到 WPF；建立 v0.30.2 配置 fixture 和未知字段 round-trip；完成后正式入口不再依赖经典设置。

## 会话恢复指令

```text
继续 Hona-Cao/FlyPPTTimer 4.0 连续重构。同步 agent/v4-foundation，阅读 docs/v4/CODEX_V4_COMPLETE_REBUILD.md、V0302_PARITY_MATRIX.md、V4_ARCHITECTURE.md、V4_PROGRESS.md，核对最新提交和 CI；从 V4_PROGRESS 的下一步继续，不重做完成阶段，不删除仍为兼容保留的 WinForms 功能。每阶段完成构建、测试、真实 WPF UIA、双 EXE Artifact/SHA-256、文档、提交推送并等待 Actions。
```
