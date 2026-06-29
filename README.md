# AtomUI.Cli

AtomUI.Cli 是 AtomUI 生态的命令行工具入口。它以 .NET Tool 形式通过 NuGet 分发，安装后可以在本地开发、CI、脚本和 Agent 工作流中通过 `dotnet atomui ...` 调用。

AtomUI.Cli 聚焦于稳定、可脚本化的 AtomUI 项目能力：元数据查询、项目诊断、初始化与配置自动化、迁移辅助，以及面向 Agent 的 MCP 集成入口。

## 当前状态

仓库正在围绕 `1.0` 版本线开发。当前代码已经包含核心应用宿主、命令调度、输出抽象、错误码基础设施和首批内置命令。

## 设计目标

- 作为 NuGet 分发的 .NET Tool 服务 AtomUI 项目开发流程。
- 基于 `GenericHost` 管理应用级宿主、依赖注入、配置、日志和生命周期。
- 基于 `AtomUI.Modularity` 组织 CLI 模块、模块依赖和模块生命周期钩子。
- 坚持 AOT-first 和 trimming-friendly，避免在运行时热路径中扫描程序集。
- 提供 Agent 友好的 JSON 输出、稳定错误码，以及清晰的 stdout/stderr 行为。
- 通过显式模块和数据快照支持 AtomUI 核心包、扩展包和商业控件包。

## 安装

包发布到 NuGet 后，可按全局 .NET Tool 安装：

```bash
dotnet tool install -g AtomUI.Cli
```

安装后执行：

```bash
dotnet atomui --help
dotnet atomui --version
```

本地开发时可直接运行入口项目：

```bash
dotnet run --project src/AtomUI.Cli -- --help
```

## 命令范围

公开命令面设计见 [docs/commands](docs/commands/overview.md)：

- 知识查询：控件列表、API 元数据、文档、示例、Token、semantic parts、包信息和变更记录。
- 项目分析：环境检查、诊断、使用扫描、lint 和迁移分析。
- 集成与写入工作流：MCP server 入口，以及带显式写入确认的 setup、init、add、upgrade 流程。

所有命令必须遵守统一的错误码、退出码、输出和 JSON 序列化规则，详见 [docs/commands/error-code-standard.md](docs/commands/error-code-standard.md)。

## 架构

AtomUI.Cli 由轻量入口项目和 hosted runtime 组成：

- `src/AtomUI.Cli`：.NET Tool 入口项目。
- `src/AtomUI.Cli.Abstractions`：命令、输出、诊断、结果和错误契约。
- `src/AtomUI.Cli.Hosting`：应用宿主、命令 catalog、解析器、调度器、输出 writer 和核心模块。
- `src/AtomUI.Cli.Modularity`：CLI 专属模块和命令贡献契约。
- `tests/AtomUI.Cli.Tests`：当前核心运行时行为测试。

完整架构见 [docs/architecture/atomui-cli-architecture-design.md](docs/architecture/atomui-cli-architecture-design.md)，模块设计见 [docs/modules](docs/modules/overview.md)。

## 开发

仓库通过 [global.json](global.json) 固定 .NET SDK。构建和测试命令：

```bash
dotnet build AtomUICli.slnx
dotnet test AtomUICli.slnx
```

提交前建议同时执行：

```bash
git diff --check
```

## 许可

Copyright (c) 2018-2026 Qinware Technologies Co., Ltd. All rights reserved.

AtomUI.Cli 使用 GNU Lesser General Public License v3.0 only 许可，详见 [LICENSE](LICENSE)。
