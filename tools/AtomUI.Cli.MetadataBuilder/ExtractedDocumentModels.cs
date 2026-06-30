using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

namespace AtomUI.Cli.MetadataBuilder;

internal sealed record ExtractedDocumentSnapshot(
    string SchemaVersion,
    string SnapshotId,
    string TargetVersion,
    string ProductId,
    string PackageId,
    string Language,
    ExtractedDocumentSource Source,
    IReadOnlyList<ExtractedControlDocument> Controls,
    IReadOnlyList<ExtractedTopicDocument> Topics);

internal sealed record ExtractedDocumentSource(
    string SourceRootConvention,
    string SourceRef,
    string SourceCommit,
    string GeneratedAt);

internal sealed record ExtractedControlDocument(
    string Id,
    string Name,
    string DisplayName,
    string CategoryId,
    string ProductId,
    string PackageId,
    bool IsCommercial,
    string Status,
    string Summary,
    ExtractedControlIdentityDocument Identity,
    ExtractedControlUsageDocument Usage,
    ExtractedControlApiSurfaceDocument ApiSurface,
    ExtractedControlLogicStructureDocument LogicStructure,
    ExtractedControlThemeDocument Theme,
    IReadOnlyList<ExtractedControlExampleDocument> Examples,
    IReadOnlyList<ExtractedControlTokenDocument> Tokens,
    IReadOnlyList<ExtractedControlSemanticPartDocument> SemanticParts,
    IReadOnlyList<ExtractedControlSourceFileDocument> SourceFiles,
    IReadOnlyList<ExtractedDocumentSectionContent> Sections,
    IReadOnlyList<ExtractedDocumentRelatedItem> Related,
    IReadOnlyList<ExtractedDocumentWarning> Warnings,
    ExtractedDocumentSource Source,
    string SnapshotId,
    string SnapshotSchemaVersion,
    string TargetVersion,
    string Language);

internal sealed record ExtractedControlIdentityDocument(
    string Id,
    string Name,
    string DisplayName,
    string CategoryId,
    string ProductId,
    string PackageId,
    string Namespace,
    string XamlNamespace,
    string? BaseType,
    string Status,
    bool IsCommercial);

internal sealed record ExtractedControlUsageDocument(
    string Summary,
    IReadOnlyList<string> WhenToUse,
    IReadOnlyList<string> WhenNotToUse,
    IReadOnlyList<ExtractedCodeSnippetDocument> MinimalSnippets);

internal sealed record ExtractedControlApiSurfaceDocument(
    IReadOnlyList<ExtractedApiMemberDocument> Members,
    IReadOnlyList<ExtractedApiEventDocument> Events,
    IReadOnlyList<ExtractedApiInheritanceDocument> InheritedContracts,
    IReadOnlyList<ExtractedApiSurfaceDiagnostic> Diagnostics);

internal sealed record ExtractedApiMemberDocument(
    string Name,
    string Kind,
    string DeclaringType,
    string Accessibility,
    string Signature,
    string? Type,
    string? DefaultValue,
    string ContractLevel,
    string Description,
    string SourcePath,
    int? SourceLine);

internal sealed record ExtractedApiEventDocument(
    string Name,
    string Kind,
    string DeclaringType,
    string Accessibility,
    string Signature,
    string? RoutingStrategy,
    string? EventArgsType,
    string Description,
    string SourcePath,
    int? SourceLine);

internal sealed record ExtractedApiInheritanceDocument(
    string MemberName,
    string Kind,
    string DeclaringType,
    string Reason);

internal sealed record ExtractedApiSurfaceDiagnostic(
    string Code,
    string Message,
    string Severity,
    string? MemberName);

internal sealed record ExtractedControlLogicStructureDocument(
    IReadOnlyList<ExtractedLogicNodeDocument> Inheritance,
    IReadOnlyList<ExtractedLogicNodeDocument> PublicApiGroups,
    IReadOnlyList<ExtractedLogicFlowDocument> StateFlows,
    IReadOnlyList<ExtractedLogicNodeDocument> RuntimeCollaborators,
    IReadOnlyList<ExtractedLogicFlowDocument> ThemeBridge);

internal sealed record ExtractedLogicNodeDocument(
    string Id,
    string Kind,
    string Label,
    string? Description,
    IReadOnlyList<string> RelatedApis);

internal sealed record ExtractedLogicFlowDocument(
    string Id,
    string Title,
    IReadOnlyList<string> Steps);

internal sealed record ExtractedControlThemeDocument(
    string TargetType,
    IReadOnlyList<ExtractedThemeTemplateDocument> Templates,
    IReadOnlyList<ExtractedThemeSelectorGroupDocument> SelectorGroups,
    IReadOnlyList<ExtractedThemeBindingDocument> TemplateBindings,
    IReadOnlyList<ExtractedThemeTokenUsageDocument> TokenUsages,
    IReadOnlyList<ExtractedThemeCustomizationBoundaryDocument> CustomizationBoundaries);

internal sealed record ExtractedThemeTemplateDocument(
    string Id,
    string SourceSelector,
    string SourcePath,
    int? SourceLine,
    IReadOnlyList<ExtractedThemeNodeDocument> Roots);

internal sealed record ExtractedThemeNodeDocument(
    string ElementType,
    string? Name,
    string? Stability,
    IReadOnlyList<ExtractedThemeNodeDocument> Children);

internal sealed record ExtractedThemeSelectorGroupDocument(
    string Kind,
    IReadOnlyList<string> Selectors,
    string Description);

internal sealed record ExtractedThemeBindingDocument(
    string TargetNode,
    string TargetProperty,
    string SourceProperty);

internal sealed record ExtractedThemeTokenUsageDocument(
    string ResourceKind,
    string TokenName,
    string TargetSelector,
    string TargetProperty);

internal sealed record ExtractedThemeCustomizationBoundaryDocument(
    string Target,
    string Stability,
    string Guidance);

internal sealed record ExtractedControlExampleDocument(
    string SourceKey,
    string Title,
    string Description,
    string Kind,
    int Priority,
    string? BadgeText,
    IReadOnlyList<ExtractedCodeSnippetDocument> Snippets,
    string SourcePath,
    int? SourceLine);

internal sealed record ExtractedCodeSnippetDocument(
    string Language,
    string Code,
    string? SourcePath,
    int? SourceLine);

internal sealed record ExtractedControlTokenDocument(
    string Name,
    string Scope,
    string Status,
    string Description,
    IReadOnlyList<string> ThemeUsage);

internal sealed record ExtractedControlSemanticPartDocument(
    string Part,
    string AtomUINode,
    string Responsibility,
    IReadOnlyList<string> RelatedApis,
    IReadOnlyList<string> RelatedTokens,
    string Stability);

internal sealed record ExtractedControlSourceFileDocument(
    string Kind,
    string Path,
    string? Description);

internal sealed record ExtractedTopicDocument(
    string Id,
    string Title,
    string Language,
    IReadOnlyList<ExtractedDocumentSectionContent> Sections,
    IReadOnlyList<ExtractedDocumentRelatedItem> Related,
    IReadOnlyList<ExtractedDocumentWarning> Warnings,
    ExtractedDocumentSource Source,
    string SnapshotId,
    string SnapshotSchemaVersion,
    string TargetVersion);

internal sealed record ExtractedDocumentSectionContent(
    string Id,
    string Title,
    int Order,
    string Markdown,
    IReadOnlyList<string> SourceKinds);

internal sealed record ExtractedDocumentRelatedItem(
    string Kind,
    string Id,
    string Title,
    string Command);

internal sealed record ExtractedDocumentWarning(
    string Code,
    string Message,
    string? Section);
