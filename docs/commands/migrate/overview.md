# dotnet atomui migrate 设计

## 定位

`migrate` 是 P2 只读迁移规划命令，用于从一个 AtomUI 版本迁移到另一个版本时输出破坏性变更、包替代关系、API 变更、Token 变更和 Agent 可执行的迁移提示。

## 所属模块

`AtomUICliProjectAnalysisModule`

## 调用形式

```bash
dotnet atomui migrate 6.0.0 6.0.6
dotnet atomui migrate 6.0.0 6.0.6 ./src/MyApp --format markdown
dotnet atomui migrate --from 6.0.0 --to 6.0.6 --project ./MyApp.csproj --format json
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `from` | 空 | 源版本。可用 `--from` 替代。 |
| `to` | 空 | 目标版本。可用 `--to` 替代。 |
| `path` | 空 | 可选项目路径。传入后结合项目实际使用生成迁移计划。 |
| `--from <version>` | 空 | 源版本。 |
| `--to <version>` | 空 | 目标版本。 |
| `--project <path>` | 空 | 项目路径。 |
| `--include-agent-prompts` | `true` | 输出 Agent 迁移提示。 |
| `--format <text|json|markdown>` | `markdown` | 默认输出 Markdown。 |

## 输入

- 版本索引和 migration guides。
- changelog breaking entries。
- 控件、Token、semantic parts 和包替代关系。
- 可选项目扫描结果。

## 输出

json 输出示例：

```json
{
  "from": "6.0.0",
  "to": "6.0.6",
  "steps": [
    {
      "kind": "package",
      "severity": "warning",
      "title": "Update package references",
      "actions": ["Update AtomUI packages to 6.0.6"]
    }
  ],
  "agentPrompts": [
    "Run dotnet atomui doctor after applying package changes."
  ]
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `MigrateCommandOptions` | Scoped value | 版本范围、项目路径和输出选项。 |
| `MigrateCommandHandler` | Transient | 构建迁移计划。 |
| `IMigrationPlanBuilder` | Scoped | 迁移步骤生成。 |
| `IChangelogQueryService` | Singleton | breaking change 查询。 |
| `IPackageQueryService` | Singleton | 包替代和冲突关系。 |
| `IProjectDiagnosticEngine` | Scoped | 可选项目上下文诊断。 |
| `IVersionIndexLoader` | Singleton | 版本合法性校验。 |

## 执行流程

1. 解析 `from` 和 `to`，位置参数和命名参数不能冲突。
2. 校验版本存在且方向合法。
3. 查询版本范围内的迁移数据。
4. 如果传入项目路径，扫描项目实际包和控件使用。
5. 生成迁移步骤、风险和验证命令。
6. 输出 markdown、text 或 json。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | `2` | 缺少 from 或 to。 |
| `ATOMUICLI_ARG002` | `2` | 版本参数冲突或方向非法。 |
| `ATOMUICLI_DATA004` | `4` | 版本索引缺失。 |
| `ATOMUICLI_PRJ001` | `3` | 指定项目不存在。 |

## AOT 约束

- 迁移规则来自 metadata 快照和显式规则 catalog。
- 不执行项目写入。
- 不加载旧版本或新版本程序集做 API diff。

## 测试点

- 位置参数和命名参数解析一致。
- from/to 缺失时返回参数错误。
- 版本范围内 breaking change 进入迁移步骤。
- 传入项目路径后只输出项目相关步骤。
- markdown 输出包含验证命令。

