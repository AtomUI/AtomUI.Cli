# dotnet atomui upgrade 详细设计

## 目标

`upgrade` 输出 CLI 自身和项目 AtomUI 包的升级计划。它默认只读；`--write` 只允许修改项目包版本，不执行在线查询和外部命令。

## Options

```csharp
public sealed record UpgradeCommandOptions(
    GlobalCliOptions Global,
    string Path,
    string To,
    bool Cli,
    bool Packages,
    bool Write,
    bool Force) : IAtomUICliCommandOptions;
```

## Payload

```csharp
public sealed record UpgradeCommandPayload(
    string SchemaVersion,
    string Command,
    bool Write,
    string TargetVersion,
    CliUpgradeDto? Cli,
    IReadOnlyList<PackageUpgradeDto> Packages,
    MigrationSummaryDto Migration,
    IReadOnlyList<SetupActionDto> Actions,
    IReadOnlyList<string> VerificationCommands);
```

## Handler

依赖：

- `IVersionIndexLoader`
- `IUpgradeAdvisor`
- `IPackageReferenceReader`
- `IMigrationPlanBuilder`
- `ISetupWriter`
- `IOutputWriter`

执行步骤：

1. 校验 `To` 在版本索引中。
2. 如果 `Cli` 为 true，生成 CLI tool update 建议。
3. 如果 `Packages` 为 true，读取项目包引用。
4. 生成包升级 actions。
5. 查询迁移摘要。
6. dry-run 输出计划。
7. `--write` 时更新受控包版本文件。

## 版本来源

- 内置 metadata 版本索引。
- 显式 `--data-root` 中的版本索引。
- 不联网查询最新版本。

## 错误

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | 目标版本非法。 |
| `ATOMUICLI_DATA004` | 版本索引缺失。 |
| `ATOMUICLI_PRJ001` | 未找到项目。 |
| `ATOMUICLI_PKG003` | 包冲突阻止升级。 |
| `ATOMUICLI_SETUP003` | 写入失败。 |

## 测试

- `--cli` 只输出 CLI 升级建议。
- 项目包低于目标版本时生成 update action。
- 目标版本不存在时失败。
- dry-run 不修改版本文件。
- `--write` 只更新 AtomUI 相关包。

