# AtomUICliCommercialDataModule 详细设计

## 目标

`AtomUICliCommercialDataModule` 负责接入商业控件数据根，并在商业数据不可用时提供明确降级提示。它不动态加载商业程序集，不把商业私有 API 编译进公开包，也不改变公开 metadata schema 的语义。

## 项目与命名空间

| 项目 | 命名空间 | 说明 |
| --- | --- | --- |
| `src/AtomUI.Cli.Metadata` | `AtomUI.Cli.Metadata.Commercial` | 商业数据根、可见性和诊断服务。 |
| `src/AtomUI.Cli.Abstractions` | `AtomUI.Cli.Metadata.Contracts` | 共享产品、包和可见性 DTO。 |

## 数据根来源

商业数据根只来自显式配置：

- 命令行 `--data-root <path>`。
- 未来可选的本机配置文件登记路径。
- 测试中的显式 fixture path。

公开 CLI 包不内置商业私有 API 快照。若某个商业产品只有公开包级信息，命令只能输出包、注册、授权和数据根配置建议，不能输出不存在的控件 API。

## 可见性策略

```csharp
public interface ICommercialVisibilityPolicy
{
    CommercialVisibilityResult Evaluate(
        ProductDescriptor product,
        MetadataRootSet roots);
}
```

可见性结果：

- `Public`：公开数据完整。
- `CommercialAvailable`：商业数据根存在且 schema 兼容。
- `CommercialUnavailable`：商业产品已知，但无数据根。
- `CommercialIncompatible`：数据根 schema 或版本不兼容。
- `CommercialUnauthorized`：数据根存在但授权诊断失败。

命令输出必须根据可见性裁剪字段。

## 快照加载

`ICommercialSnapshotLoader` 复用 metadata loader 的 gzip/plain JSON 机制，但增加额外校验：

- `schemaVersion` 必须等于当前 `AtomUICliDataSchemaVersion`。
- `visibility` 必须为 `commercial` 或 `internal-commercial`。
- 快照 product id 必须和产品清单一致。
- 不允许覆盖公开快照中的 public product id。

## 诊断服务

`ICommercialDataDiagnosticService` 提供统一提示：

| 场景 | 输出 |
| --- | --- |
| 未配置数据根 | 提示 `--data-root` 和可公开包级信息。 |
| 数据根不存在 | 输出路径和 `ATOMUICLI_DATA001`。 |
| schema 不兼容 | 输出当前 schema 和期望 schema。 |
| 版本不匹配 | 输出可用版本和目标版本。 |
| 授权失败 | 输出授权状态，不泄露私有内容。 |

## 与公开命令的关系

商业模块不新增独立命令。它通过数据根和 visibility policy 影响下列命令：

- `list`
- `info`
- `package`
- `doctor`
- `add`
- `upgrade`

命令 handler 仍依赖公开 query service，由 query service 组合商业可见性结果。

## AOT-first 实现要求

- 不动态加载商业程序集。
- 不反射商业控件 API。
- 商业 DTO 纳入 source generated JSON context。
- 数据根 provider 显式注册，不扫描目录发现 provider 插件。

## 测试设计

| 测试文件 | 覆盖点 |
| --- | --- |
| `CommercialDataRootResolverTests` | `--data-root` 解析和优先级。 |
| `CommercialSnapshotLoaderTests` | schema、visibility、版本校验。 |
| `CommercialVisibilityPolicyTests` | 可见性裁剪。 |
| `CommercialDataDiagnosticServiceTests` | 缺失、失配和授权提示。 |
| `CommercialCommandIntegrationTests` | list/info/package 的降级输出。 |

fixture 必须使用虚构商业产品和虚构控件名，不包含真实私有 API。

