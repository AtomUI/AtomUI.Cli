# dotnet atomui mcp 详细设计

## 1. 命令定位

`mcp` 启动 AtomUI Cli 的 stdio MCP server，将只读知识查询和项目诊断能力暴露给 Agent。该命令长驻运行，不执行写入。

## 2. 所属模块与注册

| 字段 | 值 |
| --- | --- |
| 所属模块 | `AtomUICliMcpModule` |
| 注册阶段 | `ConfigureAtomUICliCommands` |
| 分组 | integration |
| 读写 | `isReadOnly=true` |
| 项目上下文 | `requiresProject=false` |
| 写入确认 | `requiresWriteConfirmation=false` |
| Handler 生命周期 | Transient |
| 支持格式 | text |

## 3. 调用语法

```bash
dotnet atomui mcp
dotnet atomui mcp --target-version 1.1.0
dotnet atomui mcp --data-root ./metadata --tool-prefix atomui --log-level debug
```

## 4. 参数与选项

| 参数 | 类型 | 默认值 | 校验 | 说明 |
| --- | --- | --- | --- | --- |
| `--transport` | enum | stdio | 非 `stdio` 返回 `ATOMUICLI_ARG002` | P1 只支持 stdio。 |
| `--tool-prefix` | string | atomui | 非法名称返回 `ATOMUICLI_ARG002` | MCP tool 名称前缀。 |
| `--read-only` | bool | true | false 返回 `ATOMUICLI_ARG002` | 当前固定只读。 |
| `--target-version` | global version | 默认 metadata 版本 | 版本无法解析返回 `ATOMUICLI_DATA005` | tool 查询目标版本。 |
| `--data-root` | global path | 内置数据根 | 不可读返回 `ATOMUICLI_DATA004` | 追加数据根。 |
| `--log-level` | enum | warning | 非法返回 `ATOMUICLI_ARG002` | stderr 诊断等级。 |
| `--client` | string | null | 无 | 可选客户端标识，只影响日志。 |

## 5. Options 类型

```csharp
public sealed record McpCommandOptions(
    GlobalCliOptions Global,
    McpTransport Transport,
    string ToolPrefix,
    bool ReadOnly,
    AtomUICliLogLevel LogLevel,
    string? Client) : IAtomUICliCommandOptions;
```

## 6. Handler 依赖

- `IMcpServer`
- `McpToolDescriptorCatalog`
- `IErrorWriter`

## 7. 执行流程

1. 校验 transport、tool-prefix、read-only 和 log level。
2. 确认 MCP tool catalog 已冻结。
3. 启动 `McpStdioServer`。
4. 处理 `initialize`、`tools/list`、`tools/call`、`shutdown`。
5. 每次 `tools/call` 创建 invocation scope。
6. shutdown 或 stdin EOF 后返回。

## 8. 领域服务契约

```csharp
public interface IMcpServer
{
    ValueTask<McpServerResult> RunAsync(
        McpServerOptions options,
        CancellationToken cancellationToken);
}
```

`McpServerResult` 分支：`Completed`、`Cancelled`、`TransportError`、`ProtocolError`。

## 9. 输出模型

MCP 命令本身不输出普通 payload。stdout 只承载 JSON-RPC response。stderr 只承载 server diagnostics。

MCP tool response 使用对应 tool DTO，例如 `AtomUIInfoToolResponse` 复用 `InfoCommandPayload` 字段语义。

## 10. 输出格式

`mcp` 不支持 `--format json` 作为 CLI 输出格式，因为 stdout 被 JSON-RPC 协议占用。传入非默认格式返回 `ATOMUICLI_ARG002`。

## 11. 错误与退出码

| 错误码 | 触发 | 退出码 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | transport、tool-prefix、read-only、log-level 或 format 非法 | `2` |
| `ATOMUICLI_DATA004` | data-root 不可读或授权失败 | `4` |
| `ATOMUICLI_DATA005` | target version 无法解析 | `4` |
| `ATOMUICLI_MCP001` | request 非法或 transport 运行失败 | `1` 或 JSON-RPC error |
| `ATOMUICLI_MCP002` | tool 不存在 | JSON-RPC error |
| `ATOMUICLI_MCP003` | tool invocation 未处理错误 | `1` 或 JSON-RPC error |
| `ATOMUICLI_SYS002` | 取消 | `1` |

## 12. AOT-first 约束

tool catalog 显式贡献。input schema 静态定义。JSON-RPC DTO 和 tool DTO 纳入 source generated context。

## 13. 测试矩阵

| 场景 | 输入 | 期望 |
| --- | --- | --- |
| initialize | JSON-RPC initialize | server metadata。 |
| tools/list | tools/list | 返回 frozen tools。 |
| tools/call | atomui_info | 创建 invocation scope。 |
| malformed | 非法 JSON | JSON-RPC parse error。 |
| data root | `--data-root bad` | `ATOMUICLI_DATA004`。 |
| format invalid | `mcp --format json` | `ATOMUICLI_ARG002`。 |

## 14. 实现文件建议

- `src/AtomUI.Cli.Mcp/Commands/McpCommandOptions.cs`
- `src/AtomUI.Cli.Mcp/Commands/McpCommandHandler.cs`
- `src/AtomUI.Cli.Mcp/Server/McpStdioServer.cs`
