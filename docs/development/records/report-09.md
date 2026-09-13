[记录目录](../README.md) · [原始汇总](../../v1/CODEX_RESULT.md)

# UX13 审核后稳定性修复与新电脑环境预检

日期：2026-09-10

Review 分支：`codex/v1-06-manual-test`

本轮起始 HEAD：`ec30b3179ac7154d34f95e4f7cd18a868d110fdf`（已 fetch 远端最新提交）。

## 新电脑环境预检

按 AGENTS、HANDOFF、V1_BASELINE_CHECKLIST、CODEX_TASK、CODEX_RESULT 顺序读取；并核对 v0.30.2 的 AppCommandService、RemoteControlForm、AppConfig 和默认配置，以及 v4 的单调计时技术经验。

- 原仓库 `J:\codex2\FlyPPTTimer_GUI` 位于 main，存在用户未提交的 RELEASE_NOTES 删除及测试目录。保留原状，在 `J:\codex2\FlyPPTTimer-v1-06-manual-test` 建立独立 worktree，跟踪目标 review 分支；新工作区开始时干净。
- Git 可用；origin 为 `https://github.com/Hona-Cao/FlyPPTTimer.git`，保留原 gitee remote。
- 原工具链：`rustc 1.86.0 (05f9846f8 2025-03-31)`、`cargo 1.86.0 (adf9b6ad1 2025-02-28)`；已有 x86_64-pc-windows-msvc target。
- 第一次 `cargo check --locked` 原文：`error: rustc 1.86.0 is not supported by the following packages:`，其中 `slint@1.17.1 requires rustc 1.92`，同版本 Slint backend/compiler/core/build 等也要求 1.92。
- 安装官方 `1.92.0-x86_64-pc-windows-msvc` 工具链及 rustfmt、clippy：`rustup toolchain install 1.92.0 --profile minimal --component rustfmt --component clippy`。最终为 `rustc 1.92.0 (ded5c06cf 2025-12-08)` / `cargo 1.92.0 (344c4567c 2025-10-21)`。通过 `cargo +1.92.0` 或进程级 `RUSTUP_TOOLCHAIN=1.92.0` 使用，不改变用户默认工具链，不升级项目依赖。
- Visual Studio Community 2022 17.13.6 已安装；MSVC 为 `C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.43.34808`。cl.exe 存在但不在普通 shell PATH。
- Windows SDK 已安装在 `F:\Windows Kits\10`，有 10.0.22000.0 / 10.0.22621.0；本轮设置进程级 `RC=F:\Windows Kits\10\bin\10.0.22621.0\x64\rc.exe`，资源编译器可执行。未重新安装 VS/SDK，未修改 build.rs 绕过环境问题。
- 修改源码前，`cargo +1.92.0 check --locked` 已通过（55.19s）。Cargo.toml / Cargo.lock 保持不变，程序版本仍为 1.13.0。
- PowerPoint：有，注册路径及 `C:\Program Files\Microsoft Office\Root\Office16\POWERPNT.EXE` 文件存在。WPS：有，wpp 注册入口指向 `I:\APP\wps person\WPS Office\12.1.0.28488\office6\wps.exe`，文件存在；实际放映能力本轮未验收。
- 双屏：检测到 DISPLAY1 / DISPLAY2，WinForms 报告各 2048×1152 逻辑区域；实际缩放比例和混合 DPI 拖动效果尚未验收。
- 手机 Remote 同局域网设备：暂不可确认，待用户用手机连接验证；不能把本机 HTTP 测试当作手机连接验收。
- Inno Setup：有，6.7.3，`C:\Users\mf203\AppData\Local\Programs\Inno Setup 6\ISCC.exe` 可执行。
- ffmpeg：有，`I:\APP\ffmpeg-master\bin\ffmpeg.exe`。
- Windows PowerShell 默认禁止脚本执行；发布脚本验证仅使用进程级执行策略参数，未修改系统执行策略。

## 根因与实际修改

1. **F3**：旧 startPause 把 Paused 路由到 start 并重置提醒。改为显式匹配四种状态：Running 暂停、Paused 恢复、Stopped/Finished 新开始；恢复保留累计 elapsed 和本轮提醒记录。Timer 模块未改。
2. **Settings**：旧 Apply 将完整旧 draft 保存并替换共享配置，dirty 也错误比较实时 applied。增加会话 baseline，以 baseline/draft 差异合并到最新 applied；对象按字段合并，列表按实际编辑的领域合并（包括 Rules，未编辑规则时全部采用外部最新值；若双方同时修改同一规则领域，则本次 Settings Apply 的领域修改优先）。双方快照先用原规则规范化，避免把未编辑字段的历史清理误认为本次修改。校验和落盘都使用合并结果，保存成功后才替换 applied，并同步 draft/baseline；Discard 仍只丢弃本窗口草稿。直接保存 token/重启服务的设置操作同步自己的 baseline，防止再次被当成未应用修改。未增加配置框架或 revision 系统。
3. **Remote 端口**：周期刷新不再写 next-port；创建窗口和应用端口回调才同步编辑框。刷新服务状态、更新地址/令牌等操作保留正在输入的文本。原服务启动、端口占用 fallback 逻辑未变。
4. **Remote 规则**：普通保存只写可编辑的 duration/mode，保留各规则 enabled；更新列表时确保当前行属于实际选中集合，空集合为 -1；Ctrl 取消后、批量保存后同步当前编辑器。周期刷新同一规则不覆写正在编辑的时长。删除/保存不再在无选择时退回第一行。非法批量时长保持弹窗并显示现有中英文错误文案，不修改共享配置或磁盘。
5. **发布版本**：build-release.ps1 用 cargo metadata 读取当前 Cargo.toml 对应的根 package version，统一 Portable 目录、zip、installer 参数和名称；安装器取消旧 1.6.0 fallback，要求从脚本传入 MyVersion。不改程序版本，不创建 Release/Tag。

## 验证

- 定向命令/回调测试通过：F3 真实命令入口，Pause 等待期间 elapsed 固定，Resume 继续原进度且不重复提醒，Finished 后可重新开始。
- 使用 Slint 自带 MinimalSoftwareWindow 调用生产窗口回调验证数据路径，不新增 GUI 自动化框架：A→Ctrl B→Ctrl 取消 B 回到 A；启用/禁用混合保存保持 enabled；batch 后普通保存保持 10 分钟与模式；非法 batch 时长有错误反馈且配置文件字节不变；无选择保存/删除不修改第一条规则。
- Settings 回调测试覆盖未应用外观修改 + 外部规则批量修改/新增规则/token/port/窗口尺寸落盘 + Apply，验证两边更改均保留；第二次 Apply 使用新 baseline；改回原值清除 dirty；无 dirty Cancel 不落盘。
- 补充同一配置对象内不同字段的合并测试：Settings 修改 Remote enabled、外观宽度，外部 port/token/Remote 窗口宽度/外观高度/提醒文字仍保留。
- Windows PowerShell 版本解析最小验证通过，输出 `Parsed package version: 1.13.0`。
- 端口回调测试覆盖半截输入经多次 refresh / 更新令牌仍保留，以及完整端口输入应用后服务启动、编辑框和持久配置与实际端口一致。
- 提交前统一检查：`cargo fmt --all -- --check` 通过；`cargo clippy --locked --all-targets --all-features -- -D warnings` 通过；`cargo test --locked` 为 **44 passed、0 failed、1 ignored**（既有 Office COM 手测测试）；`cargo build --locked --release` 通过。开发时测试代码曾出现整数/浮点类型不匹配，已修正后重新编译验证，未删除或跳过任何回归测试。
- 使用 Windows PowerShell 实际运行 build-release.ps1 成功（脚本内部 build 复用缓存，0.72s）；Inno Setup 生成 installer 成功，读取安装器版本资源为 ProductVersion `1.13.0` / FileVersion `1.13.0.0`。
- 便携目录：`J:\codex2\FlyPPTTimer-v1-06-manual-test\artifacts\release\v1.13.0\FlyPPTTimer-v1.13.0-portable-win-x64`，可直接运行其中 FlyPPTTimer.exe；同级 zip 为 7,655,303 字节，setup-win-x64.exe 为 7,380,002 字节，主程序为 17,668,096 字节。均为本地 review 产物，使用仓库默认配置，未执行安装器，未发布 GitHub Release/Tag。

## 待用户真实手测

本轮自动回调/数据测试不等于原生鼠标、键盘、视觉体验通过。请在实际 Windows 桌面复核 F3 快捷键注册与暂停恢复、Settings 未应用草稿与 PC Remote 来回切换、端口输入停顿数秒、Ctrl/Shift 选择、批量弹窗错误提示及保存、双屏混合 DPI 拖动、PowerPoint/WPS 放映、声音/TTS 与手机局域网连接。软件和双屏已检测到，并非缺失；这些项目未被本轮源码测试实际验收。既有 Office COM 手测测试继续保留。

完成本轮源码和文档提交、推送后停止编码，等待 ChatGPT 下一轮审核。

---
