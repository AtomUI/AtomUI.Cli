# dotnet atomui design.md 详细设计

## 目标

`design.md` 输出 AtomUI 设计语言文档。文档按 major version 组织，用于 Agent 理解视觉原则、Token、布局、可访问性和控件选择。

## Options

```csharp
public sealed record DesignMarkdownCommandOptions(
    GlobalCliOptions Global,
    DesignDocumentSection Section) : IAtomUICliCommandOptions;

public enum DesignDocumentSection
{
    All,
    Principles,
    Layout,
    Tokens,
    Accessibility,
    Patterns,
    Controls
}
```

## Payload

```csharp
public sealed record DesignMarkdownCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    IReadOnlyList<DocumentSectionDto> Sections,
    IReadOnlyList<TokenDto> ReferencedTokens);
```

## Handler

依赖：

- `ITargetVersionResolver`
- `IDesignDocumentQueryService`
- `ITokenQueryService`
- `IProductCatalogLoader`
- `IOutputWriter`

执行步骤：

1. 解析目标 major version。
2. 校验 section。
3. 读取设计文档 section。
4. 按 `--product` 裁剪控件选择建议。
5. 附加相关 global/shared token。
6. 输出 markdown、json 或 text。

## 输出规则

- 默认格式为 `markdown`。
- markdown 保留设计文档标题层级。
- JSON 输出 section 和 token 引用，便于 Agent 局部加载。

## 错误

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | section 非法。 |
| `ATOMUICLI_PKG001` | product 不存在。 |
| `ATOMUICLI_DATA001` | 设计文档缺失。 |

## 测试

- 默认输出所有 section。
- `--section tokens` 只输出 Token 相关内容。
- 目标版本映射到正确 major 文档。
- product filter 影响控件选择 section。
- markdown heading 顺序稳定。

