# dotnet atomui usage 设计

## 定位

`usage` 是 P1 只读项目分析命令，用于统计项目中 AtomUI 控件使用情况、文件分布、产品归属和潜在未使用包。

## 所属模块

`AtomUICliProjectAnalysisModule`

## 调用形式

```bash
dotnet atomui usage
dotnet atomui usage ./src/MyApp
dotnet atomui usage ./MyApp.csproj --control Button --format json
dotnet atomui usage ./MyApp.slnx --group-by product
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `path` | 当前目录 | 目录、`.sln`、`.slnx` 或 `.csproj`。 |
| `--control <name>` | 空 | 只统计指定控件。 |
| `--group-by <control|product|file|category>` | `control` | 汇总维度。 |
| `--include-locations` | `false` | 输出文件位置和行号。 |
| `--include-unused-packages` | `false` | 输出可能未使用的 AtomUI 包。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- `.axaml`、`.xaml`、`.cs`。
- 项目包引用。
- 控件 metadata 和产品清单。

## 输出

json 输出示例：

```json
{
  "project": "/repo/App/App.csproj",
  "groupBy": "control",
  "items": [
    {
      "control": "Button",
      "productId": "desktop",
      "count": 12,
      "files": 4
    }
  ],
  "unusedPackages": []
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `UsageCommandOptions` | Scoped value | 路径、过滤和分组选项。 |
| `UsageCommandHandler` | Transient | 调用扫描器并聚合统计。 |
| `IProjectPathResolver` | Scoped | 项目路径解析。 |
| `IXamlUsageScanner` | Scoped | XAML 控件使用扫描。 |
| `ICSharpUsageScanner` | Scoped | C# 控件使用扫描。 |
| `IControlQueryService` | Singleton | 控件名称和产品归属解析。 |
| `IPackageReferenceReader` | Scoped | 包引用读取。 |

## 执行流程

1. 解析项目路径。
2. 校验 `--group-by` 和 `--control`。
3. 扫描 XAML 和 C# 使用点。
4. 将使用点映射到控件 metadata。
5. 按指定维度聚合。
6. 可选计算可能未使用包。
7. 输出统计结果。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | `--group-by` 非法。 |
| `ATOMUICLI_CTRL001` | `3` | 指定控件不存在。 |
| `ATOMUICLI_PRJ001` | `3` | 未找到项目。 |
| `ATOMUICLI_PRJ003` | `4` | 扫描文件读取失败。 |

## AOT 约束

- 使用扫描不加载用户程序集。
- 控件识别基于 XAML XML 名称、namespace 和 C# syntax。
- 不执行编译或 design-time build。

## 测试点

- XAML 控件使用计数正确。
- C# new 控件场景可识别。
- `--group-by product` 聚合产品维度。
- `--include-locations` 输出稳定路径和行号。
- generated files 被跳过。

