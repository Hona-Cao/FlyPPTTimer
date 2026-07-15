# FlyPPTTimer v0.14.1 测试报告

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
- 本地 `dist/v0.14.1/FlyPPTTimer.exe` SHA-256：`CC927129AD66F287890501642398D018891AF20C2B6E71A121F6E4177D142304`。本地文件仅用于本机构建排错，不作为 PR 审核交付物。
- CI Artifact 审核 EXE（Actions `29401118043`）SHA-256：`4D90C1A541F346CA5C6761F9B0C986861373C9C45CB3EE967E76DA6AAE150742`。已下载 Artifact 并重新计算，结果与 Artifact 内 `FlyPPTTimer-v0.14.1.sha256` 一致。
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
