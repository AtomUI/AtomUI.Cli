# dotnet atomui package 设计

## 定位

`package` 是 P0 只读知识查询命令，用于查询 AtomUI 生态 NuGet 包、产品归属、依赖关系、注册方式、替代关系、冲突关系和商业数据可见性。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui package
dotnet atomui package AtomUI.Desktop.Controls
dotnet atomui package AtomUI.Desktop.Controls.DataGrid --detail --format json
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `packageId` | 空 | NuGet 包 ID。未传入时列出包。 |
| `--include-conflicts` | `true` | 输出冲突和替代关系。 |
| `--include-registration` | `true` | 输出注册方法。 |
| `--strict` | `false` | 要求包 ID 精确匹配。 |
| `--product <id>` | 全产品 | 产品过滤。 |
| `--detail` | `false` | 输出依赖范围、引入版本和可见性。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 输入

- 产品清单。
- 包清单和依赖关系。
- 版本快照。
- 可选商业数据根。

## 输出

json 输出示例：

```json
{
  "packageId": "AtomUI.Desktop.Controls",
  "productId": "desktop",
  "visibility": "public",
  "dependencies": [
    {
      "packageId": "Avalonia",
      "versionRange": "[11.0.0,)"
    }
  ],
  "registrationMethods": ["UseDesktopControls"],
  "conflicts": [],
  "replacements": []
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `PackageCommandOptions` | Scoped value | 包 ID、过滤和输出选项。 |
| `PackageCommandHandler` | Transient | 包查询和输出。 |
| `IPackageQueryService` | Singleton | 包详情、依赖和冲突查询。 |
| `IProductCatalogLoader` | Singleton | 产品和可见性查询。 |
| `ICommercialDataDiagnosticService` | Singleton | 商业数据不可用提示。 |

## 执行流程

1. 解析目标版本和产品过滤。
2. 未传入 `packageId` 时列出包清单。
3. 传入 `packageId` 时解析唯一包。
4. 查询依赖、注册、冲突和替代关系。
5. 若包属于商业数据但数据不可用，输出可公开提示。
6. 输出结果。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_PKG001` | `3` | 包或产品不存在。 |
| `ATOMUICLI_PKG002` | `3` | 包 ID 存在歧义。 |
| `ATOMUICLI_DATA001` | `4` | 包数据不可用。 |
| `ATOMUICLI_DATA003` | `4` | 商业数据 schema 不兼容。 |

## AOT 约束

- 包关系来自产品清单和 metadata 快照。
- 不调用 NuGet feed。
- 不反射包程序集。

## 测试点

- 无参数列出所有公开包。
- 指定包输出依赖和注册方式。
- 产品过滤只返回对应包。
- 商业数据缺失时输出降级提示。
- 冲突关系按稳定顺序输出。

