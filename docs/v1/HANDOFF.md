# FlyPPTTimer V1 — 新会话交接

## 最新授权与工作状态（2026-09-11）

用户在 RC-2 终审后进一步要求：ChatGPT 直接修可确认的问题，其余交给 Codex 整改，给出绿色测试 EXE，并汇总手测。因此旧“无新反馈就停止”的指令已由最新 `CODEX_TASK.md` 替代。不是授权再次重制、换框架或全仓库无限审计。

Review 分支：`codex/v1-06-manual-test`。先拉取远端最新 HEAD；保护用户未提交文件，不回滚或强推，不修改默认分支。

程序仍为 `1.13.0`，Rust 仍为 `1.92.0`。不同包必须以 BUILD.txt 的源码 SHA 区分，不能仅凭同一版本号判断。

## 产品约束

全部既有功能稳定，界面美观协调、操作舒适。用户对现有总体布局基本满意，不重新设计整套 UI，不因重构削减功能。

根目录 `AGENTS.md` 给出阅读顺序；`APPROVED_PRODUCT_DEVIATIONS.md` 覆盖冲突的旧基线：

- Remote 固定端口优先，不暴露随机端口控件；
- PC Remote 演示文稿页仅文件规则管理，不恢复放映按钮；Web Remote 保留演示控制。

## ChatGPT 本次直接修改

产品源码只改 `src/presentation.rs`。

- `c0b5e37a71849f451e4c71cc7b158791165d436b`：修复路径为空时连续全屏状态反复 Start；保留最后已知文稿身份，A→未知→A 不重开计时。修复已知目标 B 找不到时错误回退控制唯一窗口 A；只有目标不可得才能唯一窗口 fallback。
- `96273ddf0e34fe73902f9fcd3ea4a6f356138c18`：新测试 rustfmt 格式收尾，无行为变化。
- 增加 `presentation::direct_fix_regressions` 五个回归测试；全部原有测试保留。

旧 RC-2 55 项通过属于旧源码，不能据此宣称本轮新源码已完成验证。最新直接修复记录见 `DIRECT_FIXES_20260911.md`（若已提交）；构建结果以相同源码 SHA 的 GitHub Actions `RC portable review` 为准。第一次云端构建止于新增测试的格式检查；已修正后重新触发，不能把该失败写成通过。

为用户要求的 EXE 增加了一个限定 review 分支的 Windows 构建 workflow：`.github/workflows/rc-portable-review.yml`。只读仓库内容，按锁定工具链运行 fmt/clippy/test/release 并上传带 SHA 的绿色包，不发布 Release/Tag。它是交付辅助，不是新的大型 CI 方案；交付后可移除。

## Codex 接下来只做什么

严格读取最新 `CODEX_TASK.md`：

1. 接收上述直接修复，核对当前源码的编译/测试；不重写已修部分。
2. 整改 `Session::start_show()` 临时放映设置的错误路径：中途 put 失败不能跳过恢复；恢复不完整不能把文稿标为 Saved。对正常/失败/原本 dirty 文稿进行安全的 Windows Office 验证。
3. 定向验证 COM 读取失败造成默认 false 状态，以及 COM worker join 导致退出等待的风险。未复现不得夸大为确认故障，禁止新建复杂框架。
4. 尽量自行完成普通托盘入口、PC Remote 和临时文稿回归。只留下真实跨设备、声音听感、混合 DPI/整体观感及适用的旧版升级给用户。
5. 如再次修改源码，重新构建含最终改动的绿色 EXE/ZIP，附源码 SHA 和证据边界，不能拿旧包改名冒充。
6. 追加 CODEX_RESULT，不覆盖历史；更新候选记录后 commit + push 并停止。

## 旧 RC-2 证据保留但不扩大解释

旧结果提交 `633a830d78ca02be05994364d86f93eaf40a263e`，候选源码 `952226d4c114b59334b13d0a7b44c60d89b9f030`，55 passed / 0 failed / 1 ignored。

已有真实进程记录：F3/F4/F5、HTTP 鉴权与计时、本机 LAN、端口回退、配置 Apply/Cancel/Import/Reset、Portable 持久化、隔离安装/同版本覆盖/卸载和 --show-settings 模式退出。不是全场景 Windows 11 正式验收。

旧本地产物 `E:\快传\计时器\v1.0\artifacts\release\v1.13.0\` 不含这次新修复，不作为当前交付包。其他电脑不要照抄 E/J/F 盘工具路径。

Office 之前因存在用户文稿未操作；PC Remote/托盘/跨屏部分受工具限制；headless PNG 只证明离屏渲染，不代表真实视觉已验收。

## 用户手测统一入口

仍用 `docs/v1/RC_MANUAL_TEST.md` 五组，不重复派发历史清单。补充本轮两点：无路径白名单全屏计时应持续增长；A 放映、B 活动但未放映时不得控制 A。尽量由 Codex 先验证，用户不做故障注入或代码排查。

未测不等于失败，也不等于通过。当前为测试构建，不作全功能稳定或正式发布保证。
