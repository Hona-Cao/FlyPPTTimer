# v0.16.0 审计说明

## 基线

`734aeb121a1cb540e7d8179d2bd949c3a96a1f4c`

## 修改范围

- `RemoteControlForm.cs`
- `RemoteDashboardTheme.cs`
- 新增 `RemotePresentationRow.cs`
- 远控窗口契约测试
- 版本、CI 和发布材料

## 设计约束

- 纯文字 UI
- 单行按钮
- 简短说明
- 仅列表滚动
- 不修改业务服务

## 自动化

构建和测试完成后记录真实结果，不得伪造。

## 人工验收

- 100%、125%、150%、200% DPI
- 默认、最小、最大化窗口
- 空列表、1 条、4 条、20 条规则
- 中文长文件名与长路径
- 真实 PowerPoint/WPS
- 手机访问
- 双屏与演讲者视图
