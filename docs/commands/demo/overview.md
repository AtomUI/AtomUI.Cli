# dotnet atomui demo 设计

## 定位

`demo` 是 P0 只读知识查询命令，用于列出控件示例或输出指定示例代码。它为开发者和 Agent 提供可复制的最小用法、组合用法和复杂场景示例。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui demo Button
dotnet atomui demo Button basic
dotnet atomui demo DataGrid filtering --product datagrid --format markdown
dotnet atomui demo Button --format json
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `control` | 必填 | 控件名称。 |
| `name` | 空 | 示例名称。未传入时列出示例。 |
| `--language <xaml|csharp|all>` | `all` | 过滤示例代码语言。 |
| `--scenario <name>` | 空 | 过滤场景，例如 `basic`、`form`、`theme`。 |
| `--strict` | `false` | 要求控件名称和示例名称精确匹配。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 控件 metadata。
- 示例索引和示例代码片段。
- 示例依赖的包和注册信息。

## 输出

未指定 `name` 时输出示例列表。指定 `name` 时输出示例详情。

json 输出示例：

```json
{
  "control": "Button",
  "demos": [
    {
      "name": "basic",
      "title": "基础按钮",
      "scenario": "basic",
      "files": [
        {
          "language": "xaml",
          "path": "ButtonBasic.axaml",
          "content": "<Button Content=\"Save\" />"
        }
      ]
    }
  ]
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `DemoCommandOptions` | Scoped value | 控件名、示例名和过滤条件。 |
| `DemoCommandHandler` | Transient | 示例解析和输出。 |
| `IControlQueryService` | Singleton | 控件名称解析。 |
| `IDemoQueryService` | Singleton | 示例列表和代码查询。 |
| `IPackageQueryService` | Singleton | 示例依赖和注册提示。 |

## 执行流程

1. 校验 `control`。
2. 解析控件和产品范围。
3. 未传入 `name` 时查询示例列表并输出摘要。
4. 传入 `name` 时按名称或场景解析唯一示例。
5. 按 `--language` 过滤文件。
6. 输出示例代码、依赖包和注册提示。

## 错误码与退出码

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | `2` | 缺少 `control`。 |
| `ATOMUICLI_ARG002` | `2` | `--language` 非法。 |
| `ATOMUICLI_CTRL001` | `3` | 控件不存在。 |
| `ATOMUICLI_CTRL003` | `3` | 示例不存在。 |
| `ATOMUICLI_DATA001` | `4` | 示例数据不可用。 |

## AOT 约束

- 示例内容来自 metadata 快照或内置资源。
- 不运行示例项目。
- 不通过文件系统扫描包内任意目录发现示例。

## 测试点

- 未传入示例名时输出示例列表。
- 指定示例名时输出代码内容。
- `--language xaml` 只返回 XAML 文件。
- 示例不存在时返回候选示例。
- markdown 输出 fenced code block 语言标记正确。

