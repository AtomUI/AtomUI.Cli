using AtomUI.Cli;

namespace AtomUI.Cli.Hosting.Commands.BuiltIn;

public sealed record BuiltInCommandOptions(GlobalCliOptions Global) : IAtomUICliCommandOptions;
