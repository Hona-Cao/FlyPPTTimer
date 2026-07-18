# v0.16.2 DPI 与窗口布局审计

- 应用 DPI 模式由项目属性 `ApplicationHighDpiMode=PerMonitorV2` 统一生成。
- 远控窗体使用 `AutoScaleMode.Dpi` 和 `AutoScaleDimensions=96×96`。
- Standard/Compact 只依据当前客户区的逻辑 DIP 大小判断，不依据分辨率或特定缩放百分比。
- `RemoteDashboardTheme.Scale` 仅保留在圆角、焦点框和文字绘制安全内缩等纯绘图路径。
- 窗口配置独立保存到 `RemoteControl.Window`，不复用悬浮计时窗 `Placement`。
- 纯计算测试覆盖 96、120、144、168、192 DPI、位置比例、屏幕回退、工作区限制和窗口状态。
