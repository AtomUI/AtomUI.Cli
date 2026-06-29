# dotnet atomui migrate 详细设计

## 1. 命令定位

`migrate` 输出版本迁移清单、破坏性变更、包替代建议和 Agent 可执行迁移提示。它默认只读，不修改项目。

## 2. 所属模块与注册

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliProjectAnalysisModule` |
| 注册阶段 | `ConfigureAtomUICliCommands` |
| 分组 | project-analysis |
| 读写 | `isReadOnly=true` |
| 项目上下文 | optional |
| 写入确认 | `requiresWriteConfirmation=false` |
| Handler 生命周期 | Transient |
| 支持格式 | text、json、markdown |

## 3. 调用语法

```bash
dotnet atomui migrate 1.0.0 1.1.0
dotnet atomui migrate 1.0.0 1.1.0 ./src/App --format markdown
dotnet atomui migrate --from 1.0.0 --to 1.1.0 --project ./App.csproj --format json
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `from` | version | null | 缺失返回 `ATOMUICLI_ARG001`，非法返回 `ATOMUICLI_ARG002` | 位置参数源版本。 |
| `to` | version | null | 缺失返回 `ATOMUICLI_ARG001`，非法返回 `ATOMUICLI_ARG002` | 位置参数目标版本。 |
| `path` | path | null | 不存在返回 `ATOMUICLI_PRJ001` | 位置参数项目入口。 |
| `--from` | version | 项目当前版本或 metadata 最低版本 | 非法返回 `ATOMUICLI_ARG002` | 源版本。 |
| `--to` | version | target version | 非法返回 `ATOMUICLI_ARG002` | 目标版本。 |
| `--project` | path | null | 不存在返回 `ATOMUICLI_PRJ001` | 项目路径，优先级高于位置参数 path。 |
| `--control` | string | null | 不存在返回 `ATOMUICLI_CTRL001` | 控件过滤。 |
| `--breaking-only` | bool | false | 无 | 只输出 breaking。 |
| `--include-agent-prompts` | bool | true | 无 | 输出 Agent 迁移提示。 |

## 5. Options 类型

```csharp
public sealed record MigrateCommandOptions(
    GlobalCliOptions Global,
    string? PositionalFrom,
    string? PositionalTo,
    string? Path,
    string? Project,
    string? From,
    string? To,
    string? Control,
    bool BreakingOnly,
    bool IncludeAgentPrompts) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IMetadataCommandContextFactory`
- `IProjectAnalysisContextFactory`
- `IMigrationPlanService`
- `IControlQueryService`
- `IOutputWriter`

## 7. 执行流程

1. 合并位置参数和 `--from`/`--to`，解析版本范围。
2. 合并位置参数 path 和 `--project`，创建可选项目分析上下文。
3. 可选解析 control。
4. 查询 migration guides 和 changelog。
5. 结合项目 usage 生成 project-aware steps。
6. 输出 migration payload。

## 8. 领域服务契约

```csharp
public interface IMigrationPlanService
{
    ValueTask<MigrationPlanResult> CreateAsync(
        MigrationPlanRequest request,
        CancellationToken cancellationToken);
}
```

分支：`Success`、`InvalidVersionRange`、`ProjectInvalid`、`ControlNotFound`、`DataUnavailable`。

## 9. 输出模型

```csharp
public sealed record MigrateCommandPayload(
    string SchemaVersion,
    string Command,
    string FromVersion,
    string ToVersion,
    ProjectSummaryDto? Project,
    ControlSummaryDto? Control,
    IReadOnlyList<MigrationStepDto> Steps,
    IReadOnlyList<ChangelogEntryDto> RelatedChanges,
    IReadOnlyList<AgentMigrationPromptDto> AgentPrompts,
    IReadOnlyList<WarningDto> Warnings);
```

## 10. 输出格式

- text：迁移步骤摘要。
- json：完整 steps 和 related changes。
- markdown：迁移文档。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | from/to 缺失 | `2` |
| `ATOMUICLI_ARG002` | 版本范围非法 | `2` |
| `ATOMUICLI_CTRL001` | 控件不存在 | `3` |
| `ATOMUICLI_PRJ001` | 项目路径不存在 | `4` |
| `ATOMUICLI_DATA001` | migration 数据不可用 | `4` |
| `ATOMUICLI_DATA005` | 版本范围无法解析到快照 | `4` |

## 12. AOT-first 约束

迁移数据来自 metadata snapshot。不执行代码修改。不加载用户程序集。DTO 纳入 JSON context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| metadata-only | `1.0 1.1` | migration steps。 |
| project-aware | `migrate fixture --to 1.1` | usage 相关步骤。 |
| breaking | `--breaking-only` | 只输出 breaking。 |
| invalid range | from > to | `ATOMUICLI_ARG002`。 |
| missing version | `9.0 9.1` | `ATOMUICLI_DATA005`。 |
| json | `--format json` | steps 稳定。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.ProjectAnalysis/Commands/MigrateCommandOptions.cs`
- `src/AtomUI.Cli.ProjectAnalysis/Commands/MigrateCommandHandler.cs`
- `src/AtomUI.Cli.ProjectAnalysis/Migrations/MigrationPlanService.cs`
