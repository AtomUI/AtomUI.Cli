namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Projection;

internal static class CatalogSnapshotProjection
{
    public static ExtractedCatalogSnapshot Create(SourceAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var categories = context.Facts.GetByPrefix("category:")
            .Select(fact => fact.Value)
            .OfType<ExtractedCatalogCategory>()
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Id, StringComparer.Ordinal)
            .ToArray();
        var controls = context.Facts.GetByPrefix("control:")
            .Select(fact => fact.Value)
            .OfType<ExtractedCatalogControl>()
            .OrderBy(control => control.DisplayOrder)
            .ThenBy(control => control.Name, StringComparer.Ordinal)
            .ToArray();
        if (categories.Length == 0 || controls.Length == 0)
        {
            throw new InvalidOperationException("Catalog category/control facts have not been produced by the source analysis pipeline.");
        }

        var products = context.Facts.GetByPrefix("product:")
            .Select(fact => fact.Value)
            .OfType<ExtractedCatalogProduct>()
            .OrderBy(product => product.Id, StringComparer.Ordinal)
            .ToArray();
        var packages = context.Facts.GetByPrefix("package:")
            .Select(fact => fact.Value)
            .OfType<ExtractedCatalogPackage>()
            .OrderBy(package => package.Id, StringComparer.Ordinal)
            .ToArray();
        var documents = context.Facts.GetByPrefix("document:")
            .Select(fact => fact.Value)
            .OfType<ExtractedCatalogDocument>()
            .OrderBy(document => document.Kind, StringComparer.Ordinal)
            .ThenBy(document => document.TargetId, StringComparer.Ordinal)
            .ToArray();
        var changelog = context.Facts.GetByPrefix("changelog:")
            .Select(fact => fact.Value)
            .OfType<ExtractedCatalogChangelogEntry>()
            .OrderByDescending(entry => entry.Version, StringComparer.Ordinal)
            .ThenBy(entry => entry.TargetKind, StringComparer.Ordinal)
            .ThenBy(entry => entry.TargetId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Message, StringComparer.Ordinal)
            .ToArray();

        return new ExtractedCatalogSnapshot(
            "1.0",
            context.Identity.TargetVersion,
            context.Identity.SourceCommit,
            products,
            packages,
            categories,
            controls,
            documents,
            changelog);
    }
}
