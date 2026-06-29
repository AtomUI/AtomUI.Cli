# AtomUI Cli 错误码标准

本文档定义 AtomUI Cli 的统一错误码、诊断码、退出码和输出契约。所有命令、模块生命周期错误、MCP tool 错误、项目诊断 finding 都必须遵守同一套编号和映射规则。

## 适用范围

错误码同时用于两类对象：

| 对象 | 用途 | 退出码来源 |
| --- | --- | --- |
| `AtomUICliError.Code` | 命令无法继续执行、数据不可用、写入失败、模块失败等硬错误。 | `IExitCodeMapper` 按错误码映射。 |
| `AtomUICliDiagnostic.Code` | `doctor`、`lint`、`usage` 等命令输出的诊断 finding。 | 诊断 severity 聚合；存在 error 或命中 `--fail-on-warning` 时返回 `5`。 |

诊断 finding 不是命令运行时异常。诊断类命令可以在 stdout 输出完整 payload，同时因诊断结果返回非零退出码。命令本身无法执行时，才写 `AtomUICliError` 到 stderr。

## 编号格式

错误码格式固定为：

```text
ATOMUICLI_<DOMAIN><NNN>
```

规则：

- `ATOMUICLI_` 是固定前缀。
- `<DOMAIN>` 必须是已登记的大写域名，例如 `ARG`、`DATA`、`CTRL`。
- `<NNN>` 是三位十进制编号，从 `001` 开始。
- 一个错误码只能表达一个稳定语义，不能在不同命令里复用为不同含义。
- 错误码一旦发布不能改名、不能复用；废弃错误码保留在注册表中。
- 命令不能创建命令名前缀错误码，例如 `INFO001`。应按失败所属领域选择现有域。
- 参数 parser、handler、service、module lifecycle 和 MCP adapter 都必须返回注册表里的错误码。

## 域名注册

| 域名 | 范围 |
| --- | --- |
| `SYS` | 未分类异常、取消、运行时兜底错误。 |
| `ARG` | 命令不存在、参数缺失、参数值非法和选项组合非法。 |
| `MOD` | 模块解析、模块生命周期和 contribution catalog 错误。 |
| `DATA` | 元数据快照、schema、版本索引、数据根和授权错误。 |
| `CTRL` | 控件、示例、Token、semantic part 等知识查询目标错误。 |
| `PKG` | 包、产品、包图谱、包兼容和包冲突错误。 |
| `PRJ` | 项目路径、项目文件、源码扫描和项目级诊断 finding。 |
| `AOT` | Native AOT、trim、single-file 和动态访问相关诊断。 |
| `MCP` | MCP transport、request、tool 查找和 tool 调用错误。 |
| `SETUP` | setup、init、add、upgrade 等写入计划和落盘错误。 |

新增域名必须先更新本文档，再更新 `IErrorCodeCatalog`、`IExitCodeMapper` 和命令文档。

## 退出码

| 退出码 | 场景 |
| --- | --- |
| `0` | 成功，或诊断命令没有达到失败阈值。 |
| `1` | 未分类异常、模块失败、MCP 运行时失败、取消。 |
| `2` | 参数错误、命令不存在、选项组合非法。 |
| `3` | 查询目标不存在或存在歧义，例如控件、包、项目路径。 |
| `4` | 数据不可用、schema 不兼容、项目文件或源码无法读取。 |
| `5` | 项目诊断、包冲突、AOT 或 lint finding 达到失败阈值。 |
| `6` | 写入计划冲突、配置读取失败或文件写入失败。 |

命令 handler 不直接返回进程退出码，只返回 `AtomUICliResult`。退出码只由 `IExitCodeMapper` 根据错误码、结果类型和诊断 severity 计算。

## 错误码注册表

| 错误码 | 类型 | 默认退出码 | 语义 |
| --- | --- | --- | --- |
| `ATOMUICLI_SYS001` | error | `1` | 未处理异常或无法归类的运行时错误。 |
| `ATOMUICLI_SYS002` | error | `1` | 用户取消、进程取消或取消令牌触发。 |
| `ATOMUICLI_ARG001` | error | `2` | 缺少必需参数或必需选项。 |
| `ATOMUICLI_ARG002` | error | `2` | 参数值非法、范围非法、枚举值非法或选项组合冲突。 |
| `ATOMUICLI_ARG003` | error | `2` | 命令不存在、命令歧义或命令未注册。 |
| `ATOMUICLI_MOD001` | error | `1` | 模块解析、实例化、依赖排序或生命周期钩子失败。 |
| `ATOMUICLI_MOD002` | error | `1` | 模块 contribution catalog 冲突，例如重复命令、重复 tool 或重复数据根。 |
| `ATOMUICLI_DATA001` | error | `4` | 元数据快照、文档区块、示例数据、Token 数据或设计数据不可用。 |
| `ATOMUICLI_DATA002` | error | `4` | 内置 metadata schema 与 CLI 支持范围不兼容。 |
| `ATOMUICLI_DATA003` | error | `4` | metadata 内部引用缺失，例如文档 section、示例、Token 或 semantic part 的数据引用不存在。 |
| `ATOMUICLI_DATA004` | error | `4` | 外部或商业数据根缺失、不可读、授权缺失或授权诊断失败。 |
| `ATOMUICLI_DATA005` | error | `4` | 数据根、商业 product、目标版本或版本范围无法解析到可用快照。 |
| `ATOMUICLI_CTRL001` | error | `3` | 控件不存在。 |
| `ATOMUICLI_CTRL002` | error | `3` | 控件名存在歧义，需要用户选择唯一目标。 |
| `ATOMUICLI_CTRL003` | error | `3` | 示例不存在。 |
| `ATOMUICLI_CTRL004` | error | `3` | Token 不存在。 |
| `ATOMUICLI_CTRL005` | error | `3` | semantic part 不存在。 |
| `ATOMUICLI_PKG001` | error | `3` | 包或产品不存在。 |
| `ATOMUICLI_PKG002` | error | `3` | 包 ID、产品 ID 或别名存在歧义。 |
| `ATOMUICLI_PKG003` | error/diagnostic | `5` | 包冲突、包版本不兼容或包组合阻止继续执行。 |
| `ATOMUICLI_PRJ001` | error | `4` | 项目、解决方案或输入路径不存在。 |
| `ATOMUICLI_PRJ002` | error | `4` | 项目文件无法读取、XML 非法或项目结构无法解析。 |
| `ATOMUICLI_PRJ003` | error | `4` | 源码文件无法读取或扫描失败。 |
| `ATOMUICLI_PRJ010` | diagnostic | severity 聚合 | XAML namespace、控件使用或项目结构 lint finding。 |
| `ATOMUICLI_AOT001` | diagnostic | severity 聚合 | Native AOT、trim、single-file 或动态访问风险 finding。 |
| `ATOMUICLI_MCP001` | error | `1` | MCP request 非法或无法解析。 |
| `ATOMUICLI_MCP002` | error | `1` | MCP tool 不存在或未注册。 |
| `ATOMUICLI_MCP003` | error | `1` | MCP tool invocation 失败。 |
| `ATOMUICLI_SETUP001` | error | `6` | 目标配置探测失败。 |
| `ATOMUICLI_SETUP002` | error | `6` | 写入计划存在 blocking conflict，且未传入 `--force`。 |
| `ATOMUICLI_SETUP003` | error | `6` | 配置文件读取或解析失败。 |
| `ATOMUICLI_SETUP004` | error | `6` | 文件写入、更新、回滚或写入后校验失败。 |

## 选择规则

当一个失败场景可能匹配多个错误码时，按下列顺序选择：

1. 参数解析失败优先使用 `ARG`。
2. 命令能运行但查询目标不存在，使用 `CTRL`、`PKG` 或 `PRJ001`。
3. 数据文件、schema、版本索引或授权导致无法查询，使用 `DATA`。
4. 项目文件和源码读取失败，使用 `PRJ002` 或 `PRJ003`。
5. 包冲突、AOT 风险、lint finding 属于诊断结果，使用 `PKG003`、`AOT001` 或 `PRJ010`，并由 severity 聚合退出码。
6. 写入前冲突或写入失败，使用 `SETUP`。
7. 模块初始化和 contribution 冲突，使用 `MOD`。
8. 仍无法分类时，使用 `SYS001`，同时在 `details.category` 记录原始异常类别。

同一命令只返回第一个阻止继续执行的 `AtomUICliError`。需要展示多个问题时，应返回诊断 payload，而不是把多个硬错误塞进 stderr。

## 输出契约

成功结果：

- stdout 只输出命令结果。
- stderr 只输出 warning、update check 提示或诊断摘要。

硬错误结果：

- stdout 保持为空。
- stderr 输出错误摘要。
- `--format json` 时，stderr 输出稳定 JSON envelope。

JSON 错误 envelope：

```json
{
  "schemaVersion": "1.0",
  "success": false,
  "command": "info",
  "error": {
    "code": "ATOMUICLI_CTRL001",
    "severity": "error",
    "message": "Control 'Foo' was not found.",
    "suggestion": "Run `dotnet atomui list controls` to view available controls.",
    "stage": "execute",
    "location": null,
    "details": {
      "control": "Foo"
    }
  }
}
```

`message` 面向人类阅读，可以本地化；`code`、`stage`、`details` key 必须稳定。`suggestion` 应给出可执行下一步，不能依赖网络查询。

## 命令文档要求

每个命令的 `overview.md` 和 `design.md` 必须包含错误码章节，并遵守下列写法：

- 章节开头链接本文档。
- 表格只列本命令可能产生的错误码子集。
- 表格中的退出码必须来自本文档，不能在命令文档中重新定义。
- 诊断类命令应明确哪些 code 是 finding code，退出码由 severity 聚合。
- 新增错误场景时，先更新本文档注册表，再更新命令文档和测试矩阵。

共享设计文档也必须引用本文档，避免在知识查询、项目分析、写入集成三类命令中分叉出不同规则。

## 实现要求

- `AtomUI.Cli.Abstractions` 定义错误码常量，常量名与注册表一一对应。
- `IErrorCodeCatalog` 提供错误码到领域、类型和默认退出码的只读映射。
- `IExitCodeMapper` 是唯一退出码计算入口。
- `IErrorWriter` 负责 stderr 文本和 JSON envelope，不能把错误写入 stdout。
- JSON source generation context 必须包含 `AtomUICliError`、`AtomUICliDiagnostic`、错误 envelope 和所有命令 payload。
- AOT 构建中不得通过 reflection 扫描错误码常量；错误码 catalog 由显式代码或 source generator 生成。

## 测试要求

| 测试 | 覆盖点 |
| --- | --- |
| `ErrorCodeCatalogTests` | 注册表无重复 code、无未知 domain、所有 code 符合格式。 |
| `ExitCodeMapperTests` | 每个注册 code 的退出码映射和诊断 severity 聚合。 |
| `ErrorWriterTests` | text/json 错误输出只写 stderr，stdout 为空。 |
| `CommandErrorContractTests` | 每个命令声明的错误码都存在于 catalog。 |
| `McpErrorMapperTests` | CLI 错误码到 JSON-RPC error code 的映射稳定。 |
| `AotJsonContextTests` | 错误 DTO、诊断 DTO 和 envelope 均纳入 source generated JSON context。 |
