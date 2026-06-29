# dotnet atomui demo 详细设计

## 1. 命令定位

`demo` 列出或输出控件示例代码。它面向开发者复制示例，也面向 Agent 生成符合 AtomUI 用法的代码。

## 2. 所属模块与注册

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliMetadataModule` |
| 注册阶段 | `ConfigureAtomUICliCommands` |
| 分组 | knowledge |
| 读写 | `isReadOnly=true` |
| 项目上下文 | `requiresProject=false` |
| 写入确认 | `requiresWriteConfirmation=false` |
| Handler 生命周期 | Transient |
| 支持格式 | text、json、markdown |

## 3. 调用语法

```bash
dotnet atomui demo Button
dotnet atomui demo Button basic
dotnet atomui demo DataGrid filtering --product datagrid --language xaml --format markdown
dotnet atomui demo Button --list --format json
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `control` | string | 必填 | 缺失返回 `ATOMUICLI_ARG001` | 控件名。 |
| `name` | string | null | 不存在返回 `ATOMUICLI_CTRL003` | 示例名；未传入时列出示例。 |
| `--scenario` | string | null | 无匹配时输出空列表和 warning | 场景过滤，例如 `basic`、`form`、`theme`。 |
| `--strict` | bool | false | 无 | 控件名和示例名要求精确匹配。 |
| `--list` | bool | false | 与具体 demo 可共存时忽略 detail | 只列示例摘要。 |
| `--code-only` | bool | false | 仅在具体 demo 时有效 | 只输出代码。 |
| `--language` | enum | `all` | 非法返回 `ATOMUICLI_ARG002` | `xaml`、`csharp`、`all`。 |

## 5. Options 类型

```csharp
public sealed record DemoCommandOptions(
    GlobalCliOptions Global,
    string Control,
    string? DemoName,
    string? Scenario,
    bool Strict,
    bool ListOnly,
    bool CodeOnly,
    DemoLanguage Language) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IMetadataCommandContextFactory`
- `IControlQueryService`
- `IDemoQueryService`
- `IOutputWriter`

## 7. 执行流程

1. 校验控件名。
2. 解析控件。
3. 查询 demo list 或 demo detail。
4. 应用 scenario、language 和 code-only。
5. 构建 payload。
6. 输出 text/json/markdown。

## 8. 领域服务契约

```csharp
public interface IDemoQueryService
{
    ValueTask<DemoQueryResult> QueryAsync(
        DemoQuery query,
        CancellationToken cancellationToken);
}
```

分支：`ListFound`、`DemoFound`、`ControlNotFound`、`DemoNotFound`、`DataUnavailable`。

## 9. 输出模型

```csharp
public sealed record DemoCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    ControlSummaryDto Control,
    IReadOnlyList<DemoSummaryDto> Demos,
    DemoDetailDto? SelectedDemo,
    IReadOnlyList<WarningDto> Warnings);
```

`DemoDetailDto` 包含 name、title、description、xaml code、csharp code、required packages、registration hints。

## 10. 输出格式

- text：list 模式输出 demo 名称和标题；detail 模式输出说明和代码块。
- json：输出完整 payload。
- markdown：输出带 heading 和 fenced code block 的示例文档。
- code-only：stdout 只输出请求语言代码，不输出标题。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | 缺少控件名 | `2` |
| `ATOMUICLI_ARG002` | language 非法 | `2` |
| `ATOMUICLI_CTRL001` | 控件不存在 | `3` |
| `ATOMUICLI_CTRL003` | demo 不存在 | `3` |
| `ATOMUICLI_DATA004` | 商业 demo 不可用 | `4` |

## 12. AOT-first 约束

示例代码来自 metadata snapshot。不得扫描 Gallery 源码或文件系统。DTO 纳入 JSON source generation。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| list | `demo Button --list` | demo 摘要列表。 |
| detail | `demo Button basic` | 输出代码和依赖。 |
| code only | `--code-only` | stdout 只有代码。 |
| missing demo | `demo Button none` | `ATOMUICLI_CTRL003`。 |
| json | `--format json` | payload 稳定。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Metadata/Commands/DemoCommandOptions.cs`
- `src/AtomUI.Cli.Metadata/Commands/DemoCommandHandler.cs`
- `src/AtomUI.Cli.Metadata/Queries/DemoQueryService.cs`
