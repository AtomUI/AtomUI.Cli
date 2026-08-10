# dotnet atomui token 设计

## 定位

`token` 是 AtomUI Cli 的设计 Token 知识命令，用于查询共享 Token、控件 Token、派生链、资源键、主题使用点和定制建议。它不是简单的 Token 列表命令，而是主题定制和控件样式开发的查询入口。

## 所属模块

`AtomUICliMetadataModule`

命令预解析到 `token` 后只激活 Metadata 模块，运行期只读取内置 Token Snapshot，符合 AOT-first 约束。

## 调用形式

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

## 核心能力

| 能力 | 说明 |
| --- | --- |
| 共享 Token 查询 | 输出 seed、map、alias Token，并按 color、size、font、motion 等分类。 |
| 控件 Token 查询 | 输出控件 Token scope、Token 类型、资源键、注册控件、Token 列表和源码描述。 |
| 单 Token 详情 | 输出描述、类型、默认值、资源键、XAML 片段、可定制边界。 |
| 派生链查询 | 输出 shared -> control -> theme usage 的依赖链路。 |
| 主题使用点查询 | 输出 ControlTheme 中的 Selector、Setter、目标属性和资源表达式。 |
| 多格式输出 | text 面向终端，markdown 面向文档，json 面向 Agent、IDE、MCP 和 CI。 |

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `[control]` | 空 | 控件名称。未传入时查询共享 Token。 |
| `[token]` | 空 | Token 名称。传入后进入单 Token 详情模式。 |
| `--scope <shared|control|all>` | 无控件时 `shared`，有控件时 `control` | 查询范围。 |
| `--kind <seed|map|alias|control|resource|all>` | `all` | Token 层级过滤。 |
| `--category <color|size|font|motion|radius|shadow|spacing|state|layout|other>` | 空 | 产品化分类过滤。 |
| `--match <text>` | 空 | 按名称、描述、资源键、MarkupExtension 模糊搜索。 |
| `--theme <default|dark|compact|all>` | `default` | 查询指定主题算法下的值或差异。 |
| `--include <usage,chain,source,examples,diagnostics>` | 空 | 展开附加信息。 |
| `--usage` | false | 展开 ControlTheme 使用点。 |
| `--chain` | false | 展开 Token 派生链。 |
| `--source` | false | 展开定义来源和生成资源来源。 |
| `--customizable-only` | false | 只输出推荐作为外部定制入口的 Token。 |
| `--used-only` | false | 只输出已被 ControlTheme 消费的 Token。 |
| `--strict` | false | Snapshot 存在 error 级诊断时返回失败。 |
| `--format <text|markdown|json>` | `text` | 输出格式。 |

## 数据来源

`token` 命令的数据必须来自构建期对 AtomUI 项目源码的抽取结果。默认源码根是 `<AtomUICliRepoRoot>/.workspace/AtomUI`，CI 应保持同样工作区布局，或通过 `AtomUISourceRoot`、`ATOMUI_SOURCE_ROOT` 显式覆盖。

构建期由 `tools/AtomUI.Cli.MetadataBuilder` 读取源码，生成 `output/obj/AtomUI.Cli.Hosting/Generated/Metadata/BuiltInTokenSnapshot.g.cs`，并编译进 `AtomUI.Cli.Hosting`。发布后的 CLI 不读取源码目录、不读取 Git、不解析 XAML、不反射 AtomUI 控件程序集。

禁止在 `TokenSnapshotRegistry`、query service 或 command handler 中手写控件 Token 数据。缺失数据必须修复构建期抽取器或上游 AtomUI 源码，而不是在运行时补丁。

Snapshot 包含：

- 共享 Token：seed、map、alias。
- 控件 Token：Token ID、ScopeProvider、Token 属性、XML 注释。
- 资源键：SharedTokenKind、控件 TokenKind、MarkupExtension。
- 派生关系：共享 Token 到控件 Token 的计算依赖。
- 主题使用点：ControlTheme Selector、Setter、目标属性、资源表达式。
- 控件注册关系：一个 Token scope 覆盖哪些控件。
- 产品包和商业控件可见性。

Token 描述必须来自源码 XML `<summary>`。默认 text 和 markdown 输出都展示描述；`--detail` 只用于展开源码路径等长信息，不控制描述是否显示。源码缺少描述时构建期记录 `ATOMUICLI_TOKEN_DESCRIPTION_MISSING` 诊断，运行期不根据 Token 名称生成猜测性描述。

## 默认输出

`dotnet atomui token Button` 的 text 输出应优先展示可用信息：

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

`dotnet atomui token Button Padding --chain --usage` 应输出单 Token 的完整解释：

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

## 服务设计

| 服务 | 生命周期 | 说明 |
| --- | --- | --- |
| `ITokenSnapshotRegistry` | Singleton | 加载内置 Snapshot，按版本、产品包、可见性建立索引。 |
| `ITokenQueryService` | Singleton | 处理 scope、kind、category、match、theme、product 过滤。 |
| `ITokenGraphService` | Singleton | 查询 Token 派生链。 |
| `ITokenUsageQueryService` | Singleton | 查询 ControlTheme 使用点。 |
| `ITokenCommandPayloadFactory` | Transient | 组装结构化命令 payload。 |
| `ITokenOutputRenderer` | Singleton | 渲染 text、markdown、json。 |
| `TokenCommandHandler` | Transient | 参数校验和服务编排。 |

## 错误码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | `--scope`、`--kind`、`--theme`、`--include`、`--format` 非法。 |
| `ATOMUICLI_CTRL001` | 控件不存在或当前产品包不可见。 |
| `ATOMUICLI_CTRL004` | 控件存在，但指定 Token 不存在。 |
| `ATOMUICLI_DATA001` | Token Snapshot 不存在或无法加载。 |
| `ATOMUICLI_DATA003` | Snapshot 内部引用断裂。 |
| `ATOMUICLI_DATA004` | 商业控件 Token 元数据不可用或授权不可见。 |

## 相关文档

- [详细设计](design.md)
- [命令设计标准](../command-design-standard.md)
- [知识查询命令共享设计](../knowledge-query-design.md)
- [错误码标准](../error-code-standard.md)
