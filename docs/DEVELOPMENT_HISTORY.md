# Development history / 真实开发迭代记录

[English home](../README.md) · [中文首页](../README.zh-CN.md) · [Changelog](../CHANGELOG.md)

从 v0.30.2 到 v1.13.1，开发和测试并未停止。早期工作位于 review/RC 分支；本次发布把已验收分支的**实际提交链完整并入 main**，不压缩成一个快照、不修改作者、不补造过去日期。下表把分散的过程整理为可阅读的入口。阶段名称是历史开发/手测标识，不表示每一项都曾是公开 Release。

Work continued on review branches between v0.30.2 and v1.13.1. Publication preserves the accepted branch's real ancestry, rather than squashing the work into a snapshot or inventing backdated activity. These are development/test milestones, not a claim that every stage was a public release.

Dates below are the original author dates normalized to **UTC**. A linked commit can be a representative implementation or a contemporaneous review record; inspect its message/diff for the exact scope. Some changes span multiple commits.

| Stage / 阶段 | Date (UTC) | Actual commit | Change / 变更 |
|---|---|---|---|
| V1 初始重构 / Rust + Slint foundation | 2026-09-03 | [57144e3](https://github.com/Hona-Cao/FlyPPTTimer/commit/57144e3c494618d94128d91d2f46caff851d006e) | 引入 Rust + Slint 实现并开始窗口审查。 / Introduced the V1 Rust/Slint implementation and window audit. |
| v1.06 窗口候选 / window candidate | 2026-09-03 | [68d9090](https://github.com/Hona-Cao/FlyPPTTimer/commit/68d9090636227401cd9a97008f57cb141deb54c4) | 整理窗口审查和手工测试候选。 / Prepared window-audit and manual-test candidate. |
| v1.07 / frameless window | 2026-09-03 | [b5ecf5a](https://github.com/Hona-Cao/FlyPPTTimer/commit/b5ecf5a0f54d8555944e67056ea0516c44a32087) | 仅在样式改变时恢复无边框计时窗，继续右键标题栏排查。 / Restore frameless styling only when changed. |
| v1.8.0 / presentation & windows | 2026-09-03 | [61e3158](https://github.com/Hona-Cao/FlyPPTTimer/commit/61e3158bf09009f7a3b21f56111c9e6b74b3be85) | 恢复遥控演示控制和窗口行为。 / Restore presentation controls and window behavior. |
| v1.9.0 / reopen & DPI | 2026-09-04 | [0a473f4](https://github.com/Hona-Cao/FlyPPTTimer/commit/0a473f4bd7f5898e1d1bc95fe50bd8ece9dc928a) | 窗口按需创建、重开闪烁和后续跨屏 DPI 调整的手测基线。 / Manual-test baseline for lazy windows, reopen and subsequent DPI fixes. |
| UX-01 | 2026-09-08 | [c11553e](https://github.com/Hona-Cao/FlyPPTTimer/commit/c11553ee5ef484bcbc99f683ef52ed1dd0935e45) | 统一桌面颜色和遥控按钮状态。 / Unified desktop palette and remote button states. |
| UX-02 | 2026-09-08 | [bd088f7](https://github.com/Hona-Cao/FlyPPTTimer/commit/bd088f79b2a2cfa42a2d6189b1736f6f9c32b11d) | 设置分组、信息密度和可调尺寸 Remote。 / Grouped settings fields and resizable Remote. |
| UX-03 | 2026-09-08 | [66a65bf](https://github.com/Hona-Cao/FlyPPTTimer/commit/66a65bfa28371390a8d9df6dd6a773fee07f4de6) | 提高 PC Remote 可读性和操作分组。 / Improved Remote readability and command grouping. |
| UX-04 | 2026-09-08 | [401f650](https://github.com/Hona-Cao/FlyPPTTimer/commit/401f6501fae5c387c88568201835cbd7a6c8b680) | 键盘访问和模态交互隔离。 / Keyboard access and modal interaction isolation. |
| UX-05 / DPI follow-up | 2026-09-08 | [7d970a1](https://github.com/Hona-Cao/FlyPPTTimer/commit/7d970a111e1959d13a050b04fb108888b4b754e3) | 修正设置初始尺寸和跨显示器 DPI 同步。 / Corrected initial Settings size and cross-monitor DPI synchronization. |
| UX-06 / cross-screen rules follow-up | 2026-09-09 | [46eac11](https://github.com/Hona-Cao/FlyPPTTimer/commit/46eac11c190b03ad1027f3d18ef8c3fcf08f4348) | 继续设置、遥控及跨屏规则交互修正。 / Continued Settings/Remote/cross-screen rule fixes. |
| UX-07 | 2026-09-09 | [7c039a8](https://github.com/Hona-Cao/FlyPPTTimer/commit/7c039a8d37b534c57583401712bfa9b8349ee1b1) | 设置重绘和桌面布局。 / Settings repaint and desktop layout. |
| UX-08 | 2026-09-09 | [887f051](https://github.com/Hona-Cao/FlyPPTTimer/commit/887f0510e132570fc561d0fe4fce0267ccb82bcb) | 压缩设置密度、Remote 拉伸；随后修复按钮组路由。 / Settings density, Remote resize, followed by grouped-action routing repair. |
| UX-09 | 2026-09-09 | [f6ee291](https://github.com/Hona-Cao/FlyPPTTimer/commit/f6ee291f3e0964dfe682b1c4a5928e6052035829) | Remote 规则选择和窗口位置。 / Remote rule selection and window placement. |
| UX-10 | 2026-09-09 | [62e1829](https://github.com/Hona-Cao/FlyPPTTimer/commit/62e18299c24f8c770a8117158970eea606f9ab5d) | 底部操作栏及文稿文件过滤。 / Settings footer and presentation-file filtering. |
| UX-11 | 2026-09-09 | [e962010](https://github.com/Hona-Cao/FlyPPTTimer/commit/e962010358d125a9e1dd18609c49272d1bd0a766) | 设置与 Remote 的间距调整。 / Refined Settings and Remote spacing. |
| UX-12 | 2026-09-09 | [c1da90b](https://github.com/Hona-Cao/FlyPPTTimer/commit/c1da90b07da6c422cbc3fe6e40cb0a1ae652033d) | 重开时 DPI 尺寸及底部留白。 / Reopen DPI sizing and footer spacing. |
| UX-13 | 2026-09-10 | [8e8b9cd](https://github.com/Hona-Cao/FlyPPTTimer/commit/8e8b9cd972b3aef3f32cda5ce903274bd678986d) | 规则布局、计时与配置编辑稳定性。 / Rule layout, timer/configuration editor stability. |
| RC 配置与鉴权 / config & auth | 2026-09-10 | [829c8c7](https://github.com/Hona-Cao/FlyPPTTimer/commit/829c8c79b430d92bbd1e7e37fa6d301d07ad40cb) | 关闭配置和 Remote 鉴权缺口。 / Closed configuration and remote-authentication gaps. |
| RC1 | 2026-09-10 | [988358c](https://github.com/Hona-Cao/FlyPPTTimer/commit/988358cef27c93b696fb65915fc7e86ecd7c0fe8) | 明确目标放映窗口，避免误操作别的文稿。 / Target the active presentation window. |
| RC1.1 | 2026-09-10 | [72b7a42](https://github.com/Hona-Cao/FlyPPTTimer/commit/72b7a42e9be1598e671c6a67e40799a4b3235118) | 保留无法明确判断的放映状态。 / Preserve ambiguous slideshow state. |
| RC2 | 2026-09-11 | [633a830](https://github.com/Hona-Cao/FlyPPTTimer/commit/633a830d78ca02be05994364d86f93eaf40a263e) | 整合验证与候选冻结记录，区分自动结果和现场待验收项。 / Integration review and candidate-freeze record. |
| RC3 / direct-feedback work | 2026-09-11 | [c0b5e37](https://github.com/Hona-Cao/FlyPPTTimer/commit/c0b5e37a71849f451e4c71cc7b158791165d436b) | 修复空路径反复重启计时及错误文稿匹配；后续处理原生音频、全屏到时、窗口和移动列表。 / Empty-path restarts and target identity, followed by native audio, blackout, window and mobile-list feedback. |
| RC3.1 | 2026-09-12 | [5bc223b](https://github.com/Hona-Cao/FlyPPTTimer/commit/5bc223bd7bea12339311bac4494f369bb3d363b1) | 稳定窗口/DPI、菜单与交互阻塞项。 / Stabilized the focused RC3.1 review blockers. |
| RC3.2 | 2026-09-12 | [fadf203](https://github.com/Hona-Cao/FlyPPTTimer/commit/fadf20323d5ed3ce42845bcf16017deb4eae2207) | 自动/自定义尺寸，页数字体排版，大屏去重，受控列表排序和长按拖动。 / Sizing, page typography, fullscreen exclusion and controlled-list sorting/drag. |
| RC3.3 | 2026-09-12 | [298d747](https://github.com/Hona-Cao/FlyPPTTimer/commit/298d74772b8fdc879e397239f1abe1bb0bdd753b) | 设置与 Remote 并存，可视颜色选择，PPT 类型限制，移动列表状态稳定。 / Concurrent windows, visual colors, PPT-only additions and stable mobile snapshots. |
| RC3.3.1 | 2026-09-12 | [19bd8d1](https://github.com/Hona-Cao/FlyPPTTimer/commit/19bd8d13e98851223a8290cb052b9de4f8e4a09f) | 现代多选文件对话框，修复首次手机访问 Winsock 10035。 / Modern multi-select picker and first-request Winsock 10035 correction. |
| RC3.4 | 2026-09-12 | [ea123cd](https://github.com/Hona-Cao/FlyPPTTimer/commit/ea123cda7f2e73778c2c1a3a5b04839fbd3afae4) | 页数新默认值、小圆角、百分比预览、结束编辑、未保存提示、暗黑主题和打开文稿最大化。 / Page defaults, small corners, percentages, editing/unsaved state, dark theme and maximized presentation opening. |
| v1.13.1 | 2026-09-12 | [e625c80](https://github.com/Hona-Cao/FlyPPTTimer/commit/e625c809f8cf7515f0143fab476bb4467d2fe1d7) | 移动列表横滑与滚动衔接、滚轮/精确百分比输入、紧凑对齐表头、语言重启后设置关闭修复、启动始终显示。 / Mobile gestures, precise percentage entry, aligned compact rows and window lifecycle fixes. |

## Detailed records / 逐轮原始记录

The following files retain the original requests, reviews and explicit limitations. Later approved requirements supersede older instructions; a historical “do not release yet” is not a current release prohibition.

- [UX plan](v1/UX_OPTIMIZATION_PLAN.md), [approved product deviations](v1/APPROVED_PRODUCT_DEVIATIONS.md), [original v0.30.2 baseline](v1/V1_BASELINE_CHECKLIST.md).
- [Accumulated implementation and validation results](v1/CODEX_RESULT.md), [first feedback](v1/USER_FEEDBACK_20260911.md), [second feedback](v1/USER_FEEDBACK_20260911_2.md).
- [RC3.1 manual scope](v1/RC31_MANUAL_TEST.md), [RC3.2 delivery](v1/RC32_DELIVERY.md), [RC3.3 feedback](v1/USER_FEEDBACK_20260912_4.md).
- [RC3.4 scope](v1/RC34_SCOPE.md), [v1.13.1 implementation delivery](v1/V1131_DELIVERY.md), [current release notes](RELEASE_NOTES_v1.13.1.md).

## Evidence is not interchangeable / 验证口径

Accepted v1.13.1 source: `e625c809f8cf7515f0143fab476bb4467d2fe1d7`.
[Windows build run 34709344878](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34709344878) passed formatting, Clippy, 90 tests (3 explicitly ignored), web syntax, Release build, version metadata and startup/Remote checks.

Earlier Windows runs include [RC3.3.1](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34701245739) and [RC3.4](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34705986469).
Actions artifacts have retention limits; the source commits and checked-in reports remain the durable history.

用户在交互试用后批准发布 v1.13.1。这不等于把历史上未完成的每一种手机、Office、音频、混合 DPI 或投影环境测试都补记为通过。离屏 GUI 图和模拟触摸只是相应范围的证据，不冒充实体设备测试。公开发布也不回改这些历史记录。

The user approved publication after using the delivered version. This does not retroactively mark every previously untested Office, audio, phone, DPI or projector scenario as passed. GUI renders and browser simulations are labeled as such.

## Complete accepted-branch commit index

[Preserved commit index](development-commits.tsv) records the accepted branch's actual implementation, build and review commits up to v1.13.1. It is exported from Git, not a synthetic activity calendar. Additional release/documentation commits are visible in [main history](https://github.com/Hona-Cao/FlyPPTTimer/commits/main).
