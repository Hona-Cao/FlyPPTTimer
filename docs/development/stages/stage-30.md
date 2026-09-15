# v1.14.1 — 连接引导、紧凑浮窗与真实贴边

[中文图解](../../USER_GUIDE.zh-CN.md#v1141) · [English instructions](../../USER_GUIDE.en.md#v1141) · [开发索引](../README.md)

## 实际交付 / Actual delivery

v1.14.1 已在 `review/v1.14.1` 完成交付，没有创建公开 Release。程序源码：
[`6e8bd3aeaec7ae8c247e09b535157db7f490047a`](https://github.com/Hona-Cao/FlyPPTTimer/commit/6e8bd3aeaec7ae8c247e09b535157db7f490047a)。

[运行时验证 34990592629](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34990592629) 的格式、Clippy、102 个通过测试（3 个明确忽略）、构建、GUI捕获和启动检查通过；后续提交步骤因为生成文档的换行符失败。没有把整条失败工作流描述成全部成功。

[完成打包 34992459836](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34992459836) 修复文档换行、复用已验证程序，提交实际源码并生成便携版和安装版 ZIP。Actions 附件受保留期限制，提交和文档记录长期保留。

The runtime checks succeeded before a documentation-line-ending failure. The successful packaging run reused the validated executable; ignored tests are not counted as passed.

## 用户可见变化

手机在同一服务运行期间首次从一个已认证的非本机 IP 连接时，如果还未允许浏览文件，电脑打开“设置 → 远程控制”，突出显示授权开关并提示应用。已有草稿不被导航覆盖。权限仍是当前遥控令牌范围的共享开关，不是逐设备权限。

主时间与下方秒表/页数、窗口四周留白进一步缩小。自动尺寸计算同步收紧；手动宽高保留。全部点位改用实际窗口大小：0% 上中贴顶、下中贴底；使用完整显示器而非工作区边界。

## 本次图文更新

图文在独立的 `docs/v1.14.1-guide` 分支制作，基于 main `67f0491`。中文/英文教程增加“连接→勾选→应用→手机选文件”的逐步截图、是否保存的区别、无提示排查、紧凑尺寸与零偏移操作；新增10张生产界面渲染图，其中包含浅/深色的授权前与保存后两个状态及两张计时器图。

[截图工作流 34994278753](https://github.com/Hona-Cao/FlyPPTTimer/actions/runs/34994278753) 使用真实 v1.14.1 布局及回调。CJK字体只用于临时文档渲染程序，未进入仓库或分发包。此任务不改动 main 中的产品源代码或现有 Release。
