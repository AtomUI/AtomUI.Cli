# dotnet atomui semantic 详细设计

## 目标

`semantic` 查询控件的可定制结构，包括 template part、pseudo class、selector、状态和关联 token。它用于安全编写样式和模板覆盖。

## Options

```csharp
public sealed record SemanticCommandOptions(
    GlobalCliOptions Global,
    string Control,
    string? Part,
    bool IncludeTemplate,
    bool Strict) : IAtomUICliCommandOptions;
```

## Payload

```csharp
public sealed record SemanticCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string Control,
    IReadOnlyList<SemanticPartDto> Parts,
    TemplateSummaryDto? Template);
```

## Handler

依赖：

- `IControlQueryService`
- `ISemanticPartQueryService`
- `ITokenQueryService`
- `IOutputWriter`

执行步骤：

1. 解析控件。
2. 查询 semantic parts。
3. 如果指定 `Part`，解析唯一 part。
4. `--include-template` 时加载 template summary。
5. `--detail` 时附加 states、pseudo classes 和 token 关联。
6. 输出结果。

## 输出规则

- `text`：简表输出 part、role、selector。
- `json`：输出 selector、states、tokens、source。
- `markdown`：按 part 分节。

## 错误

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG001` | 缺少控件名。 |
| `ATOMUICLI_CTRL001` | 控件不存在。 |
| `ATOMUICLI_CTRL005` | part 不存在。 |
| `ATOMUICLI_DATA001` | semantic 数据不可用。 |

## 测试

- TemplatePart 进入 parts。
- PseudoClasses 进入 states。
- `--part` 能精准裁剪。
- `--include-template` 输出摘要而不是完整模板源码。
- 不存在 part 返回候选建议。

