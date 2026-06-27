# dotnet atomui env 详细设计

## 目标

`env` 收集本机和项目环境，用于 issue 排查、Agent 诊断和 `doctor` 前置上下文。命令只读，不执行 restore、build 或 publish。

## Options

```csharp
public sealed record EnvCommandOptions(
    GlobalCliOptions Global,
    string Path,
    bool IncludePackages,
    bool IncludeFiles) : IAtomUICliCommandOptions;
```

## Payload

```csharp
public sealed record EnvCommandPayload(
    string SchemaVersion,
    string Command,
    EnvironmentDto Environment,
    IReadOnlyList<ProjectEnvironmentDto> Projects,
    IReadOnlyList<PackageReferenceDto> Packages,
    IReadOnlyList<ProjectFileDto> Files);
```

## Handler

依赖：

- `IEnvironmentInfoProvider`
- `IProjectPathResolver`
- `IProjectFileReader`
- `IPackageReferenceReader`
- `IOutputWriter`

执行步骤：

1. 解析 path，支持目录、`.slnx`、`.sln`、`.csproj`。
2. 读取 `global.json`、project、props 和 lock file。
3. 读取 TargetFramework、RuntimeIdentifier、PublishAot、TrimMode。
4. 按 `--include-packages` 和 `--include-files` 裁剪 payload。
5. 输出结果。

## 输出规则

- `text`：分段输出 OS、SDK、项目和核心包。
- `json`：输出完整 payload。
- `markdown`：适合 issue 模板。

## 错误

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | path 格式非法。 |
| `ATOMUICLI_PRJ001` | 未找到项目。 |
| `ATOMUICLI_PRJ002` | 项目 XML 无法读取。 |

## 测试

- 目录输入自动发现 solution 或项目。
- `.slnx` 中多个项目可输出多个 project。
- Central Package Management 版本解析正确。
- `--detail` 通过全局 detail 输出 publish/AOT 属性。
- 不访问网络和外部命令。
