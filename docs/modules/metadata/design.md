# AtomUICliMetadataModule 详细设计

## 1. 模块定位

`AtomUICliMetadataModule` 是 AtomUI Cli 的知识数据模块。它负责加载 AtomUI metadata 快照、合并内置和外部数据根、构建不可变查询索引，并向知识查询命令和 MCP tools 提供领域服务。

模块不解析命令行参数，不读取用户项目，不访问网络，不从 AtomUI 运行时程序集反射提取 API。

## 2. 模块注册

| 字段 | 值 |
| --- | --- |
| 模块类型 | `AtomUICliMetadataModule` |
| 依赖 | `AtomUICliCoreModule` |
| 生命周期 | singleton 服务为主，按 data-root/version/language 缓存不可变索引。 |
| 命令贡献 | `list`、`info`、`doc`、`demo`、`token`、`semantic`、`design.md`、`package`、`changelog`。 |
| MCP 贡献 | 由 MCP 模块复用本模块领域服务。 |
| 写入能力 | 无。 |

## 3. 数据根设计

数据根按优先级合并：

1. 命令行 `--data-root`。
2. 商业数据模块贡献的数据根。
3. tool 包内置公开数据根。

```csharp
public sealed record MetadataRoot(
    string Id,
    string PhysicalPath,
    MetadataRootKind Kind,
    MetadataVisibility Visibility,
    int Priority,
    string? ProductId);

public sealed record MetadataRootSet(
    IReadOnlyList<MetadataRoot> Roots,
    string EffectiveTargetVersion,
    string Fingerprint);
```

合并规则：

- 高优先级数据根优先解析版本索引。
- 同一产品在多个数据根出现时，高优先级根覆盖扩展字段。
- 公开字段的 schema 语义不能被商业数据根改写。
- 商业数据根未配置时，公开查询继续工作。
- data-root fingerprint 参与缓存 key。

## 4. 快照模型

```csharp
public sealed record MetadataSnapshot(
    int SchemaVersion,
    string Version,
    IReadOnlyList<ProductDescriptor> Products,
    IReadOnlyList<PackageDescriptor> Packages,
    IReadOnlyList<ControlDescriptor> Controls,
    IReadOnlyList<TokenDescriptor> Tokens,
    IReadOnlyList<DemoDescriptor> Demos,
    IReadOnlyList<DocumentDescriptor> Documents,
    IReadOnlyList<SemanticPartDescriptor> SemanticParts,
    IReadOnlyList<ChangelogEntry> Changelog,
    IReadOnlyList<MigrationGuideDescriptor> MigrationGuides);
```

运行时只读取 plain JSON 或 gzip JSON。schema 不兼容时返回 `ATOMUICLI_DATA002`，不能尝试猜测字段含义。

## 5. 索引设计

`IMetadataIndexProvider` 返回不可变索引：

```csharp
public interface IMetadataIndexProvider
{
    ValueTask<MetadataSnapshotIndexResult> GetIndexAsync(
        MetadataIndexRequest request,
        CancellationToken cancellationToken);
}
```

索引：

| 索引 | Key | Value |
| --- | --- | --- |
| `ControlNameIndex` | normalized control name + product id | `ControlDescriptor` |
| `PackageIdIndex` | normalized package id | `PackageDescriptor` |
| `ProductIdIndex` | product id | `ProductDescriptor` |
| `TokenNameIndex` | scope + token name | `TokenDescriptor` |
| `DemoNameIndex` | control name + demo name | `DemoDescriptor` |
| `DocumentIndex` | document kind + target id | `DocumentDescriptor` |
| `SemanticPartIndex` | control name + part name | `SemanticPartDescriptor` |
| `ChangelogIndex` | version + target id | `ChangelogEntry` |

名称规范化：

- 使用 ordinal ignore case。
- 保留原始名称用于输出。
- 不使用文化相关大小写转换。
- fuzzy suggestion 只在失败分支计算。

## 6. Query Service 契约

所有 query service 返回 result union，不直接写 stdout/stderr。

```csharp
public interface IControlQueryService
{
    ValueTask<ControlQueryResult> QueryAsync(ControlQuery query, CancellationToken cancellationToken);
}

public interface IPackageQueryService
{
    ValueTask<PackageQueryResult> QueryAsync(PackageQuery query, CancellationToken cancellationToken);
}

public interface ITokenQueryService
{
    ValueTask<TokenQueryResult> QueryAsync(TokenQuery query, CancellationToken cancellationToken);
}

public interface IDemoQueryService
{
    ValueTask<DemoQueryResult> QueryAsync(DemoQuery query, CancellationToken cancellationToken);
}

public interface IDocumentationQueryService
{
    ValueTask<DocumentQueryResult> QueryAsync(DocumentQuery query, CancellationToken cancellationToken);
}

public interface ISemanticPartQueryService
{
    ValueTask<SemanticPartQueryResult> QueryAsync(SemanticPartQuery query, CancellationToken cancellationToken);
}

public interface IChangelogQueryService
{
    ValueTask<ChangelogQueryResult> QueryAsync(ChangelogQuery query, CancellationToken cancellationToken);
}
```

结果分支：

| 分支 | 必须携带 |
| --- | --- |
| `Found` | descriptor、target version、source root id。 |
| `NotFound` | query、suggestions、visible product set。 |
| `Ambiguous` | candidates、disambiguation hint。 |
| `DataUnavailable` | root id、schema version、related error code。 |
| `Unauthorized` | product id、required data root kind、configuration hint。 |

## 7. 命令贡献

`ConfigureAtomUICliCommands` 显式贡献：

```csharp
context.Add<ListCommandOptions, ListCommandHandler>("list", ...);
context.Add<InfoCommandOptions, InfoCommandHandler>("info", ...);
context.Add<DocCommandOptions, DocCommandHandler>("doc", ...);
context.Add<DemoCommandOptions, DemoCommandHandler>("demo", ...);
context.Add<TokenCommandOptions, TokenCommandHandler>("token", ...);
context.Add<SemanticCommandOptions, SemanticCommandHandler>("semantic", ...);
context.Add<DesignCommandOptions, DesignCommandHandler>("design.md", ...);
context.Add<PackageCommandOptions, PackageCommandHandler>("package", ...);
context.Add<ChangelogCommandOptions, ChangelogCommandHandler>("changelog", ...);
```

每个 descriptor 必须声明 supported formats、read-only、requiresProject=false、requiresWriteConfirmation=false。

## 8. DI 生命周期

| 服务 | 生命周期 | 原因 |
| --- | --- | --- |
| `IMetadataRootResolver` | Singleton | 只解析不可变 root 配置。 |
| `IMetadataSnapshotLoader` | Singleton | 缓存 snapshot bytes 和 schema 检查结果。 |
| `IMetadataIndexProvider` | Singleton | 缓存不可变索引。 |
| Query services | Singleton | 只读取不可变索引，无请求状态。 |
| Command handlers | Transient | 每次命令执行创建。 |

缓存 key 必须包含 target version、data root fingerprint、language、product visibility。

## 9. 错误映射

| 场景 | 错误码 |
| --- | --- |
| 版本索引缺失 | `ATOMUICLI_DATA001` |
| schema 不兼容 | `ATOMUICLI_DATA002` |
| 快照引用不完整 | `ATOMUICLI_DATA003` |
| 商业数据不可用 | `ATOMUICLI_DATA004` |
| product/data-root 未知 | `ATOMUICLI_DATA005` |
| 控件不存在 | `ATOMUICLI_CTRL001` |
| 控件歧义 | `ATOMUICLI_CTRL002` |
| 包不存在 | `ATOMUICLI_PKG001` |

## 10. AOT-first 约束

- snapshot DTO 和 query payload DTO 纳入 source generated JSON context。
- 不使用反射读取 AtomUI 控件程序集。
- 不动态加载商业模块程序集。
- 不通过正则运行时编译构建热路径。
- command 和 query service 显式 DI 注册。

## 11. 测试矩阵

| 测试 | 覆盖 |
| --- | --- |
| data root priority | 外部数据根覆盖内置数据。 |
| schema incompatible | 返回 `ATOMUICLI_DATA002`。 |
| control found/not found/ambiguous | 三种 result 分支。 |
| commercial visibility | 未授权字段不输出。 |
| gzip/plain loader | 两种快照格式。 |
| command contribution | 9 个知识查询命令全部注册。 |
