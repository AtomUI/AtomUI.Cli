# dotnet atomui semantic 设计

## 定位

`semantic` 是 P0 只读知识查询命令，用于查询控件 semantic parts、模板结构摘要、可定制节点和样式定位建议。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui semantic Button
dotnet atomui semantic Button --part icon
dotnet atomui semantic DataGrid --product datagrid --detail --format json
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `control` | 必填 | 控件名称。 |
| `--part <name>` | 空 | 查询指定 semantic part。 |
| `--include-template` | `false` | 输出模板结构摘要。 |
| `--strict` | `false` | 要求控件名称和 part 名称精确匹配。 |
| `--detail` | `false` | 输出样式状态、伪类和可用 Token。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 控件 semantic parts 快照。
- 模板结构摘要。
- Token 和状态信息。

## 输出

json 输出示例：

```json
{
  "control": "Button",
  "parts": [
    {
      "name": "icon",
      "role": "leading icon container",
      "selector": ":icon",
      "tokens": ["iconSize"],
      "states": ["disabled", "pressed"]
    }
  ]
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `SemanticCommandOptions` | Scoped value | 控件名、part 和输出选项。 |
| `SemanticCommandHandler` | Transient | semantic 查询和输出映射。 |
| `ISemanticPartQueryService` | Singleton | semantic parts 查询。 |
| `IControlQueryService` | Singleton | 控件解析。 |
| `ITokenQueryService` | Singleton | 关联 Token 查询。 |

## 执行流程

1. 校验 `control`。
2. 解析控件和目标版本。
3. 查询 semantic parts。
4. 如果传入 `--part`，解析唯一 part。
5. 需要模板摘要时加载 template summary。
6. 输出 parts、selector、状态和 Token 关联。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | `2` | 缺少 `control`。 |
| `ATOMUICLI_CTRL001` | `3` | 控件不存在。 |
| `ATOMUICLI_CTRL005` | `3` | semantic part 不存在。 |
| `ATOMUICLI_DATA001` | `4` | semantic 数据不可用。 |

## AOT 约束

- semantic parts 来自快照，不从模板类型反射。
- selector 和状态以数据字段表示。
- JSON 输出使用 source generated context。

## 测试点

- 控件 semantic parts 列表稳定排序。
- `--part` 精确输出单个 part。
- `--include-template` 输出模板摘要但不输出完整模板源码。
- 不存在 part 返回候选建议。
- `--detail` 包含状态和 Token 关联。

