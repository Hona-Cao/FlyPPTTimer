# FlyPPTTimer V1 — 当前 Codex 任务

状态：**待执行**  
基线：`docs/v1/V1_BASELINE_CHECKLIST.md`  
当前审核代码起点：`codex/v1-05-window-audit`  
目标：修复当前窗口问题、补齐明确基线差异，并产出一个供用户手工测试的下一版候选。

## 1. 先修复计时窗口点击后遮罩/残影 Bug

ChatGPT 已审查当前审核分支。异常截图在 150% DPI 下顶部出现约 34px 的透明/空白区域，与 Windows 非客户区高度吻合。当前 `AppWindow` 已由 Slint 使用 `no-frame: true`，但 `src/window.rs::apply_native_window()` 又通过 Win32 修改 `GWL_STYLE` 并使用 `SWP_FRAMECHANGED` 强制重新计算框架，极可能造成 Slint/winit software surface 与 Win32 client rect 失配。

只做下面的最小修复：

1. 普通 Timer 的 `apply_native_window()` 删除全部 `GWL_STYLE` 修改；不再手工删除 `WS_CAPTION/WS_THICKFRAME/...`，不再添加 `WS_POPUP`。无框由 Slint `no-frame: true` 负责。
2. 普通 Timer 的扩展样式保留现有值并加入 `WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`，移除 `WS_EX_APPWINDOW`，继续按配置切换 `WS_EX_TRANSPARENT`。
3. 普通 Timer 的 `SetWindowPos()` 删除 `SWP_FRAMECHANGED`，只保留现有 TopMost 与 `SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE`。
4. `configure_timer_window()` 保留首次 `apply_native_window()`；80ms 延迟回调中删除第二次 `apply_native_window()`，只做 `refresh_shape()`。
5. 暂时不要改 `WS_EX_LAYERED`、renderer、透明度实现、圆角算法或 Timer。

完成后只做一次最直接的本机运行确认：窗口能启动、能点击、能拖动、右键菜单正常。不要为这个 Bug 编写新的验证工具或测试代码。

如果仍能稳定复现同一 Bug，停止继续加补丁，在结果报告中说明现象和已完成修改，留给 ChatGPT 下一轮审查。

## 2. 补齐当前仍未闭环的基线项目

逐项以 v0.30.2 实际源码确认，只补真实差异。

- **文字过宽行为**：第二阶段曾报告尚未迁移。检查 v0.30.2 `TimerOverlayForm` 的真实行为并等价恢复，包括原有提示文字和触发条件。
- **Remote operation/busy 行为**：检查旧 Web/Remote PC UI 是否实际依赖或显示；只恢复旧版真实使用的部分。
- **更新 / Portable / Installer**：当前已有 `src/updater.rs`、安装脚本和打包脚本。核对 v0.30.2 的用户可见更新流程、安装版/便携版路径和文字，修正真实差异，并准备本地候选产物。

同时快速对照 `V1_BASELINE_CHECKLIST.md` 与 v0.30.2，确认设置页、用户可见文字、选项、默认值、托盘菜单、Timer 右键菜单和 Remote Web 没有明显漂移。发现真实差异就修正，没有差异就继续。

## 3. 产出手工测试候选

完成修正后：

1. 运行现有的 `cargo fmt --check`、`cargo clippy --all-targets -- -D warnings`、`cargo test`、`cargo build --release`。
2. 生成用户可直接手工测试的 Release EXE；如果现有 Portable/Installer 打包脚本可以正常工作，也生成对应本地候选产物。
3. 不创建 GitHub Release、Tag 或正式发布页。
4. 不新增专门用于验收、截图、模拟真实设备或自动操作 GUI 的代码。真实场景由用户手工测试。

在 `CODEX_RESULT.md` 中给用户列一份简短手工测试清单即可，重点包括：

- Timer 窗口点击后是否还出现遮罩/残影；拖动、右键、圆角、透明度。
- 提醒、声音/TTS、闪烁、快捷键。
- PowerPoint / WPS 实际放映和计时联动。
- 手机 Remote。
- 多显示器和大屏。
- Portable / Installer / 更新检测。

Codex 不需要代替用户完成这些真实场景测试。

## 4. GitHub 联动要求

1. 从当前审核分支继续开发，完成后推送到 review 分支，优先使用 `codex/v1-06-manual-test`。
2. 提交并推送本轮所有源码、配置、脚本和文档修改。
3. 不提交 `target/`、EXE、ZIP、Installer 二进制或其他大型构建产物。
4. 更新 `docs/v1/CODEX_RESULT.md`，至少包含：
   - review 分支名
   - 最终 commit SHA
   - 主要修改文件
   - 窗口 Bug 修复方式
   - 未闭环项目的处理结果
   - 编译、Clippy、现有测试结果
   - Release EXE 路径与大小
   - Portable/Installer 本地路径与大小（若生成）
   - 给用户的简短手工测试清单
   - 仍存在的已知差异或阻塞问题
5. 推送完成后停止，等待 ChatGPT 审核和用户手工测试反馈。

保持当前简单结构，只增加完成 v0.30.2 等价行为和修复实际问题所需的代码。不要修改 v0.30.2 或 `agent/v4-foundation`，也不要增加 v0.30.2 没有的产品功能。
