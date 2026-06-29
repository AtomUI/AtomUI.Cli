# AtomUI Cli 命令设计标准

本文档定义所有 `docs/commands/<command>/design.md` 必须达到的设计粒度。命令设计文档不是命令简介，必须能直接指导研发创建 options、handler、领域服务调用、输出 DTO、错误分支和测试。

## 必备章节

每个命令设计文档必须包含以下章节：

1. 命令定位
2. 所属模块与注册
3. 调用语法
4. 参数与选项
5. Options 类型
6. Handler 依赖
7. 执行流程
8. 领域服务契约
9. 输出模型
10. 输出格式
11. 错误与退出码
12. AOT-first 约束
13. 测试矩阵
14. 实现文件建议

## 所属模块与注册

每个命令必须声明：

| 字段 | 要求 |
| --- | --- |
| 所属模块 | 必须是 `AtomUICliCoreModule`、`AtomUICliMetadataModule`、`AtomUICliProjectAnalysisModule`、`AtomUICliMcpModule` 或 `AtomUICliSetupModule`。 |
| 注册阶段 | 必须通过 `ConfigureAtomUICliCommands` 贡献到 command catalog。 |
| 命令分组 | 必须是 knowledge、project-analysis、integration 或 write。 |
| 读写属性 | 必须明确 `isReadOnly`、`requiresProject`、`requiresWriteConfirmation`。 |
| handler 生命周期 | 默认 `Transient`，每次命令执行在 command scope 内解析。 |
| 输出格式 | 必须声明支持 `text`、`json`、`markdown` 的哪几种。 |

命令不得通过程序集扫描、命名约定或 DI 枚举发现。

Core 内置命令也必须遵守本标准。`help` 还必须声明首页、详情页、命令建议和 manifest 帮助元数据；`version` 可以只保留轻量设计，除非后续扩展版本诊断能力。

## 参数与选项

每个选项必须写清楚：

| 字段 | 说明 |
| --- | --- |
| 名称 | CLI 参数名，例如 `--include`。 |
| 类型 | string、bool、enum、path、version、string list 等。 |
| 默认值 | 未传入时的确定行为。 |
| 合法值 | 枚举值、格式约束或路径约束。 |
| 校验失败 | 对应错误码和退出码。 |
| 与全局选项交互 | 与 `--format`、`--target-version`、`--product`、`--data-root`、`--detail` 的关系。 |

位置参数也必须用同样方式描述。

## Handler 依赖

Handler 章节必须区分运行时基础设施和领域服务：

| 类型 | 示例 |
| --- | --- |
| 运行时基础设施 | `IOutputWriter`、`IErrorWriter`、`IExitCodeMapper`、`CliInvocationContext`。 |
| 领域服务 | `IControlQueryService`、`IProjectDiagnosticEngine`、`IWritePlanBuilder`。 |
| 辅助服务 | `ITargetVersionResolver`、`IProductFilterResolver`、`IDataRootResolver`。 |

Handler 不允许接收 root `IServiceProvider`，不允许自己创建 scope，不允许直接扫描项目或写文件。Handler 只负责参数校验、调用领域服务、构造 payload 和返回 `AtomUICliResult`。

## 领域服务契约

每个命令必须写出它依赖的领域服务方法。服务结果不能只是 `object` 或异常，必须是明确 result union。

推荐结果分支：

| 分支 | 使用场景 |
| --- | --- |
| `Found` / `Success` | 查询或操作成功。 |
| `NotFound` | 查询目标不存在，必须带 suggestions。 |
| `Ambiguous` | 输入可匹配多个目标，必须带 candidates。 |
| `DataUnavailable` | 快照、schema、数据根或授权不可用。 |
| `InvalidProject` | 项目路径、文件或源码不可读取。 |
| `Conflict` | 写入计划或包关系存在冲突。 |

命令文档必须说明每个分支如何映射成 payload 或错误。

## 输出模型

JSON payload 必须是稳定 DTO，并包含：

- `schemaVersion`
- `command`
- `targetVersion`，适用时必填
- `productFilter`，适用时必填
- 命令自身 payload
- `diagnostics` 或 `warnings`，适用时必填
- `suggestions`，存在 not found 或 conflict 时必填

字段顺序、null 策略和列表排序必须固定。DTO 必须纳入 source generated JSON context。

## 错误与退出码

命令错误矩阵必须包含命令特有错误码、触发条件和退出码：

| 字段 | 说明 |
| --- | --- |
| 错误码 | 必须来自 [error-code-standard](error-code-standard.md)。 |
| 触发条件 | 精确说明哪一步触发。 |
| 退出码 | 由 `IExitCodeMapper` 统一计算。 |

stderr/stdout 边界、text 错误摘要和 `--format json` error envelope 由 [error-code-standard](error-code-standard.md) 统一定义。命令只有存在特殊错误载荷、诊断 finding 聚合或 MCP JSON-RPC 映射时，才在本命令设计中补充说明。

命令文档只能引用已登记错误码。新增错误码必须先更新错误码标准。

## 测试矩阵

每个命令设计文档的测试矩阵必须覆盖命令特有路径：

- 成功 text 输出。
- 成功 JSON 输出。
- 缺少必填参数。
- 非法选项值。
- 查询目标不存在或项目路径不存在。

写入命令还必须覆盖 dry-run、`--write`、conflict 和不可写文件。

下列通用契约通过共享测试覆盖，命令设计文档无需逐个重复，但命令实现必须纳入对应测试集：

| 测试 | 覆盖点 |
| --- | --- |
| `CommandCatalogTests` | 命令 descriptor 注册、重复命令检测和模块 contribution 顺序。 |
| `CommandErrorContractTests` | 命令声明的错误码都存在于 catalog，退出码来自 `IExitCodeMapper`。 |
| `CommandJsonOutputTests` | `--format json` 成功 payload 和 error envelope 使用 source generated context。 |
| `CommandCancellationTests` | cancellation token 传播到 handler 和领域服务。 |
| `CommandAotCompatibilityTests` | options、payload、error DTO 和 command descriptor 不依赖运行时反射发现。 |
