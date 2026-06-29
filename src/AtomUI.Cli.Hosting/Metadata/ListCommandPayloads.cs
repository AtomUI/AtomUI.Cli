namespace AtomUI.Cli.Hosting.Metadata;

public sealed record ListCommandPayload(
    string SchemaVersion,
    string TargetVersion,
    ListKind Kind,
    string? ProductFilter,
    string? CategoryFilter,
    string? PackageFilter,
    IReadOnlyList<ListControlCategoryPayload> Categories,
    IReadOnlyList<ListProductPayload> Products,
    IReadOnlyList<ListPackagePayload> Packages,
    IReadOnlyList<ListWarningPayload> Warnings) : IAtomUICliJsonPayload
{
    public int Total => Kind switch
    {
        ListKind.Products => Products.Count,
        ListKind.Packages => Packages.Count,
        _ => Categories.Sum(category => category.Controls.Count)
    };

    public IReadOnlyDictionary<string, object?> ToJsonPayload()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = SchemaVersion,
            ["targetVersion"] = TargetVersion,
            ["kind"] = Kind.ToString().ToLowerInvariant(),
            ["total"] = Total,
            ["productFilter"] = ProductFilter,
            ["categoryFilter"] = CategoryFilter,
            ["packageFilter"] = PackageFilter,
            ["categories"] = Categories.Select(category => category.ToJson()).ToArray(),
            ["products"] = Products.Select(product => product.ToJson()).ToArray(),
            ["packages"] = Packages.Select(package => package.ToJson()).ToArray(),
            ["warnings"] = Warnings.Select(warning => warning.ToJson()).ToArray()
        };
    }
}

public sealed record ListControlCategoryPayload(
    string Id,
    string Name,
    int DisplayOrder,
    IReadOnlyList<ListControlPayload> Controls)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["name"] = Name,
            ["displayOrder"] = DisplayOrder,
            ["count"] = Controls.Count,
            ["controls"] = Controls.Select(control => control.ToJson()).ToArray()
        };
    }
}

public sealed record ListControlPayload(
    string Id,
    string Name,
    string DisplayName,
    string CategoryId,
    string CategoryName,
    string ProductId,
    string PackageId,
    string Namespace,
    string GalleryRoute,
    string Description,
    int DisplayOrder,
    bool IsCommercial,
    bool IsOptionalPackage)
{
    public static ListControlPayload FromDescriptor(ControlDescriptor descriptor)
    {
        return new ListControlPayload(
            descriptor.Name.ToLowerInvariant(),
            descriptor.Name,
            descriptor.DisplayName,
            descriptor.CategoryId,
            descriptor.CategoryName,
            descriptor.ProductId,
            descriptor.PackageId,
            descriptor.Namespace,
            descriptor.GalleryRoute,
            descriptor.Description,
            descriptor.DisplayOrder,
            descriptor.IsCommercial,
            descriptor.IsOptionalPackage);
    }

    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["name"] = Name,
            ["displayName"] = DisplayName,
            ["categoryId"] = CategoryId,
            ["categoryName"] = CategoryName,
            ["productId"] = ProductId,
            ["packageId"] = PackageId,
            ["namespace"] = Namespace,
            ["galleryRoute"] = GalleryRoute,
            ["description"] = Description,
            ["displayOrder"] = DisplayOrder,
            ["isCommercial"] = IsCommercial,
            ["isOptionalPackage"] = IsOptionalPackage
        };
    }
}

public sealed record ListProductPayload(string Id, string Name, string Description)
{
    public static ListProductPayload FromDescriptor(ProductDescriptor descriptor)
    {
        return new ListProductPayload(descriptor.Id, descriptor.Name, descriptor.Description);
    }

    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["name"] = Name,
            ["description"] = Description
        };
    }
}

public sealed record ListPackagePayload(
    string Id,
    string ProductId,
    string Version,
    string Description,
    int ControlCount,
    bool IsOptional,
    bool IsCommercial)
{
    public static ListPackagePayload FromDescriptor(PackageDescriptor descriptor)
    {
        return new ListPackagePayload(
            descriptor.Id,
            descriptor.ProductId,
            descriptor.Version,
            descriptor.Description,
            descriptor.Controls.Count,
            descriptor.IsOptional,
            descriptor.IsCommercial);
    }

    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["productId"] = ProductId,
            ["version"] = Version,
            ["description"] = Description,
            ["controlCount"] = ControlCount,
            ["isOptional"] = IsOptional,
            ["isCommercial"] = IsCommercial
        };
    }
}

public sealed record ListWarningPayload(string Code, string Message)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = Code,
            ["message"] = Message
        };
    }
}
