# AtomUI.Cli

<div align="center">

[![NuGet](https://img.shields.io/nuget/v/AtomUI.Cli?style=flat-square&logo=nuget&label=NuGet)](https://www.nuget.org/packages/AtomUI.Cli)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AtomUI.Cli?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/AtomUI.Cli)
[![License](https://img.shields.io/badge/license-LGPL--3.0-white?labelColor=black&style=flat-square)](LICENSE)
[![.NET Tool](https://img.shields.io/badge/.NET%20Tool-AtomUI.Cli-512bd4?style=flat-square&logo=dotnet)](https://www.nuget.org/packages/AtomUI.Cli)

Documentation Language: [English](README.md) | [简体中文](README.zh-CN.md)

</div>

## Overview

AtomUI.Cli is the command-line entry point for the AtomUI ecosystem. It is distributed as a NuGet-powered .NET Tool and can be used from local development environments, CI pipelines, scripts, and agent workflows through `dotnet atomui ...`.

AtomUI.Cli focuses on everyday engineering workflows for AtomUI projects: looking up control and package metadata, generating component usage notes, diagnosing project environments, assisting migrations, initializing project configuration, and exposing MCP integration for agent-based tooling. It is designed as a stable command runtime for the AtomUI ecosystem, not as a collection of ad hoc scripts.

## Features

- Installed and invoked as a .NET Tool for terminals, CI pipelines, scripts, and automation workflows.
- Provides knowledge lookup for controls, packages, design tokens, semantic parts, demos, and changelogs.
- Provides project environment checks, diagnostics, usage scanning, linting, and migration analysis.
- Supports setup, init, add, and upgrade workflows with explicit write boundaries.
- Provides stable JSON output, error codes, exit codes, and stdout/stderr behavior for agents and scripts.
- Uses GenericHost, dependency injection, and AtomUI.Modularity to organize application lifecycle, module dependencies, and command contributions.
- Follows an AOT-first and trimming-friendly design, avoiding assembly scanning on runtime hot paths.

## Status

The repository is being developed around the `1.0` release line. The current implementation includes the core application host, modular runtime, command pre-parsing, on-demand module activation, command dispatching, output abstractions, error-code infrastructure, and the first implemented commands.

## Get Started

After the package is published to NuGet, install it as a global .NET Tool:

```bash
dotnet tool install -g AtomUI.Cli
```

Then check the version and command help:

```bash
dotnet atomui --version
dotnet atomui help
dotnet atomui help info
```

For local development, run the entry project directly:

```bash
dotnet run --project src/AtomUI.Cli/AtomUI.Cli.csproj -- help
```

## Command Surface

The public command surface is described in [docs/commands](docs/commands/overview.md).

| Group | Capabilities |
| --- | --- |
| Knowledge | Look up controls, control metadata, docs, demos, tokens, semantic parts, packages, and changelogs. |
| Project analysis | Inspect environments, diagnose projects, scan control usage, run lint checks, and generate migration analysis. |
| Setup | Initialize configuration, add packages or products, and upgrade the CLI or project dependencies. |
| Integration | Show help, print version information, and start the MCP server. |

Every command follows the shared error-code, exit-code, output, and JSON serialization rules documented in the [error-code standard](docs/commands/error-code-standard.md).

## Architecture

AtomUI.Cli is composed of a lightweight entry project and a hosted runtime:

- `src/AtomUI.Cli`: .NET Tool entry project.
- `src/AtomUI.Cli.Abstractions`: command, output, diagnostic, result, and error contracts.
- `src/AtomUI.Cli.Hosting`: application host, command catalogs, parsers, dispatcher, output writers, and core module.
- `src/AtomUI.Cli.Modularity`: CLI-specific module and command contribution contracts.
- `tests/AtomUI.Cli.Tests`: core runtime behavior tests.

See the [architecture design](docs/architecture/atomui-cli-architecture-design.md) and [module design](docs/modules/overview.md) for details.

## Development

The repository pins its .NET SDK through [global.json](global.json). Common commands:

```bash
dotnet build AtomUICli.slnx
dotnet test AtomUICli.slnx
dotnet pack src/AtomUI.Cli/AtomUI.Cli.csproj --configuration Release
```

Before submitting changes, run:

```bash
git diff --check
```

## License

Copyright (c) 2018-2026 Qinware Technologies Co., Ltd. All rights reserved.

AtomUI.Cli is licensed under the GNU Lesser General Public License v3.0 only. See [LICENSE](LICENSE) for details.
