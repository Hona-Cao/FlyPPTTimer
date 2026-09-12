# FlyPPTTimer V1 — 当前交接

## 最新状态：家用 Windows 第二轮整合完成，等待审核

2026-09-12：从 GitHub 最新 `b193bef` 开始本轮，完成原子切换、手机列表、页码、自适应和限定字段实时预览；最终 72 passed / 0 failed / 3 ignored，fmt/clippy/release 全绿。真实 WPS、125% 同 DPI 双屏和普通 Edge 验证范围及环境限制详见 CODEX_RESULT 顶部。只交付一个 `home-feedback-20260912` ZIP，提交推送后停止等待审核。

## 本轮任务来源（历史交接）

日期：2026-09-11。Review 分支：`codex/v1-06-manual-test`。版本先保持 `1.13.0`，Rust `1.92.0`。

唯一当前实现指令：`docs/v1/CODEX_TASK.md`。
第二轮用户真实结论与产品语义：`docs/v1/USER_FEEDBACK_20260911_2.md`。

## 用户真实确认的上一包结果

基于 `bf00cf0` 反馈整改包，用户已经确认：

- 跨屏最终尺寸稳定，不再累计放大/缩小；
- ScrollView 不再遮挡控件；
- “作者的话”弹窗正常；
- 手机 Remote 已可正常连接和控制；
- 提示音播放正常；
- “时间到”全屏黑屏正常。

不要在本轮推倒这些成功项。

当前跨屏残余只是在鼠标仍按住、跨不同 DPI 屏幕拖动时偶发内容/显示瞬时错位，松手即恢复。目标是修拖动过程视觉同步，同时保留最终尺寸稳定。

## ChatGPT 直接小修已经完成并验证

产品提交：
`fdadde3ba8b4e88799cc2b67b049437001117a20`

提交信息：`fix: streamline blackout audio and remote resilience`

Windows workflow：`Second feedback direct fixes`，run `34586469962`，已成功。

自动验证：

- `cargo fmt --all -- --check` 通过；
- `cargo clippy --locked --all-targets --all-features -- -D warnings` 通过；
- `cargo test --locked`：**66 passed / 0 failed / 3 ignored**；
- `cargo build --locked --release` 通过。

Codex 后续源码工作以 `fdadde3...` 为基点；不要重复实现以下内容，只做真实桌面/手机受影响验证：

1. Settings / PC Remote ScrollView 已进一步增加内容与滚动条 gutter；
2. FlyPPTTimer TimeUp 全屏遮罩存在时，PC ESC 可解除遮罩；
3. 提示音实际播放最多 10 秒，短音自然结束，TTS 不截断；
4. 提示音复制保留原 basename，以 `alert-sounds/<slot>/<原文件名>` 形式隔离三个提示槽；
5. mobile Web Remote 的主动 `ppt.*` 操作在执行原命令前先解除 FlyPPTTimer TimeUp 遮罩，`ppt.refresh` 不改变状态；
6. Web Remote 状态轮询对短时失败容错，并在 online/focus/页面回前台时主动重新同步；不会自动重放 Next/Close 等 POST 操作。

这些是源码+Windows CI 证据，不等于所有真机体验已经通过。

## Codex 本轮核心职责

### 1. 跨屏拖动中的瞬时错位

真实混合 DPI Windows 上复现 drag-in-progress 视觉不同步，只修具体原因，不重做已经通过的无累计漂移逻辑；禁止递归 SetWindowPos、延迟循环校正、禁用 DPI/拖动。

### 2. 其他文本弹窗

“作者的话”已通过。继续检查 Settings、PC Remote、更新、Remote 错误/确认、Web confirm 等中英文长文本，在 125%/150% 条件可用时做真实桌面回归，只修有证据的裁切/覆盖/键盘问题。

### 3. 文稿切换必须成为单个串行操作

用户复现 A 正在 Slide Show 时手机切 B：B 置顶但 A 仍放映，Timer 继续被 A 占用。

在 Presentation STA worker 中完成“结束旧放映 -> 打开/激活目标 -> 可选启动目标放映”的一个高层命令，不要 Web 连发 EndShow + Open。切换不能关闭、保存或改 dirty 状态的旧文稿；新放映必须让目标 FileRule/Timer 正确接管。

关闭当前正在放映的目标文稿时，在既有确认之后可在同一关闭流程先结束该文稿放映再关闭。Force Quit 继续危险确认；Previous/Next/Goto 无放映时不自动启动。

### 4. 手机演示文件列表管理

支持：隐藏/恢复、删除列表规则、添加已打开但未成为规则的文稿、自定义持久排序。

- 隐藏不等于禁用规则；
- 删除绝不删除磁盘 PPT/PPTX/PDF，也不自动关闭 Office 文稿；
- 不把手机变成任意 PC 文件浏览器，不默认增加上传协议；
- 排序不被每秒状态刷新打乱；
- 新 FileRule metadata 必须旧配置兼容，并纳入 Settings 逐规则 merge，避免 stale Settings Apply 覆盖手机更改。

### 5. Remote 稳定性

先验证 `fdadde3` 客户端自动重连策略。真实手机可用时做锁屏、后台、Wi-Fi 短断和约 10 分钟连续操作。token/port 未变时恢复后不应要求重新扫码，POST 命令不能重复执行。

只有有服务端复现证据才改 `remote.rs`；不引入 WebSocket/公网/复杂心跳框架，不降低 token 鉴权。

### 6. Timer 页码

新增“显示当前页/总页数”，默认开启。复用 `PresentationState.current_slide/total_slides`。有效时主时间下方显示 `1/23`，页码字号更小且居中，无有效页数时不显示 `0/0`。普通 Timer、多屏镜像、大屏体验保持协调。

### 7. Timer 内容自适应 + Settings 实时预览

收口现有只扩大并写回配置的 `expand_timer_windows_if_needed`，不要叠第二套 resize loop。

Width/Height 作为用户最小/基准尺寸；实际窗口根据 time-only / time+page 自动增大，并能缩回基准。自动实际尺寸不写磁盘。

Settings 修改 Width、Height、FontSize、页码显示开关时立即预览 Timer；Cancel/放弃恢复最后 Applied，Apply 才持久化。不要把整个 Settings draft 改成立即应用。

## 操作逻辑原则

可以把**安全、明确、可逆的前置清理**并入用户真正想执行的动作，以减少多步操作：

- mobile Presentation 主动操作 -> 先解除 FlyPPTTimer TimeUp 遮罩；
- 切换另一个文稿 -> 先结束旧放映，再切目标；
- 关闭正在放映的目标文稿 -> 用户确认后先结束该放映，再关闭；
- Start/Restart/Resume -> 保留既有 TimeUp 清理。

不要把破坏性或语义不确定的动作偷偷合并：切换文稿不关闭旧文稿、不保存旧文稿；Previous/Next/Goto 无放映时不自动启动；Force Quit 必须继续明确确认。

## 测试和安全边界

Office 只使用可丢弃临时文稿，绝不打开/保存/关闭/强退用户真实文稿。

不关闭杀软、防火墙或 Remote token；不为了测试建立大型 GUI 自动化框架。

凡真实桌面/手机当前环境能由 Codex 完成的验收由 Codex 完成。最终只把客观上仍依赖用户现场感受的最少项目留下。

本轮全部源码收口后只生成一个最终绿色测试 ZIP，不让用户测试中间包。

## 持久约束

- Rust + Slint V1，不换技术栈；
- PC Remote “演示文稿”页仍只做规则管理，不恢复已删除的 PC 放映控制按钮；
- Web/mobile Remote 保留完整演示控制；
- 保留 F3 Pause/Resume、Settings merge/import/reset、Remote token、端口输入保护、规则多选、Office range restore、COM unknown sample、多放映目标匹配等已修行为；
- 不升级版本/依赖，不创建 Release/Tag，不合并默认分支，未经批准不强推。