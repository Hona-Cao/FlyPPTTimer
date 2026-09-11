# FlyPPTTimer V1 — 当前交接

## 最新状态：第二轮用户反馈进入“直接小修 + Codex 深层整合”阶段

日期：2026-09-11。Review 分支：`codex/v1-06-manual-test`。版本先保持 `1.13.0`，Rust `1.92.0`。

唯一当前实现指令：`docs/v1/CODEX_TASK.md`。
第二轮用户原始结论与产品语义：`docs/v1/USER_FEEDBACK_20260911_2.md`。

## 用户刚刚真实确认的结果

基于 `bf00cf0` 反馈整改包，用户确认：

- 跨屏最终尺寸已经稳定，不再累计放大/缩小；
- 滚动条已不再遮挡控件；
- “作者的话”弹窗正常；
- 手机 Remote 已可正常连接和控制；
- 提示音播放正常；
- “时间到”全屏黑屏正常。

不要因为进入新一轮就推倒这些通过项。

仍有一个跨屏细节：鼠标按住跨不同 DPI 屏幕拖动期间偶尔能看到内容/显示瞬时错位，松手即恢复。目标是消除拖动过程视觉不同步，同时保留最终尺寸稳定。

## ChatGPT 直接处理的局部改动

ChatGPT 已在 GitHub 发起 `Second feedback direct fixes` Windows workflow（run `34586469962`）。该流程先把补丁应用到临时 checkout，只有 fmt/clippy/test/locked release build 全部成功后才会生成并推送产品提交 `fix: streamline blackout audio and remote resilience`；失败半成品不会推入产品源码。

直接补丁范围：

1. 增大 Settings / PC Remote ScrollView 的内容-滚动条 gutter；
2. 全屏 TimeUp 遮罩期间检测 ESC 并解除应用遮罩；
3. 提示音可听播放最多 10 秒；
4. 提示音复制保持原 basename，用每个 prompt slot 的子目录避免同名互相覆盖；
5. mobile Web Remote 的主动 `ppt.*` 操作统一在服务端先解除 FlyPPTTimer TimeUp 遮罩，再执行原命令，`ppt.refresh` 不改变状态；
6. Web Remote 状态轮询容忍短时失败，online / focus / 回前台立即重连；不自动重试可能非幂等的 POST 命令。

Codex 开工前先确认这条 workflow 成功并拉到它产生的产品提交；若失败，按 `CODEX_TASK.md` 先修直接补丁的编译/测试问题，不要在旧基点另写一套相同实现。

## Codex 负责的深层整合

### 1. 跨屏拖动中瞬时错位

需要真实 Windows 混合 DPI 复现和原生消息/Slint 布局交叉观察。只修 drag-in-progress 视觉同步，不重做已经通过的无累计尺寸漂移逻辑。

### 2. 其他文本弹窗真机回归

“作者的话”已通过。继续覆盖 Settings/PC Remote/更新/错误/确认/Web confirm 等文本弹窗的中英文和可用 DPI，发现具体裁切才改。

### 3. 文稿切换成为一个串行原子操作

用户已复现 A 正在 Slide Show 时直接切换 B：B 置顶而 A 仍在放映，timer 仍被 A 占用。

必须在 Presentation STA worker 中把“结束旧放映 -> 打开/激活目标 -> 可选启动目标放映”做成单个高层操作。不要从 Web 连发 EndShow+Open。切换不能关闭/保存旧文稿，新放映必须让 timer/rule 正常接管。

### 4. 手机演示文件列表管理

增加隐藏/恢复、删除规则、添加已打开文稿到规则列表、自定义排序。删除绝不删除磁盘文件；隐藏不等于禁用规则；排序持久化。不得把手机页面变成任意 PC 文件系统浏览器，也不默认加上传协议。

### 5. Timer 页码

新增“显示当前页/总页数”设置，默认开启。有效演示状态下主时间下面显示如 `1/23`；无有效页数时不显示 `0/0`。复用现有 PresentationState，不新建 Office 查询线程。

### 6. Timer 内容自适应与 Settings 实时预览

现有 `expand_timer_windows_if_needed` 只扩大并把扩大值写回配置，需收口为真正内容适配，而不是再叠 resize loop。

建议 Width/Height 作为用户最小/基准尺寸；实际窗口可因 time/page 内容自动增大并缩回基准，自动实际尺寸不写磁盘。

Settings 修改 width/height/font size/page-count 开关时立即预览 Timer；Cancel/放弃恢复 applied，Apply 才持久化。只给这些外观字段实时预览，不把 Settings 全部变成立即应用。

### 7. Remote 稳定验证

先验证 ChatGPT 的客户端断线容错是否足够。只有真实复现服务端故障再改 `remote.rs`；不引入 WebSocket/公网/复杂心跳框架，不降低 token 鉴权。

## 流程优化原则

这轮应把“用户明确下一步动作能够安全包含前置清理”的地方合并：

- 任何 mobile Presentation 主动操作可先退出 FlyPPTTimer TimeUp 遮罩；
- 切换到另一文稿时先结束旧放映；
- 关闭正在放映的目标文稿时，在确认后先结束其放映再关闭；
- Start/Restart/Resume 继续沿用已有 TimeUp 清理；
- Force Quit 仍需危险确认；
- Previous/Next/Goto 在没有放映时不自作主张启动放映。

原则是减少“先清状态、再点真正想做的操作”的重复步骤，但不能把破坏性动作藏进普通按钮。

## 测试和安全边界

Office 只使用可丢弃临时文稿。不要打开/保存/关闭/强退用户真实文稿。

不要关闭杀软、防火墙或 Remote token。不要为了测试建立大型 GUI 自动化框架。

真实桌面能由 Codex 验证的由 Codex 做，最终只把必须依赖用户手机/现场感受的最少项目留给用户。

完成本轮所有源码后只生成一个最终绿色测试包，不让用户轮流试中间包。

## 持久约束

- Rust + Slint V1，不重写技术栈；
- PC Remote “演示文稿”页仍只做规则管理，不恢复已删除的 PC 放映控制按钮；
- Web/mobile Remote 保留完整演示控制；
- 保留 F3 Pause/Resume、Settings merge/import/reset、Remote token、端口编辑保护、规则多选、Office range restore、COM unknown sample 等已修行为；
- 不升级版本/依赖，不创建 Release/Tag，不合并默认分支，未经批准不强推。