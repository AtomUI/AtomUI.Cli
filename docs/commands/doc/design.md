# dotnet atomui doc 详细设计

## 1. 命令定位

`doc` 是 AtomUI Cli 的控件和主题文档查询命令。它面向控件使用者、控件维护者、主题维护者、Agent 上下文注入、离线文档生成和 CI 文档校验，输出的是产品级控件完整文档，而不是控件元信息卡或少量示例拼接。

`doc` 回答的问题：

- 这个控件解决什么问题。
- 什么时候使用这个控件。
- 需要安装和引用哪些包。
- 最小 XAML/C# 用法是什么，常见场景如何组合。
- 完整 API surface 包含哪些 public 属性、public 方法、protected 扩展点、事件、Avalonia 属性和显式接口契约。
- 控件逻辑结构是什么，public API 如何归一为内部状态、伪类、主题变量和模板消费数据。
- ControlTheme 的真实结构、模板变体、命名节点、TemplateBinding、selector 分组和 Token 资源使用点是什么。
- 关键 Token、语义部件、模板部件、伪类和可定制边界是什么。
- 有哪些 Demo、相关控件和变更记录。
- 商业控件是否需要额外产品包、授权或数据根。

`doc` 不在运行时拼接源码信息。所有文档内容来自构建期生成的 documentation snapshot。CLI 运行时只负责选择快照、查询文档、应用 section/style 过滤和渲染输出。

本设计适用于所有 AtomUI 控件及商业控件项目。Button 只能作为首个 golden sample 和测试样例，不能在快照模型、渲染器或测试中形成 Button 专用逻辑。

与相邻命令的边界：

| 命令 | 定位 |
| --- | --- |
| `list` | 查询有哪些控件、产品、包和分类。 |
| `info` | 查询单个控件的结构化元信息和契约摘要。 |
| `doc` | 输出控件或主题的完整文档视图，覆盖使用、API surface、事件、逻辑结构、ControlTheme、Token 和语义结构。 |
| `demo` | 查询 Demo 列表和示例源码。 |
| `token` | 查询 Token 详情和过滤结果。 |
| `semantic` | 深查语义部件、模板部件、伪类和主题定制入口；`doc` 只输出完整文档中的语义摘要和主题结构。 |

## 2. 所属模块与注册

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliMetadataModule` |
| 注册阶段 | `ConfigureAtomUICliCommands` |
| 分组 | knowledge |
| 读写 | `isReadOnly=true` |
| 项目上下文 | `requiresProject=false` |
| 写入确认 | `requiresWriteConfirmation=false` |
| Handler 生命周期 | Transient |
| 支持格式 | text、json、markdown |
| 默认输出格式 | markdown |
| 所需模块 | Core + Metadata；商业文档通过显式数据根或商业数据模块追加 |

`doc` 命令必须被 command pre-parser 识别为 Metadata 模块命令。执行 `doc` 时只激活 Core 模块、Metadata 模块，以及命令 manifest 明确声明的 required modules，不挂载无关项目分析或写入模块。

`doc` 的默认格式是 markdown。为了避免全局默认 text 影响文档体验，command manifest 需要支持 command-level default format；首版如果 parser 还没有该能力，必须在 `DocCommandOptions` 中保留“用户是否显式传入 `--format`”的信息，再将未显式指定格式的 `doc` 映射为 markdown。

## 3. 调用语法

```bash
dotnet atomui doc Button
dotnet atomui doc Button --section usage
dotnet atomui doc Button --section api --format markdown
dotnet atomui doc Button --section events
dotnet atomui doc Button --section methods
dotnet atomui doc Button --section logic
dotnet atomui doc Button --section theme
dotnet atomui doc Button --section examples --examples all
dotnet atomui doc Button --example button-loading
dotnet atomui doc Button --style summary
dotnet atomui doc Button --style agent
dotnet atomui doc DataGrid --product datagrid
dotnet atomui doc --topic design-language
dotnet atomui doc Button --format json
dotnet atomui doc Button --lang zh
dotnet atomui doc Button --target-version 6.0
dotnet atomui doc Button --data-root ./metadata
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 合法值 | 校验失败 | 说明 |
| --- | --- | --- | --- | --- | --- |
| `target` | string | null | 控件名 | `ATOMUICLI_ARG001` | 控件文档目标。和 `--topic` 二选一。 |
| `--topic` | string | null | topic id | `ATOMUICLI_ARG002` | 查询主题文档，例如 `design-language`。不能和 `target` 同时传入。 |
| `--section` | enum | `all` | `all`、`overview`、`install`、`usage`、`scenarios`、`examples`、`api`、`properties`、`methods`、`events`、`logic`、`theme`、`tokens`、`semantic`、`demos`、`changelog`、`source` | `ATOMUICLI_ARG002` | 只输出指定文档区块。 |
| `--example` | string | null | stable sourceKey | `ATOMUICLI_CTRL003` | 只输出某个 Gallery 稳定示例，要求来自 `ShowCaseItem.SourceKey`。 |
| `--examples` | enum | `recommended` | `recommended`、`all`、`basic`、`state`、`theme`、`integration`、`advanced` | `ATOMUICLI_ARG002` | 控制 examples section 的示例覆盖范围。 |
| `--style` | enum | `full` | `full`、`summary`、`agent` | `ATOMUICLI_ARG002` | 控制输出裁剪策略。 |
| `--strict` | bool | `false` | true/false | 无 | 要求控件名精确匹配，不使用别名或建议匹配。 |
| `--product` | global string | all visible | product id | `ATOMUICLI_PKG001` | 限定产品范围，商业控件必须能通过 product 限定消除歧义。 |
| `--target-version` | global version | 内置默认版本 | semantic version 或 release line | `ATOMUICLI_DATA005` | 选择对应版本的 documentation snapshot。 |
| `--data-root` | global path | 内置数据根 | 存在且可读目录 | `ATOMUICLI_DATA004` | 追加外部文档/metadata 快照，优先级高于内置公开数据。 |
| `--lang` | global string | `en` 或 CLI 默认语言 | `en`、`zh`，后续可扩展 | `ATOMUICLI_ARG002` | 选择文档语言；结构化 ID 不受语言影响。 |
| `--format` | global enum | markdown | `text`、`json`、`markdown` | `ATOMUICLI_ARG002` | 输出格式。 |
| `--markdown` | global bool | false | true/false | 无 | `--format markdown` 快捷方式。 |
| `--detail` | global bool | false | true/false | 无 | 对 `doc` 不改变 section 集合，只允许 renderer 输出更多来源诊断和相关链接。 |

section 语义：

| section | 内容 |
| --- | --- |
| `overview` | 控件定位、适用场景、使用建议。 |
| `install` | 包、命名空间、XAML xmlns、产品/商业提示。 |
| `usage` | 最小用法、基础 XAML/C# 片段。 |
| `scenarios` | 常见场景和推荐组合方式。 |
| `examples` | Gallery 稳定示例，包含 SourceKey、标题、说明、真实 XAML/C# 片段和来源路径。 |
| `api` | 完整 API surface 摘要，包含 public 属性、public 方法、protected 扩展点、事件、Avalonia 属性和显式接口契约。 |
| `properties` | public CLR 属性、Avalonia `StyledProperty`、`DirectProperty`、`AttachedProperty` 和关键继承属性。 |
| `methods` | public 方法、构造函数、protected/protected virtual/protected override 扩展点和关键继承方法。 |
| `events` | CLR event、Avalonia `RoutedEvent`、显式接口 event、属性变化契约和事件路由类型。 |
| `logic` | 控件继承关系、接口实现、内部协作对象、状态归一流程和 public API 到 internal state 的映射。 |
| `theme` | ControlTheme 结构、模板变体、命名节点、TemplateBinding、selector 分组和 Token resource 使用点。 |
| `tokens` | 控件 Token、共享 Token 和主题扩展提示。 |
| `semantic` | 模板部件、语义部件、伪类、样式入口。 |
| `demos` | Demo 索引、路由、示例说明和可继续执行的命令。 |
| `changelog` | 相关变更、破坏性变更和迁移提示。 |
| `source` | 源码索引、主题文件、Gallery 文件、测试文件和 source identity。 |

style 语义：

| style | 行为 |
| --- | --- |
| `full` | 输出所有非空 section，适合完整阅读、控件维护和离线文档。 |
| `summary` | 输出 overview、install、usage、常见场景、关键 API、示例索引、主题入口和 warnings。 |
| `agent` | 输出面向 Agent 的紧凑文档，优先保留事实、代码、API surface、事件、逻辑结构、ControlTheme 摘要、Token、约束和相关命令。 |

## 5. Options 类型

```csharp
public sealed record DocCommandOptions(
    GlobalCliOptions Global,
    string? Target,
    string? Topic,
    DocumentSection Section,
    DocumentStyle Style,
    DocumentExamplesMode Examples,
    string? ExampleKey,
    bool Strict,
    bool FormatSpecified) : IAtomUICliCommandOptions;
```

`DocumentSection` 和 `DocumentStyle` 必须使用静态 parser，不能依赖 runtime reflection 或 `Enum.Parse` 的异常控制流。

```csharp
public enum DocumentSection
{
    All,
    Overview,
    Install,
    Usage,
    Scenarios,
    Examples,
    Api,
    Properties,
    Methods,
    Events,
    Logic,
    Theme,
    Tokens,
    Semantic,
    Demos,
    Changelog,
    Source
}

public enum DocumentStyle
{
    Full,
    Summary,
    Agent
}

public enum DocumentExamplesMode
{
    Recommended,
    All,
    Basic,
    State,
    Theme,
    Integration,
    Advanced
}
```

`FormatSpecified` 用于区分用户显式传入 `--format text` 和未传入格式。未传入格式时，`doc` 使用 markdown；显式 `--format text` 时输出纯文本。

## 6. 统一控件文档契约

每个进入 documentation snapshot 的控件都必须遵守同一套文档契约。构建期允许对不同控件输出不同数量的示例和不同复杂度的 theme 结构，但不允许只对 Button 或少数控件实现完整字段。

### 6.1 必备文档层次

| 层次 | 必填内容 | 主要来源 |
| --- | --- | --- |
| 控件使用文档 | 概述、安装、命名空间、何时使用、常见场景、真实示例。 | `docs/controls/**`、Gallery ShowCase、本地化资源。 |
| API surface | public 属性、public 方法、构造函数、protected 扩展点、Avalonia 属性、事件、显式接口和关键继承契约。 | 控件源码、Gallery API 表、源码文档配置。 |
| 逻辑结构 | 继承关系、接口实现、内部协作对象、状态归一流程、public API 到 internal state 的映射。 | 控件源码、implementation 文档、主题变量和状态同步代码。 |
| ControlTheme 结构 | TargetType、模板变体、真实视觉树、命名节点、TemplateBinding、selector 分组、Token resource 使用点。 | `Themes/*.axaml`、`Themes/*.cs`、源码索引。 |
| 语义结构 | semantic parts、template parts、pseudo classes、状态流、可定制边界。 | 控件文档、源码属性、ControlTheme、pseudo class 常量。 |
| Token 模型 | 共享 Token、控件 Token、Token scope、资源键、主题消费点。 | `token.md`、Token 类型、ControlTheme。 |
| 来源索引 | 控件源码、主题文件、Gallery 文件、文档文件、测试文件和 source identity。 | source extraction 中间模型。 |

构建期缺少某个必备层次时，不能静默生成空文档。公开控件必须输出 warning，缺少 API surface、ControlTheme 或稳定示例时 release 构建应按 validator 配置失败。

### 6.2 API surface 分类

`doc` 的 API section 不是 Gallery 属性表。Gallery API 表只表示常用 API 推荐视图，完整 API surface 由源码和文档配置合并生成。

| API kind | 输出要求 |
| --- | --- |
| `constructor` | public 构造函数和必要的可见性说明。 |
| `public-property` | CLR wrapper、类型、默认值、Avalonia property kind、描述、是否推荐。 |
| `styled-property` | `StyledProperty<T>`、owner、默认值、注册方式、CLR wrapper。 |
| `direct-property` | `DirectProperty<TOwner,T>`、getter/setter、CLR wrapper、变更通知方式。 |
| `attached-property` | attached owner、getter/setter、XAML 使用方式。 |
| `public-method` | public 方法签名、参数、返回值、使用边界。 |
| `protected-method` | `protected`、`protected virtual`、`protected override` 方法签名、生命周期阶段、扩展边界。 |
| `clr-event` | 普通 .NET event、事件参数、触发语义。 |
| `routed-event` | Avalonia `RoutedEvent`、routing strategy、event args、CLR wrapper。 |
| `interface-event` | 显式接口 event，例如表单或服务契约中的事件。 |
| `explicit-interface` | 显式接口方法和属性，说明由哪个协作系统调用。 |
| `inherited-contract` | 从 Avalonia 基类继承且对控件使用非常关键的属性、事件或方法。 |

事件必须区分 CLR event、Avalonia routed event、显式接口 event 和属性变化契约。`AvaloniaPropertyChangedEventArgs` 触发的属性变化不能伪装为事件，只能归入生命周期或状态流说明。

### 6.3 Protected 扩展点

`doc` 必须标注 protected/protected virtual/protected override 方法。对控件维护和派生控件开发有意义的方法，应输出：

- 方法签名。
- 生命周期阶段，例如 initialization、loaded、template、measure、property-change、detach。
- 调用顺序。
- 可覆盖边界。
- 覆盖时必须调用 base 的要求。
- 影响的 public API、伪类、theme variable 或 template part。

### 6.4 逻辑结构

每个控件都应生成逻辑结构树。逻辑结构不是 ControlTheme 视觉树，也不是 semantic parts 表。

逻辑结构至少包含：

- 基类和接口实现。
- public API 分组。
- public API 到 effective/internal state 的归一路径。
- 内部协作对象和 owner 关系。
- 状态同步入口，例如 `OnApplyTemplate`、`OnPropertyChanged`、事件 handler、collection change handler。
- 输出到主题层的伪类、internal styled property、direct property 或 template binding。

示例形态：

```text
<Control>
  -> <BaseType>
  -> <InterfaceA>

Public API
  -> property group
  -> method group
  -> event group

State normalization
  -> public API
  -> effective state
  -> pseudo classes / theme variables
  -> ControlTheme selectors

Runtime collaborators
  -> internal presenter / popup host / item container / adorner / motion actor
```

### 6.5 ControlTheme 结构

ControlTheme section 必须来自真实 `.axaml` 解析，不允许根据控件名称或通用模式生成伪结构。

每个控件的 ControlTheme 文档至少包含：

- `ControlTheme` target type 和资源来源文件。
- 模板变体，例如不同 selector 下的 `Template`。
- 每个模板变体的真实视觉树摘要。
- 所有 `Name` / `x:Name` 节点，尤其是 `PART_*`。
- `TemplateBinding` 映射关系。
- selector 分组：尺寸、状态、语义颜色、变体、伪类、禁用态、焦点态、弹层态、选择态等。
- Token resource 使用点，例如 `SharedTokenResource`、`ButtonTokenResource`、商业控件 Token resource。
- 可定制边界：哪些节点稳定，哪些只是内部实现细节。

模板视觉树必须保持父子层级，不能退化成命名节点列表。多个 `ControlTemplate` 或 selector 变体可以形成多个根树。

### 6.6 示例覆盖标准

示例必须来自真实 Gallery `ShowCaseItem`，并优先使用稳定 `SourceKey`。`doc` 不维护第二套手写示例。

每个公开控件应满足：

- 至少 1 个基础示例。
- 覆盖该控件最常见的状态、尺寸、变体、数据绑定、事件或组合场景。
- 复杂控件必须覆盖 integration 或 advanced 示例，例如弹层、数据源、item template、选择、编辑、异步状态。
- 示例代码必须可追溯到 Gallery 源文件和行号。
- 示例过长时可以按 `--examples recommended` 裁剪，但 `--examples all` 必须能输出所有文档级稳定示例。

缺少稳定 `SourceKey` 时，构建期可以生成临时 key，但必须输出 warning；release 构建可以按控件可见性和质量等级把该 warning 升级为错误。

## 7. Handler 依赖

`DocCommandHandler` 只负责命令编排，不读取文件系统、不扫描源码、不解析 JSON、不访问 Git、不直接拼接文档。

| 依赖 | 生命周期 | 说明 |
| --- | --- | --- |
| `IMetadataCommandContextFactory` | Singleton | 解析 target version、product filter、data root、language 和 detail。 |
| `IDocumentationQueryService` | Singleton | 基于 documentation snapshot 查询控件或主题文档。 |
| `IDocumentSectionSelector` | Singleton | 应用 section 和 style，保持稳定区块顺序。 |
| `IDocOutputRenderer` | Singleton | 渲染 text/markdown 输出；JSON 走 payload。 |
| `ICommandSuggestionService` | Singleton | NotFound/Ambiguous 分支生成稳定建议。 |

运行时基础设施由 command dispatcher 统一处理：

- `IOutputWriter`
- `IErrorWriter`
- `IJsonOutputSerializer`
- `IExitCodeMapper`
- `CliInvocationContext`

Handler 不接收 root `IServiceProvider`，不创建 scope，不直接访问 `DocumentSnapshotRegistry` 之外的数据源。

## 8. 执行流程

```text
CliCommandDispatcher
  -> DocCommandOptions.Parse
  -> DocCommandHandler
  -> MetadataCommandContextFactory
  -> DocumentationQueryService
  -> DocumentSectionSelector
  -> DocOutputRenderer
  -> AtomUICliResult
```

详细流程：

1. 校验 `target` 和 `--topic` 必须二选一。
2. 校验 `--section` 和 `--style`。
3. 创建 `MetadataCommandContext`，解析 target version、product filter、language 和 data roots。
4. 根据 `target` 或 `--topic` 构造 `DocumentQuery`。
5. `IDocumentationQueryService` 从 `DocumentSnapshotRegistry` 查询目标文档。
6. 查询控件文档时，先在目标 product/version/language 下解析唯一控件；未找到返回 suggestions，多个匹配返回 candidates。
7. 对查询结果应用 section、style、`--examples` 和 `--example`。section 或 example 不存在时返回 `SectionNotFound` 或 `ExampleNotFound`，不输出空文档。
8. 构造 `DocCommandPayload`。控件文档必须包含结构化 `ControlDocumentPayload`；主题文档可以只包含 sections。
9. `--format json` 返回结构化 payload。
10. `--format markdown` 输出 markdown renderer 结果。
11. `--format text` 输出纯文本 renderer 结果。

运行时不得从 `<AtomUICliRepoRoot>/.workspace/AtomUI` 或任何源码目录读取文档。该路径只用于构建期生成快照。

## 9. 领域服务契约

### IDocumentationQueryService

```csharp
public interface IDocumentationQueryService
{
    ValueTask<DocumentQueryResult> QueryAsync(
        DocumentQuery query,
        CancellationToken cancellationToken);
}
```

```csharp
public sealed record DocumentQuery(
    DocumentTargetKind TargetKind,
    string TargetId,
    string TargetVersion,
    ProductFilter ProductFilter,
    string Language,
    DocumentSection Section,
    DocumentStyle Style,
    DocumentExamplesMode Examples,
    string? ExampleKey,
    bool Strict,
    MetadataRootSet DataRoots);
```

结果分支：

| 分支 | 场景 | 命令映射 |
| --- | --- | --- |
| `Found` | 找到唯一文档。 | 输出 payload。 |
| `NotFound` | 控件、topic 或文档不存在。 | `ATOMUICLI_CTRL001` 或 `ATOMUICLI_DATA001`，带 suggestions。 |
| `AmbiguousTarget` | 同名控件存在于多个可见产品。 | `ATOMUICLI_CTRL002`，带 candidates。 |
| `SectionNotFound` | 请求 section 在目标文档中不存在。 | `ATOMUICLI_DATA001`，带 available sections。 |
| `ExampleNotFound` | 请求 `--example` 指定的 SourceKey 不存在。 | `ATOMUICLI_CTRL003`，带 available examples。 |
| `SnapshotUnavailable` | 目标版本、语言或数据根没有可用快照。 | `ATOMUICLI_DATA001` 或 `ATOMUICLI_DATA005`。 |
| `SnapshotIncompatible` | 快照 schema 不兼容。 | `ATOMUICLI_DATA002`。 |
| `DataRootAccessDenied` | 外部数据根不可读或授权失败。 | `ATOMUICLI_DATA004`。 |
| `StructureIncomplete` | 控件文档缺少必备 API surface、逻辑结构、ControlTheme 或示例结构。 | `ATOMUICLI_DATA003`，带 diagnostics。 |

### DocumentSnapshotRegistry

```csharp
public interface IDocumentSnapshotProvider
{
    IReadOnlyList<DocumentSnapshot> GetSnapshots();
}

public sealed class DocumentSnapshotRegistry
{
    public DocumentSnapshotSelectionResult Select(DocumentSnapshotSelection selection);
}
```

Provider 必须显式注册：

- 内置公开文档快照由 `AtomUICliMetadataModule` 注册。
- 商业文档快照由商业数据模块或显式 `--data-root` 注册。
- 测试可注册 in-memory provider。

禁止通过程序集扫描、目录扫描或命名约定发现 provider。

### IDocumentSectionSelector

```csharp
public interface IDocumentSectionSelector
{
    DocumentSectionSelectionResult Select(
        IReadOnlyList<DocumentSectionContent> sections,
        DocumentSection section,
        DocumentStyle style);
}
```

选择规则：

- `section=all` 时按 snapshot 中的 `Order` 升序输出所有非空区块。
- 指定 section 时只输出目标区块；目标区块不存在返回 `SectionNotFound`。
- `style=summary` 时保留 overview、install、usage、关键 API、demos 和 warnings。
- `style=agent` 时保留事实性内容和代码，删除营销性或长篇解释段落。
- style 裁剪不能改变 section id、target id、product id 和 source identity。

## 10. 输出模型

`doc` 的 JSON 输出必须是结构化 payload，不能把整篇 Markdown 作为裸字符串 payload。

```csharp
public sealed record DocCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string TargetKind,
    string TargetId,
    string Title,
    string Language,
    string? ProductId,
    string? PackageId,
    bool IsCommercial,
    DocumentSection RequestedSection,
    DocumentStyle Style,
    DocumentExamplesMode Examples,
    string? ExampleKey,
    DocSourceIdentityPayload Source,
    DocControlDocumentPayload? Control,
    IReadOnlyList<DocSectionPayload> Sections,
    IReadOnlyList<DocRelatedItemPayload> Related,
    IReadOnlyList<DocWarningPayload> Warnings,
    IReadOnlyList<DocSuggestionPayload> Suggestions) : IAtomUICliJsonPayload;
```

```csharp
public sealed record DocSectionPayload(
    string Id,
    string Title,
    int Order,
    string Markdown,
    IReadOnlyList<string> SourceKinds);

public sealed record DocControlDocumentPayload(
    DocControlIdentityPayload Identity,
    DocUsagePayload Usage,
    DocApiSurfacePayload ApiSurface,
    DocLogicStructurePayload LogicStructure,
    DocControlThemePayload Theme,
    IReadOnlyList<DocExamplePayload> Examples,
    IReadOnlyList<DocTokenPayload> Tokens,
    IReadOnlyList<DocSemanticPartPayload> SemanticParts,
    IReadOnlyList<DocSourceFilePayload> SourceFiles);

public sealed record DocControlIdentityPayload(
    string Id,
    string Name,
    string DisplayName,
    string CategoryId,
    string ProductId,
    string PackageId,
    string Namespace,
    string XamlNamespace,
    string? BaseType,
    string Status,
    bool IsCommercial);

public sealed record DocUsagePayload(
    string Summary,
    IReadOnlyList<string> WhenToUse,
    IReadOnlyList<string> WhenNotToUse,
    IReadOnlyList<DocCodeSnippetPayload> MinimalSnippets);

public sealed record DocApiSurfacePayload(
    IReadOnlyList<DocApiMemberPayload> Members,
    IReadOnlyList<DocApiEventPayload> Events,
    IReadOnlyList<DocApiInheritancePayload> InheritedContracts,
    IReadOnlyList<DocApiDiagnosticPayload> Diagnostics);

public sealed record DocApiMemberPayload(
    string Name,
    string Kind,
    string DeclaringType,
    string Accessibility,
    string Signature,
    string? Type,
    string? DefaultValue,
    string ContractLevel,
    string Description,
    string SourcePath,
    int? SourceLine);

public sealed record DocApiEventPayload(
    string Name,
    string Kind,
    string DeclaringType,
    string Accessibility,
    string Signature,
    string? RoutingStrategy,
    string? EventArgsType,
    string Description,
    string SourcePath,
    int? SourceLine);

public sealed record DocApiInheritancePayload(
    string MemberName,
    string Kind,
    string DeclaringType,
    string Reason);

public sealed record DocApiDiagnosticPayload(
    string Code,
    string Message,
    string Severity,
    string? MemberName);

public sealed record DocLogicStructurePayload(
    IReadOnlyList<DocLogicNodePayload> Inheritance,
    IReadOnlyList<DocLogicNodePayload> PublicApiGroups,
    IReadOnlyList<DocLogicFlowPayload> StateFlows,
    IReadOnlyList<DocLogicNodePayload> RuntimeCollaborators,
    IReadOnlyList<DocLogicFlowPayload> ThemeBridge);

public sealed record DocLogicNodePayload(
    string Id,
    string Kind,
    string Label,
    string? Description,
    IReadOnlyList<string> RelatedApis);

public sealed record DocLogicFlowPayload(
    string Id,
    string Title,
    IReadOnlyList<string> Steps);

public sealed record DocControlThemePayload(
    string TargetType,
    IReadOnlyList<DocThemeTemplatePayload> Templates,
    IReadOnlyList<DocThemeSelectorGroupPayload> SelectorGroups,
    IReadOnlyList<DocThemeBindingPayload> TemplateBindings,
    IReadOnlyList<DocThemeTokenUsagePayload> TokenUsages,
    IReadOnlyList<DocThemeCustomizationBoundaryPayload> CustomizationBoundaries);

public sealed record DocThemeTemplatePayload(
    string Id,
    string SourceSelector,
    string SourcePath,
    int? SourceLine,
    IReadOnlyList<DocThemeNodePayload> Roots);

public sealed record DocThemeNodePayload(
    string ElementType,
    string? Name,
    string? Stability,
    IReadOnlyList<DocThemeNodePayload> Children);

public sealed record DocThemeSelectorGroupPayload(
    string Kind,
    IReadOnlyList<string> Selectors,
    string Description);

public sealed record DocThemeBindingPayload(
    string TargetNode,
    string TargetProperty,
    string SourceProperty);

public sealed record DocThemeTokenUsagePayload(
    string ResourceKind,
    string TokenName,
    string TargetSelector,
    string TargetProperty);

public sealed record DocThemeCustomizationBoundaryPayload(
    string Target,
    string Stability,
    string Guidance);

public sealed record DocExamplePayload(
    string SourceKey,
    string Title,
    string Description,
    string Kind,
    int Priority,
    string? BadgeText,
    IReadOnlyList<DocCodeSnippetPayload> Snippets,
    string SourcePath,
    int? SourceLine);

public sealed record DocCodeSnippetPayload(
    string Language,
    string Code,
    string? SourcePath,
    int? SourceLine);

public sealed record DocTokenPayload(
    string Name,
    string Scope,
    string Status,
    string Description,
    IReadOnlyList<string> ThemeUsage);

public sealed record DocSemanticPartPayload(
    string Part,
    string AtomUINode,
    string Responsibility,
    IReadOnlyList<string> RelatedApis,
    IReadOnlyList<string> RelatedTokens,
    string Stability);

public sealed record DocSourceFilePayload(
    string Kind,
    string Path,
    string? Description);

public sealed record DocSourceIdentityPayload(
    string SnapshotSchemaVersion,
    string SnapshotId,
    string SourceRef,
    string SourceCommit,
    string GeneratedAt);

public sealed record DocRelatedItemPayload(
    string Kind,
    string Id,
    string Title,
    string Command);

public sealed record DocWarningPayload(
    string Code,
    string Message,
    string? Section);

public sealed record DocSuggestionPayload(
    string Kind,
    string Id,
    string Title,
    string? ProductId);
```

字段要求：

- `Sections` 必须按 `Order` 升序稳定输出。
- `Control` 在 `TargetKind=Control` 时必填，在 topic 文档时为 null。
- `ApiSurface.Members` 必须覆盖 public 属性、public 方法、构造函数、protected 扩展点、Avalonia property 和显式接口契约。
- `ApiSurface.Events` 必须区分 `clr-event`、`routed-event`、`interface-event` 和 `property-change-contract`。
- `Theme.Templates` 必须来自真实 ControlTheme，不允许生成伪节点。
- `Examples` 必须来自 Gallery 稳定示例，`SourceKey` 在同一控件内唯一。
- `SourceKinds` 只能使用固定值：`curated`、`api`、`event`、`logic`、`theme`、`token`、`demo`、`semantic`、`changelog`、`package`、`generated`。
- `Markdown` 字段保留对应 section 的 markdown，不包含整篇文档外壳。
- `Related.Command` 必须是可复制的 CLI 命令，例如 `dotnet atomui demo Button --list`。
- 不输出本机绝对源码路径。

## 11. 输出格式

### markdown

默认输出。用于开发者阅读和离线文档生成。

```markdown
# Button

## 概述

## 包与命名空间

## 何时使用

## 常见使用场景

## 核心设计模型

## 使用示例

## API

### 公共属性

### 公共方法

### 事件

### Protected 扩展点

### 显式接口契约

### 关键继承契约

## 逻辑结构

## ControlTheme 结构

## 状态模型

## 主题与 Design Token

## 语义结构

## 源码索引
```

规则：

- 标题层级从 `# <Title>` 开始。
- section 使用 `##`。
- API、事件、Token、Demo、semantic parts 使用 Markdown 表格。
- 逻辑结构和 ControlTheme 结构优先使用 fenced `text` 树，保留父子层级。
- ControlTheme 模板变体必须显示 source selector 和来源文件。
- XAML/C# 示例使用 fenced code block。
- 示例必须显示 SourceKey；`--detail` 时显示逻辑来源路径和行号。
- warnings 输出在文档末尾的 `## Warnings`。
- `--detail` 可追加 `## Source`，展示 snapshot id、source ref、source commit 和 generated at。

### text

用于终端快速阅读。text 输出不是 markdown 原样输出。

规则：

- 去掉 Markdown 表格语法。
- 保留标题和代码块。
- API、事件、Token、Demo 渲染为缩进列表。
- 逻辑结构和 ControlTheme 结构保留缩进树。
- 不输出 JSON 包装。

### json

用于 Agent、IDE、CI 和 MCP 工具集成。

规则：

- 输出 `DocCommandPayload`。
- `payload` 必须是 JSON object，不允许是 string。
- 控件文档必须输出结构化 `control` 对象，不允许只输出 `sections[].markdown`。
- error envelope 遵守错误码标准。
- DTO 必须纳入 source generated JSON context 或实现 `IAtomUICliJsonPayload`。

## 12. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | `target` 和 `--topic` 都缺失。 | `2` |
| `ATOMUICLI_ARG002` | `target` 与 `--topic` 同时传入，或 section/style/lang 非法。 | `2` |
| `ATOMUICLI_CTRL001` | 控件不存在。 | `3` |
| `ATOMUICLI_CTRL002` | 控件在多个 product 中歧义。 | `3` |
| `ATOMUICLI_CTRL003` | `--example` 指定的 Gallery SourceKey 不存在。 | `3` |
| `ATOMUICLI_PKG001` | `--product` 指定未知产品。 | `3` |
| `ATOMUICLI_DATA001` | 文档快照、topic、section 或文档区块不可用。 | `4` |
| `ATOMUICLI_DATA002` | documentation snapshot schema 不兼容。 | `4` |
| `ATOMUICLI_DATA003` | documentation snapshot 内部引用缺失，或控件文档缺少必备结构。 | `4` |
| `ATOMUICLI_DATA004` | `--data-root` 不可读、授权失败或商业文档不可见。 | `4` |
| `ATOMUICLI_DATA005` | `--target-version` 无法解析到可用快照。 | `4` |

失败输出要求：

- 控件不存在时必须带 suggestions。
- 控件歧义时必须带 candidates，并提示使用 `--product`。
- section 不存在时必须列出 available sections。
- example 不存在时必须列出 available examples。
- structure incomplete 时必须列出缺失层次，例如 API surface、events、logic、ControlTheme、examples。
- snapshot 不可用时必须包含 target version、language 和 data root 摘要。

## 13. AOT-first 约束

`doc` 运行时必须满足：

- 不读取 AtomUI 源码目录。
- 不扫描 Gallery、主题 XAML 或文档源码。
- 不访问 Git。
- 不访问网络。
- 不加载或反射 AtomUI 控件程序集。
- 不动态编译模板。
- 不通过程序集扫描发现 snapshot provider。
- section/style/lang parser 静态实现。
- renderer 使用显式 formatter。
- JSON payload DTO 纳入 source generation 或实现显式 JSON payload contract。

documentation snapshot 在构建期生成。构建期源码根目录解析必须遵守 [命令设计标准](../command-design-standard.md) 中的构建期源码输入标准：

1. `AtomUISourceRoot` MSBuild 属性或 `--source-root <path>`。
2. `ATOMUI_SOURCE_ROOT` 环境变量。
3. 默认约定路径：`<AtomUICliRepoRoot>/.workspace/AtomUI`。

构建期工具只消费已存在的源码目录，不自动 clone、pull 或 checkout。Release 构建必须校验源码 commit 与锁文件一致，并把 source ref、source commit、target version、snapshot schema version 和 generated at 写入快照。

## 14. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| 默认控件文档 | `doc Button` | markdown 输出，包含 `# Button`、overview、install、usage。 |
| 默认格式 | `doc Button` | 未传 `--format` 时使用 markdown，不使用 text。 |
| 显式 text | `doc Button --format text` | 纯文本输出，无 Markdown 表格语法。 |
| JSON payload | `doc Button --format json` | `payload` 是 object，包含 control、sections、source、warnings，不是 string。 |
| API surface | `doc Button --section api` | 包含 public 属性、protected 扩展点、事件、Avalonia property 和显式接口契约。 |
| methods section | `doc Button --section methods` | 输出 public 方法和 protected/protected override 扩展点。 |
| events section | `doc Button --section events` | 区分 CLR event、Avalonia routed event、显式接口 event 和属性变化契约。 |
| logic section | `doc Button --section logic` | 输出继承关系、接口实现、状态归一流程和 theme bridge。 |
| theme section | `doc Button --section theme` | 输出 ControlTheme target、模板变体、真实视觉树、TemplateBinding 和 selector 分组。 |
| examples all | `doc Button --section examples --examples all` | 输出所有文档级 Gallery SourceKey 示例。 |
| single example | `doc Button --example button-loading` | 只输出 `button-loading` 示例。 |
| section 过滤 | `doc Button --section api` | 只输出 API section 和必要标题。 |
| summary style | `doc Button --style summary` | 输出短文档，不包含 changelog 全量内容。 |
| agent style | `doc Button --style agent` | 输出事实、代码、API、Token 和相关命令，少解释性段落。 |
| topic 文档 | `doc --topic design-language` | 输出主题文档，不要求 control/package 字段。 |
| 商业控件 | `doc DataGrid --product datagrid` | 输出商业产品、包和可见性提示。 |
| 控件不存在 | `doc Unknown` | `ATOMUICLI_CTRL001`，带 suggestions。 |
| 控件歧义 | `doc SharedName` | `ATOMUICLI_CTRL002`，带 candidates 和 `--product` 建议。 |
| section 不存在 | `doc Button --section changelog`，快照无 changelog section | `ATOMUICLI_DATA001`，带 available sections。 |
| example 不存在 | `doc Button --example none` | `ATOMUICLI_CTRL003`，带 available examples。 |
| topic/target 互斥 | `doc Button --topic design-language` | `ATOMUICLI_ARG002`。 |
| 非法 style | `doc Button --style compact` | `ATOMUICLI_ARG002`。 |
| 外部数据根 | `doc Button --data-root ./metadata` | 外部快照优先级高于内置快照。 |
| schema 不兼容 | `doc Button --data-root ./bad-schema` | `ATOMUICLI_DATA002`。 |
| 版本不存在 | `doc Button --target-version 0.0.0` | `ATOMUICLI_DATA005`。 |

构建期测试：

| 测试 | 覆盖点 |
| --- | --- |
| `DocumentSourceRootResolverTests` | `--source-root`、MSBuild 属性、环境变量、默认路径优先级。 |
| `DocumentSnapshotBuilderTests` | 从 metadata 中间模型生成 control/topic 文档。 |
| `DocumentSnapshotValidatorTests` | section id、order、source identity、商业字段边界。 |
| `DocumentSnapshotLockTests` | source commit 和锁文件不匹配时失败。 |
| `DocRendererTests` | text、markdown、agent style 输出稳定。 |
| `DocApiSurfaceContractTests` | 所有公开控件必须具备 API surface；public/protected/event/Avalonia property 分类稳定。 |
| `DocControlThemeContractTests` | 所有带 ControlTheme 的控件必须输出真实模板结构，不能只有命名节点列表。 |
| `DocExampleCoverageTests` | 所有公开控件必须具备稳定 Gallery 示例；复杂控件必须覆盖 common/integration 场景。 |

控件覆盖矩阵：

| 控件类型 | golden controls | 必须覆盖 |
| --- | --- | --- |
| 基础动作控件 | Button、SplitButton、DropdownButton | API surface、protected hooks、状态归一、ControlTheme、常见按钮场景。 |
| 输入控件 | LineEdit、NumericUpDown、DatePicker | 输入值、验证状态、事件、绑定、ControlTheme、表单协作。 |
| 选择/弹层控件 | ComboBox、Select、Cascader | popup host、item container、选择事件、键盘交互、模板协作节点。 |
| 数据展示控件 | Table、Tree、ListView | item container、数据源、选择、虚拟化、模板结构。 |
| 反馈/浮层控件 | Modal、Drawer、Message、Notification | service API、adorner/popup、生命周期、motion actor、主题边界。 |
| 商业复杂控件 | DataGrid | 商业可见性、授权数据根、列/行 API、事件、编辑状态、复杂主题结构。 |

## 15. 实现文件建议

运行时：

- `src/AtomUI.Cli.Hosting/Metadata/DocCommandOptions.cs`
- `src/AtomUI.Cli.Hosting/Metadata/DocCommandHandler.cs`
- `src/AtomUI.Cli.Hosting/Metadata/DocCommandPayloads.cs`
- `src/AtomUI.Cli.Hosting/Metadata/DocumentationQueryService.cs`
- `src/AtomUI.Cli.Hosting/Metadata/DocumentSnapshotRegistry.cs`
- `src/AtomUI.Cli.Hosting/Metadata/DocumentSnapshotModels.cs`
- `src/AtomUI.Cli.Hosting/Metadata/DocumentSectionSelector.cs`
- `src/AtomUI.Cli.Hosting/Metadata/DocOutputRenderer.cs`
- `src/AtomUI.Cli.Hosting/Metadata/Generated/BuiltInDocumentSnapshot.g.cs`

构建期：

- `tools/AtomUI.Cli.MetadataBuilder/Documentation/DocumentSourceRootResolver.cs`
- `tools/AtomUI.Cli.MetadataBuilder/Documentation/DocumentSnapshotBuilder.cs`
- `tools/AtomUI.Cli.MetadataBuilder/Documentation/DocumentSnapshotValidator.cs`
- `tools/AtomUI.Cli.MetadataBuilder/Documentation/DocumentSnapshotWriter.cs`
- `tools/AtomUI.Cli.MetadataBuilder/Documentation/DocumentSnapshotLockFile.cs`

测试：

- `tests/AtomUI.Cli.Tests/FeatureModules/DocCommandTests.cs`
- `tests/AtomUI.Cli.Tests/FeatureModules/DocOutputContractTests.cs`
- `tests/AtomUI.Cli.Tests/Metadata/DocumentSnapshotRegistryTests.cs`
- `tests/AtomUI.Cli.MetadataBuilder.Tests/Documentation/DocumentSourceRootResolverTests.cs`
- `tests/AtomUI.Cli.MetadataBuilder.Tests/Documentation/DocumentSnapshotBuilderTests.cs`

## 16. Documentation Snapshot 架构

documentation snapshot 是 `doc` 命令的唯一运行时事实源。它可以和 metadata snapshot 存在于同一 data root，也可以作为 metadata snapshot 的 `documents` 分区发布。

```csharp
public sealed record DocumentSnapshot(
    string SchemaVersion,
    string SnapshotId,
    string TargetVersion,
    string ProductId,
    string PackageId,
    string Language,
    DocumentSourceIdentity Source,
    IReadOnlyList<ControlDocument> Controls,
    IReadOnlyList<TopicDocument> Topics,
    DocumentSearchIndex SearchIndex);

public sealed record DocumentSourceIdentity(
    string SourceRootConvention,
    string SourceRef,
    string SourceCommit,
    string GeneratedAt);

public sealed record ControlDocument(
    string Id,
    string Name,
    string DisplayName,
    string CategoryId,
    string ProductId,
    string PackageId,
    bool IsCommercial,
    string Status,
    string Summary,
    ControlDocumentIdentity Identity,
    ControlUsageDocument Usage,
    ControlApiSurfaceDocument ApiSurface,
    ControlLogicStructureDocument LogicStructure,
    ControlThemeDocument Theme,
    IReadOnlyList<ControlExampleDocument> Examples,
    IReadOnlyList<ControlTokenDocument> Tokens,
    IReadOnlyList<ControlSemanticPartDocument> SemanticParts,
    IReadOnlyList<ControlSourceFileDocument> SourceFiles,
    IReadOnlyList<DocumentSectionContent> Sections,
    IReadOnlyList<DocumentRelatedItem> Related,
    IReadOnlyList<DocumentWarning> Warnings);

public sealed record ControlDocumentIdentity(
    string Namespace,
    string XamlNamespace,
    string? BaseType,
    IReadOnlyList<string> ImplementedInterfaces,
    string GalleryPath,
    string DocsPath);

public sealed record ControlUsageDocument(
    IReadOnlyList<string> WhenToUse,
    IReadOnlyList<string> WhenNotToUse,
    IReadOnlyList<CodeSnippetDocument> MinimalSnippets);

public sealed record ControlApiSurfaceDocument(
    IReadOnlyList<ApiMemberDocument> Members,
    IReadOnlyList<ApiEventDocument> Events,
    IReadOnlyList<ApiInheritanceDocument> InheritedContracts,
    IReadOnlyList<ApiSurfaceDiagnostic> Diagnostics);

public sealed record ApiMemberDocument(
    string Name,
    string Kind,
    string DeclaringType,
    string Accessibility,
    string Signature,
    string? Type,
    string? DefaultValue,
    string ContractLevel,
    string Description,
    string SourcePath,
    int? SourceLine);

public sealed record ApiEventDocument(
    string Name,
    string Kind,
    string DeclaringType,
    string Accessibility,
    string Signature,
    string? RoutingStrategy,
    string? EventArgsType,
    string Description,
    string SourcePath,
    int? SourceLine);

public sealed record ApiInheritanceDocument(
    string MemberName,
    string Kind,
    string DeclaringType,
    string Reason);

public sealed record ApiSurfaceDiagnostic(
    string Code,
    string Severity,
    string Message,
    string? MemberName);

public sealed record ControlLogicStructureDocument(
    IReadOnlyList<LogicNodeDocument> Inheritance,
    IReadOnlyList<LogicNodeDocument> PublicApiGroups,
    IReadOnlyList<LogicFlowDocument> StateFlows,
    IReadOnlyList<LogicNodeDocument> RuntimeCollaborators,
    IReadOnlyList<LogicFlowDocument> ThemeBridge);

public sealed record LogicNodeDocument(
    string Id,
    string Kind,
    string Label,
    string? Description,
    IReadOnlyList<string> RelatedApis);

public sealed record LogicFlowDocument(
    string Id,
    string Title,
    IReadOnlyList<string> Steps);

public sealed record ControlThemeDocument(
    string TargetType,
    IReadOnlyList<ThemeTemplateDocument> Templates,
    IReadOnlyList<ThemeSelectorGroupDocument> SelectorGroups,
    IReadOnlyList<ThemeBindingDocument> TemplateBindings,
    IReadOnlyList<ThemeTokenUsageDocument> TokenUsages,
    IReadOnlyList<ThemeCustomizationBoundaryDocument> CustomizationBoundaries);

public sealed record ThemeTemplateDocument(
    string Id,
    string SourceSelector,
    string SourcePath,
    int? SourceLine,
    IReadOnlyList<ThemeNodeDocument> Roots);

public sealed record ThemeNodeDocument(
    string ElementType,
    string? Name,
    string? Stability,
    IReadOnlyList<ThemeNodeDocument> Children);

public sealed record ThemeSelectorGroupDocument(
    string Kind,
    IReadOnlyList<string> Selectors,
    string Description);

public sealed record ThemeBindingDocument(
    string TargetNode,
    string TargetProperty,
    string SourceProperty);

public sealed record ThemeTokenUsageDocument(
    string ResourceKind,
    string TokenName,
    string TargetSelector,
    string TargetProperty);

public sealed record ThemeCustomizationBoundaryDocument(
    string Target,
    string Stability,
    string Guidance);

public sealed record ControlExampleDocument(
    string SourceKey,
    string Title,
    string Description,
    string Kind,
    int Priority,
    string? BadgeText,
    IReadOnlyList<CodeSnippetDocument> Snippets,
    string SourcePath,
    int? SourceLine);

public sealed record CodeSnippetDocument(
    string Language,
    string Code,
    string? SourcePath,
    int? SourceLine);

public sealed record ControlTokenDocument(
    string Name,
    string Scope,
    string Status,
    string Description,
    IReadOnlyList<string> ThemeUsage);

public sealed record ControlSemanticPartDocument(
    string Part,
    string AtomUINode,
    string Responsibility,
    IReadOnlyList<string> RelatedApis,
    IReadOnlyList<string> RelatedTokens,
    string Stability);

public sealed record ControlSourceFileDocument(
    string Kind,
    string Path,
    string? Description);

public sealed record TopicDocument(
    string Id,
    string Title,
    string Language,
    IReadOnlyList<DocumentSectionContent> Sections,
    IReadOnlyList<DocumentRelatedItem> Related,
    IReadOnlyList<DocumentWarning> Warnings);
```

快照合并优先级：

1. 外部 `--data-root` 文档快照。
2. 商业数据模块注册的文档快照。
3. 内置公开文档快照。

同一个 target/version/language/product 出现多个文档时，优先级高的快照覆盖低优先级文档；覆盖必须记录 warning，便于 `--detail` 输出。

快照 schema 要求：

- 所有公开控件必须具备 `ApiSurface`、`LogicStructure`、`Theme`、`Examples`、`Tokens`、`SemanticParts` 和 `SourceFiles` 字段。
- 不存在 ControlTheme 的控件必须在 `Theme.CustomizationBoundaries` 中说明原因，并提供源码或服务型控件的结构入口；不能输出伪模板。
- 所有 `SourcePath` 必须是源码根下的逻辑相对路径，禁止本机绝对路径。
- 商业控件快照必须带可见性字段，公开快照不得泄漏 internal/private API、示例或源码路径。

## 17. 构建期生成流程

documentation snapshot 由构建期 pipeline 生成，首版作为 `tools/AtomUI.Cli.MetadataBuilder` 的 documentation target 实现，复用 metadata source extraction 的 SourceWorkspaceReader、Gallery adapter、Control source adapter 和 Localization adapter。

```text
AtomUI source root
  -> SourceWorkspaceReader
  -> ProductProjectAdapter
  -> GalleryNavigationAdapter
  -> GalleryShowCaseAdapter
  -> MarkdownControlDocAdapter
  -> ControlSourceAdapter
  -> AvaloniaEventContractAdapter
  -> ControlThemeAdapter
  -> TokenAdapter
  -> LocalizationAdapter
  -> Metadata intermediate model
  -> Documentation content assembler
  -> Documentation section generator
  -> Documentation snapshot validator
  -> JSON snapshot or generated C# snapshot
  -> AtomUI Cli package
```

输入来源：

| 输入 | 用途 |
| --- | --- |
| Gallery navigation | 控件分类、顺序、路由和显示名。 |
| Gallery examples | usage、scenarios、examples、demos、SourceKey 和代码片段。 |
| Gallery ViewModel | 推荐 API 表、Token 表、说明文案。 |
| 控件源码 | 完整 API surface、public/protected 方法、事件、Avalonia property、显式接口、继承关系、伪类、命名空间。 |
| ControlTheme | 真实模板结构、命名节点、TemplateBinding、selector 分组和 Token resource 使用点。 |
| build props | target version、包版本。 |
| curated docs | overview、implementation、token、changelog、逻辑结构说明、维护边界。 |
| changelog | changelog section。 |
| commercial metadata | 商业控件包、授权提示和可见性。 |

输出形态：

```text
data/docs/v6/en.json
data/docs/v6/zh.json
data/docs/v6/datagrid.en.json
src/AtomUI.Cli.Hosting/Metadata/Generated/BuiltInDocumentSnapshot.g.cs
```

开发态可以提交 plain JSON 便于 review；发布态可以生成 gzip JSON 或 generated C# snapshot。运行时只加载发布态快照。

生成质量门槛：

- API surface 不能只来自 Gallery 表；Gallery 表只能作为推荐/常用 API 层。
- ControlTheme 结构必须来自真实 `.axaml`，并保持模板父子层级。
- 示例必须来自真实 `ShowCaseItem`，`--examples all` 不能丢弃文档级稳定示例。
- 所有控件共享同一 validator，不允许按控件名写特殊逻辑。
- Button 作为首个 golden sample；其他控件按控件类型矩阵逐步纳入 golden 覆盖。
