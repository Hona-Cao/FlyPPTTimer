# FlyPPTTimer V1 — 当前交接

## 最新状态：反馈整改完成，等待用户四项最终体验确认

日期：2026-09-11。Review 分支：`codex/v1-06-manual-test`。版本 `1.13.0`，Rust `1.92.0`。

产品源码冻结候选为：`bf00cf0dc6ccec337141520f385f8e37d5dab639`。

后续提交 `eeb4ed6315d956c1a1e3aeace01abb6894107d59` 只用于把该源码的 Actions 构建加入 Microsoft 签名 app-local VC Runtime；再后的任务/交接文档提交也不改变产品源码。任何绿色包都以 `BUILD.txt` 中的 product source SHA 判断，不以分支当前文档 HEAD 或相同的 1.13.0 版本号猜测。

## 本轮已经收口

用户在 96273dd 测试包中反馈 F01～F09。Codex 在接收 ChatGPT 直接修复后完成最终 `bf00cf0`：

- F01：混合 DPI 跨屏尺寸累计漂移改为在 DPI 转换的待应用 WINDOWPOS 中保持最后一次用户主动逻辑客户区尺寸；实现侧在 150%/125% 双屏做了绕边、反向往返、拉伸、最大化/还原和重开验证。
- F02～F06：底栏留白、紧凑规则编辑卡、滚动条独立通道与裁剪、作者说明完整显示、去掉说明弹窗蓝色顶条以及弹窗键盘/背景禁用状态均完成原生检查。
- F07：Remote 只使用实际网卡本机单播地址作为主要手机入口，UI/二维码/复制/打开一致；电脑侧 LAN HTTP 与 token 鉴权通过，但真实手机热点仍必须由用户确认。
- F08：提示音不再启动 PowerShell，MCI 失败改用进程内 Windows Media Player COM；TTS 仍为 SAPI。更新安装等待也改成本 EXE 原生 helper 路径，不再生成/执行 ps1。常用音频格式、中文空格路径、SAPI、静音恢复和无害更新交接已做真实 Windows 验证。
- F09：“黑屏并显示时间到”改为每个显示器一个 Slint fullscreen 纯黑顶层窗口；实现侧双屏重复测得完整物理覆盖，普通点击/翻页不解除，F4/Remote Reset 可解除，退出清理。
- B1：放映临时 RangeType/StartingSlide/EndingSlide 在写入或 Run 失败时也尝试完整恢复；恢复不完整不标 Saved=true，原本 dirty 不标 clean。可丢弃 PowerPoint/WPS 文稿的 clean/dirty 验证已记录。
- B2：COM 读取失败作为 unknown sample，不再直接等同于放映结束；PresentationService Drop 不等待仍阻塞的 COM worker。

最终普通自动检查：66 passed / 0 failed / 3 ignored；另外显式运行了真实音频测试和可丢弃 Office 文稿范围恢复测试并通过。完整证据和限制见 `CODEX_RESULT.md`。

## 最终绿色测试包

`RC portable review` 对 `bf00cf0` 成功构建。随后 `RC portable runtime assembly` 从该准确 artifact 组装运行库，验证两个 x64 VC Runtime 文件 Microsoft Authenticode 有效，并实际启动 EXE、确认加载的是同目录 `VCRUNTIME140.dll`。最终包应含：

- `FlyPPTTimer.exe`
- `FlyPPTTimer.config.json`
- `vcruntime140.dll`
- `vcruntime140_1.dll`
- `LICENSE`
- `BUILD.txt`
- `THIRD_PARTY_RUNTIME.txt`

`BUILD.txt` 必须明确写：`Product source: bf00cf0dc6ccec337141520f385f8e37d5dab639`。

这是 review 绿色包，不是正式 Release/安装版；主 EXE 未做 Authenticode 签名。不要与 96273dd、5c1cfa9 或更早同为 1.13.0 的包混用。

## 用户只需要确认四项

以 `RC_MANUAL_TEST.md` 为唯一入口：

1. 在用户原来能快速复现问题的真实双屏上，Settings 与 PC Remote 沿两屏边缘按原路径各绕一次；确认不再累计变大/缩小、控件不变形。
2. 电脑连接真实手机热点/同 LAN 后，用 Remote 的唯一主要二维码/地址在手机打开，试 Start/Pause/Resume/Reset 和一条临时文稿命令。失败只记录超时/拒绝/403/404/页面能开但命令失败，不关闭整个防火墙、不公开 token。
3. 安全软件保持正常开启，实际听提示音首次+第二次和 TTS；不应再看到产品音频路径启动 PowerShell。如仍拦截，记录安全软件名称、被拦截进程和入口，不要求用户排查代码。
4. 用可丢弃文稿在真实单屏/投影场景做短计时。“黑屏并显示时间到”必须完整盖住实际输出，普通点击/下一页不能解除；F4 或 Remote Reset 等主持人明确操作解除。另确认“仅提示/退出放映”不误出现该黑屏。

除用户反馈新的稳定 P0/P1 外，当前停止主动编码，不再扩大审查或 UI 重做。

## 持久约束

`AGENTS.md` 阅读顺序仍有效；`APPROVED_PRODUCT_DEVIATIONS.md` 优先于冲突旧基线。Remote 不恢复随机端口控件；PC Remote 演示文稿页只做规则管理，Web/mobile Remote 保留演示控制。

保留既有 F3 Pause/Resume、Settings 配置合并、Import/Reset、token 防护、端口输入、多选批量、空路径 fullscreen round、多放映目标匹配等修复和测试。

保护用户配置和真实 Office 文稿；不强推、不合并默认分支、不升级依赖/版本、不创建 Release/Tag。用户反馈前不要再次编码。
