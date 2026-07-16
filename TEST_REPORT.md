# FlyPPTTimer 测试报告

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
