# ChatGPT 直接修复与绿色测试版交付

日期：2026-09-11
分支：`codex/v1-06-manual-test`
程序版本：`1.13.0`
产品源码：`96273ddf0e34fe73902f9fcd3ea4a6f356138c18`

## 已完成，不是仅下发任务

用户要求 ChatGPT 先直接修改可以确认的问题，其余交 Codex，并提供绿色测试 EXE。已实际修改 `src/presentation.rs` 并完成 Windows 云端编译、测试和绿色包获取；没有改 UI 布局、依赖版本、配置格式或 Remote 协议。

### 1. 无路径全屏/歧义状态反复 Start

旧 `same_path("", "")` 必定 false；`PresentationLifecycle::observe(true, "", ...)` 因而在每次观察时产生 Start，令全屏白名单计时反复清零。A→路径未知→A 也会不必要地重新开始。

现已在同一 showing 会话内对空路径保持当前轮次及最后已知身份。首次无路径全屏开始一次；真正离开后仍按设置停止/重置；确实切换到另一个非空文稿路径仍沿用原开始行为。

这也补上此前 RC-1.1 审查遗漏的下游路径：仅保留 slide_show_running=true 并不足以证明 Timer 不会重开，还必须检查 observe 对空路径的处理。

### 2. 已知目标不匹配却回退控制唯一窗口

旧 `choose_show_window_index(Some(B), [A])` 会回退 A。现已要求所有非空已知目标匹配正确路径；找不到就拒绝操作，状态仍保留存在放映。只有目标不可得时才能对唯一窗口 fallback。零窗口仍为未放映。

新增五个回归在 `presentation::direct_fix_regressions`，覆盖连续空路径、A→未知→A、真实文稿切换、已知目标与唯一窗口不匹配、窗口身份读取为空，以及无窗口。

修复提交：`c0b5e37a71849f451e4c71cc7b158791165d436b`。
格式收尾：`96273ddf0e34fe73902f9fcd3ea4a6f356138c18`。

## 实际验证

编译与测试运行：
https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34550315848

在 Windows Server 2022 runner / Rust 1.92.0 上，针对上述源码实际完成：

- fmt --check 通过；
- clippy --locked --all-targets --all-features -- -D warnings 通过；
- cargo test --locked：**60 passed、0 failed、1 ignored**；
- cargo build --locked --release 通过。

五个新增测试均在该次测试输出中通过，全部已有测试保留。ignored 仍是原有 Office COM 手测，不是掩盖失败。首次运行 `34550130680` 止于一处新增断言的 rustfmt 排版；已修正并完整重跑，不把该失败记成成功。

没有在用户 Windows 11 上亲自复跑，也没有把 Windows runner 当作 PowerPoint/WPS、GUI、手机或混合 DPI 实机验收。

## 绿色包及运行库

最初 EXE 导入表显示依赖 `VCRUNTIME140.dll`。为避免让新电脑用户先安装开发环境，已进一步组装 app-local 运行库包：

https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34551389738

- 使用前一成功运行的原始 EXE，没有重新改名冒充另一个版本，也没有改 EXE 字节；
- 从 Visual Studio x64 Redist 目录复制未修改的 `vcruntime140.dll` 和 `vcruntime140_1.dll`；
- 两个 DLL 的 Microsoft Authenticode 签名均验证有效，版本均为 `14.44.35211.0`；
- 实际启动同一 Release EXE，确认检查期间进程存活，且加载的是程序同目录 `VCRUNTIME140.dll`；
- 临时测试关闭 Remote，仅清理自己启动的测试进程，随后恢复原默认配置；没有把进程清理当作正常退出验收；
- 最终包只包含允许清单文件，无测试日志、私人规则或真实 token。

最终 GitHub artifact：`FlyPPTTimer-portable-with-runtime-96273dd`，ID `10180934561`（原始 artifact 依仓库设置保留 7 天）。

ChatGPT 会话交付：

- `FlyPPTTimer-v1.13.0-directfix-96273dd-win-x64.zip`：完整绿色目录，另附中文修复/手测说明；
- `FlyPPTTimer-v1.13.0-directfix-96273dd.exe`：原始主程序副本，单独下载不包含运行库；
- `FlyPPTTimer_Direct_Fixes_and_Test_Guide_20260911.md`：完整说明。

EXE 大小：17,697,280 字节。下载提取后再次检查 ZIP 完整性、源码标识、PE64 GUI 格式、默认配置以及组装前后 EXE 字节一致。

建议使用完整 ZIP，解压到新的可写目录，正常双击 `FlyPPTTimer.exe`，保留两个 DLL 在同目录，不运行安装器、不覆盖旧程序或唯一配置。主程序测试版未做 Authenticode 签名；微软运行库的签名验证不代表主程序已签名。

微软运行库不是项目 MIT 许可的内容，说明见包内 `THIRD_PARTY_RUNTIME.txt`。后续 Codex 如重新构建/打包，必须继续检查并正确部署运行库，不能回到仅复制 EXE 且假设每台 Win11 已安装 VC Runtime 的做法。

## 本包没有完成的整改

`CODEX_TASK.md` 是 Codex 的唯一实现指令，已同步：

- **B1，明确源码遗漏，尚未修复：** `Session::start_show()` 的多个临时设置 put 失败会跳过恢复；恢复失败后仍可能设置 Saved=true。由 Codex 最小整改并用一次性 Office 文稿验证。未声称已观察到用户数据丢失。
- **B2，待确认风险：** COM 读取错误是否误报退出放映，以及 worker.join 是否拖住退出。先安全验证，有证据再最小修复；不建立复杂重试/缓存框架。
- **B3，原生回归：** 普通托盘入口、PC Remote、临时 PowerPoint/WPS、多屏。Codex 尽量完成，不把已可靠通过项目重新全部交给用户。

因此本包是包含两处直接修复的测试构建，不是所有已知风险均已排除的正式发布版。Office 必须使用可丢弃副本，用户不承担故障注入或代码定位。

## 用户手测

统一沿用 `RC_MANUAL_TEST.md` 五组：桌面入口/PC Remote/双屏，实际演示及上述两处修复，手机跨设备，实际声音/TTS，适用的旧配置/升级。Codex 能可靠验证的子项不再派给用户。

没有创建 Release、没有创建 Tag、没有合并默认分支。旧 RC-2 55 项通过及 E 盘旧包属于旧源码，不作为本次交付证据。
