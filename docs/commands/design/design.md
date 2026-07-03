# dotnet atomui design.md 详细设计

## 1. 命令定位

`design.md` 输出 AtomUI 设计语言与控件研发约束的 Markdown 文档，供 AI 设计工具、Agent 和开发者在生成界面或修改控件前读取稳定上下文。

该命令对齐同类 CLI 的 `design.md` 能力：文档在构建期进入内置快照，运行时只按版本读取，不访问远程网络，不扫描源码目录。

## 2. 所属模块与注册

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliMetadataModule` |
| 注册阶段 | `ConfigureAtomUICliCommands` |
| 分组 | knowledge |
| 读写 | `isReadOnly=true` |
| 项目上下文 | `requiresProject=false` |
| 写入确认 | `requiresWriteConfirmation=false` |
| Handler 生命周期 | Transient |
| 支持格式 | text、json、markdown |

## 3. 调用语法

```bash
dotnet atomui design.md
dotnet atomui design.md --section tokens
dotnet atomui design.md --audience agent
dotnet atomui design.md --format json
dotnet atomui design.md --target-version 6.0
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `--section` | string | `all` | 非法返回 `ATOMUICLI_ARG002` | `all`、`overview`、`principles`、`layout`、`tokens`、`accessibility`、`patterns`、`controls`。 |
| `--audience` | string | `developer` | 非法返回 `ATOMUICLI_ARG002` | `developer` 或 `agent`。 |
| `--target-version` | global version | 最新内置文档快照 | 无匹配快照返回 `ATOMUICLI_DATA005` | 选择目标版本的设计文档。 |
| `--format` | global enum | `text` | 非法返回 `ATOMUICLI_ARG002` | `text`、`json`、`markdown`。text 与 markdown 都输出 Markdown 正文。 |

`--product` 是全局选项，但 `design.md` 不做产品过滤。设计语言文档描述 AtomUI 的通用设计与工程约束，不按单个产品裁剪。

## 5. Options 类型

```csharp
public sealed record DesignCommandOptions(
    GlobalCliOptions Global,
    string Section,
    string Audience) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `DesignDocumentQueryService`
- `DocumentSnapshotRegistry`
- `IOutputWriter`

`DesignCommandHandler` 只负责调用查询服务、转换错误和选择输出形态，不读取文档源文件。

## 7. 构建期数据来源

`MarkdownDocProcessor` 在 Source Analysis Pipeline 中生成 `document:topic:design-language` fact。

如果 AtomUI 源码提供专用设计语言文档，优先读取：

- `DESIGN.md`
- `docs/design.md`
- `docs/design-language.md`

这与同类 CLI 的做法一致：设计语言文档是手写、可审核、按版本进入内置快照的稳定资产。

当前 AtomUI 源码未提供专用设计语言文档时，由下列文档拼接，保留 `<!-- Source: ... -->` 标记：

- `docs/overview.md`
- `docs/controls/overview.md`
- `docs/engineering/control-development-guidelines.md`
- `docs/engineering/control-documentation-guidelines.md`
- `docs/engineering/control-token-guidelines.md`
- `docs/gallery/gallery-showcase-design-pattern.md`

该 topic 同时进入 `CatalogSnapshot` 和 `DocumentSnapshot`。`design.md` 命令运行时使用 `DocumentSnapshot`，以获得 target version、language、source commit、snapshot id 等来源信息。

## 8. 执行流程

1. 解析 `--section`、`--audience` 和全局 `--target-version`、`--format`。
2. `DesignDocumentQueryService` 按 target version 和 language 选择 `DocumentSnapshot`。
3. 查找 topic `design-language`。
4. 按 section 从 Markdown source block 中裁剪正文。
5. 按 audience 做 Agent 视角标记。
6. 构建 `DesignCommandPayload`。
7. text/markdown 输出 `payload.Doc`，json 输出结构化 payload。

## 9. Section 映射

Section 裁剪优先使用构建期保留的 `<!-- Source: ... -->` source marker。若 AtomUI 源码后续提供单个专用设计语言文档，无法通过 source marker 切分 section 时，运行时按 Markdown 标题别名裁剪，从匹配标题截取到下一个同级或更高级标题。

| Section | 来源 |
| --- | --- |
| `all` | 完整设计文档。 |
| `overview` | `docs/overview.md`。 |
| `controls` | `docs/controls/overview.md`、`docs/engineering/control-documentation-guidelines.md`。 |
| `principles` | `docs/engineering/control-development-guidelines.md`。 |
| `tokens` | `docs/engineering/control-token-guidelines.md`。 |
| `patterns` | `docs/gallery/gallery-showcase-design-pattern.md`。 |
| `layout` | `docs/gallery/gallery-showcase-design-pattern.md`。 |
| `accessibility` | 当前复用完整文档；后续若 AtomUI 源码提供独立可访问性文档，再映射到专用 source block。 |

## 10. 输出模型

```csharp
public sealed record DesignCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string Section,
    string Audience,
    string Doc,
    DesignDocumentSourcePayload Source,
    IReadOnlyList<DesignDocumentWarningPayload> Warnings);
```

JSON payload 必须至少包含：

- `command`
- `targetVersion`
- `section`
- `audience`
- `doc`
- `source.snapshotId`
- `source.sourceCommit`
- `warnings`

## 11. 输出格式

| 格式 | 行为 |
| --- | --- |
| text | 输出 Markdown 正文。 |
| markdown | 输出 Markdown 正文。 |
| json | 输出 `DesignCommandPayload`。正文位于 `payload.doc`。 |

## 12. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | section 或 audience 非法 | `2` |
| `ATOMUICLI_DATA001` | 当前快照缺少 `design-language` topic | `4` |
| `ATOMUICLI_DATA005` | target version 无法匹配内置文档快照 | `4` |

## 13. AOT-first 约束

- 文档数据来自构建期快照，不运行时读取 AtomUI 源码。
- 不使用反射或动态模板。
- JSON payload 实现 `IAtomUICliJsonPayload`，由统一 JSON writer 序列化。
- Markdown section 裁剪只基于构建期保留的 source marker，不依赖外部 markdown parser。

## 14. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| default | `design.md` | 输出完整 Markdown，包含 source marker。 |
| json | `--format json design.md` | `payload.doc` 是 Markdown 字符串，不是字符串化 payload。 |
| missing version | `--target-version 9.0 design.md` | 返回 `ATOMUICLI_DATA005`。 |
| section | `design.md --section tokens` | 只输出 Token 规范相关 source block。 |
| dedicated doc section | 内置文档只来自 `docs/design.md` 且执行 `--section tokens` | 按 `## Tokens` 或同义标题裁剪。 |
| audience | `design.md --audience agent` | 输出 Agent audience marker。 |
| invalid | `design.md --section bad` | 返回 `ATOMUICLI_ARG002`。 |

## 15. 实现文件

- `src/AtomUI.Cli.Hosting/Metadata/MetadataCommandOptions.cs`
- `src/AtomUI.Cli.Hosting/Metadata/MetadataCommandHandlers.cs`
- `src/AtomUI.Cli.Hosting/Metadata/DesignDocumentQueryService.cs`
- `src/AtomUI.Cli.Hosting/Metadata/DesignCommandPayloads.cs`
- `tools/AtomUI.Cli.MetadataBuilder/SourceAnalysis/Processors/MarkdownDocProcessor.cs`
