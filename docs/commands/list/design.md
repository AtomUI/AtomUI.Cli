# dotnet atomui list 详细设计

## 1. 命令定位

`list` 是 AtomUI Cli 的能力发现入口。它必须让开发者和 Agent 一次性知道当前 AtomUI metadata 中有哪些控件、包、产品和分类，而不是只输出少量样例数据。

`list` 不读取用户项目，不扫描程序集，不连接远程服务。它只读取 CLI 内置 metadata snapshot 以及用户通过 `--data-root` 显式追加的数据根。默认输出必须与 AtomUI Gallery 的 Components 导航分类保持一致，确保 CLI、文档和 Gallery 使用同一套分类语言。

核心目标：

- 默认 `dotnet atomui list` 按 Gallery Components 分类输出完整控件清单。
- 支持按 kind、category、product、package 过滤。
- 支持 text、json、markdown 三种输出。
- JSON 输出必须稳定，便于 Agent 做后续 `info`、`doc`、`demo`、`token` 查询。
- 运行时不能依赖 Gallery 工程，Gallery 只作为 metadata 生成和校验的上游事实来源。

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

`list` 所属模块是 metadata 模块。命令执行时只激活 Core 和 Metadata，不激活 Project Analysis、Setup、MCP 或商业控件运行时模块。

## 3. 调用语法

```bash
dotnet atomui list
dotnet atomui list controls
dotnet atomui list categories
dotnet atomui list packages
dotnet atomui list products
dotnet atomui list all
dotnet atomui list --category data-display
dotnet atomui list controls --category data-entry
dotnet atomui list controls --package AtomUI.Desktop.Controls.DataGrid
dotnet atomui list --product desktop --format json
dotnet atomui list --format markdown
```

语义规则：

- 空 kind 等同于 `controls`，按分类输出可见控件。
- `controls` 只输出可实例化或可直接使用的控件，不输出非控件页面入口。
- `all` 输出 products、packages、categories、controls 的汇总。
- `--category` 支持分类 id、展示名和常见连字符写法，例如 `Data Display`、`data-display`、`datadisplay`。
- `--package` 只过滤 controls，不影响 products。
- `--product` 使用全局选项，过滤 product 下的 packages 和 entries。

## 4. 参数与选项

| 参数或选项 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `kind` | enum | `controls` | 非法值返回 `ATOMUICLI_ARG002` | 位置参数，可选 `controls`、`categories`、`packages`、`products`、`all`。 |
| `--kind` | enum | null | 与位置参数冲突返回 `ATOMUICLI_ARG002` | `kind` 的显式选项形式。 |
| `--category` | string | null | 未知分类返回空列表和 warning | 分类过滤，只用于 `controls`、`all`。 |
| `--package` | string | null | 未知包返回 `ATOMUICLI_PKG002` | 包过滤，支持精确包 id。 |
| `--since` | version | null | 版本无法解析返回 `ATOMUICLI_DATA005` | 只输出指定版本之后引入或变更的条目。 |
| `--include-hidden` | bool | false | 无 | 输出 hidden/internal 条目，但不输出私有字段。 |
| `--product` | global string | all visible | 未知产品返回 `ATOMUICLI_PKG001` | 产品过滤。 |
| `--target-version` | global version | 默认 metadata 版本 | 版本不存在返回 `ATOMUICLI_DATA005` | 查询目标 metadata 版本。 |
| `--data-root` | global path | 内置数据根 | 不可读或授权失败返回 `ATOMUICLI_DATA004` | 追加 metadata 数据根。 |
| `--detail` | global bool | false | 无 | text/markdown 输出增加 route、package、visibility 等字段。 |

## 5. Kind 定义

```csharp
public enum ListKind
{
    Controls,
    Categories,
    Packages,
    Products,
    All
}
```

| kind | 输出内容 |
| --- | --- |
| `controls` | 可实例化、可在应用中直接使用或可通过包启用的控件。 |
| `categories` | Gallery Components 分类列表。 |
| `packages` | AtomUI NuGet 包列表。 |
| `products` | AtomUI 产品线列表。 |
| `all` | 汇总输出 products、packages、categories、controls。 |

## 6. Options 类型

```csharp
public sealed record ListCommandOptions(
    GlobalCliOptions Global,
    ListKind Kind,
    string? Category,
    string? PackageId,
    string? Since,
    bool IncludeHidden) : IAtomUICliCommandOptions;
```

Parser 只做语法级校验。未知 product、package、target version、data root 等数据级校验由 metadata query service 完成。

## 7. Metadata 事实来源

`list` 的展示顺序和分类必须来自 metadata snapshot，不允许在 handler 中写死排序逻辑。metadata snapshot 的上游事实来源是 AtomUI Gallery Components 导航配置。

同步原则：

1. Gallery Components 的一级分组是 CLI category 的事实来源。
2. Gallery Components 下的页面顺序是 CLI display order 的事实来源。
3. Gallery 顶层页面不进入 `list controls`，例如 Overview、Community。
4. Gallery 中不是控件的页面入口不进入 `list controls`。
5. 商业或独立包控件必须明确 `packageId`、`productId`、`isCommercial` 和 `isOptionalPackage`。
6. CLI 运行时不引用 Gallery 程序集；同步可以由人工维护、脚本生成或后续 source generator 完成。

## 8. 分类与条目清单

首版 metadata 必须覆盖 Gallery Components 的 7 个分类、71 个控件。

### 8.1 General

| Display order | Name | Package | 说明 |
| --- | --- | --- | --- |
| 30 | Button | AtomUI.Desktop.Controls | 命令按钮。 |
| 40 | FloatButton | AtomUI.Desktop.Controls | 悬浮操作按钮。 |
| 50 | SplitButton | AtomUI.Desktop.Controls | 分裂式按钮。 |
| 60 | Separator | AtomUI.Desktop.Controls | 分隔线。 |

### 8.2 Layout

| Display order | Name | Package | 说明 |
| --- | --- | --- | --- |
| 110 | FlexPanel | AtomUI.Desktop.Controls | 弹性布局容器。 |
| 120 | Grid | AtomUI.Desktop.Controls | 栅格布局。 |
| 130 | Space | AtomUI.Desktop.Controls | 间距布局。 |
| 140 | Splitter | AtomUI.Desktop.Controls | 可拖拽分割布局。 |
| 150 | Masonry | AtomUI.Desktop.Controls | 瀑布流布局。 |

### 8.3 Navigation

| Display order | Name | Package | 说明 |
| --- | --- | --- | --- |
| 210 | Breadcrumb | AtomUI.Desktop.Controls | 面包屑导航。 |
| 220 | ButtonSpinner | AtomUI.Desktop.Controls | 带递增递减操作的按钮式导航输入。 |
| 230 | ComboBox | AtomUI.Desktop.Controls | 组合选择控件。 |
| 240 | DropdownButton | AtomUI.Desktop.Controls | 下拉按钮。 |
| 250 | Menu | AtomUI.Desktop.Controls | 菜单导航。 |
| 260 | Pagination | AtomUI.Desktop.Controls | 分页导航。 |
| 270 | Steps | AtomUI.Desktop.Controls | 步骤导航。 |
| 280 | TabControl | AtomUI.Desktop.Controls | 选项卡内容容器。 |
| 290 | TabStrip | AtomUI.Desktop.Controls | 选项卡条。 |

### 8.4 Data Entry

| Display order | Name | Package | 说明 |
| --- | --- | --- | --- |
| 310 | AutoComplete | AtomUI.Desktop.Controls | 自动完成输入。 |
| 320 | Cascader | AtomUI.Desktop.Controls | 级联选择。 |
| 330 | CheckBox | AtomUI.Desktop.Controls | 复选框。 |
| 340 | ColorPicker | AtomUI.Desktop.Controls.ColorPicker | 颜色选择器。 |
| 350 | DatePicker | AtomUI.Desktop.Controls | 日期选择器。 |
| 360 | TimePicker | AtomUI.Desktop.Controls | 时间选择器。 |
| 370 | Form | AtomUI.Desktop.Controls | 表单容器和校验入口。 |
| 380 | LineEdit | AtomUI.Desktop.Controls | 单行文本输入。 |
| 390 | Mentions | AtomUI.Desktop.Controls | 提及输入。 |
| 400 | NumberUpDown | AtomUI.Desktop.Controls | 数字步进输入。 |
| 410 | RadioButton | AtomUI.Desktop.Controls | 单选按钮。 |
| 420 | Rate | AtomUI.Desktop.Controls | 评分输入。 |
| 430 | Select | AtomUI.Desktop.Controls | 选择器。 |
| 440 | Slider | AtomUI.Desktop.Controls | 滑动输入。 |
| 450 | ToggleSwitch | AtomUI.Desktop.Controls | 开关输入。 |
| 460 | TreeSelect | AtomUI.Desktop.Controls | 树形选择器。 |
| 470 | Transfer | AtomUI.Desktop.Controls | 穿梭框。 |
| 480 | Upload | AtomUI.Desktop.Controls | 上传入口。 |

### 8.5 Data Display

| Display order | Name | Package | 说明 |
| --- | --- | --- | --- |
| 510 | Avatar | AtomUI.Desktop.Controls | 头像展示。 |
| 520 | Badge | AtomUI.Desktop.Controls | 徽标展示。 |
| 530 | Calendar | AtomUI.Desktop.Controls | 日历展示。 |
| 540 | Card | AtomUI.Desktop.Controls | 卡片容器。 |
| 550 | Carousel | AtomUI.Desktop.Controls | 轮播展示。 |
| 560 | Collapse | AtomUI.Desktop.Controls | 折叠面板。 |
| 570 | Descriptions | AtomUI.Desktop.Controls | 描述列表。 |
| 580 | DataGrid | AtomUI.Desktop.Controls.DataGrid | 数据表格。 |
| 590 | Expander | AtomUI.Desktop.Controls | 展开收起容器。 |
| 600 | Empty | AtomUI.Desktop.Controls | 空状态展示。 |
| 610 | ImagePreviewer | AtomUI.Desktop.Controls | 图片预览。 |
| 620 | GroupBox | AtomUI.Desktop.Controls | 分组容器。 |
| 630 | InfoFlyout | AtomUI.Desktop.Controls | 信息浮层。 |
| 640 | List | AtomUI.Desktop.Controls | 列表展示。 |
| 650 | QRCode | AtomUI.Desktop.Controls | 二维码展示。 |
| 660 | Segmented | AtomUI.Desktop.Controls | 分段选择展示与输入。 |
| 670 | Statistic | AtomUI.Desktop.Controls | 统计数值展示。 |
| 680 | Tag | AtomUI.Desktop.Controls | 标签展示。 |
| 690 | Timeline | AtomUI.Desktop.Controls | 时间线。 |
| 700 | TreeView | AtomUI.Desktop.Controls | 树形展示。 |
| 710 | Tooltip | AtomUI.Desktop.Controls | 文字提示。 |
| 720 | Tour | AtomUI.Desktop.Controls | 引导游览。 |

### 8.6 Feedback

| Display order | Name | Package | 说明 |
| --- | --- | --- | --- |
| 810 | Alert | AtomUI.Desktop.Controls | 警告提示。 |
| 820 | Drawer | AtomUI.Desktop.Controls | 抽屉面板。 |
| 830 | Message | AtomUI.Desktop.Controls | 全局消息提示。 |
| 840 | Modal | AtomUI.Desktop.Controls | 模态对话框。 |
| 850 | Notification | AtomUI.Desktop.Controls | 通知提醒。 |
| 860 | PopupConfirm | AtomUI.Desktop.Controls | 气泡确认框。 |
| 870 | ProgressBar | AtomUI.Desktop.Controls | 进度条。 |
| 880 | Result | AtomUI.Desktop.Controls | 结果页展示。 |
| 890 | Skeleton | AtomUI.Desktop.Controls | 骨架屏。 |
| 900 | Spin | AtomUI.Desktop.Controls | 加载中状态。 |
| 910 | Watermark | AtomUI.Desktop.Controls | 水印。 |

### 8.7 Other

| Display order | Name | Package | 说明 |
| --- | --- | --- | --- |
| 1010 | BorderBeam | AtomUI.Desktop.Controls | 边框光束效果。 |
| 1020 | Splash | AtomUI.Desktop.Controls | 启动画面。 |

## 9. Metadata 模型

首版可以继续使用内置 C# snapshot，但模型必须具备后续从 JSON 或 source generator 迁移的空间。

```csharp
public sealed record ControlEntryDescriptor(
    string Name,
    string DisplayName,
    string CategoryId,
    string CategoryName,
    string ProductId,
    string PackageId,
    string Namespace,
    string GalleryRoute,
    string Description,
    int DisplayOrder,
    bool IsCommercial,
    bool IsOptionalPackage,
    bool IsHidden);
```

兼容性规则：

- 现有 `ControlDescriptor` 可以临时保留，但必须能映射到 `ControlEntryDescriptor`。
- `controls` 查询只返回 `ControlEntryDescriptor`。
- `info/doc/demo/token/semantic` 默认只接受 `ControlEntryDescriptor` 中存在的控件。
- `PackageDescriptor.Controls` 继续表示控件列表，不包含非控件页面入口。

## 10. 产品与包归属

首版产品线：

| Product id | Name | 说明 |
| --- | --- | --- |
| `desktop` | AtomUI Desktop | 默认桌面控件产品线。 |
| `datagrid` | AtomUI DataGrid | DataGrid 独立产品线。 |
| `colorpicker` | AtomUI ColorPicker | ColorPicker 独立包能力。 |

首版包：

| Package id | Product id | Optional | Commercial | 包含条目 |
| --- | --- | --- | --- | --- |
| `AtomUI.Desktop.Controls` | desktop | false | false | 大部分桌面控件。 |
| `AtomUI.Desktop.Controls.ColorPicker` | colorpicker | true | false | ColorPicker。 |
| `AtomUI.Desktop.Controls.DataGrid` | datagrid | true | true | DataGrid。 |

商业和可选包不影响默认可见性。默认列表仍展示 DataGrid、ColorPicker，但 text 输出中必须能通过 `--detail` 看出它们来自独立包。

## 11. 查询服务设计

```csharp
public interface IListQueryService
{
    ValueTask<ListQueryResult> QueryAsync(
        ListQuery query,
        CancellationToken cancellationToken);
}

public sealed record ListQuery(
    ListKind Kind,
    string? ProductId,
    string? Category,
    string? PackageId,
    string? Since,
    bool IncludeHidden);

public abstract record ListQueryResult
{
    public sealed record Success(ListCommandPayload Payload) : ListQueryResult;

    public sealed record UnknownProduct(string ProductId) : ListQueryResult;

    public sealed record UnknownPackage(string PackageId) : ListQueryResult;

    public sealed record DataUnavailable(string Reason) : ListQueryResult;
}
```

过滤顺序：

1. 读取目标 metadata snapshot。
2. 合并 `--data-root` 追加数据。
3. 应用 hidden visibility。
4. 应用 product 过滤。
5. 应用 package 过滤。
6. 应用 category 过滤。
7. 应用 since 过滤。
8. 按 `Category.DisplayOrder`、`Control.DisplayOrder` 排序。

未知 category 不作为硬错误：输出空列表并添加 warning。未知 product/package 是硬错误，因为它们通常表示拼写或数据源配置错误。

## 12. 输出模型

```csharp
public sealed record ListCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    ListKind Kind,
    ProductFilterDto ProductFilter,
    string? CategoryFilter,
    string? PackageFilter,
    IReadOnlyList<ControlCategoryListDto> Categories,
    IReadOnlyList<ProductListItemDto> Products,
    IReadOnlyList<PackageListItemDto> Packages,
    IReadOnlyList<WarningDto> Warnings);

public sealed record ControlCategoryListDto(
    string Id,
    string Name,
    int DisplayOrder,
    IReadOnlyList<ControlListItemDto> Items);

public sealed record ControlListItemDto(
    string Name,
    string DisplayName,
    string CategoryId,
    string CategoryName,
    string ProductId,
    string PackageId,
    string Namespace,
    string GalleryRoute,
    string Description,
    bool IsCommercial,
    bool IsOptionalPackage);
```

JSON 输出必须保留空数组，不能把 payload 压成字符串。字段命名使用 camelCase。

## 13. Text 输出设计

默认 `dotnet atomui list`：

```text
AtomUI controls (71)

General
  Button            AtomUI.Desktop.Controls
  FloatButton       AtomUI.Desktop.Controls
  ...

Layout
  FlexPanel         AtomUI.Desktop.Controls
  Grid              AtomUI.Desktop.Controls
  ...
```

`dotnet atomui list controls --category data-display --detail`：

```text
Data Display controls (22)

  DataGrid          AtomUI.Desktop.Controls.DataGrid
    product: datagrid
    route: Components/DataDisplay/DataGrid
    commercial: yes
    optional package: yes

  Card              AtomUI.Desktop.Controls
    product: desktop
    route: Components/DataDisplay/Card
    commercial: no
    optional package: no
```

`list categories`：

```text
General
Layout
Navigation
Data Entry
Data Display
Feedback
Other
```

输出要求：

- text 不输出 JSON schema 字段。
- 分组顺序必须和 Gallery Components 一致。
- 条目顺序必须和 Gallery Components 一致。
- 默认 text 必须包含 count。
- 空结果要输出明确提示，例如 `No controls matched the current filters.`。

## 14. JSON 输出示例

```json
{
  "schemaVersion": "1.0",
  "command": "list",
  "targetVersion": "1.0.0-alpha.1",
  "kind": "controls",
  "productFilter": {
    "value": null,
    "isAll": true
  },
  "categoryFilter": null,
  "packageFilter": null,
  "categories": [
    {
      "id": "data-display",
      "name": "Data Display",
      "displayOrder": 500,
      "items": [
        {
          "name": "DataGrid",
          "displayName": "DataGrid",
          "categoryId": "data-display",
          "categoryName": "Data Display",
          "productId": "datagrid",
          "packageId": "AtomUI.Desktop.Controls.DataGrid",
          "namespace": "AtomUI.Desktop.Controls",
          "galleryRoute": "Components/DataDisplay/DataGrid",
          "description": "数据表格。",
          "isCommercial": true,
          "isOptionalPackage": true
        }
      ]
    }
  ],
  "products": [],
  "packages": [],
  "warnings": []
}
```

## 15. Markdown 输出设计

markdown 输出用于文档生成和上下文注入：

```markdown
# AtomUI Controls

## Data Display

| Name | Package | Product |
| --- | --- | --- |
| DataGrid | AtomUI.Desktop.Controls.DataGrid | datagrid |
```

要求：

- 标题稳定。
- 表格列稳定。
- `--detail` 时增加 `Route`、`Commercial`、`Optional Package` 列。
- 不输出终端专用对齐空格。

## 16. 错误与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | kind 非法，或 `--kind` 与位置参数冲突。 | `2` |
| `ATOMUICLI_PKG001` | `--product` 不存在。 | `3` |
| `ATOMUICLI_PKG002` | `--package` 不存在。 | `3` |
| `ATOMUICLI_DATA001` | metadata snapshot 不可用。 | `4` |
| `ATOMUICLI_DATA002` | metadata schema 不兼容。 | `4` |
| `ATOMUICLI_DATA004` | `--data-root` 不可读或授权失败。 | `4` |
| `ATOMUICLI_DATA005` | `--target-version` 或 `--since` 版本无法解析。 | `4` |

未知 category 不返回硬错误：

- exit code 为 `0`。
- payload `warnings` 增加 warning。
- text 输出 `No controls matched category '<category>'.`。

## 17. AOT-first 约束

- CLI 运行时不得扫描 AtomUI 控件程序集。
- CLI 运行时不得引用 Gallery 程序集。
- metadata snapshot 必须是显式数据，或由构建期 generator 生成。
- kind、category、package、product 的归一化必须使用显式 map。
- JSON payload DTO 必须纳入 source-generated JSON context。
- 不使用 `Type.GetType`、程序集枚举、attribute scanning 或 XML doc runtime 解析来发现控件。

## 18. 实现切分

建议按以下边界实现：

| 文件或类型 | 职责 |
| --- | --- |
| `MetadataSnapshot.cs` | 内置 metadata snapshot，包含 7 类 71 条控件数据。 |
| `ControlEntryDescriptor.cs` | 控件条目领域模型。 |
| `ControlCategoryDescriptor.cs` | 分类领域模型。 |
| `ListCommandOptions.cs` | list 命令 options。 |
| `ListQueryService.cs` | 过滤、排序、错误分支、payload 构造。 |
| `ListCommandHandler.cs` | handler，只负责调用 query service 和渲染 payload。 |
| `ListTextRenderer.cs` | text 输出。 |
| `ListMarkdownRenderer.cs` | markdown 输出。 |
| `ListJsonDtos.cs` | JSON DTO 和 source generation 支持。 |

短期可以保留在 `src/AtomUI.Cli.Hosting/Metadata` 下；后续 metadata 模块独立成项目时再移动到独立 assembly。

## 19. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| 默认列表完整性 | `list` | 输出 7 个分类，包含 `Button`、`DataGrid`、`ColorPicker`、`Tour`、`Watermark`。 |
| 默认数量 | `list --format json` | controls 总数为 71。 |
| 分类顺序 | `list categories` | 顺序为 General、Layout、Navigation、Data Entry、Data Display、Feedback、Other。 |
| Data Display 分类 | `list --category data-display` | 输出 22 个条目，包含 DataGrid、Card、TreeView、Tour。 |
| Data Entry 分类 | `list controls --category data-entry` | 输出 18 个控件，包含 AutoComplete、ColorPicker、TreeSelect、Upload。 |
| 包过滤 | `list controls --package AtomUI.Desktop.Controls.DataGrid` | 只输出 DataGrid。 |
| 产品过滤 | `list --product datagrid` | 只输出 DataGrid 相关条目和包。 |
| categories kind | `list categories` | 只输出分类，不输出控件详情。 |
| products kind | `list products` | 输出 desktop、datagrid、colorpicker。 |
| packages kind | `list packages` | 输出 AtomUI.Desktop.Controls、ColorPicker、DataGrid 包。 |
| JSON 结构 | `list --format json` | payload 是结构化对象，包含 categories/items/warnings，不是字符串。 |
| markdown 结构 | `list --format markdown` | 输出稳定 heading 和表格。 |
| 未知 category | `list --category bad` | exit 0，空列表，warning。 |
| 未知 package | `list --package bad` | 返回 `ATOMUICLI_PKG002`。 |
| 未知 product | `list --product bad` | 返回 `ATOMUICLI_PKG001`。 |

## 20. 验收口径

实现完成后，下面命令必须可作为人工验收入口：

```bash
dotnet atomui list
dotnet atomui list controls
dotnet atomui list categories
dotnet atomui list --category data-display
dotnet atomui list --format json
```

验收标准：

- 不再只输出 `DataGrid` 和 `Button`。
- 默认列表按 7 个分类展示完整 AtomUI Gallery Components 控件清单。
- `DataGrid` 明确显示属于 `AtomUI.Desktop.Controls.DataGrid`，并标记商业/可选包。
- `ColorPicker` 明确显示属于 `AtomUI.Desktop.Controls.ColorPicker`，并标记可选包。
- text 面向人类可读，json 面向 Agent 稳定可解析，markdown 可直接放进文档。
