# dotnet atomui lint 详细设计

## 1. 命令定位

`lint` 对 AtomUI 使用方式执行规则检查，重点关注废弃 API、错误包组合、XAML 命名空间、注册入口和可维护性问题。它只读，不自动修复。

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
dotnet atomui lint
dotnet atomui lint ./src/App --category xaml
dotnet atomui lint ./App.slnx --fix-plan markdown
dotnet atomui lint --severity warning --format json
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `path` | path | 当前目录 | 不存在返回 `ATOMUICLI_PRJ001` | 项目入口。 |
| `--category` | enum list | all | 非法返回 `ATOMUICLI_ARG002` | `package`、`registration`、`xaml`、`api`、`theme`、`aot`。 |
| `--rule` | string list | lint rules | 未知返回 `ATOMUICLI_ARG002` | 指定规则；比 category 更精确。 |
| `--severity` | enum | info | 非法返回 `ATOMUICLI_ARG002` | 最低输出级别；`--minimum-severity` 作为兼容别名。 |
| `--fix-plan` | enum | null | 非法返回 `ATOMUICLI_ARG002` | 额外输出修复计划，格式为 `text`、`markdown`、`json`，不写文件。 |
| `--fail-on-warning` | bool | false | 无 | warning 是否让命令返回 exit `5`。 |

## 5. Options 类型

```csharp
public sealed record LintCommandOptions(
    GlobalCliOptions Global,
    string Path,
    IReadOnlyList<LintCategory> Categories,
    IReadOnlyList<string> Rules,
    AtomUICliSeverity Severity,
    FixPlanFormat? FixPlan,
    bool FailOnWarning) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IProjectAnalysisContextFactory`
- `IProjectDiagnosticEngine`
- `IDiagnosticExitPolicy`
- `IOutputWriter`

## 7. 执行流程

1. 创建项目分析上下文。
2. 根据 category/rule 选择 lint 适用规则。
3. 执行诊断。
4. 过滤 severity。
5. 生成可选 fix plan。
6. 根据 fail-on-warning 计算退出码。
7. 输出 payload。

## 8. 领域服务契约

复用 `IProjectDiagnosticEngine`。`DiagnosticRunOptions.CommandKind=Lint`，默认规则集包含 deprecated、namespace、registration、package conflict 和 generated file。

## 9. 输出模型

```csharp
public sealed record LintCommandPayload(
    string SchemaVersion,
    string Command,
    ProjectSummaryDto Project,
    DiagnosticSummaryDto Summary,
    IReadOnlyList<ProjectDiagnosticDto> Findings,
    FixPlanDto? FixPlan,
    IReadOnlyList<WarningDto> Warnings);
```

## 10. 输出格式

- text：按 severity 和文件排序输出。
- json：输出完整 findings。
- markdown：输出 finding 表格；`--fix-plan markdown` 时追加修复计划。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | category/rule/severity/fix-plan 非法 | `2` |
| `ATOMUICLI_PRJ001` | 未找到项目 | `4` |
| `ATOMUICLI_PRJ002` | 项目文件无法读取 | `4` |
| `ATOMUICLI_PRJ010` | lint error 或 warning 失败阈值 | `5` |

## 12. AOT-first 约束

规则显式注册。不加载用户程序集。不执行自动修复。DTO 纳入 JSON context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| deprecated | 废弃 API fixture | finding。 |
| namespace | XAML namespace fixture | finding。 |
| fail warning | warning fixture | exit `5`。 |
| category filter | `--category xaml` | 只执行 XAML 规则。 |
| fix plan | `--fix-plan markdown` | 输出修复计划，不写文件。 |
| json | `--format json` | findings 稳定。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.ProjectAnalysis/Commands/LintCommandOptions.cs`
- `src/AtomUI.Cli.ProjectAnalysis/Commands/LintCommandHandler.cs`
