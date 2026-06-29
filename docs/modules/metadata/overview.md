# AtomUICliMetadataModule 设计

## 定位

`AtomUICliMetadataModule` 属于 `AtomUI.Cli.Metadata`。它负责加载 AtomUI 公开元数据快照、产品清单、版本索引、控件 API、Token、semantic parts、Demo、包信息和变更记录。

## 模块声明

```csharp
[Module(
    DisplayName = "AtomUI Cli Metadata",
    Dependencies = [typeof(AtomUICliCoreModule)])]
public sealed partial class AtomUICliMetadataModule : AtomUICliModule
{
}
```

## 依赖

| 依赖 | 原因 |
| --- | --- |
| `AtomUICliCoreModule` | 使用输出、错误、命令调度和 JSON 基础设施。 |

## 注册服务

| 服务 | 生命周期 | 说明 |
| --- | --- | --- |
| `IMetadataRootResolver` | Singleton | 合并内置数据根和 `--data-root`。 |
| `IVersionIndexLoader` | Singleton | 加载 `versions.json`。 |
| `IProductCatalogLoader` | Singleton | 加载 `products.json`。 |
| `IMetadataSnapshotLoader` | Singleton | 加载 plain JSON 或 gzip JSON 快照。 |
| `IControlQueryService` | Singleton | 控件查询、模糊匹配和拼写建议。 |
| `IPackageQueryService` | Singleton | 包查询、依赖、冲突和替代关系。 |
| `IChangelogQueryService` | Singleton | 版本变更和控件变更查询。 |
| `IDemoQueryService` | Singleton | 示例列表和示例内容查询。 |
| `ITokenQueryService` | Singleton | Token 查询。 |
| `ISemanticPartQueryService` | Singleton | semantic parts 查询。 |

## 贡献命令

| 命令 | 文档 | 阶段 |
| --- | --- | --- |
| `list` | [commands/list](../../commands/list/overview.md) | P0 |
| `info` | [commands/info](../../commands/info/overview.md) | P0 |
| `doc` | [commands/doc](../../commands/doc/overview.md) | P0 |
| `demo` | [commands/demo](../../commands/demo/overview.md) | P0 |
| `token` | [commands/token](../../commands/token/overview.md) | P0 |
| `semantic` | [commands/semantic](../../commands/semantic/overview.md) | P0 |
| `design.md` | [commands/design](../../commands/design/overview.md) | P0 |
| `package` | [commands/package](../../commands/package/overview.md) | P0 |
| `changelog` | [commands/changelog](../../commands/changelog/overview.md) | P0 |

## 数据契约

- `data/versions.json`
- `data/products.json`
- `data/v<major>.json`
- `data/v<major>.<minor>.<patch>.json`
- 发布包内的 `.json.gz` 压缩快照

## 生命周期钩子

- `ConfigureServices`：注册所有 metadata service。
- `ConfigureAtomUICliCommands`：注册查询类命令。
- `ConfigureAtomUICliDataRoots`：注册内置公开数据根。
- `Initialize`：校验默认版本索引和产品清单可加载。

## AOT 约束

- 不从 AtomUI 运行时程序集反射提取 API。
- 不通过类型名字符串构造控件描述。
- JSON 反序列化必须使用 source generated context。
- gzip/plain JSON loader 必须共享同一 schema 校验。

## 测试

- 内置数据根加载。
- `versions.json` 指向不存在快照时失败。
- 控件不存在时返回结构化错误和建议。
- 商业数据缺失时不能伪造控件 API。
- JSON 输出快照测试。

## 相关文档

- [详细设计](design.md)
- [源码提取设计](source-extraction-design.md)
- [模块设计索引](../overview.md)
- [整体架构设计](../../architecture/atomui-cli-architecture-design.md)
