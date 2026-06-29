# AtomUICliCommercialDataModule 详细设计

## 1. 模块定位

`AtomUICliCommercialDataModule` 为商业控件项目提供外部数据根、可见性控制和授权状态提示。它扩展 Metadata 模块的数据来源，不新增独立命令。

## 2. 模块注册

| 字段 | 值 |
| --- | --- |
| 模块类型 | `AtomUICliCommercialDataModule` |
| 依赖 | `AtomUICliMetadataModule` |
| 命令贡献 | 无 |
| 数据根贡献 | commercial metadata roots |
| 写入能力 | 无 |

## 3. 数据根贡献

商业模块通过 Data Root Contribution 提供数据根：

```csharp
public sealed record CommercialDataRootDescriptor(
    string ProductId,
    string RootPath,
    string SchemaVersion,
    CommercialDataVisibility Visibility,
    CommercialAuthorizationState AuthorizationState);
```

贡献规则：

- root id 必须唯一。
- product id 必须与 product catalog 匹配。
- schema version 必须被当前 CLI 支持。
- 未授权或不可读时不加载私有字段。

## 4. 可见性模型

| Visibility | 行为 |
| --- | --- |
| `Public` | 可进入公开查询结果。 |
| `CommercialSummary` | 只输出包、产品和配置提示。 |
| `CommercialFull` | 输出商业控件 API、Token、Demo、文档。 |
| `Internal` | 不进入普通 CLI 输出。 |

查询命令必须按 visibility 裁剪字段。不可见字段不能在 JSON 中输出 null 伪装为可见字段，应直接省略或返回数据不可用 warning。

## 5. 授权状态

| 状态 | 行为 |
| --- | --- |
| `Unknown` | 只输出配置建议，不输出私有 API。 |
| `Missing` | 返回 `ATOMUICLI_DATA004`。 |
| `Invalid` | 返回 `ATOMUICLI_DATA004`，带修复提示。 |
| `Valid` | 允许加载商业数据。 |

CLI 不负责联网校验授权。授权状态来自本地数据根或配置文件。

## 6. Metadata 合并

商业数据使用与公开数据相同的 snapshot 模型：

- product descriptor；
- package descriptor；
- control descriptor；
- token descriptor；
- demo descriptor；
- document descriptor；
- changelog entry。

合并规则：

- 商业数据不能覆盖公开 schema 语义。
- 商业产品 ID 与公开产品冲突时，商业数据根加载失败。
- 商业数据缺失时，知识查询命令仍可输出包级安装和注册建议。
- `--product` 指向商业产品但数据不可用时，返回结构化错误。

## 7. 错误映射

| 场景 | 错误码 | 退出码 |
| --- | --- | --- |
| 商业 root 不存在 | `ATOMUICLI_DATA004` | `4` |
| 商业 schema 不兼容 | `ATOMUICLI_DATA002` | `4` |
| 商业 product 未知 | `ATOMUICLI_DATA005` | `4` |
| 授权缺失或无效 | `ATOMUICLI_DATA004` | `4` |
| 商业字段不可见 | warning，不失败 |

## 8. DI 生命周期

| 服务 | 生命周期 |
| --- | --- |
| commercial root provider | Singleton |
| authorization state reader | Singleton |
| visibility filter | Singleton |
| data root descriptors | Singleton catalog |

## 9. AOT-first 约束

- 商业能力默认通过数据快照接入。
- 不动态加载商业控件程序集。
- 不通过 reflection 提取商业 API。
- commercial DTO 纳入 metadata JSON context。

## 10. 测试矩阵

| 测试 | 覆盖 |
| --- | --- |
| missing root | `ATOMUICLI_DATA004`。 |
| invalid schema | `ATOMUICLI_DATA002`。 |
| visibility filter | 不可见字段不输出。 |
| commercial product query | 有数据和无数据两种分支。 |
| public query unaffected | 商业数据缺失不影响公开控件查询。 |
