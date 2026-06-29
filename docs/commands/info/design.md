# dotnet atomui info 详细设计

## 1. 命令定位

`info` 是 AtomUI 控件信息查询命令，用于在研发者或 Agent 编写控件代码前，快速确认一个控件的身份、包归属、XAML 使用方式、公开 API、主题模板契约、视觉状态、控件 Token 和示例索引。

`info` 不做完整文档展开，也不输出示例源码全集。完整说明交给 `doc`，示例源码交给 `demo`，Token 全量查询交给 `token`，模板和语义结构深查交给 `semantic`。

默认输出必须是终端友好的纯文本。只有显式传入 `--format markdown` 时才输出 Markdown，显式传入 `--format json` 时输出结构化 JSON。

## 2. 设计目标

`info <control>` 需要回答这些问题：

| 问题 | 输出区块 |
| --- | --- |
| 这个控件属于哪个产品、包、分类和命名空间？ | Identity |
| 这个控件在 XAML 里如何最小化使用？ | Usage |
| 用户应该优先使用哪些公开属性、方法和事件？ | API |
| 哪些 API 是 Gallery 已整理的常用契约，哪些是源码中公开但不推荐优先展示的能力？ | API tier |
| 自定义主题时必须知道哪些 `PART_*` 模板部件？ | Template |
| 控件有哪些 pseudo class 和视觉状态？ | States |
| 控件受哪些共享 Token 和控件 Token 影响？ | Tokens |
| 哪里能继续查看示例、Token 或语义结构？ | Related commands |
| 可选包或商业控件是否需要额外提示？ | Diagnostics |

## 3. 命令边界

| 命令 | 负责内容 |
| --- | --- |
| `info` | 控件契约摘要，适合快速理解和代码生成前置查询。 |
| `doc` | 面向人的完整控件说明、使用建议、限制和最佳实践。 |
| `demo` | 示例列表、XAML/C# 示例源码和 Gallery 场景。 |
| `token` | 全量共享 Token、控件 Token、默认值和资源键查询。 |
| `semantic` | 模板结构、语义部件、选择器和自定义主题深查。 |

`info` 默认不输出继承自 Avalonia 的大量基础属性，也不输出内部属性。需要扩展时通过 `--detail` 或 `--include` 明确请求。

## 4. 所属模块与运行方式

| 字段 | 设计 |
| --- | --- |
| 所属模块 | `AtomUICliMetadataModule` |
| 命令分组 | `knowledge` |
| 读写属性 | 只读 |
| 项目上下文 | 不要求当前目录是 AtomUI 项目 |
| 模块激活 | 命令预解析命中 `info` 后，只激活 Core 和 Metadata 相关模块 |
| Handler 生命周期 | Transient |
| Metadata 生命周期 | Singleton 静态快照服务，查询过程无运行时程序集扫描 |
| 输出格式 | `text`、`markdown`、`json` |

`info` 必须符合 AOT-first 约束：运行时不反射控件程序集，不扫描本机源码目录，不解析 XAML 文件。所有控件信息来自随 CLI 分发的静态 metadata snapshot，后续通过构建期提取器生成。

## 5. 调用语法

```bash
dotnet atomui info <control> [options]
```

常用示例：

```bash
dotnet atomui info Button
dotnet atomui info Button --detail
dotnet atomui info Button --include usage,api,template,states,tokens,demos
dotnet atomui info Button --format markdown
dotnet atomui info Button --format json
dotnet atomui info DataGrid --product datagrid --detail
dotnet atomui info Button --lang zh-CN
```

## 6. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `control` | string | 必填 | 缺失返回 `ATOMUICLI_ARG001` | 控件名或别名，例如 `Button`、`DataGrid`。 |
| `--include` | enum list | all visible sections | 非法项返回 `ATOMUICLI_ARG002` | 限定输出区块；未指定时展示默认完整控件信息。 |
| `--strict` | bool | false | 无 | 启用严格名称匹配，不做模糊建议匹配。 |
| `--product` | global string | all visible | 未知产品返回 `ATOMUICLI_PKG001` | 限定查询产品，例如 `desktop`、`datagrid`。 |
| `--detail` | global bool | false | 无 | 输出完整控件契约，包括扩展 API、模板来源、Token 资源键和诊断信息。 |
| `--format` | global enum | `text` | 非法值返回 `ATOMUICLI_ARG002` | 可选 `text`、`markdown`、`json`。 |
| `--lang` | global string | `zh` | 不支持时回退默认语言并输出 warning | 控件描述、API 描述和示例标题的语言。 |
| `--target-version` | global string | bundled metadata version | 版本不存在返回 `ATOMUICLI_DATA002` | 查询指定 AtomUI metadata 版本。 |
| `--data-root` | global path | null | 路径不可用返回 `ATOMUICLI_DATA001` | 使用外部 metadata 根目录。 |

`--include` 支持值：

| 值 | 含义 |
| --- | --- |
| `identity` | 控件身份、产品、包、命名空间、分类、状态。 |
| `usage` | XAML 命名空间和最小使用片段。 |
| `api` | Gallery curated API 和必要的公开 API。 |
| `events` | `api` 的兼容别名，事件详细建模后再独立输出。 |
| `methods` | `api` 的兼容别名，方法详细建模后再独立输出。 |
| `template` | `PART_*` 模板部件、主题入口和自定义主题提示。 |
| `states` | pseudo class、选择器状态和属性驱动状态。 |
| `tokens` | 共享 Token、控件 Token 和资源键摘要。 |
| `demos` | Gallery 示例索引。 |
| `related` | 建议继续执行的 CLI 命令。 |
| `diagnostics` | 可选包、商业控件、metadata 缺失、过期项等提示。 |
| `all` | 除内部实现信息外的全部区块。 |

显式传入 `--include` 时，输出固定保留控件身份区块，并只渲染请求的其他区块。例如 `--include tokens` 只输出身份摘要和 Token 区块。

## 7. 输出格式规则

### 7.1 默认纯文本

未指定 `--format` 时输出纯文本。纯文本不使用 Markdown 标题、表格语法或代码围栏，必须适合终端直接阅读。

```text
Button - General
Package: AtomUI.Desktop.Controls
Namespace: AtomUI.Desktop.Controls
Base type: Avalonia.Controls.Button
Product: desktop
Gallery: General/Button
Status: Stable

Trigger actions and express intent with clear visual priority.

Usage:
  <atom:Button ButtonType="Primary" Content="Save" />

API:
  ButtonType     ButtonType           Default  Styled  Sets visual button type.
  SizeType       CustomizableSizeType  Middle   Styled  Controls height and padding density.
  Shape          ButtonShape          Default  Styled  Changes button shape.
  Icon           Icon?                null     Styled  Displays an icon.
  IsLoading      bool                 false    Styled  Shows loading state.

Template parts:
  PART_WaveSpirit        WaveSpiritDecorator
  PART_RootLayout        DockPanel
  PART_LoadingIcon       LoadingOutlined
  PART_ButtonIcon        IconPresenter
  PART_ContentPresenter  ContentPresenter

States:
  :icononly, :loading, :danger, :default, :dashed, :primary, :link, :text

Tokens:
  ColorPrimary   Shared   Stable
  ControlHeight  Shared   Stable
  ButtonToken    Control  Mapped

Related:
  dotnet atomui demo Button
  dotnet atomui token Button
  dotnet atomui semantic Button
```

### 7.2 Markdown

显式传入 `--format markdown` 时输出 Markdown。Markdown 用于复制到设计文档、开发说明或 Agent 上下文。

````markdown
# Button

| Field | Value |
| --- | --- |
| Category | General |
| Package | AtomUI.Desktop.Controls |
| Namespace | AtomUI.Desktop.Controls |
| Base type | Avalonia.Controls.Button |
| Product | desktop |
| Status | Stable |

## Usage

```xml
<atom:Button ButtonType="Primary" Content="Save" />
```

## API

| Property | Type | Default | Kind | Description |
| --- | --- | --- | --- | --- |
| ButtonType | ButtonType | Default | Styled | Sets visual button type. |
| SizeType | CustomizableSizeType | Middle | Styled | Controls height and padding density. |
````

Markdown 输出必须保持标题顺序稳定：Identity、Usage、API、Template、States、Tokens、Demos、Diagnostics、Related。

### 7.3 JSON

显式传入 `--format json` 时输出 DTO。JSON 用于工具、MCP 和 Agent 结构化消费，不做文本渲染压缩。

```json
{
  "schemaVersion": "1.0",
  "command": "info",
  "targetVersion": "1.0.0-alpha.1",
  "control": {
    "name": "Button",
    "categoryId": "general",
    "packageId": "AtomUI.Desktop.Controls",
    "productId": "desktop"
  }
}
```

## 8. 控件信息模型

```csharp
public sealed record InfoCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    InfoControlIdentityDto Control,
    InfoControlTypeDto Type,
    InfoControlDescriptionDto Description,
    InfoControlUsageDto Usage,
    IReadOnlyList<InfoApiMemberDto> Api,
    InfoTemplateContractDto Template,
    IReadOnlyList<InfoControlStateDto> States,
    IReadOnlyList<InfoTokenSummaryDto> Tokens,
    IReadOnlyList<InfoDemoSummaryDto> Demos,
    IReadOnlyList<InfoRelatedCommandDto> RelatedCommands,
    IReadOnlyList<InfoDiagnosticDto> Diagnostics);
```

### 8.1 控件身份

```csharp
public sealed record InfoControlIdentityDto(
    string Name,
    string DisplayName,
    string CategoryId,
    string CategoryName,
    string ProductId,
    string PackageId,
    bool IsCommercial,
    bool IsOptionalPackage,
    string GalleryRoute,
    string Status);
```

设计要求：

- `Name` 使用稳定控件名，例如 `Button`。
- `DisplayName` 面向显示，可以与 `Name` 相同。
- `GalleryRoute` 使用逻辑路由，不输出本机源码路径。
- 商业控件必须通过 `IsCommercial` 和 `IsOptionalPackage` 明确标记。

### 8.2 类型信息

```csharp
public sealed record InfoControlTypeDto(
    string Namespace,
    string AssemblyName,
    string TypeName,
    string BaseType,
    IReadOnlyList<string> Interfaces,
    string XamlNamespace,
    string XamlPrefix);
```

类型信息用于回答控件应该从哪个包引用、XAML 命名空间如何声明、C# 类型名是什么。`Interfaces` 只展示 AtomUI 相关接口，不展示所有运行时接口。

### 8.3 描述信息

```csharp
public sealed record InfoControlDescriptionDto(
    string Subtitle,
    string Summary,
    string UsageGuidance);
```

描述来自随 metadata 分发的本地化资源。`--lang` 指定语言不可用时回退默认语言，并在 `Diagnostics` 输出一条 warning。

### 8.4 使用片段

```csharp
public sealed record InfoControlUsageDto(
    IReadOnlyList<string> RequiredPackages,
    IReadOnlyList<string> RequiredNamespaces,
    string XamlSnippet,
    string? CSharpSnippet);
```

默认片段必须最小可读，不要在 `info` 中输出完整示例。复杂示例通过 `demo` 查询。

## 9. API 契约模型

```csharp
public sealed record InfoApiMemberDto(
    string Name,
    string MemberKind,
    string Type,
    string DefaultValue,
    string PropertyKind,
    string OwnerType,
    bool IsBindable,
    bool IsInherited,
    bool IsCurated,
    bool IsDeprecated,
    string? Since,
    string? Replacement,
    string Description);
```

字段规则：

| 字段 | 规则 |
| --- | --- |
| `MemberKind` | `property`、`event`、`method`、`attached-property`。 |
| `PropertyKind` | `styled`、`direct`、`clr`、`attached`、`method`、`event`。 |
| `IsCurated` | 来自 Gallery API 表或人工整理规则时为 true。默认纯文本优先展示 true 的行。 |
| `IsInherited` | 继承自 Avalonia 或基类时为 true。默认不展示，`--detail` 可展示。 |
| `IsDeprecated` | 已废弃时为 true，同时必须给出 `Replacement` 或诊断说明。 |

API 分层：

| 层级 | 默认输出 | 来源 |
| --- | --- | --- |
| Curated API | 是 | Gallery API 表和控件 metadata 人工规则。 |
| Public AtomUI API | `--detail` 输出 | 控件公开属性、方法和事件的构建期提取结果。 |
| Inherited API | 默认不输出 | 只在明确 include 时输出关键继承能力。 |
| Internal API | 不输出 | 任何 `internal`、私有或主题内部状态都不进入 `info`。 |

## 10. 模板契约模型

```csharp
public sealed record InfoTemplateContractDto(
    IReadOnlyList<string> ThemeFiles,
    IReadOnlyList<InfoTemplatePartDto> Parts,
    IReadOnlyList<string> Notes);

public sealed record InfoTemplatePartDto(
    string Name,
    string Type,
    bool IsRequired,
    string Source,
    string Description);
```

模板契约是 AtomUI 控件查询的一等信息。原因是 Avalonia 自定义主题依赖 `PART_*` 名称、模板绑定和状态选择器。`info` 默认输出主要模板部件，`--detail` 输出来源主题文件、是否必需和说明。

模板区块不得输出本机绝对源码路径，只输出逻辑来源，例如 `AtomUI.Desktop.Controls/Buttons/Themes/ButtonTheme.axaml`。

## 11. 状态模型

```csharp
public sealed record InfoControlStateDto(
    string Name,
    string Kind,
    string Description,
    string Source);
```

`Kind` 支持：

| 值 | 含义 |
| --- | --- |
| `pseudo-class` | 来自 Avalonia `[PseudoClasses]` 或主题 selector。 |
| `property-state` | 由控件属性驱动的视觉状态，例如 `ButtonType=Primary`。 |
| `interaction-state` | 指针悬浮、按下、禁用、焦点等交互状态。 |
| `validation-state` | 输入控件的校验状态。 |

默认输出 pseudo class 和最重要的属性状态。`--detail` 输出全部状态及来源。

## 12. Token 模型

```csharp
public sealed record InfoTokenSummaryDto(
    string Name,
    string Scope,
    string Type,
    string Status,
    string? DefaultValue,
    string? ResourceKey,
    string Description);
```

`Scope` 支持：

| 值 | 含义 |
| --- | --- |
| `shared` | 全局共享 Token。 |
| `control` | 控件 Token。 |
| `mapped` | 从共享 Token 映射得到的控件 Token。 |

默认输出 Top tokens，不输出全量 Token。完整 Token 表由 `token <control>` 负责。

## 13. 示例索引模型

```csharp
public sealed record InfoDemoSummaryDto(
    string Name,
    string Title,
    string Description,
    string Route,
    IReadOnlyList<string> Tags);
```

`info` 只展示示例索引和可继续执行的命令，不输出示例源码：

```text
Demos:
  basic     Button type
  icon      Shape and icon
  loading   Loading

Run `dotnet atomui demo Button basic` to view demo source.
```

## 14. 数据来源与生成策略

CLI 运行时只读取静态 metadata。metadata 的长期生成方案如下：

| 数据 | 构建期来源 | 运行时用途 |
| --- | --- | --- |
| 控件分类、顺序、路由 | Gallery 导航配置 | Identity、list、related route。 |
| 控件摘要和描述 | Gallery 本地化资源 | Description、API 描述、demo 标题。 |
| Curated API | Gallery ViewModel 的 API 行 | 默认 API 表。 |
| Curated Token | Gallery ViewModel 的 Token 行 | 默认 Token 摘要。 |
| Public AtomUI API | 控件 C# 源码的公开属性、方法、事件 | `--detail` API。 |
| Base type 和接口 | 控件 C# 源码 | Type。 |
| Pseudo classes | 控件 C# 属性和主题 selector | States。 |
| Template parts | 控件主题 XAML | Template。 |
| Token resource keys | 控件主题 XAML 和 Token 类型 | Tokens。 |
| Demo 索引 | Gallery 示例资源 | Demos。 |

metadata 生成器可以是独立构建工具或仓库内工具项目，但 CLI 发布包中只携带生成后的快照。这样保证：

- AOT 发布不依赖运行时反射。
- 用户安装 .NET Tool 后不需要本机存在 AtomUI 源码。
- 商业控件 metadata 可以按产品包独立合并。
- 不同目标版本可以通过 metadata 版本切换。

## 15. 查询流程

1. 解析全局选项，默认 `Format=Text`。
2. 命令预解析识别 `info`，只激活 Core 和 Metadata 模块。
3. 校验 `control` 参数，缺失返回 `ATOMUICLI_ARG001`。
4. 校验 `--include`、`--format`、`--product`、`--target-version`。
5. 根据控件名、产品过滤和 strict 规则解析控件。
6. 不存在时返回 `ATOMUICLI_CTRL001`，并给出近似控件名建议。
7. 多产品或多包同名时返回 `ATOMUICLI_CTRL002`，并列出候选项。
8. 根据 include 和 detail 组装 `InfoCommandPayload`。
9. 根据格式输出：
   - `text`：`InfoOutputRenderer.RenderText(payload, detail)`。
   - `markdown`：`InfoOutputRenderer.RenderMarkdown(payload, detail)`。
   - `json`：直接输出 payload。

## 16. 错误与诊断

| 错误码 | 场景 | 处理 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | 缺少控件名 | 输出 `dotnet atomui info <control>` 用法。 |
| `ATOMUICLI_ARG002` | include、format 或 lang 值非法 | 输出合法值。 |
| `ATOMUICLI_CTRL001` | 控件不存在 | 输出 suggestions。 |
| `ATOMUICLI_CTRL002` | 控件名歧义 | 输出候选产品和包。 |
| `ATOMUICLI_PKG001` | product 或 package 不存在 | 输出可用产品列表。 |
| `ATOMUICLI_DATA001` | metadata 无法加载 | 输出 data root 和恢复建议。 |
| `ATOMUICLI_DATA002` | target version 不存在 | 输出可用版本。 |
| `ATOMUICLI_DATA004` | 商业控件 metadata 不可用 | 输出安装或授权提示。 |

非致命问题写入 `Diagnostics`：

- 目标语言不可用并发生回退。
- 控件属于可选包。
- 控件属于商业产品。
- 某些区块 metadata 缺失。
- API 已废弃或存在替代项。

## 17. 渲染规则

纯文本渲染：

- 第一行固定为 `<ControlName> - <CategoryName>`。
- 字段使用 `Key: Value`。
- 表格使用空格对齐，不使用 Markdown 管道。
- 命令建议使用可复制命令。
- 默认最多展示 8 条 API、5 条 Token、5 条 demo；`--detail` 不截断。

Markdown 渲染：

- 第一行固定为 `# <ControlName>`。
- Identity 使用 Markdown 表格。
- Usage 使用 `xml` 或 `csharp` 代码块。
- API、Template、States、Tokens、Demos 使用 Markdown 表格。
- Related commands 使用 `bash` 代码块。

JSON 渲染：

- 字段命名使用 camelCase。
- 未请求区块输出空数组或空对象，不输出无法解释的 `null`。
- 所有 DTO 纳入 JSON source generation。
- JSON 不包含渲染后的纯文本或 Markdown。

## 18. AOT-first 约束

实现必须满足：

- include parser 使用静态映射，不依赖反射枚举。
- payload DTO 使用 source-generated JSON metadata。
- metadata snapshot 使用显式 record 类型。
- 不在运行时加载 AtomUI 控件程序集。
- 不在运行时扫描 Gallery、主题 XAML 或源码目录。
- 不使用动态代理、动态表达式编译或运行时生成类型。

## 19. 与 MCP 的关系

`atomui_info` MCP tool 复用 `InfoCommandPayload` 的语义。MCP 默认请求 JSON payload，不走 text 或 Markdown renderer。

MCP tool 必须支持和 CLI 一致的查询参数：

| MCP 参数 | 对应 CLI |
| --- | --- |
| `control` | `<control>` |
| `product` | `--product` |
| `include` | `--include` |
| `detail` | `--detail` |
| `language` | `--lang` |
| `targetVersion` | `--target-version` |

## 20. 版本演进

`InfoCommandPayload.SchemaVersion` 从 `1.0` 开始。新增字段必须保持向后兼容：

- 新增区块只能作为可选字段或空集合。
- 不重命名已有字段。
- 不改变枚举字符串含义。
- 删除字段需要进入下一个 major schema。
- metadata 版本和 CLI 版本分开表达，`targetVersion` 表示 AtomUI metadata 版本。
