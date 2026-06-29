# dotnet atomui usage 详细设计

## 1. 命令定位

`usage` 统计项目中 AtomUI 控件、包、XAML 命名空间和注册入口的使用情况，用于迁移评估和代码审计。

## 2. 所属模块与注册

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliProjectAnalysisModule` |
| 注册阶段 | `ConfigureAtomUICliCommands` |
| 分组 | project-analysis |
| 读写 | `isReadOnly=true` |
| 项目上下文 | `requiresProject=true` |
| 写入确认 | `requiresWriteConfirmation=false` |
| Handler 生命周期 | Transient |
| 支持格式 | text、json、markdown |

## 3. 调用语法

```bash
dotnet atomui usage
dotnet atomui usage ./src/App --group-by control
dotnet atomui usage ./App.csproj --control Button --include-locations --format json
dotnet atomui usage ./App.slnx --group-by product
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `path` | path | 当前目录 | 不存在返回 `ATOMUICLI_PRJ001` | 项目入口。 |
| `--control` | string | null | 控件不存在返回 `ATOMUICLI_CTRL001` | 只统计指定控件。 |
| `--group-by` | enum | control | 非法返回 `ATOMUICLI_ARG002` | `control`、`product`、`file`、`category`。 |
| `--include-locations` | bool | false | 无 | 输出文件位置和行号。 |
| `--include-unused-packages` | bool | false | 无 | 输出可能未使用的 AtomUI 包。 |
| `--include-csharp` | bool | true | 无 | 是否扫描 C# 控件使用。 |
| `--include-xaml` | bool | true | 无 | 是否扫描 XAML 控件使用。 |

## 5. Options 类型

```csharp
public sealed record UsageCommandOptions(
    GlobalCliOptions Global,
    string Path,
    string? Control,
    UsageGroupBy GroupBy,
    bool IncludeLocations,
    bool IncludeUnusedPackages,
    bool IncludeCSharp,
    bool IncludeXaml) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IProjectAnalysisContextFactory`
- `IUsageAggregationService`
- `IOutputWriter`

## 7. 执行流程

1. 校验 include 至少一个为 true。
2. 创建项目分析上下文。
3. 可选解析 control 过滤。
4. 执行 XAML/C# usage scan。
5. 按 group-by 聚合。
6. 计算可能未使用包。
7. 构建 payload。
8. 输出。

## 8. 领域服务契约

```csharp
public interface IUsageAggregationService
{
    ValueTask<UsageAggregationResult> AggregateAsync(
        ProjectAnalysisContext context,
        UsageAggregationOptions options,
        CancellationToken cancellationToken);
}
```

结果分支：`Success`、`ProjectInvalid`、`ScanPartialFailure`。

## 9. 输出模型

```csharp
public sealed record UsageCommandPayload(
    string SchemaVersion,
    string Command,
    ProjectSummaryDto Project,
    ControlSummaryDto? Control,
    UsageSummaryDto Summary,
    IReadOnlyList<ControlUsageDto> Usages,
    IReadOnlyList<UsageGroupDto> Groups,
    IReadOnlyList<UnusedPackageDto> UnusedPackages,
    IReadOnlyList<WarningDto> Warnings);
```

## 10. 输出格式

- text：输出 summary 和前 N 个控件使用。
- json：输出全部 usage；只有 `--include-locations` 时包含 file、line、column。
- markdown：输出聚合表格。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | group-by 非法或 XAML/C# 都关闭 | `2` |
| `ATOMUICLI_CTRL001` | 指定控件不存在 | `3` |
| `ATOMUICLI_PRJ001` | 未找到项目 | `4` |
| `ATOMUICLI_PRJ002` | 项目文件无法读取 | `4` |

## 12. AOT-first 约束

不加载用户程序集。扫描器显式注册。DTO 纳入 JSON context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| xaml usage | XAML fixture | 控件和行号。 |
| csharp usage | C# fixture | new 控件统计。 |
| group product | `--group-by product` | 按产品聚合。 |
| control filter | `--control Button` | 只统计 Button。 |
| locations | `--include-locations` | 输出稳定路径和行号。 |
| no scans | `--include-xaml false --include-csharp false` | `ATOMUICLI_ARG002`。 |
| json | `--format json` | usage 明细稳定。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.ProjectAnalysis/Commands/UsageCommandOptions.cs`
- `src/AtomUI.Cli.ProjectAnalysis/Commands/UsageCommandHandler.cs`
- `src/AtomUI.Cli.ProjectAnalysis/Usage/UsageAggregationService.cs`
