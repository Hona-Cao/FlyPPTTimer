[记录目录](../README.md) · [原始汇总](../../v1/CODEX_RESULT.md)

# 2026-09-11 用户反馈整改结果（1.13.0）

本节为当前结果；后面的 RC-2 记录仅作历史证据。基点为 `5c1cfa9fdd3e3c9c5af0e3c69ac28639a02f625b`，已核对 User feedback fixes 工作流成功并接收 F02～F08 的直接修复。最终源码 SHA 见绿色包 `BUILD.txt`。未升级 Rust 1.92.0、依赖版本或 Cargo.lock；未发布 Release、Tag 或合并默认分支。

## 分项状态

| 项目 | 状态 | 本轮证据与边界 |
|---|---|---|
| F01 跨屏尺寸 | 已修且真机验证 | 150%/125% 双屏；Settings、PC Remote 各绕边 3 圈，再跨屏反向往返 10 次。Settings 返回主屏始终 1350×975 客户区；副屏 1125×813，关闭后同进程重开不变。Remote 875×638（125%）/1050×766（150%），无累计漂移。主动扩大至 1200×856 后跨屏和最大化还原保持；贴边吸附后重开 1200×857，仅首次逻辑取整 1 px。Timer 跨屏后为 150×53/125×44。 |
| F02 底栏留白 | 已修且真机验证 | 接收布局调整；原生检查发现滚动内容仍可能进入底栏，进一步限定 viewport 高度并增加裁剪矩形。底栏三按钮保持等高和固定外边距。 |
| F03 规则编辑卡 | 已修且真机验证 | 保留 54 DIP 编辑卡；用可丢弃 PPTX 的规则检查 Settings 和 PC Remote，窗口增高不拉伸编辑卡。 |
| F04 滚动区域 | 已修且真机验证 | 保留滚动条独立通道；给四个 ScrollView 添加明确裁剪边界，防止内容越过标题、编辑区和底栏。 |
| F05 作者说明 | 已修且真机验证 | 中文首段、段落和“祝大家使用愉快”末句完整；确定按钮可见。中英文布局及两种 DPI 的实际截图在临时证据目录。 |
| F06 蓝条/键盘 | 已修且真机验证 | 无顶部装饰蓝条；修正操作按钮组未遵守弹窗禁用状态。作者弹窗 Tab 后焦点仍在确定，Space/Enter 正常关闭，背景按钮禁用。 |
| F07 单一入口/热点 | 仅源码或自动验证；手机环境限制 | UI、QR、复制、打开采用实际网卡地址、当前端口和 token；网络/令牌变化每秒更新，Settings 仅刷新只读状态，不覆盖未应用端口。当前 Wi-Fi 192.168.36.125（网关 .1）与 Meta/StarVPN 并存，本机 LAN 正确 token/page 200，无/错 token 403。没有真实手机扫码验收；未修改防火墙。 |
| F08 原生音频/更新 | 已修且真机验证；听感/杀软归因环境限制 | WAV/MP3/WMA/M4A 中文空格路径首次、重复及直接原生 WMP 回退均完成；SAPI、静音切换及恢复通过。音频失败入日志，队列有界，回退启动及停止进展有超时。更新安装交接改为本 EXE 的原生等待分支，真实无害测试安装器在父进程退出 236ms 后启动，helper exit 0，无 ps1。未能取得用户所报杀软拦截记录，不能断言根因或保证所有杀软放行。 |
| F09 全屏到时 | 已修且真机验证；单屏/现场投影确认待补 | 复现旧遮罩仅 324×138；改为每屏独立 Slint fullscreen，连续三轮测得 2560×1600/2560×1440，所有四角纯黑。到时后保持；翻页命令、普通点击不解除；Remote reset 和本地 F4 清除两屏。退出前 2 层、退出后 0 层且进程正常退出。“仅提示/退出放映”模式用 F3 启动短计时，均无黑色遮罩（`non-black-modes.json`；本次未运行实际放映）。PowerPoint 联动补测在创建 COM 实例时返回 80080005，尚未打开临时文稿，故实际放映结束/下一页与遮罩联动仍留现场确认；不能用翻页 HTTP 命令代替实际放映证据。没有改变系统分辨率或安全组合键。 |
| B1 放映范围清理 | 已修且真机验证 | 完整快照后才写入；每个 put/Run 失败都尝试全部恢复；恢复失败不标 Saved=true，原本 dirty 不标 clean。4 个定向回归覆盖写入/恢复/Run 失败、阻塞 worker 和未知状态；PowerPoint、WPS 临时三页文稿 clean/dirty 各一轮，范围及 Saved 均正确。 |
| B2 COM 错误与退出 | 仅源码或自动验证 | 明确区分未知采样，未知不触发自动结束，后续确定结束正常生效；Drop 不等待仍卡在 COM 的线程。用受控阻塞 worker 证明退出不等待，不杀 Office，不保留完整旧状态缓存。真实 Office 拒绝响应的所有场景未穷举。 |

## F01 根因与实现

临时消息日志实际记录了嵌套 DPI：外层准备 144 DPI，内部又回到 120 DPI，外层随后仍按旧比例写入。旧校正会放大；单纯去掉校正也不足以覆盖 Winit 的嵌套尺寸计算。最终只在 DPI 消息范围内改写待应用的 `WINDOWPOS`，从最后一次用户主动调整后的逻辑客户区尺寸计算；不在 DPI 回调里调用 SetWindowPos，不从中间物理尺寸继续乘比例。WM_SIZING/WM_EXITSIZEMOVE 区分主动拉伸与移动，最大化和吸附不更新普通尺寸基准。保存 WINDOWPLACEMENT 时扣除窗口装饰，并先定位目标屏再恢复物理客户区尺寸。

未使用固定窗口大小、禁用缩放、循环延迟校正或递归 SetWindowPos。曾尝试的 Winit 事件接入和 GetWindowSubclass 均已移除；后者在本机 Comctl32 下缺少导出，已用兼容窗口属性替代，失败中间 EXE 不交付。

## 验证与交付

- 最终统一检查：`cargo fmt --all -- --check`、`cargo clippy --all-targets --locked -- -D warnings`、`cargo test --locked`、`cargo build --locked --release`。普通测试 66 passed / 0 failed / 3 ignored；另显式执行音频与可丢弃文稿两个真实 Windows ignored 测试均通过。旧 COM 连接 smoke 不擅自运行，避免影响现有文稿。
- 临时数据仅在 `target/feedback-temp/`：DPI 前后几何/消息日志、`settings-native-size.jsonl`、`remote-native-size.jsonl`、`remote-resize-reopen.jsonl`、`black-rounds.json`、`black-click-f4.json`、`black-exit.json`、`lan-http.json`、`audio-test.log`、`native-range-test.log`、`native-handoff-result.json` 及应用窗口截图。测试 token、规则、日志不放入 ZIP。
- 最终绿色包：`artifacts/feedback/FlyPPTTimer-v1.13.0-feedback-win-x64.zip`，包含当前编译 EXE、微软签名运行库、干净配置、LICENSE、BUILD.txt、运行库说明和一页测试说明。源码 SHA 由提交后打包时写入 BUILD。两个 CRT 均为 Microsoft 签名 Valid；最终 EXE SHA256 `DEF866A5D0CA15FC9992B04825D8C7F40F69E7984F997B7244DE3BEDF0008906`。副本真实启动并加载同目录 VCRUNTIME140.dll，完成中文 125%/150% UI 检查后正常退出（`final-loader.json`）。
- 剩余体验确认仅用 `RC_MANUAL_TEST.md`：手机热点扫码、真实听感/安全软件、自己的跨屏路径和单屏/投影放映效果。电脑侧回归不再整包交给用户重做。

---
