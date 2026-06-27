# AtomUICliProjectAnalysisModule 详细设计

## 目标

`AtomUICliProjectAnalysisModule` 负责把用户传入的目录、解决方案或项目文件转换为项目模型，并基于 metadata 执行诊断、使用统计、lint 和迁移规划。模块保持只读，不执行 restore、build、publish，也不加载用户程序集。

## 项目与命名空间

| 项目 | 命名空间 | 说明 |
| --- | --- | --- |
| `src/AtomUI.Cli.ProjectAnalysis` | `AtomUI.Cli.ProjectAnalysis` | 模块类和服务注册。 |
| `src/AtomUI.Cli.ProjectAnalysis` | `AtomUI.Cli.ProjectAnalysis.Projects` | 路径解析、项目读取、包引用读取。 |
| `src/AtomUI.Cli.ProjectAnalysis` | `AtomUI.Cli.ProjectAnalysis.Scanning` | XAML/C# 扫描。 |
| `src/AtomUI.Cli.ProjectAnalysis` | `AtomUI.Cli.ProjectAnalysis.Diagnostics` | 诊断规则和诊断引擎。 |
| `src/AtomUI.Cli.ProjectAnalysis` | `AtomUI.Cli.ProjectAnalysis.Migrations` | 迁移计划。 |

## 项目模型

```csharp
public sealed record ProjectAnalysisContext(
    ProjectInput Input,
    IReadOnlyList<ProjectDescriptor> Projects,
    PackageReferenceSet Packages,
    SourceFileSet Sources,
    AtomUIMetadataContext Metadata);
```

输入解析规则：

- 目录：优先 `.slnx`，其次 `.sln`，最后单个 `.csproj`。
- `.slnx` 或 `.sln`：读取其中项目路径，不执行 MSBuild。
- `.csproj`：作为单项目分析。
- 多项目解决方案保留每个项目的相对路径和 TargetFramework。
- solution 中声明但磁盘不存在的项目返回 warning，不阻断其他项目分析。

跳过目录由 `ProjectAnalysisFileSystemOptions` 固定，并允许测试覆盖。

## 文件读取

| 类型 | 职责 |
| --- | --- |
| `IProjectPathResolver` | 把输入 path 解析为项目集合。 |
| `IProjectFileReader` | 使用 XML reader 读取 `.csproj` 和 props。 |
| `IPackageReferenceReader` | 合并 PackageReference、PackageVersion 和 lock file。 |
| `ISourceFileEnumerator` | 枚举 XAML/C#，跳过输出和 generated 目录。 |

读取失败策略：

- 单项目读取失败：返回项目级 error。
- 解决方案中部分项目失败：输出 warning，并继续分析可读项目。
- XML 非法：返回 `ATOMUICLI_PRJ002`。

## 扫描器设计

| 扫描器 | 输入 | 输出 |
| --- | --- | --- |
| `IXamlUsageScanner` | `.axaml`、`.xaml` | `ControlUsage`、namespace、line/column。 |
| `ICSharpRegistrationScanner` | `.cs` | 注册方法调用、builder 链、using namespace。 |
| `ICSharpUsageScanner` | `.cs` | `new Control()`、generic references、attached property 字符串风险。 |

扫描器要求：

- 不需要语义模型。
- C# 优先使用 Roslyn syntax tree；不可用场景可降级为保守文本扫描。
- XAML 使用 XML reader 或轻量 parser，保留行号。
- 扫描结果必须包含 source file hash，便于未来增量分析。
- 对 AtomUI Gallery 风格的 `ShowCaseItem`、`ShowCasePanel`、`DeferredContentTemplate` 仅作为普通 XAML 结构读取，不执行控件初始化。

## 诊断规则

```csharp
public interface IProjectDiagnosticRule
{
    string Id { get; }

    ValueTask<IReadOnlyList<ProjectDiagnostic>> AnalyzeAsync(
        ProjectAnalysisContext context,
        CancellationToken cancellationToken);
}
```

首批规则：

| 规则 | 输入 | 输出 |
| --- | --- | --- |
| `PackageCompatibilityRule` | TargetFramework、包版本、metadata 版本 | 版本兼容 error/warning。 |
| `PackageConflictRule` | 包清单、产品冲突关系 | 冲突和替代建议。 |
| `RegistrationRule` | 包引用、注册扫描结果 | 缺失注册或重复注册。 |
| `XamlNamespaceRule` | XAML namespace、控件 metadata | 错误命名空间和无法识别控件。 |
| `AotReadinessRule` | publish 属性、源码扫描 | trimming/AOT 风险。 |
| `DeprecatedApiRule` | 控件使用、metadata deprecated 信息 | 废弃 API。 |
| `GeneratedFileRule` | 文件路径和目录 | generated 文件修改风险。 |

规则通过 diagnostic contribution catalog 显式注册。

## 命令贡献

```csharp
context.Commands.Add<EnvCommandOptions, EnvCommandHandler>("env");
context.Commands.Add<DoctorCommandOptions, DoctorCommandHandler>("doctor");
context.Commands.Add<UsageCommandOptions, UsageCommandHandler>("usage");
context.Commands.Add<LintCommandOptions, LintCommandHandler>("lint");
context.Commands.Add<MigrateCommandOptions, MigrateCommandHandler>("migrate");
```

`env`、`usage`、`doctor`、`lint` 共享 `ProjectAnalysisContextFactory`。`migrate` 在无项目路径时只使用 metadata migration guides。

## AOT-first 实现要求

- 不使用 MSBuild workspace。
- 不加载用户项目输出程序集。
- Roslyn syntax API 作为包依赖时必须验证 Native AOT analyzer。
- 诊断规则显式注册，不动态加载 analyzer。
- 输出 DTO 纳入 JSON source generation。

## 测试设计

| 测试文件 | 覆盖点 |
| --- | --- |
| `ProjectPathResolverTests` | 目录、`.slnx`、`.sln`、`.csproj` 输入。 |
| `PackageReferenceReaderTests` | Central Package Management 和 lock file。 |
| `XamlUsageScannerTests` | 命名空间、控件使用、行号。 |
| `CSharpRegistrationScannerTests` | 注册方法调用和 builder 链。 |
| `ProjectDiagnosticEngineTests` | 规则过滤、severity 聚合、退出码触发。 |
| `MigrationPlanBuilderTests` | 版本范围和项目相关迁移步骤。 |

fixture 项目放在 `tests/AtomUI.Cli.ProjectAnalysis.Tests/Fixtures/projects`，不得执行 build。
