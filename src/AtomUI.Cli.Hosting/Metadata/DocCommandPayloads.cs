namespace AtomUI.Cli.Hosting.Metadata;

public sealed record DocCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string TargetKind,
    string TargetId,
    string Title,
    string Language,
    string? ProductId,
    string? PackageId,
    bool IsCommercial,
    DocumentSection RequestedSection,
    DocumentStyle Style,
    DocumentExamplesMode Examples,
    string? ExampleKey,
    DocSourceIdentityPayload Source,
    DocControlDocumentPayload? Control,
    IReadOnlyList<DocSectionPayload> Sections,
    IReadOnlyList<DocRelatedItemPayload> Related,
    IReadOnlyList<DocWarningPayload> Warnings,
    IReadOnlyList<DocSuggestionPayload> Suggestions) : IAtomUICliJsonPayload
{
    public IReadOnlyDictionary<string, object?> ToJsonPayload()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = SchemaVersion,
            ["command"] = Command,
            ["targetVersion"] = TargetVersion,
            ["targetKind"] = TargetKind,
            ["targetId"] = TargetId,
            ["title"] = Title,
            ["language"] = Language,
            ["productId"] = ProductId,
            ["packageId"] = PackageId,
            ["isCommercial"] = IsCommercial,
            ["requestedSection"] = DocumentSectionNames.ToId(RequestedSection),
            ["style"] = Style.ToString().ToLowerInvariant(),
            ["examples"] = Examples.ToString().ToLowerInvariant(),
            ["exampleKey"] = ExampleKey,
            ["source"] = Source.ToJson(),
            ["control"] = Control?.ToJson(),
            ["sections"] = Sections.Select(section => section.ToJson()).ToArray(),
            ["related"] = Related.Select(item => item.ToJson()).ToArray(),
            ["warnings"] = Warnings.Select(warning => warning.ToJson()).ToArray(),
            ["suggestions"] = Suggestions.Select(suggestion => suggestion.ToJson()).ToArray()
        };
    }
}

public sealed record DocControlDocumentPayload(
    DocControlIdentityPayload Identity,
    DocUsagePayload Usage,
    DocApiSurfacePayload ApiSurface,
    DocLogicStructurePayload LogicStructure,
    DocControlThemePayload Theme,
    IReadOnlyList<DocExamplePayload> Examples,
    IReadOnlyList<DocTokenPayload> Tokens,
    IReadOnlyList<DocSemanticPartPayload> SemanticParts,
    IReadOnlyList<DocSourceFilePayload> SourceFiles)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["identity"] = Identity.ToJson(),
            ["usage"] = Usage.ToJson(),
            ["apiSurface"] = ApiSurface.ToJson(),
            ["logicStructure"] = LogicStructure.ToJson(),
            ["theme"] = Theme.ToJson(),
            ["examples"] = Examples.Select(item => item.ToJson()).ToArray(),
            ["tokens"] = Tokens.Select(item => item.ToJson()).ToArray(),
            ["semanticParts"] = SemanticParts.Select(item => item.ToJson()).ToArray(),
            ["sourceFiles"] = SourceFiles.Select(item => item.ToJson()).ToArray()
        };
    }
}

public sealed record DocControlIdentityPayload(
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
    bool IsCommercial)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["name"] = Name,
            ["displayName"] = DisplayName,
            ["categoryId"] = CategoryId,
            ["productId"] = ProductId,
            ["packageId"] = PackageId,
            ["namespace"] = Namespace,
            ["xamlNamespace"] = XamlNamespace,
            ["baseType"] = BaseType,
            ["status"] = Status,
            ["isCommercial"] = IsCommercial
        };
    }
}

public sealed record DocUsagePayload(
    string Summary,
    IReadOnlyList<string> WhenToUse,
    IReadOnlyList<string> WhenNotToUse,
    IReadOnlyList<DocCodeSnippetPayload> MinimalSnippets)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["summary"] = Summary,
            ["whenToUse"] = WhenToUse.ToArray(),
            ["whenNotToUse"] = WhenNotToUse.ToArray(),
            ["minimalSnippets"] = MinimalSnippets.Select(item => item.ToJson()).ToArray()
        };
    }
}

public sealed record DocApiSurfacePayload(
    IReadOnlyList<DocApiMemberPayload> Members,
    IReadOnlyList<DocApiEventPayload> Events,
    IReadOnlyList<DocApiInheritancePayload> InheritedContracts,
    IReadOnlyList<DocApiDiagnosticPayload> Diagnostics)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["members"] = Members.Select(item => item.ToJson()).ToArray(),
            ["events"] = Events.Select(item => item.ToJson()).ToArray(),
            ["inheritedContracts"] = InheritedContracts.Select(item => item.ToJson()).ToArray(),
            ["diagnostics"] = Diagnostics.Select(item => item.ToJson()).ToArray()
        };
    }
}

public sealed record DocApiMemberPayload(
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
    int? SourceLine)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["kind"] = Kind,
            ["declaringType"] = DeclaringType,
            ["accessibility"] = Accessibility,
            ["signature"] = Signature,
            ["type"] = Type,
            ["defaultValue"] = DefaultValue,
            ["contractLevel"] = ContractLevel,
            ["description"] = Description,
            ["sourcePath"] = SourcePath,
            ["sourceLine"] = SourceLine
        };
    }
}

public sealed record DocApiEventPayload(
    string Name,
    string Kind,
    string DeclaringType,
    string Accessibility,
    string Signature,
    string? RoutingStrategy,
    string? EventArgsType,
    string Description,
    string SourcePath,
    int? SourceLine)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["kind"] = Kind,
            ["declaringType"] = DeclaringType,
            ["accessibility"] = Accessibility,
            ["signature"] = Signature,
            ["routingStrategy"] = RoutingStrategy,
            ["eventArgsType"] = EventArgsType,
            ["description"] = Description,
            ["sourcePath"] = SourcePath,
            ["sourceLine"] = SourceLine
        };
    }
}

public sealed record DocApiInheritancePayload(
    string MemberName,
    string Kind,
    string DeclaringType,
    string Reason)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["memberName"] = MemberName,
            ["kind"] = Kind,
            ["declaringType"] = DeclaringType,
            ["reason"] = Reason
        };
    }
}

public sealed record DocApiDiagnosticPayload(
    string Code,
    string Message,
    string Severity,
    string? MemberName)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = Code,
            ["message"] = Message,
            ["severity"] = Severity,
            ["memberName"] = MemberName
        };
    }
}

public sealed record DocLogicStructurePayload(
    IReadOnlyList<DocLogicNodePayload> Inheritance,
    IReadOnlyList<DocLogicNodePayload> PublicApiGroups,
    IReadOnlyList<DocLogicFlowPayload> StateFlows,
    IReadOnlyList<DocLogicNodePayload> RuntimeCollaborators,
    IReadOnlyList<DocLogicFlowPayload> ThemeBridge)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["inheritance"] = Inheritance.Select(item => item.ToJson()).ToArray(),
            ["publicApiGroups"] = PublicApiGroups.Select(item => item.ToJson()).ToArray(),
            ["stateFlows"] = StateFlows.Select(item => item.ToJson()).ToArray(),
            ["runtimeCollaborators"] = RuntimeCollaborators.Select(item => item.ToJson()).ToArray(),
            ["themeBridge"] = ThemeBridge.Select(item => item.ToJson()).ToArray()
        };
    }
}

public sealed record DocLogicNodePayload(
    string Id,
    string Kind,
    string Label,
    string? Description,
    IReadOnlyList<string> RelatedApis)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["kind"] = Kind,
            ["label"] = Label,
            ["description"] = Description,
            ["relatedApis"] = RelatedApis.ToArray()
        };
    }
}

public sealed record DocLogicFlowPayload(
    string Id,
    string Title,
    IReadOnlyList<string> Steps)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["title"] = Title,
            ["steps"] = Steps.ToArray()
        };
    }
}

public sealed record DocControlThemePayload(
    string TargetType,
    IReadOnlyList<DocThemeTemplatePayload> Templates,
    IReadOnlyList<DocThemeSelectorGroupPayload> SelectorGroups,
    IReadOnlyList<DocThemeBindingPayload> TemplateBindings,
    IReadOnlyList<DocThemeTokenUsagePayload> TokenUsages,
    IReadOnlyList<DocThemeCustomizationBoundaryPayload> CustomizationBoundaries)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["targetType"] = TargetType,
            ["templates"] = Templates.Select(item => item.ToJson()).ToArray(),
            ["selectorGroups"] = SelectorGroups.Select(item => item.ToJson()).ToArray(),
            ["templateBindings"] = TemplateBindings.Select(item => item.ToJson()).ToArray(),
            ["tokenUsages"] = TokenUsages.Select(item => item.ToJson()).ToArray(),
            ["customizationBoundaries"] = CustomizationBoundaries.Select(item => item.ToJson()).ToArray()
        };
    }
}

public sealed record DocThemeTemplatePayload(
    string Id,
    string SourceSelector,
    string SourcePath,
    int? SourceLine,
    IReadOnlyList<DocThemeNodePayload> Roots)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["sourceSelector"] = SourceSelector,
            ["sourcePath"] = SourcePath,
            ["sourceLine"] = SourceLine,
            ["roots"] = Roots.Select(item => item.ToJson()).ToArray()
        };
    }
}

public sealed record DocThemeNodePayload(
    string ElementType,
    string? Name,
    string? Stability,
    IReadOnlyList<DocThemeNodePayload> Children)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["elementType"] = ElementType,
            ["name"] = Name,
            ["stability"] = Stability,
            ["children"] = Children.Select(item => item.ToJson()).ToArray()
        };
    }
}

public sealed record DocThemeSelectorGroupPayload(
    string Kind,
    IReadOnlyList<string> Selectors,
    string Description)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["kind"] = Kind,
            ["selectors"] = Selectors.ToArray(),
            ["description"] = Description
        };
    }
}

public sealed record DocThemeBindingPayload(
    string TargetNode,
    string TargetProperty,
    string SourceProperty)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["targetNode"] = TargetNode,
            ["targetProperty"] = TargetProperty,
            ["sourceProperty"] = SourceProperty
        };
    }
}

public sealed record DocThemeTokenUsagePayload(
    string ResourceKind,
    string TokenName,
    string TargetSelector,
    string TargetProperty)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["resourceKind"] = ResourceKind,
            ["tokenName"] = TokenName,
            ["targetSelector"] = TargetSelector,
            ["targetProperty"] = TargetProperty
        };
    }
}

public sealed record DocThemeCustomizationBoundaryPayload(
    string Target,
    string Stability,
    string Guidance)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["target"] = Target,
            ["stability"] = Stability,
            ["guidance"] = Guidance
        };
    }
}

public sealed record DocExamplePayload(
    string SourceKey,
    string Title,
    string Description,
    string Kind,
    int Priority,
    string? BadgeText,
    IReadOnlyList<DocCodeSnippetPayload> Snippets,
    string SourcePath,
    int? SourceLine)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sourceKey"] = SourceKey,
            ["title"] = Title,
            ["description"] = Description,
            ["kind"] = Kind,
            ["priority"] = Priority,
            ["badgeText"] = BadgeText,
            ["snippets"] = Snippets.Select(item => item.ToJson()).ToArray(),
            ["sourcePath"] = SourcePath,
            ["sourceLine"] = SourceLine
        };
    }
}

public sealed record DocCodeSnippetPayload(
    string Language,
    string Code,
    string? SourcePath,
    int? SourceLine)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["language"] = Language,
            ["code"] = Code,
            ["sourcePath"] = SourcePath,
            ["sourceLine"] = SourceLine
        };
    }
}

public sealed record DocTokenPayload(
    string Name,
    string Scope,
    string Status,
    string Description,
    IReadOnlyList<string> ThemeUsage)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["scope"] = Scope,
            ["status"] = Status,
            ["description"] = Description,
            ["themeUsage"] = ThemeUsage.ToArray()
        };
    }
}

public sealed record DocSemanticPartPayload(
    string Part,
    string AtomUINode,
    string Responsibility,
    IReadOnlyList<string> RelatedApis,
    IReadOnlyList<string> RelatedTokens,
    string Stability)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["part"] = Part,
            ["atomUINode"] = AtomUINode,
            ["responsibility"] = Responsibility,
            ["relatedApis"] = RelatedApis.ToArray(),
            ["relatedTokens"] = RelatedTokens.ToArray(),
            ["stability"] = Stability
        };
    }
}

public sealed record DocSourceFilePayload(
    string Kind,
    string Path,
    string? Description)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["kind"] = Kind,
            ["path"] = Path,
            ["description"] = Description
        };
    }
}

public sealed record DocSectionPayload(
    string Id,
    string Title,
    int Order,
    string Markdown,
    IReadOnlyList<string> SourceKinds)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["title"] = Title,
            ["order"] = Order,
            ["markdown"] = Markdown,
            ["sourceKinds"] = SourceKinds.ToArray()
        };
    }
}

public sealed record DocSourceIdentityPayload(
    string SnapshotSchemaVersion,
    string SnapshotId,
    string SourceRef,
    string SourceCommit,
    string GeneratedAt)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["snapshotSchemaVersion"] = SnapshotSchemaVersion,
            ["snapshotId"] = SnapshotId,
            ["sourceRef"] = SourceRef,
            ["sourceCommit"] = SourceCommit,
            ["generatedAt"] = GeneratedAt
        };
    }
}

public sealed record DocRelatedItemPayload(
    string Kind,
    string Id,
    string Title,
    string Command)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["kind"] = Kind,
            ["id"] = Id,
            ["title"] = Title,
            ["command"] = Command
        };
    }
}

public sealed record DocWarningPayload(
    string Code,
    string Message,
    string? Section)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = Code,
            ["message"] = Message,
            ["section"] = Section
        };
    }
}

public sealed record DocSuggestionPayload(
    string Kind,
    string Id,
    string Title,
    string? ProductId)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["kind"] = Kind,
            ["id"] = Id,
            ["title"] = Title,
            ["productId"] = ProductId
        };
    }
}
