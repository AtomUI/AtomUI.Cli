# dotnet atomui doc 详细设计

## 1. 命令定位

`doc` 输出控件或主题文档内容，适合开发者阅读和 Agent 获取完整说明。默认输出 markdown，也支持 JSON 包装文档内容。

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
dotnet atomui doc Button
dotnet atomui doc Button --section api --format markdown
dotnet atomui doc DataGrid --product datagrid --strict
dotnet atomui doc --topic design-language --format json
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `target` | string | 控件名或 topic 二选一 | 缺失返回 `ATOMUICLI_ARG001` | 文档目标。 |
| `--topic` | string | null | 与 target 互斥 | 查询非控件主题。 |
| `--section` | enum | all | 非法返回 `ATOMUICLI_ARG002` | 输出文档 section。 |
| `--strict` | bool | false | 无 | 控件名要求精确匹配。 |
| `--product` | global string | all visible | 未知返回 `ATOMUICLI_PKG001` | 限定产品范围。 |
| `--style` | enum | `full` | 非法返回 `ATOMUICLI_ARG002` | `full`、`summary`、`agent`。 |

## 5. Options 类型

```csharp
public sealed record DocCommandOptions(
    GlobalCliOptions Global,
    string? Target,
    string? Topic,
    DocumentSection Section,
    bool Strict,
    DocumentStyle Style) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IMetadataCommandContextFactory`
- `IDocumentationQueryService`
- `IControlQueryService`
- `IOutputWriter`

## 7. 执行流程

1. 校验 target/topic 二选一。
2. 创建 metadata 上下文。
3. 若是控件文档，先解析控件，处理 not found/ambiguous。
4. 调用 `IDocumentationQueryService` 查询文档。
5. 应用 section 和 style。
6. 构建 `DocCommandPayload`。
7. markdown 直接输出文档正文，json 输出结构化 payload。

## 8. 领域服务契约

```csharp
public interface IDocumentationQueryService
{
    ValueTask<DocumentQueryResult> QueryAsync(
        DocumentQuery query,
        CancellationToken cancellationToken);
}
```

结果分支：`Found`、`NotFound`、`AmbiguousTarget`、`SectionNotFound`、`DataUnavailable`。

## 9. 输出模型

```csharp
public sealed record DocCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string TargetKind,
    string TargetId,
    string Title,
    DocumentSection Section,
    DocumentStyle Style,
    string Markdown,
    IReadOnlyList<string> RelatedDemos,
    IReadOnlyList<WarningDto> Warnings);
```

## 10. 输出格式

- text：输出 markdown 正文，但不加 JSON 包装。
- markdown：与 text 相同，保留完整 heading。
- json：输出 payload，`Markdown` 字段包含正文。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | target/topic 缺失 | `2` |
| `ATOMUICLI_ARG002` | section/style 非法或 target/topic 同时传入 | `2` |
| `ATOMUICLI_CTRL001` | 控件不存在 | `3` |
| `ATOMUICLI_CTRL002` | 控件歧义 | `3` |
| `ATOMUICLI_PKG001` | product 不存在 | `3` |
| `ATOMUICLI_DATA001` | 文档数据不可用 | `4` |

## 12. AOT-first 约束

section/style parser 静态实现。文档内容来自 snapshot，不从磁盘任意路径加载模板。JSON DTO 纳入 source generation。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| 控件文档 | `doc Button` | markdown 正文。 |
| section | `doc Button --section api` | 只输出 API section。 |
| topic | `doc --topic design-language` | 输出主题文档。 |
| 非法互斥 | `doc Button --topic x` | `ATOMUICLI_ARG002`。 |
| json | `--format json` | Markdown 字段存在。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Metadata/Commands/DocCommandOptions.cs`
- `src/AtomUI.Cli.Metadata/Commands/DocCommandHandler.cs`
- `src/AtomUI.Cli.Metadata/Queries/DocumentationQueryService.cs`
