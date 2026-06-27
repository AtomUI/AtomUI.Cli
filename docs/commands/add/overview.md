# dotnet atomui add 设计

## 定位

`add` 是 P2 写入类包辅助命令，用于为项目添加 AtomUI 生态包或产品能力，并生成对应的包引用、注册入口和验证建议。默认 dry-run，只有 `--write` 才修改项目文件。

## 所属模块

`AtomUICliSetupModule`

## 调用形式

```bash
dotnet atomui add datagrid
dotnet atomui add AtomUI.Desktop.Controls.DataGrid ./src/MyApp
dotnet atomui add datagrid --project ./MyApp.csproj --format json
dotnet atomui add datagrid --project ./MyApp.csproj --write
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `packageOrProduct` | 必填 | 包 ID 或产品 ID。 |
| `path` | 当前目录 | 可选项目路径。 |
| `--project <path>` | 当前目录 | 项目路径，优先级高于位置参数 path。 |
| `--version <version>` | 目标版本 | 包版本。 |
| `--include-registration` | `true` | 添加对应注册入口。 |
| `--write` | `false` | 执行写入。未传入时只输出计划。 |
| `--force` | `false` | 忽略可恢复冲突。仅 `--write` 时有效。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 项目文件和 Central Package Management 文件。
- 产品清单、包清单、依赖和冲突关系。
- 注册方法 metadata。

## 输出

json 输出示例：

```json
{
  "write": false,
  "project": "/repo/App/App.csproj",
  "requested": "datagrid",
  "resolvedPackage": "AtomUI.Desktop.Controls.DataGrid",
  "actions": [
    {
      "kind": "addPackageReference",
      "packageId": "AtomUI.Desktop.Controls.DataGrid",
      "version": "6.0.6"
    },
    {
      "kind": "addRegistration",
      "method": "UseDesktopDataGrid"
    }
  ],
  "conflicts": []
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `AddCommandOptions` | Scoped value | 包或产品、项目路径和写入选项。 |
| `AddCommandHandler` | Transient | 包解析、计划生成和可选写入。 |
| `IPackageAddPlanner` | Scoped | 包添加计划。 |
| `IPackageQueryService` | Singleton | 包和产品解析。 |
| `IProjectPathResolver` | Scoped | 项目定位。 |
| `IPackageReferenceReader` | Scoped | 已有包引用读取。 |
| `ISetupWriter` | Scoped | 写入计划执行。 |

## 执行流程

1. 校验 `packageOrProduct`。
2. 定位项目。
3. 将产品 ID 或别名解析为包 ID。
4. 检查依赖、冲突和替代关系。
5. 生成包引用和注册入口计划。
6. dry-run 输出计划。
7. `--write` 时应用计划并输出验证命令。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | `2` | 缺少 `packageOrProduct`。 |
| `ATOMUICLI_PRJ001` | `3` | 未找到项目。 |
| `ATOMUICLI_PKG001` | `3` | 包或产品不存在。 |
| `ATOMUICLI_PKG003` | `5` | 包冲突阻止添加。 |
| `ATOMUICLI_SETUP003` | `6` | 写入失败。 |

## AOT 约束

- 包解析来自 metadata，不访问 NuGet feed。
- 写入使用结构化 XML 修改。
- 不调用外部 `dotnet` 命令作为主实现。

## 测试点

- 产品 ID 可解析到包 ID。
- 已存在包引用时计划为空或输出已满足状态。
- 冲突包阻止写入。
- dry-run 不修改文件。
- `--write` 同时更新包引用和注册入口。

