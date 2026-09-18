[记录目录](../README.md) · [原始汇总](../../v1/CODEX_RESULT.md)

# UX-04 已有交互可用性结果

日期：2026-09-08

Review 分支：`codex/v1-06-manual-test`

任务基点：8d11f63。保留 UX-01～UX-03 已通过的配色和布局。

## 实际修改

本轮仅修改 `ui/app-window.slint`。

- `RemoteButton` 使用 Slint 1.17.1 的 FocusScope 与 forward-focus，接入框架 Tab 导航。参考本地依赖中 Fluent Button 的焦点实现，没有引入另一套组件库。
- 获得焦点时显示内侧 2px 焦点边框：蓝色主按钮使用白色，其余按钮使用蓝色。保留既有鼠标 hover/pressed/clicked。
- Enter/Space 按下时接受事件，松开时调用现有 clicked；不在按下重复事件中发送命令。FocusScope 的 enabled 绑定原按钮 enabled，禁用按钮不接受键盘激活。
- PC Remote 退出确认层创建时聚焦取消按钮；现有背景按钮、输入框、列表点击和页面切换均叠加 `!confirm-exit` 启用条件。保留遮罩、取消行为和 command(13, "") 确认退出语义。
- 设置批量层创建时聚焦已有时长输入框；背景导航、普通设置字段、规则选择/编辑/操作与底部三个按钮叠加 `!batch-open`。FieldRow 只增加一个 Slint 层 interactive 输入，将现有弹层布尔值传给标准控件；没有修改 Rust 设置模型。
- 关闭弹层后依原有布尔值重新启用背景，不增加焦点恢复对象、历史栈或恢复链。后续焦点移动由框架处理。

只读路径仍绑定 LineEdit.read-only，标准 LineEdit/ComboBox/CheckBox/Button 保留其键盘和输入行为；本轮未增加滚轮行为、复制按钮、Tooltip、Esc 全局语义或新确认步骤。

## 实际验证与限制

`cargo check`：通过，仅运行一次。没有修改 Rust，未运行全量测试、Clippy、Release build 或新增焦点/视觉快照测试。

本轮所需 FocusScope、forward-focus、焦点边框、键盘回调和弹层初始化聚焦均可由当前 Slint 编译。不建立自定义 Tab 顺序或底层事件系统；Tab 的真实顺序、关闭弹层后的焦点落点，以及按键按住/松开行为仍需用户验收。

## 仍需用户手测

1. Remote 通过 Tab/Shift+Tab 移动时可交互按钮焦点是否清晰，顺序是否基本符合视觉顺序。
2. 聚焦按钮后 Enter/Space 按下并松开是否只执行一次原命令；禁用按钮是否不接受鼠标/键盘操作。
3. 退出确认打开后焦点是否落在取消按钮，Tab 是否只到达弹层内操作，背景不能误操作；取消后继续 Tab 是否可用。
4. 批量设置打开后时长框是否聚焦，背景设置/规则/底部按钮不可操作；批量保存/取消保持原有行为。
5. 非弹层状态下只读路径仍可选取复制但不可编辑，禁用设置控件仍清晰可辨。
6. UX-01～UX-03 的配色、布局和窗口缩放效果没有回退。

编译通过不代表真实键盘、焦点和窗口交互已经验收。本轮未生成新手测包，未创建 Release/Tag。推送后停止等待审核，不进入 UX-05。

## UX-04 本地手测包交付

用户要求提供手测版，进入本地打包阶段后统一执行一次：`cargo fmt --check`、`cargo clippy --all-targets --all-features -- -D warnings`、`cargo test`、`cargo build --release`，均通过；测试为 37 passed、0 failed、1 ignored（既有 Office 真机测试）。

手测包：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX04-401f650-win-x64.zip`；同名目录内可直接运行 EXE。程序内部版本仍为 1.13.0，包标识为 v1.13.0-UX04-401f650，包含 UX-01～UX-04。附版本和手测说明。等待真实手测与审核，未创建 GitHub Release/Tag。

---
