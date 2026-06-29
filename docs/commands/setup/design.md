# dotnet atomui setup 详细设计

## 1. 命令定位

`setup` 生成或写入 Agent/编辑器的 AtomUI MCP 配置。默认 dry-run，只输出配置计划；传入 `--write` 才修改文件。

## 2. 所属模块与注册

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliSetupModule` |
| 注册阶段 | `ConfigureAtomUICliCommands` |
| 分组 | write |
| 读写 | `isReadOnly=false` |
| 项目上下文 | `requiresProject=false` |
| 写入确认 | `requiresWriteConfirmation=true` |
| Handler 生命周期 | Transient |
| 支持格式 | text、json、markdown |

## 3. 调用语法

```bash
dotnet atomui setup --target codex
dotnet atomui setup --target vscode --scope workspace --workspace ./repo --format json
dotnet atomui setup --target codex --write
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `--target` | enum | all | 不支持返回 `ATOMUICLI_ARG002` | 配置目标：`codex`、`claude`、`cursor`、`vscode`、`all`；`--client` 是兼容别名。 |
| `--scope` | enum | user | 非法返回 `ATOMUICLI_ARG002` | user、workspace。 |
| `--workspace` | path | 当前目录 | workspace scope 下不存在返回 `ATOMUICLI_PRJ001` | workspace 配置路径。 |
| `--write` | bool | false | 无 | 执行写入。 |
| `--force` | bool | false | 不能绕过 blocking conflict | 允许覆盖非阻断差异。 |

## 5. Options 类型

```csharp
public sealed record SetupCommandOptions(
    GlobalCliOptions Global,
    SetupTarget Target,
    SetupScope Scope,
    string Workspace,
    bool Write,
    bool Force) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IWritePlanBuilder<SetupCommandOptions>`
- `IWriteConflictChecker`
- `IWriteExecutor`
- `IOutputWriter`

## 7. 执行流程

1. 解析 target/scope/workspace。
2. 读取当前配置。
3. 生成 MCP server desired state。
4. 构建 write plan。
5. 检查 conflicts。
6. 无 `--write` 时输出 dry-run plan。
7. 有 `--write` 且无 blocking conflict 时执行。
8. 输出 `WriteResult`。

## 8. 领域服务契约

```csharp
public interface ISetupPlanBuilder : IWritePlanBuilder<SetupCommandOptions>
{
}
```

结果分支：`PlanCreated`、`Conflict`、`ConfigUnreadable`、`WriteFailed`。

## 9. 输出模型

复用 `WritePlan` 和 `WriteResult`。setup operation 包括 `JsonConfigOperation` 和 `TextFileOperation`。

## 10. 输出格式

- text：输出计划摘要、目标文件、是否写入、冲突。
- json：输出完整 write plan/result。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | target/scope 非法 | `2` |
| `ATOMUICLI_PRJ001` | workspace scope 下 workspace 不存在 | `4` |
| `ATOMUICLI_SETUP002` | blocking conflict | `6` |
| `ATOMUICLI_SETUP003` | 配置读取失败 | `6` |
| `ATOMUICLI_SETUP004` | 写入失败 | `6` |

## 12. AOT-first 约束

客户端配置 schema 静态定义。write plan DTO 纳入 JSON context。不调用外部命令探测客户端。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| dry-run | `setup --target codex` | 不修改文件。 |
| write | `--write` | 写入目标配置。 |
| conflict | 已存在冲突配置 | `ATOMUICLI_SETUP002`。 |
| json | `--format json` | write plan 稳定。 |
| invalid target | `--target bad` | `ATOMUICLI_ARG002`。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Hosting/Setup/SetupCommandOptions.cs`
- `src/AtomUI.Cli.Hosting/Setup/SetupCommandHandler.cs`
- `src/AtomUI.Cli.Hosting/Setup/SetupPlanBuilder.cs`
