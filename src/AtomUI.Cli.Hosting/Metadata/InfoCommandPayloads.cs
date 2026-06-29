namespace AtomUI.Cli.Hosting.Metadata;

public sealed record InfoCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    InfoControlIdentityPayload Control,
    InfoControlTypePayload Type,
    InfoControlDescriptionPayload Description,
    InfoControlUsagePayload Usage,
    IReadOnlyList<InfoApiMemberPayload> Api,
    InfoTemplateContractPayload Template,
    IReadOnlyList<InfoControlStatePayload> States,
    IReadOnlyList<InfoTokenSummaryPayload> Tokens,
    IReadOnlyList<InfoDemoSummaryPayload> Demos,
    IReadOnlyList<InfoRelatedCommandPayload> RelatedCommands,
    IReadOnlyList<InfoDiagnosticPayload> Diagnostics) : IAtomUICliJsonPayload
{
    public IReadOnlyDictionary<string, object?> ToJsonPayload()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = SchemaVersion,
            ["command"] = Command,
            ["targetVersion"] = TargetVersion,
            ["control"] = Control.ToJson(),
            ["type"] = Type.ToJson(),
            ["description"] = Description.ToJson(),
            ["usage"] = Usage.ToJson(),
            ["api"] = Api.Select(item => item.ToJson()).ToArray(),
            ["template"] = Template.ToJson(),
            ["states"] = States.Select(item => item.ToJson()).ToArray(),
            ["tokens"] = Tokens.Select(item => item.ToJson()).ToArray(),
            ["demos"] = Demos.Select(item => item.ToJson()).ToArray(),
            ["relatedCommands"] = RelatedCommands.Select(item => item.ToJson()).ToArray(),
            ["diagnostics"] = Diagnostics.Select(item => item.ToJson()).ToArray()
        };
    }
}

public sealed record InfoControlIdentityPayload(
    string Name,
    string DisplayName,
    string CategoryId,
    string CategoryName,
    string ProductId,
    string PackageId,
    bool IsCommercial,
    bool IsOptionalPackage,
    string GalleryRoute,
    string Status)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["displayName"] = DisplayName,
            ["categoryId"] = CategoryId,
            ["categoryName"] = CategoryName,
            ["productId"] = ProductId,
            ["packageId"] = PackageId,
            ["isCommercial"] = IsCommercial,
            ["isOptionalPackage"] = IsOptionalPackage,
            ["galleryRoute"] = GalleryRoute,
            ["status"] = Status
        };
    }
}

public sealed record InfoControlTypePayload(
    string Namespace,
    string AssemblyName,
    string TypeName,
    string BaseType,
    IReadOnlyList<string> Interfaces,
    string XamlNamespace,
    string XamlPrefix)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["namespace"] = Namespace,
            ["assemblyName"] = AssemblyName,
            ["typeName"] = TypeName,
            ["baseType"] = BaseType,
            ["interfaces"] = Interfaces.ToArray(),
            ["xamlNamespace"] = XamlNamespace,
            ["xamlPrefix"] = XamlPrefix
        };
    }
}

public sealed record InfoControlDescriptionPayload(
    string Subtitle,
    string Summary,
    string UsageGuidance)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["subtitle"] = Subtitle,
            ["summary"] = Summary,
            ["usageGuidance"] = UsageGuidance
        };
    }
}

public sealed record InfoControlUsagePayload(
    IReadOnlyList<string> RequiredPackages,
    IReadOnlyList<string> RequiredNamespaces,
    string XamlSnippet,
    string? CSharpSnippet)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["requiredPackages"] = RequiredPackages.ToArray(),
            ["requiredNamespaces"] = RequiredNamespaces.ToArray(),
            ["xamlSnippet"] = XamlSnippet,
            ["csharpSnippet"] = CSharpSnippet
        };
    }
}

public sealed record InfoApiMemberPayload(
    string Name,
    string MemberKind,
    string Type,
    string DefaultValue,
    string PropertyKind,
    string OwnerType,
    bool IsBindable,
    bool IsInherited,
    bool IsCurated,
    bool IsDeprecated,
    string? Since,
    string? Replacement,
    string Description)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["memberKind"] = MemberKind,
            ["type"] = Type,
            ["defaultValue"] = DefaultValue,
            ["propertyKind"] = PropertyKind,
            ["ownerType"] = OwnerType,
            ["isBindable"] = IsBindable,
            ["isInherited"] = IsInherited,
            ["isCurated"] = IsCurated,
            ["isDeprecated"] = IsDeprecated,
            ["since"] = Since,
            ["replacement"] = Replacement,
            ["description"] = Description
        };
    }
}

public sealed record InfoTemplateContractPayload(
    IReadOnlyList<string> ThemeFiles,
    IReadOnlyList<InfoTemplatePartPayload> Parts,
    IReadOnlyList<string> Notes)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["themeFiles"] = ThemeFiles.ToArray(),
            ["parts"] = Parts.Select(item => item.ToJson()).ToArray(),
            ["notes"] = Notes.ToArray()
        };
    }
}

public sealed record InfoTemplatePartPayload(
    string Name,
    string Type,
    bool IsRequired,
    string Source,
    string Description)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["type"] = Type,
            ["isRequired"] = IsRequired,
            ["source"] = Source,
            ["description"] = Description
        };
    }
}

public sealed record InfoControlStatePayload(
    string Name,
    string Kind,
    string Description,
    string Source)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["kind"] = Kind,
            ["description"] = Description,
            ["source"] = Source
        };
    }
}

public sealed record InfoTokenSummaryPayload(
    string Name,
    string Scope,
    string Type,
    string Status,
    string? DefaultValue,
    string? ResourceKey,
    string Description)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["scope"] = Scope,
            ["type"] = Type,
            ["status"] = Status,
            ["defaultValue"] = DefaultValue,
            ["resourceKey"] = ResourceKey,
            ["description"] = Description
        };
    }
}

public sealed record InfoDemoSummaryPayload(
    string Name,
    string Title,
    string Description,
    string Route,
    IReadOnlyList<string> Tags)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["title"] = Title,
            ["description"] = Description,
            ["route"] = Route,
            ["tags"] = Tags.ToArray()
        };
    }
}

public sealed record InfoRelatedCommandPayload(string Command, string Description)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["command"] = Command,
            ["description"] = Description
        };
    }
}

public sealed record InfoDiagnosticPayload(string Code, string Severity, string Message, string? Suggestion)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = Code,
            ["severity"] = Severity,
            ["message"] = Message,
            ["suggestion"] = Suggestion
        };
    }
}
