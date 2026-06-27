# AtomUICliMetadataModule 详细设计

## 目标

`AtomUICliMetadataModule` 负责把随工具分发的 AtomUI 数据快照转换为稳定查询服务。模块边界内只处理“数据加载、schema 校验、索引构建、查询和格式无关结果”，不负责命令行解析，也不读取用户项目。

## 项目与命名空间

| 项目 | 命名空间 | 说明 |
| --- | --- | --- |
| `src/AtomUI.Cli.Metadata` | `AtomUI.Cli.Metadata` | 模块类和服务注册。 |
| `src/AtomUI.Cli.Metadata` | `AtomUI.Cli.Metadata.Snapshots` | 快照加载和 schema 校验。 |
| `src/AtomUI.Cli.Metadata` | `AtomUI.Cli.Metadata.Queries` | 控件、包、Token、Demo 查询服务。 |
| `src/AtomUI.Cli.Metadata` | `AtomUI.Cli.Metadata.Indexes` | 不可变索引。 |
| `src/AtomUI.Cli.Abstractions` | `AtomUI.Cli.Metadata.Contracts` | 公开 DTO 和 schema 常量。 |

## 数据根

数据根按优先级合并：

1. 命令行 `--data-root`。
2. 商业数据模块贡献的数据根。
3. 工具包内置公开数据根。

`IMetadataRootResolver` 输出不可变 `MetadataRootSet`：

```csharp
public sealed record MetadataRootSet(
    IReadOnlyList<MetadataRoot> Roots,
    string EffectiveTargetVersion);
```

查找策略：

- `versions.json` 采用优先级最高的数据根版本索引。
- 版本快照按数据根优先级查找，先匹配完整版本，再匹配 major 快照。
- 同一产品在多个数据根出现时，高优先级根覆盖同名产品的可扩展字段，但不能覆盖公开字段的 schema 语义。

## 快照加载

| 类型 | 职责 |
| --- | --- |
| `IVersionIndexLoader` | 加载和缓存 `versions.json`。 |
| `IProductCatalogLoader` | 加载和缓存 `products.json`。 |
| `IMetadataSnapshotLoader` | 加载 plain JSON 或 `.json.gz` 快照。 |
| `IMetadataSchemaValidator` | 校验 schemaVersion、必填字段、引用完整性。 |
| `IMetadataIndexBuilder` | 从快照构建不可变查询索引。 |

缓存粒度：

- Singleton service 持有 `ConcurrentDictionary<MetadataCacheKey, Lazy<Task<MetadataSnapshotIndex>>>`。
- cache key 包含 target version、data root fingerprint 和 language。
- cache value 必须不可变，避免跨命令 scope 状态污染。

## 构建期生成管线

运行时 metadata loader 只读取快照。快照由 `tools/AtomUI.Cli.MetadataBuilder` 生成，详细输入和 adapter 见 [source-extraction-design](source-extraction-design.md)。

生成管线必须覆盖：

- 从 AtomUI solution 和 build props 读取版本、项目和包边界。
- 从 Gallery 导航读取分类和控件顺序。
- 从 Gallery ShowCase 读取 Demo、API 表和 Token 表。
- 从控件源码读取 Avalonia property、TemplatePart、PseudoClasses 和 XAML namespace。
- 从 changelog 读取版本变更。
- 输出 plain JSON，并在发布阶段生成 gzip 快照。

运行时命令不得重新执行这些提取步骤。

## 查询索引

首批索引：

| 索引 | key | value |
| --- | --- | --- |
| `ControlNameIndex` | normalized control name + product id | `ControlDescriptor` |
| `PackageIdIndex` | normalized package id | `PackageDescriptor` |
| `ProductIdIndex` | product id | `ProductDescriptor` |
| `TokenNameIndex` | token name + scope | `TokenDescriptor` |
| `DemoNameIndex` | control name + demo name | `DemoDescriptor` |
| `ChangelogIndex` | version + target | `ChangelogEntry` |

名称规范化：

- 使用 ordinal ignore case。
- 保留原始名称用于输出。
- 不做文化相关大小写转换。
- 模糊建议只在查询失败时计算，不进入热路径。

## 服务契约

```csharp
public interface IControlQueryService
{
    ValueTask<ControlQueryResult> GetControlAsync(
        ControlQuery query,
        CancellationToken cancellationToken);
}
```

所有 query service 返回 result object，不直接写 stdout/stderr：

- `Found`
- `NotFound`，包含候选建议。
- `Ambiguous`，包含候选列表。
- `DataUnavailable`，包含数据根、schema、版本索引或授权错误，并映射为 [错误码标准](../../commands/error-code-standard.md) 中的 `ATOMUICLI_DATA001` 至 `ATOMUICLI_DATA005`。

命令 handler 负责把 result object 映射为 `AtomUICliResult`。

## 命令贡献

`ConfigureAtomUICliCommands` 显式注册：

```csharp
context.Commands.Add<ListCommandOptions, ListCommandHandler>("list");
context.Commands.Add<InfoCommandOptions, InfoCommandHandler>("info");
context.Commands.Add<DocCommandOptions, DocCommandHandler>("doc");
context.Commands.Add<DemoCommandOptions, DemoCommandHandler>("demo");
context.Commands.Add<TokenCommandOptions, TokenCommandHandler>("token");
context.Commands.Add<SemanticCommandOptions, SemanticCommandHandler>("semantic");
context.Commands.Add<DesignMarkdownCommandOptions, DesignMarkdownCommandHandler>("design.md");
context.Commands.Add<PackageCommandOptions, PackageCommandHandler>("package");
context.Commands.Add<ChangelogCommandOptions, ChangelogCommandHandler>("changelog");
```

模块不能根据文件名、类型名或 attribute 扫描注册命令。

## AOT-first 实现要求

- 所有 snapshot DTO 纳入 source generated JSON context。
- gzip/plain loader 共享同一反序列化入口。
- 禁止从 AtomUI 运行时程序集读取 API。
- 禁止按类型名拼接控件对象。
- 模糊匹配算法不能依赖动态代码生成或正则表达式源外编译。

## 测试设计

| 测试文件 | 覆盖点 |
| --- | --- |
| `MetadataRootResolverTests` | data root 优先级和版本解析。 |
| `MetadataSnapshotLoaderTests` | plain/gzip 快照加载和 schema 错误。 |
| `MetadataIndexBuilderTests` | 引用完整性和索引稳定性。 |
| `ControlQueryServiceTests` | found/not found/ambiguous/suggestion。 |
| `PackageQueryServiceTests` | 依赖、冲突和注册关系。 |
| `TokenQueryServiceTests` | global/control scope 查询。 |

测试数据放在 `tests/AtomUI.Cli.Metadata.Tests/Fixtures/metadata`，必须保持小而完整。
