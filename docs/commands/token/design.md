# dotnet atomui token 详细设计

## 1. 命令定位

`token` 是 AtomUI Cli 的设计 Token 知识命令，不只是输出 Token 名称和值。它面向主题定制、控件样式开发、文档生成和 Agent 代码生成，负责解释 AtomUI Token 体系中的定义、派生、资源键、控件主题使用点和可定制边界。

命令必须能回答以下问题：

- AtomUI 当前版本有哪些共享 Token，分别属于 seed、map、alias 哪一层。
- 某个控件定义了哪些控件 Token，Token scope、Token 类型和资源键是什么。
- 某个控件 Token 由哪些共享 Token 派生，派生链路是否经过主题算法。
- 某个 Token 在 ControlTheme 中被哪些 Selector、Setter、模板节点消费。
- 用户在 XAML 中应该使用哪个 MarkupExtension 和资源键。
- 这个 Token 是否适合作为外部定制入口，还是只属于内部实现或生成资源。
- 商业控件包的 Token 元数据是否可见、是否受授权或产品包限制。

## 2. 设计原则

| 原则 | 说明 |
| --- | --- |
| AOT-first | 运行期只读取构建期生成的 Token Snapshot，不扫描源码、不反射控件程序集、不动态解析 XAML。 |
| Snapshot-first | 所有查询、输出、错误建议都以版本化 Snapshot 为数据源。 |
| 控件优先 | 命令输出围绕控件主题定制链路组织，而不是机械输出字段列表。 |
| 可解释 | 重要 Token 必须能解释来源、派生依赖、资源键和主题使用点。 |
| 可机器消费 | `json` 输出必须是结构化 payload，不允许只包装 text。 |
| 商业生态可扩展 | Snapshot 支持多产品包、多控件包和授权可见性。 |

## 3. 所属模块与激活

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliMetadataModule` |
| 命令分组 | knowledge |
| 读写属性 | 只读 |
| 项目上下文 | 不要求在用户项目目录内执行 |
| 写入确认 | 不需要 |
| 支持格式 | `text`、`markdown`、`json` |
| Handler 生命周期 | Transient |
| 核心查询服务生命周期 | Singleton |

命令预解析阶段识别到 `token` 后，只激活 Metadata 模块。不得因为执行 `token` 命令加载 setup、project-analysis、mcp 等无关模块。

## 4. 调用语法

```bash
dotnet atomui token
dotnet atomui token Button
dotnet atomui token Button Padding
dotnet atomui token Button Padding --chain
dotnet atomui token Button --usage
dotnet atomui token --kind seed
dotnet atomui token --category color
dotnet atomui token Button --format markdown
dotnet atomui token Button Padding --format json
```

## 5. 查询模式

| 模式 | 输入 | 说明 |
| --- | --- | --- |
| 共享 Token 总览 | `token` | 输出共享 Token 概览，按 seed、map、alias 和 category 分组。 |
| 共享 Token 过滤 | `token --kind seed`、`token --category color` | 查询共享 Token 的子集。 |
| 控件 Token 总览 | `token Button` | 输出 Button 的 Token scope、Token 类型、注册控件、控件 Token 列表、共享依赖摘要。 |
| 单 Token 详情 | `token Button Padding` | 输出指定控件 Token 的类型、资源键、派生来源、可定制边界。 |
| 使用点查询 | `token Button --usage` | 输出该控件所有 Token 在 ControlTheme 中的消费位置。 |
| 派生链查询 | `token Button Padding --chain` | 输出 seed -> map -> alias/shared -> control -> theme setter 链路。 |
| 机器消费 | `--format json` | 输出完整结构化 payload，供 Agent、IDE、MCP 或文档生成使用。 |
| 文档消费 | `--format markdown` | 输出可直接嵌入 Markdown 文档的 Token 说明。 |

## 6. 参数与选项

### 6.1 位置参数

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `control` | string | null | 不存在返回 `ATOMUICLI_CTRL001` | 控件名称。未传入时查询共享 Token。 |
| `token` | string | null | 不存在返回 `ATOMUICLI_CTRL004` | Token 名称。传入后进入单 Token 详情模式。 |

### 6.2 过滤选项

| 选项 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `--scope <shared|control|all>` | enum | 无控件时 `shared`，有控件时 `control` | 查询共享 Token、控件 Token 或两者。 |
| `--kind <seed|map|alias|control|resource|all>` | enum | `all` | 按 Token 层级过滤。 |
| `--category <color|size|font|motion|radius|shadow|spacing|state|layout|other>` | enum | null | 按产品化分类过滤。 |
| `--match <text>` | string | null | 按名称、描述、资源键、来源类型做大小写不敏感模糊匹配。 |
| `--product <id>` | string | null | 限定产品包或商业控件包。未传入时查询当前内置可见范围。 |
| `--theme <default|dark|compact|all>` | enum | `default` | 查询指定主题算法下的值或差异。 |

### 6.3 展开选项

| 选项 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `--include <items>` | csv enum | null | 可选 `usage`、`chain`、`source`、`examples`、`diagnostics`。 |
| `--usage` | bool | false | 等价于 `--include usage`。 |
| `--chain` | bool | false | 等价于 `--include chain`。 |
| `--source` | bool | false | 等价于 `--include source`。 |
| `--customizable-only` | bool | false | 只输出建议作为外部定制入口的 Token。 |
| `--used-only` | bool | false | 只输出至少存在 ControlTheme 使用点的 Token。 |
| `--strict` | bool | false | Snapshot 存在诊断告警时返回失败，供 CI 使用。 |

### 6.4 输出选项

| 选项 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `--format <text|markdown|json>` | enum | `text` | 输出格式。 |
| `--no-color` | bool | false | 禁用终端颜色。 |

非法选项值统一返回 `ATOMUICLI_ARG002`。

## 7. Options 类型

```csharp
public sealed record TokenCommandOptions(
    GlobalCliOptions Global,
    string? Control,
    string? Token,
    TokenScope Scope,
    TokenKindFilter Kind,
    TokenCategoryFilter? Category,
    string? Match,
    string? Product,
    TokenThemeFilter Theme,
    TokenIncludeFlags Include,
    bool CustomizableOnly,
    bool UsedOnly,
    bool Strict) : IAtomUICliCommandOptions;
```

`--usage`、`--chain`、`--source` 在解析阶段合并进 `TokenIncludeFlags`，Handler 不再处理多个布尔分支。

## 8. Snapshot 数据模型

### 8.1 TokenSnapshot

```csharp
public sealed record TokenSnapshot(
    string SchemaVersion,
    string SnapshotId,
    string TargetVersion,
    string SourceCommit,
    IReadOnlyList<TokenProductDocument> Products,
    IReadOnlyList<TokenDocument> SharedTokens,
    IReadOnlyList<ControlTokenSetDocument> ControlTokenSets,
    IReadOnlyList<TokenGraphEdgeDocument> GraphEdges,
    IReadOnlyList<TokenDiagnosticDocument> Diagnostics);
```

### 8.2 TokenDocument

```csharp
public sealed record TokenDocument(
    string Id,
    string Name,
    string Scope,
    string Kind,
    string Category,
    string Type,
    string? DefaultValue,
    string? Description,
    TokenResourceDocument Resource,
    TokenSourceDocument Source,
    IReadOnlyList<TokenValueVariantDocument> Values,
    IReadOnlyList<TokenGraphEdgeDocument> Dependencies,
    IReadOnlyList<TokenUsageDocument> Usages,
    TokenCustomizationDocument Customization);
```

字段语义：

| 字段 | 说明 |
| --- | --- |
| `Scope` | `shared` 或 `control`。 |
| `Kind` | `seed`、`map`、`alias`、`control`、`resource`。 |
| `Category` | 用于产品输出分组，不直接等同源码文件名。 |
| `DefaultValue` | 构建期可确定的默认值；无法静态确定时为 null，并由 `Values` 解释。 |
| `Resource` | 资源键、MarkupExtension、可复制 XAML 片段。 |
| `Source` | 定义来源、生成资源来源、主题使用来源。 |
| `Values` | 不同主题算法、产品包或版本下的值。 |
| `Dependencies` | Token 派生依赖。 |
| `Usages` | ControlTheme 消费点。 |
| `Customization` | 外部定制建议和稳定性级别。 |

### 8.3 ControlTokenSetDocument

```csharp
public sealed record ControlTokenSetDocument(
    string ControlName,
    string TokenId,
    string TokenTypeName,
    string ScopeProviderName,
    string PackageId,
    IReadOnlyList<string> RegisteredControls,
    IReadOnlyList<TokenDocument> Tokens,
    IReadOnlyList<string> SharedDependencies,
    IReadOnlyList<TokenUsageDocument> ThemeUsages);
```

### 8.4 TokenResourceDocument

```csharp
public sealed record TokenResourceDocument(
    string? ResourceKind,
    string? ResourceKey,
    string? MarkupExtension,
    string? XamlSnippet,
    string Stability);
```

示例：

```json
{
  "resourceKind": "ButtonTokenResource",
  "resourceKey": "ButtonTokenKind.Padding",
  "markupExtension": "{atom:ButtonTokenResource Padding}",
  "xamlSnippet": "Padding=\"{atom:ButtonTokenResource Padding}\"",
  "stability": "generated-public"
}
```

### 8.5 TokenUsageDocument

```csharp
public sealed record TokenUsageDocument(
    string ThemeName,
    string Selector,
    string TargetProperty,
    string? TargetNode,
    string ResourceExpression,
    TokenSourceLocationDocument SourceLocation);
```

`TargetNode` 用于记录模板节点，例如 `Frame`、`PART_RootLayout`、`PART_LoadingIcon`。纯 Setter 没有节点时允许为 null。

### 8.6 TokenCustomizationDocument

```csharp
public sealed record TokenCustomizationDocument(
    string Level,
    string Recommendation,
    IReadOnlyList<string> Notes);
```

`Level` 取值：

| Level | 说明 |
| --- | --- |
| `public-stable` | 推荐作为外部主题定制入口。 |
| `public-generated` | 可通过生成资源键访问，但需要跟随版本变更。 |
| `internal` | 内部实现 Token，不建议用户直接依赖。 |
| `not-recommended` | 存在兼容性或语义风险，不建议作为定制入口。 |

## 9. 数据来源与构建期抽取架构

### 9.1 数据来源硬规则

`token` 命令的数据来源必须是 AtomUI 项目源码的构建期抽取结果，不能由 CLI 运行时扫描源码，也不能在 `TokenSnapshotRegistry`、查询服务或命令 handler 中手写控件 Token 数据。

允许存在的运行时数据只有构建期生成的 `TokenSnapshot`。当前实现采用编译期 C# 生成：`AtomUI.Cli.Hosting.csproj` 在 `CoreCompile` 前执行 `tools/AtomUI.Cli.MetadataBuilder`，生成 `output/obj/AtomUI.Cli.Hosting/Generated/Metadata/BuiltInTokenSnapshot.g.cs`。运行时由 `TokenSnapshotRegistry.CreateDefault()` 调用生成的 `BuiltInTokenSnapshotFactory.Create()`。

生成文件是构建产物，不提交到源码目录，不要求用户安装 .NET Tool 后本机仍存在 AtomUI 源码。发布态 CLI 的 `token` 命令只读取编译进 `AtomUI.Cli.Hosting` 程序集的 snapshot。

### 9.2 构建链路

```text
AtomUI source root
  -> tools/AtomUI.Cli.MetadataBuilder
  -> output/obj/AtomUI.Cli.Hosting/Generated/Metadata/BuiltInTokenSnapshot.g.cs
  -> AtomUI.Cli.Hosting compile
  -> TokenSnapshotRegistry.CreateDefault()
  -> TokenQueryService
  -> token command payload/output
```

`AtomUI.Cli.Hosting.csproj` 负责挂载构建 target：

- target 名称：`GenerateBuiltInTokenSnapshot`。
- 执行时机：`BeforeTargets="CoreCompile"`。
- 输出文件：`$(BaseIntermediateOutputPath)Generated/Metadata/BuiltInTokenSnapshot.g.cs`。
- 生成器项目：`tools/AtomUI.Cli.MetadataBuilder/AtomUI.Cli.MetadataBuilder.csproj`。
- 生成失败时，`AtomUI.Cli.Hosting` 编译必须失败。

### 9.3 源码根解析

默认引用源码位置遵守项目标准：`<AtomUICliRepoRoot>/.workspace/AtomUI`。CI 使用同样相对路径或通过显式参数覆盖。

源码根解析优先级：

1. `AtomUISourceRoot` MSBuild 属性或 MetadataBuilder 的 `--source-root`。
2. `ATOMUI_SOURCE_ROOT` 环境变量。
3. `<AtomUICliRepoRoot>/.workspace/AtomUI`。

缺失源码根时构建失败，错误信息必须提示配置 `--source-root` 或 `ATOMUI_SOURCE_ROOT`。源码根路径不得写入命令输出，snapshot 中只能记录相对源码路径和 `sourceCommit`。

### 9.4 字段权威来源

抽取器读取以下信息。字段产生冲突时，以本表的权威来源为准：

| 来源 | 抽取内容 |
| --- | --- |
| `src/AtomUI.Core/Theme/TokenSystem/TokenDefinitions/DesignToken.*.cs` | 共享 Token 名称、类型、默认值、seed/map/alias 层级、XML 注释、源码行号。 |
| `src/**/**Token.cs` 且包含 `[ControlDesignToken]` / `AbstractControlDesignToken` | 控件 Token 类型、Token ID、ScopeProvider、Token 属性、XML 注释、源码行号。 |
| `CalculateTokenValues` | 控件 Token 对 `SharedToken.X` 的依赖、控件 Token 内部派生、局部变量派生、计算值来源。 |
| `src/**/GeneratedFiles/**/TokenResourceConst.g.cs` | `SharedTokenKind`、控件 `*TokenKind`、资源键完整性校验、生成资源来源路径。 |
| `src/**/Themes/*.axaml` | Token 资源表达式、ControlTheme 文件名、Selector、Setter、模板节点、目标属性。 |
| 控件源码中的 `RegisterTokenResourceScope(*Token.ScopeProvider)` | 一个 Token scope 覆盖哪些控件。 |
| 控件源码所在 `.csproj` | 包名、产品线、商业包标记的初始推断。 |
| AtomUI 源码 Git HEAD | `sourceCommit` 和 `snapshotId`。 |

### 9.5 Token 描述来源

Token 描述是 `token` 命令的一等输出字段，必须来自构建期源码抽取，不允许在运行时 renderer、query service、handler 或 `TokenSnapshotRegistry` 中手写补丁。

描述字段来源优先级：

1. Token 属性上方的 XML `<summary>` 注释。
2. 同一个 Token 属性连续多行 XML 注释合并后的纯文本。
3. 如果源码没有描述，Snapshot 保留空描述，并输出 `ATOMUICLI_TOKEN_DESCRIPTION_MISSING` warning 诊断。

描述清洗规则：

- 去掉 `///`、`<summary>`、`</summary>` 等 XML 包装。
- 多行描述合并为一个空格分隔的句子。
- 不在运行期基于 Token 名称自动生成描述，避免误导用户。
- text、markdown、json 输出都必须保留描述字段；text 和 markdown 的默认输出必须展示描述，不依赖 `--detail`。

### 9.6 Snapshot 字段要求

生成的 `TokenSnapshot` 必须至少包含：

- `SnapshotId`：格式为 `atomui-token-source-<commit>`。
- `SourceCommit`：AtomUI 源码仓库当前 HEAD 的短 commit。
- `SharedTokens`：从共享 Token 源码抽取，不允许只保留命令示例所需的子集。
- `ControlTokenSets`：从所有可见控件 Token 源码抽取，不允许只手写 Button 或商业控件样例。
- `TokenDocument.Source`：记录相对源码路径和源码行号。
- `TokenDocument.Description`：记录源码 XML `<summary>` 说明，缺失时记录诊断。
- `TokenDocument.Dependencies`：记录共享 Token 和控件 Token 派生依赖。
- `TokenDocument.Usages`：记录 ControlTheme 中的资源使用点。
- `Diagnostics`：记录抽取过程中的缺失资源键、源码和生成枚举不一致等问题。

### 9.7 运行时边界

运行期不得读取上述源码文件。Snapshot 作为 CLI 发布产物的一部分编译进 `AtomUI.Cli.Hosting` 程序集。

运行时禁止：

- 根据控件名称拼接 Token 名称。
- 从 AtomUI 控件程序集反射读取 Token 类型。
- 解析 `*Token.cs`、`TokenResourceConst.g.cs` 或 `*.axaml`。
- 在查询服务中补齐遗漏字段。
- 为了让测试通过而在 `TokenSnapshotRegistry` 中增加手写控件数据。

## 10. 领域服务设计

| 服务 | 生命周期 | 职责 |
| --- | --- | --- |
| `ITokenSnapshotRegistry` | Singleton | 加载内置 Snapshot，按版本、产品包、可见性建立索引。 |
| `ITokenQueryService` | Singleton | 处理 scope、kind、category、match、theme、product 过滤。 |
| `ITokenGraphService` | Singleton | 查询 Token 派生链和依赖图。 |
| `ITokenUsageQueryService` | Singleton | 查询 ControlTheme 使用点。 |
| `ITokenCommandPayloadFactory` | Transient | 根据查询模式组装命令 payload。 |
| `ITokenOutputRenderer` | Singleton | 渲染 text、markdown、json。 |
| `TokenCommandHandler` | Transient | 参数校验、调用 payload factory、返回输出结果。 |

服务之间的依赖方向：

```text
TokenCommandHandler
  -> ITokenCommandPayloadFactory
      -> ITokenQueryService
      -> ITokenGraphService
      -> ITokenUsageQueryService
          -> ITokenSnapshotRegistry
  -> ITokenOutputRenderer
```

Handler 不直接读取 Snapshot，也不拼接业务输出。

## 11. 查询流程

1. 解析位置参数和选项，合并 `--usage`、`--chain`、`--source` 到 include flags。
2. 根据 `control` 是否存在决定默认 scope。
3. 通过 `ITokenSnapshotRegistry` 解析当前可用 Snapshot。
4. 如果传入 `control`，解析控件别名和产品包可见性。
5. 如果传入 `token`，进入单 Token 详情查询；否则进入列表查询。
6. 应用 `scope`、`kind`、`category`、`match`、`theme`、`product` 过滤。
7. 根据 include flags 补齐 usage、chain、source、examples、diagnostics。
8. 如果 `--strict` 且 Snapshot 存在 error 级诊断，返回 `ATOMUICLI_DATA003`。
9. 构造结构化 payload。
10. 根据 `--format` 渲染输出。

## 12. 输出 Payload

```csharp
public sealed record TokenCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string SnapshotId,
    TokenQuerySummaryDto Query,
    ControlTokenSummaryDto? Control,
    IReadOnlyList<TokenGroupDto> Groups,
    IReadOnlyList<TokenDocumentDto> Tokens,
    IReadOnlyList<TokenGraphEdgeDto> Graph,
    IReadOnlyList<TokenUsageDto> Usages,
    IReadOnlyList<DiagnosticDto> Diagnostics,
    IReadOnlyList<NextActionDto> NextActions);
```

`json` 输出必须完整保留 payload。`text` 和 `markdown` 是 payload 的视图，不允许成为数据源。

## 13. Text 输出规范

### 13.1 共享 Token 总览

```text
AtomUI Tokens 6.0

Shared tokens
  Seed
    ColorPrimary        Color        #1677ff
      Description: 品牌主色。
    FontSize            double       14
      Description: 基础字号。
    ControlHeight       double       32
      Description: 基础控件高度。

  Alias
    ColorText           Color        derived
      Description: 文本主色。
    ColorBgContainer    Color        derived
      Description: 容器背景色。

Run:
  dotnet atomui token --kind seed
  dotnet atomui token Button
```

### 13.2 控件 Token 总览

```text
AtomUI Token: Button

Token scope
  Token id: Button
  Token type: AtomUI.Desktop.Controls.ButtonToken
  Resource: ButtonTokenResource / ButtonTokenKind
  Registered controls: Button, IconButton, HyperLinkButton, SplitButton

Layout
  Padding              Thickness
    Description: 按钮内间距。
    Source: derived from Shared.PaddingContentHorizontal, Shared.LineWidth, Shared.ControlHeight, Button.ContentLineHeight
    XAML: {atom:ButtonTokenResource Padding}
  PaddingLG            Thickness
    Description: 大号按钮内间距。
    Source: derived from Shared.ControlPaddingHorizontal, Shared.LineWidth, Shared.ControlHeightLG, Button.ContentLineHeightLG
    XAML: {atom:ButtonTokenResource PaddingLG}
  ContentFontSize      double
    Description: 按钮内容字体大小。
    Source: from shared FontSize
    XAML: {atom:ButtonTokenResource ContentFontSize}

Color
  DefaultColor         Color
    Description: 默认按钮文本颜色。
    Source: from shared ColorText
    XAML: {atom:ButtonTokenResource DefaultColor}
  DefaultBg            Color
    Description: 默认按钮背景色。
    Source: from shared ColorBgContainer
    XAML: {atom:ButtonTokenResource DefaultBg}
  DefaultHoverColor    Color
    Description: 默认按钮悬浮文本颜色。
    Source: from shared ColorPrimaryHover
    XAML: {atom:ButtonTokenResource DefaultHoverColor}

Run:
  dotnet atomui token Button Padding --chain
  dotnet atomui token Button --usage
  dotnet atomui token Button --format markdown
```

### 13.3 单 Token 详情

```text
Button.Padding

Description: 按钮内间距。
Type: Thickness
Resource key: ButtonTokenKind.Padding
XAML: {atom:ButtonTokenResource Padding}
Customization: public-stable

Value source:
  calculated by ButtonToken.CalculateTokenValues

Depends on:
  Shared.ControlPaddingHorizontal
  Shared.LineWidth

Used by:
  ButtonTheme
    Selector: ^[SizeType=Middle] ^:not(^[Shape=Circle])
    Setter: Padding
    Resource: {atom:ButtonTokenResource Padding}
```

## 14. Markdown 输出规范

Markdown 输出用于文档生成，必须包含标题、摘要、Token 表格和可复制代码块。

````markdown
# Button Tokens

## Scope

| Field | Value |
| --- | --- |
| Token id | Button |
| Resource | ButtonTokenResource / ButtonTokenKind |

## Layout

| Token | Type | Description | Source | XAML |
| --- | --- | --- | --- | --- |
| Padding | Thickness | 按钮内间距。 | derived from Shared.PaddingContentHorizontal, Shared.LineWidth, Shared.ControlHeight, Button.ContentLineHeight | `{atom:ButtonTokenResource Padding}` |

## Usage

| Theme | Selector | Property | Resource |
| --- | --- | --- | --- |
| ButtonTheme | `^[SizeType=Middle]` | Padding | `{atom:ButtonTokenResource Padding}` |

```xml
<Setter Property="Padding" Value="{atom:ButtonTokenResource Padding}" />
```
````

## 15. JSON 输出规范

JSON 输出服务于 Agent、IDE、MCP 和 CI，不做字段裁剪。示例：

```json
{
  "schemaVersion": "1.0",
  "command": "token",
  "targetVersion": "6.0",
  "snapshotId": "atomui-token-6.0",
  "query": {
    "control": "Button",
    "token": "Padding",
    "scope": "control",
    "include": ["chain", "usage"]
  },
  "control": {
    "name": "Button",
    "tokenId": "Button",
    "resourceKind": "ButtonTokenResource"
  },
  "tokens": [
    {
      "name": "Padding",
      "scope": "control",
      "kind": "control",
      "category": "layout",
      "type": "Thickness",
      "resource": {
        "resourceKey": "ButtonTokenKind.Padding",
        "markupExtension": "{atom:ButtonTokenResource Padding}"
      },
      "customization": {
        "level": "public-stable"
      }
    }
  ]
}
```

## 16. 错误与退出码

错误码、退出码、stdout/stderr 边界遵守 [错误码标准](../error-code-standard.md)。

| 错误码 | 退出码 | 触发场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | `--scope`、`--kind`、`--theme`、`--include`、`--format` 非法。 |
| `ATOMUICLI_CTRL001` | `3` | 控件不存在或当前产品包不可见。 |
| `ATOMUICLI_CTRL004` | `3` | 控件存在，但指定 Token 不存在。 |
| `ATOMUICLI_DATA001` | `4` | Token Snapshot 不存在或无法加载。 |
| `ATOMUICLI_DATA003` | `4` | Snapshot 内部引用断裂，例如 usage 指向不存在的 Token。 |
| `ATOMUICLI_DATA004` | `4` | 商业控件 Token 元数据不可用或授权不可见。 |

错误输出必须提供建议。例如 Token 不存在时输出相近 Token 名，控件不存在时输出相近控件名。

## 17. 排序与匹配

- 控件名、Token 名、资源键匹配使用 ordinal ignore case。
- 输出排序优先级：scope -> kind -> category -> 控件名 -> Token 名。
- category 内部顺序固定为：color、size、font、motion、radius、shadow、spacing、state、layout、other。
- `--match` 同时匹配 Token 名、描述、资源键、MarkupExtension、来源类型。
- `--customizable-only` 在所有过滤之后执行。
- `--used-only` 只保留至少存在 ControlTheme usage 的 Token。

## 18. AOT 与 JSON 约束

- Snapshot DTO、命令 payload DTO、错误 DTO 必须进入 source generated JSON context。
- 不使用运行期反射枚举 Token 类型。
- 不使用动态 LINQ、表达式编译或运行期源码解析。
- Snapshot schema 必须带版本号，新增字段只能向后兼容读取；项目未发布前可直接调整命令参数。
- Text 和 Markdown renderer 不依赖反射读取对象属性，必须显式渲染字段。

## 19. 测试设计

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| 共享总览 | `token` | 输出 shared token，包含 seed/map/alias 分组。 |
| kind 过滤 | `token --kind seed` | 只输出 seed token。 |
| 控件总览 | `token Button` | 输出 Token scope、Token 类型、资源键、控件 Token 分组。 |
| 单 Token | `token Button Padding` | 输出资源键、XAML、定制建议。 |
| 派生链 | `token Button Padding --chain` | 输出 shared -> control -> usage 链路。 |
| 使用点 | `token Button --usage` | 输出 ControlTheme selector、setter、资源表达式。 |
| Markdown | `token Button --format markdown` | 输出合法 Markdown 表格和代码块。 |
| JSON | `token Button Padding --format json` | 输出完整结构化 payload。 |
| 控件不存在 | `token Missing` | 返回 `ATOMUICLI_CTRL001` 和候选建议。 |
| Token 不存在 | `token Button Missing` | 返回 `ATOMUICLI_CTRL004` 和候选建议。 |
| Snapshot 缺失 | 内置资源缺失 | 返回 `ATOMUICLI_DATA001`。 |
| Strict 诊断 | `token --strict` 且 Snapshot 有 error | 返回 `ATOMUICLI_DATA003`。 |
