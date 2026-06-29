# dotnet atomui help 设计

## 定位

`help` 是 AtomUI Cli 的入口引导命令，用于让首次使用者快速理解工具能力、常用命令、命令分组、全局选项和下一步命令详情。它不能只是输出命令名列表。

`help` 属于 Core 能力，只读取轻量 command manifest。默认 `help` 不激活 metadata、project analysis、setup、MCP 或商业数据模块。

## 所属模块

`AtomUICliCoreModule`

## 调用形式

```bash
dotnet atomui help
dotnet atomui --help
dotnet atomui help info
dotnet atomui info --help
dotnet atomui help --format json
```

## 输出目标

| 场景 | 输出 |
| --- | --- |
| `help` | 工具定位、Usage、常用命令、命令分组、全局选项、详情入口。 |
| `help <command>` | 单个命令的摘要、Usage、参数、选项、示例、支持格式和模块激活说明。 |
| `<command> --help` | 与 `help <command>` 等价，不执行该命令 handler。 |
| 未知命令 | 参数错误，带相似命令建议。 |

## 首页文本结构

```text
AtomUI Cli

Usage:
  dotnet atomui <command> [options]

Common commands:
  list                 List available AtomUI controls.
  info <control>       Show metadata for a control.
  doc <control>        Generate usage documentation.
  demo <control>       Show demos for a control.
  doctor <path>        Diagnose an AtomUI project.
  setup                Configure local AtomUI Cli integration.

Command groups:
  Knowledge:
    list               List controls and packages.
    info               Show control metadata.

Global options:
  --format <text|json|markdown>
  --lang <zh|en>
  --target-version <version>
  --data-root <path>
  --detail
  --no-update-check

More:
  dotnet atomui help <command>
  dotnet atomui <command> --help
```

## 错误码与退出码

错误码、退出码、stderr/stdout 边界遵守 [AtomUI Cli 错误码标准](../error-code-standard.md)。

| 错误码 | 退出码 | 场景 |
| --- | --- | --- |
| `ATOMUICLI_ARG002` | `2` | `--format` 非法或 help 选项组合非法。 |
| `ATOMUICLI_ARG003` | `2` | `help <command>` 的目标命令不存在。 |

## 相关文档

- [详细设计](design.md)
- [命令运行时设计](../runtime-design.md)
- [命令设计标准](../command-design-standard.md)
- [Core 模块设计](../../modules/core/design.md)
