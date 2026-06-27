# dotnet atomui upgrade 设计

## 定位

`upgrade` 是 P2 升级辅助命令，用于输出 AtomUI Cli 自身和用户项目中 AtomUI 包的升级建议。它默认只读；未来在显式 `--write` 下可更新项目包版本，不执行在线包查询。

## 所属模块

`AtomUICliSetupModule`

## 调用形式

```bash
dotnet atomui upgrade
dotnet atomui upgrade ./src/MyApp
dotnet atomui upgrade ./MyApp.csproj --to 6.0.6 --format json
dotnet atomui upgrade ./MyApp.csproj --to 6.0.6 --write
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `path` | 当前目录 | 目录、`.sln`、`.slnx` 或 `.csproj`。 |
| `--to <version>` | metadata 默认版本 | 目标 AtomUI 版本。 |
| `--cli` | `false` | 只输出 CLI 工具升级建议。 |
| `--packages` | `true` | 输出项目包升级建议。 |
| `--write` | `false` | 执行包版本写入。未传入时只输出计划。 |
| `--force` | `false` | 存在冲突时强制写入可恢复修改。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 当前 CLI build info。
- metadata 版本索引。
- 项目包引用和 Central Package Management 文件。
- changelog 和 migration guides。

## 输出

json 输出示例：

```json
{
  "write": false,
  "targetVersion": "6.0.6",
  "cli": {
    "currentVersion": "0.1.0",
    "suggestedCommand": "dotnet tool update --global AtomUI.Cli"
  },
  "packages": [
    {
      "packageId": "AtomUI.Desktop.Controls",
      "currentVersion": "6.0.0",
      "targetVersion": "6.0.6",
      "action": "update"
    }
  ],
  "migration": {
    "requiresReview": true,
    "suggestedCommand": "dotnet atomui migrate 6.0.0 6.0.6"
  }
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `UpgradeCommandOptions` | Scoped value | 项目路径、目标版本和写入选项。 |
| `UpgradeCommandHandler` | Transient | 生成升级建议和可选写入。 |
| `IUpgradeAdvisor` | Scoped | CLI 和包升级建议。 |
| `IVersionIndexLoader` | Singleton | 目标版本校验。 |
| `IPackageReferenceReader` | Scoped | 当前包版本读取。 |
| `IMigrationPlanBuilder` | Scoped | 迁移风险摘要。 |
| `ISetupWriter` | Scoped | 包版本写入。 |

## 执行流程

1. 校验 `--to` 是否在版本索引中。
2. 如果传入 `--cli`，生成 CLI tool update 建议。
3. 如果启用 packages，读取项目包引用。
4. 生成包版本升级计划和冲突提示。
5. 查询迁移风险摘要。
6. dry-run 输出计划。
7. `--write` 时更新受控包版本文件。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | 目标版本非法。 |
| `ATOMUICLI_DATA004` | `4` | 版本索引缺失。 |
| `ATOMUICLI_PRJ001` | `3` | 未找到项目。 |
| `ATOMUICLI_PKG003` | `5` | 包冲突阻止升级。 |
| `ATOMUICLI_SETUP003` | `6` | 写入失败。 |

## AOT 约束

- 不联网查询最新包版本。
- 可升级版本来自随包 metadata 或显式数据根。
- 写入 Central Package Management 文件使用 XML parser。

## 测试点

- `--cli` 只输出 CLI 升级建议。
- 项目包版本低于目标版本时生成 update action。
- 目标版本不存在时返回 `ATOMUICLI_DATA004`。
- dry-run 不修改版本文件。
- `--write` 只更新 AtomUI 相关包版本。

