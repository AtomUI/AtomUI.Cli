# dotnet atomui doc 设计

## 定位

`doc` 是 P0 只读知识查询命令，用于输出单个控件的完整 Markdown 文档。它面向开发者阅读、Agent 上下文注入和离线文档生成。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui doc Button
dotnet atomui doc Button --section api
dotnet atomui doc DataGrid --product datagrid --format markdown
dotnet atomui doc Button --format json
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `control` | 必填 | 控件名称。 |
| `--section <name>` | `all` | 可选 `all`、`overview`、`usage`、`api`、`tokens`、`semantic`、`demos`、`changelog`。 |
| `--strict` | `false` | 要求控件名称精确匹配。 |
| `--product <id>` | 全产品 | 限定产品范围。 |
| `--format <text|json|markdown>` | `markdown` | 默认输出 Markdown。 |

## 输入

- 控件 metadata。
- Demo 索引和示例片段。
- Token、semantic parts 和控件变更记录。

## 输出

markdown 输出结构：

```markdown
# Button

## 安装与注册

## 基础用法

## API

## Token

## Semantic Parts

## 示例

## 变更记录
```

json 输出返回结构化文档区块，便于 Agent 自行选择上下文。

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `DocCommandOptions` | Scoped value | 控件名、section 和全局选项。 |
| `DocCommandHandler` | Transient | 文档聚合和格式输出。 |
| `IControlQueryService` | Singleton | 控件 summary 和 API。 |
| `IDemoQueryService` | Singleton | 示例索引和代码块。 |
| `ITokenQueryService` | Singleton | Token 文档块。 |
| `ISemanticPartQueryService` | Singleton | semantic 文档块。 |
| `IChangelogQueryService` | Singleton | 控件变更块。 |

## 执行流程

1. 校验 `control` 和 `--section`。
2. 解析目标版本和产品过滤。
3. 查询控件详情。
4. 按 section 聚合文档块。
5. 过滤空区块，保留稳定标题顺序。
6. 输出 markdown、text 或 json。

## 错误码与退出码

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | `2` | 缺少 `control`。 |
| `ATOMUICLI_ARG002` | `2` | `--section` 非法。 |
| `ATOMUICLI_CTRL001` | `3` | 控件不存在。 |
| `ATOMUICLI_DATA001` | `4` | 文档数据不可用。 |

## AOT 约束

- Markdown 渲染使用显式 renderer，不通过模板运行时编译。
- JSON 文档 DTO 纳入 source generated context。
- 不从外部程序集动态读取 XML doc。

## 测试点

- 默认输出包含所有非空区块。
- `--section api` 只输出 API 区块。
- markdown 标题顺序稳定。
- json 输出能被 source generated context 序列化。
- 控件不存在时返回候选建议。

