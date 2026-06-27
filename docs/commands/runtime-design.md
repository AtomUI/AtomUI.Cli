# Command Runtime 详细设计

## 目标

AtomUI Cli 命令运行时负责把 `dotnet atomui ...` 调用转换为强类型 options、命令 scope、handler 调用、结构化结果和退出码。命令运行时不包含具体业务查询逻辑，业务逻辑由 metadata、project analysis、MCP 和 setup 模块提供。

## 显式注册

所有命令通过模块 contribution catalog 注册：

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

禁止通过程序集扫描、attribute scanning 或命名约定发现命令。

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
CliCommandDispatcher
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
| `CommandOutputTests` | text/json/markdown 格式稳定。 |
| `CommandErrorContractTests` | 所有命令声明的错误码都存在于 error code catalog。 |
| `CommandAotTests` | JSON context 覆盖所有 payload DTO。 |
