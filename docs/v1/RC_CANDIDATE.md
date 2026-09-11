# RC-2 代码冻结候选版

日期：2026-09-11；review 分支：`codex/v1-06-manual-test`。

- 候选源码 commit：`952226d4c114b59334b13d0a7b44c60d89b9f030`。RC-2 仅新增验收文档；本记录所在提交的程序源码与该 commit 相同。
- 程序版本：`1.13.0`；Rust：`1.92.0 (ded5c06cf 2025-12-08)`。
- 状态：**代码冻结候选版，除最终验收发现 P0/P1 外不再修改源码**。本轮没有发现稳定复现的 P0/P1；尚未完成的场景不等于验收通过。

## 本地产物

根目录：`E:\快传\计时器\v1.0\artifacts\release\v1.13.0\`

| 产物 | 文件名 / 相对根目录路径 |
|---|---|
| Portable 目录 | `FlyPPTTimer-v1.13.0-portable-win-x64\` |
| Portable 程序 | `FlyPPTTimer-v1.13.0-portable-win-x64\FlyPPTTimer.exe` |
| ZIP | `FlyPPTTimer-v1.13.0-portable-win-x64.zip` |
| Installer | `FlyPPTTimer-v1.13.0-setup-win-x64.exe` |

主程序 17,687,552 字节；ZIP 7,898,644 字节；Installer 7,382,897 字节。Installer ProductVersion `1.13.0`、FileVersion `1.13.0.0`。Portable 目录已恢复仓库默认配置，测试日志移入 `target/rc-temp/`；ZIP / Installer 使用打包时的默认配置。

## 最终证据

`cargo fmt --all -- --check`、`cargo clippy --locked --all-targets --all-features -- -D warnings`、`cargo test --locked`、`cargo build --locked --release` 和现有 `scripts/build-release.ps1` 均通过。测试 **55 passed、0 failed、1 ignored**；忽略项仍为既有 Office COM 手测。

真实 Windows 通过：Portable 启动与第二实例退出；F3 暂停/继续、F4 重置、F5 可见状态切换；HTTP 鉴权、Timer 命令、正计时/超时、本机 LAN 地址访问；占用端口回退并保存；便携配置重启回读；Settings Apply、导入/恢复默认的界面与运行状态同步、Cancel 放弃草稿；`--show-settings` 模式正常退出及重新启动；临时隔离安装、同版本覆盖保留配置、安装版启动与正常退出、卸载。

声音证据限于 WAV/MP3 系统播放链成功、TTS 调用成功、应用结束提示路径不崩溃。Settings 在当前显示器显示完整。均不替代听感或混合 DPI 拖动验收。

剩余五个场景见 [RC_MANUAL_TEST.md](RC_MANUAL_TEST.md)：桌面菜单/PC Remote/跨屏体验，Office 工作流，手机跨设备连接，实际听感，旧正式版升级（仅使用安装版时）。详细方法和限制见 [CODEX_RESULT.md](CODEX_RESULT.md)。

最终证据来自本机 Rust RC 验证，旧 CI 不作为本轮验收依据。未创建 Release / Tag；等待 ChatGPT 最终审核。
