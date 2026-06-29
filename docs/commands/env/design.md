# dotnet atomui env 详细设计

## 1. 命令定位

`env` 读取项目和运行环境信息，输出 SDK、TargetFramework、OS、AtomUI 包、Avalonia 包和配置摘要。它是诊断前的轻量命令，默认不扫描 XAML/C# 源码。

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
dotnet atomui env
dotnet atomui env ./src/App
dotnet atomui env ./App.csproj --detail
dotnet atomui env ./App.slnx --include-files --format json
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `path` | path | 当前目录 | 不存在返回 `ATOMUICLI_PRJ001` | 目录、`.slnx`、`.sln` 或 `.csproj`。 |
| `--include-packages` | bool | true | 无 | 输出项目包引用和 central package version。 |
| `--include-files` | bool | false | 无 | 输出参与分析的关键文件路径。 |
| `--detail` | global bool | false | 无 | 输出 RID、RuntimeIdentifier、publish/AOT 属性和 lock 文件摘要。 |

## 5. Options 类型

```csharp
public sealed record EnvCommandOptions(
    GlobalCliOptions Global,
    string Path,
    bool IncludePackages,
    bool IncludeFiles,
    bool Detail) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IProjectAnalysisContextFactory`
- `IEnvironmentSnapshotService`
- `IOutputWriter`

## 7. 执行流程

1. 解析 path。
2. 读取 solution/project 文件。
3. 读取 package references 和 central package versions。
4. 读取 SDK、OS、RID、target framework。
5. 按选项补充 packages、关键文件和 detail 字段。
6. 构建 `EnvCommandPayload`。
7. 输出。

## 8. 领域服务契约

```csharp
public interface IEnvironmentSnapshotService
{
    ValueTask<EnvironmentSnapshotResult> CreateAsync(
        ProjectAnalysisContext context,
        EnvSnapshotOptions options,
        CancellationToken cancellationToken);
}
```

分支：`Success`、`ProjectNotFound`、`ProjectUnreadable`、`PartialProjectFailure`。

## 9. 输出模型

```csharp
public sealed record EnvCommandPayload(
    string SchemaVersion,
    string Command,
    ProjectSummaryDto Project,
    RuntimeEnvironmentDto Runtime,
    IReadOnlyList<PackageReferenceDto> AtomUIPackages,
    IReadOnlyList<PackageReferenceDto> AvaloniaPackages,
    IReadOnlyList<ProjectFileDto> Files,
    IReadOnlyList<WarningDto> Warnings);
```

## 10. 输出格式

- text：环境摘要和包版本列表。
- json：完整 payload。
- markdown：环境摘要表和包列表。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_PRJ001` | path 不存在或找不到项目 | `4` |
| `ATOMUICLI_PRJ002` | 项目文件不可读或 XML 非法 | `4` |
| `ATOMUICLI_ARG002` | path 类型非法 | `2` |

## 12. AOT-first 约束

不使用 MSBuild workspace，不执行 restore/build。DTO 纳入 JSON context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| 当前目录 | `env` | 成功输出环境。 |
| csproj | `env App.csproj` | 单项目摘要。 |
| missing | `env none` | `ATOMUICLI_PRJ001`。 |
| json | `--format json` | payload 稳定。 |
| include files | `--include-files` | 输出关键文件路径。 |
| detail | `--detail` | 输出 RID、publish/AOT 摘要。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.ProjectAnalysis/Commands/EnvCommandOptions.cs`
- `src/AtomUI.Cli.ProjectAnalysis/Commands/EnvCommandHandler.cs`
- `src/AtomUI.Cli.ProjectAnalysis/Environment/EnvironmentSnapshotService.cs`
