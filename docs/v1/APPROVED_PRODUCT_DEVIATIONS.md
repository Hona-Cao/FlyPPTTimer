# V1 用户已批准的产品基线覆盖项

本文件记录 v0.30.2 基线之后用户明确批准的行为调整与补充要求。它记录产品要求，不代表每项已经实施或验收；最新实现状态见 HANDOFF、CODEX_RESULT 和 USER_FEEDBACK_20260911。

与 `V1_BASELINE_CHECKLIST.md` 冲突时以本文件为准；未列出的功能、默认值、文字与行为继续按旧版基线。不得用旧基线恢复用户明确要求删除的界面。

## 1. Remote 设置不再暴露“使用随机端口”

用户 2026-09-09 明确要求：设置页不显示随机端口控件，优先使用已保存固定端口。固定端口不可用或无权限时，允许系统分配可用端口，将实际端口写回配置并提示地址变化。

旧配置的 `UseRandomPort` 字段可以兼容读取，但不得因此恢复可见控件或每次随机端口的产品逻辑。

## 2. PC Remote“演示文稿”页只保留文件规则管理

用户 2026-09-09 明确要求该页复刻设置页文件规则管理，不承担放映状态展示和演示软件控制。

保留文件名/路径、时长、模式、启用状态，添加、删除、清空、保存，以及普通单选、Ctrl 多选、Shift 范围选择和批量时长/模式设置。

不恢复“演示软件已运行”状态区；不恢复打开、从头/当前页放映、翻页、跳转、黑白屏、结束放映、关闭文稿、退出演示软件等 PC 页控制按钮。

手机/浏览器 Web Remote 仍保留 v0.30.2 的完整 `ppt.*` 控制能力。这项调整覆盖旧基线第 13 节的 PC 放映控制界面要求。

## 3. 单一主要连接入口，支持电脑连接手机热点

用户 2026-09-11 明确要求：远程联机只需要一个入口，并支持手机热点。

主要界面给一个可用连接 URL、与其一致的二维码和复制内容，不让用户从一串本机/网关/虚拟网卡地址中猜选。必须使用电脑在实际局域网/手机热点上被分配的地址；不能将手机网关、DNS、回环或无关网卡当作手机访问地址。

同一 HTTP 服务供手机和电脑浏览器使用，不增加第二套联机系统；本要求不表示只允许一个客户端。保留必要高级诊断和服务/令牌管理，但避免重复连接选项。

真实手机热点与防火墙条件仍需实测；不能降低鉴权或关闭安全防护作为实现方式。

## 4. 设置和说明弹窗的具体视觉要求

用户 2026-09-11 明确要求：

- 确定、取消、应用三按钮统一高度、间距，右侧和底部有留白；
- 时长/模式编辑卡紧凑，不留大片上下冗余；
- 滚动条靠近内容区域外缘，独占通道，不与内部控件重叠；
- 收起的说明文字展开后，应按真实文字/字体/换行宽度适配，超出窗口时可完整滚到末尾；
- 说明弹窗不再有顶部蓝色装饰条。

不改变六页设置结构，不以此授权全局换主题。普通按钮焦点/选中态仍需清晰。

## 5. 无脚本音频与全屏到时黑屏

用户 2026-09-11 报告提示音疑似触发 PowerShell 且被安全软件拦截。音频应使用 Windows 原生接口，不运行隐藏/编码 PowerShell 或其他脚本解释器；保持现有音频和 TTS 功能，不靠删除音频支持或绕过检测处理。

用户明确“黑屏并显示时间到”是覆盖整个屏幕的纯黑不透明遮罩，不是小卡片。应遮住演示输出，包含扩展屏，不允许普通翻页继续露出演示内容；明确的主持人重置/解除/下一轮操作可以恢复。不要将其实现为锁定 Windows、禁止系统安全退出或强制杀死其他应用。

保留“仅提示”和“退出放映”各自语义。黑屏的最终覆盖、持续与恢复必须经真实显示器验证，不能用设置了窗口尺寸来替代。

## 6. 第二轮演示、列表、页码与尺寸语义

用户 2026-09-11 的完整批准见 `USER_FEEDBACK_20260911_2.md`（U07–U11）及当前 `CODEX_TASK.md`：切换异文稿须在单 STA 操作内先结束旧放映，不因切换保存或关闭旧文稿；手机列表允许独立隐藏、恢复、仅删除规则、添加已打开文稿和持久排序；页码默认开启；Timer 宽高成为手工最小基准，内容自动尺寸不写回基准；仅宽、高、字号、页码开关实时预览，Apply 才持久化，放弃恢复应用值。

同轮直接修正允许 ESC 和主动 mobile `ppt.*`（不含 refresh）解除到时遮罩，单次提示音可听上限 10 秒，TTS 不受限。这些显式新要求覆盖前述与其冲突的旧语义，其余基线不变。

## 使用规则

只有用户批准的变化写入本文件；不作为增加新功能的授权。实现与验证状态必须另行如实记录。未来用户再次明确改变要求时，先同步本文件与任务书，避免后续按旧需求回改。


## 7. RC3.2 explicit user approval (2026-09-12)

1. Automatic sizing (default) measures current time/page content and fonts; width/height settings are hidden. Custom sizing uses the saved exact width/height, shows the fields, and does not silently grow. Too-small custom sizes can clip; switch to Automatic for full fit. Custom dimensions survive switching modes.
2. Time and page rows share one vertically centered block with equal top/bottom layout margins. Page size and color follow time by default, with independent overrides; italic, left/center/right alignment and above/below placement are configurable. These fields preview together; Apply saves and Cancel discards. The page-width reserve prevents jitter during pagination.
3. A display occupied by the full-screen timer is excluded from small overlays, including mirrored overlays, show/hide hotkeys and refresh. Disabling/closing the large timer restores eligible small overlays. Display selection still requires an extended screen.
4. The controlled mobile list includes only explicit file rules. Open unlisted documents are offered separately under Add open file. Remove file requires confirmation, removes control membership, never deletes/saves/closes the disk or Office document. Backend rejects subsequent direct remote controls for unlisted targets. Global quit refuses unknown/mixed/shared Office processes rather than kill removed files.
5. Name sorting uses Windows StrCmpLogicalW (numeric-aware, case-insensitive locale collation); size uses actual bytes; modified time uses actual file metadata. Both directions are available; unavailable metadata stays last. A manual move switches to manual sorting, persists compact order, and preserves PC list order. Hidden files remain controlled; removed files do not.
6. Keyed mobile rows animate movement, mark the moved row, and support stationary long-press drag, a drag ghost, neighbor transitions and edge scroll. One command is submitted on drop, never on cancel. Stale orders are rejected. Normal swipe scroll remains available. Text selection/copy/context menus are disabled; numeric fields remain editable. Reduced-motion preference is respected.
