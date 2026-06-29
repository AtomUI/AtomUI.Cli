# AtomUI.Cli

<div align="center">

[![NuGet](https://img.shields.io/nuget/v/AtomUI.Cli?style=flat-square&logo=nuget&label=NuGet)](https://www.nuget.org/packages/AtomUI.Cli)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AtomUI.Cli?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/AtomUI.Cli)
[![License](https://img.shields.io/badge/license-LGPL--3.0-white?labelColor=black&style=flat-square)](LICENSE)
[![.NET Tool](https://img.shields.io/badge/.NET%20Tool-AtomUI.Cli-512bd4?style=flat-square&logo=dotnet)](https://www.nuget.org/packages/AtomUI.Cli)

文档语言：[English](README.md) | [简体中文](README.zh-CN.md)

</div>

## 简介

AtomUI.Cli 是 AtomUI 生态的命令行工具入口。它以 NuGet 分发的 .NET Tool 形式交付，安装后可以在本地开发、CI、脚本和 Agent 工作流中通过 `dotnet atomui ...` 调用。

AtomUI.Cli 面向 AtomUI 项目的日常工程任务：查询控件和包元数据、生成组件使用说明、诊断项目环境、辅助迁移、初始化项目配置，以及为 Agent 提供 MCP 集成入口。它不是一个临时脚本集合，而是 AtomUI 生态的稳定命令运行时。

## 功能特性

- 通过 .NET Tool 安装和调用，适合本地终端、CI、脚本和自动化工作流。
- 提供控件、包、设计 Token、semantic parts、示例和变更记录等知识查询能力。
- 提供项目环境检查、诊断、使用扫描、lint 和迁移分析能力。
- 支持 setup、init、add、upgrade 等带显式写入边界的项目配置流程。
- 面向 Agent 和脚本提供稳定 JSON 输出、错误码、退出码和 stdout/stderr 行为。
- 基于 GenericHost、DI 容器和 AtomUI.Modularity 组织应用生命周期、模块依赖和命令贡献。
- 以 AOT-first 和 trimming-friendly 为约束，避免在运行时热路径中扫描程序集。

## 当前状态

仓库正在围绕 `1.0` 版本线开发。当前代码已经包含核心应用宿主、模块化运行时、命令预解析、按需模块激活、命令调度、输出抽象、错误码基础设施和首批命令实现。

## 快速开始

包发布到 NuGet 后，可以按全局 .NET Tool 安装：

```bash
dotnet tool install -g AtomUI.Cli
```

安装后查看版本和帮助：

```bash
dotnet atomui --version
dotnet atomui help
dotnet atomui help info
```

本地开发时可以直接运行入口项目：

```bash
dotnet run --project src/AtomUI.Cli/AtomUI.Cli.csproj -- help
```

## 命令能力

公开命令面设计见 [docs/commands](docs/commands/overview.md)。

| 分组 | 能力 |
| --- | --- |
| Knowledge | 查询控件列表、控件信息、文档、示例、Token、semantic parts、包信息和变更记录。 |
| Project analysis | 检查环境、诊断项目、扫描控件使用、执行 lint 和生成迁移分析。 |
| Setup | 初始化配置、添加包或产品、升级 CLI 或项目依赖。 |
| Integration | 输出帮助、打印版本、启动 MCP server。 |

所有命令必须遵守统一的错误码、退出码、输出和 JSON 序列化规则，详见 [错误码标准](docs/commands/error-code-standard.md)。

## 架构

AtomUI.Cli 由轻量入口项目和 hosted runtime 组成：

- `src/AtomUI.Cli`：.NET Tool 入口项目。
- `src/AtomUI.Cli.Abstractions`：命令、输出、诊断、结果和错误契约。
- `src/AtomUI.Cli.Hosting`：应用宿主、命令 catalog、解析器、调度器、输出 writer 和核心模块。
- `src/AtomUI.Cli.Modularity`：CLI 专属模块和命令贡献契约。
- `tests/AtomUI.Cli.Tests`：核心运行时行为测试。

完整架构见 [架构设计](docs/architecture/atomui-cli-architecture-design.md)，模块设计见 [模块设计](docs/modules/overview.md)。

## 开发

仓库通过 [global.json](global.json) 固定 .NET SDK。常用命令：

```bash
dotnet build AtomUICli.slnx
dotnet test AtomUICli.slnx
dotnet pack src/AtomUI.Cli/AtomUI.Cli.csproj --configuration Release
```

提交前建议执行：

```bash
git diff --check
```

## 许可

Copyright (c) 2018-2026 Qinware Technologies Co., Ltd. All rights reserved.

AtomUI.Cli 使用 GNU Lesser General Public License v3.0 only 许可，详见 [LICENSE](LICENSE)。
