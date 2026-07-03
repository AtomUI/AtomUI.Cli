namespace AtomUI.Cli.Hosting.Metadata;

public sealed record PackageSnapshot(
    string SchemaVersion,
    string SnapshotId,
    string TargetVersion,
    PackageSourceIdentity Source,
    IReadOnlyList<ProductPackageProfile> Products,
    IReadOnlyList<PackageDocument> Packages,
    IReadOnlyList<PackageConflictDocument> Conflicts,
    IReadOnlyList<PackageReplacementDocument> Replacements,
    IReadOnlyList<PackageDiagnostic> Diagnostics);

public sealed record PackageSourceIdentity(
    string SourceRootConvention,
    string SourceRef,
    string SourceCommit,
    string GeneratedAt);

public sealed record ProductPackageProfile(
    string Id,
    string Name,
    string Description,
    int DisplayOrder);

public sealed record PackageDocument(
    string Id,
    string ProductId,
    string Version,
    string Description,
    string PackageKind,
    bool IsRequired,
    bool IsOptional,
    bool IsCommercial,
    bool IsHidden,
    IReadOnlyList<string> TargetFrameworks,
    string RootNamespace,
    string AssemblyName,
    IReadOnlyList<PackageDependencyDocument> Dependencies,
    IReadOnlyList<PackageRegistrationDocument> Registration,
    PackageCompatibilityDocument Compatibility,
    IReadOnlyList<PackageControlDocument> Controls,
    IReadOnlyList<PackageSourceFileDocument> SourceFiles,
    IReadOnlyList<PackageDiagnostic> Diagnostics);

public sealed record PackageDependencyDocument(
    string Id,
    string Version,
    string Kind,
    bool IsAtomUI);

public sealed record PackageRegistrationDocument(
    string Name,
    string Kind,
    string Description,
    string? SourcePath);

public sealed record PackageCompatibilityDocument(
    IReadOnlyList<string> TargetFrameworks,
    bool IsAotCompatible,
    bool IsTrimCompatible,
    IReadOnlyList<string> Notes);

public sealed record PackageControlDocument(
    string Name,
    string DisplayName,
    string CategoryId,
    string CategoryName,
    bool IsCommercial);

public sealed record PackageSourceFileDocument(
    string Kind,
    string Path);

public sealed record PackageConflictDocument(
    string PackageId,
    string ConflictingPackageId,
    string Reason,
    string Severity);

public sealed record PackageReplacementDocument(
    string PackageId,
    string ReplacementPackageId,
    string Reason);

public sealed record PackageDiagnostic(
    string Code,
    string Severity,
    string Message,
    string? PackageId);
