# AtomUI.Cli

AtomUI.Cli is the command-line entry point for the AtomUI ecosystem. It is distributed as a .NET Tool and can be used from local development environments, CI pipelines, scripts, and agent workflows through `dotnet atomui ...`.

## Install

```bash
dotnet tool install -g AtomUI.Cli
```

## Basic Usage

```bash
dotnet atomui --version
dotnet atomui help
dotnet atomui help info
```

## Capabilities

- Query AtomUI controls, packages, design tokens, semantic parts, demos, and changelogs.
- Diagnose AtomUI project environments and inspect project usage.
- Run setup, init, add, and upgrade workflows with explicit write boundaries.
- Provide stable JSON output, error codes, exit codes, and stdout/stderr behavior for scripts and agents.
- Start the AtomUI MCP server for agent-based integrations.

## Runtime Design

AtomUI.Cli is built on GenericHost, dependency injection, and AtomUI.Modularity. Its runtime uses command pre-parsing and on-demand module activation so a single command only loads the modules it needs.

## License

Copyright (c) 2018-2026 Qinware Technologies Co., Ltd. All rights reserved.

AtomUI.Cli is licensed under the GNU Lesser General Public License v3.0 only.
