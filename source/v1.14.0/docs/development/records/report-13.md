[记录目录](../README.md) · [原始汇总](../../v1/CODEX_RESULT.md)

# 设置与 Remote 规则页第二轮手测反馈

日期：2026-09-09

Review 分支：`codex/v1-06-manual-test`

源码提交：`46eac11`

## 本轮触发

用户继续手测后确认：设置和 Remote 窗口在鼠标指针跨屏瞬间仍可能异常放大；文件规则需要明确的普通单选、Ctrl 追加/取消和 Shift 范围选择；选中文件后的详情应只保留时长和模式；说明弹窗出现文字拥挤；Remote 的“演示文稿”页只需要复刻设置页文件规则，并删除运行状态及放映控制按钮。

## 实际修改

- 设置和 PC Remote 继续共用窗口 DPI 处理，但改为记录上一次 DPI，先把 `WM_DPICHANGED` 转发给 Winit / Slint，再只在实际客户区尺寸与 DPI 比例不一致时校正客户区；最大化窗口不强行改尺寸。正常路径不会额外调整，跨屏发生重复缩放时只修正可测出的偏差。
- 文件规则行把 Ctrl 和 Shift 修饰键传入 Rust：普通左键清空其它选择并单选，Ctrl+左键追加或取消当前项，Shift+左键按上一次选中项到当前项建立范围；Ctrl+Shift 可在已有选择上追加范围。选中项继续用色块提示。
- 文件规则详情卡片压缩为时长和模式两项，移除文件名、路径和启用复选框的重复展示；启用状态仍保留在规则列表中。
- 说明弹窗增加隐藏测量文本，以实际换行后的首选高度计算弹窗高度和滚动区域，并扩大正文宽度、固定顶部对齐和留白，避免长说明挤成一团或被裁切。
- Remote 的“演示文稿”页数据源改为配置中的文件规则，移除“演示软件已运行”状态区以及从“打开演示文稿”到“退出演示软件”的放映控制按钮，只保留规则列表、时长/模式编辑、添加、删除、清空和保存。

## 验证

- `cargo fmt --all`：通过。
- `cargo check`：通过。
- `cargo clippy --all-targets --all-features -- -D warnings`：通过。
- `cargo test`：37 passed、0 failed、1 ignored（既有 Office COM 手测测试）。
- `cargo build --release`：通过，生成 `target/release/FlyPPTTimer.exe`。
- 本轮在当前桌面启动了 Release `--show-settings` 实例；Computer Use 窗口捕获随后返回“foreground window did not report a process id”，无法可靠取得该实例截图，因此未把本机单屏操作当作跨 DPI 真机验收。150% / 125% 双屏拖拽仍需用户实际设备复核。

## 本地手测包

程序版本仍为 `1.13.0`；本轮包标识为 `v1.13.0-UX06-SettingsRules-20260909`。便携目录：`E:/快传/计时器/tests/FlyPPTTimer-v1.13.0-UX06-SettingsRules-20260909-win-x64/`，直接运行其中的 `FlyPPTTimer.exe`，不要求解压或安装；包内使用默认配置，不携带临时调试配置、用户配置或日志。这是 review 手测版，不是正式 Release/Tag。

本轮完成后提交并推送 `codex/v1-06-manual-test`，停止编码等待审核；跨屏尺寸、Ctrl/Shift 选择和弹窗视觉仍以用户手测结果为准。

---
