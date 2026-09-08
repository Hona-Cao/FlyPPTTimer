# V1 Remote parity 定向整改结果

日期：2026-09-08

分支：`codex/v1-06-manual-test`

本轮基于 `70fef2e` 的最新任务，仅处理本轮审核指出的问题。

## 整改内容

- `src/app.rs`：`timer.setDuration` 优先取正数 `durationMs`，否则使用现有文本时长解析；毫秒按 C# `Math.Round` 的中点取偶规则转整秒，限制到 1～86399 秒，并统一保存为 `HH:mm:ss`。保留全局、Timer 和文件规则同步。
- `src/app.rs`：`timer.setMode` 仅将 `countup` 和 `正计时` 解释为 CountUp，其余值（包括缺失值）直接使用 Countdown；保留原有规则同步和保存逻辑。
- `src/app.rs`：Restart 未匹配规则返回“已按全局时长重新计时”，匹配规则返回“已按 {FileName} 的规则时长重新计时”。Remote 规则匹配排除空白 ID 和空白文件路径，沿用旧版 FindRule 语义。
- `src/config.rs`：删除保存前对刚序列化 JSON 的重复反序列化；临时文件与替换写入保持原状。现有 `parse_duration` 改为 crate 内可见，供 Remote 直接复用，没有新增解析器。
- 在现有 Remote parity 测试内补入双参数优先级、一个负毫秒代表值、超过 24 小时的上限、中点取偶舍入、未知模式，以及命中/未命中规则的消息断言。

## 四项验证

- `cargo fmt --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：37 passed，0 failed，1 ignored（既有 Office COM 真机测试）。
- `cargo build --release`：通过。

## 仍需用户手测

- 在实际手机 Remote 上确认设置时长/模式后的 Timer、规则与保存结果。
- 选择受管演示文稿重新计时，确认返回文稿名称；无匹配规则时确认显示全局时长提示。
- 实际桌面、声音、PowerPoint/WPS 与多屏体验仍需用户验收，本轮纯逻辑测试不替代这些手测。
