# dotnet atomui demo 详细设计

## 目标

`demo` 查询控件示例列表或具体示例源码。示例来源于构建期从 Gallery ShowCase 提取的 AXAML、code-behind 和 ViewModel 片段。

## Options

```csharp
public sealed record DemoCommandOptions(
    GlobalCliOptions Global,
    string Control,
    string? Name,
    DemoLanguage Language,
    string? Scenario,
    bool Strict) : IAtomUICliCommandOptions;

public enum DemoLanguage
{
    All,
    Axaml,
    CSharp
}
```

## Payload

```csharp
public sealed record DemoCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string Control,
    IReadOnlyList<DemoDto> Demos);

public sealed record DemoDto(
    string Name,
    string Title,
    string? Description,
    string Scenario,
    IReadOnlyList<DemoFileDto> Files);
```

## Handler

依赖：

- `IControlQueryService`
- `IDemoQueryService`
- `IPackageQueryService`
- `IOutputWriter`

执行步骤：

1. 解析控件。
2. 未传 `Name` 时查询 demo summary。
3. 传 `Name` 时解析唯一 demo。
4. 应用 `--language` 和 `--scenario` 过滤。
5. 附加包和注册提示。
6. 输出结果。

## 输出规则

- 列表模式 text 输出 `Name`、`Title`、`Scenario`。
- 详情模式 markdown 输出多个 code fence。
- JSON 输出保留文件路径、语言、起止行号和内容。

## 错误

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG001` | 缺少控件名。 |
| `ATOMUICLI_ARG002` | language 非法。 |
| `ATOMUICLI_CTRL001` | 控件不存在。 |
| `ATOMUICLI_CTRL003` | demo 不存在。 |

## 测试

- 不传 demo 名返回列表。
- `--language axaml` 只返回 AXAML 片段。
- demo 名大小写不敏感。
- `--strict` 禁止候选 fallback。
- 示例内容保持构建期提取的原始缩进。

