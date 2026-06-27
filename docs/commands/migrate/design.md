# dotnet atomui migrate 详细设计

## 目标

`migrate` 生成版本迁移计划，覆盖包升级、破坏性变更、API 变化、Token 变化、样式变化和验证命令。默认不读取项目；传入项目后生成项目相关迁移步骤。

## Options

```csharp
public sealed record MigrateCommandOptions(
    GlobalCliOptions Global,
    string From,
    string To,
    string? ProjectPath,
    bool IncludeAgentPrompts) : IAtomUICliCommandOptions;
```

## Payload

```csharp
public sealed record MigrateCommandPayload(
    string SchemaVersion,
    string Command,
    string From,
    string To,
    IReadOnlyList<MigrationStepDto> Steps,
    IReadOnlyList<string> VerificationCommands,
    IReadOnlyList<string> AgentPrompts);
```

## Handler

依赖：

- `IVersionIndexLoader`
- `IMetadataSnapshotLoader`
- `IChangelogQueryService`
- `IPackageQueryService`
- `IMigrationPlanBuilder`
- `ProjectCommandContextFactory`
- `IOutputWriter`

执行步骤：

1. 解析 from/to，禁止位置参数和命名参数冲突。
2. 校验版本存在且方向合法。
3. 加载迁移范围内 changelog 和 migration guides。
4. 比较 from/to 快照中的 API、Token 和包关系。
5. 如果传入项目路径，读取项目包和使用统计。
6. 构建迁移步骤、验证命令和 Agent prompts。
7. 输出 payload。

## 输出规则

- 默认 `markdown`。
- `json` 用于 Agent 自动规划。
- text 输出只保留摘要和验证命令。

## 错误

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG001` | 缺少 from 或 to。 |
| `ATOMUICLI_ARG002` | 版本冲突或方向非法。 |
| `ATOMUICLI_DATA004` | 版本索引缺失。 |
| `ATOMUICLI_PRJ001` | 指定项目不存在。 |

## 测试

- from/to 缺失返回参数错误。
- 版本范围方向非法返回参数错误。
- 快照 diff 生成 API/Token/package 步骤。
- 传入项目路径后只输出相关步骤。
- markdown 包含迁移后验证命令。

