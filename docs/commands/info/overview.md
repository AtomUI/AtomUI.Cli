# dotnet atomui info 设计

## 定位

`info` 是 P0 只读知识查询命令，用于查询单个控件的 API、注册要求、包归属、XAML 命名空间、Token、semantic parts 和示例索引。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui info Button
dotnet atomui info DataGrid --product datagrid --detail
dotnet atomui info Button --include properties,events,tokens --format json
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `control` | 必填 | 控件名称，支持大小写不敏感匹配。 |
| `--include <parts>` | `summary,api,registration` | 逗号分隔，可选 `summary`、`api`、`properties`、`events`、`methods`、`tokens`、`semantic`、`demos`、`registration`。 |
| `--strict` | `false` | 要求控件名称精确匹配。 |
| `--product <id>` | 全产品 | 限定产品范围。 |
| `--detail` | `false` | 输出默认值、类型约束、废弃信息和来源版本。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 目标版本 metadata 快照。
- 产品清单和包清单。
- 可选商业数据根。商业数据不可用时，只输出可公开的包级提示。

## 输出

json 输出示例：

```json
{
  "name": "Button",
  "displayName": "按钮",
  "productId": "desktop",
  "packageId": "AtomUI.Desktop.Controls",
  "namespace": "AtomUI.Desktop.Controls",
  "xamlNamespace": "https://atomui.net",
  "registration": {
    "required": true,
    "methods": ["UseDesktopControls"]
  },
  "api": {
    "properties": [],
    "events": [],
    "methods": []
  }
}
```

text 输出优先展示安装、注册和常用 API。markdown 输出用于拼接文档。

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `InfoCommandOptions` | Scoped value | 控件名、include 集合和全局选项。 |
| `InfoCommandHandler` | Transient | 控件名称解析和输出映射。 |
| `IControlQueryService` | Singleton | 控件详情查询。 |
| `IPackageQueryService` | Singleton | 包、依赖和注册关系查询。 |
| `ISemanticPartQueryService` | Singleton | semantic parts 查询。 |
| `ITokenQueryService` | Singleton | Token 摘要查询。 |
| `IOutputWriter` | Scoped | 输出格式化。 |

## 执行流程

1. 校验 `control` 非空。
2. 解析目标版本和产品过滤。
3. 使用 `IControlQueryService` 执行精确匹配或模糊匹配。
4. 控件唯一命中时装载所需 include 数据。
5. 多个候选命中时返回候选列表和修正建议。
6. 按输出格式写入 stdout。

## 错误码与退出码

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | `2` | 缺少 `control`。 |
| `ATOMUICLI_ARG002` | `2` | `--include` 包含未知部分。 |
| `ATOMUICLI_CTRL001` | `3` | 控件不存在。 |
| `ATOMUICLI_CTRL002` | `3` | 控件名存在歧义。 |
| `ATOMUICLI_DATA001` | `4` | 元数据快照不可用。 |

## AOT 约束

- 控件查询来自显式 metadata 快照。
- include 映射使用枚举或 source generated lookup。
- 不从控件运行时类型反射 API。

## 测试点

- 精确控件名返回完整 summary。
- 大小写不敏感匹配可用。
- `--strict` 拒绝模糊匹配。
- `--include tokens,semantic` 只输出请求部分。
- 不存在控件返回建议和 `ATOMUICLI_CTRL001`。

