# dotnet atomui doctor 设计

## 定位

`doctor` 是 P1 只读项目诊断命令，用于检查包版本兼容、包冲突、注册缺失、XAML 命名空间、AOT/trimming 风险和 generated file 风险。它是用户排查 AtomUI 项目问题的主命令。

## 所属模块

`AtomUICliProjectAnalysisModule`

## 调用形式

```bash
dotnet atomui doctor
dotnet atomui doctor ./src/MyApp
dotnet atomui doctor ./MyApp.slnx --format json
dotnet atomui doctor ./MyApp.csproj --severity warning
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `path` | 当前目录 | 目录、`.sln`、`.slnx` 或 `.csproj`。 |
| `--severity <info|warning|error>` | `info` | 最低输出级别。 |
| `--rule <id>` | 空 | 只运行指定规则，可重复。 |
| `--no-usage-scan` | `false` | 跳过 XAML/C# 使用扫描，只诊断项目文件和包。 |
| `--fail-on-warning` | `false` | 存在 warning 时返回退出码 `5`。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 项目文件、包引用和 lock 文件。
- `.axaml`、`.xaml`、`.cs`。
- AtomUI metadata 产品清单、包关系和诊断规则 catalog。
- 跳过 `bin/`、`obj/`、`output/`、`.git/`、`.idea/`、`.vs/`、`.referenceprojects/`、`GeneratedFiles/`。

## 输出

json 输出示例：

```json
{
  "project": "/repo/App/App.csproj",
  "summary": {
    "errors": 1,
    "warnings": 2,
    "infos": 3
  },
  "diagnostics": [
    {
      "code": "ATOMUICLI_PKG003",
      "severity": "error",
      "message": "The project references conflicting AtomUI packages.",
      "file": "App.csproj",
      "suggestion": "Keep one package according to the product catalog replacement rule."
    }
  ]
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `DoctorCommandOptions` | Scoped value | 路径、规则过滤和输出选项。 |
| `DoctorCommandHandler` | Transient | 创建分析上下文并运行诊断。 |
| `IProjectPathResolver` | Scoped | 输入路径解析。 |
| `IProjectDiagnosticEngine` | Scoped | 诊断规则执行。 |
| `IPackageReferenceReader` | Scoped | 包引用读取。 |
| `IXamlUsageScanner` | Scoped | XAML 使用扫描。 |
| `ICSharpRegistrationScanner` | Scoped | 注册入口扫描。 |

## 执行流程

1. 解析项目路径。
2. 创建项目分析上下文。
3. 根据 `--rule` 和 `--no-usage-scan` 选择诊断规则。
4. 读取包、TargetFramework、注册入口和控件使用。
5. 执行诊断规则并聚合结果。
6. 根据 severity 过滤输出。
7. 根据 error 或 `--fail-on-warning` 计算退出码。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | severity 或 rule 参数非法。 |
| `ATOMUICLI_PRJ001` | `3` | 未找到项目。 |
| `ATOMUICLI_PRJ002` | `4` | 项目文件无法读取。 |
| `ATOMUICLI_DATA001` | `4` | 诊断所需 metadata 不可用。 |
| `ATOMUICLI_PKG003` | `5` | 包冲突或兼容性 error。 |
| `ATOMUICLI_PRJ010` | `5` | 项目结构或 XAML finding 达到失败阈值。 |
| `ATOMUICLI_AOT001` | `5` | AOT 相关 error。 |

## AOT 约束

- 诊断规则通过 contribution catalog 显式注册。
- 不加载用户项目程序集。
- C# 分析使用 Roslyn syntax API 或文本扫描，不需要编译用户项目。
- XAML 分析使用 XML reader 或轻量 parser，不实例化控件。

## 测试点

- 包冲突返回 error 和退出码 `5`。
- 注册缺失能关联包和建议方法。
- `--severity warning` 过滤 info。
- `--fail-on-warning` 在 warning 时返回 `5`。
- `--no-usage-scan` 不读取 XAML/C# 文件。
