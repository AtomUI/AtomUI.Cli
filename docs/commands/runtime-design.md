# Command Runtime 详细设计

## 目标

AtomUI Cli 命令运行时负责把 `dotnet atomui ...` 调用转换为强类型 options、命令 scope、handler 调用、结构化结果和退出码。命令运行时不包含具体业务查询逻辑，业务逻辑由 metadata、project analysis、MCP 和 setup 模块提供。

## 显式注册

命令运行时分为两层注册：

| 层级 | 作用 | 是否需要模块实例 |
| --- | --- | --- |
| `CommandManifestCatalog` | 启动早期识别命令、所属模块、额外 required modules、分组、格式、读写属性、项目要求和帮助摘要。 | 否 |
| `CliCommandDescriptorCatalog` | active modules 生命周期完成后，提供 options factory、handler 类型和执行元数据。 | 是，仅 active modules |

所有公开命令都必须先进入 manifest，再由所属模块贡献真实 descriptor。manifest 来自显式代码或 source generator，不能通过程序集扫描生成。

模块贡献 descriptor 示例：

```csharp
context.Commands.Add<InfoCommandOptions, InfoCommandHandler>("info");
```

descriptor 必须包含：

| 字段 | 说明 |
| --- | --- |
| `Name` | 命令名，例如 `info`。 |
| `Aliases` | 可选别名，首期为空。 |
| `OptionsType` | 强类型 options。 |
| `HandlerType` | 强类型 handler。 |
| `Group` | `knowledge`、`analysis`、`integration`、`write`。 |
| `SupportedFormats` | 支持的输出格式。 |
| `IsReadOnly` | 是否只读。 |
| `RequiresProject` | 是否需要项目路径。 |
| `RequiresWriteConfirmation` | 是否需要 `--write`。 |

manifest 必须额外包含 help 首页和详情页所需的轻量字段：

| 字段 | 说明 |
| --- | --- |
| `Summary` | 一句话说明命令用途，首页和详情页都使用。 |
| `Usage` | 可复制的调用语法，例如 `dotnet atomui info <control> [options]`。 |
| `Arguments` | 位置参数摘要，不包含运行时业务查询。 |
| `Options` | 命令私有选项摘要，全局选项由 Core 统一注入。 |
| `Examples` | 2 到 4 条高价值示例。 |
| `HelpPriority` | 首页常用命令和分组排序使用，避免纯字母排序。 |

禁止通过程序集扫描、attribute scanning 或命名约定发现命令。

## 命令预解析

完整 options parser 不能在模块激活前运行，因为 handler、options factory 和领域服务都属于模块贡献。启动早期只允许做轻量预解析：

```text
Raw args
  -> CommandPreParser
  -> command name + global output format + help/version shortcut
  -> CommandManifestCatalog lookup
  -> ModuleActivationPlanner
```

预解析规则：

- `--version` 和 `-v` 映射到 `version`。
- 空参数、`--help` 和 `-h` 映射到 `help`。
- `help <command>` 不激活全部模块，优先读取 manifest 输出命令详情；只有后续引入命令私有 help provider 时才激活目标命令所属模块。
- `<command> --help` 在预解析阶段转换为 `help <command>`，不能执行目标命令 handler。
- `--format` 可以在预解析阶段校验目标命令是否支持该格式。
- 未知命令从 manifest 返回参数错误和 suggestions，不进入模块生命周期。
- 预解析不绑定命令私有选项，不读取文件系统，不访问数据根。

## 按需模块激活

预解析完成后，`ModuleActivationPlanner` 计算 active modules：

```text
active modules = Core + command owner module + command required modules + hard dependency closure
```

示例：

| 命令 | active modules |
| --- | --- |
| `version` | Core |
| `info Button` | Core、Metadata |
| `doctor ./app` | Core、Project Analysis、Metadata |
| `setup` | Core、Setup |
| `add datagrid` | Core、Setup、Metadata |
| `mcp` | Core、MCP |

只有 active modules 可以执行 `ConfigureServices`、贡献真实 command descriptor、进入 `Initialize` 和 `Shutdown`。未激活模块不能创建实例，不能注册服务，不能执行生命周期钩子。

## Help 输出契约

`help` 是命令运行时的一部分，不是业务模块命令。它必须提供首页和详情页两层输出。

首页 `dotnet atomui help` 必须包含：

- `AtomUI Cli` 标题。
- CLI 展示版本，取包版本，不包含 Git 修订号后缀。
- 版权信息，统一使用 `Qinware Technologies Co., Ltd.`。
- `Usage`。
- `Common commands`，优先展示 `list`、`info`、`doc`、`demo`、`doctor`、`setup`。
- `Command groups`，按 Knowledge、Project analysis、Setup、Integration 分组。
- `Global options`。
- `More`，提示 `dotnet atomui help <command>` 和 `dotnet atomui <command> --help`。

详情页 `dotnet atomui help <command>` 必须包含：

- 命令 summary。
- Usage。
- Arguments。
- Options。
- Examples。
- supported formats。
- 是否需要项目上下文。
- 是否需要写入确认。

help 禁止输出只有命令名的裸列表。每个命令至少必须显示 summary。`--format json` 时必须输出结构化 payload，而不是把 text help 包成字符串；首页 payload 必须包含 `version` 和 `copyright` 字段。

## 全局选项绑定

```csharp
public sealed record GlobalCliOptions(
    OutputFormat Format,
    string? TargetVersion,
    string? Product,
    string Language,
    bool Detail,
    string? DataRoot,
    bool NoUpdateCheck);
```

绑定规则：

- `--format` 只接受 `text`、`json`、`markdown`。
- `--target-version` 是 AtomUI 目标版本。
- `--version` 保留给 CLI 工具版本。
- `--lang` 首期支持 `zh`、`en`。
- `--data-root` 必须是存在的目录；不存在时由 metadata 模块返回数据错误。

## Options 设计

命令 options 分两层：

```csharp
public interface IAtomUICliCommandOptions
{
    GlobalCliOptions Global { get; }
}
```

示例：

```csharp
public sealed record InfoCommandOptions(
    GlobalCliOptions Global,
    string Control,
    IReadOnlyList<InfoSection> Include,
    bool Strict) : IAtomUICliCommandOptions;
```

options parser 只做语法级校验。业务级校验由 handler 或 service 完成。

## Result 设计

所有 handler 返回 `AtomUICliResult`：

```csharp
public sealed record AtomUICliResult(
    bool IsSuccess,
    object? Payload,
    AtomUICliError? Error,
    IReadOnlyList<AtomUICliDiagnostic> Diagnostics);
```

payload 必须是已登记到 JSON source generation context 的 DTO。

错误码和诊断码遵守 [AtomUI Cli 错误码标准](error-code-standard.md)。`AtomUICliError` 用于命令无法继续执行的硬错误；`AtomUICliDiagnostic` 用于 `doctor`、`lint`、`usage` 等命令的 finding。两者共用同一错误码 catalog，但退出码计算不同：硬错误按具体 code 映射，诊断 payload 按 severity 聚合。

`AtomUICliError` 最小字段：

```csharp
public sealed record AtomUICliError(
    string Code,
    AtomUICliSeverity Severity,
    string Message,
    string? Suggestion,
    string Stage,
    CliLocation? Location,
    IReadOnlyDictionary<string, string> Details);
```

handler 不创建未登记错误码，也不直接设置进程退出码。

## Handler 生命周期

```text
CommandPreParser
  -> ModuleActivationPlanner
  -> Active ModuleHost lifecycle
  -> CliCommandDispatcher
  -> create IServiceScope
  -> bind GlobalCliOptions and command options
  -> resolve handler
  -> ExecuteAsync
  -> write result
  -> map exit code
  -> dispose scope
```

handler 规则：

- `Transient` 生命周期。
- 不持有跨命令状态。
- 不接收根 `IServiceProvider`。
- 不直接访问文件系统，除非该命令本身是项目分析或写入命令。
- 不调用其他命令 handler 作为业务复用；共享逻辑放 service。

## MCP 按需调用

`mcp` 命令启动时只激活 Core 和 MCP 模块。MCP server 的 tool manifest 可以来自 `CommandManifestCatalog` 或独立 `McpToolManifestCatalog`。`tools/list` 不激活所有领域模块。

`tools/call` 必须根据 tool 所属模块创建一次 invocation activation plan：

```text
MCP tools/call
  -> tool manifest lookup
  -> owner module activation
  -> invocation scope
  -> resolve tool handler / domain service
  -> write JSON-RPC response
```

这样可以避免 MCP 长驻进程启动时加载 metadata、project analysis、setup 和商业数据模块的全部资源。

## 输出格式

| 格式 | 用途 | 要求 |
| --- | --- | --- |
| `text` | 人类终端阅读 | 简短、稳定、无颜色依赖。 |
| `json` | Agent 和脚本 | 字段稳定，失败时返回结构化错误。 |
| `markdown` | 文档和上下文注入 | heading 顺序稳定，code fence 语言明确。 |

JSON 输出需要包含：

- `schemaVersion`。
- `command`。
- `targetVersion`。
- `dataSource`。
- 命令 payload。
- warnings。

硬错误输出规则：

- stdout 保持为空。
- stderr 输出错误摘要。
- `--format json` 时，stderr 输出错误 envelope，字段以 [错误码标准](error-code-standard.md) 为准。

诊断命令输出规则：

- 命令成功完成扫描时，stdout 输出 payload 和 diagnostics/findings。
- 存在 error 级 finding 或 `--fail-on-warning` 命中时，退出码返回 `5`。
- 扫描前置条件失败，例如项目文件无法读取，返回 `AtomUICliError` 并按错误码映射退出码。

## Update Check

更新检查是 Host 级可选 post action：

- 默认只在非 MCP、非测试、非 `--no-update-check` 场景运行。
- 查询失败不能影响主命令退出码。
- 首期可以不实现网络检查，只保留 `IUpdateCheckService` 接口和 no-op 实现。
- Native AOT 包不得因更新检查引入不可裁剪依赖。

## 测试要求

| 测试 | 覆盖点 |
| --- | --- |
| `CommandCatalogTests` | 重名命令、格式能力、只读标记。 |
| `CommandParserTests` | 全局选项、子命令参数、非法格式。 |
| `CommandDispatcherTests` | scope、handler 生命周期、取消传播。 |
| `CommandPreParserTests` | `--version`、`help`、未知命令、format 预校验。 |
| `ModuleActivationTests` | 单命令只激活 Core、所属模块、命令级 required modules 和硬依赖模块。 |
| `CommandOutputTests` | text/json/markdown 格式稳定。 |
| `CommandErrorContractTests` | 所有命令声明的错误码都存在于 error code catalog。 |
| `CommandAotTests` | JSON context 覆盖所有 payload DTO。 |
