# FlyPPTTimer V1 — 新会话交接

## 目标

V1 是对 v0.30.2 的 Rust + Slint 重构，不是功能升级。

最终要求：功能、用户可见选项、默认值、行为、中英文文字、Remote 协议、PowerPoint/WPS 行为与 v0.30.2 对齐；允许改进视觉、性能、稳定性和代码实现。

用户最终要求进一步明确为：

- 当前全部既有功能必须稳定运行；
- 不因为重构削减功能；
- 当前整体排版和体验方向基本满意，不再进行无目的的大规模 UI 重做；
- 最终界面应美观、配合舒服、风格统一和谐；
- 后续优先消灭真实 Bug、交互不一致和发布阻断项。

## 当前工作分支

`codex/v1-06-manual-test`

新会话开始时先读取该分支最新 HEAD，不要依赖旧聊天中的版本号或提交号。

## 2026-09-10 UX13 审核后的当前状态

UX13 已完成设置说明弹窗、设置规则布局、PC Remote 规则列表和批量设置等视觉/交互调整，整体 UI 方向继续保留。

ChatGPT 对 UX13 提交 `007f4dc2aab1c9e5c6e81cfa53961263c013f2e5` 做了源码审核后，确认还有几项需要在候选版本前修正：

1. F3 Start/Pause 在 Paused 状态错误调用 `start()`，会从头重新计时，而不是 `resume()`；
2. Settings 长期保留的完整 draft 可能覆盖 PC Remote 等其他入口已经保存的新配置；
3. PC Remote 的“下次服务端口”输入会被周期状态刷新写回旧值；
4. PC Remote 规则多选 / 单项 editor / batch editor 的选中和保存状态存在一致性问题；
5. `scripts/build-release.ps1` 的版本号仍硬编码旧值，需要改为以 Rust `Cargo.toml` package version 为唯一来源。

详细修复边界、复现路径和验收标准全部写在最新的 `docs/v1/CODEX_TASK.md`。**CODEX_TASK 是当前唯一实现指令。**

## 新电脑环境说明

用户现在使用一台新的电脑，本地开发和手测环境可能暂缺。

因此本轮 Codex 开始编码前必须先完成环境预检：

- Git / 当前分支 / HEAD；
- Rust / Cargo / rustup；
- MSVC Windows 构建环境；
- Windows SDK / `rc.exe`；
- 当前锁定依赖下的 `cargo check`；
- PowerPoint、WPS、双屏、Inno Setup、ffmpeg 等真实手测/发布工具当前是否可用。

原则：

- 构建工具链缺失时先准备环境，不通过修改产品源码绕过；
- Office / WPS / 双屏等真实测试条件缺失不阻止纯源码修复，但必须记录为待用户手测；
- 不因新电脑环境擅自升级 Rust 项目依赖或重做技术实现。

## 2026-09-08 用户授权的体验优化

Remote parity 整改已经审核通过。用户随后明确要求同步详细优化计划并启动执行，继续由 ChatGPT 逐轮审核。

计划入口：[UX_OPTIMIZATION_PLAN.md](UX_OPTIMIZATION_PLAN.md)。UX-01～UX13 已进行了多轮真实问题驱动的优化。当前不再按 UX 阶段扩展，进入稳定性收口。

## 当前实现状态

主体功能已经基本完成：

- Rust + Slint 单程序主体
- Timer：倒计时、正计时、暂停/恢复、Restart、超时
- 六页设置窗口与配置编辑
- 文件规则
- 中英文 UI
- 提醒、闪烁、声音、TTS、静音
- 全局快捷键、托盘、Timer 右键菜单
- PowerPoint / WPS 演示控制与自动计时
- Remote HTTP、原 v0.30.2 Web Remote、PC Remote 两页
- Remote 演示文稿列表与规则管理
- 多显示器、大屏计时、九宫格定位
- 更新模块
- Portable / Installer 基础脚本

当前阶段是“候选版本稳定性收口”，不是继续增加新功能。

## 最近已解决的问题

### Timer 右键后遮罩/残影

通过真实桌面右键并监测 Win32 消息定位：右键菜单切换 foreground 时，Windows 触发非客户区重绘，导致无框 Timer 出现主题标题栏残影。当前分支已通过 Timer HWND 的轻量消息处理修正。

### 设置 / Remote 打开时闪窗或退出

原因包括 Slint 事件循环重入和首帧 HWND 尚未稳定就显示。当前分支已改为非重入、延后首帧显示并复用已存在窗口。

### 混合 DPI / 跨屏

设置窗口、Remote 窗口和 Timer 已经过多轮定向修正。此前已经在 150% 与 125% 双屏组合上做过基本跨屏验证；新电脑若暂时没有相同双屏环境，不要把缺失条件当成代码失败。

## 下一阶段重点

不要再按“大阶段”增加新功能。后续重点是：

1. 执行最新 `CODEX_TASK.md` 中的 UX13 审核后稳定性修复；
2. 用户手工测试发现的真实 Bug；
3. 与 v0.30.2 的功能、选项、文字、默认值和行为差异清零；
4. PowerPoint / WPS、手机 Remote、声音/TTS、多屏的真实使用验收；
5. 发布收口：统一 Rust V1 的 Portable / Installer 路径，清理旧开发期或旧 C# 发布残留。

## 当前需要特别复核的收口项

- `scripts/build-release.ps1` 的版本处理必须跟随 Rust package version，而不是长期硬编码旧测试版本；
- 仓库旧 `package_release.ps1` 属于旧发布路径，最终 V1 不应同时维护两套发布流程；
- `src/capture.rs` 和 `--capture-settings` / `--capture-windows` 是开发期辅助入口，正式 V1 前评估删除；
- 更新模块只需与 v0.30.2 行为对齐，不扩展成新的更新产品；
- `V1_BASELINE_CHECKLIST.md` 需要在 RC 阶段逐项做最终 parity 复核。

## 工作方式

### 新 ChatGPT 会话

先读取：

1. `docs/v1/HANDOFF.md`
2. `docs/v1/V1_BASELINE_CHECKLIST.md`
3. `docs/v1/CODEX_RESULT.md`
4. `docs/v1/CODEX_TASK.md`
5. 当前 V1 相关源码

ChatGPT 负责：审查现状、判断下一项真实问题、更新 `CODEX_TASK.md`、审核 Codex 推送结果。

### 新 Codex 会话

先读取根目录 `AGENTS.md`，再按其中顺序读取交接、基线和当前任务。

Codex 负责：

1. 拉取 `codex/v1-06-manual-test` 最新 HEAD；
2. 按最新 `CODEX_TASK.md` 先完成新电脑环境预检；
3. 只执行当前任务，不扩大修改范围；
4. 完成后更新 `docs/v1/CODEX_RESULT.md`；
5. commit 并 push 到 review 分支；
6. 停止继续编码，等待 ChatGPT 再审核。

真实视觉、PPT/WPS、手机 Remote、声音和多屏体验优先由用户手工测试；不要为了这些体验另建复杂测试系统。

## 产品方向

用户最看重：

**美观、小巧、稳定、快速。**

当前实现已经进入稳定性收口阶段。后续 Bug 修复和视觉优化优先解决真实使用问题，保持实现直接，不为了工程形式增加与产品体验无关的复杂度。
