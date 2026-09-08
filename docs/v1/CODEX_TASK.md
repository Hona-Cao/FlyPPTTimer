# 当前任务：UX-04 源码审核通过，手测包已就绪；UX-05 等待真实复现

当前分支：`codex/v1-06-manual-test`

## 审核结论

UX-04“已有交互可用性”源码审核通过，当前实现可以保留，不要重做。

确认可保留：

- `RemoteButton` 使用 Slint `FocusScope` + `forward-focus` 接入框架焦点导航；
- 获得焦点时显示清晰焦点边框；
- Enter / Space 复用现有 `clicked()`，没有新增第二套命令回调；
- `FocusScope.enabled` 直接绑定按钮 `enabled`，禁用按钮不接受键盘激活；
- PC Remote 退出确认层打开后聚焦取消按钮，并通过现有 `confirm-exit` 禁用背景可交互控件；
- 设置批量层打开后聚焦现有时长输入框，并通过现有 `batch-open` 禁用背景设置、规则和底部按钮；
- 关闭弹层后只恢复原有布尔状态，不增加焦点历史、恢复栈、重试或额外状态机；
- 标准 `LineEdit / ComboBox / CheckBox / Button` 保持原生交互；
- 只读路径继续使用原有 `read-only`，没有新增复制按钮、Tooltip、弹窗或菜单；
- Remote 命令编号、参数和 Rust 业务逻辑未修改。

UX-04 实现阶段实际只运行一次 `cargo check`，验证强度符合当前风险分层策略。

随后用户要求提供本地手测版，进入明确的 package 阶段后统一执行了一次：

```powershell
cargo fmt --check
cargo clippy --all-targets --all-features -- -D warnings
cargo test
cargo build --release
```

Codex 记录四项均通过，`cargo test` 为 37 passed、0 failed、1 ignored（既有 Office 真机测试）。这次完整验证属于本地手测包打包阶段，不代表后续每轮都需要重复四项命令。

当前本地手测包：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX04-401f650-win-x64.zip`。程序内部版本仍为 1.13.0；这是 review 手测包，不是正式 Release。

## 仍需用户真机手测

请使用最新 UX-04 本地手测包验证：

1. PC Remote 用 Tab / Shift+Tab 移动时，焦点是否清晰，顺序是否基本符合视觉顺序；
2. 聚焦 `RemoteButton` 后 Enter / Space 是否只触发一次正确命令；
3. disabled Remote 按钮是否无法被鼠标或键盘误触发；
4. 退出确认层打开后，焦点是否落在取消按钮，背景控件是否不可操作，取消后继续 Tab 是否正常；
5. 设置批量层打开后，时长输入框是否聚焦，背景设置/规则/底部按钮是否不可操作；
6. 只读路径是否仍可选取复制但不能编辑；
7. UX-01～UX-03 的配色、布局、Remote 缩放和信息层级是否没有回退。

如果用户报告具体交互问题，只针对可复现症状做定向修正。

---

## UX-05：当前不要盲改 Timer / 多屏显示

`docs/v1/UX_OPTIMIZATION_PLAN.md` 对 UX-05 的前提仍然有效：先获得用户真机复现条件，再定向处理 Timer、大屏或“时间到”显示问题。

当前用户尚未提供新的 Timer / 多屏具体故障，因此 **UX-05 暂不进入代码修改阶段**。

不要仅凭源码猜测并修改：

- Timer 默认尺寸、字号、圆角、位置或配色；
- 六套 Timer 配色和用户自定义颜色；
- 大屏显示策略；
- “时间到”窗口；
- 多屏目标选择；
- DPI / 窗口创建或移动时序；
- 计时核心行为。

只有用户提供真实复现后才启动 UX-05。至少记录与问题直接相关的：

- 哪个窗口（Timer / 大屏 / 时间到）；
- 单屏或多屏、主屏/副屏关系；
- 系统缩放比例或明显的跨 DPI 场景；
- 实际窗口尺寸 / 字号 / 时间文本（例如是否包含小时位或负号）；
- 看到的具体异常，例如截断、偏移、跳动、跨屏错位或任务栏问题。

不需要为了“完整”收集额外设备矩阵；有能稳定复现问题的最小条件即可。

---

## 持续编码约束：禁止过度防御

- 不增加 SHA / Hash / checksum / 完整性验证；
- 不重复验证同一事实；
- 不增加无必要重试、双重读取、备用路径、备份/恢复链或额外状态机；
- 不制造覆盖率、焦点、截图、DPI 或设备测试矩阵；
- 不为真实未复现的问题预先增加 fallback；
- 外部输入只保留满足既有契约所需的最小校验；
- 目标仍是：**最少必要逻辑 + 明确行为 + 明确失败**。

## 持续测试策略：按风险分层

- 小改动 / 定向修 Bug：只跑直接相关的最小测试；没有必要时可不额外跑 `cargo check`；
- Timer 核心 / 配置 / Remote 协议等逻辑变化：只跑对应模块或行为的定向测试；
- 一次任务完成、准备交给 ChatGPT / 用户手测：最多一次 `cargo test`，已有足够定向验证时可跳过；
- 只有明确进入 merge / package / release 收口阶段时，统一运行一次：

```powershell
cargo fmt --check
cargo clippy --all-targets --all-features -- -D warnings
cargo test
cargo build --release
```

## 真机验收边界

PowerPoint / WPS、多屏 / 跨 DPI、实际声音、Slint 真实显示和交互、手机 Remote 真实连接，必须由用户真机手测。自动测试、源码审核或编译通过不能替代这些验收。

## 当前动作

- UX-01：用户已确认配色；
- Remote 可缩放补充、UX-02、UX-03、UX-04：源码审核通过；
- UX-04 本地手测包已就绪，等待用户真实键盘/焦点/窗口交互验收；
- UX-05：等待用户提供真实 Timer / 多屏复现条件，不做预防性修改；
- Codex 当前停止修改，等待用户新的具体手测反馈；
- 不创建 Release / Tag，不生成正式发布包。