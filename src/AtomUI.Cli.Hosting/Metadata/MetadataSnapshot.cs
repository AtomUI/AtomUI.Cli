namespace AtomUI.Cli.Hosting.Metadata;

public sealed record MetadataCatalog(
    string SchemaVersion,
    string TargetVersion,
    string SourceCommit,
    IReadOnlyList<ProductDescriptor> Products,
    IReadOnlyList<PackageDescriptor> Packages,
    IReadOnlyList<ControlCategoryDescriptor> Categories,
    IReadOnlyList<ControlDescriptor> Controls,
    IReadOnlyList<TokenDescriptor> Tokens,
    IReadOnlyList<DemoDescriptor> Demos,
    IReadOnlyList<DocumentDescriptor> Documents,
    IReadOnlyList<SemanticPartDescriptor> SemanticParts,
    IReadOnlyList<ChangelogEntryDescriptor> Changelog)
{
    public static MetadataCatalog CreateDefault()
    {
        return BuiltInMetadataCatalogFactory.Create();
    }

    internal static MetadataCatalog CreateFallback()
    {
        return new MetadataCatalog(
            "1.0",
            "unknown",
            "runtime-empty-fallback",
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
    }
}

public sealed record ProductDescriptor(string Id, string Name, string Description);

public sealed record PackageDescriptor(
    string Id,
    string ProductId,
    string Version,
    string Description,
    IReadOnlyList<string> Controls,
    bool IsOptional,
    bool IsCommercial);

public sealed record ControlCategoryDescriptor(
    string Id,
    string Name,
    string RouteSegment,
    int DisplayOrder);

public sealed record ControlDescriptor(
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
    bool IsHidden)
{
    public string Category => CategoryName;
}

public sealed record TokenDescriptor(
    string Name,
    string Scope,
    string? ControlName,
    string Type,
    string DefaultValue,
    string Description,
    bool IsInherited);

public sealed record DemoDescriptor(
    string ControlName,
    string Name,
    string Title,
    string Description,
    string Xaml,
    string CSharp);

public sealed record DocumentDescriptor(
    string Kind,
    string TargetId,
    string Title,
    string Markdown);

public sealed record SemanticPartDescriptor(
    string ControlName,
    string Name,
    string Description,
    IReadOnlyList<string> PseudoClasses);

public sealed record ChangelogEntryDescriptor(
    string Version,
    string TargetKind,
    string TargetId,
    string Severity,
    string Message);
