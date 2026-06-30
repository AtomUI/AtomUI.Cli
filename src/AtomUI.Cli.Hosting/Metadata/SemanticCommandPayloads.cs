namespace AtomUI.Cli.Hosting.Metadata;

public sealed record SemanticCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string SnapshotId,
    string SourceCommit,
    string Control,
    string? Part,
    IReadOnlyList<SemanticPartPayload> Parts,
    IReadOnlyList<SemanticPseudoClassPayload> PseudoClasses,
    IReadOnlyList<SemanticThemeTemplatePayload> Templates,
    IReadOnlyList<SemanticDiagnosticPayload> Diagnostics) : IAtomUICliJsonPayload
{
    public IReadOnlyDictionary<string, object?> ToJsonPayload()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = SchemaVersion,
            ["command"] = Command,
            ["targetVersion"] = TargetVersion,
            ["snapshotId"] = SnapshotId,
            ["sourceCommit"] = SourceCommit,
            ["control"] = Control,
            ["part"] = Part,
            ["parts"] = Parts.Select(part => part.ToJson()).ToArray(),
            ["pseudoClasses"] = PseudoClasses.Select(pseudoClass => pseudoClass.ToJson()).ToArray(),
            ["templates"] = Templates.Select(template => template.ToJson()).ToArray(),
            ["diagnostics"] = Diagnostics.Select(diagnostic => diagnostic.ToJson()).ToArray()
        };
    }
}

public sealed record SemanticPartPayload(
    string Name,
    string NodeType,
    string Description,
    IReadOnlyList<string> BoundApis,
    IReadOnlyList<string> PseudoClasses,
    IReadOnlyList<string> TokenUsages,
    SemanticSourcePayload Source)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["nodeType"] = NodeType,
            ["description"] = Description,
            ["boundApis"] = BoundApis.ToArray(),
            ["pseudoClasses"] = PseudoClasses.ToArray(),
            ["tokenUsages"] = TokenUsages.ToArray(),
            ["source"] = Source.ToJson()
        };
    }
}

public sealed record SemanticPseudoClassPayload(
    string Name,
    string Description,
    IReadOnlyList<string> BoundApis,
    IReadOnlyList<string> Selectors,
    SemanticSourcePayload Source)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["description"] = Description,
            ["boundApis"] = BoundApis.ToArray(),
            ["selectors"] = Selectors.ToArray(),
            ["source"] = Source.ToJson()
        };
    }
}

public sealed record SemanticSourcePayload(string Path, int? Line)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["path"] = Path,
            ["line"] = Line
        };
    }
}

public sealed record SemanticThemeTemplatePayload(
    string ThemeName,
    string Selector,
    SemanticSourcePayload Source,
    IReadOnlyList<SemanticThemeNodePayload> Roots)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["themeName"] = ThemeName,
            ["selector"] = Selector,
            ["source"] = Source.ToJson(),
            ["roots"] = Roots.Select(root => root.ToJson()).ToArray()
        };
    }
}

public sealed record SemanticThemeNodePayload(
    string ElementType,
    string? Name,
    IReadOnlyList<SemanticThemeNodePayload> Children)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["elementType"] = ElementType,
            ["name"] = Name,
            ["children"] = Children.Select(child => child.ToJson()).ToArray()
        };
    }
}

public sealed record SemanticDiagnosticPayload(string Code, string Severity, string Message)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = Code,
            ["severity"] = Severity,
            ["message"] = Message
        };
    }
}
