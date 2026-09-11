# 当前任务：第二轮用户反馈——演示流程、页码、自适应 Timer 与真实桌面收口

日期：2026-09-11。Review 分支：`codex/v1-06-manual-test`。版本先保持 `1.13.0`。完整用户反馈见 `docs/v1/USER_FEEDBACK_20260911_2.md`。

## 0. 当前确定基点

先拉取最新 `codex/v1-06-manual-test`，按 `AGENTS.md` 顺序阅读，并额外读 `USER_FEEDBACK_20260911_2.md`。

ChatGPT 的第二轮直接小修已经完成，不再是待执行状态：

- 产品提交：`fdadde3ba8b4e88799cc2b67b049437001117a20`
- 提交：`fix: streamline blackout audio and remote resilience`
- Windows workflow：`Second feedback direct fixes`，run `34586469962`
- 结果：fmt 通过；clippy `-D warnings` 通过；`cargo test --locked` **66 passed / 0 failed / 3 ignored**；`cargo build --locked --release` 通过。

因此 **Codex 的源码工作基点是 `fdadde3...`**。以下直接小修已进入源码，Codex 只做真实桌面/受影响回归，不得另写一套重复实现：

1. Settings / PC Remote 主 ScrollView 已扩大内容到滚动条的 gutter；
2. FlyPPTTimer 自己的“时间到”全屏遮罩期间，PC 按 ESC 可解除；
3. 单次提示音可听播放时间上限为 10 秒，TTS 不受此限制；
4. 提示音复制仍进入程序目录，但保留用户选择文件的原 basename，并用每个 prompt slot 的子目录隔离同名文件；
5. Web/mobile 的主动 `ppt.*` 操作在服务端执行前先解除 FlyPPTTimer TimeUp 遮罩，`ppt.refresh` 除外；
6. Web Remote 对短时状态轮询失败容错，并在 online / focus / 页面回前台时立即重新同步；不会自动重放可能非幂等的 POST 命令。

上述内容目前是**源码 + Windows CI 通过**，不等于全部真实手机/真实桌面体验已完成。真实可操作部分由 Codex 本轮补验。

用户已经真实确认上一包：跨屏最终尺寸稳定、ScrollView 不再遮挡、“作者的话”正常、手机 Remote 可用、音频可用、全屏黑屏可用。不要重做这些成功项。当前剩余跨屏问题仅是“鼠标仍按住跨 DPI 拖动时偶发瞬时错位，松开恢复”。

## 1. U01：消除跨 DPI 拖动期间的瞬时显示错位

保留 `bf00cf0` / `fdadde3` 已经验证的无累计尺寸漂移行为，不重新设计窗口尺寸流程。

在真实 150%/125%（或当前可用的混合 DPI）双屏，用 Settings 和 PC Remote 按用户的沿边/跨边路径复现**鼠标仍按住时**的瞬时错位。记录必要的应用窗口几何、`WM_DPICHANGED` / `WINDOWPOS` / client rect 与 Slint scale factor，不采集私人桌面。

目标：跨 DPI 边界拖动过程中，Slint 内容、client rect 和原生外框在可见帧内同步，不再明显错位后等松手恢复。

禁止：重新引入累计尺寸缩放、在 DPI handler 递归 `SetWindowPos`、循环延迟校正、固定窗口尺寸、禁用 Per-Monitor DPI、禁止用户跨屏拖动。

回归：绕边至少 3 圈、反向至少 10 次、主动 resize 后跨屏、最大化/还原、同进程重开。最终尺寸必须继续稳定。

## 2. U03：其他文本/确认弹窗统一真机检查

“作者的话”用户已通过，不再改其文案。检查剩余文本/确认/错误弹窗：

- Settings 未应用修改、导入、恢复默认、错误提示；
- PC Remote 批量设置、错误/确认；
- 更新提示、Remote 错误等当前可安全触发的 Win32/Slint 消息；
- Web Remote confirm panel 的长中文/英文。

125%/150% 可用时各做一次，中英文覆盖典型长文本。要求：不裁字、不盖按钮；需滚动时能到末尾；Tab/Enter/Space/ESC 等既有键盘行为正常；modal 打开时背景不可误操作。

只修有证据的具体布局缺陷，不重做主题。

## 3. U07：切换/启动不同文稿时，原子地结束旧放映再处理目标

这是本轮最高优先的演示逻辑问题。用户真实复现：A.pptx 正在 Slide Show 时，手机直接打开/切换 B.pptx，B 被置顶但 A 仍在放映，计时生命周期继续被 A 占用。

不要在 Web 端连续发 `EndShow` + `Open` 两个独立 HTTP 命令。`PresentationService` 已有单 STA worker 和 busy 状态，应做一个**单个串行高层操作**（命名自定，保持小改动）：

- 若目标与当前正在放映的文稿不同，先结束旧 Slide Show；
- 再打开/激活目标；
- 若原请求是“从头放映/从当前页放映”，继续在同一 worker 操作中启动目标放映；
- 切换本身不关闭旧文稿、不保存旧文稿、不改变旧文稿 dirty 状态；
- 同一目标已处于需要状态时不做无意义 stop/reopen；
- 新放映启动后 Timer 必须绑定新目标文件规则，不能继续占用旧规则/旧 round。

同类流程优化：

- 关闭一个**当前正在放映的目标文稿**时，在既有危险/关闭确认语义之后，同一高层关闭流程先结束该文稿放映再执行既有关闭；
- Force Quit 继续保留危险确认；
- Previous / Next / Goto 在没有放映时继续明确报错，不擅自自动启动放映；
- 保留 `fdadde3` 的行为：所有 mobile `ppt.*` 主动操作先退出 FlyPPTTimer TimeUp 遮罩。

用两份可丢弃三页文稿 A/B 做真实 PowerPoint 和 WPS（环境存在时）验证：A 放映+计时 -> 手机选 B -> A 不再放映；分别测试 Open、StartFromBeginning、StartFromCurrent，确认新目标 timer rule 接管。

## 4. U08：手机演示文件列表支持隐藏 / 恢复 / 删除 / 添加 / 排序

保持手机演示页总体结构；PC Remote “演示文稿”页仍只做规则管理，不恢复 PC 放映控制按钮。

### 隐藏 / 恢复

- 隐藏只影响 mobile 演示列表可见性，**不能等价于 `FileRule.enabled=false`**；隐藏的规则仍能正常参与计时；
- 提供隐藏项入口，可恢复；
- 当前活动文稿即使规则已隐藏，顶部当前演示状态仍正常显示。

### 删除

- 删除的是 FlyPPTTimer 保存的列表/规则条目，必须确认；
- 绝不能删除磁盘上的 PPT/PPTX/PDF；
- 不因删除列表项自动关闭已打开 Office 文稿；
- 对仅“已打开、尚未保存为规则”的临时项目，不显示容易误解成“删文件”的危险 Delete。

### 添加

不要让手机获得任意浏览电脑文件系统的能力，也不要增加上传协议。优先安全实现：

- 将“当前/已打开但尚未成为 FileRule 的演示文稿”直接加入受控列表；
- 如现有架构能安全做到，可另提供“在电脑选择文件”，由电脑端现有原生文件选择器完成；不要让长时间阻塞的选择器导致手机 POST 被重复执行或误判两次。

### 排序

- 自定义顺序持久保存并在重启/重连后保持；
- 手机触控优先，可用上移/下移或稳定拖拽柄；
- 每秒状态刷新不能重排用户列表；
- unmanaged 已打开文稿可单独稳定显示，不污染持久规则顺序。

如果给 `FileRule` 增加 `mobile_hidden` / `mobile_order` 或等价字段：

- 必须 `serde(default)` 或等价默认，保证旧配置兼容；
- 更新 Settings 的 `merge_rule_fields`，防止 Settings 打开后手机改隐藏/排序，随后 Settings Apply 又覆盖这些新字段；
- 新 Remote 规则管理命令继续走现有 token 鉴权；
- 删除逻辑只能改配置，测试要证明不会调用真实文件删除。

## 5. U09：Remote 稳定与断线自动恢复

先验证 `fdadde3` 的客户端低风险修复是否已解决用户体验：短时状态 poll 失败不立即永久断线；连续失败才显示断开；online/focus/回前台立即重新同步；POST 不盲重放。

真实手机条件可用时验证：

- 连续操作约 10 分钟；
- 手机锁屏 20~30 秒再解锁；
- 浏览器切后台再回来；
- 临时关闭/恢复 Wi-Fi；
- token/port 未改变时，网络恢复后无需重新扫码即可继续；
- Next/Close 等命令不会因为重连执行两次。

只有真实复现服务端问题时才最小修改 `remote.rs`。不要引入 WebSocket、数据库、公网服务、复杂 heartbeat/revision 框架，也不要关闭 token 或降低安全边界。

## 6. U10：Timer 显示“当前页/总页数”，默认开启

复用现有 `PresentationState.current_slide`、`total_slides`，不要建立第二套 Office 查询线程。

新增 Appearance/Display 设置：**“显示当前页/总页数”**，默认开启。旧配置加载时也默认开启，并增加兼容测试。

要求：

- 主时间在上，有有效演示页码时在下显示如 `1/23`；
- 页码字号明显小于主时间、居中、视觉层级次要；
- 仅 `current_slide > 0 && total_slides > 0` 显示，绝不出现 `0/0`；
- 普通 Timer、多屏镜像 Timer 一致；大屏 Timer 也尽量一致，若真实验证会破坏当前大屏体验，在结果里说明后采取最小合理布局；
- 翻页更新不能重置 timer、不能写配置、不能让窗口尺寸频繁抖动。

该开关需要支持下一节的 Settings 实时预览。

## 7. U11：Timer 按内容自动适配 + 宽/高/字号实时预览

先处理现有 `expand_timer_windows_if_needed`：它当前只会扩大，并把扩大结果写回 `Appearance.Width/Height`。**不要再叠一个新的 resize loop。**

推荐的简单产品语义：

- `Appearance.Width/Height` 保留为用户手工的**最小/基准尺寸**；
- 实际 Timer 尺寸至少为该基准，同时足以容纳当前 time text 和可选页码行；
- 内容变长时自动增大，内容变短/页码消失时可缩回基准；
- 自动算出的瞬时实际尺寸不写回配置，不每秒产生磁盘写入；
- 除非真实实现证明必要，不增加“自动尺寸”新开关；
- `sync_timer_window_scale`、内容测量、混合 DPI 只保留一个明确的最终尺寸来源，避免多个循环争抢尺寸并恢复之前的跨屏漂移。

### Settings 实时预览

用户在 Settings 修改以下项目时，Timer 立即变化，不必点“应用”：

- Width；
- Height；
- Font size；
- “显示当前页/总页数”开关。

这些字段只作为**运行态预览**应用到当前 Timer / mirrors / big screen；不得因此把整个 Settings draft 提前写入 shared applied config 或磁盘。Cancel / 选择放弃修改后恢复最后一次 Applied 值；Apply 才持久化。Settings 其他字段继续维持原 Apply 语义。

真实回归至少覆盖：时间单行、时间+`1/23` 双行、超时前缀/较长时间文字、页码出现/消失、修改字号/宽高实时预览、Cancel 恢复、Apply 持久化、多屏镜像、125%/150% 跨屏后无新的累计尺寸异常与明显 resize jitter。

## 8. 必补自动回归

- 新 FileRule mobile metadata 的旧配置默认值与 Settings merge；
- hide/unhide/delete/order 的纯配置逻辑，证明不删除磁盘文件；
- A 放映 -> 原子切 B 能覆盖的 worker/状态逻辑；
- page label 显示条件；
- auto-size 的 time-only / time+page 计算，并证明自动尺寸不写回基准配置；
- live preview 的 Cancel/Apply 状态边界；
- 保留既有 Remote token、F3、rules merge、Import/Reset、Office range restore、COM unknown sample 等全部回归。

## 9. 最终验证与交付

由 Codex 使用真实 Windows / computer-use 尽量完成 U01/U03/U07/U08/U09/U10/U11 的可操作部分。Office 只用可丢弃文稿。

最后统一执行：

```powershell
cargo fmt --all -- --check
cargo clippy --locked --all-targets --all-features -- -D warnings
cargo test --locked
cargo build --locked --release
```

更新 `docs/v1/CODEX_RESULT.md`，逐项标记“真实桌面通过 / 自动或源码通过 / 环境限制 / 未解决”，并更新 `RC_MANUAL_TEST.md`，只留下客观上仍需要用户体验确认的最少项。

完成后 commit + push，停止等待 ChatGPT 审核。**不要生成多个让用户轮流测试的中间包**；本轮源码和真实桌面收口完成后只生成一个最终绿色 ZIP。

未经用户明确批准：不创建 Release/Tag，不合并默认分支，不升级 Rust/Slint/依赖，不换技术栈，不删产品功能。