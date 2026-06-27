# dotnet atomui mcp 设计

## 定位

`mcp` 是 P1 集成命令，用于启动 AtomUI Cli 的 stdio MCP server，将只读查询和诊断能力暴露给支持 MCP 的 Agent。

## 所属模块

`AtomUICliMcpModule`

## 调用形式

```bash
dotnet atomui mcp
dotnet atomui mcp --target-version 6.0.6
dotnet atomui mcp --data-root ./metadata
```

## 参数与选项

| 参数或选项 | 默认值 | 说明 |
| --- | --- | --- |
| `--transport <stdio>` | `stdio` | MCP transport。P1 只支持 `stdio`。 |
| `--tool-prefix <name>` | `atomui` | Tool 名称前缀。 |
| `--read-only` | `true` | 固定只读，当前不可关闭。 |
| `--target-version <version>` | 默认版本 | 查询目标版本。 |
| `--data-root <path>` | 内置数据根 | 附加数据根。 |

## 输入

- MCP JSON-RPC stdio request。
- 冻结的 MCP tool catalog。
- metadata 和 project analysis 服务。

## 输出

- stdout：JSON-RPC response。
- stderr：server 诊断、启动错误和不可恢复错误。

默认暴露 tools：

| Tool | 对应能力 |
| --- | --- |
| `atomui_list` | `list` |
| `atomui_info` | `info` |
| `atomui_doc` | `doc` |
| `atomui_demo` | `demo` |
| `atomui_token` | `token` |
| `atomui_design_md` | `design.md` |
| `atomui_semantic` | `semantic` |
| `atomui_package` | `package` |
| `atomui_changelog` | `changelog` |
| `atomui_doctor` | `doctor` |

## Handler 与服务依赖

| 类型 | 生命周期 | 说明 |
| --- | --- | --- |
| `McpCommandOptions` | Scoped value | transport、tool prefix 和全局选项。 |
| `McpCommandHandler` | Transient | 启动 server。 |
| `IMcpServer` | Singleton | stdio server 主循环。 |
| `IMcpRequestRouter` | Singleton | tools/list 和 tools/call 路由。 |
| `IMcpToolInvocationScopeFactory` | Singleton | 每次 tool 调用创建 scope。 |
| `IMcpErrorMapper` | Singleton | CLI 错误到 MCP error 映射。 |

## 执行流程

1. 校验 transport。
2. 确认 MCP tool catalog 已冻结且无重复名称。
3. 启动 stdio server。
4. 收到 `tools/list` 时返回 tool descriptors。
5. 收到 `tools/call` 时创建独立 DI scope。
6. 调用对应 tool handler 并返回 JSON-RPC response。
7. 收到取消或 EOF 时停止 server 并释放 pending scope。

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。下表只列本命令可能返回的错误码子集。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | transport 非法。 |
| `ATOMUICLI_MCP001` | `1` | MCP request 非法。 |
| `ATOMUICLI_MCP002` | `1` | tool 不存在。 |
| `ATOMUICLI_MCP003` | `1` | tool invocation 失败。 |
| `ATOMUICLI_DATA001` | `4` | metadata 不可用。 |

## AOT 约束

- MCP tools 显式注册。
- JSON-RPC request/response DTO 纳入 source generated context。
- 不通过 method reflection 暴露 tools。
- 不暴露写入类命令为默认 MCP tool。

## 测试点

- `tools/list` 返回稳定 tool 列表。
- 每次 `tools/call` 创建独立 scope。
- tool 参数错误映射为 MCP error。
- EOF 触发 server 正常停止。
- 写入类命令不在默认 tool catalog 中。

