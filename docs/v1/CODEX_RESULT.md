# V1 parity 回归修复结果

日期：2026-09-08
Review 分支：`codex/v1-06-manual-test`
任务基点：`5e86b4d`（本轮开始时远端最新 HEAD）
状态：本轮任务完成，提交推送后等待审核。

## 本轮范围与依据

按根目录 `AGENTS.md` 顺序读取 HANDOFF、基线清单、当前任务和上一轮结果，并对照 v0.30.2 的 `SettingsForm.AddBehaviorTab()`、设置应用逻辑、`AlertSoundStorage`、`AppCommandService`、`Localization` 与 Web 命令字段。仅执行当前 `CODEX_TASK.md` 的 parity 回归任务。

本地原目录停在 `agent/v4-foundation` 且含未跟踪文件，本轮在独立 worktree `E:/快传/计时器-review` 上处理 review 分支。

## 修改文件与恢复行为

- `src/settings.rs`
  - 计时结束区不再生成 `end.before`；提示 1/2 仍保留提前秒数。结束区恢复“到时语音播报 / 到时提示音 / 选择到时提示音 / 清除到时提示音”，闪烁项保留“计时结束”前缀；补齐对应旧版英文闪烁标签。
  - 三个提示音路径标记为只读，移除手工路径更新分支；选择成功后保存程序目录 `alert-sounds/prompt1.<ext>`、`prompt2.<ext>`、`end.<ext>`，启用 `play_sound`。支持大小写不敏感的 MP3/WAV/WMA/M4A，正常复制覆盖同 slot 同扩展名目标；源文件就是目标时沿用旧版不自复制行为。复制、目录或扩展名错误直接显示，不改写当前路径。清除仍清空路径并关闭 `play_sound`。修正音频文件选择器的 Win32 NUL 分隔过滤器。
  - 确定、应用和关闭时选择应用，统一进入现有 `apply_now`：先校验，再在全局时长变化且有规则时显示旧版中英文 Yes/No 询问。Yes 更新 draft 内全部规则时长，No 保留规则时长，然后保存并提交 applied。校验失败不保存，放弃和取消不进入提交；未引入规则副本或回滚机制。
  - 选择“主屏幕”清空旧设备名；单屏目标仅在关闭“所有屏幕同时显示”时可编辑；大屏目标仅在有扩展屏且启用大屏时可编辑。切换开关立即刷新当前设置模型。
- `ui/app-window.slint`
  - `SettingItem` 增加一个 `read-only` 属性并绑定 `LineEdit.read-only`，保留正常可读外观及控件原生选取/复制能力。
- `src/app.rs`
  - 将现有 Remote 计时命令处理集中为不依赖窗口/Office 的函数，正式入口直接调用，以便对真实命令行为做小范围测试。
  - `timer.stop` 执行 Stop + Reset 并清空提醒触发状态。
  - `timer.setDuration` 更新全局时长；`syncAllRules=true` 同步全部规则，否则按 `presentationId` 同步匹配规则；同步 Timer 并保存配置。
  - `timer.setMode` 更新全局及匹配规则模式，同步 Timer 并保存配置。
  - `timer.restart` 按命令中的规则 ID 使用时长/模式，未匹配时用全局设置，重置提醒并重新计时。规则查找保持旧版 Remote 命令语义，不按当前放映文稿替代命令 ID。
  - `state.get` 接受命令，并由既有响应路径返回当前状态。配置保存失败向既有 Remote 错误响应返回错误。
- `src/timer.rs`
  - Remote Restart 不再读取旧 Timer mode 后，移除已无生产调用的 `mode()` getter；既有测试读取 snapshot 的 mode，计时逻辑不变。
- `docs/v1/CODEX_RESULT.md`
  - 记录本轮实现、验证和手测范围。

本轮没有修改音频播放优先级、Web 静态资源/协议、窗口与 DPI 时序、更新器、发布脚本、任务文件或版本号（仍为 1.13.0）。未创建 Release、Tag 或上传构建产物。

## 自动验证

新增 4 个 Rust 回归测试，覆盖结束区结构与中英文标签、只读路径、主屏切回与控件启用条件、提示音导入/同 slot 覆盖/不支持扩展名，以及 Remote 停止重置、规则时长/模式同步、Restart 与提醒重置、`state.get`。提示音测试只检查导入目标和覆盖结果，不做源目标逐字节比对、Hash 或完整性快照。

- `cargo fmt --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：37 passed，0 failed，1 ignored（既有 PowerPoint/WPS COM 真机 smoke test）。
- `cargo build --release`：通过。产物：`E:/快传/计时器-review/target/release/FlyPPTTimer.exe`。
- `git diff --check`：通过。

## 待用户手测

1. 中英文行为页检查结束区顺序和文字；确认三个路径可查看/复制但不能键入修改。
2. 选择实际 MP3/WAV/WMA/M4A 文件、替换同一 slot、清除并应用；确认导入路径和声音播放，确认自定义声音仍优先于语音。
3. 已有多个文件规则时修改全局时长，分别测试“应用/确定”后的 Yes、No，以及关闭时的应用/放弃/取消；输入无效时长后确认不提交。
4. 真实双屏从副屏切回主屏，切换所有屏幕和大屏开关，检查下拉框即时启用状态与应用后的显示位置。
5. 手机 Remote 选择文稿后修改时长/模式、同步全部规则、停止并重置、按文稿重新计时及下一轮提醒。

本轮未运行 GUI 自动化，也未将纯逻辑测试表述为完成真实桌面、声音或手机验收。完成推送后停止，等待审核。
