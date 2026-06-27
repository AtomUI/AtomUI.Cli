# AtomUICliSetupModule 设计

## 定位

`AtomUICliSetupModule` 属于 `AtomUI.Cli.Hosting`。它负责写入类工作流，包括 MCP/Skill 配置、项目初始化检查、包添加和升级建议。

## 模块声明

```csharp
[Module(
    DisplayName = "AtomUI Cli Setup",
    Dependencies =
    [
        typeof(AtomUICliCoreModule),
        typeof(AtomUICliMetadataModule),
        typeof(AtomUICliProjectAnalysisModule)
    ])]
public sealed partial class AtomUICliSetupModule : AtomUICliModule
{
}
```

## 依赖

| 依赖 | 原因 |
| --- | --- |
| `AtomUICliCoreModule` | 输出、错误、命令调度和 dry-run/write 边界。 |
| `AtomUICliMetadataModule` | 包和注册建议。 |
| `AtomUICliProjectAnalysisModule` | 初始化和升级前项目诊断。 |

## 注册服务

| 服务 | 生命周期 | 说明 |
| --- | --- | --- |
| `ISetupPlanBuilder` | Scoped | 构建配置写入计划。 |
| `ISetupWriter` | Scoped | 执行 `--write` 写入。 |
| `IProjectInitAdvisor` | Scoped | 生成项目初始化建议。 |
| `IPackageAddPlanner` | Scoped | 生成包添加命令和冲突检查。 |
| `IUpgradeAdvisor` | Scoped | CLI 或 AtomUI 包升级建议。 |

## 贡献命令

| 命令 | 文档 | 阶段 |
| --- | --- | --- |
| `setup` | [commands/setup](../../commands/setup/overview.md) | P1/P2 |
| `init` | [commands/init](../../commands/init/overview.md) | P2 |
| `add` | [commands/add](../../commands/add/overview.md) | P2 |
| `upgrade` | [commands/upgrade](../../commands/upgrade/overview.md) | P2 |

## 写入策略

- 所有写入命令默认 dry-run。
- 真实写入必须显式传入 `--write`。
- 写入前必须输出计划摘要。
- 写入失败必须返回结构化错误，不允许部分成功被报告为成功。
- 对用户项目文件的修改必须能在 dry-run 中完整预览。

## AOT 约束

- 不通过 shell 扫描外部工具能力。
- 写入模板使用内置资源或显式数据快照。
- 配置文件解析使用结构化 parser，避免 ad hoc 字符串拼接。

## 测试

- dry-run 不写文件。
- `--write` 只写声明目标。
- 已存在配置的合并策略。
- 包冲突时阻止写入。
- JSON 输出包含写入计划和结果。
