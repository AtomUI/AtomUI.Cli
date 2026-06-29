namespace AtomUI.Cli.Hosting.Metadata;

public sealed class MetadataQueryService(MetadataCatalog catalog)
{
    public MetadataCatalog Catalog => catalog;

    public IReadOnlyList<ControlDescriptor> ListControls(string? productId = null, string? category = null)
    {
        return catalog.Controls
            .Where(control => Matches(control.ProductId, productId)
                              && (string.IsNullOrWhiteSpace(category) || control.Category.Equals(category, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(control => control.ProductId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(control => control.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<ProductDescriptor> ListProducts()
    {
        return catalog.Products.OrderBy(product => product.Id, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public IReadOnlyList<PackageDescriptor> ListPackages(string? productId = null)
    {
        return catalog.Packages
            .Where(package => Matches(package.ProductId, productId))
            .OrderBy(package => package.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public ControlDescriptor? FindControl(string name, string? productId = null)
    {
        return catalog.Controls.FirstOrDefault(control =>
            control.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            && Matches(control.ProductId, productId));
    }

    public PackageDescriptor? FindPackageOrProduct(string value)
    {
        return catalog.Packages.FirstOrDefault(package => package.Id.Equals(value, StringComparison.OrdinalIgnoreCase))
               ?? catalog.Products
                   .Select(product => catalog.Packages.FirstOrDefault(package => package.ProductId.Equals(product.Id, StringComparison.OrdinalIgnoreCase)))
                   .FirstOrDefault(package => package is not null && package.ProductId.Equals(value, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<TokenDescriptor> FindTokens(string? controlName, string? name = null, string? match = null)
    {
        return catalog.Tokens
            .Where(token => controlName is null
                ? token.Scope.Equals("global", StringComparison.OrdinalIgnoreCase)
                : token.ControlName?.Equals(controlName, StringComparison.OrdinalIgnoreCase) == true)
            .Where(token => string.IsNullOrWhiteSpace(name) || token.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .Where(token => string.IsNullOrWhiteSpace(match)
                            || token.Name.Contains(match, StringComparison.OrdinalIgnoreCase)
                            || token.Description.Contains(match, StringComparison.OrdinalIgnoreCase))
            .OrderBy(token => token.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<DemoDescriptor> FindDemos(string controlName, string? demoName = null)
    {
        return catalog.Demos
            .Where(demo => demo.ControlName.Equals(controlName, StringComparison.OrdinalIgnoreCase))
            .Where(demo => string.IsNullOrWhiteSpace(demoName) || demo.Name.Equals(demoName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(demo => demo.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public DocumentDescriptor? FindDocument(string kind, string targetId)
    {
        return catalog.Documents.FirstOrDefault(document =>
            document.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase)
            && document.TargetId.Equals(targetId, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<SemanticPartDescriptor> FindSemanticParts(string controlName, string? part = null)
    {
        return catalog.SemanticParts
            .Where(item => item.ControlName.Equals(controlName, StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(part) || item.Name.Equals(part, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<ChangelogEntryDescriptor> FindChangelog(string? targetId = null)
    {
        return catalog.Changelog
            .Where(item => string.IsNullOrWhiteSpace(targetId) || item.TargetId.Equals(targetId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Version, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.TargetId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string RenderSummary()
    {
        return $"AtomUI metadata {catalog.TargetVersion}: {catalog.Controls.Count} controls, {catalog.Packages.Count} packages.";
    }

    private static bool Matches(string value, string? filter)
    {
        return string.IsNullOrWhiteSpace(filter) || value.Equals(filter, StringComparison.OrdinalIgnoreCase);
    }
}
