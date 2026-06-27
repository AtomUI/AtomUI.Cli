# Integration and Write Commands 详细设计

## 范围

本文档细化集成和写入类命令：

- `mcp`
- `setup`
- `init`
- `add`
- `upgrade`

其中 `mcp` 是长驻只读命令；`setup`、`init`、`add`、`upgrade` 默认 dry-run，只有 `--write` 才允许修改文件。

## mcp

`mcp` 启动 stdio server，暴露只读 tools：

```csharp
public sealed record McpCommandOptions(
    GlobalCliOptions Global,
    string Transport,
    string ToolPrefix,
    bool ReadOnly);
```

实现要求：

- `Transport` 首期只允许 `stdio`。
- stdout 只写 JSON-RPC response。
- stderr 写 server diagnostics。
- 每次 `tools/call` 创建独立 DI scope。
- 不暴露写入类命令。

tool descriptor 需要声明 annotations：

- read only。
- non destructive。
- idempotent。
- no external access。

## setup

`setup` 为 Agent/编辑器写入 MCP 和 Skill 配置。

```csharp
public sealed record SetupCommandOptions(
    GlobalCliOptions Global,
    SetupTarget Target,
    SetupScope Scope,
    string Workspace,
    bool Write,
    bool Force);
```

计划动作：

- upsert MCP server。
- install Skill template。
- update workspace instruction block。

写入规则：

- 配置文件使用结构化 JSON parser。
- instruction block 使用稳定 marker 包围。
- dry-run 输出完整 plan。
- `--force` 只能覆盖可恢复冲突。

## init

`init` 为项目生成初始化计划。

```csharp
public sealed record InitCommandOptions(
    GlobalCliOptions Global,
    string Path,
    IReadOnlyList<string> Products,
    bool AddSample,
    bool Write,
    bool Force);
```

计划动作：

- 添加 package reference。
- 添加注册方法调用。
- 添加 XAML namespace。
- 可选写入 sample 文件。

安全约束：

- 无法定位注册插入点时返回 conflict。
- 不猜测用户架构。
- 不执行外部 `dotnet` 命令。

## add

`add` 添加产品或包能力。

```csharp
public sealed record AddCommandOptions(
    GlobalCliOptions Global,
    string PackageOrProduct,
    string Project,
    string? Version,
    bool IncludeRegistration,
    bool Write,
    bool Force);
```

实现步骤：

1. 解析产品 ID、包 ID 或别名。
2. 检查包冲突和替代关系。
3. 生成 package reference plan。
4. 可选生成 registration plan。
5. dry-run 或写入。

## upgrade

`upgrade` 输出 CLI 和项目包升级计划。

```csharp
public sealed record UpgradeCommandOptions(
    GlobalCliOptions Global,
    string Path,
    string To,
    bool Cli,
    bool Packages,
    bool Write,
    bool Force);
```

实现规则：

- 默认不联网查询版本。
- 可升级版本来自内置 metadata 或显式数据根。
- CLI 自身升级只输出 `dotnet tool update` 建议。
- 项目包升级可以在 `--write` 下修改 project/props 文件。
- 写入后建议运行 `doctor` 和 `migrate`。

## 写入计划 DTO

```csharp
public sealed record WriteCommandPayload(
    bool Write,
    string Target,
    IReadOnlyList<SetupActionDto> Actions,
    IReadOnlyList<SetupConflictDto> Conflicts,
    IReadOnlyList<SetupWriteResultDto> Results,
    IReadOnlyList<string> VerificationCommands);
```

`setup`、`init`、`add`、`upgrade` 共享该模型。

## 错误处理

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](error-code-standard.md)。下表只列 MCP 和写入类命令可能返回的错误码子集。

| 场景 | 错误码 |
| --- | --- |
| target/scope 非法 | `ATOMUICLI_ARG002` |
| 项目不存在 | `ATOMUICLI_PRJ001` |
| 包或产品不存在 | `ATOMUICLI_PKG001` |
| 包冲突 | `ATOMUICLI_PKG003` |
| metadata 不可用 | `ATOMUICLI_DATA001` |
| 版本索引缺失 | `ATOMUICLI_DATA004` |
| 配置读取失败 | `ATOMUICLI_SETUP001` |
| 写入冲突 | `ATOMUICLI_SETUP002` |
| 写入失败 | `ATOMUICLI_SETUP003` |
| MCP request 非法 | `ATOMUICLI_MCP001` |
| MCP tool 不存在 | `ATOMUICLI_MCP002` |
| MCP tool invocation 失败 | `ATOMUICLI_MCP003` |

## AOT-first 要求

- MCP schema 静态声明。
- 写入模板嵌入工具包。
- JSON/XML 修改使用结构化 API。
- 不动态加载写入 provider。
- 所有 plan/result DTO 纳入 source generated context。

## 测试矩阵

| 命令 | 必测场景 |
| --- | --- |
| `mcp` | tools/list、tools/call、scope、错误映射。 |
| `setup` | dry-run、workspace/user scope、结构化 JSON upsert。 |
| `init` | 已满足项、冲突、sample plan。 |
| `add` | 产品解析、包冲突、注册计划。 |
| `upgrade` | CLI 建议、包版本写入、迁移提示。 |
