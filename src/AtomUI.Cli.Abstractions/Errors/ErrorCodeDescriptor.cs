namespace AtomUI.Cli;

public sealed record ErrorCodeDescriptor(
    string Code,
    string Domain,
    ErrorCodeKind Kind,
    int? DefaultExitCode,
    string Description);
