namespace AtomUI.Cli.MetadataBuilder;

internal sealed record ExtractedCatalogSnapshot(
    string SchemaVersion,
    string TargetVersion,
    string SourceCommit,
    IReadOnlyList<ExtractedCatalogProduct> Products,
    IReadOnlyList<ExtractedCatalogPackage> Packages,
    IReadOnlyList<ExtractedCatalogCategory> Categories,
    IReadOnlyList<ExtractedCatalogControl> Controls,
    IReadOnlyList<ExtractedCatalogDocument> Documents,
    IReadOnlyList<ExtractedCatalogChangelogEntry> Changelog);

internal sealed record ExtractedCatalogProduct(string Id, string Name, string Description);

internal sealed record ExtractedCatalogPackage(
    string Id,
    string ProductId,
    string Version,
    string Description,
    IReadOnlyList<string> Controls,
    bool IsOptional,
    bool IsCommercial);

internal sealed record ExtractedCatalogCategory(
    string Id,
    string Name,
    string RouteSegment,
    int DisplayOrder);

internal sealed record ExtractedCatalogControl(
    string Name,
    string DisplayName,
    string ProductId,
    string PackageId,
    string Namespace,
    string GalleryRoute,
    string Description,
    string CategoryId,
    string CategoryName,
    int DisplayOrder,
    bool IsCommercial,
    bool IsOptionalPackage,
    bool IsHidden);

internal sealed record ExtractedCatalogDocument(
    string Kind,
    string TargetId,
    string Title,
    string Markdown);

internal sealed record ExtractedCatalogChangelogEntry(
    string Version,
    string TargetKind,
    string TargetId,
    string Severity,
    string Message);
