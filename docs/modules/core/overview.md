# AtomUICliCoreModule 设计

## 定位

`AtomUICliCoreModule` 是 AtomUI Cli 的基础模块，属于 `AtomUI.Cli.Hosting`。它负责应用级基础设施、命令调度、输出、错误处理、全局选项和基础服务注册。

## 模块声明

```csharp
[Module(DisplayName = "AtomUI Cli Core")]
public sealed partial class AtomUICliCoreModule : AtomUICliModule
{
}
```

## 依赖

无 required dependency。它是其他内置模块的基础依赖。

## 注册服务

| 服务 | 生命周期 | 说明 |
| --- | --- | --- |
| `IAtomUICliApplicationLifetime` | Singleton | CLI 应用状态、取消、退出码协调。 |
| `ICliCommandParser` | Singleton | 子命令和全局选项解析。 |
| `ICliCommandDispatcher` | Singleton | 创建 command scope 并调用 handler。 |
| `IOutputWriter` | Scoped | stdout 输出，支持 text/json/markdown。 |
| `IErrorWriter` | Scoped | stderr 错误输出。 |
| `IErrorCodeCatalog` | Singleton | [错误码标准](../../commands/error-code-standard.md) 的运行时注册表。 |
| `IExitCodeMapper` | Singleton | `AtomUICliResult` 到退出码映射。 |
| `IJsonOutputSerializer` | Singleton | AOT-friendly JSON 输出。 |
| `ICliDiagnosticSink` | Scoped | 命令级诊断收集。 |

## 贡献命令

| 命令 | 文档 | 阶段 |
| --- | --- | --- |
| `--version` | 内置入口 | P0 |
| `help` / implicit help | 内置入口 | P0 |

核心模块不拥有业务查询命令，只提供命令基础设施。

## 生命周期钩子

- `ConfigureServices`：注册应用基础服务。
- `ConfigureAtomUICliCommands`：注册 help/version 这类基础入口。
- `Initialize`：校验 frozen command catalog、输出 writer 和错误 writer 可用。
- `Shutdown`：释放诊断 sink、flush 输出缓冲。

## AOT 约束

- 命令 parser 不得通过 reflection 扫描 handler。
- JSON 输出必须使用 source generated context。
- `--version` 信息不得依赖运行时 attribute 扫描；版本从 generated build info 或常量服务读取。

## 测试

- `AtomUICliApplication` 状态机顺序。
- 命令 scope 创建和释放。
- stdout/stderr 分离。
- text/json/markdown 输出稳定性。
- 错误码 catalog 与退出码映射。
