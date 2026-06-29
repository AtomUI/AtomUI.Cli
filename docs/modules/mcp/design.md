# AtomUICliMcpModule 详细设计

## 1. 模块定位

`AtomUICliMcpModule` 将 AtomUI Cli 的只读知识查询和项目分析能力暴露为 MCP stdio server。它复用 Metadata 和 Project Analysis 的领域服务，不调用命令行 parser，不暴露默认写入 tool。

## 2. 模块注册

| 字段 | 值 |
| --- | --- |
| 模块类型 | `AtomUICliMcpModule` |
| 依赖 | `AtomUICliMetadataModule`、`AtomUICliProjectAnalysisModule` |
| 命令贡献 | `mcp` |
| MCP tool 贡献 | `atomui_list`、`atomui_info`、`atomui_doc`、`atomui_demo`、`atomui_token`、`atomui_semantic`、`atomui_package`、`atomui_changelog`、`atomui_doctor` |
| 写入能力 | 默认无 |

## 3. Server 生命周期

```text
dotnet atomui mcp
  -> McpCommandHandler
  -> McpStdioServer.RunAsync
  -> initialize
  -> tools/list
  -> tools/call
  -> shutdown
```

MCP server 复用 GenericHost root provider。每次 `tools/call` 创建独立 invocation scope，不允许 singleton 保存 request 状态。

## 4. Tool Descriptor

```csharp
public sealed record McpToolDescriptor(
    string Name,
    string Description,
    JsonSchemaDescriptor InputSchema,
    Type RequestType,
    Type ResponseType,
    Type HandlerType,
    bool IsReadOnly,
    string RelatedCommand);
```

descriptor catalog 必须在 Host build 前冻结。`tools/list` 只读取 catalog，不解析 handler。

## 5. Tool Handler 契约

```csharp
public interface IAtomUIMcpToolHandler<in TRequest, TResponse>
{
    ValueTask<TResponse> InvokeAsync(
        TRequest request,
        McpToolInvocationContext context,
        CancellationToken cancellationToken);
}
```

handler 职责：

- 校验 request DTO。
- 调用 Metadata 或 Project Analysis 领域服务。
- 返回 response DTO。
- 不写 stdout/stderr。
- 不创建 scope。

## 6. 默认 tools

| Tool | 领域服务 | 说明 |
| --- | --- | --- |
| `atomui_list` | `IControlQueryService`、`IPackageQueryService` | 列出控件、产品、包。 |
| `atomui_info` | `IControlQueryService` | 查询控件详情。 |
| `atomui_doc` | `IDocumentationQueryService` | 查询文档。 |
| `atomui_demo` | `IDemoQueryService` | 查询示例。 |
| `atomui_token` | `ITokenQueryService` | 查询 Token。 |
| `atomui_semantic` | `ISemanticPartQueryService` | 查询 semantic parts。 |
| `atomui_package` | `IPackageQueryService` | 查询包和冲突。 |
| `atomui_changelog` | `IChangelogQueryService` | 查询变更记录。 |
| `atomui_doctor` | `IProjectDiagnosticEngine` | 只读项目诊断。 |

## 7. JSON-RPC 错误映射

| 场景 | JSON-RPC code | CLI 错误域 |
| --- | --- | --- |
| JSON parse error | `-32700` | `ATOMUICLI_MCP` |
| method 不存在 | `-32601` | `ATOMUICLI_MCP` |
| 参数非法 | `-32602` | `ATOMUICLI_ARG` |
| tool 不存在 | `-32601` | `ATOMUICLI_MCP` |
| 数据不可用 | `-32004` | `ATOMUICLI_DATA` |
| 项目诊断失败 | `-32005` | `ATOMUICLI_PRJ` / `ATOMUICLI_AOT` |
| 未分类错误 | `-32603` | `ATOMUICLI_SYS` |

stdout 只输出 JSON-RPC response；stderr 只输出 server diagnostics。

## 8. DI 生命周期

| 服务 | 生命周期 |
| --- | --- |
| `McpToolDescriptorCatalog` | Singleton |
| `McpStdioServer` | Singleton |
| `McpRequestRouter` | Singleton |
| `McpToolInvocationContext` | Scoped |
| tool handlers | Transient |

## 9. AOT-first 约束

- tool catalog 显式贡献或 source generated。
- input schema 静态定义，不反射 DTO property。
- request/response DTO 纳入 source generated JSON context。
- 不动态暴露命令 handler 为 MCP tool。
- 不动态加载外部 tool assembly。

## 10. 测试矩阵

| 测试 | 覆盖 |
| --- | --- |
| initialize | protocol metadata。 |
| tools/list | catalog 输出稳定。 |
| tools/call | 每次 invocation 独立 scope。 |
| malformed request | JSON-RPC error。 |
| tool error mapping | CLI error 到 JSON-RPC。 |
| cancellation | request 取消和 server 关闭。 |
