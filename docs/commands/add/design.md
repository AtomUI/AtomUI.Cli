# dotnet atomui add 详细设计

## 目标

`add` 为项目添加 AtomUI 产品或包能力，生成包引用、依赖、注册入口和验证命令。默认 dry-run，只有 `--write` 修改项目文件。

## Options

```csharp
public sealed record AddCommandOptions(
    GlobalCliOptions Global,
    string PackageOrProduct,
    string Project,
    string? Version,
    bool IncludeRegistration,
    bool Write,
    bool Force) : IAtomUICliCommandOptions;
```

## Payload

```csharp
public sealed record AddCommandPayload(
    string SchemaVersion,
    string Command,
    bool Write,
    ProjectSummaryDto Project,
    string Requested,
    string ResolvedPackageId,
    IReadOnlyList<SetupActionDto> Actions,
    IReadOnlyList<SetupConflictDto> Conflicts,
    IReadOnlyList<string> VerificationCommands);
```

## Handler

依赖：

- `IProjectPathResolver`
- `IPackageQueryService`
- `IPackageAddPlanner`
- `IPackageReferenceReader`
- `ISetupWriter`
- `IOutputWriter`

执行步骤：

1. 校验 `PackageOrProduct`。
2. 定位项目。
3. 将产品 ID、包 ID 或别名解析为包。
4. 检查依赖、冲突和替代关系。
5. 生成 package reference action。
6. 如果 `IncludeRegistration`，生成 registration action。
7. dry-run 或写入。

## 冲突策略

- 互斥包存在时阻止写入。
- 替代包存在时输出替换建议。
- 已存在目标包时输出已满足，不重复写入。
- 商业数据不可用时只输出公开包级建议。

## 错误

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG001` | 缺少 packageOrProduct。 |
| `ATOMUICLI_PRJ001` | 未找到项目。 |
| `ATOMUICLI_PKG001` | 包或产品不存在。 |
| `ATOMUICLI_PKG003` | 包冲突阻止添加。 |
| `ATOMUICLI_SETUP003` | 写入失败。 |

## 测试

- 产品 ID 可解析到包 ID。
- 已存在包引用不重复写入。
- 冲突包阻止写入。
- 注册入口可选。
- `--write` 更新 package reference 和注册入口。

