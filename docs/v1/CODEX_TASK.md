# FlyPPTTimer V1 — 当前 Codex 任务

状态：**执行本轮 V1 parity 回归修复**  
当前分支：`codex/v1-06-manual-test`

## 任务原则

先读根目录 `AGENTS.md`、`docs/v1/HANDOFF.md`、`docs/v1/V1_BASELINE_CHECKLIST.md` 和最新 `docs/v1/CODEX_RESULT.md`，然后以当前 review 分支源码为基础修改。不要根据旧 `CODEX_RESULT.md` 猜现状。

本轮只修复下面已经由 ChatGPT 直接对照 **v0.30.2 实际 C# 源码**确认的 parity 回归。不要顺手改 Remote Web 资源、DPI/窗口时序、更新器、发布脚本或做新的 UI 美化；这些留给后续手测/收口轮次。

## 1. 修正“行为设置”里计时结束区的结构和文案

当前 `src/settings.rs` 复用了通用 `prompt_rows()`，导致 `计时结束`区出现 v0.30.2 不存在的“距离预设时间还剩（秒）”输入框，而且生成了错误的“计时结束语音播报 / 计时结束提示音 / 选择计时结束提示音 / 清除计时结束提示音”文案。

按 v0.30.2 `SettingsForm.AddBehaviorTab()` 恢复：

- `计时结束`（启用）
- `到时语音播报`
- `到时提示音`
- `选择到时提示音`
- `清除到时提示音`
- `计时结束闪烁样式`
- `计时结束闪现时长（毫秒）`
- `计时结束隐藏时长（毫秒）`
- `计时结束闪烁持续（秒）`

要求：

- **计时结束区不得出现 trigger-before-end 输入框。**
- 提示 1 / 提示 2 仍保留各自的“距离预设时间还剩（秒）”。
- 中英文都要对应正确；不要增加 v0.30.2 没有的新选项。

## 2. 恢复提示音选择的 v0.30.2 语义

v0.30.2 的 `提示1提示音 / 提示2提示音 / 到时提示音` 是**只读路径显示**，用户只能通过“选择文件 / 恢复默认（清除）”改变路径；不是可自由输入的文本框。

当前 Slint `kind == 2` 会把这些路径渲染为可编辑 `LineEdit`。请增加最小必要的只读表达能力，使这 3 个路径值不能手工编辑，但仍清晰可复制/查看（如果 Slint 控件能力受限，至少必须做到不能修改值，且不要把整个行做成不可辨识的灰色占位）。

另外，v0.30.2 选择提示音后会把文件复制到程序目录下 `alert-sounds/`，按 slot 保存为 `prompt1.<ext>`、`prompt2.<ext>`、`end.<ext>`，支持 `.mp3/.wav/.wma/.m4a`，并在配置里保存导入后的路径。当前 Rust 直接保存用户原文件路径，需恢复旧版导入语义：

- 文件不存在时报错；
- 扩展名只允许上述 4 种；
- 创建 `alert-sounds` 目录；
- 同 slot 新选择覆盖旧文件；
- 配置保存导入后的目标路径；
- 清除按钮清空路径，`play_sound=false`；
- 选择成功后 `play_sound=true`。

**不要修改现有音频播放优先级。** v0.30.2 在 `Speak=true` 且存在已启用的自定义提示音时优先播放自定义提示音，不同时播语音；当前 Rust 的这一行为是正确的。

## 3. 恢复全局时长修改与文件规则同步询问

v0.30.2 在设置窗口应用时，如果：

- 全局 `默认时长`发生变化；
- 且已有一个或多个文件规则；

会弹出 Yes/No 询问，是否把新全局时长同步到全部受管演示文稿规则。Yes 同步全部规则，No 保留每条规则现有时长，然后继续应用。

当前 Rust `apply_now` 直接提交 draft，没有这一步。请按 v0.30.2 恢复，保留原有中英文语义，不要改成自动同步。

同时保证：

- `确定` 与 `应用`都走同一套逻辑；
- 用户取消/放弃设置时不能意外改写规则；
- 校验失败时不提交部分修改。

## 4. 修正单屏目标从副屏切回“主屏幕”

v0.30.2 选择 `主屏幕`时会把 `TargetScreenDeviceName` 写为空字符串。当前 `update_field("placement.target", ...)` 在值为“主屏幕”时什么都不做，可能遗留之前的副屏设备名。

修复为：

- 选 `主屏幕` => `target_screen_device_name = ""`；
- 选具体屏幕 => 保存具体 device name。

同时对照 v0.30.2 保持设置页启用关系：

- `所有屏幕同时显示=true` 时，`单屏显示屏幕`不可编辑；
- `大屏显示屏幕`只有在存在扩展屏且`启用大屏计时器=true`时可编辑。

## 5. 恢复 Remote 计时命令的 v0.30.2 语义

当前 `RemoteCommand` 仍解析 `presentationId` 与 `syncAllRules`，但 `src/app.rs::execute_remote_command()` 对部分命令没有使用它们。按 v0.30.2 `AppCommandService` 恢复：

### `timer.stop`

- v0.30.2 Remote 的 `timer.stop` 是 **Stop + Reset**，并重置提醒触发状态。
- 当前 Rust 只 Stop，需修正。

### `timer.setDuration`

- 更新全局默认时长；
- `syncAllRules=true` 时同步全部规则；
- 否则如果 `presentationId` 对应某条规则，只同步该规则；
- 同步当前 Timer duration；
- 保存配置。

### `timer.setMode`

- 更新全局模式；
- 如果 `presentationId` 对应某条规则，同时更新该规则模式；
- 同步当前 Timer mode；
- 保存配置。

### `timer.restart`

- 优先使用命令中的 `presentationId` 查找规则；
- 找到规则时按该规则时长/模式重新计时；
- 没有对应规则时按全局设置；
- Restart 必须重置上一轮提醒状态。

### `state.get`

- v0.30.2 命令白名单允许 `state.get`；保持兼容，不要返回“命令不被允许”。

不要重命名旧命令，不要修改旧 Web `index.html/app.css/app.js` 的协议字段。

## 6. 回归测试

至少补充能锁住本轮回归的 Rust 测试。优先覆盖纯逻辑，不要为了测试引入脆弱的 GUI 自动化：

- 计时结束 rows 不包含 `end.before`，且中文标签为 `到时...`；
- 提示 1/2 仍包含 before row；
- 主屏幕选择能清空旧 device name（可把字段更新逻辑抽成可测试的小函数）；
- Remote `timer.stop` 的 Stop+Reset 语义；
- Remote duration/mode 的 `presentationId` / `syncAllRules` 规则更新逻辑；
- 提示音导入路径/扩展名/覆盖行为可用临时目录测试时，补纯文件测试。

完成后运行：

```powershell
cargo fmt --check
cargo clippy --all-targets --all-features -- -D warnings
cargo test
cargo build --release
```

## 7. 交付要求

- 更新 `docs/v1/CODEX_RESULT.md`，写清：改了哪些文件、每个 parity 回归如何恢复、测试结果、仍待用户手测的项目。
- 提交并 push 到 `codex/v1-06-manual-test`。
- 不要改 `docs/v1/CODEX_TASK.md` 的任务范围，只在结果文件记录完成情况。
- 本轮不要处理 `scripts/build-release.ps1` 的版本硬编码、开发截图入口清理等最终发布项；下一轮单独收口。
