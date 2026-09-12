# RC3.3 当前交付结果 — 2026-09-12

ChatGPT 已直接完成本轮整改并生成绿色测试包；Codex 继续暂停。本分支只同步结果，不代表已合入最新产品源码。

## 唯一源码和产物

- 程序 1.13.0，Rust 1.92.0，依赖不变。
- 本轮产品源码：`298d74772b8fdc879e397239f1abe1bb0bdd753b`。
- 最终源码分支：`chatgpt/rc33-verified-34692149403`。
- 完整实现、验证和限制说明：https://github.com/Hona-Cao/FlyPPTTimer/blob/b9b3730556026b3b8e9083d0fde6a5f36df5d994/docs/v1/RC33_DELIVERY.md
- Windows Actions：https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34692149403 ，作业 103549498085 成功。
- 绿色包 artifact：10297324783；Windows 证据 artifact：10297324784。
- 对话下载名：`FlyPPTTimer-v1.13.0-rc33-298d747-win-x64.zip`，8,338,977 字节；CI 内层 ZIP 原样复制改名。
- ZIP SHA256：`f89766a15f5e636435ec6e3455691051f399e8e53ce7281f5c69141a653a15bf`。
- EXE SHA256：`d6e38bbd0c4b8af416ecbf6290c4a6040a51146b6b82d77e850c0104ffd6c307`。

包内有 EXE、干净配置、三个 x64 VC 运行库、LICENSE、BUILD.txt、中文手测和变更说明。已检查 ZIP CRC、PE x64、EXE 哈希与 BUILD.txt、空规则/空令牌；CI 对 DLL 签名检查通过。主程序未签名。先退出旧版，解压到新的可写目录运行，不覆盖旧配置，不关闭安全软件。

## 已实施

1. 设置和 PC Remote 可并存，关闭一个不连带关闭另一个，现有窗口复用。
2. 正文与滚动条用实际布局内边距分隔；底部三按钮独立留白。
3. 应用拥有的窗口、提示框、通用对话框与托盘使用产品图标，不修改外部程序或文件类型图标。
4. 所有可见颜色项提供色块、基本色/自定义 RGB 选择和 HEX 输入，保留 Apply/Cancel 与跟随时间规则。
5. 添加被控文件限制 .ppt/.pptx/.pptm，返回路径后再次验证；不接受 PDF/图片/Word，不自动删除旧配置历史项。
6. 手机五按钮一行、卡片紧凑；命令回复前发布新状态，快照 revision/请求时段过滤旧响应，重启用 serverInstance 区分；只在顺序真实变化时执行动画，普通刷新不重启动画，无 POST 自动重放。

## 白屏状态

偶发白屏尚未稳定复现，不能宣称根因已确认或彻底消除。本轮移除 Remote 原生隐藏/Slint 显示混用，增加过期显示回调保护、重绘和可见性/尺寸日志。

## 验证

最终 Windows fmt、Clippy 严格检查、JS syntax、Release 构建通过；85 passed / 0 failed / 3 ignored，ignored 仍为真实音频/Office 条件项目。软件布局截图完成；部分云端中文截图缺字，不能作为用户字体显示通过证据，默认字体没有改动。

原生托管 Windows 完成 12 轮同进程两窗口并存、独立关闭/重开、非零几何及图标句柄检查；未逐帧验证客户区像素。最终 CI Web 源码的 Chromium DOM + mocked fetch 检查 25/25 通过，涵盖双语窄屏、旧响应延迟、排序、动画不中断、重启、拖动/取消和禁复制。均不冒充真实 Windows 11/手机/Office 全部通过。

## 用户只需四组

1. 两窗口并存/独立重开，顺便查看间距与应用图标；正常使用时留意白屏，不要求压力测试。
2. 选色、HEX、取消与应用保存。
3. 设置/Remote 添加文件仅允许 PPT 三种后缀。
4. 手机五按钮一行，排序/上下移动/长按拖动后等待刷新，不再新→旧→新回跳，顺序持久化。

详细中文说明：测试包 MANUAL_TEST.zh-CN.md，或最终源码 docs/v1/RC33_MANUAL_TEST.zh-CN.md。其他已暂时接受的旧项不重测。

main、版本、Release、Tag 未改。本分支旧源码不能作为本测试包基点；后续只能在用户反馈后从上述明确产品提交继续。
