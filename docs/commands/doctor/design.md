# dotnet atomui doctor 详细设计

## 1. 命令定位

`doctor` 检查项目中影响 AtomUI 正常运行的问题，包括包兼容、包冲突、注册缺失、XAML 命名空间、AOT/trimming 风险和 generated 文件风险。

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
dotnet atomui doctor
dotnet atomui doctor ./src/App --severity warning
dotnet atomui doctor --rule PackageConflictRule,RegistrationRule --fail-on-warning --format json
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `path` | path | 当前目录 | 不存在返回 `ATOMUICLI_PRJ001` | 项目入口。 |
| `--severity` | enum | info | 非法返回 `ATOMUICLI_ARG002` | 输出最小 severity；`--minimum-severity` 作为兼容别名。 |
| `--rule` | string list | all doctor rules | 未知返回 `ATOMUICLI_ARG002` | 指定规则。 |
| `--no-usage-scan` | bool | false | 无 | 跳过 XAML/C# 使用扫描。 |
| `--fail-on-warning` | bool | false | 无 | warning 也返回 exit `5`。 |

## 5. Options 类型

```csharp
public sealed record DoctorCommandOptions(
    GlobalCliOptions Global,
    string Path,
    AtomUICliSeverity Severity,
    IReadOnlyList<string> Rules,
    bool NoUsageScan,
    bool FailOnWarning) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IProjectAnalysisContextFactory`
- `IProjectDiagnosticEngine`
- `IDiagnosticExitPolicy`
- `IOutputWriter`

## 7. 执行流程

1. 解析 path。
2. 创建 `ProjectAnalysisContext`。
3. 根据 `--rule` 和 `--no-usage-scan` 选择规则。
4. 执行 diagnostic engine。
5. 应用 severity 过滤。
6. 计算 summary 和 exit policy。
7. 输出 payload。

## 8. 领域服务契约

```csharp
public interface IProjectDiagnosticEngine
{
    ValueTask<ProjectDiagnosticResult> AnalyzeAsync(
        ProjectAnalysisContext context,
        DiagnosticRunOptions options,
        CancellationToken cancellationToken);
}
```

结果分支：`Success`、`DiagnosticsFailed`、`ProjectInvalid`、`DataUnavailable`。

## 9. 输出模型

```csharp
public sealed record DoctorCommandPayload(
    string SchemaVersion,
    string Command,
    ProjectSummaryDto Project,
    DiagnosticSummaryDto Summary,
    IReadOnlyList<ProjectDiagnosticDto> Diagnostics,
    IReadOnlyList<WarningDto> Warnings);
```

## 10. 输出格式

- text：输出 summary、error/warning 列表和建议。
- json：输出完整 diagnostics。
- markdown：输出诊断表格和修复建议。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | severity/rule 非法 | `2` |
| `ATOMUICLI_PRJ001` | 未找到项目 | `4` |
| `ATOMUICLI_PRJ002` | 项目文件无法读取 | `4` |
| `ATOMUICLI_DATA001` | metadata 不可用 | `4` |
| `ATOMUICLI_PKG003` | 包冲突 error | `5` |
| `ATOMUICLI_PRJ010` | 项目结构或 XAML error | `5` |
| `ATOMUICLI_AOT001` | AOT 诊断 error | `5` |

## 12. AOT-first 约束

规则显式注册。不加载用户程序集。不使用 MSBuild workspace。DTO 纳入 JSON context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| clean | `doctor fixture-clean` | exit `0`。 |
| conflict | 包冲突 fixture | `ATOMUICLI_PKG003`，exit `5`。 |
| rule filter | `--rule RegistrationRule` | 只执行该规则。 |
| no usage scan | `--no-usage-scan` | 不读取 XAML/C#。 |
| fail warning | `--fail-on-warning` | warning 返回 exit `5`。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.ProjectAnalysis/Commands/DoctorCommandOptions.cs`
- `src/AtomUI.Cli.ProjectAnalysis/Commands/DoctorCommandHandler.cs`
- `src/AtomUI.Cli.ProjectAnalysis/Diagnostics/ProjectDiagnosticEngine.cs`
