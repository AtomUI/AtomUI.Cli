# AtomUICliCommercialDataModule 设计

## 定位

`AtomUICliCommercialDataModule` 属于 `AtomUI.Cli.Metadata`。它负责商业控件数据根、商业产品快照加载、授权诊断和商业数据不可用时的降级提示。

## 模块声明

```csharp
[Module(
    DisplayName = "AtomUI Cli Commercial Data",
    Dependencies = [typeof(AtomUICliMetadataModule)])]
public sealed partial class AtomUICliCommercialDataModule : AtomUICliModule
{
}
```

## 依赖

| 依赖 | 原因 |
| --- | --- |
| `AtomUICliMetadataModule` | 商业数据根扩展公开 metadata loader。 |

## 注册服务

| 服务 | 生命周期 | 说明 |
| --- | --- | --- |
| `ICommercialDataRootResolver` | Singleton | 解析 `--data-root` 和登记数据根。 |
| `ICommercialSnapshotLoader` | Singleton | 加载商业产品快照。 |
| `ICommercialVisibilityPolicy` | Singleton | 控制公开/商业字段可见性。 |
| `ICommercialDataDiagnosticService` | Singleton | 商业数据缺失、版本不匹配和授权错误提示。 |

## 贡献能力

- `ConfigureAtomUICliDataRoots` 注册外部商业数据根 provider。
- 扩展 `package`、`list`、`info`、`doctor` 的商业包诊断。

## 数据边界

- 公开包内不包含商业私有 API。
- 商业快照必须由用户显式配置数据根。
- 商业快照 schema 必须和当前 `AtomUICliDataSchemaVersion` 兼容。
- 商业数据不可用时只能输出包级安装、注册和数据根配置建议。

## AOT 约束

- 不动态加载商业程序集。
- 不反射商业控件 API。
- 外部数据根只读取显式 JSON/gzip 快照。

## 测试

- 未配置商业数据根时的降级输出。
- 商业快照 schema 不兼容错误返回 `ATOMUICLI_DATA003`。
- 商业数据授权失败返回 `ATOMUICLI_DATA005`。
- 商业包替代关系和冲突诊断。
- 公开快照不包含商业私有字段。
