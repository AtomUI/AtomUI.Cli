# dotnet atomui info 详细设计

## 1. 命令定位

`info` 输出单个控件的结构化知识，包括包、命名空间、注册入口、API、Token、semantic parts 和 demo 摘要。它是 Agent 写控件代码前的主查询命令。

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
dotnet atomui info Button
dotnet atomui info Button --include api,tokens,semantic,demos --format json
dotnet atomui info Button --strict --product desktop
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `control` | string | 必填 | 缺失返回 `ATOMUICLI_ARG001` | 控件名或别名。 |
| `--include` | enum list | `summary,registration,api` | 非法项返回 `ATOMUICLI_ARG002` | 附加输出区块。 |
| `--strict` | bool | false | 无 | 禁止 fuzzy match。 |
| `--product` | global string | all visible | 未知产品返回 `ATOMUICLI_PKG001` | 限定产品。 |
| `--detail` | global bool | false | 无 | 输出完整 API 和 metadata source。 |

## 5. Options 类型

```csharp
public sealed record InfoCommandOptions(
    GlobalCliOptions Global,
    string Control,
    IReadOnlySet<InfoSection> Include,
    bool Strict) : IAtomUICliCommandOptions;

public enum InfoSection
{
    Summary,
    Registration,
    Api,
    Properties,
    Events,
    Methods,
    Tokens,
    Semantic,
    Demos,
    Changelog
}
```

## 6. Handler 依赖

| 依赖 | 生命周期 | 用途 |
| --- | --- | --- |
| `IMetadataCommandContextFactory` | Scoped | 创建 metadata 上下文。 |
| `IControlQueryService` | Singleton | 查询控件摘要和 API。 |
| `ITokenQueryService` | Singleton | 查询 Token 区块。 |
| `ISemanticPartQueryService` | Singleton | 查询 semantic parts。 |
| `IDemoQueryService` | Singleton | 查询 demo 摘要。 |
| `IChangelogQueryService` | Singleton | 查询控件变更。 |
| `IOutputWriter` | Scoped | 输出结果。 |

## 7. 执行流程

1. 校验控件名非空。
2. 解析 include 集合。
3. 创建 metadata 上下文。
4. 调用 `IControlQueryService`，传入 strict 和 product filter。
5. `NotFound` 时输出 suggestions。
6. `Ambiguous` 时输出 candidates。
7. 按 include 调用附加 query service。
8. 构建 `InfoCommandPayload`。
9. 输出 text/json/markdown。

## 8. 领域服务契约

```csharp
public sealed record ControlQuery(
    string Name,
    ProductFilter ProductFilter,
    string TargetVersion,
    bool Strict,
    IReadOnlySet<InfoSection> Include);
```

`ControlQueryResult` 分支：

| 分支 | 映射 |
| --- | --- |
| `Found` | 输出 payload。 |
| `NotFound` | `ATOMUICLI_CTRL001`，带 suggestions。 |
| `Ambiguous` | `ATOMUICLI_CTRL002`，带 candidates。 |
| `DataUnavailable` | `ATOMUICLI_DATA001` 至 `ATOMUICLI_DATA005`。 |

## 9. 输出模型

```csharp
public sealed record InfoCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    ProductFilterDto ProductFilter,
    ControlSummaryDto Summary,
    RegistrationDto? Registration,
    ControlApiDto? Api,
    IReadOnlyList<TokenDto>? Tokens,
    IReadOnlyList<SemanticPartDto>? SemanticParts,
    IReadOnlyList<DemoSummaryDto>? Demos,
    IReadOnlyList<ChangelogEntryDto>? Changelog,
    IReadOnlyList<WarningDto> Warnings);
```

未请求区块输出 `null`，请求但无内容输出空列表。

## 10. 输出格式

- text：显示包、命名空间、注册方法、常用属性和建议 demo。
- json：输出完整 DTO。
- markdown：输出控件小文档，标题顺序为 Summary、Registration、API、Tokens、Semantic、Demos。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | 缺少控件名 | `2` |
| `ATOMUICLI_ARG002` | include 非法 | `2` |
| `ATOMUICLI_CTRL001` | 控件不存在 | `3` |
| `ATOMUICLI_CTRL002` | 控件名歧义 | `3` |
| `ATOMUICLI_DATA001` | metadata 不可用 | `4` |
| `ATOMUICLI_DATA004` | 商业数据不可用 | `4` |

## 12. AOT-first 约束

include parser 使用静态 map。payload DTO 纳入 JSON source generation。控件 API 只来自 metadata snapshot，不反射运行时控件类型。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| 大小写 | `info button` | 匹配 Button。 |
| strict | `info buton --strict` | `ATOMUICLI_CTRL001`。 |
| fuzzy | `info buton` | NotFound 带 Button suggestion。 |
| include | `--include tokens,semantic` | 只调用对应服务。 |
| json | `--format json` | 输出完整 payload。 |
| commercial hidden | 商业控件无数据 | `ATOMUICLI_DATA004` 或摘要提示。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Metadata/Commands/InfoCommandOptions.cs`
- `src/AtomUI.Cli.Metadata/Commands/InfoCommandHandler.cs`
- `src/AtomUI.Cli.Metadata/Queries/ControlQueryService.cs`
- `tests/AtomUI.Cli.Metadata.Tests/Commands/InfoCommandTests.cs`
