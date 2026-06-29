# dotnet atomui add 详细设计

## 1. 命令定位

`add` 根据 AtomUI 产品清单生成包添加、版本选择和注册建议。默认 dry-run，未来可通过 `--write` 修改项目文件。

## 2. 所属模块与注册

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliSetupModule` |
| 注册阶段 | `ConfigureAtomUICliCommands` |
| 分组 | write |
| 读写 | `isReadOnly=false` |
| 项目上下文 | `requiresProject=true` |
| 写入确认 | `requiresWriteConfirmation=true` |
| Handler 生命周期 | Transient |
| 支持格式 | text、json、markdown |

## 3. 调用语法

```bash
dotnet atomui add datagrid
dotnet atomui add AtomUI.Controls.DataGrid ./src/App
dotnet atomui add datagrid --project ./App.csproj --format json
dotnet atomui add datagrid --project ./App.csproj --write
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `packageOrProduct` | string | 必填 | 缺失返回 `ATOMUICLI_ARG001` | 产品 ID 或 package ID。 |
| `path` | path | 当前目录 | 不存在返回 `ATOMUICLI_PRJ001` | 项目入口。 |
| `--project` | path | null | 不存在返回 `ATOMUICLI_PRJ001` | 项目路径，优先级高于位置参数 path。 |
| `--version` | version | target version 推荐版本 | 非法返回 `ATOMUICLI_ARG002` | 目标包版本。 |
| `--include-registration` | bool | true | 无 | 添加对应注册入口。 |
| `--write` | bool | false | 无 | 修改项目。 |
| `--force` | bool | false | 不能绕过 blocking conflict | 覆盖非阻断差异。 |

## 5. Options 类型

```csharp
public sealed record AddCommandOptions(
    GlobalCliOptions Global,
    string PackageOrProduct,
    string Path,
    string? Project,
    string? Version,
    bool IncludeRegistration,
    bool Write,
    bool Force) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IProjectAnalysisContextFactory`
- `IPackageQueryService`
- `IWritePlanBuilder<AddCommandOptions>`
- `IWriteExecutor`
- `IOutputWriter`

## 7. 执行流程

1. 校验 packageOrProduct。
2. 查询 product/package catalog。
3. 合并 path/--project 并读取项目当前包引用。
4. 检查冲突和替代关系。
5. 生成 package reference 和 registration write plan。
6. dry-run 输出或 `--write` 执行。

## 8. 领域服务契约

```csharp
public interface IPackageAddPlanBuilder : IWritePlanBuilder<AddCommandOptions>
{
}
```

分支：`PlanCreated`、`AlreadyInstalled`、`PackageNotFound`、`PackageConflict`、`ProjectInvalid`、`WriteFailed`。

## 9. 输出模型

复用 `WritePlan` / `WriteResult`。plan 必须包含 package operation、registration hints、verification commands。

## 10. 输出格式

- text：输出将添加的包、版本、注册入口和冲突。
- json：完整 plan/result。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | 缺少 package/product | `2` |
| `ATOMUICLI_ARG002` | version 非法 | `2` |
| `ATOMUICLI_PKG001` | 包或产品不存在 | `3` |
| `ATOMUICLI_PRJ001` | 项目不存在 | `4` |
| `ATOMUICLI_PKG003` | 包冲突 | `5` |
| `ATOMUICLI_SETUP002` | 写入计划冲突 | `6` |
| `ATOMUICLI_SETUP004` | 写入失败 | `6` |

## 12. AOT-first 约束

不访问 NuGet feed。版本建议来自 metadata。写入使用结构化 XML。DTO 纳入 JSON context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| product | `add datagrid` | 包和注册计划。 |
| package | `add AtomUI.Controls` | 单包计划。 |
| project option | `--project App.csproj` | 优先使用命名项目路径。 |
| already installed | 已安装 fixture | no-op plan。 |
| conflict | 冲突包 fixture | `ATOMUICLI_PKG003`。 |
| write | `--write` | 修改项目文件。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Hosting/Setup/AddCommandOptions.cs`
- `src/AtomUI.Cli.Hosting/Setup/AddCommandHandler.cs`
- `src/AtomUI.Cli.Hosting/Setup/PackageAddPlanBuilder.cs`
