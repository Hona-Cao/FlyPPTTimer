# 当前任务：RC-1.1 多放映状态误判收口

当前分支：`codex/v1-06-manual-test`

审核基点：`988358cef27c93b696fb65915fc7e86ecd7c0fe8`

## 审核结论

RC-1 的目标窗口选择修复方向正确并已通过 ChatGPT 源码审核：

- Previous / Next / Goto / Black / White / Restore / EndShow 不再任意控制 `SlideShowWindows.Item(1)`；
- 优先用 `ActivePresentation.FullName` 按完整路径匹配放映窗口；
- 单窗口时允许安全 fallback；
- 多窗口无法匹配时命令应明确失败而不是误控第一窗口；
- 51 passed、0 failed、1 ignored，Release build 与现有打包脚本通过。

不要重做上述内容，不做 UI 改造，不再做新的全仓库扫描。

---

# 1. 唯一需要修复的 P1：目标不明确时仍必须保留“有放映正在运行”的事实

## 已确认问题

当前 `read_application_state()`：

1. 读取 `ActivePresentation.FullName`；
2. 调用 `show_window_for_target()`；
3. 当存在多个 `SlideShowWindows`，但活动文稿不属于这些放映窗口时，选择器返回 Err；
4. `Session::read_state()` 的 Err 分支会构造一个大部分字段为默认值的 `PresentationState`，其中 `slide_show_running == false`。

这会把“存在多个放映窗口，但当前无法确定目标窗口”错误表示成“没有放映”。

主循环把 `presentation_state.slide_show_running` 交给 `PresentationLifecycle::observe()`。如果此前自动计时已激活，这个 false 可能触发离开全屏后的 Stop / Reset，因此会造成真实计时行为错误。

v0.30.2 的行为不同：它先根据 `SlideShowWindows.Count > 0` 设置 `IsSlideShowRunning = true`，然后再尝试按活动文稿路径匹配窗口；如果匹配失败，只记录“未能按目标文稿匹配放映窗口”的错误并返回状态，**不会把正在放映改成 false**。

## 修复要求

保持 RC-1 已有的“命令安全失败”策略，但把“是否存在放映”与“是否能确定目标放映窗口”分开。

要求：

- `SlideShowWindows.Count > 0` 时，状态层必须始终保留 `slide_show_running = true`；
- 如果能按活动文稿路径找到目标窗口，继续读取该窗口的文稿、页码、总页数和 screen state；
- 如果只有一个窗口且目标路径不可得，继续允许唯一窗口 fallback；
- 如果有多个放映窗口且目标路径无法匹配：
  - 状态仍为 `slide_show_running = true`；
  - 记录清晰的目标不明确错误；
  - 不虚构当前页/当前文稿；
  - 不选择任意第一个窗口；
  - 不让该状态误触发 PresentationLifecycle 的“离开放映” Stop/Reset；
- `with_show_view()` 及具体放映命令仍保持当前安全行为：多窗口目标不明确时明确报错，不误控；
- 不改变 `start_show()`、managed ownership、Close/Exit/ForceQuit、Remote 协议、Timer、UI、DPI 或配置逻辑。

实现保持小。可以让状态读取先取得 `SlideShowWindows.Count`，再单独做目标窗口解析；也可以增加一个很小的纯逻辑状态辅助函数。不要建立新的状态机或 COM 框架。

---

# 2. 必须增加的回归验证

至少证明以下语义：

1. 0 个放映窗口 → `slide_show_running = false`；
2. 1 个放映窗口、无目标路径 → `slide_show_running = true` 且使用唯一窗口；
3. 2 个放映窗口、目标 A 能匹配 `[B, A]` → `slide_show_running = true` 且选 A；
4. 2 个放映窗口、目标 Missing → `slide_show_running = true`，目标解析为 ambiguous/error，但绝不退回第一窗口；
5. 命令路径在第 4 种情况下仍返回错误，不发送到任意窗口。

尽量用纯逻辑测试完成，不为 COM 创建 Mock 框架。

如果实现方式不便直接测试完整 `PresentationState`，可抽出一个返回“是否存在放映 + 可选目标索引/歧义”的小纯函数，并测试上述语义。

---

# 3. 本轮禁止扩大范围

不要修改：

- Timer、提醒、声音/TTS；
- Settings / PC Remote UI；
- 已批准的固定 Remote 端口和 PC Remote 规则页产品调整；
- Web Remote；
- 多屏/DPI；
- 发布脚本、版本号、Cargo 依赖；
- 现有 RC 截图；
- 其他代码质量或架构问题。

没有与本问题直接相关的证据，不做额外修改。

---

# 4. 验证与完成方式

完成后统一运行一次：

```powershell
cargo fmt --all -- --check
cargo clippy --locked --all-targets --all-features -- -D warnings
cargo test --locked
cargo build --locked --release
```

不必重复 Inno Setup 打包，不必重新生成截图。

然后：

1. 在 `docs/v1/CODEX_RESULT.md` 顶部记录 RC-1.1 根因、修复和最终测试数量；
2. commit；
3. push 到 `codex/v1-06-manual-test`；
4. 停止编码，等待 ChatGPT 审核；
5. 不创建 Release / Tag。

本轮通过后，默认进入最终 RC 人工验收，不再继续主动扩大代码整改范围。
