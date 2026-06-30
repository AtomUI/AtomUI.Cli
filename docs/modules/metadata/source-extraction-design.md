# Metadata Source Extraction 详细设计

## 目标

AtomUI Cli 的 metadata 不是运行时反射产物，而是构建期从 AtomUI 源码、Gallery、文档、构建配置和变更记录生成的离线快照。`tools/AtomUI.Cli.MetadataBuilder` 负责读取这些输入，并生成 CLI 运行时可直接加载的内置快照。

Token metadata 的首个落地形态是编译期 C# 生成文件：`AtomUI.Cli.Hosting` 在 `CoreCompile` 前执行 MetadataBuilder，读取 AtomUI 源码中的 Token 定义和 ControlTheme 使用点，生成 `BuiltInTokenSnapshot.g.cs` 到 `output/obj/AtomUI.Cli.Hosting/Generated/Metadata/`。运行时只调用生成的 `BuiltInTokenSnapshotFactory.Create()`，不读取源码、不读取 Git、不解析 XAML。

## 输入源

AtomUI 源码根目录由构建期工具解析，运行时 CLI 不读取源码目录。源码根目录解析优先级必须和命令设计标准保持一致：

1. `--source-root <path>` 命令行参数或 `AtomUITokenSourceRoot` MSBuild 属性。
2. `AtomUIDocSourceRoot` MSBuild 属性。
3. `ATOMUI_SOURCE_ROOT` 环境变量。
4. 默认约定路径：`<AtomUICliRepoRoot>/../ReferenceProjects/AtomUI`。

默认路径必须基于 AtomUI Cli 仓库根目录计算，不能基于当前 shell 工作目录计算。本地开发和 CI/CD 应使用相同工作区布局：

```text
<workspace>/
  AtomUICli/
  ReferenceProjects/
    AtomUI/
```

构建期工具不负责 clone、pull 或 checkout AtomUI 源码。源码仓库准备由外层 build system 完成，构建期工具只读取已存在目录并校验 source ref、source commit 和 target version。Release 构建必须使用锁定 commit；锁文件不匹配时生成失败。

| 输入源 | 主要路径模式 | 产出字段 |
| --- | --- | --- |
| Solution | `*.slnx`、`*.sln` | 项目清单、包边界、缺失项目诊断。 |
| Build props | `build/Version.props`、`Directory.Packages.props` | AtomUI 版本、Avalonia 版本、包版本。 |
| 控件源码 | `src/AtomUI.Controls/**`、`src/AtomUI.Desktop.Controls/**` | 控件类型、完整 API surface、public/protected 方法、事件、Avalonia property、显式接口、继承关系、TemplatePart、PseudoClasses、命名空间。 |
| 控件文档 | `docs/controls/**/overview.md`、`implementation.md`、`token.md`、`changelog.md` | 控件定位、逻辑结构说明、维护边界、Token 语义、源码索引、变更记录。 |
| ControlTheme | `src/**/Themes/*.axaml`、`src/**/Themes/*.cs` | ControlTheme target、模板变体、真实视觉树、命名节点、TemplateBinding、selector 分组、Token resource 使用点。 |
| Token 定义 | `src/**/**Token.cs`、`src/AtomUI.Core/Theme/TokenSystem/TokenDefinitions/DesignToken.*.cs` | Shared Token、Control Token、Token ID、ScopeProvider、XML summary、默认值、源码行号。 |
| Token 计算逻辑 | `CalculateTokenValues` | `SharedToken.X` 依赖、控件 Token 内部派生、局部变量派生链。 |
| Token 资源生成结果 | `src/**/GeneratedFiles/**/TokenResourceConst.g.cs` | `SharedTokenKind`、`*TokenKind`、资源键完整性校验、生成来源路径。 |
| 产品项目 | `src/AtomUI.Desktop.Controls.*/*.csproj` | 产品包、可选包、依赖关系。 |
| Gallery 配置 | `controlgallery/AtomUIGallery/AtomUIGalleryModule.cs` | 分类、导航顺序、控件显示名、路由。 |
| Gallery 示例 | `controlgallery/AtomUIGallery/ShowCases/**/*.axaml` | Demo、SourceKey、源码片段、场景名、ShowCaseItem 描述、示例分类和来源行号。 |
| Gallery ViewModel | `ShowCases/**/ViewModels/**/*.cs` | API 表、Token 表、默认值、说明文本。 |
| Localization | `ShowCases/**/Localization/*.cs` | 中英文描述、标题、Token/API 文案。 |
| Assembly metadata | `Properties/AssemblyInfo.cs` | XAML namespace 和 prefix。 |
| Changelog | `CHANGELOG.md` | 版本变更和迁移提示。 |

构建器必须允许显式配置每个输入根。缺失可选输入时输出 warning；缺失核心输入时失败。

## 源码身份锁定

构建期生成的 metadata 或 documentation snapshot 必须记录源码身份，保证快照可追溯、可复现：

```json
{
  "sourceRootConvention": "../ReferenceProjects/AtomUI",
  "sourceRef": "release/6.0",
  "sourceCommit": "<commit>",
  "targetVersion": "6.0",
  "snapshotSchemaVersion": "1.0"
}
```

源码身份校验规则：

- `sourceCommit` 必须来自输入源码仓库当前 HEAD。
- Release 构建必须校验 `sourceCommit` 与锁文件一致。
- `sourceRef` 可记录 branch、tag 或 release line，但不能替代 commit 校验。
- 生成结果不得写入本机绝对源码路径，只能写入逻辑路径和源码身份字段。
- 未传入源码根且默认路径不存在时，构建期工具必须返回明确错误，提示配置 `--source-root`、`AtomUIDocSourceRoot` 或 `ATOMUI_SOURCE_ROOT`。

## Adapter 分层

```text
MetadataBuilder
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
  -> ChangelogAdapter
  -> SnapshotAssembler
  -> SnapshotValidator
  -> SnapshotCodeWriter / SnapshotCompressor
```

每个 adapter 输出中间模型，不直接写最终运行时数据。Token snapshot 当前由 `SnapshotCodeWriter` 生成 C#；完整 metadata/documentation snapshot 后续可以继续生成 JSON 或压缩 JSON，但不得回退到运行时扫描。

## SourceWorkspaceReader

职责：

- 解析 `.slnx` 和 `.sln`。
- 校验 solution 中声明的项目路径是否存在。
- 读取 `Version.props` 和 `Directory.Packages.props`。
- 生成 `WorkspaceSourceModel`。

规则：

- solution 中声明但磁盘不存在的项目必须进入 `SourceWorkspaceDiagnostic`。
- 版本读取优先 `build/Version.props`。
- 不执行 MSBuild target。
- 不 restore 包。

## ProductProjectAdapter

从项目文件构建产品和包模型：

| 项目模式 | 产品推断 |
| --- | --- |
| `AtomUI.Controls` | shared controls。 |
| `AtomUI.Desktop.Controls` | desktop controls。 |
| `AtomUI.Desktop.Controls.DataGrid` | datagrid product。 |
| `AtomUI.Desktop.Controls.ColorPicker` | color picker product。 |
| `AtomUI.Icons.*` | icon product。 |
| `AtomUI.Fonts.*` | font product。 |

产品推断只是初始值。最终产品清单必须允许通过显式 `metadata.config.json` 覆盖 display name、visibility、冲突关系和注册方法。

## GalleryNavigationAdapter

从 Gallery 配置提取：

- 分类顺序。
- 控件导航顺序。
- 控件 route id。
- 控件 fallback display name。
- category localized resource key。

提取策略：

- 使用 Roslyn syntax tree 查找 `AddGroup`、`AddPage` 调用链。
- 不执行 Gallery 代码。
- 对无法解析的表达式保留原始文本和 warning。

## GalleryShowCaseAdapter

Gallery 示例是 Demo、usage、examples 和常见场景的主输入之一。

提取内容：

- `ShowCasePanel.Name`。
- `ShowCaseItem.SourceKey`。
- `ShowCaseItem.Title`。
- `ShowCaseItem.Description`。
- `ShowCaseItem.BadgeText`。
- 示例分类 kind，例如 basic、state、theme、integration、advanced。
- `DeferredContentTemplate` 内的 AXAML 片段。
- 关联 code-behind event handler。
- 关联 ViewModel binding member。
- 来源文件相对路径和起始行号。

实现规则：

- 复用 Gallery source generator 的 AdditionalFiles 输入形态。
- 使用 XML parser 保留行号。
- 示例片段保留原始 AXAML，code-behind/ViewModel 片段作为补充文件。
- `SourceKey` 缺失时用 `panel-name + item-index` 生成稳定 key，并输出 warning。
- `--examples all` 依赖该 adapter 输出完整示例集合，不允许在文档快照阶段丢弃文档级稳定示例。

## MarkdownControlDocAdapter

控件文档是 `doc` 命令的人工维护事实源之一。

读取内容：

- `overview.md`：控件定位、设计语言、API 与契约模型、行为与状态模型、视觉与主题模型、控件家族、兼容性不变量、专项模型、LLMS 导出来源。
- `implementation.md`：源码文件结构、核心类职责、状态与数据流、生命周期、模板接入、资源、性能、AOT 边界、维护不变量、测试入口。
- `token.md`：控件 Token 定位、分类、使用范围、主题消费点、兼容边界。
- `changelog.md`：控件级设计、API、主题、Token 和实现结构变更。

规则：

- Markdown adapter 不发明 API、事件或 ControlTheme 结构，只抽取人工维护的定位、解释、边界和源码索引。
- 文档中记录的源码路径必须校验存在；不存在时输出 diagnostic。
- 文档和源码/Gallery 冲突时，进入 SnapshotAssembler 的冲突处理，不静默选择一方。

## ControlSourceAdapter

控件源码用于生成所有控件统一的 API surface、事件契约、逻辑结构和语义结构，不只是补齐 Gallery 表：

| 源码信号 | 输出字段 |
| --- | --- |
| public constructor | constructor。 |
| `StyledProperty<T>` | styled property。 |
| `DirectProperty<TOwner,T>` | direct property。 |
| `AttachedProperty<T>` | attached property。 |
| CLR property | property。 |
| public method | public method。 |
| protected/protected virtual/protected override method | protected extension point。 |
| CLR event | clr event。 |
| `RoutedEvent` | routed event。 |
| explicit interface event | interface event。 |
| explicit interface method/property | explicit interface contract。 |
| base type / implemented interface | inheritance and logic structure。 |
| `OnApplyTemplate` / lifecycle override | lifecycle extension point。 |
| `OnPropertyChanged` / event handler / collection handler | state flow trigger。 |
| `[TemplatePart]` | template part。 |
| `[PseudoClasses]` | pseudo class。 |
| `XmlnsDefinition` | XAML namespace。 |

实现规则：

- 使用 Roslyn syntax tree 和 semantic-light symbol name 提取。
- 不编译、加载或反射程序集。
- internal property 默认不进入公开快照，除非显式配置为 template/semantic 输出。
- GeneratedFiles 默认跳过。
- public/protected API 必须保留 source path 和 source line。
- API 分类必须稳定，不能根据输出语言变化。
- Gallery API 表可以覆盖 description、priority 和 recommended tier，但不能删除源码中实际存在的 public/protected 契约。

## AvaloniaEventContractAdapter

Avalonia 事件契约必须单独建模，避免把属性变化、CLR event 和 routed event 混在一起。

提取内容：

| 信号 | 输出 |
| --- | --- |
| `RoutedEvent.Register` 或等价注册 | event kind、routing strategy、event args、owner type。 |
| CLR event wrapper | wrapper name、add/remove 目标、accessibility。 |
| 显式接口 event | interface name、event name、调用系统。 |
| `On<Property>Changed`、`OnPropertyChanged` | property-change-contract，不归类为 event。 |
| Avalonia 基类关键事件 | inherited contract，必须标注 declaring type。 |

规则：

- 如果无法解析 routing strategy，保留 unresolved diagnostic，不猜测。
- 继承自 Avalonia 的大量基础事件不默认全量输出，只输出控件文档或配置标记为关键的事件。
- 商业控件 internal event 默认不进入公开快照。

## ControlThemeAdapter

ControlTheme adapter 从真实 `.axaml` 生成主题结构和可定制边界。

提取内容：

- `ControlTheme.TargetType`。
- `Style Selector` 层级。
- `Setter Property="Template"` 下的 `ControlTemplate`。
- 模板变体 source selector。
- 模板视觉树父子层级。
- `Name` / `x:Name` 节点，尤其是 `PART_*`。
- `TemplateBinding` 映射。
- `/template/` selector。
- pseudo class selector，例如 `:disabled`、`:pointerover`、`:pressed`、`:focus-visible`。
- token resource extension 使用点，例如 `SharedTokenResource`、`ButtonTokenResource`、商业控件 Token resource。

规则：

- 必须使用 XML parser 保留行号和父子层级。
- 不能根据 semantic parts 或控件名称生成伪模板。
- 不能把命名节点列表当成模板结构；输出必须保留 tree。
- 多个 `ControlTemplate` 或 selector 变体必须分别输出。
- 无 ControlTheme 的控件必须输出 explicit diagnostic 和替代结构来源，例如 service API、adorner、popup host 或 C# 创建逻辑。

## TokenAdapter

Token adapter 生成控件 Token、共享 Token 和主题资源使用点之间的映射。

提取内容：

- 控件 Token 类型、scope id、Token kind。
- Token 默认值来源。
- `*TokenResource`、`SharedTokenResource` 和 theme variable 使用点。
- token.md 中的语义说明和兼容边界。

规则：

- Token 只表达视觉和主题语义，不承载实例状态。
- ControlTheme 中实际使用的 token 必须能追溯到 token descriptor 或 shared token。

## LocalizationAdapter

Localization 文件用于解析 Gallery resource key：

- `en_US.cs`
- `zh_CN.cs`
- `zh_TW.cs`

规则：

- 快照首期输出 `zh` 和 `en`。
- 如果中文缺失，降级到英文并记录 warning。
- 如果英文缺失，保留 key 名称并记录 warning。
- 文案提取不执行资源绑定代码。

## SnapshotAssembler

合并优先级：

1. 显式 metadata config。
2. 控件文档中的定位、逻辑说明、维护边界和源码索引。
3. 控件源码结构化 API surface、事件、继承关系和生命周期扩展点。
4. ControlTheme 真实结构、selector、TemplateBinding 和 Token 使用点。
5. Gallery ViewModel 推荐 API/Token 表。
6. Gallery ShowCase 示例、SourceKey 和导航。
7. Token 类型和 token.md 语义。
8. Changelog。

冲突处理：

- 同名 property 类型冲突：保留显式 config，输出 error。
- Gallery API 表和源码 property 不一致：输出 warning，并在字段上标记 source。
- 控件文档记录的 protected 扩展点在源码中不存在：输出 error。
- ControlTheme 中出现未登记的 `PART_*`：输出 warning；如果文档声明为 stable 但主题缺失则输出 error。
- Theme token 使用点无法解析到 Token descriptor：输出 warning；公开 release 可配置为 error。
- Gallery 示例缺少 `SourceKey`：输出 warning；公开 release 可配置为 error。
- `--examples all` 可见集合不能少于 Gallery 文档级稳定示例集合。
- 控件存在于源码但不在 Gallery 导航：进入 `hidden` 或 `internal`，除非 config 排除。
- Gallery 导航存在但源码找不到控件：输出 error。

## SnapshotValidator

必须校验：

- 每个 control 有 productId、packageId、namespace、xamlNamespace。
- 每个 packageId 在 product catalog 中存在。
- 每个 demo 的 sourceKey 在同一控件内唯一。
- 每个 token 有 scope 和 status。
- 每个 changelog entry 的 target 可解析或明确标记 `general`。
- 公开快照不包含商业私有字段。
- 每个公开控件有 `ApiSurface`，并且 public/protected/event/Avalonia property 分类稳定。
- 每个公开控件有 `LogicStructure`，至少包含继承关系、public API 分组、状态流和 theme bridge。
- 每个有主题文件的控件有 `ControlTheme` 真实 tree；没有主题文件的控件必须有 explicit diagnostic。
- 每个公开控件有 Gallery 示例；复杂控件必须有 common/integration 示例覆盖。
- 每个 `SourcePath` 必须是逻辑相对路径，不能是本机绝对路径。
- 商业控件公开快照不得包含 internal/private API、示例、主题节点或源码路径。

## SnapshotCompressor

开发态提交 plain JSON：

```text
data/versions.json
data/products.json
data/v6.json
data/v6.0.6.json
```

发布态输出 gzip：

```text
output/data/v6.json.gz
output/data/v6.0.6.json.gz
```

压缩流程不能覆盖源码树中的 plain JSON。`compress` 和 `verify` 是独立命令。

## 测试设计

| 测试 | 覆盖点 |
| --- | --- |
| `SourceWorkspaceReaderTests` | `.slnx`、missing project、Version.props。 |
| `GalleryNavigationAdapterTests` | group/page 顺序和 fallback name。 |
| `GalleryShowCaseAdapterTests` | ShowCasePanel、ShowCaseItem、DeferredContentTemplate。 |
| `MarkdownControlDocAdapterTests` | overview、implementation、token、changelog 抽取和路径校验。 |
| `ControlSourceAdapterTests` | StyledProperty、DirectProperty、AttachedProperty、public/protected 方法、显式接口、TemplatePart、PseudoClasses。 |
| `AvaloniaEventContractAdapterTests` | CLR event、RoutedEvent、interface event、property-change-contract 分类。 |
| `ControlThemeAdapterTests` | ControlTheme target、模板变体、真实视觉树、TemplateBinding、selector、Token resource 使用点。 |
| `TokenAdapterTests` | Token scope、Token kind、theme resource 使用点和 token.md 语义合并。 |
| `LocalizationAdapterTests` | 中英文 fallback。 |
| `SnapshotAssemblerTests` | 冲突优先级和 source 标记。 |
| `SnapshotValidatorTests` | schema、引用完整性、API surface、逻辑结构、ControlTheme、示例覆盖和公开数据边界。 |
