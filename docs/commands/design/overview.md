# dotnet atomui design.md 设计

## 定位

`design.md` 是 P0 只读知识查询命令，用于输出 AtomUI 设计语言、控件研发规范、Token 使用规则和 Gallery 模式的 Markdown 文档。

命令运行时只读取内置文档快照，不访问网络，不扫描源码目录。默认输出 Markdown 正文；JSON 输出结构化 payload，正文位于 `payload.doc`。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui design.md
dotnet atomui design.md --section tokens
dotnet atomui design.md --audience agent
dotnet atomui design.md --target-version 6.0
dotnet atomui design.md --format json
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `--section <name>` | `all` | 可选 `all`、`overview`、`principles`、`layout`、`tokens`、`accessibility`、`patterns`、`controls`。 |
| `--audience <developer|agent>` | `developer` | 面向开发者或 Agent 输出。 |
| `--target-version <version>` | 最新内置文档快照 | 目标版本。 |
| `--format <text|json|markdown>` | `text` | text 与 markdown 都输出 Markdown 正文；json 输出结构化 payload。 |

## 输入

- `DocumentSnapshot` 中的 topic `design-language`。
- topic 的 `Source` 信息，包括 snapshot id、source commit、generated at。
- `MarkdownDocProcessor` 在构建期保留的 `<!-- Source: ... -->` source marker。

## 输出

text/markdown 输出：

```markdown
<!-- Source: docs/overview.md -->
# AtomUI 文档总览
...
```

json 输出：

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "design.md",
  "payload": {
    "command": "design.md",
    "targetVersion": "6.0",
    "section": "all",
    "audience": "developer",
    "doc": "<!-- Source: docs/overview.md -->\n# AtomUI 文档总览\n...",
    "source": {
      "snapshotId": "atomui-docs-source-..."
    },
    "warnings": []
  }
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `DesignCommandOptions` | Scoped value | section、audience 和全局选项。 |
| `DesignCommandHandler` | Transient | 执行命令并选择输出格式。 |
| `DesignDocumentQueryService` | Singleton | 查询设计文档、裁剪 section、构建 payload。 |
| `DocumentSnapshotRegistry` | Singleton | 内置文档快照注册表。 |

## 执行流程

1. 校验 `--section` 和 `--audience`。
2. 按 `--target-version` 和 `--lang` 选择文档快照。
3. 查找 topic `design-language`。
4. 按 section 映射到 source block；若专用设计文档无法按 source block 切分，则按 Markdown 标题别名裁剪。
5. text/markdown 返回 Markdown 正文。
6. json 返回结构化 payload。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | `--section` 或 `--audience` 非法。 |
| `ATOMUICLI_DATA001` | `4` | 当前快照缺少设计文档 topic。 |
| `ATOMUICLI_DATA005` | `4` | 目标版本无法解析到可用文档快照。 |

## AOT 约束

- 设计文档来自内置快照。
- 不读取远程文档。
- 不在命令 handler 中读取 AtomUI 源码。
- JSON payload 通过统一 JSON writer 输出。

## 测试点

- 默认输出包含完整 Markdown 和 source marker。
- `--format json` 输出结构化 payload，`payload.doc` 存在。
- `--target-version` 不匹配时返回 `ATOMUICLI_DATA005`。
- `--section tokens` 输出 Token source block。
- `--audience agent` 输出 Agent marker。

## 相关文档

- [详细设计](design.md)
- [命令设计标准](../command-design-standard.md)
- [知识查询命令共享设计](../knowledge-query-design.md)
- [错误码标准](../error-code-standard.md)
