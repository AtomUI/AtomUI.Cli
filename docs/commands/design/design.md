# dotnet atomui design.md 详细设计

## 1. 命令定位

`design.md` 输出 AtomUI 设计语言、Token 使用规则、组件设计原则和 Agent 编码约束。它用于让 Agent 在生成 UI 代码前获得稳定设计上下文。

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
dotnet atomui design.md --product desktop --format markdown
dotnet atomui design.md --audience agent --format json
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `--section` | enum | all | 非法返回 `ATOMUICLI_ARG002` | `all`、`principles`、`layout`、`tokens`、`accessibility`、`patterns`、`controls`。 |
| `--product` | global string | all visible | 未知返回 `ATOMUICLI_PKG001` | 产品相关设计建议过滤。 |
| `--target-version` | global version | 默认 metadata 版本 | 版本无法解析返回 `ATOMUICLI_DATA005` | 查询版本。 |
| `--audience` | enum | developer | 非法返回 `ATOMUICLI_ARG002` | `developer`、`agent`。 |

## 5. Options 类型

```csharp
public sealed record DesignCommandOptions(
    GlobalCliOptions Global,
    DesignDocumentSection Section,
    DesignDocumentAudience Audience) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IMetadataCommandContextFactory`
- `IDesignDocumentQueryService`
- `IOutputWriter`

## 7. 执行流程

1. 解析 section 和 audience。
2. 创建 metadata 上下文。
3. 查询设计文档。
4. 应用 section/audience 过滤。
5. 构建 payload。
6. markdown/text 输出文档正文，json 输出结构化文档。

## 8. 领域服务契约

```csharp
public interface IDesignDocumentQueryService
{
    ValueTask<DesignDocumentQueryResult> QueryAsync(
        DesignDocumentQuery query,
        CancellationToken cancellationToken);
}
```

分支：`Found`、`SectionNotFound`、`DataUnavailable`。

## 9. 输出模型

```csharp
public sealed record DesignCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    DesignDocumentSection Section,
    DesignDocumentAudience Audience,
    string Markdown,
    IReadOnlyList<string> RelatedCommands,
    IReadOnlyList<WarningDto> Warnings);
```

## 10. 输出格式

- text/markdown：输出 markdown 正文。
- json：输出 payload。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | section/audience 非法 | `2` |
| `ATOMUICLI_PKG001` | product 不存在 | `3` |
| `ATOMUICLI_DATA003` | section 数据引用不存在 | `4` |
| `ATOMUICLI_DATA001` | 设计文档数据不可用 | `4` |
| `ATOMUICLI_DATA005` | target version 无法解析 | `4` |

## 12. AOT-first 约束

设计文档来自 metadata snapshot 或嵌入数据，不扫描 docs 目录。DTO 纳入 JSON context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| default | `design.md` | 完整 markdown。 |
| section | `--section tokens` | Token section。 |
| agent | `--audience agent` | Agent 约束内容。 |
| invalid | `--section bad` | `ATOMUICLI_ARG002`。 |
| json | `--format json` | Markdown 字段存在。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Metadata/Commands/DesignCommandOptions.cs`
- `src/AtomUI.Cli.Metadata/Commands/DesignCommandHandler.cs`
- `src/AtomUI.Cli.Metadata/Queries/DesignDocumentQueryService.cs`
