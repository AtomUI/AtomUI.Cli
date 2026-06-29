# dotnet atomui token 设计

## 定位

`token` 是 P0 只读知识查询命令，用于查询全局 Token、控件 Token、默认值、适用范围和主题定制建议。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui token
dotnet atomui token Button
dotnet atomui token Button --name colorPrimary
dotnet atomui token --scope global --format json
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `control` | 空 | 控件名称。未传入时查询全局 Token。 |
| `--name <token>` | 空 | 查询指定 Token。 |
| `--scope <global|control|all>` | `all` | Token 范围。 |
| `--match <text>` | 空 | 按名称或描述模糊过滤。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 全局 Token 快照。
- 控件 Token 快照。
- Token 类型、默认值、可继承关系和版本信息。

## 输出

json 输出示例：

```json
{
  "scope": "control",
  "control": "Button",
  "tokens": [
    {
      "name": "colorPrimary",
      "type": "Color",
      "defaultValue": "#1677ff",
      "description": "Primary action color.",
      "since": "6.0.0"
    }
  ]
}
```

markdown 输出按 global/control 分组。

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `TokenCommandOptions` | Scoped value | 控件名、scope、name 和过滤条件。 |
| `TokenCommandHandler` | Transient | Token 查询和输出映射。 |
| `ITokenQueryService` | Singleton | Token 查询。 |
| `IControlQueryService` | Singleton | 控件名称解析。 |
| `IOutputWriter` | Scoped | 输出格式化。 |

## 执行流程

1. 校验 `--scope`。
2. 如果传入 `control`，解析控件。
3. 根据 scope 和 name 查询 Token。
4. 应用 `--match` 过滤。
5. 稳定排序：scope、控件、Token 名称。
6. 输出结果。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | `--scope` 非法。 |
| `ATOMUICLI_CTRL001` | `3` | 控件不存在。 |
| `ATOMUICLI_CTRL004` | `3` | Token 不存在。 |
| `ATOMUICLI_DATA001` | `4` | Token 数据不可用。 |

## AOT 约束

- Token 类型以 metadata 字段表达，不反射主题对象。
- JSON DTO 使用 source generated context。
- 不读取用户项目主题文件。

## 测试点

- 无参数返回全局 Token。
- 指定控件返回控件 Token。
- `--name` 精确查询失败时返回建议。
- `--match` 支持大小写不敏感过滤。
- markdown 输出按范围分组。

## 相关文档

- [详细设计](design.md)
- [命令设计标准](../command-design-standard.md)
- [知识查询命令共享设计](../knowledge-query-design.md)
- [错误码标准](../error-code-standard.md)
