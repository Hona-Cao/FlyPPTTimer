# FlyPPTTimer V1 — 新会话交接

## 产品目标

V1 是对 FlyPPTTimer v0.30.2 的 Rust + Slint 重构。当前已经进入候选版冻结阶段，不再扩展功能。

用户最终要求：

- 当前全部既有功能稳定运行；
- 不因为重构削减需要的功能；
- 当前整体排版和体验方向基本满意，不做无目的的大规模 UI 重做；
- 最终界面美观、配合舒服、风格统一和谐；
- 稳定性优先于架构纯洁和新增功能；
- 用户尽量只承担最终真实体验确认，不承担反复 Debug。

## 当前工作分支

`codex/v1-06-manual-test`

新会话必须 fetch / pull 最新 HEAD，不要依赖旧聊天里的 commit。

## 必读顺序

以根目录 `AGENTS.md` 为准。

特别注意：

`docs/v1/APPROVED_PRODUCT_DEVIATIONS.md`

其中记录用户在 v0.30.2 基线之后明确批准的产品调整。与旧 `V1_BASELINE_CHECKLIST.md` 冲突时，以该文件为准。

当前重要覆盖：

1. Remote 设置不再暴露“使用随机端口”，固定端口优先，不可用时自动切换并保存；
2. PC Remote“演示文稿”页只做文件规则管理，不恢复 PPT/WPS 放映控制按钮；手机 / 浏览器 Web Remote 仍保留完整演示控制。

## 已通过 ChatGPT 源码审核的主要收口

截至提交：

`72b7a42e9be1598e671c6a67e40799a4b3235118`

以下修复已经通过源码复审，除真实验收重新稳定复现问题外不要重做：

- F3：Running→Pause、Paused→Resume、Stopped/Finished→Start；
- Settings baseline/draft 合并，不用完整旧草稿覆盖其他入口已经保存的配置；
- Settings / PC Remote rules 按规范化完整路径逐规则、逐字段合并；
- 配置 Import / Reset 立即同步磁盘、shared config、Timer / Remote / Display 与 Settings draft/baseline；
- PC Remote 端口输入不再被周期刷新覆盖；
- PC Remote 普通/Ctrl/Shift 选择、batch 与 editor 一致性；
- 普通规则 Save 不再误改不可见的 enabled；
- Remote start/apply 确保非空 token，空 token 请求明确拒绝；
- Release / Installer 版本统一来自 Cargo package version；
- `rust-toolchain.toml` 固定 Rust 1.92.0；
- 多放映窗口命令不再任意控制 `SlideShowWindows.Item(1)`，而是优先按 `ActivePresentation.FullName` 匹配目标；
- 多放映目标歧义时，状态仍保留 `slide_show_running=true`，不会误触发自动计时的离开放映 Stop / Reset，同时具体命令继续安全失败。

RC-1.1 Codex 报告：

- `cargo test --locked`：55 passed、0 failed、1 ignored；
- clippy / fmt 通过；
- Release build 通过；
- 版本仍为 1.13.0。

ChatGPT 已核对该修复核心实现与任务目标一致。

## 当前任务：RC-2 最终集成验收与候选版冻结

最新唯一实现指令：

`docs/v1/CODEX_TASK.md`

本轮**没有预先批准的源码修改**。

Codex 应尽可能在当前新电脑上直接运行 Release / Portable，替用户完成：

- 应用生命周期与 Timer；
- Settings / PC Remote；
- 真实 Remote HTTP 鉴权和 Timer 命令；
- 在保护用户文稿的前提下完成 PowerPoint/WPS 临时文稿实测；
- 双屏基础实测；
- 声音 / TTS 非主观部分；
- Portable 持久化；
- 安全条件允许时的 Installer 隔离验证。

只有稳定复现的 P0/P1 才允许最小源码修复；否则不要继续主动找可改之处。

本轮完成后应：

1. 更新 `CODEX_RESULT.md`；
2. 把 `RC_MANUAL_TEST.md` 压缩到用户真正还需做的 3～5 个短场景；
3. 无剩余已知 P0/P1 时创建 `RC_CANDIDATE.md`；
4. 标记“代码冻结候选版，除最终验收发现 P0/P1 外不再修改源码”；
5. commit + push；
6. 停止，等待 ChatGPT 最终审核。

## 当前实现范围

主体功能已经进入候选版阶段：

- Rust + Slint 单程序；
- 倒计时 / 正计时 / Pause / Resume / Restart / 超时；
- 六页设置；
- 文件规则与批量设置；
- 中英文；
- 提醒、闪烁、声音、TTS、系统静音；
- F3/F4/F5 及原有快捷键；
- 托盘和 Timer 右键菜单；
- PowerPoint/WPS 控制和自动计时；
- 手机 / 浏览器 Remote HTTP；
- PC Remote 连接页和规则页；
- 多显示器、大屏、九宫格定位；
- 更新模块；
- Portable / Inno Setup Installer；
- v0.30.2 配置读取兼容。

## 新电脑环境

已经确认存在：

- Rust 1.92.0；
- Visual Studio / MSVC；
- Windows SDK / rc.exe；
- PowerPoint；
- WPS；
- 双屏；
- Inno Setup；
- ffmpeg。

手机真实同局域网和主观声音/视觉体验仍可能需要用户最后确认。

不要因为环境变化升级 Slint 或 Cargo 依赖。

## RC 阶段原则

- 不增加 V1 新功能；
- 不做 V5 / 再次重写；
- 不为架构洁癖重构稳定模块；
- 不恢复用户已经批准删除的 PC Remote PPT 控制 UI；
- 没有稳定复现或明确源码证据，不改代码；
- 不建立庞大 GUI 自动化 / Office mock / 设备矩阵；
- 不增加 SHA256 artifact 流程；
- 不创建 Release / Tag，除非用户明确要求；
- 当前目标是结束 Debug 循环，而不是制造下一轮开发任务。

## 工作方式

### ChatGPT

读取 GitHub 最新成果，做源码/行为审查，直接更新 `CODEX_TASK.md`，并决定候选版是否可进入用户最终验收。

### Codex

pull 最新 review 分支，严格执行唯一 `CODEX_TASK.md`，尽量自行完成真实验证，只修稳定复现 P0/P1，更新结果、commit + push 后停止。

### 用户

只进行当前电脑无法可靠替代的最终体验确认，不再承担反复 Debug。
