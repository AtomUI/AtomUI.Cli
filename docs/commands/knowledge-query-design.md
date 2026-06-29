# 知识查询类命令共享设计

知识查询类命令由 `AtomUICliMetadataModule` 贡献，默认只读，面向开发者、CI 和 Agent。它们只读取 metadata snapshot，不读取用户项目，不访问网络，不加载 AtomUI 运行时程序集。

适用命令：

- `list`
- `info`
- `doc`
- `demo`
- `token`
- `semantic`
- `design.md`
- `package`
- `changelog`

## 共享执行管线

```text
CommandOptions
  -> GlobalCliOptions
  -> TargetVersionResolver
  -> ProductFilterResolver
  -> DataRootResolver
  -> MetadataSnapshotIndexProvider
  -> QueryService
  -> PayloadBuilder
  -> OutputWriter
```

每个命令 handler 只负责参数校验、调用 query service、构造命令 payload 和返回 `AtomUICliResult`。metadata 加载、schema 校验、索引构建、商业可见性过滤和 fuzzy suggestion 都在 metadata service 层完成。

## 共享上下文

```csharp
public sealed record MetadataCommandContext(
    GlobalCliOptions Global,
    string TargetVersion,
    ProductFilter ProductFilter,
    MetadataRootSet DataRoots,
    string Language,
    bool Detail);
```

上下文创建规则：

1. `--target-version` 未传入时使用内置 metadata 默认版本。
2. `--product` 未传入时查询所有可见产品。
3. `--data-root` 可追加外部数据根，优先级高于内置公开数据。
4. 商业数据根只影响 visibility 允许的产品和字段。
5. `--lang` 只影响文案字段选择，不影响结构化 ID。

## Query Service 契约

### IControlQueryService

```csharp
public interface IControlQueryService
{
    ValueTask<ControlQueryResult> QueryAsync(
        ControlQuery query,
        CancellationToken cancellationToken);
}
```

`ControlQuery` 包含 normalized name、product filter、target version、strict flag、include sections。

`ControlQueryResult` 分支：

| 分支 | 含义 | 命令映射 |
| --- | --- | --- |
| `Found` | 找到唯一控件。 | 输出 payload。 |
| `NotFound` | 无匹配控件。 | `ATOMUICLI_CTRL001`，带 suggestions。 |
| `Ambiguous` | 多个产品或别名匹配。 | `ATOMUICLI_CTRL002`，带 candidates。 |
| `DataUnavailable` | 快照、schema 或数据根不可用。 | `ATOMUICLI_DATA001` 至 `ATOMUICLI_DATA005`。 |

### IPackageQueryService

负责包、产品、依赖、冲突、替代关系和注册入口查询。

结果分支：

- `FoundPackage`
- `FoundProduct`
- `NotFound`
- `Ambiguous`
- `ConflictMetadataUnavailable`
- `DataUnavailable`

### ITokenQueryService

负责全局 Token、控件 Token、继承链和默认值查询。Token 查询必须区分 `global`、`control` 和 `alias`。

### IDemoQueryService

负责 demo list、demo detail、code-only 输出和 demo metadata 查询。Demo 结果必须包含来源、语言、依赖包、适用版本和是否商业可见。

### IDocumentationQueryService

负责控件文档、设计文档和 section 过滤。Markdown 输出必须保持标题顺序稳定。

### ISemanticPartQueryService

负责 semantic parts、template part、pseudo class、可定制节点和样式入口查询。

### IChangelogQueryService

负责版本范围解析、breaking-only 过滤、目标控件/包过滤和迁移提示关联。

## Payload 通用字段

所有知识查询 JSON payload 必须包含：

```csharp
public abstract record KnowledgeCommandPayloadBase(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    ProductFilterDto ProductFilter,
    IReadOnlyList<WarningDto> Warnings);
```

排序规则：

- 产品按 metadata product order。
- 控件按 category order，再按 display order。
- 包按 product order，再按 package id ordinal。
- Token 按 scope、category、name。
- Demo 按 metadata order。
- Changelog 按 version desc，再按 product/control。

## 商业数据可见性

知识查询必须统一处理商业数据：

| 场景 | 行为 |
| --- | --- |
| 未配置商业数据根 | 公开数据正常输出；商业产品只输出包级提示，不伪造 API。 |
| 商业数据根 schema 不兼容 | 返回 `ATOMUICLI_DATA002`。 |
| 商业产品未知 | 返回 `ATOMUICLI_DATA005` 或 `ATOMUICLI_PKG001`，按查询目标决定。 |
| 字段不可见 | JSON 不输出该字段；text/markdown 给出“数据不可用”提示。 |
| 查询全产品 | 默认只包含当前可见产品。 |

## 错误映射

| 场景 | 错误码 | 退出码 |
| --- | --- | --- |
| 参数缺失或非法 include/section | `ATOMUICLI_ARG001` / `ATOMUICLI_ARG002` | `2` |
| 控件不存在 | `ATOMUICLI_CTRL001` | `3` |
| 控件歧义 | `ATOMUICLI_CTRL002` | `3` |
| 包或产品不存在 | `ATOMUICLI_PKG001` | `3` |
| 数据不可用 | `ATOMUICLI_DATA001` | `4` |
| schema 不兼容 | `ATOMUICLI_DATA002` | `4` |
| 商业数据不可用 | `ATOMUICLI_DATA004` | `4` |

## AOT-first 约束

- DTO 必须纳入 source generated JSON context。
- Query service 通过显式 DI 注册，不通过扫描发现。
- metadata index 不通过运行时反射读取控件程序集。
- fuzzy suggestion 只在失败分支执行，不进入热路径。
- markdown renderer 使用显式 formatter，不动态读取模板类型。

## 共享测试基线

每个知识查询命令至少覆盖：

- 默认版本查询成功。
- 指定 `--target-version` 查询成功。
- `--product` 过滤。
- 外部 `--data-root` 覆盖内置数据。
- 商业字段不可见过滤。
- `--format json` 稳定 schema。
- 查询目标不存在时 suggestions 稳定。
- command catalog 注册。
