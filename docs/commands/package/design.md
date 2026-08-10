# dotnet atomui package 详细设计

## 1. 命令定位

`package` 是 AtomUI Cli 的 P0 只读知识查询命令，用于查询 AtomUI 生态的产品、NuGet 包、包依赖、注册入口、兼容性、冲突关系、替代关系和包内控件清单。

该命令服务于三类场景：

- 开发者在安装前确认需要引用哪些 AtomUI 包。
- Agent 在修改项目引用前判断包归属、依赖和注册方式。
- CI/CD 或文档工具离线读取 AtomUI 包图谱，避免运行时访问 NuGet feed、Git 或源码目录。

`package` 不能继续依赖 `MetadataCatalog.Packages` 的轻量字段。目标实现必须引入独立 `PackageSnapshot`，与 `TokenSnapshot`、`SemanticSnapshot`、`DocumentSnapshot` 一样由 Source Analysis Pipeline 在构建期生成，运行时只查询内置快照。

## 2. 所属模块与注册

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliMetadataModule` |
| 注册阶段 | `ConfigureAtomUICliCommands` |
| 分组 | `knowledge` |
| 读写 | `isReadOnly=true` |
| 项目上下文 | `requiresProject=false` |
| 写入确认 | `requiresWriteConfirmation=false` |
| Handler 生命周期 | `Transient` |
| 支持格式 | `text`、`json`、`markdown` |

服务注册：

- `PackageSnapshotRegistry`：Singleton，加载内置包快照。
- `PackageQueryService`：Singleton，解析 package/product 查询并返回结构化结果。
- `PackageOutputRenderer`：Singleton，负责 text 和 markdown 输出。
- `PackageCommandHandler`：Transient，只做参数校验、调用查询服务和选择输出格式。

## 3. 调用语法

```bash
dotnet atomui package
dotnet atomui package desktop
dotnet atomui package AtomUI.Desktop.Controls
dotnet atomui package AtomUI.Desktop.Controls --tree
dotnet atomui package AtomUI.Desktop.Controls --include dependencies,registration,compatibility,controls,source
dotnet atomui package --product desktop
dotnet atomui package --commercial
dotnet atomui package desktop --kind product --format json
dotnet atomui package AtomUI.Desktop.Controls.DataGrid --format markdown
dotnet atomui package AtomUI.Desktop.Controls --target-version 6.0
```

不保留旧的 `--include-conflicts` 和 `--include-registration` 独立选项。它们统一并入 `--include conflicts` 和 `--include registration`。

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 合法值 | 校验失败 | 说明 |
| --- | --- | --- | --- | --- | --- |
| `package-or-product` | string | null | 包 ID、产品 ID 或别名 | 不存在返回 `ATOMUICLI_PKG001`，歧义返回 `ATOMUICLI_PKG002` | 查询目标。未传入时列出包摘要。 |
| `--kind` | enum | `all` | `package`、`product`、`all` | `ATOMUICLI_ARG002` | 限定目标解析类型，用于解决包 ID、产品 ID、别名重名。 |
| `--include` | string list | `summary` | `summary`、`dependencies`、`controls`、`registration`、`compatibility`、`conflicts`、`replacements`、`source`、`diagnostics`、`all` | `ATOMUICLI_ARG002` | 控制详情展开范围，逗号分隔。`all` 展开全部详情。 |
| `--tree` | bool | `false` | true/false | 无 | 输出包依赖树。隐式包含 `dependencies`。 |
| `--commercial` | bool | `false` | true/false | 无 | 列表或产品查询时只显示商业包。单包查询时不影响目标解析。 |
| `--include-hidden` | bool | `false` | true/false | 无 | 包含内部或隐藏包。默认只显示可公开发现的包。 |
| `--strict` | bool | `false` | true/false | 无 | 禁用别名、大小写宽松和模糊建议，只允许精确 ID 匹配。 |
| `--product` | global string | null | 产品 ID | 不存在返回 `ATOMUICLI_PKG001` | 过滤产品。与单包目标同时出现时，包必须属于该产品。 |
| `--target-version` | global version | 最新内置快照 | 内置快照版本 | 无匹配返回 `ATOMUICLI_DATA005` | 选择目标 AtomUI 版本。 |
| `--format` | global enum | `text` | `text`、`json`、`markdown` | `ATOMUICLI_ARG002` | 输出格式。JSON 必须输出对象 payload，不能输出字符串 payload。 |
| `--detail` | global bool | `false` | true/false | 无 | 等价于 `--include all`，但不改变 `--tree`。 |
| `--data-root` | global path | null | 可读目录 | 不可读返回 `ATOMUICLI_DATA004` | 后续用于商业 overlay 或外部包扩展数据。运行时不得从源码生成快照。 |

选项组合规则：

- `--tree` 仅对包详情或产品详情生效；无目标列表模式下忽略但不报错。
- `--commercial` 与单包目标同时出现时不做过滤，仍输出该包详情，并在 payload 中保留 `isCommercial`。
- `--kind package` 时目标不能解析为产品；`--kind product` 时目标不能解析为包。
- `--strict` 下不进行 contains/fuzzy 匹配；仅允许大小写精确策略由设计最终确定，推荐使用 ordinal ignore-case 精确 ID。

## 5. Options 类型

```csharp
public enum PackageQueryKind
{
    All,
    Package,
    Product
}

public enum PackageIncludeSection
{
    Summary,
    Dependencies,
    Controls,
    Registration,
    Compatibility,
    Conflicts,
    Replacements,
    Source,
    Diagnostics,
    All
}

public sealed record PackageCommandOptions(
    GlobalCliOptions Global,
    string? Target,
    PackageQueryKind Kind,
    IReadOnlySet<PackageIncludeSection> Include,
    IReadOnlyList<string> InvalidIncludes,
    string? InvalidKind,
    bool Tree,
    bool CommercialOnly,
    bool IncludeHidden,
    bool Strict) : IAtomUICliCommandOptions;
```

解析要求：

- `Target` 取第一个 positional。
- `--include dependencies,controls` 解析为集合，去重并保持稳定排序。
- `--detail` 在 options 构造时转换为 `Include={All}`。
- 解析阶段不访问 metadata，只记录非法枚举文本；handler 统一返回 `ATOMUICLI_ARG002`。

## 6. Handler 依赖

运行时基础设施：

- `CliInvocationContext`
- `CancellationToken`

领域服务：

- `PackageQueryService`
- `PackageOutputRenderer`

Handler 规则：

- 不接收 root `IServiceProvider`。
- 不读取源码目录。
- 不访问 NuGet feed。
- 不解析 `.csproj`。
- 不拼接 JSON 字符串。
- 不直接依赖 `MetadataQueryService.FindPackageOrProduct`。

## 7. 构建期数据来源

新增 `PackageSourceProcessor`，挂载到 Source Analysis Pipeline。

输入源必须遵守命令设计标准中的源码根目录规则：

1. `AtomUISourceRoot` MSBuild 属性或 `--source-root <path>`
2. `ATOMUI_SOURCE_ROOT` 环境变量
3. 默认约定：`<AtomUICliRepoRoot>/.workspace/AtomUI`

构建期读取范围：

| 输入 | 抽取内容 |
| --- | --- |
| `*.csproj` | `PackageId`、`AssemblyName`、`RootNamespace`、`TargetFrameworks`、`IsPackable`、`ProjectReference`、`PackageReference`。 |
| `Directory.Packages.props` | 中央包版本、外部依赖版本、版本范围。 |
| `Directory.Build.props` / `Directory.Build.targets` | 版本属性、pack 属性、AOT/trimming 相关属性。 |
| 控件 catalog / document snapshot | 包内控件清单、控件数量、商业属性、可选包属性。 |
| 模块文档 | 产品说明、包说明、注册方法、兼容性说明、安装建议。 |
| 源码扫描 | module 类型、service registration extension、theme provider、resource include、初始化入口。 |
| 商业 overlay | 商业包可见性、授权要求、商业控件包说明、公开可输出字段。 |

运行时不得执行 `git clone`、`git pull`、`dotnet restore` 或访问 NuGet feed。构建期生成的 `PackageSnapshot` 必须记录 source ref、source commit、generated at、target version 和 snapshot schema version。

## 8. 执行流程与查询规则

执行流程：

1. Handler 校验 `InvalidKind` 和 `InvalidIncludes`。
2. `PackageQueryService` 按 `--target-version` 选择 `PackageSnapshot`。
3. 校验 `--product` 是否存在。
4. 根据 `Target`、`--kind`、`--strict` 解析查询目标。
5. 无目标时进入列表模式，按产品分组输出可见包。
6. 目标为产品时输出该产品的包集合、安装建议、必选包、可选包和商业包。
7. 目标为包时输出单包详情。
8. 根据 `--include`、`--detail`、`--tree` 裁剪 payload。
9. 根据 `--format` 返回 text、markdown 或 JSON payload。

目标解析规则：

- 精确包 ID 命中优先于别名命中。
- 精确产品 ID 命中优先于产品别名命中。
- 当同一输入同时命中包和产品，且 `--kind all`，返回 `ATOMUICLI_PKG002` 并给出 candidates。
- `--kind package` 只在包集合内解析。
- `--kind product` 只在产品集合内解析。
- 非 strict 模式可返回 suggestions；strict 模式不返回模糊建议。

排序规则：

- 产品按 `DisplayOrder`，再按 `Id`。
- 包按 required、optional、commercial 分组，再按 `DisplayOrder`、`Id`。
- 依赖按 AtomUI 内部依赖优先，再按外部依赖，最后按 `PackageId`。
- 控件按 catalog 中的 category/display order。

## 9. 领域服务契约

```csharp
public sealed record PackageQuery(
    string? Target,
    PackageQueryKind Kind,
    string? ProductId,
    string? TargetVersion,
    string Language,
    IReadOnlySet<PackageIncludeSection> Include,
    bool Tree,
    bool CommercialOnly,
    bool IncludeHidden,
    bool Strict);

public sealed record PackageQueryResult(
    PackageQueryResultKind Kind,
    PackageCommandPayload? Payload,
    AtomUICliError? Error);

public enum PackageQueryResultKind
{
    Listed,
    FoundPackage,
    FoundProduct,
    NotFound,
    Ambiguous,
    DataUnavailable,
    Invalid
}

public sealed class PackageQueryService
{
    public PackageQueryResult Query(PackageQuery query);
}
```

结果分支映射：

| 分支 | 触发 | 输出 |
| --- | --- | --- |
| `Listed` | 无 target | 成功 payload，包含产品分组包摘要。 |
| `FoundPackage` | 唯一包命中 | 成功 payload，包含 `package`。 |
| `FoundProduct` | 唯一产品命中 | 成功 payload，包含 `product` 和该产品 packages。 |
| `NotFound` | 包或产品不存在 | `ATOMUICLI_PKG001`，带 suggestions。 |
| `Ambiguous` | 输入命中多个候选 | `ATOMUICLI_PKG002`，带 candidates 和 `--kind` 建议。 |
| `DataUnavailable` | 快照缺失、版本缺失、schema 不兼容或商业数据不可读 | `ATOMUICLI_DATA001`、`ATOMUICLI_DATA002`、`ATOMUICLI_DATA004` 或 `ATOMUICLI_DATA005`。 |
| `Invalid` | 选项值非法或组合非法 | `ATOMUICLI_ARG002`。 |

## 10. 输出模型

### PackageSnapshot

```csharp
public sealed record PackageSnapshot(
    string SchemaVersion,
    string SnapshotId,
    string TargetVersion,
    PackageSourceIdentity Source,
    IReadOnlyList<ProductPackageProfile> Products,
    IReadOnlyList<PackageDocument> Packages,
    IReadOnlyList<PackageConflictDocument> Conflicts,
    IReadOnlyList<PackageReplacementDocument> Replacements,
    IReadOnlyList<PackageDiagnostic> Diagnostics);
```

### PackageDocument

```csharp
public sealed record PackageDocument(
    string Id,
    string ProductId,
    string Version,
    string Description,
    string PackageKind,
    bool IsRequired,
    bool IsOptional,
    bool IsCommercial,
    bool IsHidden,
    IReadOnlyList<string> TargetFrameworks,
    string RootNamespace,
    string AssemblyName,
    IReadOnlyList<PackageDependencyDocument> Dependencies,
    IReadOnlyList<PackageRegistrationDocument> Registration,
    PackageCompatibilityDocument Compatibility,
    IReadOnlyList<PackageControlDocument> Controls,
    IReadOnlyList<PackageSourceFileDocument> SourceFiles,
    IReadOnlyList<PackageDiagnostic> Diagnostics);
```

### PackageCommandPayload

```csharp
public sealed record PackageCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    PackageQueryPayload Query,
    PackageSourcePayload Source,
    IReadOnlyList<ProductPackagePayload> Products,
    ProductPackagePayload? Product,
    IReadOnlyList<PackageSummaryPayload> Packages,
    PackageDetailPayload? Package,
    PackageDependencyTreePayload? DependencyTree,
    IReadOnlyList<PackageConflictPayload> Conflicts,
    IReadOnlyList<PackageReplacementPayload> Replacements,
    IReadOnlyList<AtomUICliDiagnostic> Diagnostics,
    IReadOnlyList<PackageSuggestionPayload> Suggestions) : IAtomUICliJsonPayload;
```

字段要求：

- `payload` 必须是对象，不能是字符串。
- 列表输出时 `Package` 为 null，`Products` 和 `Packages` 有值。
- 产品输出时 `Product` 有值，`Packages` 只包含该产品的包。
- 单包输出时 `Package` 有值，`Products` 可为空或只包含所属产品摘要。
- `Diagnostics` 始终存在，默认空数组。
- `Suggestions` 仅在成功但存在推荐动作时填充，例如安装命令、相关查询命令。

## 11. 输出格式

### text

无目标输出：

```text
AtomUI packages: 3

Desktop
  AtomUI.Desktop.Controls              6.0.7 required public
  AtomUI.Desktop.Controls.DataGrid     6.0.7 optional commercial
  AtomUI.Desktop.Controls.ColorPicker  6.0.7 optional commercial
```

产品输出：

```text
Product: desktop
Name: AtomUI Desktop

Required packages:
  AtomUI.Desktop.Controls 6.0.7

Optional packages:
  AtomUI.Desktop.Controls.DataGrid 6.0.7 commercial
  AtomUI.Desktop.Controls.ColorPicker 6.0.7 commercial

Install:
  dotnet add package AtomUI.Desktop.Controls
```

单包输出：

```text
Package: AtomUI.Desktop.Controls
Version: 6.0.7
Product: desktop
Visibility: public
Required: yes

Dependencies:
  AtomUI.Controls
  AtomUI.Generator
  Avalonia

Registration:
  UseAtomUI
  UseDesktopControls

Controls:
  Button, Select, TreeView, ...
```

### markdown

Markdown 输出用于文档和 Agent 上下文，必须包含稳定标题：

- `# AtomUI Package: <id>`
- `## Summary`
- `## Dependencies`
- `## Registration`
- `## Compatibility`
- `## Controls`
- `## Conflicts`
- `## Source`

列表模式使用 Markdown 表格按产品分组。

### json

JSON 输出示例：

```json
{
  "schemaVersion": "1.0",
  "success": true,
  "command": "package",
  "payload": {
    "schemaVersion": "1.0",
    "command": "package",
    "targetVersion": "6.0",
    "query": {
      "kind": "package",
      "target": "AtomUI.Desktop.Controls",
      "include": ["summary", "dependencies", "registration"]
    },
    "package": {
      "id": "AtomUI.Desktop.Controls",
      "version": "6.0.7",
      "productId": "desktop",
      "isCommercial": false,
      "dependencies": [],
      "registration": [],
      "controls": []
    },
    "diagnostics": []
  },
  "diagnostics": []
}
```

## 12. 错误与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。本命令可能返回：

| 错误码 | 退出码 | 触发 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | `--kind`、`--include` 非法，或选项组合冲突。 |
| `ATOMUICLI_PKG001` | `3` | 包或产品不存在，或 `--product` 与单包所属产品不匹配。 |
| `ATOMUICLI_PKG002` | `3` | 包 ID、产品 ID 或别名存在歧义。 |
| `ATOMUICLI_PKG003` | `5` | 包冲突、包版本不兼容或包组合阻止继续执行。作为诊断 finding 出现在 payload 中。 |
| `ATOMUICLI_DATA001` | `4` | package snapshot 不可用。 |
| `ATOMUICLI_DATA002` | `4` | package snapshot schema 与 CLI 不兼容。 |
| `ATOMUICLI_DATA004` | `4` | 外部或商业数据根缺失、不可读、授权缺失或授权诊断失败。 |
| `ATOMUICLI_DATA005` | `4` | `--target-version` 无法解析到可用 package snapshot。 |

错误 payload 要求：

- `NotFound` 必须包含可执行 suggestion，例如 `dotnet atomui package` 或 `dotnet atomui list packages`。
- `Ambiguous` 必须包含 candidates，并提示使用 `--kind package` 或 `--kind product`。
- `PKG003` 默认作为诊断输出；只有命令后续引入阻断模式时才作为硬错误。

## 13. AOT-first 约束

- 运行时只读取内置 `PackageSnapshot` 和显式 `--data-root` overlay。
- 不运行时扫描 `.csproj`、源码、Git 或 NuGet feed。
- 不使用反射发现命令、payload 或包模型。
- DTO 必须纳入 source generated JSON context。
- Renderer 不使用运行时模板编译。
- 依赖树构建必须使用快照中的显式依赖边，不通过程序集加载解析。
- 构建期 source processor 使用结构化 XML/MSBuild 读取，避免正则解析 `.csproj` 主体。

## 14. 测试矩阵与实现文件

测试矩阵：

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| list text | `package` | 按产品分组列出公开包。 |
| list json | `--format json package` | payload 是对象，包含 `products`、`packages`。 |
| product detail | `package desktop` | 输出 desktop 产品必选包、可选包和安装建议。 |
| product kind | `package desktop --kind product` | 只按产品解析。 |
| package detail | `package AtomUI.Desktop.Controls` | 输出单包依赖、注册、控件摘要。 |
| dependency tree | `package AtomUI.Desktop.Controls --tree` | 输出依赖树，隐式包含 dependencies。 |
| include controls | `package AtomUI.Desktop.Controls --include controls` | 输出包内控件，其他详情不展开。 |
| commercial filter | `package --commercial` | 只列商业包。 |
| product filter | `package --product desktop` | 只列 desktop 产品包。 |
| markdown | `package AtomUI.Desktop.Controls --format markdown` | 输出稳定 Markdown 标题和表格。 |
| invalid kind | `package --kind bad` | 返回 `ATOMUICLI_ARG002`。 |
| invalid include | `package --include bad` | 返回 `ATOMUICLI_ARG002`。 |
| not found | `package none` | 返回 `ATOMUICLI_PKG001`，stdout 为空。 |
| ambiguous | 构造重名 alias | 返回 `ATOMUICLI_PKG002`，带 candidates。 |
| missing version | `package --target-version 9.0` | 返回 `ATOMUICLI_DATA005`。 |
| json error | `--format json package none` | stderr 输出 JSON error envelope。 |

实现文件建议：

- `src/AtomUI.Cli.Hosting/Metadata/PackageSnapshotModels.cs`
- `src/AtomUI.Cli.Hosting/Metadata/PackageSnapshotRegistry.cs`
- `src/AtomUI.Cli.Hosting/Metadata/PackageCommandPayloads.cs`
- `src/AtomUI.Cli.Hosting/Metadata/PackageQueryService.cs`
- `src/AtomUI.Cli.Hosting/Metadata/PackageOutputRenderer.cs`
- `src/AtomUI.Cli.Hosting/Metadata/MetadataCommandOptions.cs`
- `src/AtomUI.Cli.Hosting/Metadata/MetadataCommandHandlers.cs`
- `tools/AtomUI.Cli.MetadataBuilder/SourceAnalysis/Processors/PackageSourceProcessor.cs`
- `tools/AtomUI.Cli.MetadataBuilder/SourceAnalysis/Projection/PackageSnapshotProjection.cs`
- `tools/AtomUI.Cli.MetadataBuilder/SourceAnalysis/Writers/PackageSnapshotCodeWriter.cs`
- `tests/AtomUI.Cli.Tests/FeatureModules/PackageCommandTests.cs`
