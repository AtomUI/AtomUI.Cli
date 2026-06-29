# dotnet atomui list 详细设计

## 1. 命令定位

`list` 列出当前 metadata 版本中可见的产品、控件、分类和包。它是开发者和 Agent 发现 AtomUI 能力边界的入口命令，只读，不读取用户项目。

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
dotnet atomui list
dotnet atomui list controls
dotnet atomui list packages --include-hidden
dotnet atomui list --product datagrid --format json
dotnet atomui list controls --since 1.0.0 --category input
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `kind` | enum | `controls` | 非法值返回 `ATOMUICLI_ARG002` | 位置参数，可选 `controls`、`products`、`packages`、`categories`、`all`。 |
| `--kind` | enum | null | 非法值返回 `ATOMUICLI_ARG002` | `kind` 的显式选项形式；与位置参数同时出现且不一致时返回 `ATOMUICLI_ARG002`。 |
| `--category` | string | null | 只能用于 `controls` 或 `all` | 控件分类过滤。 |
| `--since` | version | null | 版本不存在返回 `ATOMUICLI_DATA005` | 只输出指定版本之后引入的条目。 |
| `--include-hidden` | bool | false | 无 | 输出 metadata 中标记为 hidden 但可诊断的条目。 |
| `--product` | global string | all visible | 未知产品返回 `ATOMUICLI_PKG001` | 产品过滤。 |
| `--target-version` | global version | 默认 metadata 版本 | 版本不存在返回 `ATOMUICLI_DATA005` | 查询版本。 |
| `--data-root` | global path | 内置数据根 | 不可读或授权失败返回 `ATOMUICLI_DATA004` | 追加数据根。 |

## 5. Options 类型

```csharp
public sealed record ListCommandOptions(
    GlobalCliOptions Global,
    ListKind Kind,
    string? Category,
    string? Since,
    bool IncludeHidden) : IAtomUICliCommandOptions;

public enum ListKind
{
    Controls,
    Products,
    Packages,
    Categories,
    All
}
```

## 6. Handler 依赖

| 依赖 | 生命周期 | 用途 |
| --- | --- | --- |
| `IMetadataCommandContextFactory` | Scoped | 创建 target version、product filter、data root 上下文。 |
| `IControlQueryService` | Singleton | 查询控件和分类。 |
| `IPackageQueryService` | Singleton | 查询产品和包。 |
| `IOutputWriter` | Scoped | 输出 payload。 |

## 7. 执行流程

1. 校验 `--kind`、`--category` 组合。
2. 创建 `MetadataCommandContext`。
3. 按 kind 调用控件或包查询服务。
4. 应用 product、category、since、hidden visibility 过滤。
5. 按 metadata display order 排序。
6. 构建 `ListCommandPayload`。
7. 根据输出格式写 stdout。

## 8. 领域服务契约

```csharp
public interface IListQueryService
{
    ValueTask<ListQueryResult> QueryAsync(
        ListQuery query,
        CancellationToken cancellationToken);
}
```

结果分支：

| 分支 | 映射 |
| --- | --- |
| `Success` | 输出列表。 |
| `UnknownProduct` | `ATOMUICLI_PKG001`。 |
| `UnknownCategory` | 成功输出空列表，并给 warning。 |
| `DataUnavailable` | `ATOMUICLI_DATA001`。 |
| `TargetVersionUnavailable` | `ATOMUICLI_DATA005`。 |

## 9. 输出模型

```csharp
public sealed record ListCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    ProductFilterDto ProductFilter,
    ListKind Kind,
    IReadOnlyList<ProductListItemDto> Products,
    IReadOnlyList<ControlListItemDto> Controls,
    IReadOnlyList<PackageListItemDto> Packages,
    IReadOnlyList<string> Categories,
    IReadOnlyList<WarningDto> Warnings);
```

## 10. 输出格式

- text：按产品分组显示控件或包，隐藏 schema 字段。
- json：输出完整 payload，空列表输出 `[]`。
- markdown：输出稳定标题和表格，适合复制到文档。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `--kind` 非法或 `--category` 组合非法 | `2` |
| `ATOMUICLI_PKG001` | `--product` 不存在 | `3` |
| `ATOMUICLI_DATA001` | metadata 快照不可用 | `4` |
| `ATOMUICLI_DATA002` | schema 不兼容 | `4` |
| `ATOMUICLI_DATA004` | data-root 不可读或授权失败 | `4` |
| `ATOMUICLI_DATA005` | `--target-version` 或 `--since` 版本无法解析 | `4` |

## 12. AOT-first 约束

DTO 纳入 JSON source generation。kind 解析使用显式 enum map。列表查询不得扫描程序集或反射控件类型。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| 默认列表 | `list` | 输出 controls，exit `0`。 |
| JSON | `list --format json` | payload schema 稳定。 |
| 产品过滤 | `list --product datagrid` | 只输出 datagrid 可见项。 |
| 分类过滤 | `list --category general` | 只输出该分类控件。 |
| 非法 kind | `list bad` | `ATOMUICLI_ARG002`，exit `2`。 |
| hidden | `--include-hidden` | 输出 hidden 诊断项但不输出私有字段。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Metadata/Commands/ListCommandOptions.cs`
- `src/AtomUI.Cli.Metadata/Commands/ListCommandHandler.cs`
- `src/AtomUI.Cli.Metadata/Queries/ListQueryService.cs`
- `tests/AtomUI.Cli.Metadata.Tests/Commands/ListCommandTests.cs`
