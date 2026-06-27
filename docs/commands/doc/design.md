# dotnet atomui doc 详细设计

## 目标

`doc` 聚合控件完整文档，面向人类阅读和 Agent 上下文注入。它不直接读取 markdown 源文件，而是从 metadata snapshot 的文档区块渲染。

## Options

```csharp
public sealed record DocCommandOptions(
    GlobalCliOptions Global,
    string Control,
    DocSection Section,
    bool Strict) : IAtomUICliCommandOptions;

public enum DocSection
{
    All,
    Overview,
    Usage,
    Api,
    Tokens,
    Semantic,
    Demos,
    Changelog
}
```

## Payload

```csharp
public sealed record DocCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string Control,
    IReadOnlyList<DocumentSectionDto> Sections);

public sealed record DocumentSectionDto(
    string Id,
    string Title,
    string Format,
    string Content);
```

## Handler

依赖：

- `IControlQueryService`
- `IDocumentSectionBuilder`
- `IDemoQueryService`
- `ITokenQueryService`
- `ISemanticPartQueryService`
- `IChangelogQueryService`
- `IOutputWriter`

执行步骤：

1. 解析控件。
2. 解析 section。
3. 调用 `IDocumentSectionBuilder` 聚合 section。
4. 移除空 section。
5. 保持固定顺序：overview、usage、api、tokens、semantic、demos、changelog。
6. 输出 markdown、json 或 text。

## 输出规则

- 默认格式为 `markdown`。
- `text` 输出和 markdown 内容一致，但去掉多余 heading 层级。
- `json` 输出 section 数组，Agent 可自行裁剪。

## 错误

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG001` | 缺少控件名。 |
| `ATOMUICLI_ARG002` | section 非法。 |
| `ATOMUICLI_CTRL001` | 控件不存在。 |
| `ATOMUICLI_DATA001` | 文档区块不可用。 |

## 测试

- 默认输出所有非空 section。
- `--section api` 只输出 API。
- markdown code fence 语言稳定。
- section 缺失时不输出空标题。
- JSON section 顺序稳定。

