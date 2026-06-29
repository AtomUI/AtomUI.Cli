namespace AtomUI.Cli.Hosting.Setup;

public sealed record WritePlan(
    string SchemaVersion,
    string Command,
    bool WriteRequested,
    IReadOnlyList<WriteOperation> Operations,
    IReadOnlyList<WriteConflict> Conflicts,
    IReadOnlyList<string> VerificationCommands)
{
    public string RenderText()
    {
        var operationText = Operations.Count == 0
            ? "No file changes are required."
            : string.Join(Environment.NewLine, Operations.Select(operation => $"- {operation.Kind}: {operation.TargetPath}"));

        return $"{Command} write plan ({(WriteRequested ? "write" : "dry-run")}){Environment.NewLine}{operationText}";
    }
}

public sealed record WriteOperation(
    string Id,
    string Kind,
    string TargetPath,
    bool CreatesFile,
    bool ModifiesFile,
    string Preview,
    string RollbackHint);

public sealed record WriteConflict(
    string Code,
    string TargetPath,
    string Message,
    string SuggestedResolution,
    bool Blocking);
