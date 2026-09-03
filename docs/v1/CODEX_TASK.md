# FlyPPTTimer V1 — 当前 Codex 任务

状态：**待执行**  
基线：`docs/v1/V1_BASELINE_CHECKLIST.md`  
当前分支：`codex/v1-06-manual-test`

本轮仍然只处理计时窗口。上一轮 Style 修复后用户仍然看到原生窗口框架/图标，说明继续猜 `GWL_STYLE` 已经没有意义。改用最小复现和逐项隔离的方法定位。

## 最终目标

普通计时窗口必须只有：

- 时间文字
- 配置的纯色背景

并保持 v0.30.2 已有能力：无标题栏、无程序图标、无最小化/最大化/关闭按钮、不占任务栏、置顶、拖动、右键菜单、圆角、透明度、穿透。

## 1. 先找回“最小干净窗口”

不要先改现有 Style 组合。

在本地临时做一个最小版本，只保留当前 `AppWindow` 的 Slint 内容和：

- `no-frame: true`
- 固定 100×35
- `08:00`
- 纯色背景

临时绕过普通 Timer 的所有原生窗口后处理：不调用 `apply_native_window()`，不改 `GWL_STYLE/GWL_EXSTYLE`，不做 layered alpha，不做 Region，不做 click-through。

启动这个最小版本，确认实际原生窗口状态。可以临时打印一次：

- `GWL_STYLE`
- `GWL_EXSTYLE`
- `GetWindowRect`
- `GetClientRect`
- Slint physical size

这些诊断只用于本轮定位，最终提交前删除。

如果纯 Slint 最小窗口本身已经出现标题栏/图标，则停止修改 Win32 Style，优先检查 Slint/winit 的窗口创建和 `no-frame` 是否真正生效。

如果纯 Slint 最小窗口是干净的，则说明问题来自后续原生修改，继续下一步。

## 2. 不再直接修改 `GWL_STYLE`，优先让 Slint/winit 管窗口装饰

Slint 1.17.1 当前使用 winit backend。Slint/winit 自己会根据 `no-frame` 管理 decorations，因此不要再让 Win32 `GWL_STYLE` 和 winit 同时管理同一个窗口框架。

检查当前 Slint 1.17.1 的 `slint::winit_030::WinitWindowAccessor` 是否可直接使用。若可用，优先通过实际 winit window 完成 winit 已支持的 Windows 行为，例如：

- `set_skip_taskbar(true)`
- `set_undecorated_shadow(false)`

并继续让 Slint 的 `no-frame: true` / `always-on-top` 管装饰和置顶。

只在 winit/Slint 没有对应能力时，才对 `GWL_EXSTYLE` 做最小 Win32 补充。

普通 Timer 最终不要再写 `GWL_STYLE`，也不要再用 `SWP_FRAMECHANGED` 去反复重算非客户区，除非隔离结果明确证明这是唯一必要方式；若出现这种情况先在结果中说明，不要继续猜补丁。

## 3. 一项项加回功能，找到第一项破坏窗口的操作

在“最小干净窗口”基础上，本地逐项加回，每次只增加一类能力：

1. 隐藏任务栏 / undecorated shadow
2. NoActivate（如果基线确实需要）
3. 鼠标穿透
4. 圆角 Region
5. 背景透明度 / layered alpha
6. 多屏窗口创建和当前位置逻辑

每一步只观察两件事：

- 原生 Style / ClientRect 是否发生意外变化
- 窗口是否仍然只有时间和纯色背景

找到第一项导致异常的操作后，只修这一项，不继续叠加其它猜测。

如果 Codex 当前无法目视桌面，也仍然可以通过上面的原生 Style、WindowRect/ClientRect 前后变化完成定位；最终视觉由用户手工确认。

不要为此建立自动化测试程序、截图系统或长期诊断入口。临时打印和临时注释在最终提交前删除。

## 4. 利用 Git 历史做回归对比

对比至少这两个状态：

- `codex/v1-05-window-audit`：用户当时能看到正常的 `08:00` 外观，但点击后偶发遮罩/残影
- 当前 `codex/v1-06-manual-test`：修复后出现原生框架/图标

只比较计时窗口创建、显示和原生样式相关改动，找出从“初始外观正常”变成“启动即异常”的最小差异。

不要回滚其它已经完成的 Timer、Remote、PPT/WPS、多屏功能。

## 5. 本轮产出

本轮不推进任何其它功能，也不顺延版本号，除非确实需要重新标识本地候选。

最终代码应满足：

- 正常 Release 构建
- 不保留临时诊断代码
- 普通 Timer 的窗口装饰由一个明确的层负责，不再由 Slint/winit 和 Win32 两套逻辑互相覆盖
- 只保留真正需要的原生补充能力

运行现有：

- `cargo fmt --check`
- `cargo clippy --all-targets -- -D warnings`
- `cargo test`
- `cargo build --release`

完成后更新 `docs/v1/CODEX_RESULT.md`，重点写清：

1. 纯 Slint 最小窗口的实际 Style / ClientRect 结果
2. 第一项导致窗口异常的操作是什么
3. 最终选择由 Slint/winit 还是 Win32 管理窗口装饰，以及原因
4. 最终实际修改文件
5. Release EXE 本地路径和大小
6. 最终 commit SHA

提交并推送当前 review 分支后停止，等待用户手工测试和 ChatGPT 审核。不要创建 Release 或 Tag。
