# 当前任务：RC-3.1 终审阻断收口

日期：2026-09-12。Review 分支：`codex/v1-06-manual-test`。版本继续保持 `1.13.0`。

## 0. 审核基点与目标

本轮产品源码审核基点：

`a644e74b82bf5a65474f027ef492fd6646456ac3`

提交：`fix: integrate presentation switching and adaptive timer previews`

ChatGPT 已复审该提交、`CODEX_RESULT.md` 以及 U07–U11 的关键实现。大部分实现方向接受：原子切换、手机规则管理、页码、Timer 自适应、限定字段实时预览、WPS 真机验证和 72 passed / 0 failed / 3 ignored 均保留，不重新实现。

本轮**只收口下面 4 个审核发现**。不要重新做整仓库审计，不扩大 UI，不升级版本/依赖，不改已由用户确认通过的黑屏、音频、基本 Remote、最终跨屏尺寸稳定等功能。

开始前：拉取最新分支，按 `AGENTS.md` 顺序读取交接文档；本任务覆盖旧 `CODEX_TASK.md`。

## 1. P1：ESC 短按不能可靠解除 TimeUp 黑屏

### 已确认问题

当前 `src/app.rs` 每 100ms 轮询一次 `window::escape_key_down()`，`src/window.rs` 只读取 `GetAsyncKeyState(VK_ESCAPE)` 的当前按下高位，并做 down-edge。

你自己的真实桌面结果已经记录：瞬时 SendKeys ESC pulse 没有被捕获，改为按住约 250ms 才成功。

用户要求是“黑屏时电脑端支持按 ESC 关闭黑屏”，正常短按不能要求用户刻意长按。

### 要求

- 用最小可靠实现捕获普通 ESC 短按；优先利用 Win32 已有键状态/消息机制，不引入全局低级键盘 hook 或复杂事件框架。
- 仅 TimeUp 遮罩存在时执行解除；没有 TimeUp 时不得改变 Timer/Office/其他窗口状态。
- 保留 F4 / Remote Reset / mobile `ppt.*` 的既有解除逻辑。
- 不改变普通 Office/WPS 自己的 ESC 语义。

### 验证

至少使用真实键盘注入/真实键盘完成 30–70ms 左右的 keydown/up 短按测试，证明无需 250ms 长按也能解除；再验证无 TimeUp 时同样按键无 FlyPPTTimer 副作用。

## 2. P1：目标文稿已经在放映但不是 ActivePresentation 时，“切换”会提前返回

### 已确认代码路径

当前 `Session::open(path)`：

1. `end_other_shows(&app, &path)`；
2. 若 `matching_show_views(&app, &path, true)` 非空，立即 `return Ok("目标文稿已在放映")`；
3. 只有后面的代码才查找/打开 presentation 并 Activate 目标编辑窗口。

因此存在明确场景：

- A 已在 Slide Show；
- 用户在电脑端激活了未放映的 B，使 `ActivePresentation=B`；
- 手机上 A 一行会出现“切换”；
- 点击 A 后 `open(A)` 因 A 已在放映而提前返回，A 并未重新成为 ActivePresentation；
- 随后的 Next/Previous/Goto/黑白屏等命令按已修的 known-target 安全策略查看 ActivePresentation=B，并会拒绝匹配 A。

不能通过恢复“唯一窗口随便控制”的旧回退来解决。

### 要求

- `Open/Switch` 即使目标已经在放映，也必须完成“目标文稿成为当前活动目标”的切换语义；
- 如果目标自己的 Slide Show 已经运行，不停止、不重启、不重置 Timer；只完成必要的目标激活；
- 保留“切换不同文稿先结束其他文稿放映”的 U07 语义；
- 保留命令 known-target 安全：目标不匹配时仍不得任意控制第一个放映窗口；
- `StartFromBeginning/StartFromCurrent` 对“同一目标已经在放映”的重复启动继续短路，不重启现有放映。

### 回归

至少覆盖：

1. A 已放映 + Active=B -> mobile Open/Switch A -> Active=A，A 放映不重启；随后 Next 能控制 A；
2. A 放映 -> 切 B -> A 结束，B 激活；
3. A 已放映且 Active=A -> 再 Open/Start A 不重启；
4. 已知目标 Missing / B 未放映时仍不会误控唯一放映 A。

真实 WPS 环境可用时用两份可丢弃三页文稿做一遍；原生 PowerPoint 若当前 COM 仍被 WPS 接管，只记录环境限制。

## 3. P1：实际 CloseActive 出现 COM worker 超时，必须找到并收口阻塞路径

### 已有证据

`CODEX_RESULT.md` 已明确写明：本轮末尾追加“重新打开 A 再确认关闭”的真实测试中，presentation worker 超时，没有得到关闭路径通过证据。

这不是要求架构重写，但发布前不能留下一个已实际发生、可能把单 STA worker 卡住的关闭路径。

### 要求

只使用可丢弃文稿，区分并记录至少：

- FlyPPTTimer 自己打开/managed：clean、dirty；
- 用户/Office 已经打开的 unmanaged：clean、dirty；
- 上述文稿正在放映 / 未放映。

定位超时是在 `end_show_for`、Saved 处理、Office/WPS Close 提示、还是其他 COM 调用。

修复原则：

- mobile 的 Close 当前已有明确“关闭且不保存”确认；在**已有明确确认的路径**内可以按既有产品语义避免 Office 再弹一个隐藏/阻塞保存询问；
- 不能把未确认的用户 dirty 文稿静默丢弃；如果某调用路径没有足够的用户确认，宁可清晰拒绝并返回错误，也不能卡住 worker 或偷偷保存/丢弃；
- 关闭正在放映的目标：先安全结束该目标放映，再关闭；
- 一个失败/拒绝的 Close 之后，presentation worker 必须恢复可用，Refresh/Next/Open 等后续命令不能永久 Busy；
- 不增加第二套 COM worker、kill/retry 大框架、任意超时强杀 Office。

如果最终证明此次超时是测试环境一次性外因，也必须给出可复现尝试和后续 worker 可用性证据；不能只把超时删出报告。

## 4. P2：桌面新增规则会把 mobile 自定义顺序打乱

### 已确认原因

`FileRule::default().mobile_order == 0`。

mobile `rules.addOpen` 已先 normalize 再按末尾追加，但 Settings 和 PC Remote 的“添加文件”仍通过 `FileRule { ... ..Default::default() }` 创建，因此用户在手机排好 A/B/C 后，从桌面新增 D，D 会以 order=0 进入，mobile state 按 `mobile_order` 排序时可跳到前面/中间。

### 要求

- 所有新增 FileRule 的入口共享一致的“追加到 mobile 顺序末尾”语义：至少 Settings、PC Remote、mobile addOpen；
- 保留已有规则的相对自定义顺序；
- add/delete/move 后保证 order 可持久化且无重复/异常增长；
- Settings stale draft merge 继续保留 mobile_hidden/mobile_order 的外部更新；
- 不改变 PC Remote 本身的普通规则列表交互语义。

### 回归

至少增加：A/B/C 已有自定义 mobile 顺序 -> Settings 新增 D -> mobile 顺序 A/B/C/D；PC Remote 新增 E -> A/B/C/D/E；保存、重启/重新构建 remote state 后顺序不变。

## 5. 本轮不要修改的内容

- **不要**为家用机没有 150%/125% 条件而猜修 U01 drag-in-progress 瞬时错位；保留现有无累计漂移实现，把真实混合 DPI 继续留在最终手测边界。
- 不恢复 PC Remote 演示控制按钮。
- 不恢复随机端口 UI。
- 不改 TimeUp 全屏覆盖语义。
- 不改提示音 10 秒和原 basename 复制逻辑。
- 不重做 mobile 列表 UI、Timer 页码、自适应尺寸、Settings 预览，只做上述受影响回归。
- 不升级 Rust/Cargo 依赖，不修改版本，不创建 Release/Tag，不合并默认分支。

## 6. 必须执行的验证

```powershell
cargo fmt --all -- --check
cargo clippy --locked --all-targets --all-features -- -D warnings
cargo test --locked
cargo build --locked --release
node --check src/FlyPPTTimer/Web/app.js
```

此外完成本任务要求的真实 WPS/Office disposable-doc 测试和 ESC 短按验证。不要操作用户真实文稿。

## 7. 结果与交付

更新 `docs/v1/CODEX_RESULT.md`，在最顶部新增 **RC-3.1 审核阻断收口**，逐项写：

- ESC 短按实际时长/验证结果；
- already-showing but inactive target 的切换结果；
- CloseActive 超时根因、各 clean/dirty/managed/unmanaged 结果、失败后 worker 是否继续可用；
- desktop-added rule 顺序结果；
- fmt/clippy/test/release/node 检查结果；
- 原生 PowerPoint / 混合 DPI 等当前环境限制继续如实保留。

如果四项全部通过，只生成**一个**新的最终 review 绿色 ZIP，BUILD.txt 写产品源码 SHA 和 EXE SHA256。不要让用户测试中间包。

完成后 commit、push 到 `codex/v1-06-manual-test`，停止等待 ChatGPT 审核。