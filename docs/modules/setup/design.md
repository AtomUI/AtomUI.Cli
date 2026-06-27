# AtomUICliSetupModule 详细设计

## 目标

`AtomUICliSetupModule` 负责所有写入类工作流的计划生成和受控写入，包括 Agent/编辑器配置、项目初始化、包添加和版本升级。模块的默认行为必须是 dry-run，只有命令显式传入 `--write` 才能修改文件。

## 项目与命名空间

| 项目 | 命名空间 | 说明 |
| --- | --- | --- |
| `src/AtomUI.Cli.Hosting` | `AtomUI.Cli.Hosting.Setup` | setup/init/add/upgrade handler 和写入协调。 |
| `src/AtomUI.Cli.Hosting` | `AtomUI.Cli.Hosting.Setup.Plans` | 写入计划模型。 |
| `src/AtomUI.Cli.Hosting` | `AtomUI.Cli.Hosting.Setup.Writers` | JSON/XML/文本写入器。 |

## 写入计划模型

```csharp
public sealed record SetupPlan(
    bool Write,
    IReadOnlyList<SetupAction> Actions,
    IReadOnlyList<SetupConflict> Conflicts,
    IReadOnlyList<string> VerificationCommands);
```

`SetupAction` 使用 discriminated model：

- `UpsertMcpServerAction`
- `WriteSkillTemplateAction`
- `AddPackageReferenceAction`
- `UpdatePackageVersionAction`
- `AddRegistrationCallAction`
- `AddXamlNamespaceAction`
- `WriteSampleFileAction`

每个 action 必须声明：

- 目标文件。
- 是否会创建文件。
- 是否会修改现有节点。
- dry-run preview。
- rollback hint。

## 写入流程

1. command handler 调用 planner 生成 `SetupPlan`。
2. 若存在 blocking conflict，返回 `ATOMUICLI_SETUP002`。
3. 未传入 `--write` 时输出 plan。
4. 传入 `--write` 时执行 `ISetupWriter.ApplyAsync(plan)`。
5. writer 按 action 顺序写入，并记录 `SetupWriteResult`。
6. 任一 action 失败时停止后续 action，输出失败位置和已执行 action 列表。

不承诺自动回滚。写入前的 preview 和 action 列表必须足够清晰，让用户可以用 git diff 检查。

## 结构化写入器

| 写入器 | 文件类型 | 规则 |
| --- | --- | --- |
| `JsonConfigurationWriter` | MCP/编辑器 JSON 配置 | 保留未知字段，按目标 key upsert。 |
| `MsBuildProjectWriter` | `.csproj`、`Directory.Packages.props` | 使用 XML DOM，保留可读缩进。 |
| `CSharpRegistrationWriter` | `.cs` | 只在已识别 builder 链附近插入注册调用。 |
| `XamlNamespaceWriter` | `.axaml`、`.xaml` | 只更新根节点 namespace 声明。 |
| `TextTemplateWriter` | Skill 或 sample 文件 | 只写明确目标路径。 |

无法安全定位插入点时，writer 返回 conflict，不做猜测式修改。

## 命令贡献

```csharp
context.Commands.Add<SetupCommandOptions, SetupCommandHandler>("setup");
context.Commands.Add<InitCommandOptions, InitCommandHandler>("init");
context.Commands.Add<AddCommandOptions, AddCommandHandler>("add");
context.Commands.Add<UpgradeCommandOptions, UpgradeCommandHandler>("upgrade");
```

`setup` 可在 P1 实现 dry-run 和 Codex 配置写入；`init`、`add`、`upgrade` 在 P2 实现项目写入。

## 安全边界

- 默认 dry-run。
- `--write` 是唯一写入开关。
- `--force` 不能绕过不可恢复冲突。
- 不执行外部 shell 命令。
- 不访问网络。
- 不修改未在 plan 中声明的文件。

## AOT-first 实现要求

- 模板作为嵌入资源或 metadata 快照，不动态查找磁盘模板目录。
- JSON/XML DTO 纳入 source generated context 或使用结构化 DOM。
- 不使用反射式 serializer 处理写入计划。
- 不调用外部工具探测配置格式。

## 测试设计

| 测试文件 | 覆盖点 |
| --- | --- |
| `SetupPlanBuilderTests` | target/scope 配置计划。 |
| `ProjectInitAdvisorTests` | 初始化缺失项和已满足项。 |
| `PackageAddPlannerTests` | 包解析、冲突、注册计划。 |
| `UpgradeAdvisorTests` | 版本升级计划和迁移提示。 |
| `SetupWriterTests` | dry-run、write、conflict 和部分失败。 |

写入测试必须使用临时目录 fixture，验证未声明文件未被修改。

