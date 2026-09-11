# 当前任务：第二轮用户反馈——演示流程、页码、自适应 Timer 与真实桌面收口

日期：2026-09-11。Review 分支：`codex/v1-06-manual-test`。版本先保持 `1.13.0`。完整用户原话整理见 `docs/v1/USER_FEEDBACK_20260911_2.md`。

## 0. 先同步，尊重用户已经确认通过的结果

用户刚刚真实确认：

- 跨屏最终尺寸不再累计异常；
- ScrollView 已不再遮挡；
- “作者的话”弹窗正常；
- 手机 Remote 已可正常连接控制；
- 音频播放正常；
- “时间到”全屏黑屏正常。

不要重做这些成功项，不要恢复旧实现。当前剩余跨屏问题只是“拖动过程中偶尔瞬时错位，松开即恢复”。

先拉取最新 HEAD，按 `AGENTS.md` 顺序阅读，并额外读 `USER_FEEDBACK_20260911_2.md`。

### ChatGPT 正在直接收口的局部改动

GitHub Actions `Second feedback direct fixes`（run `34586469962`）已由 ChatGPT 发起。它只负责以下小范围：

1. Settings / PC Remote 主 ScrollView 增加内容与滚动条 gutter；
2. 应用“时间到”黑屏期间用 ESC 解除遮罩；
3. 单次提示音最多播放 10 秒；
4. 复制提示音时保留用户选择文件的原 basename，以 `alert-sounds/<slot>/<原文件名>` 隔离三个提示槽；
5. Web/mobile `ppt.*` 主动操作在执行前自动解除 FlyPPTTimer 的“时间到”遮罩（`ppt.refresh` 除外）；
6. 手机 Web Remote 对短时轮询失败做容错，并在 online / focus / 回前台时立即重连；不自动重发 POST 命令。

**开始改源码前先检查该 workflow。** 只有它成功并把 `fix: streamline blackout audio and remote resilience` 产品提交推回当前分支后，才以该产品提交为你的新基点。若 workflow 失败，不要把半成品当已修；先读失败日志，仅修直接补丁的编译/测试错误至绿色，再进入下面任务。不要重新实现一套相同功能。

## 1. U01：消除跨 DPI 拖动期间的瞬时显示错位

用户确认松手后的尺寸和布局已经正确，所以必须保留 `bf00cf0` 的无累计漂移修复。

在真实 150%/125%（或当前可用的混合 DPI）双屏，用 Settings 和 PC Remote 按用户的沿边/跨边路径复现**鼠标仍按住时**的瞬时错位。记录必要的应用窗口几何和 WM_DPICHANGED / WINDOWPOS 顺序，不采集私人桌面。

目标：拖动跨 DPI 边界时，Slint 内容、client rect 和原生外框在可见帧内同步，不出现明显错位再等鼠标松开恢复。

禁止：重新引入累计尺寸缩放、在 DPI handler 递归 `SetWindowPos`、循环延迟校正、固定窗口尺寸、禁用 Per-Monitor DPI、禁止用户拖动。

至少回归：绕边 3 圈、反向 10 次、主动 resize 后跨屏、最大化/还原、同进程重开。最终尺寸仍必须稳定。

## 2. U03：其他文本/确认弹窗统一真机检查

“作者的话”用户已通过，不再修改其文案。检查剩余 custom Slint dialog / confirmation / error / long-message 路径：

- Settings 未应用更改提示、导入/恢复默认错误和确认；
- PC Remote 批量设置、错误/确认；
- 更新提示、Remote 错误等当前可安全触发的文本弹窗；
- Web Remote confirm panel 的长中文/英文。

125%/150% 可用时各做一次，中英文至少覆盖典型长文本。要求不裁字、不盖按钮、需要滚动时能到底、Tab/Enter/Space/ESC 等既有键盘行为正常、modal 打开时背景不误操作。

只修具体布局缺陷，不重做主题。

## 3. U07：切换/启动不同文稿时，原子地结束旧放映再处理目标

这是本轮最高优先的演示逻辑问题。用户已真实复现：A.pptx 正在放映时，手机直接“切换/打开” B.pptx，B 被置顶但 A 仍在 Slide Show，计时生命周期继续被 A 占用。

不要在 Web 端连续发两个独立 HTTP 命令。`PresentationService` 当前有单 STA worker 和 busy 状态，应增加一个**单个串行高层操作**（命名自定，保持小改动）：

- 若目标与当前正在放映的文稿不同：先结束旧 Slide Show；
- 再打开/激活目标；
- 若用户请求“从头放映/从当前页放映”，继续在同一 worker 操作中启动目标放映；
- 切换本身不关闭旧文稿、不保存旧文稿、不改变旧文稿 dirty 状态；
- 如果同一目标已处于需要的状态，不做无意义 stop/reopen；
- 新放映启动后 Timer 必须绑定目标文件规则，不能继续占用旧规则/旧 round。

同类简化逻辑：

- 当前要关闭的文稿若正在放映，在既有确认之后，同一关闭操作先结束该文稿放映再关闭；
- Force Quit 继续保留危险确认；
- Previous/Next/Goto 在没有放映时仍报正常错误，不擅自启动放映；
- ChatGPT 直接补丁已让所有 mobile `ppt.*` 主动操作先解除应用自己的 TimeUp 遮罩，保留这个行为。

用两份可丢弃三页文稿 A/B 做真实 PowerPoint 和 WPS（环境存在时）验证：A 放映+计时 -> 手机选 B -> A 不再放映；再分别测试 Open、StartFromBeginning、StartFromCurrent，确认目标 timer rule 正确接管。

## 4. U08：手机演示文件列表支持隐藏 / 恢复 / 删除 / 添加 / 排序

保持现有手机演示页总体结构，不把 PC Remote “演示文稿”页恢复成放映控制器。

### 隐藏 / 恢复

- “隐藏”只影响 mobile 演示列表可见性，**不能等价于 `FileRule.enabled=false`**，计时规则仍正常生效；
- 提供“显示隐藏项/隐藏项”入口，可恢复，不能隐藏后永久找不到；
- 当前活动文稿即使对应规则已隐藏，顶部“当前演示”状态仍应正常显示。

### 删除

- 删除的是 FlyPPTTimer 的保存规则/列表项，必须确认；
- 绝不能删除磁盘上的 PPT/PPTX/PDF 文件；
- 不因删除列表项自动关闭一个已打开的 Office 文稿；
- 对仅“已打开但未保存为规则”的临时项目，不显示会让人误解为删文件的危险 Delete。

### 添加

手机不能因此获得任意浏览电脑文件系统的能力，也不要新建文件上传协议。优先实现：

- 将“当前/已打开但尚未成为 FileRule 的演示文稿”直接加入受控文件列表；
- 如果现有架构能安全做到，可另提供“在电脑选择文件”动作，由电脑端原生文件选择器完成；不要让一个长时间阻塞的文件选择器导致手机 POST 被重复发送或误判两次。

这已经满足手机端“添加文件到列表”的核心需求；不要为了它做远程文件管理器。

### 排序

- 保存用户自定义顺序并在重启/重连后保持；
- 手机触控优先，可用上移/下移或稳定拖拽柄；
- 不要因为每秒状态刷新把用户排序自动打乱；
- unmanaged 的已打开文稿可以单独稳定显示，不要污染持久规则顺序。

如果给 `FileRule` 增加 `mobile_hidden` / `mobile_order` 或等价字段：必须依赖 serde default 保持旧配置兼容，并更新 Settings 的 `merge_rule_fields`，避免并发 Settings Apply 丢掉手机端隐藏/排序更改。新增 Remote rule command 必须继续受现有 token 鉴权。

## 5. U09：Remote 稳定与可断线重连

用户确认“能连”，问题变成使用中偶发断联。

ChatGPT 直接补丁先做客户端低风险收口：前三次连续状态轮询失败才显示断开；短时失败自动重试；手机从后台/离线恢复时立即 poll；POST 操作不盲目重放。

Codex 在真实手机条件可用时重点验证：

- 连续操作 10 分钟；
- 手机锁屏 20~30 秒再解锁；
- 浏览器切后台再回来；
- 临时关闭/恢复 Wi-Fi；
- 网络恢复后无需重新扫码即可继续控制（token/port 未改变时）；
- Next/Close 等命令不会因重连执行两次。

先判断直接客户端修复是否已解决用户体验。只有有可复现的服务端问题时才最小修改 `remote.rs`。不要为此上 WebSocket/数据库/公网服务/复杂心跳框架，也不要关闭鉴权或防火墙。

## 6. U10：Timer 显示“当前页/总页数”，默认开启

`PresentationState` / Remote 已有 `current_slide`、`total_slides`，复用它，不建立第二套 Office 查询线程。

新增 Appearance/Display 设置：**“显示当前页/总页数”**，默认开启。旧配置通过 serde default 得到开启状态，并增加兼容测试。

Timer 显示要求：

- 主时间在上；有效放映页码在下，例如 `1/23`；
- 页码字号明显小于主时间、居中、视觉层级次要；
- 仅 `current_slide > 0 && total_slides > 0` 时显示，不出现 `0/0`；
- 普通 Timer 和多屏镜像 Timer 一致；大屏 Timer 也实现一致布局，除非真实验证表明会明显破坏当前大屏体验，此时在结果里说明；
- 翻页更新不能重置 timer、不能写配置、不能让窗口尺寸每 500ms 抖动。

设置开关应支持下面的实时预览语义。

## 7. U11：Timer 按内容自动适配 + 宽/高/字号实时预览

先读现有 `expand_timer_windows_if_needed`：它现在只会扩大，并把实际扩大结果写回 `Appearance.Width/Height`。**不要在它上面再叠一个新的自动尺寸循环。**

建议的简单产品语义：

- `Appearance.Width/Height` 继续存在，但解释为用户手工设置的**最小/基准尺寸**；
- 实际 Timer 尺寸 = 至少这个基准，同时足以容纳当前 time text 和可选页码行；
- 内容变长时自动增大，内容变短/页码隐藏时可以缩回基准；
- 自动算出的瞬时实际尺寸不写回配置文件；
- 不额外增加“自动尺寸”开关，除非实现验证证明无法同时保留手工尺寸语义；
- `sync_timer_window_scale`、内容测量和混合 DPI 只保留一个明确的最终尺寸来源，不能相互争抢。

### Settings 实时预览

用户在 Settings 修改以下项目时，Timer 立即变化，无需点“应用”：

- 宽；
- 高；
- 字号；
- “显示当前页/总页数”开关。

只把这些字段作为**预览**应用到当前 Timer/overlay/big screen，不能因此把整个 Settings draft 提前写入共享 applied config 或磁盘。Cancel/选择放弃修改后恢复最后一次 Applied 值；Apply 才持久化。Settings 其他字段仍维持原 Apply 语义。

真实回归至少覆盖：

- 时间单行；
- 时间+`1/23` 双行；
- 超时前缀/较长时间文本；
- 页码出现/消失时尺寸平滑变化；
- 修改字号和宽高立即预览；
- Cancel 恢复、Apply 持久化；
- 多屏镜像；
- 125%/150% 跨屏后无新的累计尺寸异常和无明显 resize jitter。

## 8. 受影响回归与交付

不要让用户重测已经确认的所有旧功能。由 Codex 使用 computer-use / 真实 Windows 尽量完成本轮回归。

必须补自动测试：

- 新 FileRule mobile metadata 的旧配置默认值与 Settings merge；
- hide/unhide/delete/order 的纯配置逻辑，不删真实文件；
- A 放映 -> 原子切 B 的 worker/状态逻辑能覆盖的部分；
- page label 显示条件；
- auto-size 的 time-only / time+page 计算，且不把自动尺寸写回配置；
- live preview Cancel/Apply 的状态边界；
- 保留现有 Remote token、F3、rule merge、Office range restore 等回归。

最终执行：

```powershell
cargo fmt --all -- --check
cargo clippy --locked --all-targets --all-features -- -D warnings
cargo test --locked
cargo build --locked --release
```

用真实 Windows 再做 U01/U03/U07/U08/U09/U10/U11 的可操作部分。Office 只用可丢弃文稿。

完成后更新 `CODEX_RESULT.md`，逐项标记“真实桌面通过 / 自动或源码通过 / 环境限制 / 未解决”，并把 `RC_MANUAL_TEST.md` 压缩到真正还需要用户感受的最少项目。

commit + push 后停止等待 ChatGPT 审核。不要生成多个让用户轮流试的中间包；本轮所有源码收口后再生成一个最终绿色 ZIP。

未经用户明确批准：不创建 Release/Tag，不合并默认分支，不升级 Rust/Slint/依赖，不换技术栈，不删产品功能。