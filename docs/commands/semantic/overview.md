# dotnet atomui semantic 设计

## 定位

`semantic` 是 AtomUI Cli 的控件语义结构查询命令，用于查询控件 semantic parts、模板命名节点、伪类、selector、TemplateBinding、相关 API、相关 Token 和可定制边界。

它不是简单的 part 列表命令。`semantic` 的目标是帮助开发者和 Agent 理解一个控件的主题结构、稳定定制入口和视觉状态关系。

## 所属模块

`AtomUICliMetadataModule`

命令预解析到 `semantic` 后只激活 Metadata 模块。运行时只读取构建期生成的 `SemanticSnapshot`。

## 调用形式

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

## 核心能力

| 能力 | 说明 |
| --- | --- |
| Semantic parts 查询 | 输出控件公开或稳定的 template part、named node 和 state part。 |
| Pseudo class 查询 | 输出控件状态伪类、触发 API 和来源。 |
| 模板结构摘要 | 通过 `--include-template` 输出 ControlTheme 模板变体和视觉树摘要。 |
| Selector 查询 | 输出 part 相关 `/template/` selector、pseudo class selector 和属性 selector。 |
| API 关联 | 输出 part 关联的 StyledProperty、DirectProperty、CLR property 或生命周期。 |
| Token 关联 | 输出 part 或 selector 使用的 shared/control Token。 |
| 来源追溯 | `--detail` 输出 TemplatePart、NameScope.Find、AXAML 命名节点、selector 和文档来源。 |
| 多格式输出 | text 面向终端，markdown 面向文档，json 面向 Agent、IDE、MCP 和 CI。 |

## 数据来源

`semantic` 的数据必须来自统一构建期 [Source Analysis Pipeline](../../architecture/source-analysis-pipeline-design.md)。

构建期 Pipeline 读取：

- 控件 C# 源码中的 `[TemplatePart]`、`[PseudoClasses]`、`PseudoClasses.Set`、`NameScope.Find/Get`。
- ControlTheme AXAML 中的 `ControlTemplate`、模板视觉树、`Name` / `x:Name`、`Style Selector`、`TemplateBinding` 和 TokenResource。
- 控件文档中的 part 职责说明、稳定性和定制建议。
- Token snapshot 中的 Token 定义和 theme usage。

运行时禁止读取源码、解析 AXAML、反射控件程序集或在命令 handler 中手写 semantic 数据。

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `control` | 必填 | 控件名称。 |
| `--part <name>` | 空 | 查询指定 semantic part、模板节点或 pseudo class。 |
| `--include-template` | `false` | 输出模板变体和视觉树摘要。 |
| `--strict` | `false` | 要求控件名称和 part 名称精确匹配。 |
| `--detail` | `false` | 输出来源、selector、TemplateBinding、状态流和 diagnostics。 |
| `--format <text|json|markdown>` | `text` | 输出格式。 |

## 默认输出

```text
Button semantic parts

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
```

## 服务设计

| 服务 | 生命周期 | 说明 |
| --- | --- | --- |
| `SemanticSnapshotRegistry` | Singleton | 加载内置 `SemanticSnapshot`。 |
| `SemanticQueryService` | Singleton | 控件解析、part 过滤、模板摘要和 diagnostics 查询。 |
| `SemanticOutputRenderer` | Singleton | 渲染 text、markdown、json。 |
| `SemanticCommandHandler` | Transient | 参数校验和服务编排。 |

## 错误码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG001` | 缺少 `control`。 |
| `ATOMUICLI_ARG002` | 选项值非法。 |
| `ATOMUICLI_CTRL001` | 控件不存在或当前产品包不可见。 |
| `ATOMUICLI_CTRL005` | semantic part 不存在。 |
| `ATOMUICLI_DATA001` | `SemanticSnapshot` 不存在或无法加载。 |
| `ATOMUICLI_DATA003` | Snapshot 内部引用断裂。 |
| `ATOMUICLI_DATA004` | 商业控件 semantic 数据不可用或授权不可见。 |

## 相关文档

- [详细设计](design.md)
- [Source Analysis Pipeline 架构设计](../../architecture/source-analysis-pipeline-design.md)
- [命令设计标准](../command-design-standard.md)
- [知识查询命令共享设计](../knowledge-query-design.md)
- [错误码标准](../error-code-standard.md)
