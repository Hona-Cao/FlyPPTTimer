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

## 2026-09-10 最新审核状态

Codex 已完成上一轮 UX13 稳定性整改，源码提交为：

`8e8b9cd972b3aef3f32cda5ce903274bd678986d`

ChatGPT 已复审并接受以下修复，不要重做：

- F3 Start/Pause：Paused 状态恢复为 `resume()`，不再从头重新计时；
- PC Remote 端口输入不再被周期刷新覆盖；
- PC Remote 规则多选、batch、单项 editor 的本轮一致性修复；
- Settings 对一般配置对象改用 baseline→draft 差异合并到最新 applied；
- 发布脚本版本改为从 Cargo package version 读取，Installer 版本同步为 1.13.0。

上一轮环境预检也已完成：新电脑原 Rust 1.86 无法构建 Slint 1.17.1，安装 Rust 1.92.0 后 `cargo check --locked`、测试、Release build 和本地 Installer 打包均可执行；PowerPoint、WPS、双屏、Inno Setup、ffmpeg 均检测到，真实 Office / 手机 / 混合 DPI 体验仍待后续手测。

## 当前仍需修复的 RC 阻断项

ChatGPT 在 `8e8b9cd` 上继续复审后确认：

1. 当前配置导入会直接写真实配置文件，但只修改 Settings draft，运行中的 shared applied / Timer / Remote / Display 等没有同步，可能出现磁盘和运行态不一致；v0.30.2 的导入 / 恢复默认属于立即 Apply 的配置操作，应恢复一致语义。
2. Remote token 默认可为空，`RemoteServer::start()` / `apply_enabled()` 当前直接复制该值，而空 token 与缺省请求可比较为相等；恢复默认或导入空 token 配置后存在无鉴权访问风险。Remote enabled/start/apply 时必须确保非空 token，空 token 请求必须拒绝。
3. Settings 的通用 JSON merge 对 `rules` 数组仍是整表替换。若 Settings 修改规则 A，同时 PC Remote 修改规则 B 或新增 C，Settings 后续 Apply 仍可能丢掉外部 B/C 修改。Rules 需要按完整路径身份做轻量字段级合并。
4. 新电脑环境已经证明项目实际需要 Rust 1.92.0；本轮增加最小 `rust-toolchain.toml` 固定项目工具链，避免下一台电脑再次由系统默认 Rust 版本触发同一构建失败，不升级任何 Cargo 依赖。

完整边界、测试场景和禁止事项已写入最新 `docs/v1/CODEX_TASK.md`。**CODEX_TASK 是当前唯一实现指令。**

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

## 已稳定方向

### Timer 与基础交互

Timer 使用单调时间，F3 暂停/恢复路径已修正。不要再为已通过的状态转换进行无目的重构。

### Settings / Remote 界面

当前视觉方向、配色、规则列表层次、说明弹窗、批量设置等整体可以保留。后续只修真实交互 Bug，不重新设计整套 UI。

### 混合 DPI / 跨屏

设置窗口、Remote 窗口和 Timer 已经过多轮定向修正。此前在 150% 与 125% 双屏组合上做过基本跨屏验证；新电脑当前检测到双屏，但实际缩放和混合 DPI 体验仍需真机复核。

## 下一阶段重点

不要再按“大阶段”增加新功能。后续重点是：

1. 执行最新 `CODEX_TASK.md` 中的配置生命周期 / Remote 鉴权 / Rules merge 收口；
2. 用户手工测试发现的真实 Bug；
3. 与 v0.30.2 的功能、选项、文字、默认值和行为差异清零；
4. PowerPoint / WPS、手机 Remote、声音/TTS、多屏的真实使用验收；
5. 最终 RC 发布收口。

## 当前仍需后续复核的发布项

- 仓库旧 `package_release.ps1` 属于旧发布路径，最终 V1 不应长期同时维护两套发布流程；
- `src/capture.rs` 和 `--capture-settings` / `--capture-windows` 是开发期辅助入口，正式 V1 前评估是否保留；
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
2. 严格执行最新 `CODEX_TASK.md`；
3. 不扩大为 UI 重做、架构重构或新功能；
4. 完成后更新 `docs/v1/CODEX_RESULT.md`；
5. commit 并 push 到 review 分支；
6. 停止继续编码，等待 ChatGPT 再审核。

真实视觉、PPT/WPS、手机 Remote、声音和多屏体验优先由用户手工测试；不要为了这些体验另建复杂测试系统。

## 产品方向

用户最看重：

**美观、小巧、稳定、快速。**

当前实现已经进入稳定性收口阶段。后续 Bug 修复和视觉优化优先解决真实使用问题，保持实现直接，不为了工程形式增加与产品体验无关的复杂度。
