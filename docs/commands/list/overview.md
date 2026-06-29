# dotnet atomui list 设计

## 定位

`list` 是 P0 只读知识查询命令，用于列出 AtomUI 控件、产品、分类、包和版本引入信息。它是 Agent 了解当前生态能力的入口命令。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui list
dotnet atomui list controls
dotnet atomui list products
dotnet atomui list packages
dotnet atomui list categories
dotnet atomui list controls --product datagrid --format json
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `kind` | `controls` | 可选值：`controls`、`products`、`packages`、`categories`。 |
| `--category <name>` | 空 | 只列出指定分类下的控件。 |
| `--since <version>` | 空 | 只列出指定版本之后引入的条目。 |
| `--include-hidden` | `false` | 输出 metadata 中标记为隐藏但可诊断的条目。 |
| `--product <id>` | 全产品 | 使用全局产品过滤。 |
| `--target-version <version>` | 默认版本 | 使用全局目标版本。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 内置 `versions.json`、`products.json` 和目标版本快照。
- 可选 `--data-root` 提供的额外快照。
- 不读取用户项目。

## 输出

text 输出面向人类阅读，包含名称、产品、分类、包和引入版本。

json 输出示例：

```json
{
  "kind": "controls",
  "targetVersion": "6.0.6",
  "items": [
    {
      "name": "Button",
      "displayName": "按钮",
      "productId": "desktop",
      "category": "general",
      "packageId": "AtomUI.Desktop.Controls",
      "since": "6.0.0"
    }
  ]
}
```

markdown 输出用于文档生成，按分类分组。

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `ListCommandOptions` | Scoped value | 命令参数和全局选项快照。 |
| `ListCommandHandler` | Transient | 参数校验、调用查询服务、输出结果。 |
| `IControlQueryService` | Singleton | 控件列表查询。 |
| `IProductCatalogLoader` | Singleton | 产品清单查询。 |
| `IPackageQueryService` | Singleton | 包列表查询。 |
| `IOutputWriter` | Scoped | 按格式输出。 |

## 执行流程

1. 解析 `kind`，未传入时使用 `controls`。
2. 校验 `--target-version`，解析目标快照。
3. 合并 `--product`、`--category`、`--since` 过滤条件。
4. 调用对应 query service 获取不可变结果集。
5. 对结果按产品、分类、名称稳定排序。
6. 根据 `--format` 输出 text、json 或 markdown。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | `kind` 非法。 |
| `ATOMUICLI_DATA001` | `4` | 元数据快照不可用。 |
| `ATOMUICLI_DATA002` | `4` | schema 版本不兼容。 |
| `ATOMUICLI_DATA004` | `4` | data-root 不可读或授权失败。 |
| `ATOMUICLI_DATA005` | `4` | `--target-version` 或 `--since` 版本无法解析到可用快照。 |
| `ATOMUICLI_PKG001` | `3` | 指定产品不存在。 |

## AOT 约束

- `kind` 到查询方法的映射使用显式 switch。
- JSON DTO 纳入 source generated context。
- 不扫描程序集查找控件。

## 测试点

- 默认调用等价于 `list controls`。
- `--product` 和 `--category` 组合过滤稳定。
- `--since` 只返回目标版本范围内的条目。
- json 输出字段和排序稳定。
- 快照缺失时返回 `ATOMUICLI_DATA001`。
- 目标版本不存在时返回 `ATOMUICLI_DATA005`。

## 相关文档

- [详细设计](design.md)
- [命令设计标准](../command-design-standard.md)
- [知识查询命令共享设计](../knowledge-query-design.md)
- [错误码标准](../error-code-standard.md)
