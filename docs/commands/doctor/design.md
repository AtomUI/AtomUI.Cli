# dotnet atomui doctor 详细设计

## 目标

`doctor` 检查项目是否存在影响 AtomUI 正常运行的问题，包括包兼容、包冲突、注册缺失、XAML 命名空间、AOT/trimming 风险和生成文件风险。

## Options

```csharp
public sealed record DoctorCommandOptions(
    GlobalCliOptions Global,
    string Path,
    DiagnosticSeverity MinimumSeverity,
    IReadOnlyList<string> Rules,
    bool NoUsageScan,
    bool FailOnWarning) : IAtomUICliCommandOptions;
```

## Payload

```csharp
public sealed record DoctorCommandPayload(
    string SchemaVersion,
    string Command,
    ProjectSummaryDto Project,
    DiagnosticSummaryDto Summary,
    IReadOnlyList<ProjectDiagnosticDto> Diagnostics);
```

## Handler

依赖：

- `ProjectCommandContextFactory`
- `IProjectDiagnosticEngine`
- `IOutputWriter`

执行步骤：

1. 创建项目分析上下文。
2. 根据 `Rules` 和 `NoUsageScan` 选择规则。
3. 执行 diagnostic engine。
4. 按 minimum severity 过滤。
5. 计算 summary。
6. 存在 error 或 `FailOnWarning` 命中时返回诊断失败结果。

## 诊断规则

- `PackageCompatibilityRule`
- `PackageConflictRule`
- `RegistrationRule`
- `XamlNamespaceRule`
- `AotReadinessRule`
- `DeprecatedApiRule`
- `GeneratedFileRule`

## 错误

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | severity 或 rule 非法。 |
| `ATOMUICLI_PRJ001` | 未找到项目。 |
| `ATOMUICLI_PRJ002` | 项目文件无法读取。 |
| `ATOMUICLI_DATA001` | metadata 不可用。 |
| `ATOMUICLI_PKG003` | 包冲突或兼容性 error。 |
| `ATOMUICLI_PRJ010` | 项目结构或 XAML 诊断 error。 |
| `ATOMUICLI_AOT001` | AOT 诊断 error。 |

## 测试

- 包冲突产生 error 和退出码 `5`。
- 注册缺失能给出注册方法建议。
- `--rule` 只执行指定规则。
- `--no-usage-scan` 不读取 XAML/C#。
- `--fail-on-warning` 将 warning 映射为失败。
