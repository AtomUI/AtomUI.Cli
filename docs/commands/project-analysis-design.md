# Project Analysis Commands 详细设计

## 范围

本文档细化项目分析命令：

- `env`
- `doctor`
- `usage`
- `lint`
- `migrate`

这些命令默认只读。它们可以读取项目文件和源码，但不执行 restore/build/publish，不加载用户程序集。

## 共享上下文

```csharp
public sealed record ProjectCommandContext(
    ProjectInput Input,
    ProjectAnalysisContext Analysis,
    TargetVersionInfo TargetVersion,
    GlobalCliOptions Global);
```

`ProjectCommandContextFactory` 负责：

1. 解析 path。
2. 读取项目和包引用。
3. 枚举源码文件。
4. 加载 metadata。
5. 创建不可变分析上下文。

## 文件范围

读取：

- `.slnx`
- `.sln`
- `.csproj`
- `Directory.Build.props`
- `Directory.Packages.props`
- `packages.lock.json`
- `.axaml`
- `.xaml`
- `.cs`

跳过：

- `bin/`
- `obj/`
- `output/`
- `outputs/`
- `.git/`
- `.idea/`
- `.vs/`
- `.referenceprojects/`
- `GeneratedFiles/`

## env

DTO：

```csharp
public sealed record EnvCommandPayload(
    EnvironmentDto Environment,
    IReadOnlyList<ProjectEnvironmentDto> Projects,
    IReadOnlyList<PackageReferenceDto> Packages,
    IReadOnlyList<ProjectFileDto> Files);
```

实现重点：

- SDK 信息来自当前进程和 `global.json`。
- 包版本来自 project/props/lock file。
- `--detail` 输出 RID、publish 属性和 AOT/trimming 属性。

## doctor

DTO：

```csharp
public sealed record DoctorCommandPayload(
    ProjectSummaryDto Project,
    DiagnosticSummaryDto Summary,
    IReadOnlyList<ProjectDiagnosticDto> Diagnostics);
```

诊断执行：

- 使用 diagnostic rule catalog。
- `--rule` 只执行指定规则。
- `--no-usage-scan` 跳过 XAML/C# usage scanner。
- error 触发退出码 `5`。
- `--fail-on-warning` 使 warning 触发退出码 `5`。

## usage

DTO：

```csharp
public sealed record UsageCommandPayload(
    ProjectSummaryDto Project,
    string GroupBy,
    IReadOnlyList<ControlUsageDto> Items,
    IReadOnlyList<PackageUsageDto> UnusedPackages);
```

扫描规则：

- XAML 控件识别基于 namespace 和 element name。
- C# 使用识别基于 syntax tree 中的 object creation、generic name 和 static member。
- Gallery 风格示例中的控件也按普通 XAML 处理。
- `--include-locations` 输出 file、line、column。

## lint

DTO：

```csharp
public sealed record LintCommandPayload(
    ProjectSummaryDto Project,
    DiagnosticSummaryDto Summary,
    IReadOnlyList<ProjectDiagnosticDto> Findings,
    FixPlanDto? FixPlan);
```

lint 和 doctor 区别：

- `doctor` 面向项目能否正常工作。
- `lint` 面向可维护性、废弃 API、样式定制风险和最佳实践。
- `lint` 只输出 fix plan，不写文件。

## migrate

DTO：

```csharp
public sealed record MigrateCommandPayload(
    string From,
    string To,
    IReadOnlyList<MigrationStepDto> Steps,
    IReadOnlyList<string> VerificationCommands,
    IReadOnlyList<string> AgentPrompts);
```

迁移数据来源：

- metadata changelog。
- migration guides。
- 快照 API diff。
- 可选项目实际使用统计。

如果传入项目路径，迁移计划只保留与项目包和控件使用相关的步骤。

## 错误处理

| 场景 | 错误码 |
| --- | --- |
| path 不存在 | `ATOMUICLI_PRJ001` |
| XML 读取失败 | `ATOMUICLI_PRJ002` |
| 源码读取失败 | `ATOMUICLI_PRJ003` |
| 诊断 error | 具体 `ATOMUICLI_PKG`、`ATOMUICLI_AOT`、`ATOMUICLI_PRJ` |
| 版本范围非法 | `ATOMUICLI_ARG002` |
| 版本索引缺失 | `ATOMUICLI_DATA004` |

## AOT-first 要求

- 不使用 MSBuild workspace。
- 不加载用户项目程序集。
- Roslyn 只用于 syntax tree。
- 诊断规则显式注册。
- JSON DTO 纳入 source generated context。

## 测试矩阵

| 命令 | 必测场景 |
| --- | --- |
| `env` | `.slnx`、Central Package Management、AOT 属性。 |
| `doctor` | 包冲突、注册缺失、AOT error、rule filter。 |
| `usage` | XAML/C# 控件识别、group-by、locations。 |
| `lint` | category filter、fix plan、只读保证。 |
| `migrate` | from/to 解析、diff、项目相关裁剪。 |

