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

### UX13 后稳定性

已通过：

- F3：Running→Pause、Paused→Resume、Stopped/Finished→Start；
- Settings baseline/draft 字段合并，不再整份旧草稿覆盖外部配置；
- PC Remote 周期刷新不再覆盖正在输入的端口；
- PC Remote 普通/Ctrl/Shift 多选、当前 editor、批量保存一致性；
- 普通规则保存不再意外统一 enabled；
- Release 版本号从 Cargo package version 获取。

### RC 配置 / 鉴权收口

提交 `829c8c79b430d92bbd1e7e37fa6d301d07ad40cb` 已完成并通过 ChatGPT 源码复审：

- 配置导入 / 恢复默认走立即应用路径，运行态、shared config、磁盘、Settings draft/baseline 同步；
- Settings 与 PC Remote 对 rules 按规范化完整路径进行逐规则、逐字段合并；
- Remote start/apply 保证非空 token，空 token 请求明确拒绝；
- 增加 `rust-toolchain.toml` 固定 Rust 1.92.0，不升级 Cargo 依赖。

Codex 报告该轮：48 passed、0 failed、1 ignored，Release build 通过。ChatGPT 已核对主要源码实现与报告一致；真实 Office、手机 Remote、多屏、声音和视觉仍不能仅凭自动测试宣告验收。

## 当前唯一实现任务：RC-1

读取最新：

`docs/v1/CODEX_TASK.md`

ChatGPT 当前完整 RC 静态抽查只确认一个需要继续修改的明确代码差异：

> Rust `src/presentation.rs` 在存在多个 SlideShowWindow 时直接使用 `SlideShowWindows.Item(1)`；而 v0.30.2 会读取活动文稿路径，并遍历放映窗口按 `Presentation.FullName` 找到目标窗口。

因此 RC-1 只预先批准：

- 多放映窗口按目标文稿路径选择的最小 parity 修复；
- 修复后做有限 RC 静态复核；
- 尽量利用当前新电脑完成真实 Release 冒烟；
- 生成少量不含私人信息的应用窗口截图供 ChatGPT 继续审查整体观感；
- 生成一份很短的 `RC_MANUAL_TEST.md`，把最终人工验收限制在真正需要真实设备/Office/视觉判断的场景。

不要重新打开已经通过的 F3、配置 merge、Remote token、端口、规则 UI、发布版本等修复。

## 新电脑环境

当前电脑上一轮已经确认：

- 系统原 Rust 1.86 过低；项目现用 Rust 1.92.0；
- 已加入 `rust-toolchain.toml`，后续仓库内普通 Cargo 命令应自动选择 1.92.0；
- Visual Studio / MSVC 可用；
- Windows SDK / rc.exe 可用；
- PowerPoint 已安装；
- WPS 已安装；
- 检测到双屏；
- Inno Setup 6 可用；
- ffmpeg 可用；
- 手机同局域网真实连接仍需要实际设备确认。

不要因为环境变化升级 Slint 或 Cargo 依赖。

## 当前开发原则

- 不继续增加 V1 新功能；
- 不做 V5 或再次重写；
- 不为了架构纯洁重构稳定模块；
- P0/P1 才允许在 RC 阶段继续改代码；
- 没有稳定复现或明确源码证据，不修改；
- 真实视觉、PPT/WPS、手机 Remote、声音、多屏问题优先以真实使用验证；
- 不建立庞大的 GUI 自动化/设备矩阵；
- 不创建 SHA Artifact 流程；
- 不创建 Release / Tag，除非用户明确要求。

## 工作方式

### ChatGPT

负责：

- 从 GitHub 读取 Codex 最新成果；
- 做源码/行为审查；
- 区分确认 Bug、真实风险和非必要重构；
- 直接更新 `CODEX_TASK.md`；
- 接近发布时审核少量真实界面证据；
- 决定何时停止继续改代码并进入人工验收。

### Codex

负责：

1. pull review 分支最新 HEAD；
2. 按 `AGENTS.md` 顺序读取资料；
3. 只执行最新 `CODEX_TASK.md`；
4. 最小实现、最小定向测试；
5. 完成后更新 `CODEX_RESULT.md`；
6. commit + push review 分支；
7. 停止编码等待 ChatGPT 再审核。

### 用户

尽量只承担最终真实体验验收，而不是反复手工 debug。

最终人工验收重点是那些难以完全自动替代的场景：PowerPoint/WPS、手机局域网、声音/TTS、双屏/跨 DPI、整体界面观感和安装/便携实际使用。
