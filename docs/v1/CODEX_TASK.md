# 当前任务：RC-1 演示窗口目标修复 + 真实候选版验收准备

当前分支：`codex/v1-06-manual-test`

审核基点：`829c8c79b430d92bbd1e7e37fa6d301d07ad40cb`

## 本轮结论

上一轮 `829c8c7` 的以下内容已通过 ChatGPT 源码审核，**不要重做**：

- 配置导入 / 恢复默认立即应用并同步运行态；
- Settings 与 PC Remote 的逐规则合并；
- Remote 空 token 防护；
- Rust 1.92.0 项目工具链固定；
- 更早已通过的 F3 Pause/Resume、Remote 端口输入、多选/批量编辑、发布版本读取等修复。

当前没有发现新的 Timer / Config / Remote 鉴权 P0 阻断。

本轮开始从“持续修 Bug”转入 **RC 收口**。只允许一个已确认的代码兼容修复，然后准备真实候选版证据和手测包。

开始前必须按最新 `AGENTS.md` 顺序阅读；尤其注意 `docs/v1/APPROVED_PRODUCT_DEVIATIONS.md`。该文件覆盖与旧 `V1_BASELINE_CHECKLIST.md` 冲突的用户后续明确要求。

---

# 1. 唯一预先批准的代码修复：多放映窗口必须按目标文稿定位

## 已确认差异

当前 Rust `src/presentation.rs` 在两个关键位置直接使用：

```text
SlideShowWindows.Item(1)
```

包括：

- `read_application_state()` 在存在放映时读取第一个 SlideShowWindow；
- `with_show_view()` 在执行 Previous / Next / Goto / Black / White / Restore / EndShow 等命令时控制第一个 SlideShowWindow。

而 v0.30.2 `PowerPointControlService` 明确不是这样：

- 先读取 `ActivePresentation.FullName`；
- `FindSlideShowWindow(windows, activePath)` 遍历所有放映窗口；
- 按 `window.Presentation.FullName` 与目标文稿完整路径匹配；
- 后续命令使用匹配到的放映窗口；
- 如果存在多个放映窗口但无法匹配目标，返回明确错误，而不是随便控制第一个窗口。

单文稿时通常看不出问题；同时存在多个放映窗口时，Rust 当前实现可能读取或控制错误文稿，因此这是 RC 前需要修正的真实 parity 风险。

## 修复要求

只做最小目标选择修复，不重构 PresentationService。

要求：

1. 增加/复用一个小型辅助函数，遍历 `SlideShowWindows`，根据 `window.Presentation.FullName` 与目标文稿路径匹配窗口；
2. 路径比较继续使用当前已有 `same_path()` / `normalize_path()` 语义；
3. `read_application_state()`：
   - 放映运行时优先读取 `ActivePresentation.FullName` 作为目标；
   - 按路径找对应 SlideShowWindow；
   - 如果目标路径不可得且当前**只有一个**放映窗口，可以安全使用唯一窗口；
   - 如果有多个放映窗口且无法确定目标，返回/记录明确错误，不得退回 `Item(1)`；
4. `with_show_view()` 及 Previous / Next / Goto / Black / White / Restore / EndShow：使用与状态读取一致的目标选择规则；
5. 不改变 `start_show()` 当前“已有放映则忽略重复启动”的现有行为；
6. 不改变文件打开、managed ownership、关闭文稿、退出 Office、force quit 的现有逻辑；
7. 不增加窗口激活重试系统、缓存状态机或额外 COM 框架。

## 验证

至少增加一个不依赖真实 Office 的最小纯逻辑测试，用来证明：

- 目标路径 A 在 `[B, A]` 时选择 A，而不是索引 1；
- 单一窗口且目标路径为空时允许唯一窗口 fallback；
- 多窗口且目标路径无法匹配时不选择任意窗口。

如果为了测试需要把“根据若干路径确定目标索引”的选择规则抽成纯函数，可以这样做；不要为 COM 写 Mock 框架。

如果当前新电脑上的 PowerPoint 可以安全进行真实验证，可额外测试两个**一次性临时文稿**的多窗口场景。必须满足：

- 不使用用户真实文稿；
- 不覆盖任何现有文件；
- 发现 PowerPoint/WPS 中已经有用户文稿打开时，不关闭、不强退、不修改它们；
- 无法安全构造多放映窗口时，直接记为“待人工复核”，不要为了测试写复杂自动化。

---

# 2. 固化用户已经批准的 Remote 产品调整

ChatGPT 已创建：

`docs/v1/APPROVED_PRODUCT_DEVIATIONS.md`

并把它加入 `AGENTS.md` 必读顺序。

本轮不要修改这些批准行为：

- Remote 设置页不再显示“使用随机端口”；固定端口不可用时才自动切换并保存替代端口；
- PC Remote“演示文稿”页只做文件规则管理，不恢复演示状态区和 PPT/WPS 放映控制按钮；
- 手机 / 浏览器 Web Remote 仍必须保留完整演示控制能力。

不要根据旧 `V1_BASELINE_CHECKLIST.md` 把上述 UI 恢复。

---

# 3. RC 静态复核：只找发布阻断，不再扩展重构

在完成第 1 项后，对当前实现做一次有限的 RC 静态核对，范围只包括：

- Timer 状态转换和单调时间；
- 默认配置与 v0.30.2 读取兼容；
- F3/F4/F5 及内部 F7/F8、Ctrl+Alt 快捷键；
- Timer 右键菜单和托盘菜单；
- Remote Web 主要命令映射；
- PowerPoint/WPS ownership / Close / Exit / ForceQuit 安全边界；
- 配置保存、导入、恢复默认；
- 单实例与正常退出；
- Portable / Installer 入口。

这一步的规则：

- **没有稳定复现或明确源码证据，不改代码。**
- 只修 P0 / P1：崩溃、卡死、计时错误、配置丢失、错误控制文稿、用户文稿风险、Remote 鉴权、无法启动/退出、核心功能不可用。
- UI 微调、代码风格、架构洁癖、理论性极端场景一律不在本轮修改。
- 如果未发现新的 P0/P1，明确写“未发现新的发布阻断”，不要为了让任务看起来有工作量而制造修改。

---

# 4. 尽可能用新电脑完成真实 Release 冒烟

新电脑上一轮已经确认 PowerPoint、WPS、双屏、Inno Setup、ffmpeg 和 Rust 1.92 环境基本存在。本轮尽量利用这些真实条件，但不要建立新的自动化框架。

## 4.1 编译与打包

代码完成后统一运行一次：

```powershell
cargo check --locked
cargo fmt --all -- --check
cargo clippy --locked --all-targets --all-features -- -D warnings
cargo test --locked
cargo build --locked --release
```

随后使用现有：

```powershell
scripts/build-release.ps1
```

生成当前 `1.13.0` review Portable 与 Installer。

要求：

- 不修改版本号；
- 不创建 GitHub Release；
- 不创建 Tag；
- 不安装到覆盖用户现有正式版本的位置；
- 本轮产物仍是 RC review 包。

## 4.2 Release 实机冒烟

尽量直接运行本轮 **Release / Portable**，不是只运行 debug。

在不危及用户文件的前提下，实际检查：

- 程序启动、单实例、正常退出；
- Timer Start → Pause → Resume → Stop/Reset；
- F3 / F4 / F5；
- Timer 右键菜单；
- 托盘菜单；
- 设置窗口打开 / 关闭 / 重新打开；
- PC Remote 两页打开 / 关闭 / 重新打开；
- 配置导入和恢复默认后当前运行界面确实同步；
- Remote 端口输入停顿后不被覆盖；
- 规则普通选择 / Ctrl / Shift / 批量修改；
- 如条件安全，PowerPoint 与 WPS 各完成一次最小打开 / 放映 / 翻页 / 结束放映路径；
- 双屏环境下至少确认 Timer、Settings、PC Remote 没有明显跑出屏幕或异常尺寸。

声音/TTS、手机真实局域网、混合 DPI 如果无法可靠自动操作，保留给最终人工验收即可。

---

# 5. 提供少量可供 ChatGPT 继续审核的真实界面证据

用户要求最终界面美观、配合舒服、风格统一；目前用户对总体排版已经满意，所以**本轮只取证，不主动重做 UI**。

如果当前现有截图工具/桌面工具可以直接做到，请从本轮 Release 生成最多 **5 张**代表性 PNG：

1. Timer 正常状态（可含右键菜单）；
2. Settings 代表性页面（优先时长设置）；
3. Settings 外观或行为页面；
4. PC Remote“远程连接”页；
5. PC Remote“演示文稿/规则”页。

要求：

- 必须是真实当前 Release 界面或现有可靠 capture 路径；
- 不为了截图修改产品布局；
- 不新建截图测试框架；
- **不要上传完整桌面截图**，避免泄露桌面文件名、通知或其他个人信息；
- 只裁取 FlyPPTTimer 自身窗口区域；
- 上传前确认图片不含用户私人文件、账号、真实 Remote token、局域网敏感地址等；
- 推荐保存到 `docs/v1/rc-review/`，仅作为 review 分支审核证据；
- 图片数量和体积保持小，后续正式合并前可再决定是否保留。

如果当前工具无法安全生成窗口截图，不要为此改代码；在 `CODEX_RESULT.md` 写明即可。

---

# 6. 生成一份非常短的最终人工验收清单

新增：

`docs/v1/RC_MANUAL_TEST.md`

这不是测试矩阵，只给用户最后实际使用前走一遍关键路径。控制在约 10 个场景以内，覆盖：

1. Timer + F3/F4/F5；
2. 提醒 / 声音 / TTS；
3. Settings Apply / Cancel / Import / Reset；
4. PC Remote 规则选择和批量修改；
5. 手机 Web Remote；
6. PowerPoint；
7. WPS；
8. 双屏 / 跨 DPI / 大屏；
9. Portable；
10. Installer / 升级 / 正常退出。

每个场景只写用户真正需要做的动作和“通过标准”，不要让用户执行开发者命令、收集日志或做设备矩阵。

---

# 7. 本轮禁止事项

除第 1 项明确修复或第 3 项发现有确凿 P0/P1 外，不要修改：

- Timer UI / 配色 /尺寸 / 字体；
- Settings / PC Remote 布局；
- DPI 实现；
- Remote Web UI / API 设计；
- Remote 固定端口产品逻辑；
- Timer 状态机架构；
- 配置 merge 架构；
- Audio 架构；
- 更新产品逻辑；
- Cargo 依赖版本；
- 程序版本号。

不要新增数据库、通用事件总线、复杂 Mock、GUI 自动化框架、截图矩阵、重试/备用路径、SHA 校验流程或另一套发布系统。

---

# 8. 完成方式

完成后：

1. 在 `docs/v1/CODEX_RESULT.md` 顶部新增“RC-1”结果；
2. 明确说明多放映窗口目标选择如何修复；
3. 列出静态 RC 复核是否发现其他 P0/P1；
4. 记录最终测试数量和 Release build / build-release 结果；
5. 记录实际完成了哪些 Release 实机冒烟，哪些仍需用户手测；
6. 如生成截图，提交安全裁剪后的 review 图片并列出路径；
7. 新增 `docs/v1/RC_MANUAL_TEST.md`；
8. commit 并 push `codex/v1-06-manual-test`；
9. 停止编码，等待 ChatGPT 审核；
10. 不创建 Release / Tag。
