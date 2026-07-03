# dotnet atomui package 设计

## 定位

`package` 是 P0 只读知识查询命令，用于查询 AtomUI 产品、NuGet 包、依赖关系、注册方式、兼容性、冲突关系、替代关系和包内控件清单。

目标实现基于构建期生成的 `PackageSnapshot`。运行时只读取内置快照或显式数据根，不访问 NuGet feed，不扫描 AtomUI 源码目录。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui package
dotnet atomui package desktop
dotnet atomui package AtomUI.Desktop.Controls
dotnet atomui package AtomUI.Desktop.Controls --tree
dotnet atomui package AtomUI.Desktop.Controls --include dependencies,registration,controls
dotnet atomui package --product desktop
dotnet atomui package --commercial
dotnet atomui package AtomUI.Desktop.Controls --format json
dotnet atomui package AtomUI.Desktop.Controls --format markdown
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `package-or-product` | 空 | 包 ID、产品 ID 或别名。未传入时列出包。 |
| `--kind <package|product|all>` | `all` | 限制目标解析类型。 |
| `--include <fields>` | `summary` | 可选 `dependencies`、`controls`、`registration`、`compatibility`、`conflicts`、`replacements`、`source`、`diagnostics`、`all`。 |
| `--tree` | `false` | 输出依赖树。 |
| `--commercial` | `false` | 列表或产品查询时只显示商业包。 |
| `--include-hidden` | `false` | 包含内部或隐藏包。 |
| `--strict` | `false` | 禁用别名和模糊建议。 |
| `--product <id>` | 空 | 按产品过滤。 |
| `--target-version <version>` | 最新内置快照 | 选择目标 AtomUI 版本。 |
| `--detail` | `false` | 等价于 `--include all`。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 数据来源

`PackageSourceProcessor` 在 Source Analysis Pipeline 中生成 `PackageSnapshot`。

构建期输入包括：

- AtomUI 源码中的 `*.csproj`。
- `Directory.Packages.props`。
- `Directory.Build.props` / `Directory.Build.targets`。
- 控件 catalog 和 document snapshot。
- 模块文档中的产品、注册和兼容性说明。
- 商业 overlay 数据。

默认源码位置遵守命令设计标准：`<AtomUICliRepoRoot>/../ReferenceProjects/AtomUI`。

## 输出

text 列表输出按产品分组：

```text
AtomUI packages: 3

Desktop
  AtomUI.Desktop.Controls              6.0.7 required public
  AtomUI.Desktop.Controls.DataGrid     6.0.7 optional commercial
  AtomUI.Desktop.Controls.ColorPicker  6.0.7 optional commercial
```

json 输出必须是结构化对象：

```json
{
  "command": "package",
  "targetVersion": "6.0",
  "query": {
    "kind": "package",
    "target": "AtomUI.Desktop.Controls"
  },
  "package": {
    "id": "AtomUI.Desktop.Controls",
    "version": "6.0.7",
    "productId": "desktop",
    "dependencies": [],
    "registration": [],
    "controls": []
  },
  "diagnostics": []
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `PackageCommandOptions` | Scoped value | 查询目标、kind、include、过滤和格式选项。 |
| `PackageCommandHandler` | Transient | 参数校验、调用查询服务、返回输出。 |
| `PackageSnapshotRegistry` | Singleton | 内置包快照注册表。 |
| `PackageQueryService` | Singleton | 包、产品、依赖、冲突、建议查询。 |
| `PackageOutputRenderer` | Singleton | text 和 markdown 渲染。 |

## 执行流程

1. 校验 `--kind` 和 `--include`。
2. 按 `--target-version` 选择 `PackageSnapshot`。
3. 校验 `--product` 过滤。
4. 解析 `package-or-product`。
5. 无目标时按产品分组列出包。
6. 产品目标输出产品包集合和安装建议。
7. 包目标输出单包详情。
8. 根据 `--include`、`--detail`、`--tree` 裁剪 payload。
9. 根据 `--format` 输出 text、markdown 或 JSON。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。本命令可能返回：

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | `--kind`、`--include` 非法或选项组合冲突。 |
| `ATOMUICLI_PKG001` | `3` | 包或产品不存在。 |
| `ATOMUICLI_PKG002` | `3` | 包 ID、产品 ID 或别名存在歧义。 |
| `ATOMUICLI_PKG003` | `5` | 包冲突或包版本不兼容诊断。 |
| `ATOMUICLI_DATA001` | `4` | package snapshot 不可用。 |
| `ATOMUICLI_DATA002` | `4` | package snapshot schema 不兼容。 |
| `ATOMUICLI_DATA004` | `4` | 商业或外部数据根不可用。 |
| `ATOMUICLI_DATA005` | `4` | 目标版本无法解析到可用快照。 |

## AOT 约束

- 运行时不扫描源码、不访问 Git、不访问 NuGet feed。
- 包图谱来自构建期 `PackageSnapshot`。
- DTO 纳入 source generated JSON context。
- Renderer 不使用运行时模板编译。

## 相关文档

- [详细设计](design.md)
- [命令设计标准](../command-design-standard.md)
- [知识查询命令共享设计](../knowledge-query-design.md)
- [错误码标准](../error-code-standard.md)
