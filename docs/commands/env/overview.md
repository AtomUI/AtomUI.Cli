# dotnet atomui env 设计

## 定位

`env` 是 P1 只读项目分析命令，用于收集当前开发环境、目标项目、TargetFramework、SDK、OS、包引用和 AtomUI 相关版本信息。它为 issue 排查、Agent 诊断和 `doctor` 前置上下文提供稳定输入。

## 所属模块

`AtomUICliProjectAnalysisModule`

## 调用形式

```bash
dotnet atomui env
dotnet atomui env ./src/MyApp
dotnet atomui env ./MyApp.slnx --format json
dotnet atomui env ./MyApp.csproj --detail
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `path` | 当前目录 | 目录、`.sln`、`.slnx` 或 `.csproj`。 |
| `--include-packages` | `true` | 输出项目包引用。 |
| `--include-files` | `false` | 输出参与分析的关键文件路径。 |
| `--detail` | `false` | 输出 RID、RuntimeIdentifier、publish/AOT 属性和 lock 文件摘要。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 当前进程的 OS、架构和 .NET SDK 信息。
- `global.json`、`.sln`、`.slnx`、`.csproj`。
- `Directory.Build.props`、`Directory.Packages.props`、`packages.lock.json`。
- 不执行 restore、build 或 publish。

## 输出

json 输出示例：

```json
{
  "environment": {
    "os": "macOS",
    "architecture": "arm64",
    "dotnetSdk": "10.0.300"
  },
  "project": {
    "path": "/repo/App/App.csproj",
    "targetFrameworks": ["net10.0"],
    "runtimeIdentifiers": ["osx-arm64"]
  },
  "packages": [
    {
      "packageId": "AtomUI.Desktop.Controls",
      "version": "6.0.6",
      "source": "Directory.Packages.props"
    }
  ]
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `EnvCommandOptions` | Scoped value | 路径和输出选项。 |
| `EnvCommandHandler` | Transient | 环境聚合和输出。 |
| `IProjectPathResolver` | Scoped | 解析输入路径。 |
| `IProjectFileReader` | Scoped | 读取项目和 props 文件。 |
| `IPackageReferenceReader` | Scoped | 读取包引用。 |
| `IEnvironmentInfoProvider` | Singleton | OS、架构和 SDK 信息。 |

## 执行流程

1. 解析 `path`，定位项目或解决方案。
2. 读取 `global.json` 和项目文件。
3. 读取 TargetFramework、RuntimeIdentifier、publish/AOT 属性。
4. 读取包引用和 Central Package Management 版本。
5. 合并环境信息。
6. 输出 text、json 或 markdown。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | 输入路径格式非法。 |
| `ATOMUICLI_PRJ001` | `3` | 未找到项目或解决方案。 |
| `ATOMUICLI_PRJ002` | `4` | 项目文件无法读取或 XML 非法。 |

## AOT 约束

- 不调用 `dotnet --info` 解析文本作为主路径；SDK 信息由运行时 API 或明确封装提供。
- 项目文件使用结构化 XML reader。
- 不加载 MSBuild workspace，不执行用户 target。

## 测试点

- 当前目录自动发现 `.slnx`、`.sln` 或 `.csproj`。
- Central Package Management 版本读取正确。
- `--detail` 包含 publish/AOT 属性。
- 输入路径不存在时返回 `ATOMUICLI_PRJ001`。
- json 输出不包含机器相关临时路径，除非字段明确为项目路径。
