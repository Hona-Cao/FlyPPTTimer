# FlyPPTTimer v0.12.1 发布候选说明

v0.12.1 是稳定性和安全性修订，不新增业务功能，也不迁移 UI 平台。

## 重点修复

- 自动计时开关对本地和手机启动 PowerPoint 放映保持一致。
- 手机列表和计时按钮在 busy、断联及恢复后按实时状态正确启用。
- 远控 token、日志、响应头和断开设备会话逻辑加强。
- 配置原子保存、损坏恢复及安装升级不覆盖用户配置。
- 单实例、全局异常捕获、日志轮转和 PowerPoint STA 退出稳定性完善。
- 新增 xUnit 测试项目及 Windows GitHub Actions。

本版本仅推送功能分支供验收，不合并 `main`，不创建 GitHub Release。
