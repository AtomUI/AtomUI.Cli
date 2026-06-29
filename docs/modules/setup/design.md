# AtomUICliSetupModule 详细设计

## 1. 模块定位

`AtomUICliSetupModule` 负责 setup、init、add、upgrade 等写入相关工作流的计划生成和受控执行。模块默认 dry-run，只有命令显式传入 `--write` 才能修改文件。

## 2. 模块注册

| 字段 | 值 |
| --- | --- |
| 模块类型 | `AtomUICliSetupModule` |
| 依赖 | `AtomUICliMetadataModule`、`AtomUICliProjectAnalysisModule` |
| 命令贡献 | `setup`、`init`、`add`、`upgrade` |
| 写入默认值 | dry-run |
| 写入开关 | `--write` |

## 3. Write Plan 模型

```csharp
public sealed record WritePlan(
    string SchemaVersion,
    string Command,
    bool WriteRequested,
    IReadOnlyList<WriteOperation> Operations,
    IReadOnlyList<WriteConflict> Conflicts,
    IReadOnlyList<string> VerificationCommands);

public abstract record WriteOperation(
    string Id,
    string Kind,
    string TargetPath,
    bool CreatesFile,
    bool ModifiesFile,
    string Preview,
    string RollbackHint);

public sealed record WriteConflict(
    string Code,
    string TargetPath,
    string Message,
    string SuggestedResolution,
    bool Blocking);
```

operation 类型：

- `JsonConfigOperation`
- `MsBuildProjectOperation`
- `CSharpRegistrationOperation`
- `XamlNamespaceOperation`
- `TextFileOperation`
- `PackageReferenceOperation`

## 4. 写入流程

```text
CommandOptions
  -> CurrentStateReader
  -> DesiredStateBuilder
  -> WritePlanBuilder
  -> ConflictChecker
  -> DryRunOutput 或 WriteExecutor
  -> WriteResult
```

规则：

- blocking conflict 存在时禁止写入。
- 未传入 `--write` 时只输出 plan。
- 写入只执行 plan 中的 operations。
- 任一 operation 失败时停止后续 operation。
- 不承诺自动回滚，但每个 operation 必须提供 rollback hint。

## 5. 服务契约

```csharp
public interface IWritePlanBuilder<TOptions>
{
    ValueTask<WritePlan> BuildAsync(
        TOptions options,
        SetupCommandContext context,
        CancellationToken cancellationToken);
}

public interface IWriteExecutor
{
    ValueTask<WriteResult> ExecuteAsync(
        WritePlan plan,
        CancellationToken cancellationToken);
}

public interface IWriteConflictChecker
{
    ValueTask<IReadOnlyList<WriteConflict>> CheckAsync(
        WritePlan plan,
        CancellationToken cancellationToken);
}
```

## 6. 命令职责

| 命令 | planner 职责 |
| --- | --- |
| `setup` | 生成 MCP/Agent 客户端配置计划。 |
| `init` | 检查项目初始化状态，生成缺失配置计划。 |
| `add` | 根据 product/package catalog 生成包添加和注册建议。 |
| `upgrade` | 生成 CLI、AtomUI 包、metadata snapshot 和迁移建议。 |

## 7. 写入器

| 写入器 | 文件类型 | 规则 |
| --- | --- | --- |
| `JsonConfigurationWriter` | MCP/编辑器 JSON 配置 | 保留未知字段，按 key upsert。 |
| `MsBuildProjectWriter` | `.csproj`、props | 使用 XML DOM，保留缩进。 |
| `CSharpRegistrationWriter` | `.cs` | 只在可识别 builder 链附近插入。 |
| `XamlNamespaceWriter` | `.axaml`、`.xaml` | 只更新根节点 namespace。 |
| `TextTemplateWriter` | skill/sample 文件 | 只写明确目标路径。 |

无法安全定位插入点时返回 blocking conflict。

## 8. DI 生命周期

| 服务 | 生命周期 |
| --- | --- |
| plan builders | Transient |
| conflict checker | Transient |
| writers | Transient |
| setup command context | Scoped |
| command handlers | Transient |

## 9. 错误映射

| 场景 | 错误码 | 退出码 |
| --- | --- | --- |
| 参数非法 | `ATOMUICLI_ARG002` | `2` |
| 项目不可读取 | `ATOMUICLI_PRJ001` / `ATOMUICLI_PRJ002` | `4` |
| blocking conflict | `ATOMUICLI_SETUP002` | `6` |
| 配置读取失败 | `ATOMUICLI_SETUP003` | `6` |
| 文件写入失败 | `ATOMUICLI_SETUP004` | `6` |

## 10. AOT-first 约束

- write plan DTO 纳入 source generated JSON context。
- 模板作为嵌入资源或 metadata 数据。
- 不扫描任意模板目录。
- 不调用外部 shell 命令。
- JSON/XML 写入使用结构化 DOM 或 source generated serializer。

## 11. 测试矩阵

| 测试 | 覆盖 |
| --- | --- |
| dry-run | 不修改文件。 |
| write | 只修改 plan 声明文件。 |
| conflict | blocking conflict 返回 exit code `6`。 |
| partial failure | 停止后续 operation 并输出已应用列表。 |
| writer safety | 未声明文件不变。 |
| JSON plan | schema 稳定。 |
