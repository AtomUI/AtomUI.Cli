namespace AtomUI.Cli.Hosting.Metadata;

public enum DocumentTargetKind
{
    Control,
    Topic
}

public sealed record DocumentSnapshot(
    string SchemaVersion,
    string SnapshotId,
    string TargetVersion,
    string ProductId,
    string PackageId,
    string Language,
    DocumentSourceIdentity Source,
    IReadOnlyList<ControlDocument> Controls,
    IReadOnlyList<TopicDocument> Topics);

public sealed record DocumentSourceIdentity(
    string SourceRootConvention,
    string SourceRef,
    string SourceCommit,
    string GeneratedAt);

public sealed record ControlDocument(
    string Id,
    string Name,
    string DisplayName,
    string CategoryId,
    string ProductId,
    string PackageId,
    bool IsCommercial,
    string Status,
    string Summary,
    ControlDocumentIdentity Identity,
    ControlUsageDocument Usage,
    ControlApiSurfaceDocument ApiSurface,
    ControlLogicStructureDocument LogicStructure,
    ControlThemeDocument Theme,
    IReadOnlyList<ControlExampleDocument> Examples,
    IReadOnlyList<ControlTokenDocument> Tokens,
    IReadOnlyList<ControlSemanticPartDocument> SemanticParts,
    IReadOnlyList<ControlSourceFileDocument> SourceFiles,
    IReadOnlyList<DocumentSectionContent> Sections,
    IReadOnlyList<DocumentRelatedItem> Related,
    IReadOnlyList<DocumentWarning> Warnings,
    DocumentSourceIdentity Source,
    string SnapshotId,
    string SnapshotSchemaVersion,
    string TargetVersion,
    string Language);

public sealed record ControlDocumentIdentity(
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

public sealed record ControlUsageDocument(
    string Summary,
    IReadOnlyList<string> WhenToUse,
    IReadOnlyList<string> WhenNotToUse,
    IReadOnlyList<CodeSnippetDocument> MinimalSnippets);

public sealed record ControlApiSurfaceDocument(
    IReadOnlyList<ApiMemberDocument> Members,
    IReadOnlyList<ApiEventDocument> Events,
    IReadOnlyList<ApiInheritanceDocument> InheritedContracts,
    IReadOnlyList<ApiSurfaceDiagnostic> Diagnostics);

public sealed record ApiMemberDocument(
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

public sealed record ApiEventDocument(
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

public sealed record ApiInheritanceDocument(
    string MemberName,
    string Kind,
    string DeclaringType,
    string Reason);

public sealed record ApiSurfaceDiagnostic(
    string Code,
    string Message,
    string Severity,
    string? MemberName);

public sealed record ControlLogicStructureDocument(
    IReadOnlyList<LogicNodeDocument> Inheritance,
    IReadOnlyList<LogicNodeDocument> PublicApiGroups,
    IReadOnlyList<LogicFlowDocument> StateFlows,
    IReadOnlyList<LogicNodeDocument> RuntimeCollaborators,
    IReadOnlyList<LogicFlowDocument> ThemeBridge);

public sealed record LogicNodeDocument(
    string Id,
    string Kind,
    string Label,
    string? Description,
    IReadOnlyList<string> RelatedApis);

public sealed record LogicFlowDocument(
    string Id,
    string Title,
    IReadOnlyList<string> Steps);

public sealed record ControlThemeDocument(
    string TargetType,
    IReadOnlyList<ThemeTemplateDocument> Templates,
    IReadOnlyList<ThemeSelectorGroupDocument> SelectorGroups,
    IReadOnlyList<ThemeBindingDocument> TemplateBindings,
    IReadOnlyList<ThemeTokenUsageDocument> TokenUsages,
    IReadOnlyList<ThemeCustomizationBoundaryDocument> CustomizationBoundaries);

public sealed record ThemeTemplateDocument(
    string Id,
    string SourceSelector,
    string SourcePath,
    int? SourceLine,
    IReadOnlyList<ThemeNodeDocument> Roots);

public sealed record ThemeNodeDocument(
    string ElementType,
    string? Name,
    string? Stability,
    IReadOnlyList<ThemeNodeDocument> Children);

public sealed record ThemeSelectorGroupDocument(
    string Kind,
    IReadOnlyList<string> Selectors,
    string Description);

public sealed record ThemeBindingDocument(
    string TargetNode,
    string TargetProperty,
    string SourceProperty);

public sealed record ThemeTokenUsageDocument(
    string ResourceKind,
    string TokenName,
    string TargetSelector,
    string TargetProperty);

public sealed record ThemeCustomizationBoundaryDocument(
    string Target,
    string Stability,
    string Guidance);

public sealed record ControlExampleDocument(
    string SourceKey,
    string Title,
    string Description,
    string Kind,
    int Priority,
    string? BadgeText,
    IReadOnlyList<CodeSnippetDocument> Snippets,
    string SourcePath,
    int? SourceLine);

public sealed record CodeSnippetDocument(
    string Language,
    string Code,
    string? SourcePath,
    int? SourceLine);

public sealed record ControlTokenDocument(
    string Name,
    string Scope,
    string Status,
    string Description,
    IReadOnlyList<string> ThemeUsage);

public sealed record ControlSemanticPartDocument(
    string Part,
    string AtomUINode,
    string Responsibility,
    IReadOnlyList<string> RelatedApis,
    IReadOnlyList<string> RelatedTokens,
    string Stability);

public sealed record ControlSourceFileDocument(
    string Kind,
    string Path,
    string? Description);

public sealed record TopicDocument(
    string Id,
    string Title,
    string Language,
    IReadOnlyList<DocumentSectionContent> Sections,
    IReadOnlyList<DocumentRelatedItem> Related,
    IReadOnlyList<DocumentWarning> Warnings,
    DocumentSourceIdentity Source,
    string SnapshotId,
    string SnapshotSchemaVersion,
    string TargetVersion);

public sealed record DocumentSectionContent(
    string Id,
    string Title,
    int Order,
    string Markdown,
    IReadOnlyList<string> SourceKinds);

public sealed record DocumentRelatedItem(
    string Kind,
    string Id,
    string Title,
    string Command);

public sealed record DocumentWarning(
    string Code,
    string Message,
    string? Section);
