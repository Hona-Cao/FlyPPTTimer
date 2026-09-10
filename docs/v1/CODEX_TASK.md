# 当前任务：UX13 审核后稳定性修复 + 新电脑环境预检

当前分支：`codex/v1-06-manual-test`

基线提交：`007f4dc2aab1c9e5c6e81cfa53961263c013f2e5`

## 本轮目标

V1 仍然是对 v0.30.2 的 Rust + Slint 重构，不是功能升级。

本轮不要继续泛化 UX 优化，也不要重构已经稳定的模块。目标只有两部分：

1. **先确认新电脑的开发 / 构建 / 手测环境是否准备好。**
2. **修复 ChatGPT 对 UX13 最新源码审核后确认的稳定性问题。**

用户最终要求不变：

- 保留并稳定运行目前全部既有功能；
- 不因重构删除或改变 v0.30.2 已有功能与行为；
- 当前整体界面方向、排版和体验可以保留；
- 后续只处理明确的功能 Bug、交互一致性和必要的视觉细节；
- 软件最终应当美观、小巧、稳定、快速，界面风格统一和谐。

---

# 0. 新电脑环境预检：必须先做

这是新电脑，本地环境可能尚未准备完整。**先检查环境，确认能否构建，再开始改源码。**

不得因为本机缺少环境就擅自升级项目依赖、改 `Cargo.toml` 版本、替换 Slint 版本、改变 Windows API 实现或修改业务逻辑。

## 0.1 先确认仓库状态

执行并记录：

```powershell
git status --short
git branch --show-current
git rev-parse HEAD
git remote -v
```

要求：

- 当前工作分支应为 `codex/v1-06-manual-test`；
- 先 `git fetch` / `git pull` 到 GitHub 远端最新状态；
- 本轮开始时不得覆盖用户本地未提交修改；若存在未提交修改，只报告，不擅自丢弃。

## 0.2 检查 Rust / Windows 构建环境

至少检查并记录：

```powershell
rustc --version
cargo --version
rustup show
where.exe rustc
where.exe cargo
```

确认：

- Rust toolchain 可以正常使用；
- MSVC Windows target 可用；
- 当前项目 `Cargo.lock` 可以使用，不先升级依赖。

然后做最小构建探测：

```powershell
cargo check
```

如果 `build.rs` 报 Windows resource compiler 缺失，再检查：

```powershell
where.exe rc.exe
where.exe cl.exe
```

必要时确认 Visual Studio Build Tools / Windows SDK 是否已安装。

**只安装项目现有构建所需要的官方工具链，不改项目源码来绕过环境缺失。**

## 0.3 检查可选的真实手测环境

分别检查并报告“有 / 无 / 暂不可测”，但这些缺失不能阻止纯源码修复：

- Microsoft PowerPoint；
- WPS Presentation；
- 至少一台可用于手机 Remote 的同局域网设备；
- 双屏 / 混合 DPI 环境；
- Inno Setup 6（仅正式 installer 收口时需要）；
- ffmpeg（只有需要复用现有截图辅助流程时才需要，不是本轮源码修复前置条件）。

如果 Office、WPS、双屏等暂缺：

- 不要为了“补测试”建立新的复杂模拟系统；
- 对不能在当前电脑完成的真机项目明确记为“待用户手测”；
- 继续完成可以通过源码和最小自动测试证明的修复。

## 0.4 环境预检结果写入 CODEX_RESULT

在 `docs/v1/CODEX_RESULT.md` 新增本轮记录，先写一段“新电脑环境预检”，包括：

- 当前 HEAD；
- Rust / Cargo 版本；
- Windows SDK / `rc.exe` 是否可用；
- `cargo check` 是否通过；
- PowerPoint / WPS / 双屏 / Inno Setup / ffmpeg 当前可用性；
- 实际为环境准备做了什么。

如果环境无法完成 `cargo check`，先解决构建环境；仍无法解决时，停止源码修改并把具体缺失项和错误原文写入结果，不要猜修源码。

---

# 1. 修复 F3 Start/Pause 状态转换错误

## 已确认问题

`src/app.rs` 的 `handle_command()` 中，`startPause` 当前逻辑是：

- Running → `pause()`；
- 其他状态 → `start()`。

这会导致 Paused 状态再次按 F3 时调用 `start()`，从头重新计时，而不是继续。

## 正确行为

保持 v0.30.2：

- `Running` → `pause()`；
- `Paused` → `resume()`；
- `Stopped` / `Finished` → 新一轮 `start()`；
- 从 Paused 恢复时不得把已经累计的 elapsed 清零；
- Resume 不应无条件重置本轮已经触发的提醒状态。

## 验证

新增或扩展**最小必要测试**，覆盖命令入口的状态选择，不能只依赖 `Timer` 自身已有的 pause/resume 单元测试。

至少验证：

`Start → 经过一段时间 → F3 pause → 等待 → F3 resume → 继续原进度`。

不要借此重构 Timer。

---

# 2. 修复 Settings 草稿覆盖外部已保存配置

## 已确认问题

设置窗口创建时复制完整 `AppConfig` 作为 draft。设置窗口可以被隐藏而保留 dirty 草稿；PC Remote 等其他入口 meanwhile 会直接修改并保存共享 `applied` 配置。

当前 `commit_draft()` 最终整份：

```rust
*applied = draft.clone();
```

因此存在明确覆盖路径：

1. 打开设置；
2. 修改任意设置但不应用；
3. 切到 PC Remote；
4. 在 Remote 添加 / 删除 / 修改文件规则，或更新 Remote token / port 等并保存；
5. 回到旧设置窗口点击“应用”；
6. 旧 draft 可能把步骤 4 的新配置覆盖掉。

## 修复目标

要求最小、直接，不引入复杂版本控制或配置框架。

需要保证：

- Settings 只提交本次设置会话**实际修改过的字段 / 领域**，不能用过期完整快照覆盖其他入口的新修改；
- Remote 在设置窗口打开期间保存的新规则、token、端口等不能因为 Settings 后续 Apply 而丢失；
- Cancel / Discard 继续只丢弃 Settings 自己的未应用更改；
- 现有“有未应用的更改”语义保持；
- 不增加数据库、事务框架、复杂 revision / merge 系统。

可以采用简单的 dirty-field / dirty-section 合并，或其他等价的轻量方案；选择实现最直接、最容易证明正确的一种。

## 必须有最小回归验证

至少覆盖：

- Settings 修改外观但不应用；
- 外部修改 `rules` 或 Remote 配置；
- Settings Apply；
- Settings 的外观修改生效；
- 外部新配置仍保留。

---

# 3. 修复 PC Remote 端口输入被周期刷新覆盖

## 已确认问题

`populate_remote_connection_window()` 会持续执行：

```rust
window.set_next_port(config.remote_control.port.to_string().into());
```

而 UI 的 `next-port` 又与 `LineEdit` 双向绑定。主循环会频繁刷新 Remote 状态，因此用户正在编辑的端口内容可能被保存值重新覆盖。

## 正确行为

- 服务状态、当前端口、设备数量等可以周期刷新；
- **用户正在编辑的“下次服务端口”不能被后台 refresh 重置**；
- 初始化窗口、成功应用端口、明确重新加载时才同步 editor；
- 输入一半、停顿几秒，再继续输入，不应跳回旧值；
- 点击应用后以完整用户输入为准；
- 保持现有 Remote 协议与端口 fallback 行为，不扩展功能。

为此可以把“刷新状态”和“刷新编辑字段”拆开，不要引入复杂状态机。

---

# 4. 修复 PC Remote 规则多选 / 编辑 / 批量保存一致性

本轮将以下问题视为**同一组交互一致性修复**，一起处理，避免反复打补丁。

## 4.1 多选保存不应意外统一 enabled

当前普通“保存”会对选中规则同时写：

- duration；
- mode；
- enabled。

但 UX13 的编辑卡只暴露时长和模式，启用状态已经从详情编辑卡移除。

因此不能把一个隐藏 / 旧 editor 状态的 `rule_enabled` 批量写给所有选中规则。

要求：

- 用户只改时长 / 模式时，仅更新用户实际可编辑的字段；
- 不得意外启用或禁用其他已选规则；
- 列表中的 enabled 状态继续保持各自原值。

## 4.2 Ctrl 取消当前行后，编辑对象必须仍属于选中集合

例如：

1. 单选 A；
2. Ctrl 选择 B；
3. Ctrl 再点 B 取消；
4. 此时只剩 A 被选中。

要求：

- `selected-presentation` / 当前编辑行必须同步回实际仍选中的规则；
- 编辑器显示值必须与实际当前编辑规则一致；
- 后续保存不能把 B 的旧 editor 值写入 A。

如果集合为空，则当前编辑行设为 -1 并隐藏编辑卡。

## 4.3 批量修改后单项 editor 必须同步

例如：

1. 规则 A 当前 8 分钟；
2. 批量改为 10 分钟并确认；
3. 列表显示 10 分钟；
4. 用户直接点普通“保存”。

当前 editor 可能仍保留旧的 8 分钟，导致再次覆盖。

要求：

- batch confirm 后重新确定一个有效当前编辑行；
- 同步 `rule-duration` / `rule-mode` 等 editor 值；
- 此后普通保存不得撤销刚完成的 batch 修改。

## 4.4 非法批量时长要有明确反馈

当前无效时长可以保持“不保存”，但不要静默失败。使用现有风格给出简洁错误提示即可，不需要新通知系统。

## 回归验证

至少覆盖这些数据路径：

- 启用 + 禁用规则混合多选，只改时长，enabled 各自不变；
- A → Ctrl B → Ctrl 取消 B，编辑器回到 A；
- batch 8→10 分钟后再按普通保存，仍为 10 分钟；
- invalid duration 不落盘且用户能知道失败。

真实 Ctrl / Shift 鼠标体验如果当前环境无法可靠自动验证，源码/逻辑验证后明确留给用户手测；不要搭大型 GUI 测试框架。

---

# 5. 修复正式发布版本号来源

`Cargo.toml` 当前 package version 为 `1.13.0`，但 `scripts/build-release.ps1` 仍硬编码：

```powershell
$version = "1.6.0"
```

要求：

- 发布脚本从当前 Rust package version 获取唯一版本号；
- Portable 目录 / zip / installer 的版本号一致；
- 不维护第二份手工版本常量；
- 不改程序实际版本，当前仍以 Cargo.toml 为准；
- 不创建 Release 或 tag。

优先使用直接、可靠、Windows PowerShell 可执行的方法读取 Cargo package version，不为了这件事引入额外依赖。

---

# 6. 本轮界面边界

用户目前对软件排版和体验总体满意。

因此：

## 保留

- 当前 Rust + Slint；
- 当前六页 Settings 信息结构；
- 当前 DesktopTheme；
- 当前 Settings / PC Remote 的整体布局方向；
- 当前 UX13 已完成的说明弹窗、规则列表、批量设置布局；
- 当前用户已要求的 PC Remote“演示文稿”页定位：以规则管理为主，不擅自恢复此前删除的演示控制按钮。

## 只允许顺手修正与本轮 Bug 直接相关的视觉问题

例如：

- 无效输入反馈；
- 当前选中状态与编辑卡同步；
- 明显错误的列标题/重复标题，如果确认不影响基线文案要求，可做最小修正。

## 不做

- 不重新设计全套 UI；
- 不换主题；
- 不重排六页；
- 不改 Timer 配色方案；
- 不做新的 UX 大阶段；
- 不为了“代码更漂亮”重构已经稳定模块。

---

# 7. 测试策略

仍按风险分层，不恢复成“每改一点都跑全部门禁”。

开发过程中：

- F3：跑 Timer / command 相关最小测试；
- Settings config merge：跑配置与设置相关最小测试；
- Remote editor：跑对应逻辑测试；
- release script：做脚本版本解析最小验证。

本轮全部修复完成、准备交 ChatGPT 审核前，统一运行一次：

```powershell
cargo fmt --all -- --check
cargo clippy --all-targets --all-features -- -D warnings
cargo test
cargo build --release
```

如果新电脑缺失 Office / WPS / 双屏：

- 不把这些当成自动测试失败；
- 把对应真机项目标记为待用户复核。

如果 Inno Setup 已有，则可以验证发布脚本生成 installer；如果没有，不要求仅为本轮源码审核安装 Inno Setup，但必须确认版本解析部分正确，并在结果中注明 installer 未实机生成的环境原因。

---

# 8. 完成标准

完成后必须：

1. 更新 `docs/v1/CODEX_RESULT.md`，写清：
   - 新电脑环境预检结果；
   - 每个确认 Bug 的 root cause；
   - 实际修改；
   - 对应验证；
   - 哪些真实 Windows / Office / WPS / 双屏项目因环境暂缺仍待用户手测。
2. 不把“编译通过”等同于“全部体验通过”。
3. 提交所有本轮源码和文档修改。
4. push 到 `codex/v1-06-manual-test`。
5. 报告最终 commit SHA。
6. **推送完成后停止继续编码，等待 ChatGPT 下一轮审核。**
7. 不创建 Release / Tag。

---

# 持续约束

- V1 功能、选项、默认值、行为、中英文文字、Remote 协议、PowerPoint/WPS 行为继续以 v0.30.2 为产品基线；
- 不删除现有功能；
- 不新增产品功能；
- 不建立复杂测试框架；
- 不增加 SHA / checksum 发布体系；
- 不增加无必要 retry / fallback / backup 链；
- 不为了适配新电脑修改产品代码；
- 不升级锁定依赖，除非已有依赖在正确环境中确认无法构建且有明确必要性；
- 优先 root cause 和最小修复；
- 每个 Bug 修复不得通过删除现有测试或绕过验证实现。
