# dotnet atomui setup 详细设计

## 目标

`setup` 为 Agent 和编辑器生成 MCP/Skill 配置计划，并在显式 `--write` 时写入配置。它不修改用户项目源码。

## Options

```csharp
public sealed record SetupCommandOptions(
    GlobalCliOptions Global,
    SetupTarget Target,
    SetupScope Scope,
    string Workspace,
    bool Write,
    bool Force) : IAtomUICliCommandOptions;

public enum SetupTarget
{
    Codex,
    Claude,
    Cursor,
    Vscode,
    All
}

public enum SetupScope
{
    User,
    Workspace
}
```

## Payload

```csharp
public sealed record SetupCommandPayload(
    string SchemaVersion,
    string Command,
    bool Write,
    IReadOnlyList<SetupTargetPlanDto> Targets);
```

## Handler

依赖：

- `ISetupPlanBuilder`
- `ISetupWriter`
- `IEnvironmentInfoProvider`
- `IOutputWriter`

执行步骤：

1. 校验 target、scope、workspace。
2. 计算目标配置文件路径。
3. 读取现有配置并生成 upsert plan。
4. 如果包含冲突且无 `--force`，返回写入冲突。
5. 未传 `--write` 时只输出 plan。
6. 传 `--write` 时执行 writer。
7. 输出结果和验证提示。

## 写入动作

- upsert MCP server。
- 写入或更新 Skill 文件。
- 写入 workspace instruction block。

## 错误

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | target 或 scope 非法。 |
| `ATOMUICLI_SETUP001` | 配置读取失败。 |
| `ATOMUICLI_SETUP002` | 冲突且未 force。 |
| `ATOMUICLI_SETUP003` | 写入失败。 |

## 测试

- 默认 dry-run 不写文件。
- workspace scope 写入 workspace 下配置。
- JSON upsert 保留未知字段。
- instruction marker 可重复更新。
- `--force` 只能覆盖可恢复冲突。

