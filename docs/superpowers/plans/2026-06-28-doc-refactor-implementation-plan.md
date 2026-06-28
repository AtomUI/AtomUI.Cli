# AtomUI Cli 文档重构开发计划

> 本文件是开发计划，不是架构设计文档。`docs/superpowers/` 只保存计划类材料。

## 目标

把 `docs/architecture/atomui-cli-architecture-design.md` 从混合型工程说明重构为真正的整体功能架构文档。架构正文只说明产品定位、能力模块、运行时协作、数据流、错误输出、AOT 约束和扩展边界。

## 已剥离内容

以下内容不再放在架构设计正文中：

- 仓库目录树和目录职责。
- Build System、MSBuild props、包版本管理和输出路径。
- `.gitignore` 策略。
- Tool 包配置、RID 包装、发布顺序和 NuGet 发布流程。
- 单元测试、快照测试、pack smoke、AOT smoke 和 MetadataBuilder verify 的详细清单。
- P0/P1/P2/P3 阶段路线。
- 以阶段为单位的结果检查清单。

这些内容后续应按主题拆到开发计划、工程规范或发布流程文档中，避免污染整体功能架构设计。

## 文档重构任务

- [x] 保留产品定位、目标和非目标。
- [x] 新增系统上下文，说明开发者、CI、Agent、CLI、模块、MCP、内置快照、外部数据根和用户项目的关系。
- [x] 用功能模块替代工程目录作为主结构。
- [x] 明确 Core Runtime、Metadata & Knowledge、Project Analysis、MCP Integration、Setup & Write Workflows、Commercial Data 的职责边界。
- [x] 明确 `AtomUICliApplication`、GenericHost 和 AtomUI.Modularity 的分工。
- [x] 明确命令架构、MCP 架构、写入工作流和商业数据边界。
- [x] 保留元数据模型、项目分析模型、数据流、错误输出和 AOT-first 约束。
- [x] 移除发布、测试、阶段路线和验收内容。

## 后续工程文档拆分建议

- `docs/engineering/build-system.md`：SDK、MSBuild props、Central Package Management、输出目录、AOT 发布属性。
- `docs/engineering/release-process.md`：NuGet Tool 包、RID-specific package、发布顺序和本地验证。
- `docs/engineering/testing-strategy.md`：单元测试、快照测试、pack smoke、AOT smoke、metadata verify。
- `docs/engineering/repository-guidelines.md`：仓库结构、`.gitignore`、生成文件和输出目录规则。
- `docs/superpowers/plans/`：只保留可执行开发计划和阶段任务清单。

## 验证清单

- [x] 架构文档不再以仓库目录、Build System 或发布流程为主体。
- [x] 架构文档清楚说明 AtomUI Cli 的功能模块和模块协作方式。
- [x] `docs/superpowers/` 下只存在计划类文档。
- [x] 文档中不出现禁用项目名或引用项目路径。
- [x] Markdown 无尾随空白。
