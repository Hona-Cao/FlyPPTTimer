# 当前任务：接收 ChatGPT 直接修复，收口 Office 风险并交付绿色测试版

日期：2026-09-11
分支：`codex/v1-06-manual-test`
直接修复源码：`c0b5e37a71849f451e4c71cc7b158791165d436b`
格式收尾源码：`96273ddf0e34fe73902f9fcd3ea4a6f356138c18`
程序版本仍为 `1.13.0`，不升级依赖、不改 UI 布局。

## 新授权与状态

用户最新明确要求：ChatGPT 先直接修改可确定的问题，不能在此环境可靠完成的交给 Codex，提供绿色测试 EXE，并说明需要用户手测的内容。本任务取代此前“没有反馈就停止”的任务，不代表可以重新全仓库大重构。

旧 RC-2 55 项通过记录属于旧源码，不能作为本次新增修复已通过的证据。先读取当前 GitHub Actions `RC portable review` 的最新结果，以及 `docs/v1/DIRECT_FIXES_20260911.md`（若已存在），再决定是否需要补跑。Windows runner 不含真实 Office 工作流与用户多屏环境，不等于 GUI/Office 验收。

## A. 已直接修改，不要重写

修改文件仅 `src/presentation.rs`。

### A1 空路径全屏状态反复重开计时

旧 `same_path("", "")` 必定为 false。连续 `observe(true, "", config)` 会每次产生 Start，影响未取得文稿路径的全屏白名单场景，也会在 A→未知→A 的歧义读取中清零已有轮次。

当前处理：已经处于 showing 且本次路径为空时，保持轮次和最后已知身份，不发 Start/Stop/Reset。真正 false 离开后仍按原配置结束；第一次无路径全屏仍只开始一次；确实切到不同的非空路径仍按原行为开始。

### A2 单一放映窗口不等于目标正确

旧选择器即使目标明确是 B，只要只剩 A 一个放映窗口，也会退回 A。现在：非空目标必须精确按原 same_path 规则匹配；找不到时拒绝控制，状态仍保留有放映；只有目标不可得时才能对唯一窗口 fallback。零窗口仍为未放映。

新增五个测试在 `presentation::direct_fix_regressions` 中。保留全部已有测试，不删测、不改基线掩盖错误。需要进一步验证时，可把这些新测试追加到独立旧源码 worktree 中做反证，但不得回滚用户工作区或覆盖当前分支。

## B. 交给 Codex 的定向整改/验证，不交给用户调试

### B1 放映设置修改的错误路径与 Saved 标志（源码确认存在遗漏，尚未做 Office 故障注入）

位置：`Session::start_show()`。

当前先执行 RangeType / StartingSlide / EndingSlide 的多个 `put(...)?`，后面才恢复原值。中途任一 put 失败会提前返回，跳过恢复。恢复阶段忽略 put 的返回值；只要原来 was_saved，就仍尝试 `Saved=true`。这与 v0.30.2 的 finally + restored 全部成功再清除 dirty 标志不同。

要求最小修复：
- 把临时修改及 Run 纳入保证走清理的局部路径；不论准备失败还是 Run 失败，都对已经成功快照的字段做恢复尝试。
- 只有原始快照完整、原文稿原本 clean、所有恢复成功，才允许恢复 clean 标志；原有未保存文稿绝不能被标成已保存。
- 未能取得完整快照时不要盲目改持久化放映设置；参照 v0.30.2 行为。
- 保留原始错误，清理错误需记录，不用“操作成功”掩盖。
- 不改变文稿关闭/强退语义，不引入通用 COM 框架或大量 mock。
- 用一次性临时 PPTX 验证从头/当前页正常路径、原本已修改的文稿仍保持 dirty，以及可安全构造的失败路径。未能注入的路径如实记录，不声称已现场发生用户数据丢失。

### B2 COM 状态读取失败与退出等待（待确认风险，不可直接称已复现）

位置：`Session::read_state()` 的 Err 默认状态，以及 `PresentationService::drop()` 中无期限 join。

先在安全临时文稿/可控 busy 条件下验证：一次 COM 读取失败是否让已有放映被误认为结束并重置 Timer；Office 等待对话框时托盘退出是否被 COM worker 无期限拖住。

只有明确复现或可以建立确定回归时做最小修复。不要为了“理论防御”新增复杂缓存状态机/重试框架；尤其不能把旧放映状态永久当成当前事实，导致真正结束后永不停止。无证据时写明未复现及现有边界。

### B3 实机回归由 Codex 尽量完成

- 普通双击启动（非 --show-settings），Timer 右键/托盘打开设置和 PC Remote、同进程反复重开、托盘退出。
- 开启全屏白名单，Office 全部关闭时浏览器全屏保持一段时间，确认计时不是反复清零；离开后符合设置。
- 临时 A 放映、活动 B 无放映时，不错误翻动 A；A→目标暂不可得→A 不应清零。
- PC Remote 端口停顿输入、Ctrl/Shift、批量保存，以及设置草稿/Remote 不同规则修改的整合。
- PowerPoint 与 WPS 分别执行临时文稿核心流程；不得使用、保存、关闭或强退用户真实文稿。

用户只做残余真实设备/听感/视觉确认；不能操作的项目记录原因，不把所有历史测试重新派给用户。

## C. 绿色版交付

当前新增 `.github/workflows/rc-portable-review.yml` 是为本次用户索要 EXE 的限定分支构建，不改旧 CI、默认分支、Release 或 Tag。只允许 read 内容权限，按锁定工具链运行 fmt / clippy / test / release，产出带源码 SHA 的 artifact。它不代表 Office/GUI 实测通过。任务结束可移除该临时 workflow，避免把它扩大成新的发布系统。

Codex 若修改源码，必须为最终源码重新构建绿色版；不能沿用旧 EXE 只改文件名。优先使用原有发布脚本；仅需绿色版时不必重复安装实验。

交付必须包含：FlyPPTTimer.exe、干净默认配置、BUILD.txt（真实源码 SHA/版本/验证边界）和简短手测说明。目录名或 ZIP 名含短 SHA，避免所有包同为 1.13.0 而混用。

先确认 exe 能在不安装开发工具的 Windows 11 测试环境启动；记录缺失 DLL 或运行库，不将“编译完成”等同于独立可运行。不得携带真实 token、用户规则、私人路径和日志。不要发布正式 Release，不创建 Tag，不覆盖用户原目录。

## D. 验证与完成

读取 `rust-toolchain.toml` 并沿用 Rust 1.92.0 / Cargo.lock；检查本机实际 MSVC/SDK/rc.exe，不照抄之前 E/J/F 盘路径。保留用户未提交文件。

发生源码修复后统一：

```powershell
cargo fmt --all -- --check
cargo clippy --locked --all-targets --all-features -- -D warnings
cargo test --locked
cargo build --locked --release
```

将实际结果追加到 CODEX_RESULT 顶部，不覆盖历史；更新 RC_CANDIDATE 的源码和产物；沿用 RC_MANUAL_TEST 五组入口并只保留真正还未验证的操作。commit + push 后停止，交 ChatGPT 审核。若已经获得针对相同源码的可信构建/测试结果，可引用，不为纯文档修改重复构建。
