# v0.14.1 审计记录

## 地址菜单崩溃

v0.14.0 的本地日志在 2026-07-15 记录了 `System.ObjectDisposedException`，对象为 `System.Windows.Forms.ContextMenuStrip`。堆栈位于 WinForms 的 `ToolStripManager.ModalMenuFilter.CloseActiveDropDown`，表明菜单在 WinForms 完成关闭流程前被释放。

日志中的地址、token、文稿路径和用户信息均未复制到仓库。修复后 `FlatSelectBox` 由控件长期持有单个菜单；菜单关闭和点选不再释放对象，仅在控件 `Dispose` 时释放。

## 验收边界

- 自动化检查验证菜单生命周期代码、手机页没有原生 `confirm()`/关闭页面调用、PC 远控包含演示管理命令。
- PowerPoint/WPS、多显示器、演讲者视图和真实菜单 30 次循环属于本机人工验收，不能由 CI 伪造。

## EXE 审核来源

- 本地 `dist` EXE 与 GitHub Hosted Runner 发布的单文件 EXE 允许不同，不能互相代替。
- PR 审核只采用 GitHub Actions Artifact 内的 `publish/FlyPPTTimer.exe` 与同包的 `FlyPPTTimer-v0.14.1.sha256`。
- 已下载 Actions `29405464934`（提交 `9f218f94391cab02a61406cdef8ff767b9abd401`）的 Artifact 并复算：`7895D17094099C327A90EC1F663BCF69DBBE110EFC70C1D0F62365581066A813`，与 `FlyPPTTimer-v0.14.1.sha256` 及 `CI_ARTIFACT_HASH.txt` 一致。后续代码提交将产生新的 Artifact，必须重新记录对应哈希。
