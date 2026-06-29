# dotnet atomui changelog 详细设计

## 1. 命令定位

`changelog` 查询 AtomUI 版本变更、控件变更、包变更和 breaking changes。它用于升级评估、迁移规划和 Agent 修改代码前的版本差异查询。

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
dotnet atomui changelog
dotnet atomui changelog 1.0.0..1.1.0
dotnet atomui changelog 1.0.0..1.1.0 Button
dotnet atomui changelog --from 1.0.0 --to 1.1.0 --severity breaking
dotnet atomui changelog --package AtomUI.Controls
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `range` | string | 当前目标版本 | 非法返回 `ATOMUICLI_ARG002`，版本无法解析返回 `ATOMUICLI_DATA005` | 位置参数，格式为 `<from>..<to>` 或单版本。 |
| `control` | string | null | 不存在返回 `ATOMUICLI_CTRL001` | 位置参数控件过滤。 |
| `--from` | version | null | 非法返回 `ATOMUICLI_ARG002`，版本无法解析返回 `ATOMUICLI_DATA005` | 起始版本；与 `range` 互斥。 |
| `--to` | version | target version | 非法返回 `ATOMUICLI_ARG002`，版本无法解析返回 `ATOMUICLI_DATA005` | 目标版本；与 `range` 互斥。 |
| `--control` | string | null | 不存在返回 `ATOMUICLI_CTRL001` | 控件过滤，优先级高于位置参数 control。 |
| `--package` | string | null | 不存在返回 `ATOMUICLI_PKG001` | 包过滤。 |
| `--severity` | enum | all | 非法返回 `ATOMUICLI_ARG002` | `info`、`warning`、`breaking`、`all`。 |

## 5. Options 类型

```csharp
public sealed record ChangelogCommandOptions(
    GlobalCliOptions Global,
    string? Range,
    string? From,
    string? To,
    string? Control,
    string? PackageId,
    ChangelogSeverityFilter Severity) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IMetadataCommandContextFactory`
- `IControlQueryService`
- `IPackageQueryService`
- `IChangelogQueryService`
- `IOutputWriter`

## 7. 执行流程

1. 解析位置参数 `range` 或 `--from`/`--to` 版本范围。
2. 校验 control/package 过滤目标。
3. 查询 changelog entries。
4. 应用 severity 过滤。
5. 按版本倒序排序。
6. 输出 payload。

## 8. 领域服务契约

```csharp
public interface IChangelogQueryService
{
    ValueTask<ChangelogQueryResult> QueryAsync(
        ChangelogQuery query,
        CancellationToken cancellationToken);
}
```

分支：`Found`、`Empty`、`InvalidRange`、`TargetNotFound`、`DataUnavailable`。

## 9. 输出模型

```csharp
public sealed record ChangelogCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string? From,
    string? To,
    ChangelogTargetDto? Target,
    ChangelogSeverityFilter Severity,
    IReadOnlyList<ChangelogEntryDto> Entries,
    IReadOnlyList<MigrationHintDto> MigrationHints,
    IReadOnlyList<WarningDto> Warnings);
```

## 10. 输出格式

- text：按版本输出摘要。
- json：输出完整 entries 和 migration hints。
- markdown：输出版本标题和 change list。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | 版本范围非法 | `2` |
| `ATOMUICLI_CTRL001` | 控件不存在 | `3` |
| `ATOMUICLI_PKG001` | 包不存在 | `3` |
| `ATOMUICLI_DATA001` | changelog 不可用 | `4` |
| `ATOMUICLI_DATA005` | 版本或版本范围无法解析到快照 | `4` |

## 12. AOT-first 约束

版本比较使用显式 semantic version parser。changelog 来自 snapshot。DTO 纳入 JSON context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| default | `changelog` | 当前目标版本变更。 |
| range | `--from 1.0.0 --to 1.1.0` | 范围内 entries。 |
| breaking | `--severity breaking` | 仅 breaking。 |
| control | `--control Button` | 仅 Button。 |
| invalid range | `--from 2.0 --to 1.0` | `ATOMUICLI_ARG002`。 |
| missing version | `9.0.0..9.0.1` | `ATOMUICLI_DATA005`。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Metadata/Commands/ChangelogCommandOptions.cs`
- `src/AtomUI.Cli.Metadata/Commands/ChangelogCommandHandler.cs`
- `src/AtomUI.Cli.Metadata/Queries/ChangelogQueryService.cs`
