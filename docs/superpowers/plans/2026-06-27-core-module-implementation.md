# AtomUI Cli Core Module Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Implement the first working AtomUI Cli core module with solution skeleton, error contract, command runtime, GenericHost integration, and a minimal executable entry.

**Architecture:** The implementation creates `AtomUI.Cli.Abstractions`, `AtomUI.Cli.Modularity`, `AtomUI.Cli.Hosting`, and `AtomUI.Cli` as the initial runtime surface. `AtomUI.Cli.Hosting` is the composition root, maps AtomUI module service descriptors into GenericHost DI, owns command dispatch, and keeps command handlers scoped.

**Tech Stack:** .NET SDK 10, C# nullable/implicit usings, AtomUI.Base 1.0.0-alpha.1, Microsoft.Extensions.Hosting 10.0.7, xUnit v3, Microsoft.NET.Test.Sdk 18.4.0.

---

### Task 1: Solution Skeleton

**Files:**
- Create: `AtomUICli.slnx`
- Create: `Directory.Build.props`
- Create: `Directory.Packages.props`
- Create: `global.json`
- Create: `src/AtomUI.Cli.Abstractions/AtomUI.Cli.Abstractions.csproj`
- Create: `src/AtomUI.Cli.Modularity/AtomUI.Cli.Modularity.csproj`
- Create: `src/AtomUI.Cli.Hosting/AtomUI.Cli.Hosting.csproj`
- Create: `src/AtomUI.Cli/AtomUI.Cli.csproj`
- Create: `tests/AtomUI.Cli.Tests/AtomUI.Cli.Tests.csproj`

- [x] **Step 1: Create project files and solution**

Use `net10.0`, Central Package Management, nullable enabled, and explicit project references matching the architecture docs.

- [x] **Step 2: Restore and build empty projects**

Run: `dotnet build AtomUICli.slnx`

Expected: build succeeds before behavior is added.

### Task 2: Error Contract

**Files:**
- Test: `tests/AtomUI.Cli.Tests/Core/ErrorCodeCatalogTests.cs`
- Test: `tests/AtomUI.Cli.Tests/Core/ExitCodeMapperTests.cs`
- Create: `src/AtomUI.Cli.Abstractions/Errors/AtomUICliErrorCodes.cs`
- Create: `src/AtomUI.Cli.Abstractions/Errors/AtomUICliError.cs`
- Create: `src/AtomUI.Cli.Abstractions/Diagnostics/AtomUICliDiagnostic.cs`
- Create: `src/AtomUI.Cli.Hosting/Errors/ErrorCodeCatalog.cs`
- Create: `src/AtomUI.Cli.Hosting/Errors/ExitCodeMapper.cs`

- [x] **Step 1: Write failing catalog and mapper tests**

Tests assert that all documented codes exist, duplicate codes are rejected by construction, and success/error/diagnostic results map to documented exit codes.

- [x] **Step 2: Implement constants, DTOs, catalog, and mapper**

Keep the catalog explicit and AOT-friendly; do not scan constants by reflection.

- [x] **Step 3: Re-run focused tests**

Run: `dotnet test tests/AtomUI.Cli.Tests/AtomUI.Cli.Tests.csproj --filter ErrorCode`

Expected: tests pass.

### Task 3: Command Runtime

**Files:**
- Test: `tests/AtomUI.Cli.Tests/Core/CommandCatalogTests.cs`
- Test: `tests/AtomUI.Cli.Tests/Core/CommandParserTests.cs`
- Test: `tests/AtomUI.Cli.Tests/Core/CommandDispatcherTests.cs`
- Create: `src/AtomUI.Cli.Abstractions/Commands/IAtomUICliCommandHandler.cs`
- Create: `src/AtomUI.Cli.Abstractions/Commands/IAtomUICliCommandOptions.cs`
- Create: `src/AtomUI.Cli.Abstractions/Commands/GlobalCliOptions.cs`
- Create: `src/AtomUI.Cli.Hosting/Commands/CliCommandDescriptor.cs`
- Create: `src/AtomUI.Cli.Hosting/Commands/CliCommandDescriptorCatalog.cs`
- Create: `src/AtomUI.Cli.Hosting/Commands/CliCommandParser.cs`
- Create: `src/AtomUI.Cli.Hosting/Commands/CliCommandDispatcher.cs`

- [x] **Step 1: Write failing command catalog, parser, and dispatcher tests**

Tests cover duplicate command names, unknown command mapping to `ATOMUICLI_ARG003`, global option parsing, scoped handler resolution, and cancellation propagation.

- [x] **Step 2: Implement command descriptors, parser, and dispatcher**

Parser handles global options and command names only in this phase. Business options binding stays outside the core module until concrete commands need it.

- [x] **Step 3: Re-run focused command tests**

Run: `dotnet test tests/AtomUI.Cli.Tests/AtomUI.Cli.Tests.csproj --filter Command`

Expected: tests pass.

### Task 4: Output Writers

**Files:**
- Test: `tests/AtomUI.Cli.Tests/Core/OutputWriterTests.cs`
- Create: `src/AtomUI.Cli.Abstractions/Output/OutputFormat.cs`
- Create: `src/AtomUI.Cli.Hosting/Output/ConsoleOutputWriter.cs`
- Create: `src/AtomUI.Cli.Hosting/Output/ConsoleErrorWriter.cs`
- Create: `src/AtomUI.Cli.Hosting/Output/JsonOutputSerializer.cs`

- [x] **Step 1: Write failing stdout/stderr separation tests**

Tests assert success payloads write stdout, hard errors write stderr, and JSON hard errors leave stdout empty.

- [x] **Step 2: Implement writer abstractions**

Use injected `TextWriter` instances in tests and console writers in production registration.

- [x] **Step 3: Re-run output tests**

Run: `dotnet test tests/AtomUI.Cli.Tests/AtomUI.Cli.Tests.csproj --filter Output`

Expected: tests pass.

### Task 5: Modularity and Application Host

**Files:**
- Test: `tests/AtomUI.Cli.Tests/Core/AtomUICliApplicationTests.cs`
- Create: `src/AtomUI.Cli.Modularity/AtomUICliModule.cs`
- Create: `src/AtomUI.Cli.Modularity/AtomUICliCommandContributionContext.cs`
- Create: `src/AtomUI.Cli.Hosting/AtomUICliApplication.cs`
- Create: `src/AtomUI.Cli.Hosting/AtomUICliApplicationBuilder.cs`
- Create: `src/AtomUI.Cli.Hosting/AtomUICliCoreModule.cs`
- Create: `src/AtomUI.Cli.Hosting/DependencyInjection/ModuleServiceDescriptorMapper.cs`

- [x] **Step 1: Write failing application lifecycle tests**

Tests use fake modules and fake handlers to verify GenericHost starts before dispatch, stops after command execution, and module shutdown runs on command failure.

- [x] **Step 2: Implement module base, contribution context, application builder, and core module registrations**

Use AtomUI.Base `ModuleHost` for lifecycle execution and GenericHost for DI.

- [x] **Step 3: Re-run application tests**

Run: `dotnet test tests/AtomUI.Cli.Tests/AtomUI.Cli.Tests.csproj --filter Application`

Expected: tests pass.

### Task 6: Executable Entry

**Files:**
- Test: `tests/AtomUI.Cli.Tests/Core/ProgramSmokeTests.cs`
- Create: `src/AtomUI.Cli/Program.cs`

- [x] **Step 1: Write failing CLI smoke test**

Test invokes `AtomUICliApplication` with `--version` and unknown command args.

- [x] **Step 2: Implement `Program.Main` and minimal help/version behavior**

`Program.Main` only creates the application and returns its exit code.

- [x] **Step 3: Run full verification**

Run: `dotnet test AtomUICli.slnx`

Run: `dotnet build AtomUICli.slnx`

Expected: all tests pass and solution builds.
