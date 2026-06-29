# AtomUICliCoreModule 详细设计

## 1. 模块定位

`AtomUICliCoreModule` 提供 AtomUI Cli 的底层运行时能力：应用生命周期、模块编排、GenericHost 集成、DI 容器映射、命令 catalog、命令解析、命令 scope、输出、错误码和退出码。

Core 模块不包含 AtomUI 控件知识，不读取用户项目，不实现 MCP tool，不执行写入。

## 2. 模块注册

| 字段 | 值 |
| --- | --- |
| 模块类型 | `AtomUICliCoreModule` |
| 依赖 | 无 |
| 命令贡献 | `--help`、`--version` 内置入口 |
| 服务注册 | command runtime、output runtime、error runtime |
| 生命周期职责 | 驱动 `ModuleHost` 和 `IHost` 的顺序 |

## 3. Application 生命周期

`AtomUICliApplication` 是唯一生命周期编排器：

```text
CreateBuilder
  -> LoadCommandManifestCatalog
  -> PreParseCommandName
  -> PlanActiveModules
  -> ResolveActiveModules
  -> CreateActiveModules
  -> ConfigureActiveModuleServices
  -> FreezeActiveModuleServices
  -> ConfigureActiveCliContributions
  -> FreezeActiveCliCatalogs
  -> BuildGenericHost
  -> InitializeActiveModules
  -> StartHost
  -> DispatchCommand / RunMcpServer
  -> StopHost
  -> ShutdownActiveModules
  -> Dispose
```

失败规则：

- Host build 前失败：不调用 `IHost.StartAsync`。
- Host build 后失败：必须 `StopAsync` 并 `Dispose`。
- 模块已创建时必须调用 shutdown。
- 命令失败不能跳过 shutdown。
- Ctrl+C 通过 `CancellationToken` 传播到 handler。
- 未在 activation plan 中的模块不能创建实例、注册服务或执行生命周期钩子。

## 4. 命令预解析与模块激活

Core 模块必须在完整模块生命周期之前完成轻量命令预解析。预解析只识别命令名和必要的全局参数，不创建业务 options，也不访问用户项目。

```text
Raw args
  -> CommandPreParser
  -> CommandManifestCatalog
  -> ModuleActivationPlanner
  -> ModuleActivationPlan
```

| 对象 | 职责 |
| --- | --- |
| `CommandManifest` | 命令轻量描述，包含名称、所属模块、额外 required modules、分组、支持格式、读写属性、项目要求和帮助元数据。 |
| `CommandManifestCatalog` | 所有命令的轻量索引，来自显式代码或 source generator，不依赖模块实例。 |
| `CommandPreParser` | 解析 `--version`、`--help`、`--format`、`--markdown` 和命令名。 |
| `ModuleActivationPlanner` | 根据命令所属模块、命令级 required modules 和硬模块依赖计算本次运行的 active modules。 |
| `ModuleActivationPlan` | 冻结后的激活集合，作为 `ModuleHost` 的启用模块输入。 |

激活规则：

- Core 模块始终激活。
- 普通命令激活所属模块、命令声明的 required modules，以及这些模块的硬依赖闭包。
- `help` 默认只读取 manifest；`help <command>` 优先读取 manifest 输出详细帮助，只有需要命令私有 help provider 时才激活目标命令所属模块。
- `<command> --help` 必须在目标命令 handler 执行前转换为 `help <command>`。
- `mcp` 启动时只激活 Core 和 MCP，tool invocation 再按 tool 所属模块懒激活。
- 未知命令从 manifest 生成错误和建议，不触发全部模块加载。

## 5. DI 容器化

模块服务先进入 `ModuleServiceCollection`，冻结后通过 mapper 进入 GenericHost。

```text
ModuleServiceCollection
  -> ModuleServiceRegistry
  -> ModuleServiceDescriptorMapper
  -> IServiceCollection
  -> IHost
  -> root IServiceProvider
```

Core 模块负责：

- 注册 runtime singleton。
- 注册 command dispatcher。
- 注册 stdout/stderr writer。
- 注册 error code catalog。
- 注册 frozen catalogs。
- 为每次命令创建 scope。
- 按 activation plan 将 active module 服务映射到 GenericHost。

业务模块禁止保存 root provider。多贡献能力必须通过 catalog，不通过 `IEnumerable<T>` 枚举 DI 服务发现。

## 6. 命令 Descriptor

```csharp
public sealed record CliCommandDescriptor(
    string Name,
    CommandGroup Group,
    Type OptionsType,
    Type HandlerType,
    IReadOnlySet<OutputFormat> SupportedFormats,
    bool IsReadOnly,
    bool RequiresProject,
    bool RequiresWriteConfirmation,
    Func<GlobalCliOptions, IReadOnlyList<string>, IAtomUICliCommandOptions> OptionsFactory);
```

catalog 规则：

- 命令名 ordinal ignore case 唯一。
- handler 必须实现 `IAtomUICliCommandHandler<TOptions>`。
- options 必须实现 `IAtomUICliCommandOptions`。
- write 命令必须设置 `RequiresWriteConfirmation=true`。
- unsupported format 在 parser/dispatcher 边界返回参数错误。

## 7. 命令执行

```text
CommandPreParser.ParseCommandName
  -> ModuleActivationPlanner.Plan
  -> active module lifecycle
  -> CliCommandParser.Parse
  -> CliCommandDescriptorCatalog.Get
  -> descriptor.OptionsFactory
  -> Create command IServiceScope
  -> Bind CliInvocationContext
  -> Resolve handler
  -> ExecuteAsync
  -> OutputWriter / ErrorWriter
  -> ExitCodeMapper
```

`CliInvocationContext` 包含：

- raw args；
- command name；
- global options；
- command descriptor；
- cancellation token；
- trace id；
- working directory。

## 8. 输出设计

| 服务 | 职责 |
| --- | --- |
| `IOutputWriter` | stdout，只写成功结果。 |
| `IErrorWriter` | stderr，只写错误、诊断摘要和调试信息。 |
| `IJsonOutputSerializer` | source generated JSON 输出。 |
| `IMarkdownOutputRenderer` | markdown 输出。 |

规则：

- `--format json` 成功时 stdout 输出 JSON payload。
- `--format json` 失败时 stdout 保持为空，stderr 输出 JSON error envelope。
- text 输出面向人类，字段可以少于 JSON，但顺序必须稳定。
- markdown 只允许命令 descriptor 声明支持。

## 8.1 Help 输出设计

Core 模块负责 help 首页和 help 详情页。help 首页不能输出裸命令名列表，必须按照用户任务组织内容：

```text
AtomUI Cli
  -> Usage
  -> Common commands
  -> Command groups
  -> Global options
  -> More
```

help 详情页必须输出命令 summary、usage、arguments、options、examples、supported formats、requires project 和 write confirmation。详情数据首期来自 `CommandManifestCatalog`，保证 `help` 不激活全部业务模块。

help 首页必须从 CLI 产品元数据读取展示版本和版权信息，text、markdown 和 json 输出都必须包含这些字段。展示版本以包版本为准，不携带 Git 修订号后缀；版权主体统一为 `Qinware Technologies Co., Ltd.`。

`CommandManifest` 因此必须包含 `Summary`、`Usage`、`Arguments`、`Options`、`Examples` 和 `HelpPriority`。这些字段必须显式声明或由 source generator 生成，不允许通过扫描 handler、attribute 或 XML doc 注释生成。

## 9. 错误与退出码

`IExitCodeMapper` 是唯一退出码来源。

| 输入 | 输出 |
| --- | --- |
| success result | `0` |
| argument error | `2` |
| not found / ambiguous | `3` |
| data/project read error | `4` |
| diagnostic failure | `5` |
| write conflict/failure | `6` |
| unexpected/module/mcp/cancel | `1` |

错误码 catalog 必须显式注册，不通过反射扫描常量。

## 10. AOT-first 约束

- command catalog 由模块 contribution 显式构建。
- command manifest catalog 由显式代码或 source generator 生成，不通过模块实例发现。
- JSON serializer 使用 source generated context。
- options factory 静态实现，不依赖动态 binder。
- 不扫描程序集发现 handler。
- `ActivatorUtilities` 只能在 Hosting 边界解析已注册 handler。

## 11. 测试矩阵

| 测试 | 覆盖 |
| --- | --- |
| lifecycle order | 模块 resolve/create/configure/freeze/init/shutdown 顺序。 |
| module activation | 单命令只激活 Core、所属模块、命令级 required modules 和硬依赖模块。 |
| command manifest | help、unknown command 和 format 校验不需要加载全部模块。 |
| host stop guarantee | 命令失败和取消仍 stop/shutdown。 |
| command catalog | 去重、格式支持、handler/options 类型校验。 |
| command scope | 每次执行独立 scope。 |
| stdout/stderr | 成功和错误分离。 |
| exit code mapper | 所有错误域映射。 |
