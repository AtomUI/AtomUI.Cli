# dotnet atomui lint 详细设计

## 目标

`lint` 检查项目可维护性和最佳实践问题，覆盖废弃 API、错误包组合、XAML 命名空间、主题 Token 使用、样式定制风险和 AOT 友好性。命令只读，只能输出修复计划。

## Options

```csharp
public sealed record LintCommandOptions(
    GlobalCliOptions Global,
    string Path,
    LintCategory? Category,
    DiagnosticSeverity MinimumSeverity,
    FixPlanFormat? FixPlan) : IAtomUICliCommandOptions;

public enum LintCategory
{
    Package,
    Registration,
    Xaml,
    Api,
    Theme,
    Aot
}
```

## Payload

```csharp
public sealed record LintCommandPayload(
    string SchemaVersion,
    string Command,
    ProjectSummaryDto Project,
    DiagnosticSummaryDto Summary,
    IReadOnlyList<ProjectDiagnosticDto> Findings,
    FixPlanDto? FixPlan);
```

## Handler

依赖：

- `ProjectCommandContextFactory`
- `IProjectDiagnosticEngine`
- `IMigrationPlanBuilder`
- `IOutputWriter`

执行步骤：

1. 创建项目上下文。
2. 根据 category 选择 lint rule set。
3. 执行规则并聚合 findings。
4. 按 severity 过滤。
5. 如果请求 fix plan，生成只读修复计划。
6. 输出 payload。

## Fix Plan

fix plan 包含：

- 文件路径。
- 建议变更摘要。
- 推荐命令。
- 人工确认点。

fix plan 不包含自动 patch，不写文件。

## 错误

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | category、severity 或 fix-plan 非法。 |
| `ATOMUICLI_PRJ001` | 未找到项目。 |
| `ATOMUICLI_PRJ003` | 源码读取失败。 |
| `ATOMUICLI_AOT001` | AOT 类 lint error。 |

## 测试

- category filter 只运行目标规则。
- fix plan markdown/json 均稳定。
- lint error 返回退出码 `5`。
- 命令不写用户项目。
- 废弃 API finding 包含文件和行号。

