# dotnet atomui help 详细设计

## 1. 命令定位

`help` 是 AtomUI Cli 的用户入口和命令导航命令。它必须回答三个问题：

1. AtomUI Cli 能做什么。
2. 常用命令应该怎么开始。
3. 如何查看某个命令的详细参数和示例。

`help` 不能输出裸命令名列表。首页应按能力组织内容，详情页应输出可直接复制的 usage 和 examples。

## 2. 所属模块与注册

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliCoreModule` |
| 注册阶段 | `ConfigureAtomUICliCommands` |
| 分组 | integration |
| 读写 | `isReadOnly=true` |
| 项目上下文 | `requiresProject=false` |
| 写入确认 | `requiresWriteConfirmation=false` |
| Handler 生命周期 | Transient |
| 支持格式 | text、json、markdown |

`help` 依赖 `CommandManifestCatalog`，默认不依赖 active command descriptor catalog。这样 `dotnet atomui help` 不会为了列出所有命令而激活全部业务模块。

## 3. 调用语法

```bash
dotnet atomui help
dotnet atomui --help
dotnet atomui help info
dotnet atomui info --help
dotnet atomui help --format json
dotnet atomui help info --format markdown
```

语义规则：

- 空参数和 `--help` 映射到 `help` 首页。
- `help <command>` 输出目标命令详情。
- `<command> --help` 在预解析阶段转换为 help detail，不执行目标命令 handler。
- `help` 首页只读取 manifest。
- `help <command>` 首期也只读取 manifest；后续如果需要命令私有 option provider，只激活目标命令所属模块和 required modules。

## 4. 参数与选项

| 参数或选项 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `command` | string | null | 不存在返回 `ATOMUICLI_ARG003` | 目标命令名，例如 `info`、`doctor`。 |
| `--format` | global enum | `text` | 非法值返回 `ATOMUICLI_ARG002` | 支持 `text`、`json`、`markdown`。 |
| `--lang` | global enum | `zh` | 非法值返回 `ATOMUICLI_ARG002` | 帮助文案语言。 |
| `--detail` | global bool | false | 无 | 首页输出全部命令；默认首页突出常用命令和分组摘要。 |

`help` 忽略 `--target-version`、`--product`、`--data-root` 和 `--no-update-check` 的业务语义，但 parser 仍按全局选项规则解析它们。

## 5. Options 类型

```csharp
public sealed record HelpCommandOptions(
    GlobalCliOptions Global,
    string? CommandName,
    bool IsDetailRequest) : IAtomUICliCommandOptions;
```

`BuiltInCommandOptions` 不能长期承载 help 参数。实现时应拆出 `HelpCommandOptions`，避免 help detail 与 version 共用无语义 options。

## 6. Handler 依赖

| 依赖 | 生命周期 | 用途 |
| --- | --- | --- |
| `CommandManifestCatalog` | Singleton | 首页和详情页的事实来源。 |
| `IHelpContentRenderer` | Singleton | 渲染 text、markdown 和 JSON payload。 |
| `IHelpSuggestionService` | Singleton | 为未知命令生成相似命令建议。 |
| `IOutputWriter` | Scoped | 输出成功结果。 |

Handler 不接收 root `IServiceProvider`，不创建 scope，不读取用户项目，不访问 metadata data-root，不枚举 DI 服务发现命令。

## 7. 执行流程

首页流程：

1. 从产品元数据读取 CLI 展示版本和版权信息。
2. 从 `CommandManifestCatalog` 读取所有 command manifest。
3. 按 `CommandGroup` 分组，按预设优先级排序。
4. 选择 common commands：`list`、`info`、`doc`、`demo`、`doctor`、`setup`。
5. 生成 `HelpHomePayload`。
6. 根据 `--format` 渲染 text、json 或 markdown。

详情流程：

1. 读取 `CommandName`。
2. 在 `CommandManifestCatalog` 中查找目标命令。
3. 未找到时返回 `ATOMUICLI_ARG003`，`suggestion` 包含相似命令。
4. 找到时生成 `HelpCommandPayload`。
5. 输出目标命令 summary、usage、arguments、options、examples、supported formats、requires project、write confirmation。

`<command> --help` 流程必须在命令 handler 执行前拦截，不能先构造目标命令 options 再返回帮助。

## 8. 领域服务契约

help 属于 Core runtime 服务，不依赖业务领域服务。推荐抽象：

```csharp
public interface IHelpContentProvider
{
    HelpHomePayload CreateHome(GlobalCliOptions global);

    HelpCommandLookupResult CreateCommandHelp(
        string commandName,
        GlobalCliOptions global);
}

public abstract record HelpCommandLookupResult
{
    public sealed record Found(HelpCommandPayload Payload) : HelpCommandLookupResult;

    public sealed record NotFound(
        string CommandName,
        IReadOnlyList<string> Suggestions) : HelpCommandLookupResult;
}
```

`CommandManifest` 必须提供帮助所需的轻量字段：

```csharp
public sealed record CommandManifest(
    string Name,
    Type OwnerModuleType,
    CommandGroup Group,
    string Summary,
    string Usage,
    IReadOnlyList<CommandArgumentHelp> Arguments,
    IReadOnlyList<CommandOptionHelp> Options,
    IReadOnlyList<string> Examples,
    IReadOnlySet<OutputFormat> SupportedFormats,
    bool IsReadOnly,
    bool RequiresProject,
    bool RequiresWriteConfirmation,
    IReadOnlyList<Type>? RequiredModuleTypes);
```

这些字段必须来自显式代码或 source generator，不能通过运行时扫描 handler 或 attributes 得到。

## 9. 输出模型

```csharp
public sealed record HelpHomePayload(
    string SchemaVersion,
    string Command,
    string Version,
    string Copyright,
    string Usage,
    IReadOnlyList<HelpCommandSummaryDto> CommonCommands,
    IReadOnlyList<HelpCommandGroupDto> Groups,
    IReadOnlyList<HelpOptionDto> GlobalOptions,
    IReadOnlyList<string> More);

public sealed record HelpCommandPayload(
    string SchemaVersion,
    string Command,
    string Name,
    string Summary,
    string Usage,
    IReadOnlyList<HelpArgumentDto> Arguments,
    IReadOnlyList<HelpOptionDto> Options,
    IReadOnlyList<string> Examples,
    IReadOnlyList<string> SupportedFormats,
    string Group,
    string OwnerModule,
    bool RequiresProject,
    bool RequiresWriteConfirmation);
```

排序规则：

- command group 顺序：Knowledge、Project analysis、Setup、Integration。
- 每个 group 内按用户使用频率排序，不按字母机械排序。
- JSON 列表顺序必须与 text 输出一致。

## 10. 输出格式

text 首页必须包含：

- 标题 `AtomUI Cli`。
- `Version`，取 CLI 包版本，不包含 Git 修订号后缀。
- 版权信息，格式为 `Copyright (c) 2018-2026 Qinware Technologies Co., Ltd. All rights reserved.`。
- `Usage`。
- `Common commands`。
- `Command groups`。
- `Global options`。
- `More`。

markdown 首页使用二级标题组织同样内容，必须在标题下方输出版本和版权，命令示例使用 `bash` code fence。

json 首页输出 `HelpHomePayload`，不能输出纯字符串数组，并且必须包含 `version` 和 `copyright` 字段，便于 Agent 和脚本判断当前 CLI 版本。

详情页 text 示例：

```text
info

Show metadata for an AtomUI control.

Usage:
  dotnet atomui info <control> [options]

Arguments:
  <control>            Control name, for example Button or DataGrid.

Options:
  --include <section>  Include metadata sections.
  --strict             Disable fuzzy matching.
  --format <format>    text, json, markdown.

Examples:
  dotnet atomui info Button
  dotnet atomui info DataGrid --format json
```

## 11. 错误与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `--format`、`--lang` 或 help 选项组合非法。 | `2` |
| `ATOMUICLI_ARG003` | `help <command>` 的目标命令不存在或未注册。 | `2` |

未知命令错误必须包含：

- `details.command`：用户输入的命令名。
- `details.suggestions`：逗号分隔的相似命令名，最多 3 个。
- `suggestion`：可执行下一步，例如 `Run dotnet atomui help info.`。

## 12. AOT-first 约束

- help 内容来自 `CommandManifestCatalog` 显式数据或 source generator。
- 不扫描程序集、attributes、handler 类型或 XML doc comments。
- JSON payload DTO 纳入 source generated JSON context。
- 命令建议使用静态字符串距离算法，不依赖动态代码生成。
- `help` 首页不得激活全部模块。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| 首页 text | `help` | 包含 `AtomUI Cli`、`Usage`、`Common commands`、`Command groups`、`Global options`。 |
| 首页产品元数据 | `help` | 包含 CLI 包版本和 `Qinware Technologies Co., Ltd.` 版权信息。 |
| 首页不裸列 | `help` | 每个命令至少带 summary；不得只输出命令名列表。 |
| 首页 json | `help --format json` | 输出 `HelpHomePayload`，包含 version、copyright、commonCommands 和 groups。 |
| 详情 text | `help info` | 输出 info summary、usage、arguments、options、examples。 |
| 命令 help shortcut | `info --help` | 输出 info help，不执行 `InfoCommandHandler`。 |
| 未知详情命令 | `help inf` | 返回 `ATOMUICLI_ARG003`，suggestions 包含 `info`。 |
| 按需激活 | `help` | 只激活 Core，不创建 metadata/project/setup/MCP 模块实例。 |
| manifest 完整性 | built-in manifests | 所有命令都有 summary、usage、examples 和 supported formats。 |
| markdown | `help info --format markdown` | 标题顺序稳定，示例使用 bash code fence。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Hosting/Commands/BuiltIn/HelpCommandOptions.cs`
- `src/AtomUI.Cli.Hosting/Commands/BuiltIn/HelpCommandHandler.cs`
- `src/AtomUI.Cli.Hosting/Commands/BuiltIn/HelpContentProvider.cs`
- `src/AtomUI.Cli.Hosting/Commands/BuiltIn/HelpContentRenderer.cs`
- `src/AtomUI.Cli.Hosting/Commands/CommandManifest.cs`
- `tests/AtomUI.Cli.Tests/Core/HelpCommandTests.cs`
- `tests/AtomUI.Cli.Tests/Core/CommandManifestTests.cs`
