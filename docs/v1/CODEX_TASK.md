# FlyPPTTimer V1 — 当前 Codex 任务

状态：**上一轮审核未完全通过，执行定向整改**  
当前分支：`codex/v1-06-manual-test`

## 任务原则

先读根目录 `AGENTS.md`、`docs/v1/HANDOFF.md`、`docs/v1/V1_BASELINE_CHECKLIST.md`、最新 `docs/v1/CODEX_RESULT.md` 和当前源码。

上一轮关于以下内容的实现经源码审核可保留，不要重做：

- 行为设置“计时结束”区去除 `end.before`，恢复“到时...”文案；
- 三个提示音路径只读；
- 提示音导入到 `alert-sounds/`；
- 设置页修改全局时长时询问是否同步全部规则；
- 主屏幕选择清空旧 device name，以及单屏/大屏下拉启用关系；
- Remote `timer.stop` 的 Stop + Reset；
- `presentationId` / `syncAllRules` 的规则同步主体；
- `state.get` 已进入允许路径。

本轮只修下面审核确认的问题。不要顺手修改 Web 静态资源、DPI/窗口时序、音频播放优先级、更新器、发布脚本或做 UI 美化。

## 强制编码约束：禁止过度防御

继续执行以下约束：

- 不增加 SHA / Hash / checksum / 文件完整性验证；
- 不重复验证同一事实；
- 不增加无必要重试、双重读取、备用路径、静默 fallback、备份/恢复链或额外状态机；
- 测试只锁定实际用户可观察行为和必要纯逻辑，不做完整性快照；
- 不为了覆盖率制造测试矩阵；
- 不额外运行没有要求的“保险检查”。本轮交付只执行任务末尾列出的四个 Cargo 命令，不再附加 `git diff --check`、Hash、产物复核等额外 gate。

必要的 Remote 外部输入处理只做到 v0.30.2 已有语义，不自行强化协议。

## 1. 修正 `timer.setDuration` 尚未对齐的 v0.30.2 语义

当前 `src/app.rs::execute_remote_timer_command()` 仍有明确 parity 问题。

v0.30.2 `AppCommandService.ExecuteRemoteCommandCore()` 的规则是：

1. **`durationMs > 0` 时优先使用 `durationMs`。**
2. 只有 `durationMs` 不大于 0 时，才尝试文本 `duration`。
3. 最终时长按旧版 `SetDuration()` 处理为整秒，并限制到 **1 秒 ~ 23:59:59**。
4. 保存到配置的文本应是规范化 `HH:mm:ss`。

当前 Rust 的问题：

- 先取 `duration`，再取 `durationMs`，优先级反了；
- `duration_ms: i64` 被直接 `as u64`，负数会变成巨大正数；
- 没有恢复旧版 24 小时以内的上限；
- 毫秒转整秒当前直接截断，而旧版是按整秒处理后再限制范围。

整改要求：

- `durationMs` 为正数时必须覆盖/忽略同时传入的 `duration`；
- `durationMs <= 0` 时不要 cast 成 `u64`，直接进入文本 duration 分支；
- 正常化为整秒后 clamp 到 `1..=86399`；
- 配置统一保存 `HH:mm:ss`；
- Timer、全局配置、`syncAllRules`、`presentationId` 的现有同步逻辑保留；
- 不增加第二套 parser、fallback 链或复杂输入恢复机制。沿用现有 duration 解析能力即可，只把旧版明确的优先级、负数处理、整秒化和范围恢复正确。

## 2. 修正 `timer.setMode` 的旧协议兼容

v0.30.2 的实际代码是：

- `mode == "countup"` 或 `mode == "正计时"` => `CountUp`；
- **其他任何值，包括空值/未知值，均按 `Countdown`。**

当前 Rust 对未知值返回“模式无效”，这不是 v0.30.2 语义。

请恢复旧行为，不新增 mode 白名单层：

- 只有两个 CountUp 值特殊判断；
- 其余直接 Countdown；
- 保留现有全局模式、匹配规则模式、当前 Timer mode 和配置保存逻辑。

## 3. 恢复 `timer.restart` 的旧版 Remote 返回消息

v0.30.2 的 `Restart(presentationId)` 会设置可见消息：

- 未匹配规则：`已按全局时长重新计时`
- 匹配规则：`已按 {FileName} 的规则时长重新计时`

Remote 对 `timer.restart` 会把这个消息返回给网页端。

当前 Rust 无论是否命中规则都返回 `已重新计时`，丢失了旧版用户可见反馈。

请在现有 restart 逻辑上直接恢复这两个返回值，不建立额外消息状态对象。

规则匹配保持旧版 `FindRule` 语义：空 `presentationId` 不匹配；空 `file_path` 的规则不参与匹配。

## 4. 删除 `AppConfig::save()` 中明确的重复自校验

当前 `src/config.rs::AppConfig::save()`：

1. 从强类型 `AppConfig` 序列化为 JSON；
2. 随后立刻又 `serde_json::from_slice::<AppConfig>(&json)?` 反序列化一次；
3. 然后才写文件。

这一步反序列化只是对刚由同一强类型序列化器产生的数据再次验证，属于本项目明确禁止的重复防御性校验。

请删除这一行重复验证。

本轮**不要顺手重写整个配置保存机制**；临时文件/替换写入保持现状，避免扩大任务。只删除这次审核确认的无意义自校验。

## 5. 最小回归测试

只补本轮缺失语义，不建立输入矩阵。

在现有 Remote parity 测试中最小覆盖：

- 同时传 `duration="00:01:00"` 与正 `durationMs=120000` 时，最终必须使用 2 分钟；
- `durationMs <= 0` 不得发生负数转巨大正数；可用一个代表值锁住即可；
- 超过 24 小时的正 `durationMs` 最终为 `23:59:59`；
- 未知 `timer.setMode` 最终为 Countdown；
- restart 命中规则与未命中规则的返回消息各确认一次。

不要为每个边界值再复制多组测试；不要做 Hash、文件字节复核、配置快照或重复断言。

## 6. 验证与交付

只运行：

```powershell
cargo fmt --check
cargo clippy --all-targets --all-features -- -D warnings
cargo test
cargo build --release
```

然后：

- 更新 `docs/v1/CODEX_RESULT.md`，只写本轮整改内容、上述四项验证结果和仍需用户手测的事项；
- 提交并 push 到 `codex/v1-06-manual-test`；
- 不修改本任务范围；
- 不创建 Release / Tag；
- 不附加 SHA/Hash/完整性证明或额外验证步骤。
