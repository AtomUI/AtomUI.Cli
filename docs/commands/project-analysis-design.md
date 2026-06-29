# 项目分析类命令共享设计

项目分析类命令由 `AtomUICliProjectAnalysisModule` 贡献，默认只读，不执行 restore、build、publish，不加载用户程序集。它们把用户输入路径转换成 `ProjectAnalysisContext`，再运行统计或诊断服务。

适用命令：

- `env`
- `doctor`
- `usage`
- `lint`
- `migrate`

## 共享执行管线

```text
ProjectCommandOptions
  -> ProjectInputResolver
  -> ProjectPathResolver
  -> ProjectFileReader
  -> PackageReferenceReader
  -> SourceFileEnumerator
  -> XamlUsageScanner
  -> CSharpRegistrationScanner
  -> ProjectAnalysisContext
  -> DiagnosticRuleCatalog / UsageAggregator / MigrationPlanner
  -> PayloadBuilder
  -> OutputWriter
```

命令 handler 不直接读取项目文件。文件读取、扫描、诊断规则执行和迁移规划都在 Project Analysis 领域服务中完成。

## ProjectAnalysisContext

```csharp
public sealed record ProjectAnalysisContext(
    ProjectInput Input,
    IReadOnlyList<ProjectDescriptor> Projects,
    PackageReferenceSet Packages,
    SourceFileSet Sources,
    XamlUsageIndex XamlUsage,
    CSharpRegistrationIndex Registrations,
    PublishOptionSet PublishOptions,
    MetadataCommandContext Metadata);
```

输入解析规则：

| 输入 | 行为 |
| --- | --- |
| 目录 | 优先 `.slnx`，其次 `.sln`，最后单个 `.csproj`。 |
| `.slnx` / `.sln` | 读取项目路径，不执行 MSBuild。 |
| `.csproj` | 作为单项目分析。 |
| 不存在路径 | `ATOMUICLI_PRJ001`，退出码 `4`。 |
| XML 非法 | `ATOMUICLI_PRJ002`，退出码 `4`。 |

## 项目模型 DTO

```csharp
public sealed record ProjectDescriptor(
    string ProjectPath,
    string? TargetFramework,
    IReadOnlyList<string> TargetFrameworks,
    IReadOnlyList<PackageReferenceDto> PackageReferences,
    IReadOnlyList<string> SourceFiles,
    IReadOnlyList<string> XamlFiles);

public sealed record PackageReferenceSet(
    IReadOnlyList<PackageReferenceDto> DirectReferences,
    IReadOnlyList<PackageVersionDto> CentralVersions,
    IReadOnlyList<PackageLockDto> LockEntries);

public sealed record ControlUsage(
    string ControlName,
    string SourceFile,
    int Line,
    int Column,
    string UsageKind,
    string? ProductId);

public sealed record RegistrationUsage(
    string MethodName,
    string SourceFile,
    int Line,
    string BuilderExpression);
```

所有 DTO 必须使用相对路径输出，除非用户输入的是绝对路径且命令声明输出绝对路径。

## 诊断规则贡献

诊断规则必须通过 diagnostic contribution catalog 注册。

```csharp
public sealed record DiagnosticRuleDescriptor(
    string RuleId,
    string Category,
    AtomUICliSeverity DefaultSeverity,
    IReadOnlySet<string> ApplicableCommands,
    IReadOnlySet<string> RequiredIndexes,
    string RelatedErrorCode);
```

规则执行结果：

```csharp
public sealed record ProjectDiagnosticDto(
    string Code,
    AtomUICliSeverity Severity,
    string Message,
    string? Project,
    string? File,
    int? Line,
    int? Column,
    string? SuggestedFix,
    IReadOnlyList<string> RelatedSymbols);
```

## 共享规则

| 规则 | 输入 | 输出 |
| --- | --- | --- |
| PackageCompatibilityRule | target framework、package version、metadata version | 兼容性 error/warning。 |
| PackageConflictRule | package set、product catalog conflict | 包冲突和替代建议。 |
| RegistrationRule | package references、registration usages | 缺失或重复注册。 |
| XamlNamespaceRule | XAML namespace、control usage、metadata | 命名空间错误和未知控件。 |
| AotReadinessRule | publish options、source scan | trimming/AOT 风险。 |
| DeprecatedApiRule | usage index、metadata deprecated 信息 | 废弃 API。 |
| GeneratedFileRule | source file path | generated file 修改风险。 |

## 扫描边界

- XAML 使用 XML reader 或轻量 parser，保留 line/column。
- C# 优先使用 Roslyn syntax tree；不能使用 MSBuild workspace。
- 不执行用户代码。
- 不读取 `bin/`、`obj/`、`output/`、`.git/`、`.vs/`、`.idea/`。
- 扫描失败单文件产生 warning，不阻断其他文件，除非项目文件本身不可读。

## 退出码聚合

| 场景 | 退出码 |
| --- | --- |
| 读取成功且无失败诊断 | `0` |
| 项目或源码不可读取 | `4` |
| 存在 error 级诊断 | `5` |
| `--fail-on-warning` 且存在 warning | `5` |
| 参数错误 | `2` |

## AOT-first 约束

- 不使用 MSBuild workspace。
- 不加载用户项目输出程序集。
- 诊断规则显式注册。
- 输出 DTO 纳入 source generated JSON context。
- 文件扫描器不能依赖动态代码生成。

## 共享测试基线

每个项目分析命令至少覆盖：

- 目录、`.slnx`、`.sln`、`.csproj` 输入。
- 不存在路径。
- XML 非法。
- Central Package Management。
- XAML line/column。
- C# 注册链扫描。
- `--format json` schema。
- cancellation 传播。
