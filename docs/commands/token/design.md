# dotnet atomui token 详细设计

## 1. 命令定位

`token` 查询 AtomUI 全局 Token 或控件 Token，用于主题、样式和 Agent 生成样式代码。

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
dotnet atomui token
dotnet atomui token Button
dotnet atomui token Button --name colorPrimary
dotnet atomui token --scope global --format json
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `control` | string | null | 不存在返回 `ATOMUICLI_CTRL001` | 控件 token 查询。 |
| `--scope` | enum | all | 非法返回 `ATOMUICLI_ARG002` | `global`、`control`、`all`；无 control 时 `all` 只返回全局 Token。 |
| `--name` | string | null | 不存在返回 `ATOMUICLI_CTRL004` | token 名过滤。 |
| `--match` | string | null | 无 | 按名称或描述做大小写不敏感过滤。 |
| `--include-inherited` | bool | true | 无 | 是否包含继承 token。 |

## 5. Options 类型

```csharp
public sealed record TokenCommandOptions(
    GlobalCliOptions Global,
    string? Control,
    TokenScope Scope,
    string? Name,
    string? Match,
    bool IncludeInherited) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IMetadataCommandContextFactory`
- `IControlQueryService`
- `ITokenQueryService`
- `IOutputWriter`

## 7. 执行流程

1. 解析 scope：无 control 时默认 global，有 control 时默认 control。
2. 若传入 control，先解析控件。
3. 调用 token query service。
4. 应用 name、match 和 inherited 过滤。
5. 构建 payload。
6. 输出。

## 8. 领域服务契约

```csharp
public interface ITokenQueryService
{
    ValueTask<TokenQueryResult> QueryAsync(
        TokenQuery query,
        CancellationToken cancellationToken);
}
```

分支：`Found`、`TokenNotFound`、`ControlNotFound`、`DataUnavailable`。

## 9. 输出模型

```csharp
public sealed record TokenCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    TokenScope Scope,
    ControlSummaryDto? Control,
    IReadOnlyList<TokenDto> Tokens,
    IReadOnlyList<WarningDto> Warnings);
```

`TokenDto` 包含 name、type、defaultValue、description、scope、source、isInherited。

## 10. 输出格式

- text：按 category 分组显示 token。
- json：输出完整 token 字段。
- markdown：输出 token 表格。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | scope 非法 | `2` |
| `ATOMUICLI_CTRL001` | 控件不存在 | `3` |
| `ATOMUICLI_CTRL004` | token 不存在 | `3` |
| `ATOMUICLI_DATA001` | token 数据不可用 | `4` |

## 12. AOT-first 约束

Token 数据只来自 snapshot。DTO 纳入 JSON context。scope/name 匹配使用 ordinal ignore case。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| global | `token` | global tokens。 |
| control | `token Button` | Button tokens。 |
| name | `--name colorPrimary` | 单 token。 |
| match | `--match color` | 模糊过滤 token。 |
| inherited false | `--include-inherited false` | 不含继承项。 |
| missing | `--name none` | `ATOMUICLI_CTRL004`。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Metadata/Commands/TokenCommandOptions.cs`
- `src/AtomUI.Cli.Metadata/Commands/TokenCommandHandler.cs`
- `src/AtomUI.Cli.Metadata/Queries/TokenQueryService.cs`
