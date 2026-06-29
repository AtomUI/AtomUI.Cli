# AtomUICliProjectAnalysisModule 详细设计

## 1. 模块定位

`AtomUICliProjectAnalysisModule` 负责把用户输入路径转换成项目分析模型，并基于 metadata 执行环境读取、使用统计、诊断、lint 和迁移规划。模块保持只读，不执行 restore、build、publish，不加载用户程序集。

## 2. 模块注册

| 字段 | 值 |
| --- | --- |
| 模块类型 | `AtomUICliProjectAnalysisModule` |
| 依赖 | `AtomUICliMetadataModule` |
| 命令贡献 | `env`、`doctor`、`usage`、`lint`、`migrate` |
| 诊断贡献 | package、registration、xaml、aot、deprecated、generated-file 规则 |
| 写入能力 | 无 |

## 3. ProjectAnalysisContext

```csharp
public sealed record ProjectAnalysisContext(
    ProjectInput Input,
    IReadOnlyList<ProjectDescriptor> Projects,
    PackageReferenceSet Packages,
    SourceFileSet Sources,
    XamlUsageIndex XamlUsage,
    CSharpUsageIndex CSharpUsage,
    CSharpRegistrationIndex Registrations,
    PublishOptionSet PublishOptions,
    MetadataCommandContext Metadata);
```

输入解析：

| 输入 | 行为 |
| --- | --- |
| 目录 | 优先 `.slnx`，其次 `.sln`，最后单个 `.csproj`。 |
| `.slnx` / `.sln` | 读取项目路径，不执行 MSBuild。 |
| `.csproj` | 单项目分析。 |
| 多项目解决方案 | 保留每个项目相对路径和 target frameworks。 |
| 部分项目缺失 | warning，不阻断其他项目。 |

## 4. 文件读取服务

```csharp
public interface IProjectPathResolver
{
    ValueTask<ProjectInputResolutionResult> ResolveAsync(ProjectInput input, CancellationToken cancellationToken);
}

public interface IProjectFileReader
{
    ValueTask<ProjectDescriptorResult> ReadAsync(string projectPath, CancellationToken cancellationToken);
}

public interface IPackageReferenceReader
{
    ValueTask<PackageReferenceSet> ReadAsync(ProjectDescriptor project, CancellationToken cancellationToken);
}

public interface ISourceFileEnumerator
{
    ValueTask<SourceFileSet> EnumerateAsync(ProjectDescriptor project, CancellationToken cancellationToken);
}
```

读取失败：

- 输入路径不存在：`ATOMUICLI_PRJ001`。
- 项目 XML 非法：`ATOMUICLI_PRJ002`。
- 单个源码文件不可读：diagnostic warning。

## 5. 扫描器

| 扫描器 | 输入 | 输出 |
| --- | --- | --- |
| `IXamlUsageScanner` | `.axaml`、`.xaml` | `ControlUsage`、namespace、line、column。 |
| `ICSharpRegistrationScanner` | `.cs` | 注册方法调用、builder 链、using namespace。 |
| `ICSharpUsageScanner` | `.cs` | `new Control()`、generic reference、attached property 字符串风险。 |

扫描要求：

- XAML parser 必须保留 line/column。
- C# 可使用 Roslyn syntax tree，禁止使用 MSBuild workspace。
- 不进入 `bin/`、`obj/`、`output/`、`.git/`、`.vs/`、`.idea/`。
- 扫描结果必须带 source file hash，支持未来增量分析。

## 6. 诊断规则契约

```csharp
public interface IProjectDiagnosticRule
{
    DiagnosticRuleDescriptor Descriptor { get; }

    ValueTask<IReadOnlyList<ProjectDiagnosticDto>> AnalyzeAsync(
        ProjectAnalysisContext context,
        CancellationToken cancellationToken);
}
```

规则 descriptor：

| 字段 | 说明 |
| --- | --- |
| `RuleId` | 稳定规则 ID。 |
| `Category` | package、registration、xaml、aot、deprecated、generated-file。 |
| `DefaultSeverity` | 默认 severity。 |
| `RequiredIndexes` | 依赖的项目索引。 |
| `ApplicableCommands` | `doctor`、`lint`、`migrate` 等。 |
| `RelatedErrorCode` | 触发失败阈值时对应错误码。 |

## 7. 诊断引擎

```csharp
public interface IProjectDiagnosticEngine
{
    ValueTask<ProjectDiagnosticResult> AnalyzeAsync(
        ProjectAnalysisContext context,
        DiagnosticRunOptions options,
        CancellationToken cancellationToken);
}
```

执行顺序：

1. 根据 command 和 `--rule` 选择规则。
2. 检查规则依赖的 indexes 是否可用。
3. 按规则 ID 稳定排序执行。
4. 聚合 severity。
5. 根据 fail threshold 输出 result。

## 8. 命令贡献

```csharp
context.Add<EnvCommandOptions, EnvCommandHandler>("env", ...);
context.Add<DoctorCommandOptions, DoctorCommandHandler>("doctor", ...);
context.Add<UsageCommandOptions, UsageCommandHandler>("usage", ...);
context.Add<LintCommandOptions, LintCommandHandler>("lint", ...);
context.Add<MigrateCommandOptions, MigrateCommandHandler>("migrate", ...);
```

`env` 可跳过源码扫描；`doctor`、`usage`、`lint` 默认需要项目上下文；`migrate` 支持 metadata-only 模式。

## 9. DI 生命周期

| 服务 | 生命周期 | 原因 |
| --- | --- | --- |
| path/file readers | Singleton | 无请求状态。 |
| scanners | Singleton | 无请求状态，可并发。 |
| diagnostic rule descriptors | Singleton | 不可变。 |
| `ProjectAnalysisContext` | Scoped | 与一次命令或 MCP invocation 绑定。 |
| command handlers | Transient | 每次执行创建。 |

## 10. AOT-first 约束

- 不使用 MSBuild workspace。
- 不加载用户项目输出程序集。
- 诊断规则显式注册。
- DTO 纳入 source generated JSON context。
- Roslyn 使用必须通过 AOT analyzer 验证。

## 11. 测试矩阵

| 测试 | 覆盖 |
| --- | --- |
| path resolver | 目录、`.slnx`、`.sln`、`.csproj`。 |
| package reader | direct reference、central version、lock file。 |
| xaml scanner | namespace、控件、line/column。 |
| csharp scanner | builder 链和注册方法。 |
| diagnostic engine | rule filter、severity 聚合、退出码触发。 |
| migration planner | metadata-only 和 project-aware 模式。 |
