# 当前任务：RC-2 最终集成验收与候选版冻结

当前分支：`codex/v1-06-manual-test`

审核基点：`72b7a42e9be1598e671c6a67e40799a4b3235118`

## 当前审核结论

ChatGPT 已复审 RC-1.1，并接受本轮多放映状态修复：

- `SlideShowWindows.Count > 0` 与“能否确定目标放映窗口”已经分离；
- 多窗口目标不明确时，状态仍保持 `slide_show_running = true`，不会再误触发 PresentationLifecycle 的离开放映 Stop / Reset；
- 命令路径仍安全失败，不会任意控制第一个放映窗口；
- 0 窗口、单窗口 fallback、乱序目标匹配、多窗口歧义均已有纯逻辑回归；
- Codex 报告 `cargo test --locked` 为 55 passed、0 failed、1 ignored，Release build 通过；
- 程序版本仍为 `1.13.0`。

因此从本任务开始，**没有任何预先批准的源码修改**。

本轮目标不是继续主动找代码问题，而是尽可能替用户完成最终真实 Windows 集成验收，把剩余人工工作压缩到真正无法由当前电脑可靠替代的少数场景，然后冻结一个候选版供 ChatGPT 最终审核。

---

# 1. 开始前

1. fetch / pull `codex/v1-06-manual-test` 最新 HEAD；
2. 按 `AGENTS.md` 必读顺序读取资料；
3. 确认工作区干净；
4. 确认项目继续使用 `rust-toolchain.toml` 的 Rust 1.92.0；
5. 不升级 Cargo / Slint / Windows crate 等依赖；
6. 不修改程序版本号；
7. 不创建 GitHub Release / Tag。

如果发现用户原工作区有未提交文件，不覆盖、不清理；继续使用独立 worktree。

---

# 2. 最终自动验证：只统一执行一次

先对当前源码运行：

```powershell
cargo fmt --all -- --check
cargo clippy --locked --all-targets --all-features -- -D warnings
cargo test --locked
cargo build --locked --release
```

然后运行现有：

```powershell
scripts/build-release.ps1
```

核对：

- 程序版本为 `1.13.0`；
- Portable 目录和 ZIP 名称为 1.13.0；
- Installer ProductVersion 为 1.13.0；
- Installer FileVersion 为 1.13.0.0；
- 不维护第二套新的发布脚本；
- 不增加 SHA256 artifact 流程、覆盖率门槛或新的 CI 框架。

GitHub 旧 CI 若仍与当前 Rust V1 不匹配，不要在本轮为了 CI 重构发布流程；只在结果中注明“最终证据来自本机 Rust RC 验证，旧 CI 不作为本轮验收依据”。

---

# 3. 尽可能由 Codex 完成真实 Windows 集成验收

这里的原则是：**用户尽量只做最终体验确认，不承担 Debug。**

可以使用当前电脑已有的 PowerPoint、WPS、双屏、Inno Setup、PowerShell、浏览器/HTTP 工具和现有 capture 能力。允许写一次性的 PowerShell / 临时测试脚本，但放在系统临时目录或 `target/rc-temp/`，不要提交新的测试框架。

每项必须记录：`通过 / 失败 / 无法可靠验证`，以及真实执行方式。不能把“源码看起来正确”写成“真机通过”。

## 3.1 应用生命周期与 Timer

尽可能运行当前 **Release / Portable**，验证：

- 正常启动；
- 第二实例不会产生第二套主程序；
- F3：Start → Pause → Resume；暂停期间时间不增加；
- F4 Stop/Reset；
- F5 显示/隐藏 Timer；
- Timer 右键菜单可打开；
- 托盘菜单可打开，设置 / Remote 可进入；
- 正常退出后无残留 FlyPPTTimer 进程。

如果现有自动桌面交互能力不能可靠发送全局快捷键或点击托盘，不为此建立 GUI 自动化系统，记录为最终人工项目即可。

## 3.2 Settings / PC Remote

尽可能验证：

- Settings 打开、关闭、再次打开；
- Apply 与 Cancel 基础行为；
- 使用**测试配置副本**验证 Import / Reset 后运行界面与持久配置同步；
- 不使用或覆盖用户真实配置；
- PC Remote 两页可以打开、切换、关闭、再次打开；
- “下次服务端口”输入停顿数秒不被周期刷新覆盖；
- 普通 / Ctrl / Shift 规则选择和批量设置保持一致；
- PC Remote 演示文稿页只保留规则管理，不恢复放映控制按钮；
- 固定端口不可用时现有 fallback 行为不回退。

如果需要生成测试规则，只使用 `target/rc-temp/` 中的临时文件路径，完成后不提交测试配置。

## 3.3 Remote HTTP

不需要真实手机也能先完成协议和鉴权的真实进程级验证：

- 启动当前 Release 的 Remote 服务；
- 使用 localhost 请求实际 HTTP 服务；
- 缺少 token → 必须拒绝；
- 错 token → 必须拒绝；
- 正确 token → 状态请求成功；
- 通过真实 HTTP 执行至少 Start / Pause / Resume / Reset，并确认后续 state 反映变化；
- 若可以安全取得当前 LAN 地址，可从本机使用 LAN 地址访问同一服务，证明监听地址和防火墙前的本机链路正常；
- **不要宣称这等同于真实手机跨设备连接通过**。

不要为了 Remote 验收增加新协议、WebSocket 或测试服务器。

## 3.4 PowerPoint / WPS

目标是尽可能替用户完成 Office 真机验证，但必须保护用户文件。

安全规则：

1. 测试开始前先确认 PowerPoint/WPS 中是否已经打开用户文稿；
2. 如果存在无法明确证明是本轮临时创建的用户文稿，不关闭、不保存、不修改、不强退；对应测试记为“无法安全自动验证”；
3. 测试文稿必须创建在 `target/rc-temp/office/` 或系统临时目录，文件名清楚标明 RC TEMP；
4. 可以使用一次性 PowerShell/COM 创建最小测试 PPTX，但不要把临时 PPTX 提交仓库；
5. 只关闭/删除本轮自己创建且能明确识别的临时测试文稿。

在安全条件满足时，尽可能真实验证 PowerPoint 与 WPS：

- 打开临时文稿；
- 从头放映；
- 如可行，从当前页放映；
- Previous / Next；
- Goto；
- Black / White / Restore；
- EndShow；
- 自动计时开始 / 结束行为；
- managed ownership 不误关用户已有文稿。

### 多放映窗口重点

如果能安全构造两个临时文稿同时存在放映窗口，额外验证：

- 活动文稿 A 时命令作用于 A；
- 窗口顺序不是 `[A, B]` 的情况下仍按路径匹配；
- 如果人为切到一个不属于正在放映窗口的活动文稿，状态仍报告“存在放映”，而控制命令明确失败，不停止/重置正在自动计时的 Timer。

如果 PowerPoint 或 WPS 的实际 COM / UI 条件不适合安全自动构造多窗口，保留给最终人工验收，不写复杂 Office 自动化框架。

## 3.5 双屏 / DPI

当前电脑已检测到双屏。尽可能真实验证：

- Timer、Settings、PC Remote 打开时没有明显跑出可见屏幕；
- 大屏计时只在启用的扩展显示器出现；
- 窗口关闭/重新打开后的尺寸和位置合理；
- 如两块屏幕实际 DPI 不同且现有桌面工具能可靠拖动，做一次跨屏拖动观察。

如果当前两屏缩放相同，不能宣称“混合 DPI 已通过”；记录实际缩放条件即可。

不要为了这一步继续改 DPI 实现，除非真实复现 P0/P1（崩溃、窗口不可用或严重跑出屏幕）。

## 3.6 声音 / TTS

自动化无法替代“人耳听到正确声音”。可以尽可能验证：

- 测试提示调用不崩溃；
- 临时 WAV/MP3 文件路径能被当前播放链打开；
- TTS 调用不返回明显错误；
- 日志没有播放线程崩溃。

不要修改用户系统音量或永久静音状态来做测试。如果不能可靠确认实际听感，保留一个很短的人工“听一下”项目。

## 3.7 Portable / Installer

Portable：

- 从本轮生成的 Portable 目录直接运行；
- 确认配置文件位于 Portable 程序目录；
- 修改一个无风险测试设置、退出重启，确认持久化。

Installer：

- 首先检查当前电脑是否已有用户正式安装版和用户配置；
- 不覆盖、不卸载用户现有正式安装；
- 如果 Inno Setup 支持安全重定向到临时目录，并且不会触碰现有用户配置，可在临时目录做安装 / 启动 / 卸载验证；
- 如果无法保证隔离，只验证 Installer 能生成、版本资源正确，并把“真实安装/升级/卸载”保留给最终人工验收。

---

# 4. 本轮发现问题时的处理规则

本轮**没有预设源码修改**。

只有实际执行中出现下面情况，才允许直接修复：

- 稳定可复现崩溃 / 卡死；
- Timer 计时或状态错误；
- 配置丢失 / 覆盖；
- Remote 鉴权失败；
- Office 控制错误文稿或有用户文稿风险；
- 核心窗口无法正常打开 / 关闭；
- Portable / Installer 无法正常启动；
- 其他明确 P0/P1。

若发现：

1. 先记录最小复现；
2. 增加最小定向回归（能自动测试时）；
3. 做最小修复；
4. 不顺手重构相邻模块；
5. 重新运行受影响测试以及最后统一验证；
6. 在 `CODEX_RESULT.md` 顶部明确写“RC-2 实测发现的问题、复现、修复、验证”。

以下一律不在本轮修改：

- UI 风格偏好微调；
- 代码洁癖；
- 大型重构；
- 新功能；
- 新测试框架；
- 理论性极端场景；
- 未稳定复现的问题。

---

# 5. 压缩用户最终人工验收

更新：

`docs/v1/RC_MANUAL_TEST.md`

不要继续保留一个“凡事都让用户再测一遍”的 10 项矩阵。

根据本轮**真正执行并可靠通过**的结果，把已经验证的项目从用户任务中移出，或者标注为“Codex RC-2 已实测通过，无需用户重复”。

最终只保留当前电脑无法可靠替代的项目，例如可能包括：

- 手机在同一局域网真实访问 Remote；
- 用户实际听一遍提示音 / TTS；
- 用户自己的 PowerPoint/WPS 工作流体验（仅当 Codex 无法安全真机完成）；
- 真正的 125% / 150% 混合 DPI 跨屏视觉（仅当当前机器条件不具备）；
- 实际安装升级（仅当无法安全隔离安装）。

目标是把用户最终操作控制在 **3～5 个短场景以内**，而不是让用户继续做开发测试员。

---

# 6. 候选版冻结记录

如果本轮没有剩余已知 P0/P1，新增：

`docs/v1/RC_CANDIDATE.md`

内容保持简短，记录：

- 候选源码 commit；
- 程序版本 `1.13.0`；
- Portable / ZIP / Installer 文件名与本地路径；
- cargo fmt / clippy / test / release build 结果；
- 本轮真实 Windows 集成项目中哪些通过；
- 哪些仍需用户 3～5 项最终体验确认；
- 明确状态：`代码冻结候选版，除最终验收发现 P0/P1 外不再修改源码`。

不要加入文件哈希工作流。

如果本轮发现并修复 P0/P1，则 RC_CANDIDATE 指向修复后的最终提交；最终统一验证必须在修复后重新通过。

---

# 7. 完成方式

完成后：

1. 更新 `docs/v1/CODEX_RESULT.md`；
2. 更新压缩后的 `docs/v1/RC_MANUAL_TEST.md`；
3. 若满足冻结条件，新增/更新 `docs/v1/RC_CANDIDATE.md`；
4. commit 所有必要代码/文档修改；
5. push 到 `codex/v1-06-manual-test`；
6. 停止，不继续主动寻找新问题；
7. 不创建 Release / Tag；
8. 等待 ChatGPT 对候选版做最终审核。

本轮的成功标准不是“又找到更多可改之处”，而是：**把能由 Codex 可靠验证的工作做完，并把用户最终验收缩减到最少。**
