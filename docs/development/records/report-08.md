[记录目录](../README.md) · [原始汇总](../../v1/CODEX_RESULT.md)

# RC-1.1 多放映状态误判收口

日期：2026-09-11

Review 分支：`codex/v1-06-manual-test`

审核基点：`988358cef27c93b696fb65915fc7e86ecd7c0fe8`

## 本轮修改

- `read_application_state()` 先记录 `SlideShowWindows.Count > 0`，再解析目标窗口；多窗口目标不明确时返回 `slide_show_running=true`、清晰错误和空的当前文稿/页码，不再让状态层把放映误报为已停止。
- `with_show_view()` 继续在目标不明确时直接返回错误，因此 Previous/Next/Goto/黑屏/白屏/恢复/结束命令不会发送到任意窗口。
- 增加纯逻辑状态回归，覆盖 0 窗口、唯一窗口回退、乱序目标匹配、多窗口歧义保持运行事实；未修改 Timer、Remote、UI、DPI、配置、发布脚本或 Office ownership/退出逻辑。

## 验证

- `cargo fmt --all -- --check` 通过；`cargo clippy --locked --all-targets --all-features -- -D warnings` 通过；`cargo test --locked` 为 **55 passed、0 failed、1 ignored**（既有 Office COM 手测）。
- `cargo build --locked --release` 通过，程序版本仍为 `1.13.0`；按任务要求未重复 Inno Setup、未重新生成截图。
- RC-1.1 定向静态审阅未发现新的 P0/P1；真实 PowerPoint/WPS 多窗口、手机局域网、混合 DPI、声音/TTS 与安装升级仍由 [RC_MANUAL_TEST.md](RC_MANUAL_TEST.md) 手工完成。

## 待用户手测

本轮仅收口多放映状态误判，完成定向单元测试和 Release 构建。请按 [RC_MANUAL_TEST.md](RC_MANUAL_TEST.md) 在目标 Windows 机器上完成最终人工验收；不创建 Release/Tag。

---
