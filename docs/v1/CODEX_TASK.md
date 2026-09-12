# 当前任务：暂停，等待 ChatGPT RC-3.1 用户手测

日期：2026-09-12。Review 分支：`codex/v1-06-manual-test`。版本保持 `1.13.0`。

## 状态

**Codex 暂停执行源码工作。不要继续上一版 RC-3.1 任务，不要自行拉取临时 ChatGPT 分支合并，不要生成新包，不要创建 Release/Tag。**

用户要求先由 ChatGPT 对已确认问题做一版独立小修并提供测试包，再根据用户手测结果决定是否让 Codex 接手后续整合。

当前正式 review 分支的产品源码仍保持此前状态；ChatGPT 的 RC-3.1 小修位于独立 review-only 分支/测试包中，尚未批准合入本分支。

## 等待中的用户手测

重点是：

- TimeUp 黑屏 ESC 普通短按可靠性；
- 已在放映但不是 ActivePresentation 的目标重新切为控制目标；
- 已明确确认“关闭且不保存”时 dirty 文稿关闭不阻塞 Office STA worker，关闭后后续演示命令仍可用；
- 手机自定义排序后，从 Settings / PC Remote 新增规则仍追加到末尾并在重启后保持；
- 有条件时补真实手机断线恢复、原生 Microsoft PowerPoint、150%/125% 混合 DPI 现场观察。

## 下一步

等待用户把测试结果反馈给 ChatGPT。只有 ChatGPT 根据手测结果重新改写本文件为明确实施任务后，Codex 才继续。
