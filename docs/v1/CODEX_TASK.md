# 当前任务：RC 配置生命周期 / Remote 鉴权收口 + 工具链固定

当前分支：`codex/v1-06-manual-test`

审核基点：`8e8b9cd972b3aef3f32cda5ce903274bd678986d`

## 本轮结论

上一轮任务中的以下修复已通过源码审核，**不要重做**：

- F3 Start/Pause：Running→Pause、Paused→Resume、Stopped/Finished→Start；
- PC Remote 端口编辑不再被周期刷新覆盖；
- PC Remote 多选、批量编辑、单项 editor 的本轮一致性修复；
- Settings 一般对象字段的 baseline/draft 合并；
- `scripts/build-release.ps1` 从 Cargo package version 读取版本号，并传递给 Installer。

当前不再做 UI 大改，不重构 Timer、Office、DPI、Remote 协议或发布结构。

本轮只收口 ChatGPT 在 8e8b9cd 后复审确认的 3 个逻辑问题，并补一个开发环境可复现性改进。

---

# 1. 配置导入 / 恢复默认必须保持运行态与磁盘一致

## 已确认问题

当前 `src/settings.rs` 的 `config.import` 会调用 `native_import_config(c, config_path)`：

- 读取 JSON；
- `*c = imported`，这里只修改 Settings draft；
- 随后直接 `c.save(config_path)` 写入真实配置文件；
- 但共享 `applied`、Timer、Remote、Hotkey、Display 等运行态并没有同步应用。

因此存在明确不一致：**磁盘已经是导入配置，当前运行中的软件仍是旧配置**。如果之后用户取消设置，UI 草稿会恢复，但磁盘上的导入结果仍然存在。

同时，当前 `config.reset` 只把 draft 改成 `AppConfig::default()`，而产品基线 v0.30.2 的实现是：

- Settings 的 `ImportRequested` → Context `ImportConfig()` → `ApplyConfig(imported)`；
- `ResetRequested` → `ApplyConfig(new AppConfig())`。

也就是说，v0.30.2 的“配置导入 / 恢复默认”属于**立即应用的配置操作**，不是只改一个未应用草稿。

## 修复要求

以 v0.30.2 为产品行为基线，保持实现直接：

### 配置导入

导入成功后应作为一个完整操作：

1. 读取并解析配置；
2. 用当前已有的 normalize / validate 规则做必要校验；
3. 成功后更新共享 `applied`；
4. 通过现有 `on_applied` 路径同步 Timer / Remote / Desktop / Display 等运行态；
5. 持久化到真实配置文件；
6. Settings 的 draft / baseline 同步到最终已应用配置；
7. UI 刷新并回到 clean 状态。

任一步失败时，不允许留下“磁盘是新配置、运行态还是旧配置”的半完成状态。

### 恢复默认

按同样原则立即应用默认配置，而不是只改 draft 等待底部 Apply。

成功后：

- 运行态、磁盘、shared applied、Settings draft/baseline 必须一致；
- dirty 应清除；
- UI 立即显示最终默认值；
- 不新增第二套配置应用路径，尽量复用现有 apply/on_applied 逻辑。

## 必须验证

至少覆盖：

- 导入 A 配置后，运行态与磁盘同时成为 A；
- 导入后关闭 Settings，不会回到旧配置；
- 无效配置导入不会修改磁盘或共享配置；
- 恢复默认后运行态与磁盘一致；
- 导入 / Reset 后再次普通 Apply 不会把旧 baseline 写回来。

---

# 2. Remote 鉴权令牌永远不能处于可接受的空值状态

## 已确认问题

`AppConfig::default()` 的 Remote token 为空字符串。

应用启动入口会在第一次启动时补 token，但当前 `RemoteServer::start()` 和 `apply_enabled()` 都会直接把 `config.remote_control.token` 复制进服务。

同时 `fixed_time_token_equals("", "") == true`。因此如果运行中通过恢复默认、导入配置或未来其他路径把 token 变为空，而 Remote 仍启用，缺少 token 的请求可能被视为鉴权成功。

这是发布阻断问题。

## 修复要求

建立一个非常简单的中心不变量：

> Remote 服务只要处于 enabled / start / apply 状态，配置 token 与服务内部 token 都必须是非空随机 token。

建议最小实现：

- 在 `RemoteServer::start()` / `apply_enabled()` 的公共边界确保空 token 自动生成，并写回传入的 `AppConfig`；
- 再让服务内部 token 使用最终非空值；
- `fixed_time_token_equals` 对任一侧为空时直接返回 false，作为最后一道简单保护；
- 不增加账号、加密框架、session 系统或网络权限模型。

## 必须验证

至少覆盖：

- 使用 `RemoteControlSettings::default()`（token 为空）启动 Remote 后，config 中得到非空 token；
- Remote 内部使用同一个 token；
- 请求不带 token / 空 token 返回拒绝；
- 正确 token 可以正常访问；
- “恢复默认”后 Remote 仍启用时，不会出现空 token；
- 导入一个 token 为空的旧配置后，也必须得到新的有效 token 并持久化。

---

# 3. Settings 与 PC Remote 同时编辑不同文件规则时，不能整表覆盖

## 已确认问题

上一轮新增的 `merge_draft()` 对普通对象做字段递归合并，这部分可以保留。

但是 `rules` 是 JSON array。只要 baseline.rules != draft.rules，当前通用 merge 会把整个 `rules` array 用 draft 替换。

因此仍存在：

1. 打开 Settings；
2. Settings 修改规则 A，但不 Apply；
3. PC Remote 修改规则 B，或添加规则 C，并已经保存；
4. 回到 Settings Apply；
5. Settings 的旧 rules 数组可能覆盖 PC Remote 对 B/C 的新修改。

这与“不同入口保存后都不能互相丢数据”的目标不符。

## 修复要求

只对 `rules` 做一段小型、明确的 identity merge，不改变整个配置 merge 结构。

规则身份继续使用现有完整路径规范化逻辑（`presentation_identity` / 等价路径规则）。

推荐语义：

- baseline 中未被 Settings 修改的规则：采用最新 applied 版本；
- Settings 新增的规则：添加到最新 applied，避免同路径重复；
- Settings 删除的规则：从最新 applied 删除同身份规则；
- Settings 编辑已有规则：只覆盖 **baseline→draft 实际改变的字段**；
- 如果 PC Remote 同时修改了同一规则的其他字段，未被 Settings 改过的字段必须保留外部最新值；
- 如果双方同时修改同一字段，则本次用户主动点击 Settings Apply 的本地值优先；
- PC Remote 在 Settings 打开期间新增的其他规则必须保留。

FileRule 目前字段有限，不要为此建立通用数据库 merge / revision / conflict framework。

## 必须验证

至少覆盖：

- Settings 改 A.duration，Remote 改 B.duration → 两边都保留；
- Settings 改 A.duration，Remote 新增 C → A 修改与 C 都保留；
- Settings 改 A.mode，Remote 改 A.enabled → 两边字段都保留；
- 双方都改 A.duration → Settings Apply 的值优先；
- Settings 删除 A，同时 Remote 修改 B → A 删除、B 修改保留；
- 不产生重复 path 规则。

---

# 4. 固定项目 Rust 工具链，避免下一台电脑再次踩同一环境问题

上一轮新电脑已经证明：系统默认 Rust 1.86 无法构建锁定的 Slint 1.17.1，而 Rust 1.92.0 可以正常构建。

本轮请增加一个最小的 `rust-toolchain.toml`，让进入仓库后的普通 Cargo 命令自动使用项目要求的 Rust 版本。

建议：

```toml
[toolchain]
channel = "1.92.0"
profile = "minimal"
components = ["rustfmt", "clippy"]
targets = ["x86_64-pc-windows-msvc"]
```

要求：

- 不改变 Cargo 依赖版本；
- 不升级 Slint；
- 不修改用户系统默认 toolchain；
- 在仓库目录中普通 `cargo check --locked` 应自动使用项目 toolchain；
- 如果 rustup 首次需要下载该工具链，这是正常环境准备，不要绕过。

---

# 5. 本轮禁止扩大范围

不要修改：

- Timer UI / 配色 / 尺寸；
- Settings / PC Remote 整体视觉布局；
- DPI 处理；
- PowerPoint / WPS COM 实现；
- 手机 Web Remote 协议或页面；
- 已通过的 F3、端口刷新和 Remote selection/batch 逻辑，除非本轮测试直接证明必须做最小关联修复；
- release 版本号；
- Cargo 依赖版本。

不要增加复杂 CI、数据库、配置 revision 系统、通用同步框架或大规模 GUI 自动化。

---

# 6. 验证策略

开发过程中只跑本轮相关的最小测试。

本轮完成准备提交时统一运行一次：

```powershell
cargo fmt --all -- --check
cargo clippy --locked --all-targets --all-features -- -D warnings
cargo test --locked
cargo build --locked --release
```

由于上一轮发布脚本已经通过审核，本轮没有修改发布脚本时不必重复完整 Inno Setup 打包。

如条件允许，可启动 Release 做一次最小设置窗口冒烟检查，但不要声称这代替 PowerPoint/WPS、手机 Remote、混合 DPI 等用户真机验收。

---

# 7. 完成方式

完成后：

1. 在 `docs/v1/CODEX_RESULT.md` 顶部新增本轮结果；
2. 明确写出每个问题的根因、修改位置和验证；
3. 记录 `cargo test` 最终通过数量；
4. commit 所有本轮源码 / 文档修改；
5. push 到 `codex/v1-06-manual-test`；
6. 停止继续编码，等待 ChatGPT 下一轮审核；
7. 不创建 Release / Tag。
