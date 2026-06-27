# dotnet atomui design.md 设计

## 定位

`design.md` 是 P0 只读知识查询命令，用于输出 AtomUI 设计语言、布局原则、主题 Token、可访问性和控件选型建议的 Markdown 文档。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui design.md
dotnet atomui design.md --section tokens
dotnet atomui design.md --product desktop --format markdown
dotnet atomui design.md --format json
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `--section <name>` | `all` | 可选 `all`、`principles`、`layout`、`tokens`、`accessibility`、`patterns`、`controls`。 |
| `--product <id>` | 全产品 | 产品相关设计建议过滤。 |
| `--target-version <version>` | 默认版本 | 目标版本。 |
| `--format <text|json|markdown>` | `markdown` | 默认输出 Markdown。 |

## 输入

- 设计语言文档快照。
- 全局 Token 快照。
- 产品和控件分类信息。

## 输出

markdown 输出面向 Agent 和文档系统：

```markdown
# AtomUI Design

## Principles

## Layout

## Tokens

## Accessibility

## Patterns

## Control Selection
```

json 输出以 section 数组表达，便于 Agent 按需截取上下文。

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `DesignMarkdownCommandOptions` | Scoped value | section、产品和格式选项。 |
| `DesignMarkdownCommandHandler` | Transient | 聚合设计文档区块。 |
| `IDesignDocumentQueryService` | Singleton | 设计文档查询。 |
| `ITokenQueryService` | Singleton | Token 文档引用。 |
| `IProductCatalogLoader` | Singleton | 产品相关过滤。 |

## 执行流程

1. 校验 `--section`。
2. 解析目标版本和产品过滤。
3. 查询设计文档区块。
4. 合并 Token 和控件分类引用。
5. 按固定 section 顺序输出。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | `--section` 非法。 |
| `ATOMUICLI_PKG001` | `3` | 指定产品不存在。 |
| `ATOMUICLI_DATA001` | `4` | 设计文档数据不可用。 |

## AOT 约束

- 设计文档来自内置快照，不读取远程文档。
- Markdown 渲染不使用运行时模板编译。
- JSON section DTO 纳入 source generated context。

## 测试点

- 默认输出包含所有设计 section。
- `--section tokens` 只输出 Token 相关内容。
- 产品过滤影响控件选型建议。
- markdown 输出标题稳定。
- json 输出 section 顺序稳定。

