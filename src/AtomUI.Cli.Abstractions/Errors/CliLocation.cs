namespace AtomUI.Cli;

public sealed record CliLocation(
    string? File = null,
    int? Line = null,
    int? Column = null);
