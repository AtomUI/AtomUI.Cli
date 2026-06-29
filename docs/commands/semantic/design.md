# dotnet atomui semantic 详细设计

## 1. 命令定位

`semantic` 查询控件 semantic parts、template parts、pseudo classes 和可定制节点，服务于样式定制和 Agent 生成控件模板相关代码。

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

## 3. 调用语法

```bash
dotnet atomui semantic Button
dotnet atomui semantic Button --part root
dotnet atomui semantic Button --include-template --detail --format json
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `control` | string | 必填 | 缺失返回 `ATOMUICLI_ARG001` | 控件名。 |
| `--part` | string | null | 不存在返回 `ATOMUICLI_CTRL005` | 指定 part。 |
| `--include-template` | bool | false | 无 | 输出模板结构摘要，不输出完整模板源码。 |
| `--strict` | bool | false | 无 | 控件名和 part 名要求精确匹配。 |
| `--detail` | global bool | false | 无 | 输出样式状态、伪类和可用 Token。 |

## 5. Options 类型

```csharp
public sealed record SemanticCommandOptions(
    GlobalCliOptions Global,
    string Control,
    string? Part,
    bool IncludeTemplate,
    bool Strict,
    bool Detail) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IMetadataCommandContextFactory`
- `IControlQueryService`
- `ISemanticPartQueryService`
- `IOutputWriter`

## 7. 执行流程

1. 校验控件名、part 和 detail 选项。
2. 查询控件。
3. 查询 semantic parts。
4. 应用 part 过滤。
5. 构建 payload。
6. 输出 text/json/markdown。

## 8. 领域服务契约

```csharp
public interface ISemanticPartQueryService
{
    ValueTask<SemanticPartQueryResult> QueryAsync(
        SemanticPartQuery query,
        CancellationToken cancellationToken);
}
```

分支：`Found`、`PartNotFound`、`ControlNotFound`、`DataUnavailable`。

## 9. 输出模型

```csharp
public sealed record SemanticCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    ControlSummaryDto Control,
    IReadOnlyList<SemanticPartDto> SemanticParts,
    IReadOnlyList<TemplatePartDto> TemplateParts,
    IReadOnlyList<PseudoClassDto> PseudoClasses,
    IReadOnlyList<WarningDto> Warnings);
```

## 10. 输出格式

- text：显示 part 名称、用途、可定制入口。
- json：输出完整字段。
- markdown：输出 part 表格和模板摘要。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG001` | 缺少控件名 | `2` |
| `ATOMUICLI_CTRL001` | 控件不存在 | `3` |
| `ATOMUICLI_CTRL005` | semantic part 不存在 | `3` |
| `ATOMUICLI_DATA001` | semantic 数据不可用 | `4` |

## 12. AOT-first 约束

semantic 数据来自 snapshot，不读取 XAML 模板文件。DTO 纳入 JSON context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| all | `semantic Button` | 输出所有区块。 |
| part | `--part root` | 单 part。 |
| template | `--include-template` | 输出模板摘要。 |
| missing part | `--part none` | `ATOMUICLI_CTRL005`。 |
| json | `--format json` | payload 稳定。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Metadata/Commands/SemanticCommandOptions.cs`
- `src/AtomUI.Cli.Metadata/Commands/SemanticCommandHandler.cs`
- `src/AtomUI.Cli.Metadata/Queries/SemanticPartQueryService.cs`
