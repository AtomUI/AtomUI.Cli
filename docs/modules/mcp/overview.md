# AtomUICliMcpModule 设计

## 定位

`AtomUICliMcpModule` 属于 `AtomUI.Cli.Mcp`。它负责将 AtomUI Cli 的只读查询和诊断能力暴露为 MCP stdio server。

## 模块声明

```csharp
[Module(
    DisplayName = "AtomUI Cli MCP",
    Dependencies =
    [
        typeof(AtomUICliMetadataModule),
        typeof(AtomUICliProjectAnalysisModule)
    ])]
public sealed partial class AtomUICliMcpModule : AtomUICliModule
{
}
```

## 依赖

| 依赖 | 原因 |
| --- | --- |
| `AtomUICliMetadataModule` | 提供查询类 MCP tools。 |
| `AtomUICliProjectAnalysisModule` | 提供 `doctor` 等项目诊断 tools。 |

## 注册服务

| 服务 | 生命周期 | 说明 |
| --- | --- | --- |
| `IMcpServer` | Singleton | stdio MCP server。 |
| `IMcpRequestRouter` | Singleton | tool invocation 路由。 |
| `IMcpToolInvocationScopeFactory` | Singleton | 每次 tool 调用创建独立 scope。 |
| `IMcpResponseWriter` | Scoped | JSON-RPC response 输出。 |
| `IMcpErrorMapper` | Singleton | CLI 错误到 MCP error 映射。 |

## 贡献命令

| 命令 | 文档 | 阶段 |
| --- | --- | --- |
| `mcp` | [commands/mcp](../../commands/mcp/overview.md) | P1 |

## 贡献 MCP tools

- `atomui_list`
- `atomui_info`
- `atomui_doc`
- `atomui_demo`
- `atomui_token`
- `atomui_design_md`
- `atomui_semantic`
- `atomui_package`
- `atomui_changelog`
- `atomui_doctor`

## 生命周期钩子

- `ConfigureServices`：注册 MCP server 和 tool router。
- `ConfigureAtomUICliMcpTools`：注册只读 MCP tools。
- `Initialize`：校验 tool catalog 不为空且无重复 tool name。
- `Shutdown`：停止 stdio loop，释放 pending invocation scope。

## 安全边界

- MCP tools 默认只读。
- 写入类命令不暴露为默认 MCP tool。
- tool invocation 只能访问命令 catalog 授权的 handler。
- 每次 tool invocation 必须创建独立 DI scope。

## AOT 约束

- MCP tool catalog 必须显式注册或 source generated。
- 不通过 method reflection 发现 tools。
- JSON-RPC request/response 使用 source generated JSON context。

## 测试

- MCP server 初始化。
- tool name 去重。
- 每次 invocation 创建独立 scope。
- tool error 映射。
- `atomui_doctor` 不写入用户项目。
