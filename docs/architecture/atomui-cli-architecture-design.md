# AtomUI Cli 架构设计文档

## 1. 背景

AtomUI Cli 是面向 AtomUI 生态的 .NET Tool，通过 NuGet 分发。用户安装后以 `dotnet atomui ...` 形式调用，用来查询 AtomUI 控件知识、分析项目使用情况、辅助迁移、生成 Agent 可消费的结构化信息，并为 MCP/Skill 集成提供稳定入口。

AtomUI Cli 不是应用脚手架，也不是运行时库。它的核心职责是把 AtomUI 生态的包信息、控件 API、Token、文档、示例、变更记录和项目诊断能力整理为本地、稳定、可脚本化的命令行能力。

## 2. 设计目标

- 通过 NuGet Tool 分发，安装后支持 `dotnet atomui` 子命令调用。
- 提供离线知识查询能力：控件列表、API、文档、Demo、Token、semantic parts、设计文档、包信息和变更记录。
- 提供项目分析能力：包引用、版本兼容、注册入口、XAML 命名空间、控件使用、AOT/trimming 风险和迁移问题。
- 提供 Agent 友好的 JSON 输出、稳定错误码、stdout/stderr 分离和 MCP/Skill 集成。
- 支持 AtomUI 核心包、可选扩展包和商业控件包。
- 引入 GenericHost 作为应用级宿主，统一 DI、配置、日志、生命周期、命令 scope 和 MCP 长驻模式。
- 基于 `AtomUI.Modularity` 构建 CLI 生态能力模块层，复用 AtomUI 统一模块身份、依赖拓扑、生命周期、服务冻结和 source generator catalog。
- 坚持 AOT-first：默认设计必须兼容 Native AOT、trimming、RID-specific tool packaging 和无反射热路径。
- 使用显式数据快照，不依赖运行时反射扫描 AtomUI 程序集。
- 所有源码、工具、测试、文档和构建产物遵循统一工程边界。

## 3. 非目标

- 不替代 `dotnet new` 模板系统。
- 不在基础查询命令中访问网络。
- 不在运行时加载用户项目程序集。
- 不通过反射推断控件 API、Token 或 semantic parts。
- 不在未显式确认的情况下修改用户项目。
- 不把商业包内部 API 写入公开快照。
- 不通过运行时程序集扫描发现命令、MCP tools、诊断规则或产品扩展。
- 不把应用级 DI 容器暴露为业务层 service locator。
- 不把 `AtomUI.Modularity` 当成应用级 DI 容器；它只负责模块声明、依赖、生命周期和中立服务描述。
- 不在 Native AOT 工具中动态加载外部模块程序集。商业控件和扩展能力通过显式编译进包的模块 catalog 或数据快照接入。
- 不引入会触发 Native AOT 或 trimming 警告且无法消除的默认 Host provider。

## 4. 仓库结构

AtomUI Cli 仓库采用以下结构：

```text
AtomUICli/
├── AtomUI.Cli.slnx
├── global.json
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── build/
│   ├── Version.props
│   ├── Common.props
│   ├── PackageMetaInfo.props
│   └── Output.props
├── src/
│   ├── AtomUI.Cli/
│   ├── AtomUI.Cli.Abstractions/
│   ├── AtomUI.Cli.Modularity/
│   ├── AtomUI.Cli.Hosting/
│   ├── AtomUI.Cli.Metadata/
│   ├── AtomUI.Cli.ProjectAnalysis/
│   └── AtomUI.Cli.Mcp/
├── tools/
│   └── AtomUI.Cli.MetadataBuilder/
├── tests/
│   ├── AtomUI.Cli.Tests/
│   ├── AtomUI.Cli.Modularity.Tests/
│   ├── AtomUI.Cli.Hosting.Tests/
│   ├── AtomUI.Cli.Metadata.Tests/
│   ├── AtomUI.Cli.ProjectAnalysis.Tests/
│   ├── AtomUI.Cli.Mcp.Tests/
│   └── AtomUI.Cli.Packaging.Tests/
├── docs/
│   ├── architecture/
│   ├── engineering/
│   ├── modules/
│   ├── commands/
│   └── AI/
├── data/
├── resources/
└── output/
```

目录职责：

| 目录 | 职责 |
| --- | --- |
| `src/` | CLI 运行时代码、服务和适配器。 |
| `tools/` | 构建期工具，例如元数据生成、校验和压缩。 |
| `tests/` | 单元测试、快照测试、CLI 测试、pack smoke test。 |
| `docs/architecture/` | 架构设计文档。 |
| `docs/engineering/` | 工程规范、发布流程、验证规则。 |
| `docs/modules/` | 模块和数据契约文档。 |
| `docs/commands/` | 子命令设计文档，每个子命令一个独立目录。 |
| `docs/AI/` | Agent/MCP/Skill 相关文档。 |
| `data/` | 受版本控制的公开元数据源快照。 |
| `resources/` | NuGet icon、README 附件等静态资源。 |
| `output/` | 构建产物、临时生成物、NuGet 包和本地验证输出。 |

`docs/superpowers` 不承载架构文档。若未来保留该目录，只用于本地开发计划类材料。

## 5. Build System

### 5.1 SDK

根目录提供 `global.json`，固定 SDK：

```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestFeature"
  }
}
```

### 5.2 全局 MSBuild 文件

根 `Directory.Build.props` 保持轻量，只设置通用 C# 编译选项并导入 build props：

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <Import Project="$(MSBuildThisFileDirectory)/build/Version.props"/>
  <Import Project="$(MSBuildThisFileDirectory)/build/Common.props"/>
  <Import Project="$(MSBuildThisFileDirectory)/build/PackageMetaInfo.props"/>
  <Import Project="$(MSBuildThisFileDirectory)/build/Output.props"/>
</Project>
```

`Directory.Build.targets` 只放跨项目的必要修正，不放业务逻辑。

### 5.3 版本配置

`build/Version.props` 是版本单一来源：

```xml
<Project>
  <PropertyGroup>
    <AtomUIVersion>6.0.6</AtomUIVersion>
    <AtomUICliVersion>0.1.0</AtomUICliVersion>
    <AtomUICliDataSchemaVersion>1</AtomUICliDataSchemaVersion>
  </PropertyGroup>
</Project>
```

### 5.4 通用构建配置

`build/Common.props` 定义目标框架、配置和编译规则：

```xml
<Project>
  <PropertyGroup>
    <AtomUIDevelopTargetFramework>net10.0</AtomUIDevelopTargetFramework>
    <Configuration Condition="'$(Configuration)' == ''">Debug</Configuration>
    <AtomUICliToolTargetFramework>net10.0</AtomUICliToolTargetFramework>
    <AtomUITargetFrameworks>$(AtomUICliToolTargetFramework)</AtomUITargetFrameworks>
    <WarningsAsErrors>Nullable</WarningsAsErrors>
  </PropertyGroup>
</Project>
```

CLI 运行时项目、构建期工具和测试项目统一使用 `net10.0`。NuGet Tool 以 .NET SDK 10 的 RID-specific tool packaging 为主路径，优先发布 Native AOT 包；同时保留 `any` CoreCLR fallback 包，覆盖未提供 AOT 包的平台。

运行时代码必须满足：

- `AtomUI.Cli` 可执行项目在 Release AOT 包装时启用 `PublishAot`。
- 运行时库项目声明 AOT/trim 兼容，并在 CI 中启用 trim analyzer 和 Native AOT analyzer。
- AOT、trim、single-file 相关 warning 必须作为发布阻断项处理。
- 任何 dependency 触发无法消除的 AOT warning 时，必须替换依赖、隔离到非 AOT fallback，或降低功能范围。

### 5.5 包版本管理

启用 Central Package Management：

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
```

集中管理 `AtomUI.Base`、`AtomUI.Base.Generator`、GenericHost、DI、Options、Logging、命令行框架、JSON、MCP、Roslyn、测试框架、快照测试和压缩相关依赖版本。项目文件只声明 `PackageReference Include`，不写版本号。

首批模块化依赖：

```xml
<PackageVersion Include="AtomUI.Base" Version="1.0.0-alpha.1" />
<PackageVersion Include="AtomUI.Base.Generator" Version="1.0.0-alpha.1" />
```

`AtomUI.Base.Generator` 只作为 analyzer/source generator 引入：

```xml
<PackageReference Include="AtomUI.Base.Generator" PrivateAssets="all" />
```

### 5.6 输出路径

所有输出进入 `output/`：

```text
output/bin/<Configuration>
output/<ProjectName>/obj
output/Nuget/<Configuration>
output/data
```

`build/Output.props` 统一配置：

```xml
<Project>
  <PropertyGroup>
    <PackageOutputPath>$(MSBuildThisFileDirectory)../output/Nuget/$(Configuration)</PackageOutputPath>
    <OutputPathWithoutFramework>$(MSBuildThisFileDirectory)../output/bin/$(Configuration)</OutputPathWithoutFramework>
    <OutputPath>$(OutputPathWithoutFramework)</OutputPath>
    <BaseIntermediateOutputPath>$(MSBuildThisFileDirectory)../output/$(MSBuildProjectName)/obj</BaseIntermediateOutputPath>
    <IntermediateOutputPath>$(BaseIntermediateOutputPath)/$(Configuration)</IntermediateOutputPath>
  </PropertyGroup>
</Project>
```

## 6. `.gitignore` 策略

`.gitignore` 必须忽略本地 IDE 状态、构建产物、临时依赖、本地资料目录和开发计划产物：

```text
.vs/
.idea/
*.user
bin/
obj/
[Dd]ebug/
[Rr]elease/
output/
outputs/
.nuget/
artifacts/
BenchmarkDotNet.Artifacts/
node_modules/
.referenceprojects/
.superpowers/
docs/superpowers/plans
src/**/GeneratedFiles/
tools/**/bin/
tools/**/obj/
tests/**/bin/
tests/**/obj/
```

受版本控制的内容：

- `docs/architecture/**`
- `docs/engineering/**`
- `docs/modules/**`
- `docs/commands/**`
- `docs/AI/**`
- `data/**/*.json`
- `resources/**`
- `build/*.props`
- `build/*.targets`

不提交 `output/`、压缩临时产物、本地 tool 安装目录、IDE workspace 文件和 generated debug files。

## 7. 项目拆分

| 项目 | 路径 | 职责 |
| --- | --- | --- |
| `AtomUI.Cli` | `src/AtomUI.Cli` | .NET Tool 可执行入口、`Program.Main`、退出码返回。 |
| `AtomUI.Cli.Abstractions` | `src/AtomUI.Cli.Abstractions` | DTO、错误码、服务契约、schema 常量。 |
| `AtomUI.Cli.Modularity` | `src/AtomUI.Cli.Modularity` | CLI 模块基类、模块阶段、contribution context、模块诊断 adapter。 |
| `AtomUI.Cli.Hosting` | `src/AtomUI.Cli.Hosting` | `AtomUICliApplication`、GenericHost 构建、应用级 DI 注册、`AtomUI.Modularity` adapter、命令调度、scope 生命周期。 |
| `AtomUI.Cli.Metadata` | `src/AtomUI.Cli.Metadata` | 快照加载、版本解析、产品清单、名称解析。 |
| `AtomUI.Cli.ProjectAnalysis` | `src/AtomUI.Cli.ProjectAnalysis` | 用户项目读取、XAML/C# 扫描、诊断规则。 |
| `AtomUI.Cli.Mcp` | `src/AtomUI.Cli.Mcp` | MCP stdio server adapter。 |
| `AtomUI.Cli.MetadataBuilder` | `tools/AtomUI.Cli.MetadataBuilder` | 构建期元数据生成、校验和压缩。 |

依赖方向：

```text
AtomUI.Cli
  -> AtomUI.Cli.Hosting

AtomUI.Cli.Hosting
  -> AtomUI.Cli.Modularity
  -> AtomUI.Cli.Abstractions
  -> AtomUI.Cli.Metadata
  -> AtomUI.Cli.ProjectAnalysis
  -> AtomUI.Cli.Mcp

AtomUI.Cli.Modularity
  -> AtomUI.Cli.Abstractions
  -> AtomUI.Base

AtomUI.Cli.Metadata
  -> AtomUI.Cli.Abstractions
  -> AtomUI.Cli.Modularity

AtomUI.Cli.ProjectAnalysis
  -> AtomUI.Cli.Abstractions
  -> AtomUI.Cli.Modularity
  -> AtomUI.Cli.Metadata

AtomUI.Cli.Mcp
  -> AtomUI.Cli.Abstractions
  -> AtomUI.Cli.Modularity
  -> AtomUI.Cli.Metadata

AtomUI.Cli.MetadataBuilder
  -> AtomUI.Cli.Abstractions
```

命令行和 MCP 只做适配层，不直接实现业务逻辑。

`AtomUI.Cli.Hosting` 是唯一应用级 composition root。业务项目通过 `AtomUICliModule` 提供模块声明、服务注册和 contribution 注册，不能在业务逻辑中接收 `IServiceProvider`、动态解析任意服务或依赖 GenericHost 生命周期对象。

## 8. 命令入口

### 8.1 Tool 包

NuGet Tool 包配置：

```xml
<PackageId>AtomUI.Cli</PackageId>
<PackAsTool>true</PackAsTool>
<ToolCommandName>dotnet-atomui</ToolCommandName>
<Version>$(AtomUICliVersion)</Version>
```

安装：

```bash
dotnet tool install --global AtomUI.Cli
```

调用：

```bash
dotnet atomui list
dotnet atomui info Button
dotnet atomui doctor ./src/MyApp
```

### 8.2 全局选项

| 选项 | 说明 |
| --- | --- |
| `--format <text|json|markdown>` | 输出格式。Agent 场景使用 `json`。 |
| `--target-version <version>` | 目标 AtomUI 版本，例如 `6.0.6`。 |
| `--product <id>` | 产品过滤，例如 `desktop`、`datagrid`、`charts`。 |
| `--lang <zh|en>` | 输出语言，默认 `zh`。 |
| `--detail` | 输出详细信息。 |
| `--data-root <path>` | 额外数据根目录，用于内部或商业快照。 |
| `--no-update-check` | 禁止更新检查。 |

`--version` 保留给 CLI 工具版本，不用于表示目标 AtomUI 库版本。

### 8.3 `AtomUI.Modularity` 模块层

AtomUI Cli 使用 `AtomUI.Modularity` 作为生态能力模块层。模块层负责描述“当前工具包含哪些能力、这些能力依赖什么、按什么顺序注册服务、按什么顺序贡献命令和 MCP tools”。它不负责进程生命周期和应用级 DI；这些仍由 GenericHost 负责。

模块层分工：

| 层 | 职责 |
| --- | --- |
| `AtomUI.Modularity` | 模块身份、`ModuleDescriptor`、`ModuleRegistration`、required dependency、生命周期、服务冻结、diagnostics、AOT catalog。 |
| `AtomUI.Cli.Modularity` | `AtomUICliModule`、CLI 专属阶段 marker、命令/MCP/data contribution context、模块诊断到 CLI 错误码的 adapter。 |
| `AtomUI.Cli.Hosting` | 创建 `ModuleHost`、合并 generated catalogs、执行模块阶段、把中立服务描述映射到 `IServiceCollection`、冻结 CLI catalogs。 |
| Feature projects | 声明具体模块，并贡献服务、命令、MCP tools、诊断规则和数据根能力。 |

首批模块：

| 模块 | 所属项目 | 职责 |
| --- | --- | --- |
| `AtomUICliCoreModule` | `AtomUI.Cli.Hosting` | 输出、错误码、格式化、全局选项、基础命令调度。 |
| `AtomUICliMetadataModule` | `AtomUI.Cli.Metadata` | 快照加载、版本解析、产品清单、控件查询命令。 |
| `AtomUICliProjectAnalysisModule` | `AtomUI.Cli.ProjectAnalysis` | `env`、`doctor`、`usage`、`lint`、诊断规则。 |
| `AtomUICliMcpModule` | `AtomUI.Cli.Mcp` | MCP stdio server、MCP tool descriptors、tool invocation scope。 |
| `AtomUICliSetupModule` | `AtomUI.Cli.Hosting` | `setup`、`init`、`add`、写入命令的 dry-run/write 边界。 |
| `AtomUICliCommercialDataModule` | `AtomUI.Cli.Metadata` | 外部商业数据根、商业产品快照解析和授权错误提示。 |

模块示例：

```csharp
using AtomUI.Modularity;

[Module(Dependencies = [typeof(AtomUICliCoreModule)])]
public sealed partial class AtomUICliMetadataModule : AtomUICliModule
{
    public override void ConfigureServices(ModuleServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IMetadataSnapshotLoader, MetadataSnapshotLoader>();
        context.Services.AddSingleton<IControlQueryService, ControlQueryService>();
    }

    public override void ConfigureAtomUICliCommands(AtomUICliCommandContributionContext context)
    {
        context.Commands.Add<ListCommandOptions, ListCommandHandler>("list");
        context.Commands.Add<InfoCommandOptions, InfoCommandHandler>("info");
    }
}
```

`AtomUI.Base.Generator` 生成 CLI 模块 catalog：

```xml
<PropertyGroup>
  <AtomUIModularityCatalogNamespace>AtomUI.Cli.Generated</AtomUIModularityCatalogNamespace>
  <AtomUIModularityCatalogTypeName>AtomUICliModuleCatalog</AtomUIModularityCatalogTypeName>
</PropertyGroup>
```

```text
AtomUICliModuleCatalog.CreateRegistrations()
  -> IReadOnlyList<ModuleRegistration>
  -> ModuleHost
```

模块启用规则：

- 内置 CLI 模块默认全启用，依赖闭包由 `ModuleHost` 解析。
- 调试或裁剪场景可以通过强类型 `ModuleHostOptionsBuilder.Enable<TModule>()` 构建最小模块集合。
- 常规源码、测试输入和文档示例必须使用模块类型或泛型入口，不手写模块 ID 字符串。
- 商业控件支持默认通过数据快照接入，不动态加载商业模块程序集；如果未来商业能力需要代码模块，必须编译进 tool 包并合并 generated catalog。

### 8.4 `AtomUICliApplication`、GenericHost 与模块生命周期

AtomUI Cli 抽象出 `AtomUICliApplication` 作为应用编排器。它不是某个命令 handler，也不是 GenericHost 的替代品；它持有 `ModuleHost` 和 `IHost`，统一负责模块生命周期、GenericHost 生命周期、命令执行、MCP 长驻和最终释放。

职责：

| 类型 | 职责 |
| --- | --- |
| `Program.Main` | 只解析进程入口、创建 `AtomUICliApplication` 并返回退出码。 |
| `AtomUICliApplication` | 拥有 `ModuleHost`、`IHost`、冻结 catalogs 和运行状态，调用所有模块通用钩子和 CLI 专属钩子。 |
| `AtomUICliApplicationBuilder` | 合并 args、Host 配置、模块 registrations、模块启用选项和数据根选项。 |
| `ModuleHost` | 执行 `AtomUI.Modularity` 的通用模块生命周期。 |
| `IHost` | 执行 GenericHost 的应用级 DI、logging、lifetime、hosted service 生命周期。 |

`AtomUICliApplication` 对外暴露的核心形态：

```csharp
public sealed class AtomUICliApplication : IAsyncDisposable
{
    public ModuleHost? ModuleHost { get; private set; }

    public IHost? Host { get; private set; }

    public CliCommandDescriptorCatalog Commands { get; private set; } = CliCommandDescriptorCatalog.Empty;

    public McpToolDescriptorCatalog McpTools { get; private set; } = McpToolDescriptorCatalog.Empty;

    public static AtomUICliApplicationBuilder CreateBuilder(string[] args);

    public ValueTask<int> RunAsync(
        IReadOnlyList<string> args,
        CancellationToken cancellationToken = default);
}
```

`AtomUICliApplication` 状态机：

```text
Created
  -> ModulesResolving
  -> ModulesCreating
  -> ModuleServicesConfiguring
  -> ModuleContributionsConfiguring
  -> HostBuilding
  -> ModulesInitializing
  -> HostStarting
  -> RunningCommand / RunningMcpServer
  -> HostStopping
  -> ModulesShuttingDown
  -> Disposed
```

所有模块钩子必须由 `AtomUICliApplication` 统一触发，其他类型不能绕过 Application 直接调用 `ModuleHost.InitializeAsync()` 或 `RunModulePhaseAsync(...)`。

模块钩子顺序：

```text
ModuleHost.ResolveModules()
ModuleHost.CreateModules()
ModuleHost.PreConfigureServicesAsync()
ModuleHost.ConfigureServicesAsync()
ModuleHost.PostConfigureServicesAsync()
ModuleHost.FreezeServices()
ModuleHost.RunModulePhaseAsync(AtomUICliDataRootConfiguringPhase)
ModuleHost.RunModulePhaseAsync(AtomUICliCommandConfiguringPhase)
ModuleHost.RunModulePhaseAsync(AtomUICliMcpToolConfiguringPhase)
ModuleHost.RunModulePhaseAsync(AtomUICliDiagnosticRuleConfiguringPhase)
ModuleHost.InitializeModulesAsync()
...
ModuleHost.ShutdownAsync()
```

GenericHost 与模块生命周期的关系：

- `AtomUICliApplication` 在 `HostBuilding` 前完成模块解析、模块创建、模块服务注册、服务冻结和 CLI contribution catalog 冻结。
- `AtomUICliApplication` 把冻结后的 `ModuleServiceRegistry.Descriptors` 映射到 `HostApplicationBuilder.Services`。
- `IHost` build 完成后，`AtomUICliApplication` 调用 `ModuleHost.InitializeModulesAsync()`。
- `IHost.StartAsync()` 只在模块初始化成功后执行。
- 无论命令执行成功、失败、取消或 MCP server 退出，`AtomUICliApplication` 都必须先停止 GenericHost，再反向调用 `ModuleHost.ShutdownAsync()`。

AtomUI Cli 使用 GenericHost 作为应用级宿主。Host 负责一次进程启动内的配置、DI、日志、生命周期和取消信号；命令执行通过显式 command scope 隔离。`AtomUI.Modularity` 在 Host build 之前完成模块解析、服务描述收集和 contribution catalog 冻结。

启动链路：

```text
Program.Main
  -> AtomUICliApplication.CreateBuilder(args)
  -> AtomUICliApplicationBuilder
  -> Build AtomUICliApplication in Created state
  -> AtomUICliApplication.RunAsync(args)
```

`AtomUICliApplication.RunAsync` 内部链路：

```text
Created
  -> Load AtomUICliModuleCatalog.CreateRegistrations()
  -> Create ModuleHost
  -> ResolveModules / CreateModules
  -> PreConfigureServices / ConfigureServices / PostConfigureServices
  -> FreezeServices
  -> Run AtomUICliDataRootConfiguringPhase
  -> Run AtomUICliCommandConfiguringPhase
  -> Run AtomUICliMcpToolConfiguringPhase
  -> Run AtomUICliDiagnosticRuleConfiguringPhase
  -> Freeze data root / command / MCP / diagnostic catalogs
  -> HostApplicationBuilder
  -> Map ModuleServiceRegistry.Descriptors to IServiceCollection
  -> Register frozen catalogs into IServiceCollection
  -> Build IHost
  -> InitializeModules
  -> Start IHost
  -> Dispatch command or run MCP server
  -> Stop IHost
  -> ShutdownModules
  -> Disposed
```

`ModuleServiceRegistry.Descriptors` 到 `IServiceCollection` 的映射：

| `ModuleServiceLifetime` | `IServiceCollection` 映射 |
| --- | --- |
| `Singleton` + instance | `AddSingleton(serviceType, instance)` |
| `Singleton` + implementation type | `AddSingleton(serviceType, implementationType)` |
| `Scoped` + implementation type | `AddScoped(serviceType, implementationType)` |
| `Transient` + implementation type | `AddTransient(serviceType, implementationType)` |

`ModuleServiceCollection` 是中立服务描述集合，不作为多贡献列表使用。命令、MCP tool、诊断规则、数据根 provider 这类“一类能力多个贡献”的场景，必须走 CLI 专属 contribution context：

```text
AtomUICliCommandContributionContext
  -> AtomUICliCommandCatalogBuilder

AtomUICliMcpToolContributionContext
  -> AtomUICliMcpToolCatalogBuilder

AtomUICliDiagnosticRuleContributionContext
  -> AtomUICliDiagnosticRuleCatalogBuilder
```

命令链路：

```text
AtomUICliApplication
  -> CliCommandParser.Parse(args)
  -> CliCommandDescriptorCatalog 查找显式注册命令
  -> 创建 CliInvocationContext
  -> 创建 command IServiceScope
  -> 解析 IAtomUICliCommandHandler<TOptions>
  -> ExecuteAsync
  -> OutputWriter / ErrorWriter
  -> ExitCodeMapper
```

MCP 长驻链路：

```text
dotnet atomui mcp
  -> 同一个 GenericHost root provider
  -> McpStdioServer
  -> McpToolDescriptorCatalog 查找 tool
  -> 每次 tool invocation 创建独立 IServiceScope
  -> McpToolHandler 调用 Metadata / ProjectAnalysis 服务
  -> 返回 JSON-RPC response
```

DI 生命周期：

| 生命周期 | 服务类型 |
| --- | --- |
| Singleton | `ModuleHost`、命令 descriptor catalog、MCP tool catalog、产品 schema、不可变 metadata index、格式化器注册表、错误码映射、日志基础设施。 |
| Scoped | `CliInvocationContext`、全局选项、输出目标、诊断收集器、项目分析上下文、MCP tool invocation context。 |
| Transient | 命令 handler、需要短生命周期状态的扫描器、迁移步骤执行器。 |

规则：

- `IServiceProvider` 只能在 `AtomUI.Cli.Hosting` 的调度边界使用，业务层禁止把它当 service locator。
- 命令注册必须来自 CLI 模块 contribution catalog，禁止扫描程序集查找 handler。
- MCP tool 注册必须来自 CLI 模块 contribution catalog，禁止运行时反射发现 tool。
- Host defaults 必须最小化，只启用明确需要的 configuration、logging 和 lifetime provider。
- CLI 只读命令不能因 Host 初始化访问网络。
- `CancellationToken` 从 Host lifetime、Ctrl+C、命令 scope 和 MCP request context 统一传播。
- 模块初始化失败必须转换为 `ATOMUICLI_MOD001` 或 `ATOMUICLI_MOD002` 并阻止进入命令执行。

### 8.5 AOT-first 运行时约束

AOT-first 是运行时和发布的默认约束，不是发布阶段补丁。

允许：

- 显式泛型 DI 注册，例如 `AddSingleton<TService, TImplementation>()`。
- `AtomUI.Base.Generator` 生成模块 catalog。
- CLI 模块 contribution 生成或显式注册 command/tool descriptor。
- Source generator 生成 metadata catalog、command catalog、MCP tool catalog 和 JSON serialization context。
- `System.Text.Json` source generation。
- 强类型 options 和 configuration binding source generator。

禁止：

- `Assembly.GetTypes()`、attribute scanning、目录程序集扫描。
- `Activator.CreateInstance` 作为默认对象创建路径。
- 运行时拼接类型名、成员名或泛型类型来构造服务。
- `Expression.Compile()`、Reflection.Emit 或动态代码生成。
- 没有 AOT 注解和测试覆盖的 reflection fallback。
- 在热路径按字符串查找命令、控件成员、JSON type metadata 或 MCP tool。
- 在 Native AOT 包中动态加载外部模块程序集。

例外边界：

- 兼容性迁移、错误诊断或测试底层可以使用受控 reflection，但必须标注清晰边界，并有 AOT analyzer 覆盖。
- CoreCLR fallback 包不能成为绕过 AOT 设计的理由；fallback 只用于平台覆盖，不承载 AOT-incompatible 主实现。

## 9. 命令矩阵

### 9.1 知识查询

| 命令 | 说明 |
| --- | --- |
| `dotnet atomui list` | 列出控件、产品、分类、包和引入版本。 |
| `dotnet atomui info <Control>` | 查询控件 API、属性、事件、方法、默认值、命名空间和注册要求。 |
| `dotnet atomui doc <Control>` | 输出完整 Markdown 文档。 |
| `dotnet atomui demo <Control> [name]` | 列出或输出示例代码。 |
| `dotnet atomui token [Control]` | 查询全局 Token 或控件 Token。 |
| `dotnet atomui semantic <Control>` | 查询 semantic parts、模板结构摘要和可定制节点。 |
| `dotnet atomui design.md` | 输出 AtomUI 设计语言文档。 |
| `dotnet atomui package [PackageId]` | 查询 NuGet 包、依赖、注册方式、替代关系和冲突关系。 |
| `dotnet atomui changelog [range] [Control]` | 查询版本变更或控件变更。 |

### 9.2 项目分析

| 命令 | 说明 |
| --- | --- |
| `dotnet atomui env [path]` | 收集 SDK、TargetFramework、OS、Avalonia、AtomUI 和包版本信息。 |
| `dotnet atomui doctor [path]` | 诊断依赖、包冲突、注册缺失、版本兼容和 AOT 风险。 |
| `dotnet atomui usage [path]` | 扫描 XAML/C# 中的 AtomUI 控件使用统计。 |
| `dotnet atomui lint [path]` | 检查废弃 API、错误包组合、XAML 命名空间和最佳实践。 |
| `dotnet atomui migrate <from> <to>` | 输出迁移清单、破坏性变更和 Agent 迁移提示。 |

### 9.3 集成与写入

| 命令 | 说明 |
| --- | --- |
| `dotnet atomui mcp` | 启动 stdio MCP server。 |
| `dotnet atomui setup` | 为 Codex、Claude、Cursor、VS Code 写入 MCP/Skill 配置。 |
| `dotnet atomui init` | 检查并提示项目初始化步骤，P2 再支持写入。 |
| `dotnet atomui add <package>` | 包装 AtomUI 生态包安装，P2 实现。 |
| `dotnet atomui upgrade` | 输出 CLI 或 AtomUI 包升级建议。 |

写入命令默认 dry-run 或明确要求 `--write`。

## 10. 元数据模型

### 10.1 快照

```json
{
  "schemaVersion": 1,
  "version": "6.0.6",
  "majorVersion": "v6",
  "products": [],
  "controls": [],
  "packages": [],
  "globalTokens": [],
  "changelog": [],
  "migrationGuides": []
}
```

规则：

- 开发仓库保留 `.json`，便于 diff。
- 发布包使用 `.json.gz`，降低包体积。
- Loader 同时支持 plain JSON 和 gzip JSON。
- `versions.json` 是版本解析源，所有索引项必须指向存在的快照。
- schema 变更必须提升 `schemaVersion`。

### 10.2 产品清单

产品清单定义包边界、注册入口、替代关系、冲突关系和可见性。

```json
{
  "id": "desktop",
  "displayName": "AtomUI Desktop Controls",
  "visibility": "public",
  "packageIds": ["AtomUI.Desktop.Controls"],
  "platforms": ["desktop"],
  "requires": ["AtomUI.Core", "Avalonia"],
  "registrationMethods": ["UseDesktopControls"],
  "conflicts": []
}
```

可选包示例：

```json
{
  "id": "datagrid",
  "displayName": "AtomUI Desktop DataGrid",
  "visibility": "public",
  "packageIds": ["AtomUI.Desktop.Controls.DataGrid"],
  "requires": ["AtomUI.Desktop.Controls"],
  "registrationMethods": ["UseDesktopDataGrid"]
}
```

商业包使用同一模型，不新增命令。若商业数据未配置，CLI 只能输出包级安装和注册建议，不能伪造控件 API。

### 10.3 控件模型

```json
{
  "name": "Button",
  "nameZh": "按钮",
  "productId": "desktop",
  "category": "general",
  "packageId": "AtomUI.Desktop.Controls",
  "namespace": "AtomUI.Desktop.Controls",
  "xamlNamespace": "https://atomui.net",
  "since": "6.0.0",
  "descriptionZh": "用于触发一个操作。",
  "registration": {
    "required": true,
    "methods": ["UseDesktopControls"]
  },
  "api": {
    "properties": [],
    "events": [],
    "methods": []
  },
  "demos": [],
  "tokens": [],
  "semanticParts": []
}
```

API 成员覆盖：

- CLR property
- StyledProperty
- DirectProperty
- AttachedProperty
- RoutedEvent
- ICommand 相关属性
- TemplatePart
- semantic part
- 控件 Token
- 注册方法

## 11. 元数据生成管线

元数据构建器以显式配置为入口：

```bash
dotnet run --project tools/AtomUI.Cli.MetadataBuilder/AtomUI.Cli.MetadataBuilder.csproj -- generate --config docs/AI/atomui-cli-metadata.config.json
dotnet run --project tools/AtomUI.Cli.MetadataBuilder/AtomUI.Cli.MetadataBuilder.csproj -- verify --config docs/AI/atomui-cli-metadata.config.json
dotnet run --project tools/AtomUI.Cli.MetadataBuilder/AtomUI.Cli.MetadataBuilder.csproj -- compress --config docs/AI/atomui-cli-metadata.config.json
```

输入类型：

- 控件源文档
- Agent 文档输出
- 示例源码和示例元数据
- 控件源码索引
- 变更记录
- NuGet README
- 版本 props
- 产品清单

输出：

```text
data/versions.json
data/products.json
data/v6.json
data/v6.0.6.json
output/data/v6.json.gz
output/data/v6.0.6.json.gz
```

生成器职责：

- 合并产品、包、控件、API、Token、示例、文档和变更记录。
- 校验 schema、可见性、链接、示例引用和版本索引。
- 保证公开快照不包含内部或商业私有内容。
- 保证每个 `versions.json` 索引项都有对应快照。

生成器不做：

- 不联网查询包源。
- 不运行用户项目。
- 不从运行时程序集反射提取 API。
- 不根据控件名称发明 semantic parts。

## 12. 项目分析

### 12.1 输入范围

项目分析默认扫描当前目录，也支持显式传入 `.sln`、`.slnx`、`.csproj` 或目录。

扫描文件：

- `.sln`、`.slnx`
- `.csproj`
- `Directory.Build.props`
- `Directory.Packages.props`
- `packages.lock.json`
- `.axaml`、`.xaml`
- `.cs`
- publish profile 和 app manifest，仅用于发布/AOT 诊断

跳过目录：

- `bin/`
- `obj/`
- `output/`
- `outputs/`
- `.git/`
- `.idea/`
- `.vs/`
- `.referenceprojects/`
- `GeneratedFiles/`

### 12.2 诊断规则

| 规则 | 说明 |
| --- | --- |
| `PackageCompatibilityRule` | 检查 Avalonia、AtomUI、TargetFramework 版本兼容。 |
| `PackageConflictRule` | 检查互斥包、替代包和重复包。 |
| `RegistrationRule` | 检查引用包后是否调用对应注册方法。 |
| `XamlNamespaceRule` | 检查 XAML 命名空间声明和控件使用。 |
| `AotReadinessRule` | 检查 NativeAOT、trimming、动态访问和显式注册风险。 |
| `DeprecatedApiRule` | 检查废弃控件、属性和注册入口。 |
| `GeneratedFileRule` | 提醒不要手工修改 generated files。 |

### 12.3 输出

```json
{
  "project": "/repo/App/App.csproj",
  "summary": {
    "errors": 1,
    "warnings": 2,
    "infos": 3
  },
  "diagnostics": [
    {
      "code": "ATOMUICLI_PKG003",
      "severity": "error",
      "message": "The project references packages that are declared as conflicting in the AtomUI product catalog.",
      "file": "App.csproj",
      "suggestion": "Keep one package according to the product catalog replacement rule."
    }
  ]
}
```

## 13. MCP 与 Agent 集成

MCP tools：

- `atomui_list`
- `atomui_info`
- `atomui_doc`
- `atomui_demo`
- `atomui_token`
- `atomui_design_md`
- `atomui_semantic`
- `atomui_package`
- `atomui_changelog`
- `atomui_doctor`

MCP tools 默认只读。写入类能力不暴露为默认 MCP tool。

Skill 模板要求 Agent：

- 写 AtomUI 控件代码前调用 `dotnet atomui info`。
- 写复杂示例前调用 `dotnet atomui demo`。
- 做主题或样式前调用 `dotnet atomui token` 和 `dotnet atomui semantic`。
- 处理项目问题前调用 `dotnet atomui doctor` 和 `dotnet atomui env`。
- 做包变更前调用 `dotnet atomui package` 检查依赖和冲突。

## 14. 错误码与退出码

错误码、诊断码、退出码、stdout/stderr 边界和 JSON 错误 envelope 统一遵守 [AtomUI Cli 错误码标准](../commands/error-code-standard.md)。`AtomUICliApplication` 和命令 handler 不直接返回进程退出码，退出码只由 `IExitCodeMapper` 根据 `AtomUICliResult`、错误码和诊断 severity 聚合计算。

错误码格式：

```text
ATOMUICLI_<DOMAIN><NNN>
```

已登记 domain：

| 前缀 | 范围 |
| --- | --- |
| `ATOMUICLI_SYS` | 未分类异常、取消、运行时兜底错误。 |
| `ATOMUICLI_ARG` | 参数和命令错误。 |
| `ATOMUICLI_MOD` | 模块解析、模块生命周期、模块 contribution 错误。 |
| `ATOMUICLI_DATA` | 元数据加载、schema、数据根错误。 |
| `ATOMUICLI_CTRL` | 控件查询错误。 |
| `ATOMUICLI_PKG` | 包查询、包兼容和包冲突错误。 |
| `ATOMUICLI_PRJ` | 项目读取和扫描错误。 |
| `ATOMUICLI_AOT` | AOT、trim、single-file 相关诊断。 |
| `ATOMUICLI_MCP` | MCP transport 或 tool 错误。 |
| `ATOMUICLI_SETUP` | setup 写入错误。 |

退出码摘要：

| 退出码 | 说明 |
| --- | --- |
| `0` | 成功。 |
| `1` | 未分类异常、模块失败、MCP 运行时失败、取消。 |
| `2` | 参数错误或命令不存在。 |
| `3` | 查询目标不存在或存在歧义。 |
| `4` | 数据不可用、schema 不兼容、项目文件或源码无法读取。 |
| `5` | 项目诊断、包冲突、AOT 或 lint finding 达到失败阈值。 |
| `6` | 写入计划冲突、配置读取失败或文件写入失败。 |

## 15. 打包与发布

AtomUI Cli 使用 .NET SDK 10 的 RID-specific tool packaging。发布目标是同一个 NuGet Tool 包名下提供平台优化包：

- top-level pointer package：`AtomUI.Cli.<version>.nupkg`。
- Native AOT RID 包：`AtomUI.Cli.osx-arm64.<version>.nupkg`、`AtomUI.Cli.linux-x64.<version>.nupkg`、`AtomUI.Cli.linux-arm64.<version>.nupkg`、`AtomUI.Cli.win-x64.<version>.nupkg`。
- CoreCLR fallback 包：`AtomUI.Cli.any.<version>.nupkg`。

工具项目配置：

```xml
<PropertyGroup>
  <PackAsTool>true</PackAsTool>
  <PublishAot>true</PublishAot>
  <ToolCommandName>dotnet-atomui</ToolCommandName>
  <ToolPackageRuntimeIdentifiers>osx-arm64;linux-x64;linux-arm64;win-x64;any</ToolPackageRuntimeIdentifiers>
</PropertyGroup>
```

AOT 包装规则：

- Native AOT RID 包必须在目标 OS 相同的平台构建；Linux 包通过 Linux runner 或 .NET SDK AOT container 构建。
- `any` fallback 包使用 `-p:PublishAot=false` 构建，保持跨平台可安装。
- 所有 RID 包、`any` fallback 包和 pointer package 必须使用完全相同版本号。
- 发布顺序是 RID 包和 `any` fallback 先发布，pointer package 最后发布。
- CLI 安装方式对用户保持不变，平台选择由 .NET CLI 完成。

Release 流程：

```text
dotnet test
dotnet run --project tools/AtomUI.Cli.MetadataBuilder/AtomUI.Cli.MetadataBuilder.csproj -- generate --config docs/AI/atomui-cli-metadata.config.json
dotnet run --project tools/AtomUI.Cli.MetadataBuilder/AtomUI.Cli.MetadataBuilder.csproj -- verify --config docs/AI/atomui-cli-metadata.config.json
dotnet run --project tools/AtomUI.Cli.MetadataBuilder/AtomUI.Cli.MetadataBuilder.csproj -- compress --config docs/AI/atomui-cli-metadata.config.json
dotnet pack --configuration Release
dotnet pack --configuration Release -r osx-arm64
dotnet pack --configuration Release -r linux-x64
dotnet pack --configuration Release -r linux-arm64
dotnet pack --configuration Release -r win-x64
dotnet pack --configuration Release -r any -p:PublishAot=false
install local platform package as tool
dotnet atomui list --format json
dotnet atomui info Button --format json
dotnet atomui doctor <fixture> --format json
publish to NuGet
```

本地开发机只要求完成当前 OS RID 的 AOT pack smoke；CI 发布流水线必须覆盖全部声明 RID。

NuGet Tool 包必须包含：

- CLI 可执行入口。
- 编译进包的 generated module catalog。
- 压缩后的公开快照。
- `versions.json`。
- `products.json`。
- Skill 模板。
- MCP prompt/tool metadata。
- README、LICENSE、icon。
- Native AOT 包中不得包含非必要 runtime 配置、未压缩临时数据和 generated debug files。

## 16. 测试策略

### 16.1 单元测试

- HostApplicationBuilder service registration
- AtomUICliApplication state machine
- AtomUICliApplication drives every module lifecycle hook in order
- AtomUI.Modularity generated module catalog
- ModuleHost bootstrap
- Module service descriptor to IServiceCollection adapter
- CLI command contribution phase
- MCP tool contribution phase
- Module failure to ATOMUICLI_MOD error mapping
- DI scope lifecycle
- Command descriptor catalog
- Command dispatcher
- VersionResolver
- MetadataSnapshotLoader
- ProductCatalog
- FuzzyNameResolver
- JsonOutputWriter
- ErrorWriter
- PackageReferenceReader
- XamlUsageScanner
- DiagnosticRules

Host/DI 测试要求：

- root provider 可以 `ValidateOnBuild`。
- `Program.Main` 不直接调用 `ModuleHost`，只通过 `AtomUICliApplication` 运行。
- `AtomUICliApplication` 按固定顺序调用 Resolve/Create/PreConfigure/Configure/PostConfigure/Freeze/CLI contribution/Initialize/Shutdown。
- 命令失败、参数错误、取消和 MCP server 退出都必须触发 `IHost.StopAsync()` 和 `ModuleHost.ShutdownAsync()`。
- `AtomUICliModuleCatalog.CreateRegistrations()` 能创建所有内置模块 registration。
- `ModuleHost` 能解析 required dependency closure 并按拓扑顺序执行服务配置。
- CLI 专属 contribution phase 能冻结 command、MCP tool 和 diagnostic rule catalog。
- 所有公开命令 handler 均可通过 DI 解析。
- 每次命令执行都会创建并释放 command scope。
- MCP 每次 tool invocation 都创建独立 scope。
- 只读命令的 Host 初始化不会访问网络。
- 业务服务不接收 `IServiceProvider`。
- 模块生命周期失败会返回 `ATOMUICLI_MOD001` 或 `ATOMUICLI_MOD002`。

### 16.2 快照测试

- `list`
- `info`
- `doc`
- `demo`
- `token`
- `semantic`
- `package`
- `changelog`
- 错误输出

### 16.3 Pack smoke test

- `dotnet pack --configuration Release`
- 当前 OS RID 的 `dotnet pack --configuration Release -r <RID>`。
- `dotnet pack --configuration Release -r any -p:PublishAot=false`。
- 从 `output/Nuget/Release` 安装本地 tool。
- 验证 `dotnet atomui --version`。
- 验证 `dotnet atomui list --format json`。
- 验证 `dotnet atomui info Button --format json`。
- 验证包中不存在未压缩的临时输出目录。
- 验证 Native AOT 包运行时输出 compilation mode 或诊断 metadata，证明当前 RID 使用 AOT binary。

### 16.4 AOT 与 trim 验证

- Native AOT publish / pack 不能产生未处理的 AOT warning。
- trim analyzer warning 必须失败。
- `AtomUI.Base.Generator` 输出的 module catalog 必须参与 AOT smoke。
- command catalog、MCP tool catalog 和 JSON metadata 必须来自模块 contribution、显式注册或 source generator。
- 禁止新增未标注的 runtime reflection fallback。
- AOT smoke fixture 覆盖 `list`、`info`、`package`、`doctor --format json` 和 `mcp` 初始化。

### 16.5 MetadataBuilder verify

- 源文档缺失时失败。
- Agent 文档输出过期时失败。
- 示例引用失效时失败。
- 产品清单冲突时失败。
- 公开快照包含非公开内容时失败。
- `versions.json` 指向不存在快照时失败。

## 17. 分阶段路线

### P0：工程骨架和离线查询

- 建立 build system。
- 创建 `global.json`、`Directory.Build.props`、`Directory.Packages.props`、`build/*.props`。
- 创建 `src/`、`tools/`、`tests/`、`docs/`、`data/`。
- 引入 `AtomUI.Base` / `AtomUI.Base.Generator`。
- 实现 `AtomUI.Cli.Modularity`、`AtomUICliModule`、CLI 专属 contribution phase。
- 实现 `AtomUICliApplication`、应用状态机、模块钩子统一调用、GenericHost 入口、模块启动链路、应用级 DI、命令 descriptor catalog、全局选项和命令 scope。
- 实现元数据加载和基础查询命令。
- 生成首批公开快照。
- 完成当前 OS RID 的 Native AOT pack smoke。

### P1：项目分析和 MCP

- 实现 `env`、`doctor`、`usage`、`lint`。
- 实现项目文件、XAML、C# 注册扫描。
- 实现 MCP 只读 tools，并通过 `AtomUICliMcpModule` contribution catalog 注册。
- 实现 Skill 模板和 `setup --dry-run`。
- 引入 JSON source generation 和 configuration binding source generation。

### P2：商业数据和迁移

- 支持 `--data-root` 和数据根登记。
- 接入商业产品快照。
- 实现 `migrate`。
- 实现 `setup --write`、`init --write` 和 `add`。

### P3：发布自动化

- MetadataBuilder verify 进入 CI。
- 本地 tool 安装 smoke test 进入 CI。
- 多 OS / 多 RID Native AOT pack 进入 CI。
- `any` CoreCLR fallback pack 进入 CI。
- NuGet 发布 gate。
- 数据 schema 兼容测试。
- MCP tool contract 测试。

## 18. 验收标准

P0 验收：

- `dotnet tool install --global AtomUI.Cli` 后可运行 `dotnet atomui list`。
- `dotnet atomui info Button --format json` 输出稳定 JSON。
- 查询不存在控件时输出结构化错误和拼写建议。
- 不联网也能查询内置公开快照。
- CLI 入口通过 `AtomUICliApplication` 启动，`Program.Main` 不直接管理模块或 DI。
- `AtomUICliApplication` 同时持有 `ModuleHost` 和 `IHost`，并统一调用所有模块通用钩子和 CLI 专属钩子。
- 内置模块由 `AtomUI.Modularity` generated catalog 注册，`ModuleHost` 完成依赖解析和服务配置。
- 所有命令 handler 通过模块 contribution catalog 注册，并通过应用级 DI 解析。
- 当前 OS RID 可以完成 Native AOT pack smoke，且无未处理 AOT/trim warning。
- 所有构建产物进入 `output/`。
- 架构文档位于 `docs/architecture/`。

P1 验收：

- `doctor` 能识别包冲突、注册缺失和 XAML 命名空间问题。
- `usage` 能统计 XAML/C# 中的 AtomUI 控件使用。
- `mcp` 能暴露首批只读 tools，并通过模块 contribution catalog 和 per-invocation DI scope 处理请求。
- `setup` 支持 dry-run/check。

P2 验收：

- 外部数据根可加载商业产品快照。
- 商业数据不可用时返回结构化错误和配置建议。
- `migrate` 能输出版本迁移清单。
