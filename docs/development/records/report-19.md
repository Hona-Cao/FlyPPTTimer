[记录目录](../README.md) · [原始汇总](../../v1/CODEX_RESULT.md)

# 真实 UX10 便携版复现：设置窗口 DPI 递减与底栏隔离

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`

源码提交：`c1da90b`

## 本轮触发

用户指出此前只看开发期静态截图，没有先打开实际测试版本。本轮改为直接启动并操作 `E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX10-LayoutFileFilter-20260909-win-x64/FlyPPTTimer.exe`，使用该包内带有真实文件规则的配置复现问题。

## 实际复现与修改

- 通过实际右键菜单打开设置后，连续关闭/打开五次，确认窗口客户区从约 900×650 逐次缩小，最终只剩白色区域；根因是隐藏窗口的 HWND 客户区读取受到 DPI 虚拟化，并在下一次打开时再次当作物理尺寸设置。
- 设置几何记忆改用 Slint/winit 的物理窗口尺寸；创建 native surface 后等待一次 Winit DPI/resize 事件，再直接恢复保存的物理尺寸，移除旧的嵌套显示/隐藏流程。修复后在同一真实配置上连续三次关闭/打开，窗口外框稳定为约 915×687，没有递减或空白表面。
- 设置内容面板高度和滚动视口进一步预留固定底栏的安全区（内容面板减少 18px、滚动视口减少 20px），底栏仍无背景和边框，文件规则最后一行与“确定 / 取消 / 应用”之间保留可见间隙。
- 删除不再使用的 HWND 客户区尺寸读取函数，避免后续代码重新引入 DPI 虚拟化尺寸。

## 验证

- `cargo fmt -- --check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：41 passed、0 failed、1 ignored（既有 Office COM 手测测试）。
- `cargo build --release`：通过。
- 真实 UX12 便携包使用 UX10 的文件规则配置启动；设置窗口实际打开后截图确认内容、滚动区和底栏分离，连续三次关闭/打开尺寸保持稳定。

## 本地手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX12-SettingsDpiLayout-20260909`。便携目录：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX12-SettingsDpiLayout-20260909-win-x64/`，直接运行其中的 `FlyPPTTimer.exe`，不要求解压或安装；包内保留了 UX10 的文件规则配置，便于复现本轮真实问题。这是 review 手测版，不是正式 Release/Tag。完成推送后停止编码，等待审核。
