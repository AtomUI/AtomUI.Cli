# Metadata Source Extraction 详细设计

## 目标

AtomUI Cli 的 metadata 不是运行时反射产物，而是构建期从 AtomUI 源码、Gallery、文档、构建配置和变更记录生成的离线快照。`tools/AtomUI.Cli.MetadataBuilder` 负责读取这些输入，生成 `data/*.json` 和发布态 `.json.gz`。

## 输入源

| 输入源 | 主要路径模式 | 产出字段 |
| --- | --- | --- |
| Solution | `*.slnx`、`*.sln` | 项目清单、包边界、缺失项目诊断。 |
| Build props | `build/Version.props`、`Directory.Packages.props` | AtomUI 版本、Avalonia 版本、包版本。 |
| 控件源码 | `src/AtomUI.Controls/**`、`src/AtomUI.Desktop.Controls/**` | 控件类型、属性、事件、TemplatePart、PseudoClasses、命名空间。 |
| 产品项目 | `src/AtomUI.Desktop.Controls.*/*.csproj` | 产品包、可选包、依赖关系。 |
| Gallery 配置 | `controlgallery/AtomUIGallery/AtomUIGalleryModule.cs` | 分类、导航顺序、控件显示名、路由。 |
| Gallery 示例 | `controlgallery/AtomUIGallery/ShowCases/**/*.axaml` | Demo、源码片段、场景名、ShowCaseItem 描述。 |
| Gallery ViewModel | `ShowCases/**/ViewModels/**/*.cs` | API 表、Token 表、默认值、说明文本。 |
| Localization | `ShowCases/**/Localization/*.cs` | 中英文描述、标题、Token/API 文案。 |
| Assembly metadata | `Properties/AssemblyInfo.cs` | XAML namespace 和 prefix。 |
| Changelog | `CHANGELOG.md` | 版本变更和迁移提示。 |

构建器必须允许显式配置每个输入根。缺失可选输入时输出 warning；缺失核心输入时失败。

## Adapter 分层

```text
MetadataBuilder
  -> SourceWorkspaceReader
  -> ProductProjectAdapter
  -> GalleryNavigationAdapter
  -> GalleryShowCaseAdapter
  -> ControlSourceAdapter
  -> LocalizationAdapter
  -> ChangelogAdapter
  -> SnapshotAssembler
  -> SnapshotValidator
  -> SnapshotCompressor
```

每个 adapter 输出中间模型，不直接写最终 JSON。

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

Gallery 示例是 Demo、API 和 Token 的主输入之一。

提取内容：

- `ShowCasePanel.Name`。
- `ShowCaseItem.SourceKey`。
- `ShowCaseItem.Title`。
- `ShowCaseItem.Description`。
- `DeferredContentTemplate` 内的 AXAML 片段。
- 关联 code-behind event handler。
- 关联 ViewModel binding member。

实现规则：

- 复用 Gallery source generator 的 AdditionalFiles 输入形态。
- 使用 XML parser 保留行号。
- 示例片段保留原始 AXAML，code-behind/ViewModel 片段作为补充文件。
- `SourceKey` 缺失时用 `panel-name + item-index` 生成稳定 key，并输出 warning。

## ControlSourceAdapter

控件源码用于补齐 Gallery 无法表达的结构化 API：

| 源码信号 | 输出字段 |
| --- | --- |
| `StyledProperty<T>` | styled property。 |
| `DirectProperty<TOwner,T>` | direct property。 |
| `AttachedProperty<T>` | attached property。 |
| CLR property | property。 |
| `RoutedEvent` | event。 |
| `[TemplatePart]` | template part。 |
| `[PseudoClasses]` | pseudo class。 |
| `XmlnsDefinition` | XAML namespace。 |

实现规则：

- 使用 Roslyn syntax tree 和 semantic-light symbol name 提取。
- 不编译、加载或反射程序集。
- internal property 默认不进入公开快照，除非显式配置为 template/semantic 输出。
- GeneratedFiles 默认跳过。

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
2. Gallery ViewModel API/Token 表。
3. 控件源码结构化 API。
4. Gallery ShowCase 示例和导航。
5. Changelog。

冲突处理：

- 同名 property 类型冲突：保留显式 config，输出 error。
- Gallery API 表和源码 property 不一致：输出 warning，并在字段上标记 source。
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
| `ControlSourceAdapterTests` | StyledProperty、TemplatePart、PseudoClasses。 |
| `LocalizationAdapterTests` | 中英文 fallback。 |
| `SnapshotAssemblerTests` | 冲突优先级和 source 标记。 |
| `SnapshotValidatorTests` | schema、引用完整性、公开数据边界。 |

