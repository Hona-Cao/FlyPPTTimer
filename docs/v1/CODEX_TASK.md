# 当前任务：等待最终用户实测，不主动修改源码

日期：2026-09-11。Review 分支：`codex/v1-06-manual-test`。程序版本仍为 `1.13.0`。

## 当前产品源码冻结点

产品源码候选：`bf00cf0dc6ccec337141520f385f8e37d5dab639`（`fix: stabilize mixed-DPI windows and complete feedback fixes`）。

此后 `eeb4ed6315d956c1a1e3aeace01abb6894107d59` 只更新绿色包运行库组装 workflow，不改变产品源码。后续文档提交也不得被误认成新的产品二进制基点。

Codex 已完成 F01～F09 与 B1/B2 的本轮整改/验证记录，详见 `CODEX_RESULT.md`。最终普通自动检查为 66 passed / 0 failed / 3 ignored，并另外执行了音频和可丢弃 Office 文稿的真实 ignored 测试；具体证据边界以结果报告为准。ChatGPT 已复核关键实现，包括混合 DPI WINDOWPOS 修正、每屏全屏 TimeUpWindow、COM 未知采样、放映范围恢复、原生音频回退和无 PowerShell 更新交接。

GitHub Actions 对 `bf00cf0` 的 `RC portable review` 成功；随后基于同一产物加入 Microsoft 签名的 app-local `vcruntime140.dll` / `vcruntime140_1.dll` 并完成 loader smoke。绿色包 BUILD.txt 必须明确写 `bf00cf0dc6ccec337141520f385f8e37d5dab639`。

## 现在禁止主动编码

除非用户最新实测稳定复现 P0/P1，不再修改 Timer、DPI、Remote、Office、声音、UI、配置、发布脚本或依赖；不为了“再优化一下”继续审计/重构。

不要重新执行已经由实现侧可靠完成的整套历史手测，不要求用户提供 DPI 日志、COM 故障注入、JSON 差异或命令行调试。

## 用户只剩四项实测

统一以 `RC_MANUAL_TEST.md` 为准：

1. 用户原来能快速复现的双屏沿边拖动路径，确认 Settings / PC Remote 不再累计变大或变小；
2. 真实手机热点/同 LAN 扫唯一主二维码，测试 Start/Pause/Resume/Reset 与一条临时演示命令；
3. 用户安全软件正常开启时，实际提示音/TTS 听感以及是否还有产品运行时 PowerShell 拦截；
4. 用户真实单屏/投影演示中，“黑屏并显示时间到”必须完整覆盖屏幕并保持，F4/Remote Reset 等主持人明确重置才解除，同时“仅提示/退出放映”不得误显示该遮罩。

如果用户反馈失败，只针对可稳定复现的项目做根因+最小修复，并重新构建对应源码 SHA 的绿色包。失败项之外不扩范围。

未经用户明确批准：不创建 Release/Tag，不合并默认分支，不升级版本/依赖，不更换技术栈，不删除功能。
