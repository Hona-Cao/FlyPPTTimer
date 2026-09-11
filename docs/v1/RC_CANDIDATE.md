# 当前交付：直接修复后的绿色测试构建

日期：2026-09-11
Review 分支：`codex/v1-06-manual-test`
程序版本：`1.13.0`
当前测试源码：`96273ddf0e34fe73902f9fcd3ea4a6f356138c18`

## 状态

用户在 RC-2 终审后授权 ChatGPT 直接修复可以确认的逻辑错误，并索要绿色测试 EXE。原 RC-2 冻结结论仅适用于当时审阅的源码；当前不是“全部功能已经稳定”的正式发布批准。

当前产品源码只改 `src/presentation.rs` 的两点：

- 避免无文稿路径的连续全屏状态每次重新 Start，保留 A→未知→A 的既有计时轮次；
- 已知目标必须匹配，即使只有一个放映窗口也不能误控其他文稿；只有目标不可得时才能唯一窗口 fallback。

直接修复提交 `c0b5e37`，格式收尾 `96273dd`；原有测试保留，新增五个定向回归。

## 本次构建及产物来源

GitHub Actions：

https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34550315848

对应源码必须是 `96273ddf0e34fe73902f9fcd3ea4a6f356138c18`。实际最终构建结果、测试数量及交付说明记录在 `DIRECT_FIXES_20260911.md`。没有成功执行记录或 artifact 时不得声称已产出 EXE。

构建成功时，绿色 artifact 名为：

`FlyPPTTimer-portable-win-x64-96273ddf0e34fe73902f9fcd3ea4a6f356138c18`

包内应包含 `FlyPPTTimer.exe`、干净的 `FlyPPTTimer.config.json`、`LICENSE`、标注源码 SHA 的 `BUILD.txt`。ChatGPT 下载交付包可附统一中文手测说明，不改 EXE 内容。Windows 构建/自动测试结果不代替 Windows 11 原生 UI、Office COM、多屏或手机验收。

本次未创建 Release/Tag，不是安装版，也没有覆盖用户旧程序目录。绿色包应解压到新的可写目录，正常双击运行；不要在压缩包内直接运行，不要覆盖唯一工作配置。

## 尚未包含的整改

`CODEX_TASK.md` 已列出 Codex 的定向工作：

- B1：`Session::start_show()` 中途设置失败的恢复路径及 Saved 标志；源码确认存在错误路径遗漏，未声称已经观察到用户文件丢失。
- B2：COM 状态读取失败及退出 join 等待，属于待实机确认风险；按证据最小修复，不增加复杂框架。
- B3：普通桌面入口、PC Remote、临时 Office 文稿、双屏的原生回归；后续若改源码，必须重新生成对应绿色包并更新 BUILD.txt。

本测试构建不应被标成已经包含尚未实施的 B1/B2 整改。Office 只用可以丢弃的临时文稿，用户不承担故障注入或代码定位。

## 手测入口

统一使用 `RC_MANUAL_TEST.md` 五组，不回头逐份重做历史任务：桌面/PC Remote/双屏、实际演示及本次修复、手机跨设备、实际声音/TTS、适用的旧配置/升级。Codex 能可靠完成的子项不再派给用户。

## 历史 RC-2（仅保留溯源）

旧验证提交 `633a830d78ca02be05994364d86f93eaf40a263e`，旧源码基点 `952226d4c114b59334b13d0a7b44c60d89b9f030`。Codex 当时报告 55 passed / 0 failed / 1 ignored 及若干本机集成结果，详见 `CODEX_RESULT.md`。

旧 E 盘本地产物和旧同为 1.13.0 的包不含这次直接修复。旧测试结果不作为新源码通过证据；五张离屏 PNG 不作为原生视觉验收。未经用户明确授权，不发布正式 Release、不创建 Tag、不合并默认分支、不升级版本或依赖。
