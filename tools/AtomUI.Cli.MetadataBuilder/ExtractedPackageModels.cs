namespace AtomUI.Cli.MetadataBuilder;

internal sealed record ExtractedPackageSnapshot(
    string SchemaVersion,
    string SnapshotId,
    string TargetVersion,
    ExtractedPackageSourceIdentity Source,
    IReadOnlyList<ExtractedPackageProduct> Products,
    IReadOnlyList<ExtractedPackageDocument> Packages,
    IReadOnlyList<ExtractedPackageConflict> Conflicts,
    IReadOnlyList<ExtractedPackageReplacement> Replacements,
    IReadOnlyList<ExtractedPackageDiagnostic> Diagnostics);

internal sealed record ExtractedPackageSourceIdentity(
    string SourceRootConvention,
    string SourceRef,
    string SourceCommit,
    string GeneratedAt);

internal sealed record ExtractedPackageProduct(
    string Id,
    string Name,
    string Description,
    int DisplayOrder);

internal sealed record ExtractedPackageDocument(
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
    IReadOnlyList<ExtractedPackageDependency> Dependencies,
    IReadOnlyList<ExtractedPackageRegistration> Registration,
    ExtractedPackageCompatibility Compatibility,
    IReadOnlyList<ExtractedPackageControl> Controls,
    IReadOnlyList<ExtractedPackageSourceFile> SourceFiles,
    IReadOnlyList<ExtractedPackageDiagnostic> Diagnostics);

internal sealed record ExtractedPackageDependency(
    string Id,
    string Version,
    string Kind,
    bool IsAtomUI);

internal sealed record ExtractedPackageRegistration(
    string Name,
    string Kind,
    string Description,
    string? SourcePath);

internal sealed record ExtractedPackageCompatibility(
    IReadOnlyList<string> TargetFrameworks,
    bool IsAotCompatible,
    bool IsTrimCompatible,
    IReadOnlyList<string> Notes);

internal sealed record ExtractedPackageControl(
    string Name,
    string DisplayName,
    string CategoryId,
    string CategoryName,
    bool IsCommercial);

internal sealed record ExtractedPackageSourceFile(
    string Kind,
    string Path);

internal sealed record ExtractedPackageConflict(
    string PackageId,
    string ConflictingPackageId,
    string Reason,
    string Severity);

internal sealed record ExtractedPackageReplacement(
    string PackageId,
    string ReplacementPackageId,
    string Reason);

internal sealed record ExtractedPackageDiagnostic(
    string Code,
    string Severity,
    string Message,
    string? PackageId);
