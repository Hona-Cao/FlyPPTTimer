# FlyPPTTimer V1 — 当前 Codex 任务

状态：**待执行**  
基线：`docs/v1/V1_BASELINE_CHECKLIST.md`  
当前审核代码起点：`codex/v1-05-window-audit`  
目标：产出一个供用户手工测试的下一版候选，并把源码与结果报告推送到 GitHub 供 ChatGPT 审核。

## 1. 先修复计时窗口点击后遮罩/残影 Bug

ChatGPT 已审查当前审核分支。异常截图在 150% DPI 下顶部出现约 34px 的透明/空白区域，与 Windows 非客户区高度高度吻合。当前 `AppWindow` 已由 Slint 使用 `no-frame: true`，但 `src/window.rs::apply_native_window()` 又通过 Win32 修改 `GWL_STYLE` 并使用 `SWP_FRAMECHANGED` 强制重新计算框架，极可能造成 Slint/winit software surface 与 Win32 client rect 失配。

只做下面的最小修复，不重新设计窗口系统：

1. 普通 Timer 的 `apply_native_window()` 删除全部 `GWL_STYLE` 修改；不再手工删除 `WS_CAPTION/WS_THICKFRAME/...`，不再添加 `WS_POPUP`。无框由 Slint `no-frame: true` 负责。
2. 普通 Timer 的扩展样式保留现有值并加入 `WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`，移除 `WS_EX_APPWINDOW`，继续按配置切换 `WS_EX_TRANSPARENT`。
3. 普通 Timer 的 `SetWindowPos()` 删除 `SWP_FRAMECHANGED`，只保留现有 TopMost 与 `SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE`。
4. `configure_timer_window()` 保留首次 `apply_native_window()`；80ms 延迟回调中删除第二次 `apply_native_window()`，只做 `refresh_shape()`。
5. 暂时不要改 `WS_EX_LAYERED`、renderer、透明度实现、圆角算法或 Timer。
6. Release 后人工验证：普通左键点击、拖动、右键菜单、150% DPI、圆角、透明度、置顶、任务栏不出现。
7. 若以上修复后仍能复现同一 Bug，只做一个 A/B：临时 100% 不透明并移除 `WS_EX_LAYERED/SetLayeredWindowAttributes`。记录结果后停止继续加补丁。

禁止为这个问题增加 watchdog、InvalidateRect 定时器、重绘循环、窗口修复线程或其他防御性机制。

## 2. 当前尚未完全还原/尚未确认的项目

以下是当前历史汇报和源码审查中仍未闭环的项目。逐项以 v0.30.2 实际源码确认；只补真实差异，不凭猜测新增实现。

### A. 明确需要处理

- **计时窗口遮罩/残影 Bug**：本任务第一优先级。
- **文字过宽行为**：第二阶段曾明确报告“v0.30.2 的文字过宽自动扩大窗口提示”尚未迁移，之后没有确认已补齐。检查 v0.30.2 `TimerOverlayForm` 的真实行为并等价恢复，包括原有提示文字/触发条件；不要设计新的自适应布局系统。
- **Remote operation/busy 行为**：当前协议保留了 operation 结构，但之前报告没有恢复 v0.30.2 的细粒度 busy/operation 行为。检查旧 Web/Remote PC UI 是否实际依赖或显示它；只有旧版真实使用的部分才补齐。
- **更新/Portable/Installer**：当前已有 `src/updater.rs`、安装脚本和打包脚本，但尚未经过用户手工验收。核对 v0.30.2 的用户可见更新流程、安装版/便携版路径和文字，修正真实差异。禁止 SHA-256、文件完整性校验和复杂更新框架。

### B. 功能代码已存在，但仍需真实手工测试

不要为了这些项目建立自动测试平台，只确保候选版可由用户手工验证：

- PowerPoint：真实全屏放映、自动开始/退出停止重置、规则、翻页、跳页、黑/白屏、结束放映、时间到动作。
- WPS：同等真实场景；确认当前 COM/进程/全屏检测在用户安装版本上行为可接受。
- Remote：真实手机/另一台局域网设备访问二维码链接、Timer 控制、演示控制和状态同步。
- 多显示器：单屏、所有屏幕、九宫格、微调、大屏、不同 DPI、运行中显示器变化。
- 声音/TTS/静音/五种闪烁：此前主要靠代码和非发声验证，需用户实际感官测试。
- Portable、Installer、更新检测：需要用户实际运行/安装/卸载/更新流程测试。

### C. 最终基线复核

在不大改代码的前提下快速对照 `V1_BASELINE_CHECKLIST.md` 和 v0.30.2，确认：

- 设置窗口仍只有 6 页；没有新增设置项。
- 中文/英文用户可见文字、选项和默认值没有漂移。
- 托盘菜单和 Timer 右键菜单与 v0.30.2 一致。
- Remote Web 继续直接使用 v0.30.2 的 HTML/CSS/JS 和协议，不重新设计。
- 不存在 SHA-256、Artifact 哈希或文件完整性哈希实现。

不要把这一步变成新的测试/审计框架。

## 3. 产出手工测试候选

完成上面真实需要的修正后：

1. 运行现有的 `cargo fmt --check`、`cargo clippy --all-targets -- -D warnings`、`cargo test`、`cargo build --release`。
2. 不追求新增测试数量；只在本轮新增的纯逻辑确实容易回归时增加极少量测试。
3. 生成用户可直接手工测试的 Release EXE；如果现有 Portable/Installer 打包脚本可正常工作，也生成对应本地候选产物。
4. **不要创建 GitHub Release、Tag 或正式发布页。**
5. 不为了本轮测试改成复杂版本管理；沿用当前候选版本策略即可，并在结果报告写清实际版本号和产物名。

## 4. GitHub 联动要求

这是本轮必须完成的一部分：

1. 从当前审核分支继续开发，完成后推送到一个清晰的 review 分支；建议使用 `codex/v1-06-manual-test`，若已存在则继续使用该分支。
2. 将本轮所有源码、配置、脚本和文档修改提交并推送。
3. 不提交 `target/`、EXE、ZIP、Installer 二进制或其他大型构建产物。
4. 更新 `docs/v1/CODEX_RESULT.md`，必须包含：
   - review 分支名
   - 最终 commit SHA
   - 主要修改文件
   - 窗口 Bug 修复方式及人工复现结果
   - 上述“尚未还原项目”的处理结果：已补齐 / 旧版不存在无需处理 / 留待用户手工验证
   - 编译、Clippy、测试结果
   - Release EXE 路径与大小
   - Portable/Installer 本地路径与大小（若生成）
   - 给用户的简短手工测试清单
   - 仍存在的已知差异或阻塞问题
5. 推送完成后停止，不进入新功能开发，不创建 Release。

## 5. 工程边界

保持简单：

- 不使用 SHA-256 或类似完整性哈希。
- 不新增 defensive watchdog/retry/fallback 框架。
- 不新增 GUI 自动化、截图哈希、Playwright CI、覆盖率指标或大规模 Mock。
- 不为了清理代码做大规模重构。
- 不修改 v0.30.2 或 `agent/v4-foundation`。
- 不新增任何 v0.30.2 没有的产品功能。

目标是让用户拿到一个**功能尽可能完整、窗口 Bug 已修复、适合真实手工测试**的 V1 候选版本，并让 ChatGPT 能从 GitHub 精确审核实现结果。
