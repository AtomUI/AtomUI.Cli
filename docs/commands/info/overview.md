# dotnet atomui info 设计

## 定位

`info` 是 P0 只读知识查询命令，用于查询单个 AtomUI 控件的契约摘要，包括控件身份、包归属、XAML 使用方式、公开 API、模板部件、视觉状态、控件 Token、示例索引和诊断提示。

默认输出纯文本。显式传入 `--format markdown` 时输出 Markdown，显式传入 `--format json` 时输出结构化 JSON。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui info Button
dotnet atomui info Button --format markdown
dotnet atomui info Button --format json
dotnet atomui info DataGrid --product datagrid --detail
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `control` | 必填 | 控件名称，支持大小写不敏感匹配。 |
| `--include <sections>` | 全量展示 | 逗号分隔，支持 `identity`、`usage`、`api`、`events`、`methods`、`template`、`states`、`tokens`、`demos`、`related`、`diagnostics`、`all`。 |
| `--strict` | `false` | 要求控件名称精确匹配。 |
| `--product <id>` | 全产品 | 限定产品范围。 |
| `--detail` | `false` | 输出更完整的 API、模板、状态、Token 和 demo 信息。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输出

纯文本输出用于终端阅读：

```text
Button - General
Package: AtomUI.Desktop.Controls
Namespace: AtomUI.Desktop.Controls
Base type: Avalonia.Controls.Button
Product: desktop
Gallery: General/Button
Status: Stable

Usage:
  <atom:Button ButtonType="Primary" Content="Save" />
```

Markdown 输出用于复制到文档或 Agent 上下文。JSON 输出使用 `InfoCommandPayload`，供 CLI、MCP 和工具链结构化消费。

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `InfoCommandOptions` | Scoped value | 控件名、include 字符串和全局选项。 |
| `InfoCommandHandler` | Transient | 控件名称解析、payload 构建和输出映射。 |
| `MetadataQueryService` | Singleton | 基于静态快照查询控件信息。 |
| `InfoOutputRenderer` | Static | 输出 text 和 Markdown。 |
| `IOutputWriter` | Scoped | 输出格式化后的结果。 |

## 执行流程

1. 校验 `control` 非空。
2. 根据产品过滤解析控件。
3. 使用 `MetadataQueryService` 构建 `InfoCommandPayload`。
4. `--format json` 直接输出 payload。
5. `--format markdown` 使用 Markdown renderer。
6. 默认使用纯文本 renderer。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | `2` | 缺少 `control`。 |
| `ATOMUICLI_ARG002` | `2` | `--include` 或 `--format` 包含未知值。 |
| `ATOMUICLI_CTRL001` | `3` | 控件不存在。 |
| `ATOMUICLI_CTRL002` | `3` | 控件名存在歧义。 |
| `ATOMUICLI_DATA001` | `4` | 元数据快照不可用。 |

## AOT 约束

- 控件查询来自显式 metadata 快照。
- payload 使用显式 DTO 和 `IAtomUICliJsonPayload`。
- 不从控件运行时类型反射 API。
- 不在运行时扫描源码或 XAML。

## 相关文档

- [详细设计](design.md)
- [命令设计标准](../command-design-standard.md)
- [知识查询命令共享设计](../knowledge-query-design.md)
- [错误码标准](../error-code-standard.md)
