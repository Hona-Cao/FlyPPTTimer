# 当前交付：直接修复后的绿色测试构建

日期：2026-09-11
Review 分支：`codex/v1-06-manual-test`
程序版本：`1.13.0`
当前测试源码：`96273ddf0e34fe73902f9fcd3ea4a6f356138c18`

## 已实际完成

用户授权 ChatGPT 直接修复可确认问题并交付测试 EXE。本轮产品源码只改 `src/presentation.rs`：无路径全屏/歧义读取不再反复 Start；非空已知目标不匹配时不再错误控制唯一的其他放映窗口。保留既有 UI、功能、依赖与版本。

针对该源码，Windows Server 2022 / Rust 1.92.0 实际运行 fmt、clippy、test 和 Release build 全部通过：**60 passed、0 failed、1 ignored**。原有 Office COM 手测保持 ignored；Windows 11 原生界面和 Office 工作流并未因此自动通过。

详细修复、实际日志核对和验证边界见 `DIRECT_FIXES_20260911.md`。

## 下载与构建来源

编译及自动测试：
https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34550315848

最终绿色包组装及加载检查：
https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34551389738

**优先使用完整运行库包：** `FlyPPTTimer-portable-with-runtime-96273dd`，artifact ID `10180934561`。不要将前一构建中仅 EXE/默认配置的原始 artifact 当成已包含运行库的最终绿色包。

最终包包含原始 `FlyPPTTimer.exe`、干净默认配置、LICENSE、BUILD.txt、Microsoft 签名有效的 x64 `vcruntime140.dll` / `vcruntime140_1.dll` 及第三方运行库说明。两个 DLL 版本为 14.44.35211.0，来自 Visual Studio Redist，未修改。

Windows runner 实际启动同一 EXE，确认检查期间进程存活并加载程序同目录的 VCRUNTIME140.dll；只清理自己的临时进程并恢复默认配置。这不代替正常托盘退出、Win11 GUI、Office、多屏或手机验收。

EXE 为 17,697,280 字节。组装前后 EXE 字节相同。ChatGPT 会话交付 `FlyPPTTimer-v1.13.0-directfix-96273dd-win-x64.zip`，额外附中文手测说明；最终 ZIP 为 7,734,356 字节，完整性检查通过。单独 EXE 副本不包含运行库。GitHub artifact 按当前设置保留 7 天。

完整解压到新的可写目录，双击 `FlyPPTTimer.exe`，保留两个 DLL，不运行安装器，不覆盖用户旧程序/唯一配置。此为未签名的主程序测试版；微软 DLL 的签名不表示主程序已签名。

## 尚未包含的整改

`CODEX_TASK.md` 已列出 Codex 的唯一后续任务：

- B1：`Session::start_show()` 中途设置失败的恢复路径及 Saved 标志；源码确认存在遗漏，尚未修改，不能宣称已发生或已解决用户数据丢失。
- B2：COM 状态读取错误与退出 join 等待的待实机确认风险；有证据再最小修复，不添加复杂框架。
- B3：普通托盘/PC Remote/临时 Office/双屏原生回归；如改源码，必须重新构建对应绿色包，并继续部署所需运行库。

当前交付不等于正式发布批准。Office 只用可丢弃副本，用户不做故障注入或代码定位。

## 手测与历史

统一使用 `RC_MANUAL_TEST.md` 五组；Codex 能可靠完成的子项不再派给用户。

旧 RC-2 提交 `633a830d78ca02be05994364d86f93eaf40a263e`、源码基点 `952226d4c114b59334b13d0a7b44c60d89b9f030`、55 项通过及 E 盘旧包仅供历史溯源，不含本次修复。相同 1.13.0 的包必须按 BUILD.txt 源码 SHA 区分。

本次未创建 Release/Tag，未合并默认分支，未修改 UI、程序版本或依赖。
