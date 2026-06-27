# dotnet atomui lint 设计

## 定位

`lint` 是 P1 只读项目分析命令，用于检查废弃 API、错误包组合、XAML 命名空间、注册入口、主题 Token 使用和项目最佳实践。相比 `doctor`，`lint` 更偏向代码规范和可维护性。

## 所属模块

`AtomUICliProjectAnalysisModule`

## 调用形式

```bash
dotnet atomui lint
dotnet atomui lint ./src/MyApp
dotnet atomui lint ./MyApp.csproj --category xaml --format json
dotnet atomui lint ./MyApp.slnx --fix-plan markdown
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `path` | 当前目录 | 目录、`.sln`、`.slnx` 或 `.csproj`。 |
| `--category <name>` | 全部 | 可选 `package`、`registration`、`xaml`、`api`、`theme`、`aot`。 |
| `--severity <info|warning|error>` | `info` | 最低输出级别。 |
| `--fix-plan <text|markdown|json>` | 空 | 额外输出修复计划，不写文件。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 项目文件和包引用。
- XAML/C# 源文件。
- metadata 中的废弃 API、Token、semantic parts 和诊断规则。

## 输出

json 输出示例：

```json
{
  "project": "/repo/App/App.csproj",
  "summary": {
    "errors": 0,
    "warnings": 4,
    "infos": 1
  },
  "findings": [
    {
      "code": "ATOMUICLI_PRJ010",
      "category": "xaml",
      "severity": "warning",
      "file": "Views/MainView.axaml",
      "line": 24,
      "message": "XAML namespace can be simplified.",
      "suggestion": "Use the canonical AtomUI namespace from metadata."
    }
  ]
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `LintCommandOptions` | Scoped value | 路径、分类、severity 和输出选项。 |
| `LintCommandHandler` | Transient | 执行 lint 规则并输出结果。 |
| `IProjectDiagnosticEngine` | Scoped | lint 规则执行。 |
| `IXamlUsageScanner` | Scoped | XAML 规则输入。 |
| `ICSharpRegistrationScanner` | Scoped | 注册和 API 规则输入。 |
| `IMigrationPlanBuilder` | Scoped | 修复计划片段生成。 |

## 执行流程

1. 解析项目路径。
2. 校验 category 和 severity。
3. 选择 lint 规则集合。
4. 扫描项目文件和源文件。
5. 聚合 findings。
6. 可选生成 fix plan。
7. 输出结果。`lint` 不执行写入。

## 错误码与退出码

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | category、severity 或 fix-plan 格式非法。 |
| `ATOMUICLI_PRJ001` | `3` | 未找到项目。 |
| `ATOMUICLI_PRJ003` | `4` | 源文件读取失败。 |
| `ATOMUICLI_AOT001` | `5` | AOT 类 lint error。 |

## AOT 约束

- lint 规则显式注册。
- 不执行 code fix provider，不动态加载 analyzer 程序集。
- 修复计划是结构化文本，不直接修改文件。

## 测试点

- `--category xaml` 只执行 XAML 规则。
- `--fix-plan markdown` 输出可读修复清单。
- lint error 返回退出码 `5`。
- `lint` 不写用户项目文件。
- 废弃 API 规则能定位文件和行号。

