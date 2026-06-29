# dotnet atomui package 详细设计

## 1. 命令定位

`package` 查询 AtomUI 产品包、NuGet 包、依赖、冲突、替代关系和注册入口。它用于安装前检查和 Agent 修改包引用前的依赖判断。

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
dotnet atomui package
dotnet atomui package AtomUI.Controls.DataGrid
dotnet atomui package AtomUI.Controls.DataGrid --detail --format json
dotnet atomui package --product datagrid --include-conflicts --include-registration
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `packageId` | string | null | 不存在返回 `ATOMUICLI_PKG001` | NuGet package id。 |
| `--product` | global string | all | 未知返回 `ATOMUICLI_PKG001` | 产品查询。 |
| `--include-conflicts` | bool | true | 无 | 输出冲突和替代关系。 |
| `--include-registration` | bool | true | 无 | 输出注册方法。 |
| `--strict` | bool | false | 无 | 包 ID 要求精确匹配。 |
| `--detail` | global bool | false | 无 | 输出依赖范围、引入版本和可见性。 |

## 5. Options 类型

```csharp
public sealed record PackageCommandOptions(
    GlobalCliOptions Global,
    string? PackageId,
    bool IncludeConflicts,
    bool IncludeRegistration,
    bool Strict,
    bool Detail) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IMetadataCommandContextFactory`
- `IPackageQueryService`
- `IOutputWriter`

## 7. 执行流程

1. 创建 metadata 上下文。
2. 若传入 package id，查询单包。
3. 若传入 product，查询产品包集合。
4. 否则列出可见产品包摘要。
5. 应用 include-conflicts、include-registration 和 detail。
6. 输出 payload。

## 8. 领域服务契约

```csharp
public interface IPackageQueryService
{
    ValueTask<PackageQueryResult> QueryAsync(
        PackageQuery query,
        CancellationToken cancellationToken);
}
```

分支：`FoundPackage`、`FoundProduct`、`ListProducts`、`NotFound`、`Ambiguous`、`DataUnavailable`。

## 9. 输出模型

```csharp
public sealed record PackageCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    ProductFilterDto ProductFilter,
    IReadOnlyList<ProductPackageDto> Products,
    PackageDetailDto? Package,
    IReadOnlyList<PackageConflictDto> Conflicts,
    IReadOnlyList<RegistrationDto> Registration,
    IReadOnlyList<WarningDto> Warnings);
```

## 10. 输出格式

- text：显示包 ID、版本范围、依赖和注册方法。
- json：输出完整依赖、冲突、替代关系。
- markdown：输出包说明表格。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_PKG001` | 包或产品不存在 | `3` |
| `ATOMUICLI_PKG002` | 包名歧义 | `3` |
| `ATOMUICLI_DATA001` | 包数据不可用 | `4` |

## 12. AOT-first 约束

包关系来自 metadata snapshot，不访问 NuGet feed。DTO 纳入 JSON context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| list | `package` | 可见产品包摘要。 |
| package | `package AtomUI.Controls` | 单包详情。 |
| product | `--product datagrid` | 产品包集合。 |
| strict | `package atomui.controls --strict` | 非精确命中返回 `ATOMUICLI_PKG001`。 |
| not found | `package none` | `ATOMUICLI_PKG001`。 |
| json | `--format json` | 冲突和注册字段稳定。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Metadata/Commands/PackageCommandOptions.cs`
- `src/AtomUI.Cli.Metadata/Commands/PackageCommandHandler.cs`
- `src/AtomUI.Cli.Metadata/Queries/PackageQueryService.cs`
