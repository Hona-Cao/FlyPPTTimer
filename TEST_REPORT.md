# FlyPPTTimer 测试报告

## v0.18.9 当前验证

- 根因确认：100×30 在句柄创建前先被换算为 125×38，WinForms 创建 Per-Monitor V2 句柄时又把窗口放大到 170×47，并保持左上角不变，导致中心从默认位置原点向右偏移 23 像素。
- 修复后先强制创建真实窗口句柄，再应用最终物理尺寸和中心位置。125% DPI、100×30 设置的实测窗口为 125×44（高度因 18 号单行文字完整显示自动扩展），窗口中心 `1280.5,38`，与计算原点 `1280,38.4` 的差异仅为像素取整。
- 文字采用 `SingleLine + HorizontalCenter + VerticalCenter + NoPadding`。实测 125×44 窗口中文字像素边界为 `22,11..102,33`，左右留白 22/22，上下留白 11/10。
- 自动化测试：121 总数，121 通过，0 失败，0 跳过；Release 编译 0 警告、0 错误。
- EXE：`J:\codex2\FlyPPTTimer_GUI\dist\v0.18.9\FlyPPTTimer.exe`；SHA-256：`0B7FFE17A5DA89579A3536E49ED13E4EAED5C477B0D9154DF12D076B11F05652`。
- 便携 ZIP：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.9-portable-win-x64.zip`；SHA-256：`D944C77943C84DF63F66A6B7A2180D2897DC32271AF6435EB34A11975AC7C247`。
- 安装版：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.9-setup-win-x64.exe`；SHA-256：`3C75C8F1C3C5F4841BDC0F9F1B19FC2369F869CA253C99E808458A08B5D9A897`。

## v0.18.8 当前验证

- SDK：`J:\codex2\FlyPPTTimer_GUI\.dotnet\dotnet.exe`，.NET SDK 8.0.422；Release 编译 0 警告、0 错误。
- 自动化测试：119 总数，119 通过，0 失败，0 跳过；TRX：`C:\Temp\FlyPPTTimer_v0188_tests\v0188.trx`。
- 100×30 真机视觉检查：在本机实际启动紧凑配置，时间冒号位于窗口水平中心，可见数字上下边界围绕垂直中心；截图：`C:\Temp\FlyPPTTimer-v0187-compact-screen.png`。
- 手机网页实取检查：从运行中的嵌入式 HTTP 服务读取页面，确认 `timeup.dismiss` 按钮共 2 个，分别位于计时和演示模块，且无黑屏时仍显示“当前无‘时间到’黑屏”。
- WPS 真机检查：确认外层窗口类为 `PP12FrameClass`；先把实际 WPS 窗口还原为 557×377 小窗，再通过新控制服务打开当前文稿，结果 `IsZoomed=True`、窗口为 2560×1380。Windows 若拒绝强制置前，不再误判最大化失败。
- EXE：`J:\codex2\FlyPPTTimer_GUI\dist\v0.18.8\FlyPPTTimer.exe`；SHA-256：`5ADCC6A9590691F5BE2867E1992AB683E4AA200E583474CAE44A137207E7F6B4`。
- 便携 ZIP：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.8-portable-win-x64.zip`；SHA-256：`9CD5F4923F7D928BCE6ED1491221E3FF17F0482C05736B5ABB778259C4E4E4F1`。
- 安装版：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.8-setup-win-x64.exe`；SHA-256：`F6DF412023CEA57F5286E876DDB94D495275388BAEF093EB81B11C3AE33C0B5A`。

## v0.18.6 当前验证

- SDK：`J:\codex2\FlyPPTTimer_GUI\.dotnet\dotnet.exe`，.NET SDK 8.0.422。
- Release 编译：0 个警告，0 个错误。
- 自动化测试：116 总数，116 通过，0 失败，0 跳过；TRX：`C:\Temp\FlyPPTTimer_v0186_tests_final\v0186-final.trx`。
- 真实启动与视觉检查：已用自包含 EXE 替换正在运行的 v0.18.5，v0.18.6 进程持续响应；默认 140×50 计时窗口实际显示于屏幕中上点位并保持居中。启动截图：`C:\Temp\FlyPPTTimer-v0.18.6-startup.png`。首次启动触发 Windows 防火墙网络访问确认，保留给用户决定。
- WPS 最大化：自动化契约确认应用与文稿窗口都在 `Visible=true` 之前设置最大化；当前 WPS 中存在用户正在使用的文稿，因此没有强制关闭 WPS 做破坏性冷启动，仍需用户在正常使用中验收首次显示是否完全无跳变。
- EXE：`J:\codex2\FlyPPTTimer_GUI\dist\v0.18.6\FlyPPTTimer.exe`；SHA-256：`A8F3518DE08A45004BB569B94A6E6C95980A8674424F4916B9FF36D217A41E37`。
- 便携 ZIP：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.6-portable-win-x64.zip`；SHA-256：`C8D6DF4360B16C03842F3782A212D90BE107DA1397B9436F9581FD13CB75CE6E`。
- 安装版：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.6-setup-win-x64.exe`；SHA-256：`BC79C8814392B8813191EF409889BF644854406FDF10DEE30ABFB36AE78A56D7`。

## v0.18.5 当前验证

- SDK：`J:\codex2\FlyPPTTimer_GUI\.dotnet\dotnet.exe`，.NET SDK 8.0.422。
- Release 编译：0 个警告，0 个错误。
- 自动化测试：110 总数，110 通过，0 失败，0 跳过；TRX：`test-results-v0185/v0.18.5-final.trx`。
- 真实启动与视觉检查：在本机双显示器上启动自包含 EXE，计时窗口在两块屏幕顶端居中；通过计时窗口菜单真实打开设置页，确认单页计时和字体输入已移除、批量设置入口可见。截图：`test-results-v0185/startup-screen.png`、`test-results-v0185/settings-screen.png`。
- 真实演示软件检查：使用 `ppt/6月护士长例会内容.pptx` 经当前控制服务只读打开。本机由 WPS 接管 PowerPoint COM；后备原生窗口路径成功将窗口最大化，窗口状态 `ShowCommand=3`，随后静默关闭文稿和退出演示软件均成功。
- EXE：`J:\codex2\FlyPPTTimer_GUI\dist\v0.18.5\FlyPPTTimer.exe`；SHA-256：`B4C6D0C9101C47F170B3CC90C529C536E2A556F8697FAD991BEE10929E2570E4`。
- 便携 ZIP：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.5-portable-win-x64.zip`；SHA-256：`45F336A054A6EDCFEB11D3C1FAAF5DDC748C4DA79F156DCCD22E9D3F8B3D5190`。
- 安装版：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.5-setup-win-x64.exe`；SHA-256：`27808B212D91A0AD59580BA6E7D92E8C4B14C2536B044021EFC3AF1D636270AC`。

## v0.18.4 当前验证

- SDK：仓库本地 `J:\codex2\FlyPPTTimer_GUI\.dotnet\dotnet.exe`，.NET SDK 8.0.422。
- Release 编译：0 警告、0 错误；自动测试 104 总计，104 通过，0 失败，0 跳过。
- v0.18.3 遗留根因一：悬浮窗先按 140×50 定位，`Show()` 时又被 WinForms 自动 DPI 缩放为约 170×50，左上角不变，因此新增宽度全部向右增长。v0.18.4 禁用显示阶段自动缩放，先按目标显示器 DPI 把 96-DPI 逻辑尺寸转换为最终物理尺寸，再围绕中心定位。
- v0.18.3 遗留根因二：第一次应用后，设置窗口和主程序共用同一个 `AppConfig`；后续位置修改发生在比较前，导致新旧位置被误判相同。v0.18.4 在比较和保存前深拷贝设置草稿。
- 完整设置链路真机：默认 Bounds `1192,7,175×63`；修改时长后 Bounds 完全不变；修改逻辑尺寸至 180×70 后物理 Bounds `1167,-6,225×88`，保持原中心；垂直偏移改为 5% 后 Y=69；水平偏移 -10% 时 X=911，+10% 时 X=1423，左右方向均正确。
- 逻辑尺寸核验：本机 125% DPI 下，140×50 DIP 明确转换为 175×63 物理像素；自动扩宽后的物理宽度会转换回 DIP 保存，避免下次再次放大。
- 真实启动：从独立临时目录启动 v0.18.4，6 秒后进程仍正常运行；随后仅关闭该测试实例。产品版本 0.18.4，文件版本 0.18.4.0。
- 本地 EXE：`J:\codex2\FlyPPTTimer_GUI\dist\v0.18.4\FlyPPTTimer.exe`；SHA-256：`9BC375B62AFEF7B7740BFAB00B570CA535EE1D04117F7481EAB6A7DBB89ECB82`。
- 便携 ZIP：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.4-portable-win-x64.zip`；SHA-256：`5D8A9DEDA86C84EB3F9727B91F874508B91FFBB3AC985CD1733EE28B19F84A91`。
- 安装版：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.4-setup-win-x64.exe`；SHA-256：`489620FD6DEAF56DB59F5FEF5ABF7C78CEA0D3EA6BF01DB34D213764F76757EF`。
- 待人工验收：办公室不同 DPI 双屏间拖动后的物理尺寸切换，以及安装程序完整交互。

## v0.18.3 当前验证

- SDK：仓库本地 `J:\codex2\FlyPPTTimer_GUI\.dotnet\dotnet.exe`，.NET SDK 8.0.422。
- Release 编译：0 警告、0 错误；自动测试 101 总计，101 通过，0 失败，0 跳过。
- 无漂移验证：真实 WinForms 窗口在 140×50 与 191×71 之间往返调整 20 次；每次恢复 140×50 后的位置完全相同，`DistinctSmallLocations=1`，未产生累计向右移动。
- 时间中心规则：专用绘制控件不再使用普通 Label 整段居中；`MM:SS` 的冒号中心固定在窗口水平中心，`HH:MM:SS` 的分钟两位数字中点固定在窗口水平中心；文字实际测量高度的中心固定在窗口垂直中心。
- 实际绘制截图：`test-results-v0183/v0.18.3-mmss-center.png` 与 `test-results-v0183/v0.18.3-hhmmss-center.png`，已实际打开检查。
- 中心保持机制：每个屏幕保存独立的 `PointF` 精确中心；尺寸奇偶变化、字体修改和自动扩宽均使用同一中心，不再从取整后的窗口边界反推下一次中心。
- 真实启动：从独立临时目录启动 v0.18.3，6 秒后进程仍正常运行；随后仅关闭该测试实例。产品版本 0.18.3，文件版本 0.18.3.0。
- 本地 EXE：`J:\codex2\FlyPPTTimer_GUI\dist\v0.18.3\FlyPPTTimer.exe`；SHA-256：`03A250124F567D99755E8F40DDE7D211E4CB122C42BCAAD25F64821C665CBF01`。
- 便携 ZIP：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.3-portable-win-x64.zip`；SHA-256：`B5EA6A388978D4BFF208AE69E6B656F31C9149657BABD2CF390547E1A4FC4D9F`。
- 安装版：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.3-setup-win-x64.exe`；SHA-256：`B621EE2C18A7D421945C0146402D9E20B78749CA6EA09EEF2FE2E9C843622ADB`。
- 待人工验收：不同字体的视觉字形中心、不同 DPI/多显示器拖动后的位置保持，以及安装程序完整交互。

## v0.18.2 当前验证

- SDK：仓库本地 `J:\codex2\FlyPPTTimer_GUI\.dotnet\dotnet.exe`，.NET SDK 8.0.422。
- Release 编译：0 警告、0 错误；自动测试 97 总计，97 通过，0 失败，0 跳过。
- 提示设置：提示 1、提示 2 使用“距离预设时间还剩（秒）”；删除自选提示音启用复选框，选择文件即启用并替换该提示的默认语音，“恢复默认”可清除自选文件。
- 真机提示音：把 `Alarm10.wav` 转为 MP3 后通过应用代码复制为 `prompt1.mp3`；Windows 原生 MCI 返回打开/播放成功，应用日志记录 `Prompt sound playback completed: prompt1.mp3`，确认进入播放并完整结束。旧 Windows Media Player ActiveX 在本机无法打开本地文件，因此现在仅作为 MCI 失败后的回退。
- 闪烁与超时：提示 1、提示 2、计时结束分别保存闪烁样式、闪现时长、隐藏时长和持续秒数；超时文字颜色、背景颜色与前缀归入计时结束区。
- 计时窗口：默认尺寸 140×50；自动测试确认 140×50 与 320×120 两种尺寸围绕同一水平/垂直中心计算。设置应用和文字自动扩宽会保留每个屏幕上的原窗口中心。
- 真实启动：从独立临时目录启动 v0.18.2，6 秒后进程仍正常运行；随后仅关闭该测试实例。产品版本 0.18.2，文件版本 0.18.2.0。
- 本地 EXE：`J:\codex2\FlyPPTTimer_GUI\dist\v0.18.2\FlyPPTTimer.exe`；SHA-256：`C3D34C8B11B5FC6803D5F9016EC88585315880F170585207E5C312CBF596C9F4`。
- 便携 ZIP：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.2-portable-win-x64.zip`；SHA-256：`65CCBC084495ED14981D7FD5F6B339D06847D7E40A4CD826584725828F6177B0`。
- 安装版：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.2-setup-win-x64.exe`；SHA-256：`73FEC2A66A3112E9954B2F363A0EAC53EBF77C20E469D121AF24D009F52ED9BB`。
- 待人工验收：实际观察设置页长列表在不同 DPI 下的滚动与排版；分别试听个人选择的 MP3/WAV/WMA/M4A；拖动后的多屏中心缩放；安装程序完整交互。

## v0.18.1 当前验证

- SDK：仓库本地 `J:\codex2\FlyPPTTimer_GUI\.dotnet\dotnet.exe`，.NET SDK 8.0.422。
- Release 编译：0 警告、0 错误；自动测试 90 总计，90 通过，0 失败，0 跳过。
- 语音提示：提示 1、提示 2 均固定为“时间即将结束”，结束提示固定为“预设时间到”；专用 STA 播放队列串行播放系统语音和自选提示音，操作点击不会中断当前提示。
- 时间到操作：可选择仅提示、全屏黑屏并显示“时间到”，或退出放映。
- 系统音频真机验证：实际切换 Windows 主音频 `False → True → False`，静音状态及恢复状态均读取正确，测试结束后已恢复原状态。
- PowerPoint 真机验证：用临时 PPTX 通过应用只读打开并开始放映后，COM `Saved=True`；在 PC 端结束放映并关闭受控文稿后 `Presentations.Count=0`，未出现保存询问，也未写回原始文稿。
- 本地 EXE：`J:\codex2\FlyPPTTimer_GUI\dist\v0.18.1\FlyPPTTimer.exe`；SHA-256：`38DA0AE9196C43AAF8A7C655E6FD6F07536AE45EB0F9D8A409FB7C8C30E976BE`。
- 便携 ZIP：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.1-portable-win-x64.zip`；SHA-256：`00B581C727F461C95252AE8EF39DC296022AF3A077E1C10FF1C69D3D2A0ECB25`。
- 安装版：`J:\codex2\FlyPPTTimer_GUI\dist\FlyPPTTimer-v0.18.1-setup-win-x64.exe`；SHA-256：`6C3A669B186973DF39D7E309FBC57E2F761042CEAB45A93E89DE5C0D9786D5CC`。
- 整包启动限制：本机已有用户正在运行的 v0.18.0（单实例互斥），因此未强制关闭该进程，也未虚报 v0.18.1 整包窗口启动成功；文件版本、压缩包内容、真实系统音频和真实 PowerPoint 链路均已独立核验。
- 待人工验收：关闭当前 v0.18.0 后启动 v0.18.1；实际听完连续系统语音/自选提示音；多显示器黑屏布局；实体手机的静音状态反馈；安装程序完整交互。

## v0.18.0 当前验证

- SDK：仓库本地 `.dotnet\dotnet.exe`，.NET SDK 8.0.422。
- Release 编译：0 警告、0 错误。
- 自动测试：86 总计，86 通过，0 失败，0 跳过。
- 真实启动：从便携 ZIP 解压到独立临时目录后启动成功，进程持续运行；产品版本 0.18.0，文件版本 0.18.0.0，配置版本 0.18.0。
- 默认配置核验：医疗卫生（蓝白）、宽度 160、字号 18。
- 视觉核验：实际打开设置窗口并截取桌面，悬浮时间在窗口中居中，设置窗口完整可读；截图位于 `test-results-v018/v0.18.0-settings-smoke.png`。
- EXE SHA-256：`02E96379AF6AEE3FD604E853CAA5D4C48A8F75DC3D3874BDF6B185C911E2609A`。
- 便携 ZIP SHA-256：`04428090D6C676C1086A9A07147659D9EE66F7552FC32F8F71439193B8049D68`。
- 安装版 SHA-256：`8EEEE13E1D93C077ED133F0FAEB6EAD9E487B2D620DFF8EC6433D69A0E03133D`。
- 待人工验收：不同实体显示器/DPI 下的自动扩宽提示、手机扫码与防火墙首次放行、自选 MP3 和系统语音实际听感、安装程序完整交互。

## v0.17.0 当前验证

- 范围：手机端调整时长和正/倒计时模式并同步到 PC；倒计时到零停止/继续超时策略；超时颜色与时长显示；“其他设置”项目及作者介绍。
- 本地 .NET SDK：`J:\codex2\FlyPPTTimer_GUI\.dotnet\dotnet.exe`，版本 8.0.422。
- Release 构建：0 个警告，0 个错误；自动化测试 77 项，77 通过，0 失败，0 跳过，覆盖计时边界、远程命令、手机资源、PC 同步和三段式包版本命名。
- 真实 HTTP 链路：手机接口设置 90 秒与正计时后，配置落盘为 `00:01:30 / CountUp`，已打开的 PC 设置窗口同步显示 `00:01:30 / 正计时`。
- 超时链路：1 秒倒计时到零后远程状态为 `IsOvertime=true / Running=true`；暂停后状态为“暂停”，并继续保留超时标记。
- 移动端视觉：使用真实 390×844 浏览器移动视口检查，`innerWidth/clientWidth/scrollWidth/bodyScrollWidth` 均为 390；时分秒、应用时长、模式和控制按钮无横向裁切；超时卡片显示红色“已超时”和超出时长。
- PC 关于页视觉：作者背景、需求来源、联系邮箱、GitHub 入口与共同开发邀请均已实际打开检查。
- 本地 EXE SHA-256：`E3D42869A6722B17FAB0547C989A9F010FE73E2C4FEBC5323617EF152576F9C9`。
- 本地便携 ZIP SHA-256：`86A70E2B3D5189EA32A3719583EA3C4B3FBA62F134C28D18B5C36CEAFD80963A`；ZIP 内 EXE 哈希与本地 EXE 完全一致。
- 本地安装版 SHA-256：`D6B8BF55FD5F9E0317B3F622328694AB3E799621BF7F063C4DF92E23FD0F57C6`。

## v0.16.3 当前验证

- 范围：修复设置窗口文件规则列表中“规则已启用”等状态文案在较高 DPI 下被裁切的问题；其余 v0.16.2 功能保持不变。
- 本地 .NET SDK：`J:\codex2\FlyPPTTimer_GUI\.dotnet\dotnet.exe`，版本 8.0.422。
- Release 构建：0 个警告，0 个错误；自动化测试 72 项，72 通过，0 失败，0 跳过。
- 状态列按当前字体测量全部状态文案，并额外保留 20 像素水平余量；字体或父级 DPI 变化后重新计算。
- 真实设置窗口检查：加载 5 条规则后，自动化读取到 5 个完整“规则已启用”，每个状态区域实测为 160×42 物理像素；截图确认最后一个“用”字完整显示。
- 本地 EXE SHA-256：`4B5226B2ED98FA07CB21C4EE57DE2CF2225A658DAD5848D6B014D7A620496DB6`。
- 本地便携 ZIP SHA-256：`3DA2904D9AA6EA407982ED849029DD7B7E2C4DE5A7CB1063D9E34687BA65A020`；ZIP 内 EXE 哈希与本地 EXE 完全一致。
- 本地安装版 SHA-256：`1E8E464C7BB20CA24E795D1EC1B6C9A1FF9648DC57A32E015FC6DA9A132E5836`。

## v0.16.2 当前验证

- 基线：GitHub 已合并的 v0.16.1，提交 `63e35e8b7664564c240c8b39dc5579f0cfafec32`。
- 范围：Per-Monitor V2 DPI、96 DPI 逻辑尺寸、Standard/Compact 响应式布局，以及远控窗口显示器、位置、客户区尺寸和最大化状态记忆。
- 自动化覆盖：96、120、144、168、192 DPI 转换，响应式断点，位置比例，屏幕回退，工作区限制，窗口状态和旧 JSON 兼容。
- 本地 .NET SDK：`J:\codex2\FlyPPTTimer_GUI\.dotnet\dotnet.exe`，版本 8.0.422。
- Release 构建：0 个警告，0 个错误；自动化测试 71 项，71 通过，0 失败，0 跳过。
- 125% DPI 真机：默认客户区 1475×950 物理像素（1180×760 DIP）；最小客户区 1250×825 物理像素（1000×660 DIP），Standard/Compact 两页均已实际打开检查。
- 截图精确返工真机：服务操作块等距、地址选择器完整内嵌、最小窗口网络提示无需滚动、二维码随卡片变化、放映按钮等高、设置规则列表满宽及底部栏紧凑均已检查。
- 首次打开稳定性：打开后 20 次、每 25ms 连续采样只出现 1 组窗口 Bounds；相近截图尺寸下四个放映按钮实测均为 57 物理像素高。
- 规则同步真机：远控窗口单击一次禁用后，磁盘配置与已打开的设置窗口立即显示禁用；设置窗口重新启用并应用后，磁盘配置、远控窗口和手机端状态修订链同步恢复。
- 窗口记忆真机：1000×660 DIP 与位置重启后精确恢复；最大化状态保存后重启恢复成功。
- 本地 EXE SHA-256：`800A081219BC3BC01C33231DF2F403ABDABF23F60AB4704CB0734154F15E44E3`。
- 本地便携 ZIP SHA-256：`5DF4F216E78A63A32E0D2AA942231DB4ABB0501433ED3E207CC6BE839C450182`。
- 真实 150% 与 125% 跨屏拖动仍须按 `tests/acceptance/v0.16.2/README.md` 在办公室双屏设备验收，不在本地结果中虚报。

## v0.16.0 当前验证

- 基线：$ExpectedHead。
- 范围：完全重写 PC 远程控制窗口 UI；业务服务保持不变。
- 本地 Release 自动化测试：43 项；43 通过，0 失败，0 跳过；构建 0 警告、0 错误。
- 本地 EXE SHA-256：`409479F3AAD6DABF1417315E39D307BE897FBEBF225680DFBD8726AB5BF2325D`。
- 真实 100%、125%、150%、200% DPI、Office/WPS、手机、双屏和演讲者视图仍待人工验收。
## v0.15.0 当前验证

- 基线提交：`07a5d441b35104035fc3149ae6a076824f49348e`。
- 修改范围：仅重做 PC 远程控制 WinForms 窗口、独立远控主题和对应契约测试，并同步版本、CI 与审计材料；业务逻辑保持不变。
- 本地 Release 构建：0 个警告，0 个错误。
- 自动化测试：总计 42 项，通过 42 项，失败 0 项，跳过 0 项；测试时生成 `v0.15.0.trx`，本地 `artifacts` 目录已按提交要求清理且不提交仓库。
- 本地发布 EXE：`dist/v0.15.0/FlyPPTTimer.exe`；SHA-256：`D14C92E64B7D0100CEF776553A2CA4B0C63CE9CEA3001456788DCFB55B18DDD0`。该值仅代表本地构建，不代表 CI Artifact。
- 启动冒烟：程序启动后持续运行 6 秒，未提前退出；随后终止测试实例，结果通过。
- 桌面界面自动检查：托盘型主进程未向当前 Windows 自动化接口暴露可定位窗口，无法自动打开远程控制页；未据此宣称视觉验收通过，侧边导航、页面切换和主从双栏仍需本机人工确认。
- 100%、125%、150%、200% DPI，真实 Office/WPS、双屏、演讲者视图和真实手机仍待人工验收，未伪造结果或截图。

## v0.14.5 当前验证

- 基线：本地 v0.14.4 候选代码，Git 基线提交 `604a3d0007893e6e228588f3e627d2568aaa305a`。
- 本轮范围：统一远程控制窗口及其自定义菜单/确认窗口的字体字号与交互色块高度；演示文稿页改为单一页面滚动。
- 本地 Release 自动化测试：40 项通过，0 项失败，0 项跳过；Release 构建 0 警告、0 错误。
- 本地 `dist/v0.14.5/FlyPPTTimer.exe` SHA-256：`BE6395FEBF2BC01339936FE9946D9919F44BBC251DD38949E2658AA2EBC34FBE`。该文件仅用于本机验收，不作为 PR 审核交付物。
- CI Artifact 哈希将在 Actions 完成并下载复算后记录；CI Artifact 是唯一审核交付物。
- 真实 100%、125%、150% DPI、不同窗口尺寸、PowerPoint/WPS、手机、双屏和演讲者视图仍待人工验收，未伪造截图。

## v0.14.4 当前验证

- 基线：`604a3d0007893e6e228588f3e627d2568aaa305a`。
- 本轮范围：远程控制窗口布局，不触及计时、PowerPoint COM、手机接口、token、端口或托盘业务逻辑。
- 本地 Release 自动化测试：40 项通过，0 项失败，0 项跳过。
- 本地 `dist/v0.14.4/FlyPPTTimer.exe` SHA-256：`05DB75DF5AF866A949E748D4BBC2E180892030ED2B52E6F3B9D8EB61C3F29554`。该文件仅用于本机构建与人工验收准备，不作为 PR 审核交付物；CI Artifact 是唯一审核交付物。
- 真实 100%、125%、150% DPI、不同窗口尺寸、PowerPoint/WPS、手机、双屏和演讲者视图仍待人工验收，未伪造截图。

## v0.14.3 当前验证

- 基线：`cfcd44de5b6907a68095b6a85510736a03f1e242`。
- 本地 Release 自动化测试：40 项通过，0 项失败，0 项跳过。覆盖远控页面的自动高度、字体测量、三条规则最小空间与既有业务契约。
- 本地 `dist/v0.14.3/FlyPPTTimer.exe` SHA-256：`0ABE74A4876DB4E15941506F679555962651AFBC32C1F8C2137F9E9CD2936B5A`。该文件仅用于本机构建与人工验收准备，不作为 PR 审核交付物。
- CI EXE 哈希将在 v0.14.3 Artifact 下载并复算后记录；CI Artifact 是唯一审核交付物。
- 真实 100%、125%、150% DPI、PowerPoint/WPS、手机、双屏和演讲者视图验收待执行，未伪造截图。

## v0.14.2 当前验证

- 分支：`feature/v0.13.2-winforms-ui-polish`，基线提交：`3c33152f508c59c91c1eda316914e998ed29e20b`。
- 本地 Release 自动化测试：39 项通过，0 项失败，0 项跳过。该结果覆盖版本契约、访问链接 token 脱敏、既有远控规则与 PowerPoint 控制契约。
- 本地 `dist/v0.14.2/FlyPPTTimer.exe` SHA-256：`042077A4A21B5F49643D32672FD05E22300101CDAF9EE176DB12CABF0DF23E57`。该文件仅用于本机构建与人工验收准备，不作为 PR 审核交付物。
- CI Windows Actions `29467802850`：39 项通过，0 项失败，0 项跳过。Artifact `FlyPPTTimer-v0.14.2-windows-x64` 内 `publish/FlyPPTTimer.exe` SHA-256：`77DB15DE63CAF9621AAAC559B3E52787B902F45192201E0A6D72DB0D80F044CC`。已下载后复算，结果与 `FlyPPTTimer-v0.14.2.sha256` 和 `CI_ARTIFACT_HASH.txt` 完全一致；该值是 PR 审核交付哈希。
- v0.14.1 的 Artifact、SHA-256、审计材料和历史验证记录完整保留在下方，不以本地 v0.14.2 构建替代其结论。
- v0.14.2 的 100%、125%、150% DPI，默认/最小窗口、真实 PowerPoint/WPS、手机、双屏与演讲者视图尚待人工验收，未创建或伪造截图。

## v0.14.1 历史测试报告

## 范围

- 分支：`feature/v0.13.2-winforms-ui-polish`（继续更新现有 Draft PR）
- 基线提交：`4011324e9bc806f5d3e3f1d76a5c4188ea3ef724`
- 平台：.NET 8、Windows Forms、Windows x64
- 覆盖地址菜单生命周期、PC 远控演示管理、网页内危险操作确认、PowerPoint 前台激活路径、规则时长校验、CI Artifact 哈希核对与 WinForms 体验修复。

## 历史界面验收（v0.13.2）

- 设置窗口六个页面、所有设置项及确定/取消/应用按钮保持原位置和原事件。
- 标题栏、选中页签、卡片、输入框、下拉框、表格和底部按钮显示新的统一视觉层次。
- 远程控制窗口的标题、说明、服务状态、端口、二维码、地址、链接及全部按钮完整显示，无换行裁切。
- 右键菜单实际鼠标弹出成功，菜单右边缘与鼠标 X 坐标一致；点击菜单外空白处后立即关闭。
- 双屏运行时仅设置窗口具有任务栏资格；两个计时悬浮窗和菜单辅助窗均不进入任务栏。
- 截图进程使用 Per-Monitor DPI Awareness；实测读取主屏物理尺寸 `2560x1600`，副屏物理尺寸 `2560x1440`，排除 DPI 虚拟化导致的左上角局部截图。
- 验收截图：`tests/acceptance/v0.13.2/settings-polished-physical.png`、`tests/acceptance/v0.13.2/remote-polished-physical.png`。

## 稳定性验证

- v0.14.0 Release 单文件发布：成功，0 个警告、0 个错误。
- v0.14.0 EXE SHA-256：`100C4C1AB754BA1796A64A3F4C17A9E4FB1D8E1A59F0C8E8C9CC12D6E522E543`。
- v0.14.0 自动化测试：20 项通过，0 项失败，0 项跳过。
- v0.14.1 Debug 自动化测试：36 项通过，0 项失败，0 项跳过。
- 本地 `dist/v0.14.1/FlyPPTTimer.exe` SHA-256：`E5E024CDF7A00BC308BCDB0B4CAB40EE9319EAFF3B49198AAEAC2DE4AA1601C3`。本地文件仅用于本机构建排错，不作为 PR 审核交付物。
- CI Artifact 审核 EXE（Actions `29460620706`）SHA-256：`6C2AB904C96B154AED00CAF02AB9F3DD0179FF10EFDCCD5D58496715DC1087BD`。已下载 Artifact 并重新计算，结果与 Artifact 内 `FlyPPTTimer-v0.14.1.sha256` 及 `CI_ARTIFACT_HASH.txt` 一致。
- 单文件发布已关闭压缩，以确保同一提交的 Artifact 可复现；Artifact 审核只使用 CI EXE 哈希。
- 地址菜单崩溃根因和脱敏日志说明见 `docs/audit/v0.14.1/README.md`；原始日志不提交。

## 人工验证保留项

- Microsoft PowerPoint COM 与真实演示文稿完整流程。
- 手机在 Clash 规则模式、电脑在 Clash TUN 模式下的真机远控。
- 100%、125%、150% DPI 的长期交互体验。
- Microsoft PowerPoint：只读打开、结束放映、关闭受控文稿、拒绝关闭用户文稿、无用户文稿时退出应用。
- WPS：进程检测、状态提示及强制退出二次确认；未验证的 WPS COM 文稿关闭功能必须保持禁用。
- 手机端首次启动 PowerPoint 的 1–3 秒进度提示、重复点击忙碌保护和四种演示退出操作。
- 地址菜单真实鼠标打开/关闭 30 次、PC 规则即时同步、手机网页退出电脑端 PowerPoint 后持续连接。
- PowerPoint 小窗口最大化、多个 PowerPoint 窗口的路径匹配、双显示器与演讲者视图、放映前台和计时悬浮窗层级。
- 必须使用新提交对应的 CI Artifact 重新进行上述人工验收；当前仅完成 Artifact 下载、SHA-256 复算与自动化验证，未伪造 GUI/Office 验收截图。
