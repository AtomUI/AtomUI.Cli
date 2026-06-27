# dotnet atomui init 设计

## 定位

`init` 是 P2 写入类项目辅助命令，用于检查一个 Avalonia/.NET 项目是否满足 AtomUI 初始化要求，并生成包引用、注册入口、XAML 命名空间和示例入口的初始化计划。默认 dry-run，只有 `--write` 才修改项目文件。

## 所属模块

`AtomUICliSetupModule`

## 调用形式

```bash
dotnet atomui init
dotnet atomui init ./src/MyApp
dotnet atomui init ./MyApp.csproj --product desktop --format json
dotnet atomui init ./MyApp.csproj --product desktop --write
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `path` | 当前目录 | 目录或 `.csproj`。 |
| `--product <id>` | `desktop` | 初始化产品。可重复。 |
| `--add-sample` | `false` | 生成最小示例视图或代码片段。 |
| `--write` | `false` | 执行写入。未传入时只输出计划。 |
| `--force` | `false` | 允许覆盖已存在的初始化片段。仅 `--write` 时有效。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 项目文件和 props 文件。
- 产品清单、包关系和注册方法。
- XAML 命名空间 metadata。

## 输出

json 输出示例：

```json
{
  "write": false,
  "project": "/repo/App/App.csproj",
  "actions": [
    {
      "kind": "addPackageReference",
      "packageId": "AtomUI.Desktop.Controls",
      "version": "6.0.6"
    },
    {
      "kind": "addRegistration",
      "method": "UseDesktopControls"
    }
  ]
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `InitCommandOptions` | Scoped value | 项目路径、产品和写入选项。 |
| `InitCommandHandler` | Transient | 初始化计划生成和可选写入。 |
| `IProjectInitAdvisor` | Scoped | 初始化建议和计划。 |
| `IProjectPathResolver` | Scoped | 项目定位。 |
| `IPackageAddPlanner` | Scoped | 包引用计划。 |
| `ISetupWriter` | Scoped | 结构化写入。 |

## 执行流程

1. 解析项目路径。
2. 校验产品存在且可初始化。
3. 读取现有包引用、注册入口和 XAML namespace。
4. 生成初始化计划。
5. 未传入 `--write` 时输出计划并退出。
6. 传入 `--write` 时应用计划。
7. 输出写入结果和建议验证命令。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | product 非法。 |
| `ATOMUICLI_PRJ001` | `3` | 未找到项目。 |
| `ATOMUICLI_PKG001` | `3` | 产品对应包不存在。 |
| `ATOMUICLI_SETUP002` | `6` | 初始化片段冲突且未传入 `--force`。 |
| `ATOMUICLI_SETUP003` | `6` | 写入失败。 |

## AOT 约束

- 写入计划由产品清单和显式模板生成。
- 项目修改使用 XML parser 和语法安全的文本替换边界。
- 不执行 `dotnet add package`，只修改受控文件或输出命令建议。

## 测试点

- dry-run 不写项目文件。
- 已存在包引用时不重复添加。
- 已存在注册入口时不重复添加。
- `--write` 只修改计划声明的文件。
- 冲突无 `--force` 时失败。

