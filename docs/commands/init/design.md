# dotnet atomui init 详细设计

## 目标

`init` 为项目生成 AtomUI 初始化计划，包括包引用、注册入口、XAML namespace 和可选 sample。默认 dry-run，只有 `--write` 修改文件。

## Options

```csharp
public sealed record InitCommandOptions(
    GlobalCliOptions Global,
    string Path,
    IReadOnlyList<string> Products,
    bool AddSample,
    bool Write,
    bool Force) : IAtomUICliCommandOptions;
```

## Payload

```csharp
public sealed record InitCommandPayload(
    string SchemaVersion,
    string Command,
    bool Write,
    ProjectSummaryDto Project,
    IReadOnlyList<SetupActionDto> Actions,
    IReadOnlyList<SetupConflictDto> Conflicts,
    IReadOnlyList<string> VerificationCommands);
```

## Handler

依赖：

- `IProjectPathResolver`
- `IProjectInitAdvisor`
- `IPackageAddPlanner`
- `ISetupWriter`
- `IOutputWriter`

执行步骤：

1. 定位项目。
2. 校验产品存在且可初始化。
3. 读取现有包引用、注册入口和 XAML namespace。
4. 生成初始化 actions。
5. 检查冲突。
6. dry-run 输出计划。
7. `--write` 时应用计划并输出验证命令。

## 写入边界

- 只修改 plan 声明的文件。
- 无法定位注册插入点时返回 conflict。
- sample 文件只写明确路径。
- 不执行外部命令。

## 错误

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | product 非法。 |
| `ATOMUICLI_PRJ001` | 未找到项目。 |
| `ATOMUICLI_PKG001` | 产品对应包不存在。 |
| `ATOMUICLI_SETUP002` | 初始化冲突。 |
| `ATOMUICLI_SETUP003` | 写入失败。 |

## 测试

- 已存在包引用不重复添加。
- 已存在注册入口不重复添加。
- dry-run 不修改文件。
- `--write` 只修改 plan 声明文件。
- 无法安全插入注册入口时失败。

