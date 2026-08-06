# FlyPPTTimer 4.0 渐进式重构

## 当前状态

版本基线：`4.0.0-alpha.1`

工作分支：`agent/v4-foundation`

正式普通计时窗口和大屏计时窗口已由同进程 WPF 接管；它们继续复用原单实例、托盘、命令、计时 Core、远程和 Presentation 服务。兼容 composition root 与尚未迁移的窗口仍为 WinForms，不能提前删除。WPF 设置已随同一 Artifact 发布。

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

5. **WPF 桌面与设置**
   - 新增 `FlyPPTTimer.Desktop` WPF 项目。
   - 已建立标准窗口、MVVM ViewModel、主题令牌和基础控件样式。
   - 普通计时窗已迁移为无边框 WPF Window；保留置顶、透明、形状、穿透、锁定、拖动、右键菜单和显示语义。
   - 大屏已迁移为标准可缩放 WPF Window，只允许非主屏。
   - `OverlayPlacementService` 统一九宫格、百分比偏移、负坐标与 DPI 物理像素定位。

6. **兼容性与 CI**
   - 修复窗口进程识别未释放 `Process` 对象的问题。
   - CI 自动读取应用版本。
   - 同一 PR 的旧运行会自动取消。
   - 功能分支不再同时触发 push 与 PR 两套重复检查。
   - CI 同时构建 WinForms、Core 和 WPF 项目。

## 最新自动验证

阶段 1：三个 Release Build 均 0 warnings / 0 errors；桌面 274/274、Core 4/4；`win-x64` 双自包含单文件发布通过。发布版 UI Automation 覆盖 WPF 设置四类控件、设置退出后的主程序响应，以及正式 WPF 计时窗 F3 计时和 F5 显隐。[Windows CI 31093177173](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/31093177173) 成功并上传 Artifact。

## 下一阶段顺序

1. 完成 WPF 规则、提醒/语音/声音、完整外观与显示设置。
2. 完成全部快捷键、远程、语言、更新和其他设置。
3. 用真实 v0.30.2 配置 fixture 验证升级、未知字段、token、规则和声音路径保留。
4. WPF 设置完整覆盖后移除“经典设置”依赖，但保留回归所需兼容实现直到矩阵签收。

## 风险控制

- 不直接修改 Microsoft PowerPoint 或 WPS。
- 不在一次提交中替换全部 UI。
- 不把未经测试的 WPF 入口并入正式发布。
- 每一阶段必须通过自动化检查，再交给真实环境测试。
- `main` 和当前稳定版保持不变，直到草稿 PR 经完整验收。
