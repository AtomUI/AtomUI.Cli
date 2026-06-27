# AtomUICliCoreModule 详细设计

## 目标

`AtomUICliCoreModule` 提供所有命令共享的运行时基础设施。它不包含 AtomUI 业务查询逻辑，也不读取用户项目。实现时应把“进程入口、命令解析、scope 创建、输出、错误映射、取消传播”集中在 `AtomUI.Cli.Hosting`，避免业务模块重复处理这些横切逻辑。

## 项目与命名空间

| 项目 | 命名空间 | 说明 |
| --- | --- | --- |
| `src/AtomUI.Cli.Hosting` | `AtomUI.Cli.Hosting` | Application、Host builder、命令调度。 |
| `src/AtomUI.Cli.Hosting` | `AtomUI.Cli.Hosting.Commands` | 命令 descriptor、parser、dispatcher。 |
| `src/AtomUI.Cli.Hosting` | `AtomUI.Cli.Hosting.Output` | stdout/stderr writer 和格式化。 |
| `src/AtomUI.Cli.Abstractions` | `AtomUI.Cli` | 结果、错误、退出码和基础 DTO。 |

## 核心类型

| 类型 | 类型种类 | 职责 |
| --- | --- | --- |
| `AtomUICliApplication` | class | CLI 应用编排器，持有 `ModuleHost`、`IHost` 和冻结 catalogs。 |
| `AtomUICliApplicationBuilder` | class | 组装 args、module registrations、Host 配置和全局选项。 |
| `CliInvocationContext` | sealed class | 当前命令的只读上下文，包含原始 args、全局选项、取消令牌和 trace id。 |
| `CliCommandDescriptor` | readonly record struct | 命令名称、options 类型、handler 类型、输出能力和只读/写入属性。 |
| `CliCommandDescriptorCatalog` | sealed class | 冻结命令 catalog，提供命令查找和帮助信息。 |
| `IAtomUICliCommandHandler<TOptions>` | interface | 命令 handler 契约。 |
| `AtomUICliResult` | readonly record struct | 成功、业务失败、诊断失败和取消结果。 |
| `AtomUICliError` | readonly record struct | 错误码、消息、建议、文件位置和 inner category。 |

## 命令调度契约

```csharp
public interface IAtomUICliCommandHandler<in TOptions>
{
    ValueTask<AtomUICliResult> ExecuteAsync(
        TOptions options,
        CliInvocationContext context,
        CancellationToken cancellationToken);
}
```

调度流程由 `ICliCommandDispatcher` 统一控制：

1. `ICliCommandParser.Parse(args, catalog)` 解析全局选项和子命令 options。
2. dispatcher 根据 descriptor 创建命令级 `IServiceScope`。
3. scope 内注册 `CliInvocationContext`、`GlobalCliOptions`、`IOutputWriter`、`IErrorWriter`。
4. 从 scope 解析强类型 handler。
5. 调用 `ExecuteAsync`。
6. `IExitCodeMapper` 把 `AtomUICliResult` 转换为进程退出码。

业务 handler 不允许创建 scope，不允许接收根 `IServiceProvider`。

## 输出模型

| 类型 | 说明 |
| --- | --- |
| `IOutputWriter` | 只写 stdout，负责 text/json/markdown 输出。 |
| `IErrorWriter` | 只写 stderr，负责错误、参数提示和诊断摘要。 |
| `IJsonOutputSerializer` | 统一使用 source generated `JsonSerializerContext`。 |
| `IMarkdownWriter` | 提供 heading、table、code block 等稳定格式。 |

输出规则：

- 成功结果只写 stdout。
- 失败结果优先写 stderr；当 `--format json` 时，stderr 输出结构化错误摘要，stdout 保持为空。
- `--format markdown` 只能用于命令声明支持 markdown 的 descriptor。
- writer 不负责业务字段裁剪，字段选择由 handler 或 query service 完成。

## 错误与退出码

错误码前缀和退出码映射集中在 `IExitCodeMapper`：

| 错误前缀 | 默认退出码 |
| --- | --- |
| `ATOMUICLI_ARG` | `2` |
| `ATOMUICLI_CTRL` | `3` |
| `ATOMUICLI_PKG` | `3` 或 `5` |
| `ATOMUICLI_DATA` | `4` |
| `ATOMUICLI_PRJ` | `3` 或 `4` |
| `ATOMUICLI_AOT` | `5` |
| `ATOMUICLI_MCP` | `1` |
| `ATOMUICLI_SETUP` | `6` |
| `ATOMUICLI_MOD` | `1` |

模块初始化阶段抛出的异常必须先转为 `AtomUICliError`，再进入统一退出码映射。

## 生命周期设计

`AtomUICliApplication.RunAsync` 是唯一能驱动模块生命周期和 Host 生命周期的入口：

```text
Build module host
Resolve and create modules
Configure module services
Freeze module services
Configure CLI contribution catalogs
Build GenericHost
Initialize modules
Start GenericHost
Dispatch command or run MCP server
Stop GenericHost
Shutdown modules
Dispose
```

失败处理：

- Host build 前失败：不调用 `IHost.StartAsync`，但已创建的 module instances 需要 shutdown。
- 命令执行失败：仍执行 `IHost.StopAsync` 和 `ModuleHost.ShutdownAsync`。
- Ctrl+C：通过 `CancellationToken` 进入 handler，handler 返回取消结果。

## AOT-first 实现要求

- 命令 descriptor 由模块 contribution 显式注册，不通过反射扫描。
- options binding 使用静态 parser 或 source generated binder。
- JSON context 必须包含核心错误、帮助、版本和所有基础结果 DTO。
- `ActivatorUtilities` 只允许在 Hosting 层解析已注册 handler，不作为业务对象工厂。
- 默认 Host provider 保持最小集合，不启用会触发 Native AOT 警告的 provider。

## 测试设计

| 测试文件 | 覆盖点 |
| --- | --- |
| `AtomUICliApplicationTests` | 状态机顺序、失败路径、shutdown 保证。 |
| `CliCommandDispatcherTests` | scope 创建、handler 解析、取消传播。 |
| `CliCommandParserTests` | 全局选项、未知命令、help/version。 |
| `OutputWriterTests` | stdout/stderr 分离和格式稳定性。 |
| `ExitCodeMapperTests` | 错误码到退出码映射。 |

测试夹具应使用 fake module 和 fake handler，不依赖 metadata 快照。

