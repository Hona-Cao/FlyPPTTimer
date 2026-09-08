# FlyPPTTimer V1 — 当前 Codex 任务

状态：**会话交接，等待新 ChatGPT 更新具体任务**  
当前分支：`codex/v1-06-manual-test`

上一轮窗口、Remote 和 DPI 修复已经完成，旧任务不要重复执行。

新 Codex 会话开始后：

1. 读取根目录 `AGENTS.md`。
2. 读取 `docs/v1/HANDOFF.md`、`docs/v1/V1_BASELINE_CHECKLIST.md` 和最新 `docs/v1/CODEX_RESULT.md`。
3. 拉取当前 review 分支最新 HEAD。
4. 等待新 ChatGPT 会话把新的具体任务写入本文件后再修改源码。

当前项目已经进入 V1 候选版本收口阶段。后续工作主要是用户手工测试发现的真实 Bug、显示效果修正、v0.30.2 parity 清零和最终发布收口，不再按旧阶段计划重复开发已经完成的功能。

在本文件被新 ChatGPT 更新之前，不提交新的功能修改。
