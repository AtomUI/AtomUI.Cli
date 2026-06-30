namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

internal sealed record GalleryDemoFact(
    string ControlName,
    string SourceKey,
    string Title,
    string Description,
    string Kind,
    int Priority,
    string? BadgeText,
    string XamlSnippet,
    SourceLocation SourceLocation);
