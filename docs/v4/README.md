# FlyPPTTimer 4.0 渐进式重构

## 当前状态

版本基线：`4.0.0-alpha.1`

工作分支：`agent/v4-foundation`

当前正式运行入口仍是原 WinForms 应用。新的 WPF 项目已能独立编译，但尚未参与正式发布，也尚未接管用户功能。

## 已完成

1. **.NET 10 基线**
   - 桌面应用、测试和 CI 已迁移到 .NET 10。
   - `global.json` 固定 SDK 基线。

2. **平台无关计时核心**
   - 新增 `FlyPPTTimer.Core`。
   - 计时状态机不再依赖 WinForms/WPF。
   - 现有 `TimerService` 已接入 Core，同时保留原公开事件和操作语义。
   - 单元测试使用可控单调时钟，不再依赖 `Thread.Sleep` 判断主要计时行为。

3. **配置 Schema**
   - 新增独立 `SchemaVersion`，与应用版本号分离。
   - 旧配置迁移只执行一次。
   - 应用小版本升级和普通保存不再覆盖用户的字体、提示文字、蜂鸣和逐页计时设置。

4. **演示控制边界**
   - 新增 `IPresentationControlService`。
   - 新增 `PowerPointPresentationAdapter`，包装现有 PowerPoint/WPS 控制服务。
   - 已提供受限 Codex 接线任务，后续让远程服务和窗口只依赖接口。

5. **WPF 桌面壳**
   - 新增 `FlyPPTTimer.Desktop` WPF 项目。
   - 已建立标准窗口、MVVM ViewModel、主题令牌和基础控件样式。
   - 使用系统窗口边框，不复制旧版手工圆角、缩放和无边框命中测试逻辑。

6. **兼容性与 CI**
   - 修复窗口进程识别未释放 `Process` 对象的问题。
   - CI 自动读取应用版本。
   - 同一 PR 的旧运行会自动取消。
   - 功能分支不再同时触发 push 与 PR 两套重复检查。
   - CI 同时构建 WinForms、Core 和 WPF 项目。

## 最新自动验证

Windows CI 运行编号 `101`：全部通过。

- 三个项目 Release Build：通过，0 errors。
- 桌面测试：201 项全部通过。
- Core 测试：全部通过。
- 现有 WinForms 正式入口的 win-x64 自包含单文件发布：通过。
- SHA-256 校验和生成：通过。
- Artifact 上传：通过。

Artifact：`FlyPPTTimer-v4.0.0-alpha.1-windows-x64`

注意：该 Artifact 仍是用于回归验证的 WinForms 入口，不是 WPF 测试版。

## 下一阶段顺序

1. 使用 `docs/v4/CODEX_PRESENTATION_ADAPTER_WIRING.md` 完成演示接口接线。
2. 抽离 PowerPoint/WPS 状态监控、STA 调度、窗口激活和能力检测。
3. 将设置窗口作为第一个完整 WPF 功能页迁移。
4. 创建独立 WPF 测试构建，供真实 DPI、字体和多显示器测试。
5. 迁移远程控制窗口、悬浮计时窗口和大屏窗口。

## 风险控制

- 不直接修改 Microsoft PowerPoint 或 WPS。
- 不在一次提交中替换全部 UI。
- 不把未经测试的 WPF 入口并入正式发布。
- 每一阶段必须通过自动化检查，再交给真实环境测试。
- `main` 和当前稳定版保持不变，直到草稿 PR 经完整验收。
