# 全局工程规范

## 源码工作区

- AtomUI 源码默认放在 `<AtomUICliRepoRoot>/.workspace/AtomUI`。
- `.workspace/` 必须加入 `.gitignore`。
- CLI 运行时不得自动 clone、pull 或扫描远端仓库。
- 构建期分析工具优先读取 `--source-root` 或 `AtomUISourceRoot`，缺省时回落到 `.workspace/AtomUI`。
- 普通构建默认不得访问网络；缺失源码时必须失败并给出明确提示。
- 自动供应 AtomUI 源码只能通过显式构建属性开启：`AtomUIAutoProvisionSource=true`。
- 自动供应只允许在 `.workspace/AtomUI` 不存在时执行 `git clone`；如同时配置 `AtomUISourceCommit`，可在新克隆的工作区上 checkout 到锁定 commit，但不得对已有工作区执行隐式 `git pull`、checkout 或覆盖。
- CI/CD 使用自动供应时必须锁定 `AtomUISourceCommit`；只配置 branch 或 tag 不能作为可复现 release 构建依据。
- 自动供应相关属性统一命名为 `AtomUIAutoProvisionSource`、`AtomUISourceRepository`、`AtomUISourceRef`、`AtomUISourceCommit` 和 `AtomUISourceRoot`。

## 文档与计划

- 架构、命令、模块和规范文档统一放在 `docs/` 下。
- `docs/superpowers/` 只放开发计划，不放架构文档。

## 命名

- 业务领域统一使用“控件”，不要写“组件”。
- 所有对 AtomUI 源码的引用都应使用工作区约定，不要写死旧的参考项目路径。
- `src/**/Output` 目录名禁止用于源码层实现；源码里的输出呈现实现统一命名为 `Presentation`，避免与仓库根 `output/` 产物目录冲突。
