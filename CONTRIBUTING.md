# 参与维护 FlyPPTTimer

感谢你愿意帮助改进 FlyPPTTimer。代码、测试、文档、界面适配、使用反馈和兼容性验证都很有价值。

## 可以参与的方向

- 修复计时、提示、文件规则或远程控制问题。
- 改善不同分辨率、DPI 和多显示器下的界面适配。
- 补充 Microsoft PowerPoint 或 WPS 不同版本的兼容性验证。
- 完善自动化测试、使用文档、截图和翻译。
- 提交清晰、可复现的 Bug 报告或功能建议。

## 提交 Issue 前

请先搜索现有 Issue 和 [CHANGELOG.md](CHANGELOG.md)，确认问题尚未被记录或修复。

Bug 报告建议包含：

- FlyPPTTimer 版本。
- Windows 版本。
- Microsoft PowerPoint 或 WPS 的具体版本。
- 屏幕分辨率、缩放比例和显示器数量。
- 复现步骤、预期结果和实际结果。
- 必要的日志片段和脱敏截图。

请勿公开患者信息、真实工作材料、完整本地路径、仍然有效的远控二维码/token、邮箱口令或其他敏感信息。

## 开发环境

- Windows 10/11。
- .NET 8 SDK；也可使用仓库中的 `.dotnet`。
- PowerShell。
- 建议安装 Microsoft PowerPoint 或 WPS，用于相关功能的真实验证。

## 获取依赖、构建与测试

```powershell
.\.dotnet\dotnet.exe restore
.\.dotnet\dotnet.exe build src\FlyPPTTimer\FlyPPTTimer.csproj -c Release
.\.dotnet\dotnet.exe test tests\FlyPPTTimer.Tests\FlyPPTTimer.Tests.csproj -c Release
```

也可以运行统一构建脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

正式安装包使用 Inno Setup 6 的 LZMA2 压缩生成。需要打包时先安装编译器：

```powershell
winget install --id JRSoftware.InnoSetup --exact
powershell -NoProfile -ExecutionPolicy Bypass -File .\package_release.ps1
```

## 开发原则

- 一个 Pull Request 尽量只解决一个明确主题。
- 保持现有 C#/.NET 8/Windows Forms 技术栈，避免为单一功能引入重量级依赖。
- UI 改动应同时考虑 100%、125%、150% DPI，以及标准和紧凑窗口布局。
- 网络接口变更必须继续验证 token、防止任意路径访问，并避免在日志中记录敏感查询参数。
- PowerPoint/WPS 文稿应尽可能只读操作，不得无故修改用户文件或触发保存询问。
- 配置结构变更应保持旧配置兼容，并为默认值和迁移行为增加测试。
- 用户可见文案应清晰、简短，并优先使用中文。

## 测试要求

提交前至少完成：

1. Release 构建无错误。
2. 全部自动化测试通过。
3. 手工验证本次改动直接影响的功能。

不同类型的改动还应补充：

- UI：提供脱敏前后截图，并写明分辨率和 DPI。
- 计时：覆盖正计时、倒计时、暂停、恢复、停止、超时边界。
- 手机遥控：验证断线、重连、忙碌状态和电脑端同步。
- PowerPoint/WPS：写明真实软件版本、打开方式、放映和关闭结果。
- 多显示器：说明每块屏幕的分辨率、缩放比例和跨屏结果。

## 提交与 Pull Request

- 使用能说明目的的分支名和提交信息。
- 不要提交 `dist/`、`releases/`、`logs/`、个人配置、真实 PPT、测试结果目录或 IDE 临时文件。
- 不要使用 `git add .` 把无关文件一起提交。
- 新增或修复功能时，请同步更新测试和必要文档。

Pull Request 描述应包含：

- 改了什么。
- 为什么要改。
- 对用户有什么影响。
- 如何验证。
- 仍然存在的限制或需要人工验收的部分。

## PR 检查清单

- [ ] 改动范围单一且没有夹带无关文件。
- [ ] Release 构建通过。
- [ ] 自动化测试通过。
- [ ] UI 截图和日志已经脱敏。
- [ ] 新行为有测试或说明无法自动测试的原因。
- [ ] README、CHANGELOG 或配置示例已按需要更新。
- [ ] 没有提交安装包、便携包、token、个人配置或真实演示文稿。

## 发布

版本号、标签、安装包、便携包和 GitHub Release 由项目维护者统一生成。普通 Pull Request 不应自行创建正式 tag 或 Release。

## 交流

- 功能建议和 Bug：[GitHub Issues](https://github.com/Hona-Cao/FlyPPTTimer/issues)
- 联系作者：[`caohunan@smail.nju.edu.cn`](mailto:caohunan@smail.nju.edu.cn)

参与讨论时请尊重不同经验背景，围绕问题本身提供可验证的信息和建设性建议。
