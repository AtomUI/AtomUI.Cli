namespace AtomUI.Cli;

public sealed record CliInvocationContext(
    string CommandName,
    IReadOnlyList<string> RawArguments,
    GlobalCliOptions GlobalOptions,
    string TraceId);
