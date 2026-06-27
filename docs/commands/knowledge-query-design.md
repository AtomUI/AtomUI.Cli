# Knowledge Query Commands 详细设计

## 范围

本文档细化只读知识查询命令：

- `list`
- `info`
- `doc`
- `demo`
- `token`
- `semantic`
- `design.md`
- `package`
- `changelog`

这些命令只读取 metadata 快照和可选数据根，不读取用户项目，不访问网络，不写文件。

## 共享服务

| 服务 | 使用命令 |
| --- | --- |
| `IMetadataRootResolver` | 全部。 |
| `IVersionIndexLoader` | 全部。 |
| `IControlQueryService` | `list`、`info`、`doc`、`demo`、`token`、`semantic`、`changelog`。 |
| `IPackageQueryService` | `list`、`info`、`package`、`changelog`。 |
| `IDemoQueryService` | `demo`、`doc`。 |
| `ITokenQueryService` | `token`、`doc`、`design.md`。 |
| `ISemanticPartQueryService` | `semantic`、`doc`。 |
| `IChangelogQueryService` | `changelog`、`doc`。 |
| `IDesignDocumentQueryService` | `design.md`。 |

## 版本解析

知识查询命令统一使用 `TargetVersionResolver`：

1. `--target-version`。
2. 当前目录项目包引用中的 AtomUI 版本，仅读取项目文件，不执行 restore。
3. metadata 内置最高稳定版本。

解析结果：

```csharp
public sealed record TargetVersionInfo(
    string Version,
    string MajorVersion,
    TargetVersionSource Source);
```

如果用户提供非法版本，返回 `ATOMUICLI_ARG002`。

## 命令 DTO

### list

```csharp
public sealed record ListCommandPayload(
    string Kind,
    string TargetVersion,
    IReadOnlyList<ListItemDto> Items);
```

`Kind` 使用枚举绑定：`controls`、`products`、`packages`、`categories`。

### info

```csharp
public sealed record InfoCommandPayload(
    ControlSummaryDto Summary,
    ControlApiDto? Api,
    RegistrationDto? Registration,
    IReadOnlyList<TokenDto>? Tokens,
    IReadOnlyList<SemanticPartDto>? SemanticParts,
    IReadOnlyList<DemoSummaryDto>? Demos);
```

`--include` 控制可空字段。

### doc

```csharp
public sealed record DocCommandPayload(
    string Control,
    IReadOnlyList<DocumentSectionDto> Sections);
```

Markdown 输出直接由 `Sections` 渲染，不拼接 ad hoc 字符串。

### demo

```csharp
public sealed record DemoCommandPayload(
    string Control,
    IReadOnlyList<DemoDto> Demos);
```

示例代码来源于构建期提取的 AXAML、code-behind 和 ViewModel 片段。

### token

```csharp
public sealed record TokenCommandPayload(
    string Scope,
    string? Control,
    IReadOnlyList<TokenDto> Tokens);
```

Token 来源包括 shared token、component token 和 mapped token。

### semantic

```csharp
public sealed record SemanticCommandPayload(
    string Control,
    IReadOnlyList<SemanticPartDto> Parts,
    TemplateSummaryDto? Template);
```

semantic parts 来自 TemplatePart、PseudoClasses、template selector 和显式 metadata。

### design.md

```csharp
public sealed record DesignMarkdownCommandPayload(
    IReadOnlyList<DocumentSectionDto> Sections,
    IReadOnlyList<TokenDto> ReferencedTokens);
```

文档按 major version 存储，缺失时返回 `ATOMUICLI_DATA001`。

### package

```csharp
public sealed record PackageCommandPayload(
    IReadOnlyList<PackageDto> Packages,
    IReadOnlyList<PackageConflictDto> Conflicts,
    IReadOnlyList<RegistrationDto> RegistrationMethods);
```

### changelog

```csharp
public sealed record ChangelogCommandPayload(
    VersionRangeDto Range,
    IReadOnlyList<ChangelogEntryDto> Entries);
```

支持 query mode 和 diff mode。diff mode 比较两个版本快照中的 API、Token 和包关系。

## 错误处理

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](error-code-standard.md)。下表只列知识查询类命令可能返回的错误码子集。

| 场景 | 错误码 |
| --- | --- |
| 命令参数非法 | `ATOMUICLI_ARG002` |
| 控件不存在 | `ATOMUICLI_CTRL001` |
| 控件名歧义 | `ATOMUICLI_CTRL002` |
| 示例不存在 | `ATOMUICLI_CTRL003` |
| Token 不存在 | `ATOMUICLI_CTRL004` |
| semantic part 不存在 | `ATOMUICLI_CTRL005` |
| 包不存在 | `ATOMUICLI_PKG001` |
| 包 ID 歧义 | `ATOMUICLI_PKG002` |
| metadata 不可用 | `ATOMUICLI_DATA001` |
| schema 不兼容 | `ATOMUICLI_DATA002` |
| 外部数据 schema 不兼容 | `ATOMUICLI_DATA003` |
| 版本索引缺失 | `ATOMUICLI_DATA004` |

失败结果必须包含候选建议，候选建议使用 metadata index 计算，不访问网络。

## AOT-first 要求

- 所有 payload DTO 纳入 source generated JSON context。
- Markdown 渲染器是静态代码。
- 版本解析不依赖动态 package restore。
- diff mode 只比较快照 DTO，不加载历史程序集。

## 测试矩阵

| 命令 | 必测场景 |
| --- | --- |
| `list` | 默认 controls、product filter、since filter。 |
| `info` | include 字段裁剪、模糊建议、strict。 |
| `doc` | section 渲染和 markdown 稳定性。 |
| `demo` | 列表模式、指定 demo、代码片段语言。 |
| `token` | global/control scope、name 查询。 |
| `semantic` | part 查询、template summary。 |
| `design.md` | major version 文档选择。 |
| `package` | 依赖、冲突、注册方法。 |
| `changelog` | range query、diff mode、control/package filter。 |
