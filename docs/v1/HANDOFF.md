# Current RC3.2 review handoff

ChatGPT directly implements USER_FEEDBACK_20260912_3.md from the tested RC3.1 source `5bc223bd7bea12339311bac4494f369bb3d363b1`. Source is on an independent review branch; Codex stays paused; do not merge into main. Prior unmentioned items are provisionally accepted by the user. Latest automation/binary identity are in the RC3.2 workflow and package BUILD.txt; physical acceptance is still pending for the newly changed UI/gestures. See RC32_MANUAL_TEST.md. Version/dependencies are unchanged.

---
## Historical handoff below (not the current task)

# FlyPPTTimer V1 — 当前交接

## 最新状态：a644e74 第二轮整合主体接受，RC-3.1 仍有 3 个 P1 + 1 个 P2 需收口

日期：2026-09-12。Review 分支：`codex/v1-06-manual-test`。版本继续为 `1.13.0`，Rust `1.92.0`。

唯一当前实现指令：`docs/v1/CODEX_TASK.md`。
用户第二轮需求：`docs/v1/USER_FEEDBACK_20260911_2.md`。
用户批准偏差：`docs/v1/APPROVED_PRODUCT_DEVIATIONS.md`。

## 当前产品审核基点

Codex 家用 Windows 第二轮整合产品提交：

`a644e74b82bf5a65474f027ef492fd6646456ac3`

提交信息：`fix: integrate presentation switching and adaptive timer previews`

该提交从 `b193bef...` 开始，完成了 U07–U11 主体：异文稿放映切换、mobile 规则隐藏/恢复/删除/添加/排序、Timer 页码、自适应尺寸、限定字段 Settings 实时预览，以及弹窗/深色系统主题的若干真机修正。

Codex 报告最终自动检查：**72 passed / 0 failed / 3 ignored**，fmt/clippy/release/node 检查通过；家用机真实 WPS、125% 同 DPI 双屏和普通 Edge 做了较完整验证。原生 Microsoft PowerPoint 因 `PowerPoint.Application` 被 WPS COM 接管未独立验证；家用机两屏均 125%，因此不能验证 150%/125% 的 drag-in-progress 残余。

ChatGPT 已对 `a644e74` 做源码终审。主体实现方向接受，但**尚不能进入最终用户包验收**，原因只剩 `CODEX_TASK.md` 中 RC-3.1 的四项。

## RC-3.1 四个审核项

### 1. P1 — ESC 短按可靠性

现实现每 100ms 读取 `GetAsyncKeyState(VK_ESCAPE)` 的当前按下高位；Codex 自己的真实桌面记录已经出现“瞬时 SendKeys ESC 没捕获，按住约 250ms 才通过”。用户要求普通按 ESC 即关闭 TimeUp 黑屏，不能要求刻意长按。

本轮必须最小修正短按捕获，并用约 30–70ms keydown/up 验证；无 TimeUp 时不得产生 FlyPPTTimer 副作用。

### 2. P1 — already-showing target 的“切换”提前返回

`Session::open(path)` 当前在发现目标自己的 Slide Show 已存在时直接返回，返回发生在查找/Activate 目标 presentation 之前。

因此 A 正在放映、电脑端 ActivePresentation=B 时，手机点 A“切换”可能返回“目标文稿已在放映”却没有把 ActivePresentation 切回 A；后续 Next/Previous/Goto 会按现有 known-target 安全规则拒绝匹配，而不能控制 A。

修复必须让“切换到已在放映的目标”完成目标激活，但不重启/停止同一目标的放映，不削弱 known-target 安全。

### 3. P1 — CloseActive 真实 COM worker 超时

Codex 本轮真实测试末尾已经遇到 CloseActive worker timeout，并明确没有把关闭路径写成通过。必须用可丢弃文稿区分 managed/unmanaged、clean/dirty、show/no-show 找到阻塞位置。

已有明确“关闭且不保存”确认的 mobile 路径可以按既有产品语义避免隐藏 Office 保存提示；没有足够确认的用户 dirty 文稿不能静默丢弃。失败/拒绝之后 worker 必须继续可用。不要做多 worker、强杀或复杂重试架构。

### 4. P2 — desktop 新增规则破坏 mobile 自定义顺序

`FileRule::default().mobile_order=0`。mobile addOpen 已显式按末尾追加，但 Settings 和 PC Remote 添加文件仍 `..FileRule::default()`，因此桌面新增 D 可能插到已排好的 A/B/C 前面。

所有新增规则入口统一为追加 mobile 顺序末尾，保存/重启后保持；Settings stale merge 继续保留手机端 mobile_hidden/mobile_order 外部修改。

## 已接受、不要重做的内容

- 用户已真实确认：最终跨屏尺寸无累计异常、ScrollView 不遮挡、作者弹窗正常、手机 Remote 基本连接控制正常、音频基本播放正常、TimeUp 全屏黑屏正常。
- `fdadde3...`：更大 Scroll gutter、ESC 功能基础、10 秒提示音上限、原 basename/slot 复制、mobile 主动 `ppt.*` 自动解除 TimeUp、短断线客户端恢复逻辑。
- `a644e74...`：U07 异目标原子结束旧放映再切目标的主体、mobile 规则管理模型、`show_slide_numbers` 默认 true、普通/镜像/大屏页码、Timer 内容自适应、不把瞬时尺寸写回配置、Width/Height/FontSize/Page 开关限定实时预览、Cancel/Apply 语义、Settings merge 新 mobile metadata。
- PC Remote “演示文稿”页仍只做规则管理；不要恢复 PC 放映控制按钮。
- Remote 不恢复随机端口 UI。
- 不重新引入 PowerShell 音频回退。
- 不改 sound+speech 既有优先关系、10–100 opacity、已通过的 token/port/import/reset/F3 等旧修复。

## 当前仍属环境/用户最终验收边界

这些不是 RC-3.1 要猜修的代码项：

1. **150%/125% 真混合 DPI**：鼠标按住跨边界时偶发的瞬时内容错位仍未解决；家用机两屏都 125%，不允许凭猜测改。最终需回办公室或其他真混合 DPI 环境确认。
2. **真实手机长时恢复/触控体验**：约 10 分钟、锁屏/后台/Wi-Fi 断开恢复、mobile hide/restore/move/delete/add 的触控体验。桌面 HTTP 和普通浏览器已有证据。
3. **独立原生 Microsoft PowerPoint**：家用机 COM 被 WPS 接管，WPS 已测，原生 PowerPoint 仍需其他环境。
4. **150% 长文本边界**及超过 10 秒提示音实际听感，如用户使用该场景时再确认。

## 流程与安全边界

- 当前只执行 `CODEX_TASK.md` 的 RC-3.1，不做新一轮全仓库问题搜寻。
- Office 测试只用可丢弃临时文稿，不操作用户真实文稿。
- 不关闭杀软、防火墙或 Remote token。
- 不升级版本、依赖或技术栈；不创建 Release/Tag，不合并默认分支，不强推。
- 四项通过后只生成一个新的最终 review 绿色 ZIP；不要让用户测试中间包。

完成 RC-3.1 后更新 `CODEX_RESULT.md`、commit、push，然后停止等待 ChatGPT 审核。