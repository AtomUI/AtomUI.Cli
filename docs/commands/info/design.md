# dotnet atomui info 详细设计

## 目标

`info` 输出单个控件的结构化知识，包括包、命名空间、注册入口、API、Token、semantic parts 和 demo 摘要。它是 Agent 写控件代码前的主查询命令。

## Options

```csharp
public sealed record InfoCommandOptions(
    GlobalCliOptions Global,
    string Control,
    IReadOnlyList<InfoSection> Include,
    bool Strict) : IAtomUICliCommandOptions;

public enum InfoSection
{
    Summary,
    Api,
    Properties,
    Events,
    Methods,
    Tokens,
    Semantic,
    Demos,
    Registration
}
```

## Payload

```csharp
public sealed record InfoCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    ControlSummaryDto Summary,
    ControlApiDto? Api,
    RegistrationDto? Registration,
    IReadOnlyList<TokenDto>? Tokens,
    IReadOnlyList<SemanticPartDto>? SemanticParts,
    IReadOnlyList<DemoSummaryDto>? Demos);
```

## Handler

依赖：

- `ITargetVersionResolver`
- `IControlQueryService`
- `IPackageQueryService`
- `ITokenQueryService`
- `ISemanticPartQueryService`
- `IDemoQueryService`
- `IOutputWriter`

执行步骤：

1. 校验 `Control` 非空。
2. 将 `--include` 文本转换为枚举集合。
3. 解析目标版本和产品过滤。
4. 查询控件，支持大小写不敏感匹配。
5. `--strict` 为 true 时禁止 fuzzy match。
6. 按 include 集合装载附加区块。
7. 输出 payload。

## 输出规则

- `text`：优先显示包、命名空间、注册方法和常用 API。
- `json`：输出完整 payload，未请求区块为 `null` 或省略，二者由 JSON policy 固定。
- `markdown`：渲染为控件小文档，标题顺序稳定。

## 错误

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG001` | 缺少控件名。 |
| `ATOMUICLI_ARG002` | include 项非法。 |
| `ATOMUICLI_CTRL001` | 控件不存在。 |
| `ATOMUICLI_CTRL002` | 控件名歧义。 |

## 测试

- `Button`、`button`、`BUTTON` 返回同一控件。
- `--strict` 拒绝模糊候选。
- `--include tokens,semantic` 只加载对应服务。
- 控件不存在时返回候选建议。
- JSON 输出不包含商业不可见字段。

