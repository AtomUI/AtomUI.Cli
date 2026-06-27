# dotnet atomui token 详细设计

## 目标

`token` 查询全局、共享和控件级设计 Token。它用于主题定制、样式决策和组件外观诊断。

## Options

```csharp
public sealed record TokenCommandOptions(
    GlobalCliOptions Global,
    string? Control,
    string? Name,
    TokenScopeFilter Scope,
    string? Match) : IAtomUICliCommandOptions;

public enum TokenScopeFilter
{
    Global,
    Control,
    All
}
```

## Payload

```csharp
public sealed record TokenCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string Scope,
    string? Control,
    IReadOnlyList<TokenDto> Tokens);
```

## Handler

依赖：

- `IControlQueryService`
- `ITokenQueryService`
- `IOutputWriter`

执行步骤：

1. 校验 scope。
2. 如果传入控件名，解析控件。
3. 如果传入 token 名，先做精确查询。
4. 应用 `--match` 模糊过滤。
5. 按 scope、control、name 排序。
6. 输出结果。

## 输出规则

- `text`：表格列为 `Token`、`Scope`、`Type`、`Default`、`Status`。
- `json`：输出 token 全字段，包括 source 和 since。
- `markdown`：按 scope 分组。

## 错误

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | scope 非法。 |
| `ATOMUICLI_CTRL001` | 控件不存在。 |
| `ATOMUICLI_CTRL004` | token 不存在。 |
| `ATOMUICLI_DATA001` | token 数据不可用。 |

## 测试

- 无参数返回 global/shared token。
- 指定控件返回控件 token。
- `--name` 不存在时返回候选建议。
- `--match` 过滤名称和描述。
- 输出不读取用户主题文件。

