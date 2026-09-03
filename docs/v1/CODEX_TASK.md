# FlyPPTTimer V1 — 当前 Codex 任务

状态：**待执行**  
基线：`docs/v1/V1_BASELINE_CHECKLIST.md`  
当前审核代码起点：`codex/v1-05-window-audit`  
目标：产出一个供用户手工测试的下一版候选，并把源码与结果报告推送到 GitHub 供 ChatGPT 审核。

## 1. 先修复计时窗口点击后遮罩/残影 Bug

ChatGPT 已审查当前审核分支。异常截图在 150% DPI 下顶部出现约 34px 的透明/空白区域，与 Windows 非客户区高度吻合。当前 `AppWindow` 已由 Slint 使用 `no-frame: true`，但 `src/window.rs::apply_native_window()` 又通过 Win32 修改 `GWL_STYLE` 并使用 `SWP_FRAMECHANGED` 强制重新计算框架，极可能造成 Slint/winit software surface 与 Win32 client rect 失配。

只做下面的最小修复，不重新设计窗口系统：

1. 普通 Timer 的 `apply_native_window()` 删除全部 `GWL_STYLE` 修改；不再手工删除 `WS_CAPTION/WS_THICKFRAME/...`，不再添加 `WS_POPUP`。无框由 Slint `no-frame: true` 负责。
2. 普通 Timer 的扩展样式保留现有值并加入 `WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`，移除 `WS_EX_APPWINDOW`，继续按配置切换 `WS_EX_TRANSPARENT`。
3. 普通 Timer 的 `SetWindowPos()` 删除 `SWP_FRAMECHANGED`，只保留现有 TopMost 与 `SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE`。
4. `configure_timer_window()` 保留首次 `apply_native_window()`；80ms 延迟回调中删除第二次 `apply_native_window()`，只做 `refresh_shape()`。
5. 暂时不要改 `WS_EX_LAYERED`、renderer、透明度实现、圆角算法或 Timer。
6. Release 后人工验证：普通左键点击、拖动、右键菜单、150% DPI、圆角、透明度、置顶、任务栏不出现。
7. 若仍能复现同一 Bug，只做一次 A/B：临时 100% 不透明并移除 `WS_EX_LAYERED/SetLayeredWindowAttributes`，记录结果后停止并汇报。

## 2. 补齐当前仍未闭环的基线项目

逐项以 v0.30.2 实际源码确认，只补真实差异。

- **文字过宽行为**：第二阶段曾报告尚未迁移。检查 v0.30.2 `TimerOverlayForm` 的真实行为并等价恢复，包括原有提示文字和触发条件。
- **Remote operation/busy 行为**：检查旧 Web/Remote PC UI 是否实际依赖或显示；只恢复旧版真实使用的部分。
- **更新 / Portable / Installer**：当前已有 `src/updater.rs`、安装脚本和打包脚本。核对 v0.30.2 的用户可见更新流程、安装版/便携版路径和文字，修正真实差异，并准备本地手工测试候选。

同时快速对照 `V1_BASELINE_CHECKLIST.md` 与 v0.30.2，确认：

- 设置窗口仍只有 6 页，用户可见设置项没有漂移。
- 中文/英文文字、选项和默认值与基线一致。
- 托盘菜单和 Timer 右键菜单与 v0.30.2 一致。
- Remote Web 继续沿用 v0.30.2 的 HTML/CSS/JS 与协议。

不要把基线复核扩展成新的审计工程；发现真实差异就修正，没有差异就继续。

## 3. 产出手工测试候选

完成修正后：

1. 运行现有的 `cargo fmt --check`、`cargo clippy --all-targets -- -D warnings`、`cargo test`、`cargo build --release`。
2. 生成用户可直接手工测试的 Release EXE；如果现有 Portable/Installer 打包脚本可正常工作，也生成对应本地候选产物。
3. 不创建 GitHub Release、Tag 或正式发布页。
4. 不为了本轮候选做大规模重构或增加与实际修复无关的测试设施。

本轮手工测试重点应覆盖：

- Timer：点击后遮罩 Bug、拖动、右键、圆角、透明度、提醒、声音/TTS、闪烁、快捷键。
- PowerPoint：真实放映、自动开始、退出停止/重置、文稿规则、翻页、跳页、黑/白屏、结束放映、时间到动作。
- WPS：同等可支持场景。
- Remote：真实手机/另一台局域网设备、二维码、Timer 控制、演示控制、状态同步。
- 多显示器：单屏、所有屏幕、九宫格、微调、大屏、不同 DPI、显示器变化。
- Portable / Installer / 更新检测：实际运行、安装、卸载和更新流程。

无法在当前环境完成的真实场景不要模拟成复杂测试系统，留给用户手工测试并在结果中注明。

## 4. GitHub 联动要求

1. 从当前审核分支继续开发，完成后推送到 review 分支，优先使用 `codex/v1-06-manual-test`。
2. 提交并推送本轮所有源码、配置、脚本和文档修改。
3. 不提交 `target/`、EXE、ZIP、Installer 二进制或其他大型构建产物。
4. 更新 `docs/v1/CODEX_RESULT.md`，至少包含：
   - review 分支名
   - 最终 commit SHA
   - 主要修改文件
   - 窗口 Bug 修复方式及人工复现结果
   - 上述未闭环项目的处理结果
   - 编译、Clippy、测试结果
   - Release EXE 路径与大小
   - Portable/Installer 本地路径与大小（若生成）
   - 给用户的简短手工测试清单
   - 仍存在的已知差异或阻塞问题
5. 推送完成后停止，等待 ChatGPT 审核和用户手工测试反馈。

保持当前简单结构，只增加完成 v0.30.2 等价行为和修复实际问题所需的代码。不要修改 v0.30.2 或 `agent/v4-foundation`，也不要增加 v0.30.2 没有的产品功能。
