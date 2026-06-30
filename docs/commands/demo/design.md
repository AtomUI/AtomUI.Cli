# dotnet atomui demo 详细设计

## 1. 命令定位

`demo` 是 AtomUI Cli 的控件示例查询命令。它负责浏览控件示例库、查询单个示例详情、提取可复制代码片段，并为 Agent、IDE、文档生成和 CI 校验提供结构化示例数据。

`demo` 回答的问题：

- 某个控件有哪些可用示例。
- 示例分别属于什么场景，例如 basic、state、theme、layout、integration、advanced。
- 某个稳定 SourceKey 对应的示例如何使用。
- 示例包含哪些 XAML、C# 或后续可扩展的代码片段。
- 示例依赖哪些包、命名空间、XAML xmlns 和产品数据。
- 示例来自哪个 Gallery 或 documentation snapshot 来源。
- 如何继续跳转到完整文档、控件信息、Token 或语义结构。

`demo` 不负责解释完整控件 API，不输出完整控件文档，不分析用户项目，不运行示例，不读取运行时源码目录。完整控件说明交给 `doc`，控件契约摘要交给 `info`，Token 深查交给 `token`，语义和模板深查交给 `semantic`。

`demo` 与 `doc --example` 的边界：

| 命令 | 定位 |
| --- | --- |
| `demo Button` | 浏览 Button 的示例列表。 |
| `demo Button button-loading` | 输出某个示例的详情和代码。 |
| `demo Button button-loading --code-only` | 只输出代码，适合复制和脚本消费。 |
| `doc Button --example button-loading` | 在完整控件文档上下文中输出某个示例。 |

示例主键必须使用 documentation snapshot 中的稳定 `SourceKey`，例如 `button-loading`、`button-color-variant`。命令不保留旧 demo name 兼容逻辑。

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
| 默认输出格式 | text |
| 数据来源 | documentation snapshot 的 `ControlDocument.Examples` |

`demo` 必须被 command pre-parser 识别为 Metadata 模块命令。执行 `demo` 时只激活 Core 模块、Metadata 模块，以及 manifest 显式声明的 required modules，不挂载项目分析或写入模块。

## 3. 调用语法

```bash
dotnet atomui demo Button
dotnet atomui demo Button --list
dotnet atomui demo Button --all
dotnet atomui demo Button --all --format markdown
dotnet atomui demo Button --scenario state
dotnet atomui demo Button --match loading
dotnet atomui demo Button button-loading
dotnet atomui demo Button button-loading --format markdown
dotnet atomui demo Button button-loading --code-only --code-language xaml
dotnet atomui demo Button button-color-variant --source
dotnet atomui demo DataGrid --product datagrid
dotnet atomui demo DataGrid datagrid-editing --product datagrid --code-language all
dotnet atomui demo Button --format json
```

## 4. 命令模式

`demo` 有三种稳定模式。模式由 `demo-key`、`--list`、`--all` 和 `--code-only` 共同决定。

| 模式 | 触发条件 | 行为 |
| --- | --- | --- |
| list | 未传入 `demo-key`，或显式传入 `--list` / `--all` | 列出匹配示例；默认摘要列表，`--all` 展开每个示例详情和代码。 |
| detail | 传入 `demo-key` 且未传入 `--code-only` | 输出单个示例详情、代码、来源和相关命令。 |
| code | 传入 `demo-key` 且传入 `--code-only` | stdout 只输出代码片段，不输出标题、说明、diagnostics 或 related。 |

规则：

- `demo Button` 默认是 list 模式，不允许再自动输出第一个示例代码。
- `--list` 强制 list 模式；如果同时传入 `demo-key`，只把 `demo-key` 当作匹配条件，不输出详情。
- `--all` 强制 expanded list 模式；一次展开所有匹配示例的说明和代码块。
- `--all` 可以与 `--scenario`、`--match`、`--code-language`、`--source` 和 `--related false` 组合。
- `--all` 与 `demo-key` 同时使用时，`demo-key` 作为 SourceKey/title/description 匹配条件，而不是 detail 模式。
- `--code-only` 必须指定 `demo-key`，否则返回 `ATOMUICLI_ARG002`。
- `--code-only` 不允许输出 markdown 标题、说明、warning、related 或 JSON envelope。
- `--format json` 与 `--code-only` 同时出现时返回 `ATOMUICLI_ARG002`，避免 stdout 契约歧义。

## 5. 参数与选项

### 5.1 参数

| 参数 | 类型 | 默认值 | 校验失败 | 说明 |
| --- | --- | --- | --- | --- |
| `control` | string | 必填 | `ATOMUICLI_ARG001` | 控件名，例如 `Button`、`DataGrid`。 |
| `demo-key` | string | null | `ATOMUICLI_CTRL003` | 示例稳定 SourceKey，例如 `button-loading`。 |

### 5.2 专属选项

| 选项 | 类型 | 默认值 | 合法值 | 适用模式 | 校验失败 | 说明 |
| --- | --- | --- | --- | --- | --- | --- |
| `--list` | bool | false | true/false | list | 无 | 强制列表模式。 |
| `--all` | bool | false | true/false | list | 无 | 展开所有匹配示例，输出说明和代码。 |
| `--scenario` | string | null | snapshot 中存在的 scenario | list | strict 时 `ATOMUICLI_CTRL003` | 按场景过滤，例如 `basic`、`state`、`theme`、`layout`、`integration`、`advanced`。 |
| `--match` | string | null | 非空字符串 | list | 无 | 按 SourceKey、标题、说明、scenario 模糊搜索。 |
| `--code-only` | bool | false | true/false | code | `ATOMUICLI_ARG002` | 只输出代码，必须指定 `demo-key`。 |
| `--code-language` | enum | `all` | `xaml`、`csharp`、`all` | detail/code/all | `ATOMUICLI_ARG002` | 控制输出的代码片段语言。 |
| `--source` | bool | false | true/false | detail/all | 无 | 输出 SourceKey、sourcePath、sourceLine、snapshot 来源。 |
| `--related` | bool | true | true/false | detail/all | 无 | 是否输出相关命令；`--related false` 关闭 related 区块。 |
| `--strict` | bool | false | true/false | all | 无 | 控件名、SourceKey、scenario 要求精确匹配，不做建议匹配。 |

### 5.3 全局选项

| 选项 | 默认值 | 说明 |
| --- | --- | --- |
| `--format <text\|json\|markdown>` | `text` | 输出格式。 |
| `--markdown` | false | `--format markdown` 快捷方式。 |
| `--product <id>` | null | 限定产品，例如商业控件 `datagrid`。 |
| `--target-version <version>` | 内置默认版本 | 选择目标 AtomUI 版本。 |
| `--data-root <path>` | 内置数据根 | 追加外部 metadata/documentation snapshot。 |
| `--lang <zh\|en>` | CLI 默认语言 | 输出说明语言，不影响代码语言。 |
| `--detail` | false | list/detail 模式输出更多来源、包和 snippet 元数据。 |

### 5.4 破坏性调整

AtomUI Cli 尚未发布，`demo` 不保留旧选项和旧参数兼容：

- 不支持 `--language`，统一使用 `--code-language`。
- 不支持旧 demo name，统一使用 documentation snapshot 的 `SourceKey`。
- 不支持从旧 `MetadataCatalog.Demos` 查询示例。
- help、文档、JSON payload 和错误提示都只展示新契约。

## 6. Options 类型

```csharp
public enum DemoCommandMode
{
    List,
    Detail,
    Code
}

public enum DemoCodeLanguage
{
    All,
    Xaml,
    CSharp
}

public sealed record DemoCommandOptions(
    GlobalCliOptions Global,
    string? Control,
    string? DemoKey,
    string? Scenario,
    string? Match,
    bool ListOnly,
    bool ExpandAll,
    bool CodeOnly,
    DemoCodeLanguage CodeLanguage,
    string? InvalidCodeLanguage,
    bool RemovedLanguageOption,
    bool IncludeSource,
    bool IncludeRelated,
    bool Strict) : IAtomUICliCommandOptions
{
    public DemoCommandMode Mode { get; }
}
```

解析要求：

- `DemoCodeLanguage` 必须使用显式 parser，不使用异常控制流。
- `--language` 不再解析；如果出现，返回 `ATOMUICLI_ARG002` 并提示使用 `--code-language`。
- `IncludeRelated` 默认 true，`--related false` 关闭 related 区块；`--no-related` 可作为隐藏别名保留，但帮助页不展示。
- `ExpandAll` 由 `--all` 设置，必须让 `Mode` 返回 list。
- `Mode` 可以在 options 构造后由 handler 或 parser 计算，但不能让 renderer 自行猜测。

## 7. Handler 依赖

```csharp
public sealed class DemoCommandHandler(
    MetadataQueryService metadata,
    DemoQueryService demos,
    DemoOutputRenderer renderer)
    : IAtomUICliCommandHandler<DemoCommandOptions>;
```

Handler 职责：

- 校验参数组合。
- 构造 `DemoQuery`。
- 调用 `DemoQueryService`。
- 将 result union 映射为 `AtomUICliResult`。
- 根据输出格式选择 renderer 或返回 JSON payload。

Handler 禁止：

- 直接读取 `MetadataCatalog.Demos`。
- 运行时读取 Gallery 文件、源码目录或 Git。
- 在 handler 中拼接 JSON 字符串。
- 根据控件名写专用逻辑。

## 8. 领域服务契约

```csharp
public sealed record DemoQuery(
    string ControlName,
    string? DemoKey,
    string? ProductId,
    string? Scenario,
    string? Match,
    DemoCommandMode Mode,
    DemoCodeLanguage CodeLanguage,
    bool Strict,
    string TargetVersion,
    string Language);

public enum DemoQueryStatus
{
    ListFound,
    DemoFound,
    ControlNotFound,
    DemoNotFound,
    ScenarioNotFound,
    SnapshotUnavailable,
    DataUnavailable
}

public sealed record DemoQueryResult(
    DemoQueryStatus Status,
    ControlDocument? Control,
    IReadOnlyList<ControlExampleDocument> Demos,
    ControlExampleDocument? SelectedDemo,
    IReadOnlyList<string> AvailableScenarios,
    IReadOnlyList<DemoSuggestion> Suggestions,
    IReadOnlyList<DocumentWarning> Warnings,
    string? ErrorCode,
    string? ErrorMessage);
```

查询规则：

1. 根据 target version、language 和 product 选择 documentation snapshot。
2. 根据 `control` 解析唯一 `ControlDocument`。
3. list 模式下从 `ControlDocument.Examples` 读取全部示例。
4. detail/code 模式下按 `SourceKey` 匹配；strict 时大小写也必须一致。
5. 应用 `--scenario` 和 `--match` 过滤。
6. 应用 `--code-language` 过滤 snippets。
7. 生成 suggestions 和 available scenarios。

`ScenarioNotFound` 处理：

- 非 strict list 模式：返回 `ListFound`，`Demos=[]`，warnings 包含无匹配说明。
- strict 模式：返回 `ScenarioNotFound`，映射 `ATOMUICLI_CTRL003`。

`DemoNotFound` 处理：

- 返回 `ATOMUICLI_CTRL003`。
- 必须带 suggestions，优先给同控件下相近 SourceKey。
- stderr 中不输出长列表；JSON payload 或 diagnostics 可输出 available examples。

## 9. 数据模型

`demo` 运行时数据来自 documentation snapshot：

```csharp
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
```

要求：

- `SourceKey` 在同一控件内唯一。
- `Kind` 作为 scenario 使用，必须是稳定小写标识。
- `Priority` 决定 list 输出顺序。
- `Snippets.Language` 首版支持 `xml`/`xaml` 和 `csharp`；输出选项统一使用 `xaml`、`csharp`、`all`。
- `SourcePath` 不允许使用本机绝对路径。
- 商业控件示例可以在公开快照中输出摘要，但私有代码片段必须依赖商业 data-root。

## 10. 输出模型

JSON 输出必须是结构化 payload，不能把整段文本作为 payload string。

```csharp
public sealed record DemoCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    DemoCommandMode Mode,
    DemoCodeLanguage CodeLanguage,
    DemoControlPayload Control,
    DemoSourceIdentityPayload Source,
    string? RequestedDemoKey,
    string? Scenario,
    string? Match,
    IReadOnlyList<string> AvailableScenarios,
    IReadOnlyList<DemoItemPayload> Demos,
    DemoItemPayload? SelectedDemo,
    IReadOnlyList<DemoSuggestionPayload> Suggestions,
    IReadOnlyList<DemoWarningPayload> Warnings) : IAtomUICliJsonPayload;

public sealed record DemoControlPayload(
    string Id,
    string Name,
    string DisplayName,
    string CategoryId,
    string ProductId,
    string PackageId,
    bool IsCommercial);

public sealed record DemoSourceIdentityPayload(
    string SnapshotSchemaVersion,
    string SnapshotId,
    string SourceRootConvention,
    string SourceRef,
    string SourceCommit,
    string GeneratedAt);

public sealed record DemoItemPayload(
    string SourceKey,
    string Title,
    string Description,
    string Scenario,
    int Priority,
    string? BadgeText,
    IReadOnlyList<DemoCodeSnippetPayload> Snippets,
    string SourcePath,
    int? SourceLine,
    IReadOnlyList<DemoRelatedCommandPayload> RelatedCommands);

public sealed record DemoCodeSnippetPayload(
    string Language,
    string Code,
    string? SourcePath,
    int? SourceLine);

public sealed record DemoRelatedCommandPayload(
    string Command,
    string Description);

public sealed record DemoSuggestionPayload(
    string SourceKey,
    string Title,
    string Scenario);

public sealed record DemoWarningPayload(
    string Code,
    string Message);
```

字段要求：

- `Demos` 在 list 模式下包含匹配示例摘要；detail/code 模式可以包含空数组。
- `SelectedDemo` 在 detail/code 模式下必填。
- `RelatedCommands` 至少包含 `dotnet atomui doc <control> --example <sourceKey>` 和 `dotnet atomui info <control>`。
- `AvailableScenarios` 按示例实际 scenario 去重排序。
- `Warnings` 用于无匹配、商业数据缺失等非阻断情况。

## 11. 输出格式

### 11.1 text

list 模式默认 text 输出：

```text
Button demos (12)

Basic
  button-type             按钮类型
  button-shape            按钮外形
  button-size             按钮尺寸

State
  button-loading          加载状态
  button-danger           危险动作
  button-disabled         禁用状态

Theme
  button-ghost            幽灵按钮
  button-gradient         渐变按钮
  button-color-variant    颜色变体

Run:
  dotnet atomui demo Button button-loading
  dotnet atomui demo Button --scenario theme
```

expanded list 模式 text 输出：

```text
Button demos (12)

## Basic

button-type - 按钮类型
展示 Default、Primary、Dashed、Text 和 Link 的视觉优先级。

XAML:
  <atom:Button ButtonType="Primary" Content="Primary" />

button-shape - 按钮外形
使用 Shape 切换默认、圆角和圆形图标按钮。

XAML:
  <atom:Button Shape="Round" Content="Round" />
```

detail 模式 text 输出：

```text
Button demo: 加载状态
SourceKey: button-loading
Scenario: state

命令执行中设置 IsLoading，避免重复触发并提供进度反馈。

XAML:
  <atom:Button ButtonType="Primary" IsLoading="True" Content="Saving" />

Related:
  dotnet atomui doc Button --example button-loading
  dotnet atomui info Button
```

text 输出规则：

- list 模式按 scenario 分组，组内按 priority 排序。
- `--detail` 时 list 输出 sourcePath、snippet languages 和 badge。
- `--all` 时 list 输出每个匹配示例的 SourceKey、标题、说明、代码块和 related commands。
- `--all --source` 时每个示例追加 sourcePath，并在尾部输出 snapshot 来源。
- detail 模式按 code-language 输出代码块。
- 不使用 Markdown 表格作为 text 输出。

### 11.2 markdown

list 模式 markdown 输出：

```markdown
# Button Demos

## Basic

| SourceKey | Title | Description |
| --- | --- | --- |
| `button-type` | 按钮类型 | 展示按钮视觉优先级。 |

## State

| SourceKey | Title | Description |
| --- | --- | --- |
| `button-loading` | 加载状态 | 命令执行中设置 IsLoading。 |
```

detail 模式 markdown 输出：

````markdown
# Button Demo: 加载状态

SourceKey: `button-loading`
Scenario: `state`

命令执行中设置 IsLoading，避免重复触发并提供进度反馈。

```xml
<atom:Button ButtonType="Primary" IsLoading="True" Content="Saving" />
```

## Related

```bash
dotnet atomui doc Button --example button-loading
dotnet atomui info Button
```
````

expanded list 模式 markdown 输出：

````markdown
# Button Demos

## Basic

### 按钮类型

SourceKey: `button-type`

展示 Default、Primary、Dashed、Text 和 Link 的视觉优先级。

```xaml
<atom:Button ButtonType="Primary" Content="Primary" />
```
````

### 11.3 json

json 输出完整 `DemoCommandPayload`。

规则：

- `payload` 必须是 object。
- list、detail 使用同一 schema。
- snippets 中的 code 保持原样，不做 markdown 转义。
- `--all --format json` 不改变 JSON schema；payload 仍通过 `demos` 返回全部匹配示例。

### 11.4 code-only

code-only 只输出代码：

```bash
dotnet atomui demo Button button-loading --code-only --code-language xaml
```

```xml
<atom:Button ButtonType="Primary" IsLoading="True" Content="Saving" />
```

规则：

- 不输出标题。
- 不输出 warnings。
- 不输出 JSON envelope。
- `--code-language all` 时按 snapshot 中 snippet 顺序输出多个代码块内容，中间用一个空行分隔。
- 如果请求语言没有 snippet，返回 `ATOMUICLI_ARG002`，提示可用语言。

## 12. 执行流程

```text
CliCommandDispatcher
  -> DemoCommandOptions.Parse
  -> DemoCommandHandler
  -> DemoQueryService
  -> DemoCommandPayloadFactory
  -> DemoOutputRenderer
  -> AtomUICliResult
```

详细流程：

1. 校验 `control` 必填。
2. 校验 `--code-language`。
3. 校验 `--code-only`、`--format json`、`--list`、`--all` 和 `demo-key` 的组合。
4. 解析 effective mode。
5. 构造 `DemoQuery`。
6. `DemoQueryService` 从 `DocumentSnapshotRegistry` 查询控件示例。
7. 将 result union 映射为错误或 payload。
8. 如果是 `--code-only`，由 handler 返回纯代码字符串。
9. 如果是 json，返回 `DemoCommandPayload`。
10. 如果是 markdown/text，调用 `DemoOutputRenderer`。

## 13. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | 缺少 `control`。 | `2` |
| `ATOMUICLI_ARG002` | `--code-language` 非法、参数组合非法、请求语言无 snippet。 | `2` |
| `ATOMUICLI_CTRL001` | 控件不存在。 | `3` |
| `ATOMUICLI_CTRL002` | 控件匹配多个产品且未指定 `--product`。 | `3` |
| `ATOMUICLI_CTRL003` | demo-key 不存在，或 strict scenario 无匹配。 | `3` |
| `ATOMUICLI_DATA001` | documentation snapshot 不可用。 | `4` |
| `ATOMUICLI_DATA004` | 商业 data-root 不可用。 | `4` |

错误输出要求：

- stdout 在失败时为空。
- stderr 输出统一错误格式。
- `ATOMUICLI_CTRL003` 需要给出建议，例如 `Try: dotnet atomui demo Button button-loading`。
- json 失败输出遵循全局 error envelope。

## 14. AOT-first 约束

- 示例数据只来自 documentation snapshot。
- 不运行 Gallery。
- 不读取 `../ReferenceProjects/AtomUI`。
- 不扫描文件系统发现示例。
- 不反射加载 AtomUI 控件程序集。
- JSON payload DTO 必须兼容 source generated JSON 输出路径。
- 商业示例通过显式 data-root 或商业数据模块贡献，不动态加载未知程序集。

## 15. Help 文案要求

`help demo` 必须展示完整高质量选项，不允许只显示 `--list` 和 `--code-only`。

推荐 help 摘要：

```text
Usage:
  dotnet atomui demo <control> [demo-key] [options]

Arguments:
  control                 Control name.
  demo-key                Stable demo SourceKey, such as button-loading.

Options:
  --list                  List matching demos.
  --all                   Expand all matching demos with code.
  --scenario <name>       Filter demos by scenario.
  --match <text>          Search SourceKey, title, or description.
  --code-only             Print code only. Requires demo-key.
  --code-language <lang>  Code language: xaml, csharp, all.
  --source                Include source path and snapshot identity.
  --related <true|false>  Include related commands.
  --strict                Disable fuzzy suggestions.
  --format <format>       Output format: text, json, markdown.
```

Examples 必须覆盖 list、detail、code-only、scenario、json：

```bash
dotnet atomui demo Button
dotnet atomui demo Button --all
dotnet atomui demo Button --scenario state
dotnet atomui demo Button button-loading
dotnet atomui demo Button button-loading --code-only --code-language xaml
dotnet atomui demo Button --format json
```

## 16. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| default list | `demo Button` | 输出按 scenario 分组的示例列表，不输出第一段代码。 |
| list explicit | `demo Button --list` | 输出示例摘要。 |
| all text | `demo Button --all` | 输出所有匹配示例说明和代码块。 |
| all markdown | `demo Button --all --format markdown` | 输出完整 markdown 示例文档，包含每个示例 fenced code block。 |
| all language filter | `demo Button --all --code-language xaml` | 只展开 XAML snippets。 |
| scenario filter | `demo Button --scenario state` | 只输出 state 示例。 |
| match filter | `demo Button --match loading` | 输出 `button-loading`。 |
| detail | `demo Button button-loading` | 输出标题、SourceKey、说明、XAML 代码和 related。 |
| markdown detail | `demo Button button-loading --format markdown` | 输出 markdown heading 和 fenced code block。 |
| json list | `--format json demo Button` | payload object，包含 demos 和 availableScenarios。 |
| json detail | `--format json demo Button button-loading` | payload.selectedDemo.sourceKey 为 `button-loading`。 |
| code-only xaml | `demo Button button-loading --code-only --code-language xaml` | stdout 只有 XAML。 |
| code-only missing key | `demo Button --code-only` | 返回 `ATOMUICLI_ARG002`。 |
| invalid language | `demo Button button-loading --code-language js` | 返回 `ATOMUICLI_ARG002`。 |
| missing demo | `demo Button none` | 返回 `ATOMUICLI_CTRL003`，带建议。 |
| commercial product | `demo DataGrid --product datagrid` | 从商业/公开可见 snapshot 查询 DataGrid 示例。 |
| strict scenario miss | `demo Button --scenario none --strict` | 返回 `ATOMUICLI_CTRL003`。 |
| scenario miss non-strict | `demo Button --scenario none` | 成功输出空列表和 warning。 |

## 17. 实现文件建议

建议按职责拆分，不继续把所有逻辑塞进 `MetadataCommandHandlers.cs`：

- `src/AtomUI.Cli.Hosting/Metadata/Demo/DemoCommandOptions.cs`
- `src/AtomUI.Cli.Hosting/Metadata/Demo/DemoCommandHandler.cs`
- `src/AtomUI.Cli.Hosting/Metadata/Demo/DemoQueryService.cs`
- `src/AtomUI.Cli.Hosting/Metadata/Demo/DemoCommandPayloads.cs`
- `src/AtomUI.Cli.Hosting/Metadata/Demo/DemoOutputRenderer.cs`

如果短期不移动目录，也必须至少拆出：

- `DemoCommandPayloads.cs`
- `DemoQueryService.cs`
- `DemoOutputRenderer.cs`

旧 `MetadataCatalog.Demos` 不进入 `demo` 新实现路径；实现 `demo` 重构时应同步删除或隔离相关测试数据，避免继续形成第二套示例来源。
