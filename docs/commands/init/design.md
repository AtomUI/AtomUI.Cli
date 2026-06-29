# dotnet atomui init 详细设计

## 1. 命令定位

`init` 检查项目是否已具备 AtomUI 基础配置，并生成缺失项的写入计划。默认 dry-run，不修改项目。

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
dotnet atomui init ./src/App
dotnet atomui init --product desktop --format json
dotnet atomui init ./App.csproj --product desktop --add-sample
dotnet atomui init --product desktop --write
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `path` | path | 当前目录 | 不存在返回 `ATOMUICLI_PRJ001` | 项目入口。 |
| `--product` | string list | desktop | 未知返回 `ATOMUICLI_PKG001` | 初始化产品，可重复。 |
| `--add-sample` | bool | false | 无 | 生成最小示例视图或代码片段。 |
| `--write` | bool | false | 无 | 执行写入。 |
| `--force` | bool | false | 不能绕过 blocking conflict | 覆盖非阻断差异。 |

## 5. Options 类型

```csharp
public sealed record InitCommandOptions(
    GlobalCliOptions Global,
    string Path,
    IReadOnlyList<string> Products,
    bool AddSample,
    bool Write,
    bool Force) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IProjectAnalysisContextFactory`
- `IPackageQueryService`
- `IWritePlanBuilder<InitCommandOptions>`
- `IWriteExecutor`
- `IOutputWriter`

## 7. 执行流程

1. 读取项目上下文。
2. 查询 products 所需包、注册入口和可选 sample 模板。
3. 检查当前项目包引用、注册调用、XAML namespace。
4. 生成缺失项 write plan。
5. dry-run 输出或 `--write` 执行。

## 8. 领域服务契约

```csharp
public interface IProjectInitPlanBuilder : IWritePlanBuilder<InitCommandOptions>
{
}
```

分支：`AlreadyInitialized`、`PlanCreated`、`ProjectInvalid`、`ProductUnknown`、`Conflict`、`WriteFailed`。

## 9. 输出模型

复用 `WritePlan` / `WriteResult`，operation 可包含 package reference、registration call、XAML namespace 和 sample 文件。

## 10. 输出格式

- text：输出当前状态和缺失项。
- json：输出完整 plan/result。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_PRJ001` | 项目不存在 | `4` |
| `ATOMUICLI_PRJ002` | 项目无法读取 | `4` |
| `ATOMUICLI_PKG001` | product 不存在 | `3` |
| `ATOMUICLI_SETUP002` | blocking conflict | `6` |
| `ATOMUICLI_SETUP004` | 写入失败 | `6` |

## 12. AOT-first 约束

不执行 restore/build。不动态修改未知项目结构。write DTO 纳入 JSON context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| already initialized | 完整项目 | plan 为空，exit `0`。 |
| missing package | 缺包项目 | package operation。 |
| missing registration | 缺注册 | C# operation。 |
| dry-run | 无 `--write` | 文件不变。 |
| write conflict | 无法定位插入点 | `ATOMUICLI_SETUP002`。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Hosting/Setup/InitCommandOptions.cs`
- `src/AtomUI.Cli.Hosting/Setup/InitCommandHandler.cs`
- `src/AtomUI.Cli.Hosting/Setup/ProjectInitPlanBuilder.cs`
