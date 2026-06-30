# dotnet atomui demo 设计

## 定位

`demo` 是 AtomUI Cli 的控件示例查询命令，用于浏览控件示例、查看单个示例详情、提取可复制代码片段。它面向控件使用者、控件维护者、Agent 上下文注入、IDE 集成和离线文档生成。

示例数据来自 documentation snapshot 的 `ControlDocument.Examples`。稳定主键是 `SourceKey`，例如 `button-loading`、`button-color-variant`。

## 所属模块

`AtomUICliMetadataModule`

## 调用形式

```bash
dotnet atomui demo <control> [demo-key] [options]
```

常用示例：

```bash
dotnet atomui demo Button
dotnet atomui demo Button --all
dotnet atomui demo Button --all --format markdown
dotnet atomui demo Button --scenario state
dotnet atomui demo Button --match loading
dotnet atomui demo Button button-loading
dotnet atomui demo Button button-loading --code-only --code-language xaml
dotnet atomui demo DataGrid --product datagrid
dotnet atomui demo Button --format json
```

## 命令模式

| 模式 | 触发方式 | 输出 |
| --- | --- | --- |
| list | `demo Button` 或 `demo Button --list` | 按 scenario 分组列出示例。 |
| expanded list | `demo Button --all` | 一次展开所有匹配示例的说明和代码。 |
| detail | `demo Button button-loading` | 输出单个示例说明、代码、来源和相关命令。 |
| code | `demo Button button-loading --code-only` | stdout 只输出代码。 |

默认行为：`demo Button` 是列表模式，不输出第一个示例代码。

## 参数

| 参数 | 必填 | 说明 |
| --- | --- | --- |
| `control` | 是 | 控件名称，例如 `Button`、`DataGrid`。 |
| `demo-key` | 否 | 示例稳定 SourceKey，例如 `button-loading`。指定后进入 detail 模式，配合 `--code-only` 进入 code 模式。 |

## 专属选项

| 选项 | 默认值 | 说明 |
| --- | --- | --- |
| `--list` | `false` | 强制列表模式。 |
| `--all` | `false` | 展开所有匹配示例，输出说明和代码块。 |
| `--scenario <name>` | 空 | 按场景过滤，例如 `basic`、`state`、`theme`、`layout`、`integration`、`advanced`。 |
| `--match <text>` | 空 | 按 SourceKey、标题、说明、scenario 模糊搜索。 |
| `--code-only` | `false` | 只输出代码。必须指定 `demo-key`。 |
| `--code-language <xaml\|csharp\|all>` | `all` | 控制输出代码语言。 |
| `--source` | `false` | detail 模式输出 SourceKey、sourcePath、sourceLine 和 snapshot 来源。 |
| `--related <true\|false>` | `true` | detail 模式输出相关命令；`false` 关闭 related 区块。 |
| `--strict` | `false` | 控件名、SourceKey、scenario 要求精确匹配。 |

## 全局选项

| 选项 | 默认值 | 说明 |
| --- | --- | --- |
| `--format <text\|json\|markdown>` | `text` | 输出格式。 |
| `--markdown` | `false` | `--format markdown` 快捷方式。 |
| `--product <id>` | 空 | 限定产品，例如 `datagrid`。 |
| `--target-version <version>` | 内置默认版本 | 选择 AtomUI 目标版本。 |
| `--data-root <path>` | 内置数据根 | 追加外部 metadata/documentation snapshot。 |
| `--lang <zh\|en>` | CLI 默认语言 | 输出说明语言，不影响代码语言。 |
| `--detail` | `false` | 输出更多来源、包和 snippet 元数据。 |

## 破坏性调整

AtomUI Cli 尚未发布，`demo` 不保留旧行为兼容：

- 不支持 `--language`，统一使用 `--code-language`。
- 不支持旧 demo name，统一使用 documentation snapshot 的 `SourceKey`。
- 不支持从旧 `MetadataCatalog.Demos` 查询示例。
- help、文档、JSON payload 和错误提示都只展示新契约。

## 输出

### 列表模式

```bash
dotnet atomui demo Button
```

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

### 展开全部示例

```bash
dotnet atomui demo Button --all --code-language xaml
```

```text
Button demos (12)

## Basic

button-type - 按钮类型
展示 Default、Primary、Dashed、Text 和 Link 的视觉优先级。

XAML:
  <atom:Button ButtonType="Primary" Content="Primary" />
```

`--all` 支持 `--scenario`、`--match`、`--code-language`、`--source` 和 `--related false`。它不改变 JSON schema；`--all --format json` 仍通过 `payload.demos` 返回全部匹配示例。

### 详情模式

```bash
dotnet atomui demo Button button-loading
```

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

### 纯代码模式

```bash
dotnet atomui demo Button button-loading --code-only --code-language xaml
```

```xml
<atom:Button ButtonType="Primary" IsLoading="True" Content="Saving" />
```

纯代码模式不输出标题、说明、warning、related 或 JSON envelope。

## JSON 输出

`--format json` 返回稳定结构化 payload：

```json
{
  "schemaVersion": "1.0",
  "command": "demo",
  "mode": "List",
  "control": {
    "name": "Button",
    "packageId": "AtomUI.Desktop.Controls"
  },
  "availableScenarios": ["basic", "state", "theme"],
  "demos": [
    {
      "sourceKey": "button-loading",
      "title": "加载状态",
      "scenario": "state",
      "snippets": [
        {
          "language": "xaml",
          "code": "<atom:Button ButtonType=\"Primary\" IsLoading=\"True\" Content=\"Saving\" />"
        }
      ]
    }
  ]
}
```

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `DemoCommandOptions` | Scoped value | 控件名、SourceKey、模式和过滤条件。 |
| `DemoCommandHandler` | Transient | 参数校验、查询调用和输出分发。 |
| `DemoQueryService` | Singleton | 从 documentation snapshot 查询示例。 |
| `DemoOutputRenderer` | Singleton | text、markdown 和 code-only 渲染。 |
| `DocumentSnapshotRegistry` | Singleton | 提供内置和外部 documentation snapshot。 |

`demo` 新实现不得继续依赖旧 `MetadataCatalog.Demos`。

## 执行流程

1. 校验 `control` 必填。
2. 校验 `--code-language` 和参数组合。
3. 计算模式：list、detail 或 code。
4. 根据 target version、language、product 查询 documentation snapshot。
5. 解析唯一控件文档。
6. 从 `ControlDocument.Examples` 查询示例。
7. 应用 `--scenario`、`--match` 和 `--code-language`。
8. 构造 payload。
9. 根据 `--format` 或 `--code-only` 输出结果。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | `2` | 缺少 `control`。 |
| `ATOMUICLI_ARG002` | `2` | `--code-language` 非法、参数组合非法、请求语言无 snippet。 |
| `ATOMUICLI_CTRL001` | `3` | 控件不存在。 |
| `ATOMUICLI_CTRL002` | `3` | 控件匹配多个产品。 |
| `ATOMUICLI_CTRL003` | `3` | 示例 SourceKey 不存在，或 strict scenario 无匹配。 |
| `ATOMUICLI_DATA001` | `4` | documentation snapshot 不可用。 |
| `ATOMUICLI_DATA004` | `4` | 商业 data-root 不可用。 |

## AOT 约束

- 示例内容只来自 documentation snapshot。
- 不运行 Gallery。
- 不读取 `../ReferenceProjects/AtomUI`。
- 不扫描文件系统发现示例。
- 不反射加载 AtomUI 控件程序集。
- 商业示例通过显式 data-root 或商业数据模块贡献。

## 测试点

- `demo Button` 输出列表，不输出第一个代码片段。
- `demo Button --all` 输出所有匹配示例说明和代码。
- `demo Button --all --format markdown` 输出每个示例的 markdown heading 和 fenced code block。
- `demo Button --scenario state` 只输出 state 示例。
- `demo Button --match loading` 匹配 `button-loading`。
- `demo Button button-loading` 输出详情和代码。
- `demo Button button-loading --format markdown` 输出 fenced code block。
- `--format json demo Button` 输出结构化 payload。
- `demo Button button-loading --code-only --code-language xaml` stdout 只有 XAML。
- `demo Button --code-only` 返回 `ATOMUICLI_ARG002`。
- `demo Button none` 返回 `ATOMUICLI_CTRL003`。
- `demo DataGrid --product datagrid` 支持商业控件产品过滤。

## 相关文档

- [详细设计](design.md)
- [命令设计标准](../command-design-standard.md)
- [知识查询命令共享设计](../knowledge-query-design.md)
- [错误码标准](../error-code-standard.md)
- [metadata 模块设计](../../modules/metadata/design.md)
