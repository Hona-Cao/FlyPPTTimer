# RC3.2 最终交付核验（2026-09-12）

本分支产品源码基点固定为 `fadf20323d5ed3ce42845bcf16017deb4eae2207`。本次仅补交付记录，不再修改产品源码。程序版本仍为 1.13.0，Codex 继续暂停，不创建 Release/Tag，不合并 main。

## 已核对结果

已读取 Actions run `34687385876` / job `103536640453` 的最终日志：格式检查、Clippy -D warnings、Slint binding-loop 检查、JavaScript 语法和 Release build 通过；`cargo test --locked` 为 **80 passed / 0 failed / 3 ignored**。3 个真实音频/Office 条件测试没有在本轮执行，不写成通过。

同一 Release EXE 的软件渲染截图已经生成并下载，已检查时间/页数下方、页数上方、独立大字号样式。手机示例截图不是实际手机验收。Windows 构建机不是用户的 Windows 11 / Office / 混合 DPI 现场。

下载的 artifact（10296340833）与内部绿色包已分别校验 CRC 完整性。EXE 为 x64 PE；哈希与 BUILD.txt 一致。完整绿色包包含 EXE、初始配置、VC 运行库、LICENSE、BUILD.txt、中文手测和变更说明。

- 对话交付：`FlyPPTTimer-v1.13.0-rc32-fadf203-win-x64.zip`（仅重命名复制 CI 内部绿色包，内容不变）。
- 绿色包 SHA256：`94e809390bd89b90823077da8c5321e431ff44cb034400db650235a93a3ace49`。
- EXE SHA256：`6e61a5b73ae96390a31cfd2cd31291fa6f7623d1d06388e240b5a8b78abd8577`。
- Actions 外层 artifact SHA256：`e06d83fbbef70698f34755ea69e10bfc279ea4dcca5a043799af19b7d971662c`，不是绿色包 SHA。

完整功能与证据边界记录：
https://github.com/Hona-Cao/FlyPPTTimer/blob/73139f9cb08fb85a3b38560541b895dc292b2847/docs/v1/RC32_DELIVERY.md

## 当前等待范围

只等待本轮尺寸/页码样式、大屏去重、手机排序拖动、移除/恢复控制与当前放映高亮四组反馈。详细步骤见 `RC32_MANUAL_TEST.zh-CN.md`，包内为 `MANUAL_TEST.zh-CN.md`。其余未提到的旧问题按用户意见暂时通过，不重新派发全量手测。

原 `codex/v1-06-manual-test` 分支已更新交付记录、CODEX_TASK 和 HANDOFF，明确指向本产品源码；未把本包代码合入旧 review 分支。CODEX_RESULT.md 中本轮开头的“等待 CI”叙述已由本交付核验记录取代，后面的历史 Codex 结果仍仅作历史依据。

构建辅助 patch/refine 清理命令曾报错，未把它误写成源码/运行缺陷，也不声称清理已完成。该工具残留不进入绿色包、不由用户运行。恢复开发时不要在已应用的源码上再次运行旧补丁脚本。
