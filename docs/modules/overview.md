# AtomUI Cli 模块设计索引

本文档目录描述 AtomUI Cli 的运行时模块。每个模块都必须通过 `AtomUI.Modularity` 声明模块身份、依赖和生命周期，通过 CLI 专属 contribution context 贡献命令、MCP tools、诊断规则或数据根能力。

## 模块清单

| 模块 | 架构文档 | 详细设计 | 所属项目 | 阶段 | 职责 |
| --- | --- | --- | --- | --- | --- |
| `AtomUICliCoreModule` | [core](core/overview.md) | [design](core/design.md) | `AtomUI.Cli.Hosting` | P0 | 应用基础设施、输出、错误、命令调度和通用服务。 |
| `AtomUICliMetadataModule` | [metadata](metadata/overview.md) | [design](metadata/design.md), [source extraction](metadata/source-extraction-design.md) | `AtomUI.Cli.Metadata` | P0 | 元数据快照、产品清单、控件查询、包查询。 |
| `AtomUICliProjectAnalysisModule` | [project-analysis](project-analysis/overview.md) | [design](project-analysis/design.md) | `AtomUI.Cli.ProjectAnalysis` | P1 | 项目读取、使用扫描、诊断规则、迁移分析。 |
| `AtomUICliMcpModule` | [mcp](mcp/overview.md) | [design](mcp/design.md) | `AtomUI.Cli.Mcp` | P1 | MCP stdio server 和只读 MCP tool 暴露。 |
| `AtomUICliSetupModule` | [setup](setup/overview.md) | [design](setup/design.md) | `AtomUI.Cli.Hosting` | P2 | setup/init/add/upgrade 写入类工作流。 |
| `AtomUICliCommercialDataModule` | [commercial-data](commercial-data/overview.md) | [design](commercial-data/design.md) | `AtomUI.Cli.Metadata` | P2 | 商业数据根、商业产品快照和授权诊断。 |

## 模块实现规则

- 模块类必须继承 `AtomUICliModule`，并使用 `[Module]` 声明。
- 模块依赖必须使用 `typeof(TModule)`，不得手写模块 ID 字符串。
- 服务注册走 `ModuleServiceConfigurationContext.Services`。
- 多贡献能力不得注册到 `ModuleServiceCollection`，必须走 CLI 专属 contribution context。
- 所有模块 catalog 必须由 `AtomUI.Foundation.Generator` 或显式强类型 registration 产生。
- Native AOT 包不得动态加载外部模块程序集。
- 模块失败必须转换为 [错误码标准](../commands/error-code-standard.md) 中登记的 `ATOMUICLI_MOD001` 或 `ATOMUICLI_MOD002`。

## CLI 专属阶段

```text
AtomUICliDataRootConfiguringPhase
AtomUICliCommandConfiguringPhase
AtomUICliMcpToolConfiguringPhase
AtomUICliDiagnosticRuleConfiguringPhase
```

这些阶段只能由 `AtomUICliApplication` 调用。模块不能从命令 handler 或业务服务里直接调用 `ModuleHost.RunModulePhaseAsync(...)`。
