# dotnet atomui usage 详细设计

## 目标

`usage` 统计项目中的 AtomUI 控件和 API 使用情况，输出控件、产品、分类、文件维度的聚合结果，并可提示可能未使用的包。

## Options

```csharp
public sealed record UsageCommandOptions(
    GlobalCliOptions Global,
    string Path,
    string? Control,
    UsageGroupBy GroupBy,
    bool IncludeLocations,
    bool IncludeUnusedPackages) : IAtomUICliCommandOptions;

public enum UsageGroupBy
{
    Control,
    Product,
    File,
    Category
}
```

## Payload

```csharp
public sealed record UsageCommandPayload(
    string SchemaVersion,
    string Command,
    ProjectSummaryDto Project,
    string GroupBy,
    IReadOnlyList<ControlUsageDto> Items,
    IReadOnlyList<PackageUsageDto> UnusedPackages);
```

## Handler

依赖：

- `ProjectCommandContextFactory`
- `IXamlUsageScanner`
- `ICSharpUsageScanner`
- `IControlQueryService`
- `IPackageReferenceReader`
- `IOutputWriter`

执行步骤：

1. 创建项目上下文。
2. 校验 `GroupBy` 和可选控件名。
3. 扫描 XAML 和 C#。
4. 映射到 metadata 控件。
5. 聚合到指定维度。
6. 可选计算未使用包。
7. 输出结果。

## 扫描规则

- XAML 使用 element name 和 xmlns 映射。
- C# 使用 object creation、generic type、static access。
- generated files 跳过。
- `IncludeLocations` 输出 file、line、column。

## 错误

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | group-by 非法。 |
| `ATOMUICLI_CTRL001` | 控件不存在。 |
| `ATOMUICLI_PRJ001` | 未找到项目。 |
| `ATOMUICLI_PRJ003` | 源码文件读取失败。 |

## 测试

- XAML 控件使用计数正确。
- C# object creation 可识别。
- group-by product/category/file 结果稳定。
- `--include-locations` 输出行列。
- 未使用包基于产品包和控件使用计算。

