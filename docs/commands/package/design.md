# dotnet atomui package 详细设计

## 目标

`package` 查询 AtomUI 生态包、产品归属、依赖、注册方法、替代关系和冲突关系。它是包安装、升级和诊断前的只读依据。

## Options

```csharp
public sealed record PackageCommandOptions(
    GlobalCliOptions Global,
    string? PackageId,
    bool IncludeConflicts,
    bool IncludeRegistration,
    bool Strict) : IAtomUICliCommandOptions;
```

## Payload

```csharp
public sealed record PackageCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    IReadOnlyList<PackageDto> Packages,
    IReadOnlyList<PackageConflictDto> Conflicts,
    IReadOnlyList<RegistrationDto> RegistrationMethods);
```

## Handler

依赖：

- `IPackageQueryService`
- `IProductCatalogLoader`
- `ICommercialDataDiagnosticService`
- `IOutputWriter`

执行步骤：

1. 解析目标版本和 product filter。
2. 未传包 ID 时列出包。
3. 传包 ID 时解析唯一包。
4. 装载依赖、冲突、替代和注册方法。
5. 应用商业可见性策略。
6. 输出结果。

## 输出规则

- `text`：包级摘要和注册提示。
- `json`：完整包图谱。
- `markdown`：按产品分组输出包说明。

## 错误

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_PKG001` | 包或产品不存在。 |
| `ATOMUICLI_PKG002` | 包 ID 歧义。 |
| `ATOMUICLI_DATA001` | 包数据不可用。 |
| `ATOMUICLI_DATA003` | 外部数据 schema 不兼容。 |

## 测试

- 无参数列出公开包。
- 指定包输出 dependencies 和 registration。
- product filter 只返回目标产品包。
- 商业数据不可用时输出降级提示。
- 冲突关系排序稳定。

