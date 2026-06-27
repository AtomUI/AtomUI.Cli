# dotnet atomui changelog 详细设计

## 目标

`changelog` 查询版本变更、控件变更和包变更。它同时支持普通 query mode 和版本 diff mode，为迁移和回归排查提供上下文。

## Options

```csharp
public sealed record ChangelogCommandOptions(
    GlobalCliOptions Global,
    string? Range,
    string? Control,
    string? Package,
    ChangelogSeverity? Severity,
    string? DiffFrom,
    string? DiffTo) : IAtomUICliCommandOptions;
```

## Payload

```csharp
public sealed record ChangelogCommandPayload(
    string SchemaVersion,
    string Command,
    VersionRangeDto Range,
    IReadOnlyList<ChangelogEntryDto> Entries,
    ChangelogDiffDto? Diff);
```

## Handler

依赖：

- `IVersionIndexLoader`
- `IChangelogQueryService`
- `IControlQueryService`
- `IPackageQueryService`
- `IMetadataSnapshotLoader`
- `IOutputWriter`

执行步骤：

1. 判断 query mode 或 diff mode。
2. 解析版本范围，校验方向。
3. 解析控件和包过滤。
4. query mode 读取 changelog index。
5. diff mode 比较两个版本快照中的 API、Token、package 变化。
6. 按版本倒序输出。

## 输出规则

- `text`：按版本分组。
- `json`：输出 entries 和可选 diff。
- `markdown`：适合迁移文档和 release note。

## 错误

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | range、severity 或 diff 参数非法。 |
| `ATOMUICLI_CTRL001` | 控件不存在。 |
| `ATOMUICLI_PKG001` | 包不存在。 |
| `ATOMUICLI_DATA004` | 版本索引缺失。 |

## 测试

- 无参数返回目标版本变更。
- `from..to` 范围过滤正确。
- diff mode 要求 from/to 成对出现。
- 控件和包过滤可组合。
- breaking severity 单独过滤。

