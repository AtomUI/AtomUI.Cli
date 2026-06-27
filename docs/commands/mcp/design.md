# dotnet atomui mcp 详细设计

## 目标

`mcp` 启动 stdio MCP server，把 AtomUI Cli 的只读查询和诊断能力暴露给 Agent。它是长驻命令，不走普通 stdout 文本输出。

## Options

```csharp
public sealed record McpCommandOptions(
    GlobalCliOptions Global,
    string Transport,
    string ToolPrefix,
    bool ReadOnly) : IAtomUICliCommandOptions;
```

## Handler

依赖：

- `IMcpServer`
- `IMcpRequestRouter`
- `McpToolDescriptorCatalog`
- `IErrorWriter`

执行步骤：

1. 校验 transport，首期只支持 `stdio`。
2. 校验 tool catalog 已冻结且无重复名称。
3. 构建 `McpServerOptions`，包含目标版本、语言和数据根。
4. 调用 `IMcpServer.RunAsync`。
5. EOF、shutdown 或取消时正常退出。

## Tool Catalog

默认 tools：

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

tool annotations：

- read only。
- non destructive。
- idempotent。
- no external access。

## I/O 边界

- stdout 只写 JSON-RPC response。
- stderr 写 server diagnostics。
- 禁止在 stdout 输出 banner、日志或错误文本。
- 每次 `tools/call` 创建独立 DI scope。

## 错误

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 场景 |
| --- | --- |
| `ATOMUICLI_ARG002` | transport 非法。 |
| `ATOMUICLI_MCP001` | request 非法。 |
| `ATOMUICLI_MCP002` | tool 不存在。 |
| `ATOMUICLI_MCP003` | tool invocation 失败。 |
| `ATOMUICLI_DATA001` | tool 依赖的 metadata 不可用。 |

## 测试

- `tools/list` 返回稳定 tool descriptors。
- `tools/call` 每次创建独立 scope。
- request 参数错误映射为 JSON-RPC error。
- EOF 触发正常停止。
- 写入类命令不出现在 tool catalog。
