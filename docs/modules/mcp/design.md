# AtomUICliMcpModule 详细设计

## 目标

`AtomUICliMcpModule` 把 AtomUI Cli 的只读查询和诊断能力暴露为 MCP stdio server。它复用命令背后的 query service，但不直接调用命令行 parser，也不暴露写入类命令。

## 项目与命名空间

| 项目 | 命名空间 | 说明 |
| --- | --- | --- |
| `src/AtomUI.Cli.Mcp` | `AtomUI.Cli.Mcp` | MCP 模块和 server。 |
| `src/AtomUI.Cli.Mcp` | `AtomUI.Cli.Mcp.Transport` | stdio transport。 |
| `src/AtomUI.Cli.Mcp` | `AtomUI.Cli.Mcp.Tools` | tool descriptor 和 handler。 |
| `src/AtomUI.Cli.Mcp` | `AtomUI.Cli.Mcp.JsonRpc` | JSON-RPC DTO 和错误映射。 |

## Server 模型

```csharp
public interface IMcpServer
{
    ValueTask RunAsync(McpServerOptions options, CancellationToken cancellationToken);
}
```

server 组件：

| 类型 | 职责 |
| --- | --- |
| `McpStdioServer` | stdio 主循环、request 读取、response 写入。 |
| `McpRequestRouter` | `initialize`、`tools/list`、`tools/call` 路由。 |
| `McpToolDescriptorCatalog` | 冻结 tool catalog。 |
| `McpToolInvocationScopeFactory` | 每次 tool 调用创建独立 DI scope。 |
| `McpErrorMapper` | AtomUI Cli 错误到 JSON-RPC error 的映射。 |

## Tool 契约

```csharp
public interface IAtomUIMcpToolHandler<in TRequest, TResponse>
{
    ValueTask<TResponse> InvokeAsync(
        TRequest request,
        McpToolInvocationContext context,
        CancellationToken cancellationToken);
}
```

tool descriptor 包含：

- tool name。
- description。
- input schema。
- request DTO type。
- response DTO type。
- handler type。
- read-only 标志。

request/response DTO 必须和 CLI JSON 输出保持字段语义一致，但可以为了 MCP tool schema 拆分为更小对象。

## 默认 tools

| Tool | 主要服务 |
| --- | --- |
| `atomui_list` | `IControlQueryService`、`IPackageQueryService`。 |
| `atomui_info` | `IControlQueryService`。 |
| `atomui_doc` | 文档聚合服务。 |
| `atomui_demo` | `IDemoQueryService`。 |
| `atomui_token` | `ITokenQueryService`。 |
| `atomui_design_md` | `IDesignDocumentQueryService`。 |
| `atomui_semantic` | `ISemanticPartQueryService`。 |
| `atomui_package` | `IPackageQueryService`。 |
| `atomui_changelog` | `IChangelogQueryService`。 |
| `atomui_doctor` | `IProjectDiagnosticEngine`。 |

`atomui_doctor` 只能读取项目，不写入。

## JSON-RPC 处理

支持方法：

- `initialize`
- `tools/list`
- `tools/call`
- `shutdown`

错误映射：

CLI 错误码来源遵守 [AtomUI Cli 错误码标准](../../commands/error-code-standard.md)。MCP 层只负责把 `AtomUICliError` 映射到 JSON-RPC error code，不创建新的错误语义。

| CLI 错误 | JSON-RPC error code |
| --- | --- |
| 参数错误 | `-32602` |
| tool 不存在 | `-32601` |
| 数据不可用 | `-32004` |
| 项目诊断失败 | `-32005` |
| 未分类错误 | `-32603` |

stdout 只输出 JSON-RPC response，stderr 输出 server diagnostics。

## 命令贡献

```csharp
context.Commands.Add<McpCommandOptions, McpCommandHandler>("mcp");
```

MCP tools 通过 `ConfigureAtomUICliMcpTools` 显式注册：

```csharp
context.Tools.Add<AtomUIInfoToolRequest, AtomUIInfoToolResponse, AtomUIInfoToolHandler>("atomui_info");
```

## AOT-first 实现要求

- tool catalog 显式注册或 source generated。
- input schema 由静态 descriptor 构建，不反射 DTO property。
- JSON-RPC DTO、tool request 和 response 都纳入 source generated context。
- 不动态暴露命令 handler 为 MCP tool。

## 测试设计

| 测试文件 | 覆盖点 |
| --- | --- |
| `McpRequestRouterTests` | initialize、tools/list、tools/call。 |
| `McpToolCatalogTests` | tool name 去重和 read-only 约束。 |
| `McpInvocationScopeTests` | 每次调用独立 scope。 |
| `McpErrorMapperTests` | CLI error 到 JSON-RPC error 映射。 |
| `McpStdioServerTests` | EOF、取消和 malformed request。 |

测试应使用内存 stream 代替真实 stdin/stdout。
