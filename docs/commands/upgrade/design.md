# dotnet atomui upgrade 详细设计

## 1. 命令定位

`upgrade` 输出 CLI、AtomUI 包和 metadata snapshot 的升级建议，并可生成项目包版本更新计划。默认 dry-run。

## 2. 所属模块与注册

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliSetupModule` |
| 注册阶段 | `ConfigureAtomUICliCommands` |
| 分组 | write |
| 读写 | `isReadOnly=false` |
| 项目上下文 | optional |
| 写入确认 | `requiresWriteConfirmation=true` |
| Handler 生命周期 | Transient |
| 支持格式 | text、json、markdown |

## 3. 调用语法

```bash
dotnet atomui upgrade
dotnet atomui upgrade ./src/App
dotnet atomui upgrade ./App.csproj --to 1.1.0 --format json
dotnet atomui upgrade ./App.csproj --to 1.1.0 --write
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `path` | path | 当前目录 | 不存在返回 `ATOMUICLI_PRJ001` | 可选项目入口。 |
| `--to` | version | metadata 默认版本 | 非法返回 `ATOMUICLI_ARG002`，版本无法解析返回 `ATOMUICLI_DATA005` | 目标 AtomUI 版本；`--target-version` 是全局别名。 |
| `--cli` | bool | false | 无 | 只输出 CLI 工具升级建议。 |
| `--packages` | bool | true | 无 | 输出项目包升级建议。 |
| `--write` | bool | false | 无 | 写入项目包版本。 |
| `--force` | bool | false | 不能绕过 blocking conflict | 强制写入可恢复修改。 |

## 5. Options 类型

```csharp
public sealed record UpgradeCommandOptions(
    GlobalCliOptions Global,
    string? Path,
    string? To,
    bool CliOnly,
    bool Packages,
    bool Write,
    bool Force) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IMetadataCommandContextFactory`
- `IProjectAnalysisContextFactory`
- `IUpgradeAdvisor`
- `IWriteExecutor`
- `IOutputWriter`

## 7. 执行流程

1. 解析 `--to`/`--target-version`。
2. 可选读取项目上下文。
3. 查询当前 CLI、metadata 和包版本。
4. 生成升级建议。
5. 若启用 packages 且有项目，生成 write plan。
6. dry-run 输出或 `--write` 执行。

## 8. 领域服务契约

```csharp
public interface IUpgradeAdvisor
{
    ValueTask<UpgradeAdviceResult> CreateAsync(
        UpgradeAdviceRequest request,
        CancellationToken cancellationToken);
}
```

分支：`AdviceCreated`、`AlreadyLatest`、`ProjectInvalid`、`DataUnavailable`、`Conflict`、`WriteFailed`。

## 9. 输出模型

```csharp
public sealed record UpgradeCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    ProjectSummaryDto? Project,
    IReadOnlyList<UpgradeRecommendationDto> Recommendations,
    WritePlan? PackageWritePlan,
    IReadOnlyList<WarningDto> Warnings);
```

## 10. 输出格式

- text：输出升级建议和风险。
- json：输出 recommendations 和 write plan。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | 目标版本非法 | `2` |
| `ATOMUICLI_DATA001` | metadata 版本索引不可用 | `4` |
| `ATOMUICLI_DATA005` | 目标版本无法解析到快照 | `4` |
| `ATOMUICLI_PRJ001` | 项目不存在 | `4` |
| `ATOMUICLI_PKG003` | 包升级冲突 | `5` |
| `ATOMUICLI_SETUP002` | 写入计划冲突 | `6` |
| `ATOMUICLI_SETUP004` | 写入失败 | `6` |

## 12. AOT-first 约束

不访问网络查询最新版本。升级建议来自 metadata snapshot。写入使用结构化 XML。DTO 纳入 JSON context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| metadata only | `upgrade` | CLI/metadata 建议。 |
| project | `upgrade fixture` | 包升级建议。 |
| already latest | 最新 fixture | no-op recommendation。 |
| missing target | `--to 9.0.0` | `ATOMUICLI_DATA005`。 |
| conflict | 冲突 fixture | `ATOMUICLI_PKG003`。 |
| write | `--write` | 修改包版本。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Hosting/Setup/UpgradeCommandOptions.cs`
- `src/AtomUI.Cli.Hosting/Setup/UpgradeCommandHandler.cs`
- `src/AtomUI.Cli.Hosting/Setup/UpgradeAdvisor.cs`
