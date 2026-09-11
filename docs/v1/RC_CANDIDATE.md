# RC-2 代码冻结候选版

日期：2026-09-11；review 分支：`codex/v1-06-manual-test`。

- 候选源码 commit：`952226d4c114b59334b13d0a7b44c60d89b9f030`。RC-2 与本次终审只修改文档；程序源码与该 commit 相同。
- Codex RC-2 成果提交：`633a830d78ca02be05994364d86f93eaf40a263e`。
- 程序版本：`1.13.0`；Rust：`1.92.0 (ded5c06cf 2025-12-08)`。
- 状态：**ChatGPT 终审接受为代码冻结候选版，进入用户最终实机验收；尚非正式发布批准。** 本轮不新增源码整改任务。尚未完成的场景不等于验收通过。

## 本地产物

Codex RC-2 报告记录的根目录：`E:\快传\计时器\v1.0\artifacts\release\v1.13.0\`。

| 产物 | 文件名 / 相对根目录路径 |
|---|---|
| Portable 目录 | `FlyPPTTimer-v1.13.0-portable-win-x64\` |
| Portable 程序 | `FlyPPTTimer-v1.13.0-portable-win-x64\FlyPPTTimer.exe` |
| ZIP | `FlyPPTTimer-v1.13.0-portable-win-x64.zip` |
| Installer | `FlyPPTTimer-v1.13.0-setup-win-x64.exe` |

主程序 17,687,552 字节；ZIP 7,898,644 字节；Installer 7,382,897 字节。Installer ProductVersion `1.13.0`、FileVersion `1.13.0.0`。Portable 目录已恢复仓库默认配置，测试日志移入 `target/rc-temp/`；ZIP / Installer 使用打包时的默认配置。这些为执行报告记录，ChatGPT 未直接检查用户本机文件。

## 最终证据

Codex 报告 `cargo fmt --all -- --check`、`cargo clippy --locked --all-targets --all-features -- -D warnings`、`cargo test --locked`、`cargo build --locked --release` 和现有 `scripts/build-release.ps1` 均通过。测试 **55 passed、0 failed、1 ignored**；忽略项仍为既有 Office COM 手测。ChatGPT 已核对本轮提交只改文档；并未在用户 Windows 电脑上亲自复跑。

RC-2 实测记录通过：Portable 启动与第二实例退出；F3 暂停/继续、F4 重置、F5 可见状态切换；HTTP 鉴权、Timer 命令、正计时/超时、本机 LAN 地址访问；占用端口回退并保存；便携配置重启回读；Settings Apply、导入/恢复默认的界面与运行状态同步、Cancel 放弃草稿；`--show-settings` 模式正常退出及重新启动；临时隔离安装、同版本覆盖保留配置、安装版启动与正常退出、卸载。

声音证据限于 WAV/MP3 系统播放链成功、TTS 调用成功、应用结束提示路径不崩溃。Settings 在当前显示器显示完整。均不替代听感或混合 DPI 拖动验收。仓库 rc-review 图片是离屏软件渲染，不是完整的 Windows 原生视觉验收证据；ChatGPT 本次未能直接打开这些 PNG，不声明已经完成图片观感审核。

## 终审剩余验收门槛

统一见 [RC_MANUAL_TEST.md](RC_MANUAL_TEST.md)，不再分别派发历史任务单：

1. 桌面菜单、PC Remote 原生操作、双屏与整体界面；
2. PowerPoint/WPS 工作流和适用的多放映场景；
3. 手机真实跨设备连接；
4. 应用内声音选择、提醒听感与静音；
5. 用户实际旧配置与适用的旧安装版升级。

详细已测方法和限制保留在 [CODEX_RESULT.md](CODEX_RESULT.md)。未测项不冒充已通过，纯逻辑测试不冒充 Office 真机，特殊启动模式不冒充托盘生命周期。

当前不再主动扫描/重构。关键场景验收通过后才能签收对应功能；用户明确反馈的 P0/P1 才进入定向修复，普通视觉偏好集中记录并按用户选择处理。

最终证据来自本机 Rust RC 验证，旧 CI 不作为本轮验收依据。未创建 Release / Tag；未经用户明确授权不发布、不合并默认分支。
