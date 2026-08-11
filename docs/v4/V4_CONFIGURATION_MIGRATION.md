# FlyPPTTimer 4.0 配置迁移

## 兼容目标

4.0 直接读取 `v0.30.2` 的 `FlyPPTTimer.config.json`。升级不改变用户的计时时长、规则、提醒音、远程访问令牌、窗口位置、显示器选择、热键和语言。安装版与便携版仍使用各自程序目录中的配置、日志和 `alert-sounds/`；迁移不移动或删除这些文件。

## 读取与保存

`ConfigService` 先反序列化 typed 配置，再执行按 `SchemaVersion` 幂等的归一化。所有持久化模型均用 `JsonExtensionData` 保存未知属性，因此 4.0 保存已知字段时，根对象及 Timer、Behavior/Prompt、Appearance、Controls、Remote/Window、Placement 和每条 Rule 的未来字段都会原样往返。

保存仍采用同目录临时文件、反序列化复核、原子替换和最多五份时间戳备份。主配置损坏时先保留 `.bad.json`，再按新到旧尝试备份；所有备份无效才建立默认配置。

## v0.30.2 字段语义

| 范围 | 迁移规则 |
|---|---|
| `Timer` | 保留默认时长、正/倒计时、超时策略和到时动作。 |
| `Rules` | 保留完整路径、文件名、独立时长、模式、启用状态和未知扩展字段；规范化后仅去除空路径及同一路径重复项。 |
| `Behavior/Prompt*` | 保留启用、提前秒数、语音、声音副本路径和闪烁节奏；WPF 保存不改写已有中文提示文字。 |
| `Appearance/Placement` | 保留配色、颜色、尺寸、透明度、形状、屏幕、九宫格锚点和百分比微调。 |
| `Controls` | 兼容旧 F3/F4/F5 字段并同步到完整 `Hotkeys` 字典；缺失命令补默认值，冲突由 WPF 保存校验拒绝。 |
| `RemoteControl` | 保留启用状态、4080/自选端口、随机端口语义、窗口位置和现有 token；仅 token 为空时生成新值。 |
| `Language/Update` | 保留语言选择和启动检查更新。 |

## 自动验证

真实 fixture 位于 `tests/FlyPPTTimer.Tests/Fixtures/v0.30.2-config.json`。`ConfigSchemaTests.V0302ConfigurationPreservesUnknownFieldsAtEveryPersistedLevel` 加载、修改并再次保存该文件，然后逐层检查未知 JSON、规则、声音引用、显示器和测试 token 均未丢失。WPF 设置测试另行覆盖规则、提醒、全部热键、显示、远程和语言保存，以及改变全局时长时“同步/保留规则时长”两种选择。

## 升级与回退

升级前可复制程序目录作为备份。4.0 首次保存会更新应用版本和 Schema，但保留未知字段；若需回退，退出程序后恢复升级前的配置和 `alert-sounds/`。不要在两个版本同时运行时复制配置，以免覆盖较新的原子保存结果。
