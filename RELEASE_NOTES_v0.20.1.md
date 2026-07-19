# FlyPPTTimer v0.20.1

本版专门优化下载和安装体积，不改变 v0.20.0 的功能与配置行为。

## 体积优化

- 自包含程序开启 .NET 单文件压缩，继续保持“无需预装 .NET、下载后即可运行”。
- 安装包改用 Inno Setup 6 与 LZMA2/Ultra64 固实压缩。
- 删除旧安装器中重复携带的第二套 .NET Runtime，显著降低安装包大小。
- 便携版和安装版仍包含相同的 FlyPPTTimer 功能。
- 安装包由 v0.20.0 的约 218.80 MiB 降至约 63.62 MiB，减少约 71%。

## 安装与升级

- 安装版支持直接覆盖旧版本。
- 已有 `FlyPPTTimer.config.json` 不会被默认配置覆盖；升级前仍会创建时间戳备份。
- 安装目录、开始菜单和桌面快捷方式保持不变。

## 下载文件

- `FlyPPTTimer-v0.20.1-setup-win-x64.exe`
- `FlyPPTTimer-v0.20.1-setup-win-x64.exe.sha256`
- `FlyPPTTimer-v0.20.1-portable-win-x64.zip`
- `FlyPPTTimer-v0.20.1-portable-win-x64.zip.sha256`
