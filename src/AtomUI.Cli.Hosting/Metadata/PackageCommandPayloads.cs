namespace AtomUI.Cli.Hosting.Metadata;

public sealed record PackageCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    PackageQueryPayload Query,
    PackageSourcePayload Source,
    IReadOnlyList<ProductPackagePayload> Products,
    ProductPackagePayload? Product,
    IReadOnlyList<PackageSummaryPayload> Packages,
    PackageDetailPayload? Package,
    PackageDependencyTreePayload? DependencyTree,
    IReadOnlyList<PackageConflictPayload> Conflicts,
    IReadOnlyList<PackageReplacementPayload> Replacements,
    IReadOnlyList<PackageDiagnosticPayload> Diagnostics,
    IReadOnlyList<PackageSuggestionPayload> Suggestions) : IAtomUICliJsonPayload
{
    public IReadOnlyDictionary<string, object?> ToJsonPayload()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = SchemaVersion,
            ["command"] = Command,
            ["targetVersion"] = TargetVersion,
            ["query"] = Query.ToJson(),
            ["source"] = Source.ToJson(),
            ["products"] = Products.Select(product => product.ToJson()).ToArray(),
            ["product"] = Product?.ToJson(),
            ["packages"] = Packages.Select(package => package.ToJson()).ToArray(),
            ["package"] = Package?.ToJson(),
            ["dependencyTree"] = DependencyTree?.ToJson(),
            ["conflicts"] = Conflicts.Select(conflict => conflict.ToJson()).ToArray(),
            ["replacements"] = Replacements.Select(replacement => replacement.ToJson()).ToArray(),
            ["diagnostics"] = Diagnostics.Select(diagnostic => diagnostic.ToJson()).ToArray(),
            ["suggestions"] = Suggestions.Select(suggestion => suggestion.ToJson()).ToArray()
        };
    }
}

public sealed record PackageQueryPayload(
    string Mode,
    string? Target,
    string Kind,
    string? ProductFilter,
    IReadOnlyList<string> Include,
    bool Tree,
    bool CommercialOnly,
    bool IncludeHidden,
    bool Strict)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["mode"] = Mode,
            ["target"] = Target,
            ["kind"] = Kind,
            ["productFilter"] = ProductFilter,
            ["include"] = Include.ToArray(),
            ["tree"] = Tree,
            ["commercialOnly"] = CommercialOnly,
            ["includeHidden"] = IncludeHidden,
            ["strict"] = Strict
        };
    }
}

public sealed record PackageSourcePayload(
    string SnapshotId,
    string SourceRootConvention,
    string SourceRef,
    string SourceCommit,
    string GeneratedAt)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["snapshotId"] = SnapshotId,
            ["sourceRootConvention"] = SourceRootConvention,
            ["sourceRef"] = SourceRef,
            ["sourceCommit"] = SourceCommit,
            ["generatedAt"] = GeneratedAt
        };
    }
}

public sealed record ProductPackagePayload(
    string Id,
    string Name,
    string Description,
    int DisplayOrder,
    IReadOnlyList<string> RequiredPackages,
    IReadOnlyList<string> OptionalPackages,
    IReadOnlyList<string> CommercialPackages)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["name"] = Name,
            ["description"] = Description,
            ["displayOrder"] = DisplayOrder,
            ["requiredPackages"] = RequiredPackages.ToArray(),
            ["optionalPackages"] = OptionalPackages.ToArray(),
            ["commercialPackages"] = CommercialPackages.ToArray()
        };
    }
}

public sealed record PackageSummaryPayload(
    string Id,
    string ProductId,
    string Version,
    string Description,
    string PackageKind,
    bool IsRequired,
    bool IsOptional,
    bool IsCommercial,
    int ControlCount)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["productId"] = ProductId,
            ["version"] = Version,
            ["description"] = Description,
            ["packageKind"] = PackageKind,
            ["isRequired"] = IsRequired,
            ["isOptional"] = IsOptional,
            ["isCommercial"] = IsCommercial,
            ["controlCount"] = ControlCount
        };
    }
}

public sealed record PackageDetailPayload(
    PackageSummaryPayload Summary,
    IReadOnlyList<string> TargetFrameworks,
    string RootNamespace,
    string AssemblyName,
    IReadOnlyList<PackageDependencyPayload> Dependencies,
    IReadOnlyList<PackageRegistrationPayload> Registration,
    PackageCompatibilityPayload Compatibility,
    IReadOnlyList<PackageControlPayload> Controls,
    IReadOnlyList<PackageSourceFilePayload> SourceFiles)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["summary"] = Summary.ToJson(),
            ["id"] = Summary.Id,
            ["productId"] = Summary.ProductId,
            ["version"] = Summary.Version,
            ["description"] = Summary.Description,
            ["isCommercial"] = Summary.IsCommercial,
            ["targetFrameworks"] = TargetFrameworks.ToArray(),
            ["rootNamespace"] = RootNamespace,
            ["assemblyName"] = AssemblyName,
            ["dependencies"] = Dependencies.Select(dependency => dependency.ToJson()).ToArray(),
            ["registration"] = Registration.Select(registration => registration.ToJson()).ToArray(),
            ["compatibility"] = Compatibility.ToJson(),
            ["controls"] = Controls.Select(control => control.ToJson()).ToArray(),
            ["sourceFiles"] = SourceFiles.Select(source => source.ToJson()).ToArray()
        };
    }
}

public sealed record PackageDependencyPayload(string Id, string Version, string Kind, bool IsAtomUI)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["version"] = Version,
            ["kind"] = Kind,
            ["isAtomUI"] = IsAtomUI
        };
    }
}

public sealed record PackageRegistrationPayload(string Name, string Kind, string Description, string? SourcePath)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["kind"] = Kind,
            ["description"] = Description,
            ["sourcePath"] = SourcePath
        };
    }
}

public sealed record PackageCompatibilityPayload(
    IReadOnlyList<string> TargetFrameworks,
    bool IsAotCompatible,
    bool IsTrimCompatible,
    IReadOnlyList<string> Notes)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["targetFrameworks"] = TargetFrameworks.ToArray(),
            ["isAotCompatible"] = IsAotCompatible,
            ["isTrimCompatible"] = IsTrimCompatible,
            ["notes"] = Notes.ToArray()
        };
    }
}

public sealed record PackageControlPayload(string Name, string DisplayName, string CategoryId, string CategoryName, bool IsCommercial)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["displayName"] = DisplayName,
            ["categoryId"] = CategoryId,
            ["categoryName"] = CategoryName,
            ["isCommercial"] = IsCommercial
        };
    }
}

public sealed record PackageSourceFilePayload(string Kind, string Path)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["kind"] = Kind,
            ["path"] = Path
        };
    }
}

public sealed record PackageDependencyTreePayload(string RootPackageId, IReadOnlyList<PackageDependencyTreeNodePayload> Nodes)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["rootPackageId"] = RootPackageId,
            ["nodes"] = Nodes.Select(node => node.ToJson()).ToArray()
        };
    }
}

public sealed record PackageDependencyTreeNodePayload(
    string Id,
    string Version,
    string Kind,
    int Depth,
    IReadOnlyList<PackageDependencyTreeNodePayload> Children)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["version"] = Version,
            ["kind"] = Kind,
            ["depth"] = Depth,
            ["children"] = Children.Select(child => child.ToJson()).ToArray()
        };
    }
}

public sealed record PackageConflictPayload(string PackageId, string ConflictingPackageId, string Reason, string Severity)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["packageId"] = PackageId,
            ["conflictingPackageId"] = ConflictingPackageId,
            ["reason"] = Reason,
            ["severity"] = Severity
        };
    }
}

public sealed record PackageReplacementPayload(string PackageId, string ReplacementPackageId, string Reason)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["packageId"] = PackageId,
            ["replacementPackageId"] = ReplacementPackageId,
            ["reason"] = Reason
        };
    }
}

public sealed record PackageDiagnosticPayload(string Code, string Severity, string Message, string? PackageId)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = Code,
            ["severity"] = Severity,
            ["message"] = Message,
            ["packageId"] = PackageId
        };
    }
}

public sealed record PackageSuggestionPayload(string Command, string Description)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["command"] = Command,
            ["description"] = Description
        };
    }
}
