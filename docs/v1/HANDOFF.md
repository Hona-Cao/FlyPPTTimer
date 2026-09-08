# FlyPPTTimer V1 — 新会话交接

## 目标

V1 是对 v0.30.2 的 Rust + Slint 重构，不是功能升级。

最终要求：功能、用户可见选项、默认值、行为、中英文文字、Remote 协议、PowerPoint/WPS 行为与 v0.30.2 对齐；允许改进视觉、性能、稳定性和代码实现。

## 当前工作分支

`codex/v1-06-manual-test`

新会话开始时先读取该分支最新 HEAD，不要依赖旧聊天中的版本号或提交号。

## 2026-09-08 用户授权的体验优化

Remote parity 整改已经审核通过。用户随后明确要求同步详细优化计划并启动执行，继续由 ChatGPT 逐轮审核。

计划入口：[UX_OPTIMIZATION_PLAN.md](UX_OPTIMIZATION_PLAN.md)。当前第一轮 UX-01 只统一桌面设置与 PC Remote 配色及自绘按钮状态；结果见 CODEX_RESULT。后续轮次等待审核，不将此前已通过的逻辑重做，也不将源码/编译通过当作真机体验验收。

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

当前阶段已经从“主体开发”进入“候选版本收口”。

## 最近已解决的问题

### Timer 右键后遮罩/残影

通过真实桌面右键并监测 Win32 消息定位：右键菜单切换 foreground 时，Windows 触发非客户区重绘，导致无框 Timer 出现主题标题栏残影。当前分支已通过 Timer HWND 的轻量消息处理修正。

### 设置 / Remote 打开时闪窗或退出

原因包括 Slint 事件循环重入和首帧 HWND 尚未稳定就显示。当前分支已改为非重入、延后首帧显示并复用已存在窗口。

### 混合 DPI / 跨屏

设置窗口、Remote 窗口和 Timer 已经过多轮修正。当前已在 150% 与 125% 双屏组合上验证基本跨屏尺寸和绘制行为。

## 下一阶段重点

不要再按“大阶段”增加新功能。后续重点是：

1. 用户手工测试发现的真实 Bug。
2. 设置、Remote、Timer、大屏在不同 DPI / 多屏下的显示效果与交互细节。
3. 与 v0.30.2 的功能、选项、文字、默认值和行为差异清零。
4. PowerPoint / WPS、手机 Remote、声音/TTS、多屏的真实使用验收。
5. 发布收口：统一 Rust V1 的 Portable / Installer 路径，清理旧开发期或旧 C# 发布残留。

## 当前需要特别复核的收口项

- `scripts/build-release.ps1` 的版本处理应跟随 Rust 包版本，而不是长期硬编码旧测试版本。
- 仓库旧 `package_release.ps1` 属于旧发布路径，最终 V1 不应同时维护两套发布流程。
- `src/capture.rs` 和 `--capture-settings` / `--capture-windows` 是开发期辅助入口，正式 V1 前评估删除。
- 更新模块只需与 v0.30.2 行为对齐，不扩展成新的更新产品。
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

Codex 负责：只执行当前 `CODEX_TASK.md`，完成后更新 `CODEX_RESULT.md`，提交并推送 review 分支，等待下一轮审核。

真实视觉、PPT/WPS、手机 Remote、声音和多屏体验优先由用户手工测试；不要为了这些体验另建测试系统。

## 产品方向

用户最看重：

**美观、小巧、稳定、快速。**

因此后续 Bug 修复和视觉优化优先解决真实使用问题，保持实现直接，不为了工程形式增加与产品体验无关的复杂度。
