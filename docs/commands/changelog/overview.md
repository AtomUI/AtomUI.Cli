# dotnet atomui changelog 设计

## 定位

`changelog` 是 P0 只读知识查询命令，用于查询 AtomUI 版本变更、控件变更、包变更和迁移相关提示。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui changelog
dotnet atomui changelog 6.0.0..6.0.6
dotnet atomui changelog 6.0.0..6.0.6 Button
dotnet atomui changelog --control Button --format json
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `range` | 当前目标版本 | 版本或版本范围，格式为 `<from>..<to>`。 |
| `control` | 空 | 控件名称。 |
| `--control <name>` | 空 | 控件名称，和位置参数等价。 |
| `--package <id>` | 空 | 包 ID 过滤。 |
| `--severity <level>` | 全部 | 可选 `info`、`warning`、`breaking`。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 版本索引。
- 目标版本快照中的 changelog。
- 控件和包索引。

## 输出

json 输出示例：

```json
{
  "range": {
    "from": "6.0.0",
    "to": "6.0.6"
  },
  "entries": [
    {
      "version": "6.0.6",
      "kind": "control",
      "target": "Button",
      "severity": "info",
      "message": "Button token metadata updated."
    }
  ]
}
```

markdown 输出适合发布说明和迁移上下文。

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `ChangelogCommandOptions` | Scoped value | 版本范围、控件、包和 severity。 |
| `ChangelogCommandHandler` | Transient | 变更查询和输出映射。 |
| `IChangelogQueryService` | Singleton | 变更记录查询。 |
| `IControlQueryService` | Singleton | 控件名称解析。 |
| `IPackageQueryService` | Singleton | 包过滤解析。 |
| `IVersionIndexLoader` | Singleton | 版本范围解析。 |

## 执行流程

1. 解析 `range`，未传入时使用当前目标版本。
2. 校验版本存在且范围方向合法。
3. 解析 `control` 和 `--package` 过滤。
4. 查询 changelog entries。
5. 按版本倒序、severity、target 稳定排序。
6. 输出结果。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | 版本范围或 severity 非法。 |
| `ATOMUICLI_CTRL001` | `3` | 控件不存在。 |
| `ATOMUICLI_PKG001` | `3` | 包不存在。 |
| `ATOMUICLI_DATA001` | `4` | changelog 数据不可用。 |
| `ATOMUICLI_DATA005` | `4` | 版本或版本范围无法解析到可用快照。 |

## AOT 约束

- 版本范围解析使用显式 parser。
- changelog 来自快照，不读取远程发布说明。
- JSON DTO 纳入 source generated context。

## 测试点

- 无参数返回当前版本变更。
- `<from>..<to>` 范围过滤正确。
- 控件和包过滤可组合。
- breaking severity 可单独过滤。
- 不存在版本返回 `ATOMUICLI_DATA005`。

## 相关文档

- [详细设计](design.md)
- [命令设计标准](../command-design-standard.md)
- [知识查询命令共享设计](../knowledge-query-design.md)
- [错误码标准](../error-code-standard.md)
