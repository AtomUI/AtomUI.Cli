# dotnet atomui setup 设计

## 定位

`setup` 是 P1/P2 集成命令，用于为本机 Agent 和编辑器生成 MCP/Skill 配置计划，并在显式 `--write` 时写入配置文件。它不修改用户项目源码。

## 所属模块

`AtomUICliSetupModule`

## 调用形式

```bash
dotnet atomui setup
dotnet atomui setup --target codex
dotnet atomui setup --target vscode --write
dotnet atomui setup --target cursor --format json
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `--target <codex|claude|cursor|vscode|all>` | `all` | 配置目标。 |
| `--scope <user|workspace>` | `user` | 配置范围。 |
| `--workspace <path>` | 当前目录 | workspace 配置路径。 |
| `--write` | `false` | 执行写入。未传入时只输出计划。 |
| `--force` | `false` | 覆盖冲突配置。仅 `--write` 时有效。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 当前 CLI 可执行路径。
- 内置 MCP server 配置模板。
- 内置 Skill 模板。
- 目标工具配置文件路径和现有配置。

## 输出

dry-run json 输出示例：

```json
{
  "write": false,
  "targets": [
    {
      "target": "codex",
      "scope": "user",
      "actions": [
        {
          "kind": "upsertMcpServer",
          "path": "/home/user/.codex/config.json",
          "serverName": "atomui",
          "command": "dotnet",
          "args": ["atomui", "mcp"]
        }
      ]
    }
  ]
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `SetupCommandOptions` | Scoped value | target、scope、workspace 和写入选项。 |
| `SetupCommandHandler` | Transient | 构建计划并可选写入。 |
| `ISetupPlanBuilder` | Scoped | 生成配置计划。 |
| `ISetupWriter` | Scoped | 执行结构化写入。 |
| `IEnvironmentInfoProvider` | Singleton | 用户目录和 OS 路径规则。 |

## 执行流程

1. 校验 target、scope 和 workspace。
2. 探测目标配置文件是否存在。
3. 使用结构化 parser 读取现有配置。
4. 生成 upsert 计划和冲突列表。
5. 未传入 `--write` 时只输出计划。
6. 传入 `--write` 时执行写入，写入前再次校验冲突。
7. 输出写入结果或失败原因。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | target 或 scope 非法。 |
| `ATOMUICLI_SETUP001` | `6` | 配置文件读取失败。 |
| `ATOMUICLI_SETUP002` | `6` | 存在冲突且未传入 `--force`。 |
| `ATOMUICLI_SETUP004` | `6` | 写入失败。 |

## AOT 约束

- 配置模板编译进工具包或 metadata 快照。
- 配置读写使用 JSON 或目标格式的结构化 parser。
- 不通过 shell 调用外部工具探测能力。

## 测试点

- 默认 dry-run 不写文件。
- `--target codex` 只生成对应配置计划。
- `--write` 写入前后 JSON 结构合法。
- 冲突配置无 `--force` 时阻止写入。
- workspace scope 使用传入 workspace 路径。

## 相关文档

- [详细设计](design.md)
- [命令设计标准](../command-design-standard.md)
- [集成与写入命令共享设计](../integration-write-design.md)
- [错误码标准](../error-code-standard.md)
