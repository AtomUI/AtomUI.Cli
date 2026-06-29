# 集成与写入类命令共享设计

集成与写入类命令处理 MCP server、Agent/编辑器配置、项目初始化、包添加和升级建议。除 `mcp` 外，这类命令默认 dry-run，只有显式写入选项才允许修改文件。

适用命令：

- `mcp`
- `setup`
- `init`
- `add`
- `upgrade`

## MCP 共享设计

MCP 命令启动 stdio server。Server 复用 GenericHost root provider，每次 tool invocation 创建独立 scope。

```text
dotnet atomui mcp
  -> McpStdioServer
  -> McpRequestRouter
  -> McpToolDescriptorCatalog
  -> invocation IServiceScope
  -> ToolHandler / DomainService
  -> JSON-RPC response
```

MCP tool descriptor 必须包含：

- tool name；
- description；
- input schema；
- request DTO type；
- response DTO type；
- handler type；
- read-only 标志；
- related command。

写入类 tool 默认不暴露。未来若暴露写入能力，必须引入确认协议和 write plan 审计。

## 写入计划架构

写入命令共享同一 write-plan 模型：

```text
CurrentStateReader
  -> DesiredStateBuilder
  -> WritePlanBuilder
  -> ConflictChecker
  -> DryRunOutput
  -> WriteExecutor
  -> WriteResult
```

命令 handler 只生成或执行 `WritePlan`，不直接写文件。

## 写入 DTO

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

public sealed record WriteResult(
    WritePlan Plan,
    IReadOnlyList<string> AppliedOperationIds,
    IReadOnlyList<WriteConflict> RemainingConflicts);
```

`WriteOperation` 子类型：

- `JsonConfigOperation`
- `MsBuildProjectOperation`
- `CSharpRegistrationOperation`
- `XamlNamespaceOperation`
- `TextFileOperation`
- `PackageReferenceOperation`

## 写入安全规则

- 默认 dry-run。
- `--write` 是唯一写入开关。
- blocking conflict 存在时禁止写入。
- 不静默覆盖用户内容。
- 不执行外部 shell 命令。
- 不访问网络。
- 只能修改 plan 中声明的文件。
- 写入失败时停止后续 operation，并输出已应用 operation 列表。

## 错误与退出码

| 场景 | 错误码 | 退出码 |
| --- | --- | --- |
| 写入选项非法 | `ATOMUICLI_ARG002` | `2` |
| 项目不可读取 | `ATOMUICLI_PRJ001` / `ATOMUICLI_PRJ002` | `4` |
| blocking conflict | `ATOMUICLI_SETUP002` | `6` |
| 配置文件不可读取 | `ATOMUICLI_SETUP003` | `6` |
| 文件写入失败 | `ATOMUICLI_SETUP004` | `6` |
| MCP transport 错误 | `ATOMUICLI_MCP001` | `1` |
| MCP tool 错误 | `ATOMUICLI_MCP002` | `1` 或业务错误退出码 |

## AOT-first 约束

- write plan DTO 纳入 source generated JSON context。
- JSON/XML 写入使用结构化 DOM 或显式 serializer。
- 不通过反射生成 MCP schema。
- 不动态加载 tool handler。
- 模板必须是嵌入资源或 metadata 数据，不扫描任意模板目录。

## 共享测试基线

集成与写入命令至少覆盖：

- dry-run 不修改文件。
- `--write` 修改且只修改 plan 声明文件。
- blocking conflict。
- JSON write plan。
- 不可写文件。
- cancellation。
- command catalog 注册。
- MCP command 覆盖 initialize、tools/list、tools/call、shutdown。
