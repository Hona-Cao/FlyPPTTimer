# FlyPPTTimer V1 — 新会话交接

## 产品目标

V1 是对 FlyPPTTimer v0.30.2 的 Rust + Slint 重构。目标不是继续扩张功能，而是把当前软件做稳定、做漂亮、做小、做快。

用户最终要求：

- 当前全部既有功能稳定运行；
- 不因为重构削减用户需要的功能；
- 当前整体排版和体验方向基本满意，不做无目的的大规模 UI 重做；
- 最终界面美观、配合舒服、风格统一和谐；
- 后续优先消灭真实 Bug、行为不一致和发布阻断。

## 当前工作分支

`codex/v1-06-manual-test`

新会话必须先 fetch / pull 最新 HEAD，不要依赖旧聊天中的提交号。

## 必读顺序

以根目录 `AGENTS.md` 为准。

特别注意 `docs/v1/APPROVED_PRODUCT_DEVIATIONS.md`：该文件记录用户在 v0.30.2 基线之后明确批准的产品变化；与旧 `V1_BASELINE_CHECKLIST.md` 冲突时，以批准变化文件为准。

当前已记录两项重要覆盖：

1. Remote 设置不再暴露“使用随机端口”，改为固定端口优先、不可用时自动切换并保存；
2. PC Remote“演示文稿”页只做文件规则管理，不恢复 PPT/WPS 放映控制按钮；手机 / 浏览器 Web Remote 仍保留完整演示控制。

## 当前实现状态

主体功能已经完成到候选版本阶段：

- Rust + Slint 单程序主体；
- 倒计时 / 正计时 / Pause / Resume / Restart / 超时；
- 六页设置窗口；
- 文件规则与批量设置；
- 中英文 UI；
- 提醒、闪烁、自定义声音、TTS、系统静音；
- F3/F4/F5 及 v0.30.2 内部快捷键；
- 托盘与 Timer 右键菜单；
- PowerPoint / WPS COM 控制与自动计时；
- 手机 / 浏览器 Remote HTTP 与原协议；
- PC Remote 连接页与规则页；
- 多显示器、大屏、九宫格定位；
- 更新模块；
- Portable / Inno Setup Installer；
- v0.30.2 配置读取与 V1 保存。

## 已通过 ChatGPT 源码审核的近期修复

已通过：

- F3：Running→Pause、Paused→Resume、Stopped/Finished→Start；
- Settings baseline/draft 字段合并；
- PC Remote 端口编辑、多选、批量与 editor 一致性；
- Release 版本号从 Cargo package version 获取；
- 配置导入 / 恢复默认立即应用并保持运行态、磁盘、draft/baseline 一致；
- rules 按规范化完整路径逐规则、逐字段合并；
- Remote start/apply 保证非空 token，空 token 请求拒绝；
- `rust-toolchain.toml` 固定 Rust 1.92.0；
- RC-1 多放映命令不再任意控制 `SlideShowWindows.Item(1)`，改为优先根据 `ActivePresentation.FullName` 匹配目标放映窗口；单窗口可 fallback，多窗口目标不明时命令安全失败。

RC-1 提交：`988358cef27c93b696fb65915fc7e86ecd7c0fe8`。

Codex 报告 RC-1：51 passed、0 failed、1 ignored；Release build 与 `scripts/build-release.ps1` 通过，版本仍为 1.13.0；已生成 `docs/v1/RC_MANUAL_TEST.md` 和 5 张 review PNG。真实 PowerPoint/WPS、手机、声音、混合 DPI、安装升级仍未被自动结果替代。

## 当前唯一实现任务：RC-1.1

读取最新：

`docs/v1/CODEX_TASK.md`

ChatGPT 在 RC-1 复审中确认一个与目标窗口修复直接相关的 P1：

> 当存在多个 SlideShowWindows，但 `ActivePresentation` 不属于任何正在放映的文稿时，目标选择器会返回错误；当前 `Session::read_state()` 的错误分支随后构造默认状态，使 `slide_show_running=false`。这把“有放映但目标不明确”错误表示成“没有放映”，可能让自动计时误触发离开放映 Stop / Reset。

v0.30.2 在同样情形下会保留 `IsSlideShowRunning=true`，同时报告“未能按目标文稿匹配放映窗口”。

RC-1.1 只修这一点：

- 状态层把“是否存在放映”与“是否能确定目标窗口”分开；
- `SlideShowWindows.Count > 0` 时必须保留 `slide_show_running=true`；
- 多窗口目标不明时记录错误、不虚构当前文稿/页码、不误控第一窗口；
- 命令路径继续安全失败；
- 不修改 Timer、Remote、UI、DPI、配置、发布脚本或其他模块。

本轮通过后默认进入最终 RC 人工验收，不再主动扩大代码整改范围。

## 新电脑环境

当前电脑已确认：Rust 1.92.0、Visual Studio/MSVC、Windows SDK、PowerPoint、WPS、双屏、Inno Setup、ffmpeg 均存在。手机真实局域网和混合 DPI 体验仍需要真实设备确认。

不要因为环境变化升级 Slint 或 Cargo 依赖。

## 当前开发原则

- 不继续增加 V1 新功能；
- 不做 V5 或再次重写；
- 不为了架构纯洁重构稳定模块；
- P0/P1 才允许在 RC 阶段继续改代码；
- 没有稳定复现或明确源码证据，不修改；
- 不建立庞大的 GUI 自动化/设备矩阵；
- 不创建 Release / Tag，除非用户明确要求。

## 工作方式

ChatGPT 负责读取 GitHub 最新成果、做源码/行为审查、直接更新 `CODEX_TASK.md`，并决定何时停止继续改代码。

Codex 负责 pull 最新 review 分支、严格执行最新 `CODEX_TASK.md`、最小实现与定向测试、更新 `CODEX_RESULT.md`、commit + push 后停止。

用户尽量只承担最终真实体验验收，而不是反复手工 debug。
