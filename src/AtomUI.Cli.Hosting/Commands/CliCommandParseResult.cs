using AtomUI.Cli;

namespace AtomUI.Cli.Hosting.Commands;

public sealed record CliCommandParseResult(
    bool IsSuccess,
    CliCommandDescriptor? Descriptor,
    GlobalCliOptions GlobalOptions,
    IReadOnlyList<string> CommandArguments,
    AtomUICliError? Error)
{
    public static CliCommandParseResult Success(
        CliCommandDescriptor descriptor,
        GlobalCliOptions globalOptions,
        IReadOnlyList<string> commandArguments)
    {
        return new CliCommandParseResult(true, descriptor, globalOptions, commandArguments, null);
    }

    public static CliCommandParseResult Failure(AtomUICliError error)
    {
        return new CliCommandParseResult(false, null, new GlobalCliOptions(), Array.Empty<string>(), error);
    }
}
