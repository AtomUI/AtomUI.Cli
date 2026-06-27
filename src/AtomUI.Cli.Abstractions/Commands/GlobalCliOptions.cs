namespace AtomUI.Cli;

public sealed record GlobalCliOptions(
    OutputFormat Format = OutputFormat.Text,
    string? TargetVersion = null,
    string? Product = null,
    string Language = "zh",
    bool Detail = false,
    string? DataRoot = null,
    bool NoUpdateCheck = false);
