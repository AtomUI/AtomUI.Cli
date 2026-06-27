# dotnet atomui list 详细设计

## 目标

`list` 提供元数据目录浏览能力，帮助用户和 Agent 在不知道控件或包名称时先发现可用对象。命令只读，运行时只访问已加载的 metadata index。

## Options

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
    Categories
}
```

## Payload

```csharp
public sealed record ListCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string Kind,
    IReadOnlyList<ListItemDto> Items);

public sealed record ListItemDto(
    string Id,
    string Name,
    string? DisplayName,
    string? ProductId,
    string? Category,
    string? PackageId,
    string? Since,
    string Visibility);
```

## Handler

`ListCommandHandler` 依赖：

- `ITargetVersionResolver`
- `IControlQueryService`
- `IProductCatalogLoader`
- `IPackageQueryService`
- `IOutputWriter`

执行步骤：

1. 解析目标版本。
2. 校验 `Kind`、`Category`、`Since`。
3. 根据 `Kind` 进入显式 switch。
4. 调用对应服务读取 immutable index。
5. 应用 `--product`、`--category`、`--since`、`--include-hidden`。
6. 按 `ProductId`、`Category`、`Name` 稳定排序。
7. 写出 payload。

## 输出规则

- `text`：表格列为 `Name`、`Product`、`Category`、`Package`、`Since`。
- `json`：输出完整 payload。
- `markdown`：按产品和分类生成二级列表。

## 错误

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | `kind`、`--since` 非法。 |
| `ATOMUICLI_PKG001` | `--product` 不存在。 |
| `ATOMUICLI_DATA001` | 目标版本快照不可用。 |

## 测试

- 不传 kind 时等价于 `controls`。
- `products` 不输出控件字段。
- `--since` 使用语义版本比较。
- `--include-hidden` 只影响隐藏条目，不影响商业私有字段。
- JSON 字段顺序和排序稳定。

