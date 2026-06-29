# AtomUICliProjectAnalysisModule 设计

## 定位

`AtomUICliProjectAnalysisModule` 属于 `AtomUI.Cli.ProjectAnalysis`。它负责读取用户项目文件、扫描 XAML/C#、分析包引用、注册入口、控件使用、AOT 风险和迁移问题。

## 模块声明

```csharp
[Module(
    DisplayName = "AtomUI Cli Project Analysis",
    Dependencies = [typeof(AtomUICliMetadataModule)])]
public sealed partial class AtomUICliProjectAnalysisModule : AtomUICliModule
{
}
```

## 依赖

| 依赖 | 原因 |
| --- | --- |
| `AtomUICliMetadataModule` | 诊断规则需要产品清单、包关系、控件元数据和版本信息。 |

## 注册服务

| 服务 | 生命周期 | 说明 |
| --- | --- | --- |
| `IProjectPathResolver` | Scoped | 解析目录、`.sln`、`.slnx`、`.csproj`。 |
| `IProjectFileReader` | Scoped | 读取项目和 Central Package Management 文件。 |
| `IPackageReferenceReader` | Scoped | 读取包引用、版本和 lock 文件。 |
| `IXamlUsageScanner` | Scoped | 扫描 `.axaml` / `.xaml` 控件使用。 |
| `ICSharpRegistrationScanner` | Scoped | 扫描注册方法调用。 |
| `IProjectDiagnosticEngine` | Scoped | 执行诊断规则并聚合结果。 |
| `IMigrationPlanBuilder` | Scoped | 构建版本迁移清单。 |

## 贡献命令

| 命令 | 文档 | 阶段 |
| --- | --- | --- |
| `env` | [commands/env](../../commands/env/overview.md) | P1 |
| `doctor` | [commands/doctor](../../commands/doctor/overview.md) | P1 |
| `usage` | [commands/usage](../../commands/usage/overview.md) | P1 |
| `lint` | [commands/lint](../../commands/lint/overview.md) | P1 |
| `migrate` | [commands/migrate](../../commands/migrate/overview.md) | P2 |

## 贡献诊断规则

- `PackageCompatibilityRule`
- `PackageConflictRule`
- `RegistrationRule`
- `XamlNamespaceRule`
- `AotReadinessRule`
- `DeprecatedApiRule`
- `GeneratedFileRule`

## 输入边界

扫描文件：

- `.sln`、`.slnx`
- `.csproj`
- `Directory.Build.props`
- `Directory.Packages.props`
- `packages.lock.json`
- `.axaml`、`.xaml`
- `.cs`
- publish profile 和 app manifest

跳过目录：

- `bin/`
- `obj/`
- `output/`
- `.git/`
- `.idea/`
- `.vs/`
- `.referenceprojects/`
- `GeneratedFiles/`

## AOT 约束

- 不加载用户项目程序集。
- 不调用 MSBuild 执行用户 target。
- XAML/C# 扫描使用文本或 Roslyn syntax API，不反射类型。
- 诊断规则通过 contribution catalog 显式注册。

## 测试

- `.slnx`、`.sln`、目录和 `.csproj` 输入解析。
- 包冲突、注册缺失、XAML namespace、AOT 风险诊断。
- generated files 跳过规则。
- JSON 诊断输出稳定性。

## 相关文档

- [详细设计](design.md)
- [模块设计索引](../overview.md)
- [整体架构设计](../../architecture/atomui-cli-architecture-design.md)
- [项目分析命令共享设计](../../commands/project-analysis-design.md)
