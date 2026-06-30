# dotnet atomui semantic 详细设计

## 1. 命令定位

`semantic` 是 AtomUI Cli 的控件语义结构查询命令。它面向主题定制、ControlTheme 维护、Agent 生成样式代码和控件文档校验，负责解释控件的 semantic parts、模板命名节点、伪类、模板 selector、TemplateBinding、相关 API、相关 Token 和可定制边界。

`semantic` 不是简单的 part 名称列表。它必须能回答：

- 控件有哪些公开或稳定的 semantic parts。
- 这些 part 在源码中是如何声明或依赖的。
- 这些 part 在 ControlTheme 真实模板树中的节点类型和层级是什么。
- 哪些 pseudo class 会影响控件视觉状态。
- 每个 part 和哪些 API、Token、selector、模板绑定相关。
- 哪些节点推荐用户定制，哪些只是主题内部实现。

## 2. 所属模块与激活

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliMetadataModule` |
| 命令分组 | knowledge |
| 读写属性 | 只读 |
| 项目上下文 | 不要求在用户项目目录内执行 |
| 写入确认 | 不需要 |
| 支持格式 | `text`、`markdown`、`json` |
| Handler 生命周期 | Transient |
| 查询服务生命周期 | Singleton |

命令预解析阶段识别到 `semantic` 后，只激活 Metadata 模块。不得因为执行 `semantic` 命令加载 setup、project-analysis、mcp 等无关模块。

## 3. 数据来源硬规则

`semantic` 的数据必须来自统一构建期 Source Analysis Pipeline 生成的 `SemanticSnapshot`。

运行期禁止：

- 读取 AtomUI 源码目录。
- 解析 `.cs`、`.axaml` 或 markdown。
- 反射 AtomUI 控件程序集。
- 从 `MetadataSnapshot` 或 `DocumentSnapshotRegistry` 手写 semantic parts。
- 根据控件名、part 名或 Token 名生成猜测性描述。

Pipeline 架构见 [Source Analysis Pipeline 架构设计](../../architecture/source-analysis-pipeline-design.md)。

## 4. 构建期输入

`semantic` 依赖以下 Source Analysis features：

| Feature | 产生者 | 用途 |
| --- | --- | --- |
| `ControlCatalog` | `ProductProjectProcessor`、`GalleryNavigationProcessor` | 控件名称、包、产品、分类和商业可见性。 |
| `ControlApi` | `ControlSourceProcessor` | 关联 API、属性、生命周期和继承关系。 |
| `TemplatePart` | `ControlSourceProcessor` | C# `[TemplatePart]` 声明。 |
| `PseudoClass` | `ControlSourceProcessor` | C# `[PseudoClasses]`、`PseudoClasses.Set` 和状态触发条件。 |
| `ControlTheme` | `ControlThemeProcessor` | ControlTheme、模板变体和 target type。 |
| `ThemeVisualTree` | `ControlThemeProcessor` | 真实模板树和命名节点。 |
| `ThemeSelector` | `ControlThemeProcessor` | `/template/` selector、pseudo class selector 和属性 selector。 |
| `TemplateBinding` | `ControlThemeProcessor` | 模板节点与控件 API 的绑定关系。 |
| `TokenUsage` | `TokenProcessor` | part、selector 和 TokenResource 的关系。 |
| `MarkdownDoc` | `MarkdownDocProcessor` | part 职责说明、稳定性和定制建议。 |
| `SemanticContract` | `SemanticContractProcessor` | 合并后的 semantic part 契约。 |

## 5. 查询语法

```bash
dotnet atomui semantic Button
dotnet atomui semantic Button --part PART_LoadingIcon
dotnet atomui semantic Button --part :loading
dotnet atomui semantic Button --include-template
dotnet atomui semantic Button --detail
dotnet atomui semantic Button --format markdown
dotnet atomui semantic Button --format json
dotnet atomui semantic DataGrid --product datagrid --include-template --detail
```

## 6. 参数与选项

| 参数或选项 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `control` | string | 必填 | 缺失返回 `ATOMUICLI_ARG001` | 控件名称。 |
| `--part <name>` | string | null | 不存在返回 `ATOMUICLI_CTRL005` | 查询指定 semantic part、模板节点或 pseudo class。 |
| `--include-template` | bool | false | 无 | 输出模板变体和视觉树摘要。 |
| `--strict` | bool | false | 无 | 控件名和 part 名要求精确匹配。 |
| `--detail` | global bool | false | 无 | 输出来源、selector、TemplateBinding、状态流和 diagnostics。 |
| `--format <text|markdown|json>` | enum | `text` | 非法返回 `ATOMUICLI_ARG002` | 输出格式。 |

## 7. Options 类型

```csharp
public sealed record SemanticCommandOptions(
    GlobalCliOptions Global,
    string? Control,
    string? Part,
    bool IncludeTemplate,
    bool Strict) : IAtomUICliCommandOptions;
```

`--detail` 是全局选项，从 `GlobalCliOptions.Detail` 读取。

## 8. Snapshot 模型

```csharp
public sealed record SemanticSnapshot(
    string SchemaVersion,
    string SnapshotId,
    string TargetVersion,
    SourceIdentityDocument Source,
    IReadOnlyList<SemanticControlDocument> Controls,
    IReadOnlyList<SourceDiagnosticDocument> Diagnostics);
```

```csharp
public sealed record SemanticControlDocument(
    string Name,
    string PackageId,
    string ProductId,
    bool IsCommercial,
    IReadOnlyList<SemanticPartDocument> Parts,
    IReadOnlyList<PseudoClassDocument> PseudoClasses,
    IReadOnlyList<ThemeTemplateDocument> Templates,
    IReadOnlyList<ThemeSelectorDocument> Selectors,
    IReadOnlyList<SemanticStateFlowDocument> StateFlows,
    IReadOnlyList<RelatedCommandDocument> Related);
```

```csharp
public sealed record SemanticPartDocument(
    string Name,
    string Kind,
    string? NodeType,
    string Stability,
    string Responsibility,
    IReadOnlyList<string> RelatedApis,
    IReadOnlyList<string> RelatedTokens,
    IReadOnlyList<string> RelatedPseudoClasses,
    IReadOnlyList<string> Selectors,
    IReadOnlyList<SourceContributionDocument> Sources);
```

`Kind` 取值：

| Kind | 说明 |
| --- | --- |
| `template-part` | C# TemplatePart 或 NameScope 依赖确认的模板契约。 |
| `named-node` | AXAML 真实命名节点，但不一定是公开模板契约。 |
| `pseudo-class` | 控件状态伪类。 |
| `template-selector` | 主要通过 selector 暴露的可样式化目标。 |
| `state-flow` | API 或内部状态驱动的视觉状态。 |

`Stability` 取值：

| Stability | 说明 |
| --- | --- |
| `stable` | 推荐作为公开定制或 Agent 生成入口。 |
| `runtime-required` | 控件代码依赖该节点，变更风险高。 |
| `theme-public` | 主题可样式化节点，文档或 config 标记为公开。 |
| `theme-internal` | 主题内部节点，不建议用户依赖。 |
| `implementation-detail` | 内部实现细节，只在 `--detail` 或 diagnostics 中出现。 |
| `inherited` | 来自 Avalonia 或基类约定。 |

## 9. 数据合并规则

`SemanticContractProcessor` 合并以下来源：

| 来源 | 示例 | 语义 |
| --- | --- | --- |
| C# `[TemplatePart]` | `[TemplatePart("PART_LoadingIcon", typeof(Icon))]` | 公开模板契约。 |
| C# `NameScope.Find/Get` | `e.NameScope.Find<WaveSpiritDecorator>("PART_WaveSpirit")` | 控件运行时实际依赖。 |
| C# `[PseudoClasses]` | `[PseudoClasses(ButtonPseudoClass.Loading)]` | 公开状态伪类声明。 |
| C# `PseudoClasses.Set` | `PseudoClasses.Set(ButtonPseudoClass.Loading, IsLoading)` | 伪类触发条件。 |
| AXAML `Name` / `x:Name` | `Name="PART_ContentPresenter"` | 真实模板命名节点。 |
| AXAML selector | `^:loading /template/ #PART_LoadingIcon` | part 的样式化入口。 |
| AXAML `TemplateBinding` | `Content="{TemplateBinding Content}"` | part 和 API 的关系。 |
| AXAML TokenResource | `{atom:ButtonTokenResource IconSize}` | part 和 Token 的关系。 |
| 控件文档 | `semantic` 或 `implementation` 文档 | 职责说明和稳定性。 |

合并规则：

- C# TemplatePart 和 AXAML 命名节点一致时，part 为 `stable`。
- C# NameScope 查找和 AXAML 命名节点一致时，part 为 `runtime-required` 或 `stable`。
- AXAML 有 `PART_*` 但源码没有声明或查找时，默认 `theme-internal`，可由文档或 config 提升为 `theme-public`。
- selector 指向不存在节点时输出 `ATOMUICLI_SRC_SELECTOR_TARGET_MISSING`。
- TemplatePart 声明但主题缺失时输出 `ATOMUICLI_SRC_TEMPLATE_PART_MISSING`。
- NameScope 查找但主题缺失时输出 `ATOMUICLI_SRC_NAMESCOPE_PART_MISSING`。
- selector 使用伪类但源码无法解析时输出 `ATOMUICLI_SRC_PSEUDO_CLASS_UNRESOLVED`。

## 10. 查询流程

1. 校验 `control` 是否存在。
2. 从 `SemanticSnapshotRegistry` 获取当前版本 snapshot。
3. 根据 `control` 和 `--product` 解析控件。
4. 如果传入 `--part`，按 part 名、pseudo class 名、selector target 名匹配。
5. 应用 `--strict` 控制精确匹配或候选建议。
6. 根据 `--include-template` 决定是否包含 template summary。
7. 根据 `--detail` 决定是否输出 source contribution、selector、TemplateBinding、state flow 和 diagnostics。
8. 构造 `SemanticCommandPayload`。
9. 根据 `--format` 渲染 text、markdown 或 json。

## 11. Payload 模型

```csharp
public sealed record SemanticCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string SnapshotId,
    SemanticQueryPayload Query,
    SemanticControlPayload Control,
    IReadOnlyList<SemanticPartPayload> Parts,
    IReadOnlyList<PseudoClassPayload> PseudoClasses,
    IReadOnlyList<ThemeTemplatePayload> Templates,
    IReadOnlyList<ThemeSelectorPayload> Selectors,
    IReadOnlyList<SemanticStateFlowPayload> StateFlows,
    IReadOnlyList<DiagnosticPayload> Diagnostics,
    IReadOnlyList<RelatedCommandPayload> Related);
```

JSON 输出必须保留完整 payload。text 和 markdown 只是 payload 视图，不允许成为数据源。

## 12. Text 输出规范

默认输出：

```text
Button semantic parts

Control
  Package: AtomUI.Desktop.Controls
  Product: desktop

Template parts
  PART_WaveSpirit        WaveSpiritDecorator   runtime-required
    Responsibility: 点击波纹反馈承载节点。
    APIs: IsWaveSpiritEnabled
    Tokens: ButtonToken
  PART_LoadingIcon       LoadingOutlined       stable
    Responsibility: 渲染 loading 动画并响应 IsLoading。
    APIs: IsLoading
    Pseudo classes: :loading
  PART_ContentPresenter  ContentPresenter      stable
    Responsibility: 渲染 Content。
    APIs: Content, ContentTemplate

Pseudo classes
  :loading     IsLoading 为 true 时应用。
  :danger      IsDanger 为 true 时应用。
  :icononly    Icon 不为空且 Content 为空时应用。

Run:
  dotnet atomui semantic Button --include-template
  dotnet atomui semantic Button --part PART_LoadingIcon --detail
  dotnet atomui doc Button --section theme
```

单 part 输出：

```text
Button.PART_LoadingIcon

Kind: template-part
Node type: LoadingOutlined
Stability: stable
Responsibility: 渲染 loading 动画并响应 IsLoading。

Related APIs:
  IsLoading

Related pseudo classes:
  :loading

Selectors:
  ^:loading /template/ atom|Icon#PART_LoadingIcon

Sources:
  theme-named-node: src/AtomUI.Desktop.Controls/Buttons/Themes/ButtonTheme.axaml:750
  theme-selector: src/AtomUI.Desktop.Controls/Buttons/Themes/ButtonTheme.axaml:...
```

## 13. Markdown 输出规范

```markdown
# Button Semantic Parts

## Template Parts

| Part | Node | Stability | Responsibility | APIs | Tokens |
| --- | --- | --- | --- | --- | --- |
| PART_LoadingIcon | LoadingOutlined | stable | 渲染 loading 动画并响应 IsLoading。 | IsLoading | ButtonToken |

## Pseudo Classes

| Pseudo class | Trigger | Source |
| --- | --- | --- |
| :loading | IsLoading 为 true 时应用。 | Button.cs |
```

## 14. JSON 输出规范

JSON 输出服务 Agent、IDE、MCP 和自动文档生成，必须包含：

- snapshot identity。
- query 参数。
- part 列表。
- pseudo class 列表。
- template summary。
- selector。
- state flow。
- source contribution。
- diagnostics。

## 15. 错误与退出码

错误码、退出码、stdout/stderr 边界遵守 [错误码标准](../error-code-standard.md)。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | `2` | 缺少 `control`。 |
| `ATOMUICLI_ARG002` | `2` | `--format` 等选项非法。 |
| `ATOMUICLI_CTRL001` | `3` | 控件不存在或当前产品包不可见。 |
| `ATOMUICLI_CTRL005` | `3` | semantic part 不存在。 |
| `ATOMUICLI_DATA001` | `4` | `SemanticSnapshot` 不存在或无法加载。 |
| `ATOMUICLI_DATA003` | `4` | Snapshot 内部引用断裂。 |
| `ATOMUICLI_DATA004` | `4` | 商业控件 semantic 数据不可用或授权不可见。 |

## 16. AOT 与运行时约束

- `SemanticSnapshot`、payload DTO、错误 DTO 必须进入 source generated JSON context。
- Runtime 不使用 Roslyn。
- Runtime 不使用 XML parser 解析 AXAML。
- Runtime 不反射控件程序集。
- Renderer 不根据名称生成职责、来源或关联 API。
- Query service 不补齐构建期缺失数据。

## 17. 测试设计

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| 控件总览 | `semantic Button` | 输出真实 PART 和 pseudo class，不再只有 root/contentPresenter。 |
| 单 part | `semantic Button --part PART_LoadingIcon` | 输出 node type、stability、API、selector 和 source。 |
| pseudo class | `semantic Button --part :loading` | 输出触发条件和相关 API。 |
| 模板摘要 | `semantic Button --include-template` | 输出模板变体和视觉树摘要。 |
| JSON | `semantic Button --format json` | 输出完整结构化 payload。 |
| 缺失 part | `semantic Button --part Missing` | 返回 `ATOMUICLI_CTRL005` 和候选建议。 |
| DataGrid | `semantic DataGrid --product datagrid` | 输出商业控件公开 semantic 数据或授权提示。 |
| 与 doc 一致 | `doc Button --section theme` | 与 `semantic Button --include-template` 使用相同 source identity。 |

## 18. 相关文档

- [Source Analysis Pipeline 架构设计](../../architecture/source-analysis-pipeline-design.md)
- [命令设计标准](../command-design-standard.md)
- [知识查询命令共享设计](../knowledge-query-design.md)
- [错误码标准](../error-code-standard.md)
