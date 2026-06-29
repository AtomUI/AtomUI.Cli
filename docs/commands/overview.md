# AtomUI Cli 子命令设计索引

`docs/commands/` 描述 AtomUI Cli 的公开命令面。每个子命令必须有独立目录，并通过所属模块的 CLI contribution catalog 显式注册；运行时不得通过程序集扫描或反射发现命令。

所有命令详细设计必须遵守 [命令设计标准](command-design-standard.md)。命令文档必须写到可以直接指导研发实现 options、handler、领域服务调用、输出 DTO、错误分支和测试矩阵的粒度。

## 命令分组

| 分组 | 命令 | 架构文档 | 详细设计 | 所属模块 | 阶段 | 默认写入 |
| --- | --- | --- | --- | --- | --- | --- |
| 核心入口 | `help` | [overview](help/overview.md) | [design](help/design.md) | `AtomUICliCoreModule` | P0 | 否 |
| 知识查询 | `list` | [overview](list/overview.md) | [design](list/design.md) | `AtomUICliMetadataModule` | P0 | 否 |
| 知识查询 | `info` | [overview](info/overview.md) | [design](info/design.md) | `AtomUICliMetadataModule` | P0 | 否 |
| 知识查询 | `doc` | [overview](doc/overview.md) | [design](doc/design.md) | `AtomUICliMetadataModule` | P0 | 否 |
| 知识查询 | `demo` | [overview](demo/overview.md) | [design](demo/design.md) | `AtomUICliMetadataModule` | P0 | 否 |
| 知识查询 | `token` | [overview](token/overview.md) | [design](token/design.md) | `AtomUICliMetadataModule` | P0 | 否 |
| 知识查询 | `semantic` | [overview](semantic/overview.md) | [design](semantic/design.md) | `AtomUICliMetadataModule` | P0 | 否 |
| 知识查询 | `design.md` | [overview](design/overview.md) | [design](design/design.md) | `AtomUICliMetadataModule` | P0 | 否 |
| 知识查询 | `package` | [overview](package/overview.md) | [design](package/design.md) | `AtomUICliMetadataModule` | P0 | 否 |
| 知识查询 | `changelog` | [overview](changelog/overview.md) | [design](changelog/design.md) | `AtomUICliMetadataModule` | P0 | 否 |
| 项目分析 | `env` | [overview](env/overview.md) | [design](env/design.md) | `AtomUICliProjectAnalysisModule` | P1 | 否 |
| 项目分析 | `doctor` | [overview](doctor/overview.md) | [design](doctor/design.md) | `AtomUICliProjectAnalysisModule` | P1 | 否 |
| 项目分析 | `usage` | [overview](usage/overview.md) | [design](usage/design.md) | `AtomUICliProjectAnalysisModule` | P1 | 否 |
| 项目分析 | `lint` | [overview](lint/overview.md) | [design](lint/design.md) | `AtomUICliProjectAnalysisModule` | P1 | 否 |
| 项目分析 | `migrate` | [overview](migrate/overview.md) | [design](migrate/design.md) | `AtomUICliProjectAnalysisModule` | P2 | 否 |
| 集成与写入 | `mcp` | [overview](mcp/overview.md) | [design](mcp/design.md) | `AtomUICliMcpModule` | P1 | 否 |
| 集成与写入 | `setup` | [overview](setup/overview.md) | [design](setup/design.md) | `AtomUICliSetupModule` | P1/P2 | 仅 `--write` |
| 集成与写入 | `init` | [overview](init/overview.md) | [design](init/design.md) | `AtomUICliSetupModule` | P2 | 仅 `--write` |
| 集成与写入 | `add` | [overview](add/overview.md) | [design](add/design.md) | `AtomUICliSetupModule` | P2 | 仅 `--write` |
| 集成与写入 | `upgrade` | [overview](upgrade/overview.md) | [design](upgrade/design.md) | `AtomUICliSetupModule` | P2 | 仅 `--write` |

## 全局选项

所有命令共享下列全局选项。命令文档只记录该命令额外使用或有特殊语义的选项。

| 选项 | 默认值 | 说明 |
| --- | --- | --- |
| `--format <text|json|markdown>` | `text` | 输出格式。Agent 和脚本场景应使用 `json`。 |
| `--target-version <version>` | metadata 默认版本 | 查询或诊断目标版本。 |
| `--product <id>` | 全产品 | 产品过滤，例如 `desktop`、`datagrid`、`charts`。 |
| `--lang <zh|en>` | `zh` | 输出语言。 |
| `--detail` | `false` | 输出更多字段。 |
| `--data-root <path>` | 内置数据根 | 追加数据根，用于内部或商业快照。 |
| `--no-update-check` | `false` | 禁止更新检查。 |

`--version` 固定表示 CLI 工具版本，不参与目标 AtomUI 版本解析。

## 命令实现模型

命令以强类型 descriptor 注册：

```csharp
context.Commands.Add<ListCommandOptions, ListCommandHandler>("list");
```

命令实现规则：

- 每个命令提供 `*CommandOptions` 和 `*CommandHandler`。
- `help`、`version` 属于 Core 内置命令；其中 `help` 必须有独立命令设计文档，因为它定义 CLI 的入口引导体验。
- Handler 实现 `IAtomUICliCommandHandler<TOptions>`。
- Handler 生命周期为 `Transient`，在每次命令执行的 DI scope 中创建。
- 命令不得接收根 `IServiceProvider`。
- 业务逻辑放在 metadata、project analysis、setup 或 MCP 服务中，handler 只做参数校验、调用服务、映射输出。
- stdout 只输出命令结果，stderr 只输出错误和诊断。
- JSON 输出必须使用 source generated `JsonSerializerContext`。
- 错误码、诊断码和退出码必须遵守 [error-code-standard](error-code-standard.md)，命令文档只能列本命令使用的已登记错误码子集。

详细设计：

| 文档 | 说明 |
| --- | --- |
| [runtime-design](runtime-design.md) | 命令 catalog、options、handler、输出、退出码和 AOT 约束。 |
| [error-code-standard](error-code-standard.md) | 统一错误码、诊断码、退出码、stderr/stdout 和命令文档规范。 |
| [knowledge-query-design](knowledge-query-design.md) | 知识查询类命令的 DTO、服务依赖和测试矩阵。 |
| [project-analysis-design](project-analysis-design.md) | 项目分析类命令的项目上下文、扫描和诊断设计。 |
| [integration-write-design](integration-write-design.md) | MCP、setup、init、add、upgrade 的集成和写入设计。 |

## 退出码约定

所有命令遵守 [AtomUI Cli 错误码标准](error-code-standard.md)。命令 handler 只返回 `AtomUICliResult`，退出码由 `IExitCodeMapper` 统一计算。

| 退出码 | 场景 |
| --- | --- |
| `0` | 成功。 |
| `1` | 未分类异常、模块失败、MCP 运行时失败、取消。 |
| `2` | 参数错误或命令不存在。 |
| `3` | 查询目标不存在或存在歧义。 |
| `4` | 数据不可用、schema 不兼容、项目文件或源码无法读取。 |
| `5` | 项目诊断、包冲突、AOT 或 lint finding 达到失败阈值。 |
| `6` | 写入计划冲突、配置读取失败或文件写入失败。 |

## AOT-first 约束

- 命令 catalog 必须显式注册或由 source generator 生成。
- 不允许通过 `Assembly.GetTypes()`、attribute scanning 或 handler 命名约定发现命令。
- 不允许在 Native AOT 包中动态加载外部命令程序集。
- 命令 options、输出 DTO、错误 DTO 和 MCP DTO 必须纳入 source generated JSON context。
- 命令解析不能依赖运行时动态代码生成。
