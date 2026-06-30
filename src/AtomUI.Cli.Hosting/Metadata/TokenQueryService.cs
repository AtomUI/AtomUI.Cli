namespace AtomUI.Cli.Hosting.Metadata;

public sealed class TokenQueryService(TokenSnapshotRegistry registry)
{
    private static readonly HashSet<string> SupportedScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        "shared",
        "control",
        "all"
    };

    private static readonly HashSet<string> SupportedKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "seed",
        "map",
        "alias",
        "control",
        "resource",
        "all"
    };

    private static readonly HashSet<string> SupportedThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "default",
        "dark",
        "compact",
        "all"
    };

    private static readonly HashSet<string> SupportedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "color",
        "size",
        "font",
        "motion",
        "radius",
        "shadow",
        "spacing",
        "state",
        "layout",
        "other"
    };

    private static readonly string[] CategoryOrder =
    [
        "color",
        "size",
        "font",
        "motion",
        "radius",
        "shadow",
        "spacing",
        "state",
        "layout",
        "other"
    ];

    public TokenQueryResult Query(TokenQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = registry.Snapshot;
        var scope = NormalizeScope(request.Scope, request.ControlName);
        var kind = NormalizeKind(request.Kind);
        var theme = NormalizeTheme(request.Theme);
        var includes = NormalizeIncludes(request.Include, request.IncludeUsage, request.IncludeChain, request.IncludeSource);

        if (!SupportedScopes.Contains(scope))
        {
            return Invalid($"Unknown token scope '{request.Scope}'. Supported scopes: shared, control, all.");
        }

        if (!SupportedKinds.Contains(kind))
        {
            return Invalid($"Unknown token kind '{request.Kind}'. Supported kinds: seed, map, alias, control, resource, all.");
        }

        if (!SupportedThemes.Contains(theme))
        {
            return Invalid($"Unknown token theme '{request.Theme}'. Supported themes: default, dark, compact, all.");
        }

        if (!string.IsNullOrWhiteSpace(request.Category) && !SupportedCategories.Contains(request.Category))
        {
            return Invalid($"Unknown token category '{request.Category}'. Supported categories: {string.Join(", ", SupportedCategories.OrderBy(item => Array.IndexOf(CategoryOrder, item)))}.");
        }

        ControlTokenSetDocument? controlSet = null;
        if (!string.IsNullOrWhiteSpace(request.ControlName))
        {
            controlSet = snapshot.ControlTokenSets.FirstOrDefault(control =>
                control.ControlName.Equals(request.ControlName, StringComparison.OrdinalIgnoreCase)
                && MatchesProduct(control, request.ProductId));

            if (controlSet is null)
            {
                var suggestions = snapshot.ControlTokenSets
                    .Where(control => MatchesProduct(control, request.ProductId))
                    .Select(control => control.ControlName)
                    .Where(candidate => IsClose(candidate, request.ControlName!))
                    .OrderBy(candidate => candidate, StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray();

                return TokenQueryResult.Failure(
                    TokenQueryStatus.ControlNotFound,
                    AtomUICliErrorCodes.ControlNotFound,
                    $"Control '{request.ControlName}' was not found.",
                    suggestions);
            }
        }

        var tokens = SelectScopeTokens(snapshot, controlSet, scope)
            .Where(token => MatchesKind(token, kind))
            .Where(token => string.IsNullOrWhiteSpace(request.Category) || token.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase))
            .Where(token => MatchesText(token, request.Match))
            .Where(token => !request.CustomizableOnly || token.Customization.Level.Equals("public-stable", StringComparison.OrdinalIgnoreCase))
            .Where(token => !request.UsedOnly || token.Usages.Count > 0)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(request.TokenName))
        {
            tokens = tokens
                .Where(token => token.Name.Equals(request.TokenName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (tokens.Length == 0)
            {
                var suggestions = SelectScopeTokens(snapshot, controlSet, scope)
                    .Select(token => token.Name)
                    .Where(candidate => IsClose(candidate, request.TokenName!))
                    .OrderBy(candidate => candidate, StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray();

                return TokenQueryResult.Failure(
                    TokenQueryStatus.TokenNotFound,
                    AtomUICliErrorCodes.TokenNotFound,
                    $"Token '{request.TokenName}' was not found.",
                    suggestions);
            }
        }

        tokens = tokens
            .OrderBy(token => ScopeOrder(token.Scope))
            .ThenBy(token => KindOrder(token.Kind))
            .ThenBy(token => CategoryOrderIndex(token.Category))
            .ThenBy(token => token.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var graph = includes.Contains("chain", StringComparer.OrdinalIgnoreCase)
            ? tokens.SelectMany(token => token.Dependencies).ToArray()
            : Array.Empty<TokenGraphEdgeDocument>();
        var usages = includes.Contains("usage", StringComparer.OrdinalIgnoreCase)
            ? tokens.SelectMany(token => token.Usages).ToArray()
            : Array.Empty<TokenUsageDocument>();

        if (request.Strict && snapshot.Diagnostics.Any(diagnostic => diagnostic.Severity.Equals("error", StringComparison.OrdinalIgnoreCase)))
        {
            return TokenQueryResult.Failure(
                TokenQueryStatus.DataReferenceMissing,
                AtomUICliErrorCodes.DataReferenceMissing,
                "Token snapshot contains error diagnostics.",
                []);
        }

        var payload = new TokenCommandPayload(
            "1.0",
            "token",
            request.TargetVersion ?? snapshot.TargetVersion,
            snapshot.SnapshotId,
            new TokenQuerySummaryPayload(
                controlSet?.ControlName,
                request.TokenName,
                scope,
                kind,
                request.Category,
                request.Match,
                theme,
                includes),
            controlSet is null ? null : CreateControlPayload(controlSet),
            CreateGroups(tokens),
            tokens.Select(CreateTokenPayload).ToArray(),
            graph.Select(CreateGraphPayload).ToArray(),
            usages.Select(CreateUsagePayload).ToArray(),
            snapshot.Diagnostics.Select(CreateDiagnosticPayload).ToArray(),
            CreateNextActions(controlSet, request.TokenName));

        return TokenQueryResult.Success(payload);
    }

    private static string NormalizeScope(string? scope, string? controlName)
    {
        return string.IsNullOrWhiteSpace(scope)
            ? string.IsNullOrWhiteSpace(controlName) ? "shared" : "control"
            : scope.Trim().ToLowerInvariant();
    }

    private static string NormalizeKind(string? kind)
    {
        return string.IsNullOrWhiteSpace(kind) ? "all" : kind.Trim().ToLowerInvariant();
    }

    private static string NormalizeTheme(string? theme)
    {
        return string.IsNullOrWhiteSpace(theme) ? "default" : theme.Trim().ToLowerInvariant();
    }

    private static IReadOnlyList<string> NormalizeIncludes(string? include, bool usage, bool chain, bool source)
    {
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(include))
        {
            foreach (var item in include.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                includes.Add(item.ToLowerInvariant());
            }
        }

        if (usage)
        {
            includes.Add("usage");
        }

        if (chain)
        {
            includes.Add("chain");
        }

        if (source)
        {
            includes.Add("source");
        }

        return includes.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyList<TokenDocument> SelectScopeTokens(TokenSnapshot snapshot, ControlTokenSetDocument? controlSet, string scope)
    {
        if (controlSet is null)
        {
            return scope.Equals("control", StringComparison.OrdinalIgnoreCase)
                ? []
                : snapshot.SharedTokens;
        }

        return scope.ToLowerInvariant() switch
        {
            "shared" => snapshot.SharedTokens
                .Where(token => controlSet.SharedDependencies.Contains(token.Id, StringComparer.OrdinalIgnoreCase))
                .ToArray(),
            "all" => snapshot.SharedTokens
                .Where(token => controlSet.SharedDependencies.Contains(token.Id, StringComparer.OrdinalIgnoreCase))
                .Concat(controlSet.Tokens)
                .ToArray(),
            _ => controlSet.Tokens
        };
    }

    private static bool MatchesProduct(ControlTokenSetDocument control, string? productId)
    {
        return string.IsNullOrWhiteSpace(productId)
               || control.ProductId.Equals(productId, StringComparison.OrdinalIgnoreCase)
               || control.PackageId.Equals(productId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesKind(TokenDocument token, string kind)
    {
        return kind.Equals("all", StringComparison.OrdinalIgnoreCase)
               || token.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase)
               || (kind.Equals("resource", StringComparison.OrdinalIgnoreCase) && token.Resource.ResourceKey is not null);
    }

    private static bool MatchesText(TokenDocument token, string? match)
    {
        return string.IsNullOrWhiteSpace(match)
               || token.Name.Contains(match, StringComparison.OrdinalIgnoreCase)
               || (token.Description?.Contains(match, StringComparison.OrdinalIgnoreCase) == true)
               || (token.Resource.ResourceKey?.Contains(match, StringComparison.OrdinalIgnoreCase) == true)
               || (token.Resource.MarkupExtension?.Contains(match, StringComparison.OrdinalIgnoreCase) == true)
               || token.Type.Contains(match, StringComparison.OrdinalIgnoreCase);
    }

    private static TokenControlPayload CreateControlPayload(ControlTokenSetDocument control)
    {
        return new TokenControlPayload(
            control.ControlName,
            control.TokenId,
            control.TokenTypeName,
            control.ScopeProviderName,
            control.PackageId,
            control.ProductId,
            control.IsCommercial,
            $"{control.TokenId}TokenResource",
            $"{control.TokenId}TokenKind",
            control.RegisteredControls);
    }

    private static IReadOnlyList<TokenGroupPayload> CreateGroups(IReadOnlyList<TokenDocument> tokens)
    {
        return tokens
            .GroupBy(token => token.Category, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => CategoryOrderIndex(group.Key))
            .Select(group => new TokenGroupPayload(group.Key, FormatCategory(group.Key), group.Select(CreateTokenPayload).ToArray()))
            .ToArray();
    }

    private static TokenDocumentPayload CreateTokenPayload(TokenDocument token)
    {
        return new TokenDocumentPayload(
            token.Id,
            token.Name,
            token.Scope,
            token.Kind,
            token.Category,
            token.Type,
            token.DefaultValue,
            token.Description,
            new TokenResourcePayload(
                token.Resource.ResourceKind,
                token.Resource.ResourceKey,
                token.Resource.MarkupExtension,
                token.Resource.XamlSnippet,
                token.Resource.Stability),
            new TokenSourcePayload(
                token.Source.DefinitionPath,
                token.Source.DefinitionLine,
                token.Source.GeneratedPath,
                token.Source.GeneratedLine),
            token.Values.Select(value => new TokenValueVariantPayload(value.Theme, value.Value, value.Source)).ToArray(),
            token.Dependencies.Select(CreateGraphPayload).ToArray(),
            token.Usages.Select(CreateUsagePayload).ToArray(),
            new TokenCustomizationPayload(
                token.Customization.Level,
                token.Customization.Recommendation,
                token.Customization.Notes));
    }

    private static TokenGraphEdgePayload CreateGraphPayload(TokenGraphEdgeDocument edge)
    {
        return new TokenGraphEdgePayload(edge.From, edge.To, edge.Relation, edge.Description);
    }

    private static TokenUsagePayload CreateUsagePayload(TokenUsageDocument usage)
    {
        return new TokenUsagePayload(
            usage.ThemeName,
            usage.Selector,
            usage.TargetProperty,
            usage.TargetNode,
            usage.ResourceExpression,
            usage.SourcePath,
            usage.SourceLine);
    }

    private static TokenDiagnosticPayload CreateDiagnosticPayload(TokenDiagnosticDocument diagnostic)
    {
        return new TokenDiagnosticPayload(diagnostic.Code, diagnostic.Severity, diagnostic.Message);
    }

    private static IReadOnlyList<TokenNextActionPayload> CreateNextActions(
        ControlTokenSetDocument? control,
        string? tokenName)
    {
        if (control is null)
        {
            return
            [
                new TokenNextActionPayload("dotnet atomui token --kind seed", "List seed shared tokens."),
                new TokenNextActionPayload("dotnet atomui token Button", "Inspect a control token set.")
            ];
        }

        if (!string.IsNullOrWhiteSpace(tokenName))
        {
            return
            [
                new TokenNextActionPayload($"dotnet atomui token {control.ControlName}", "Return to the full control token catalog."),
                new TokenNextActionPayload($"dotnet atomui doc {control.ControlName} --section tokens", "Open the token documentation section.")
            ];
        }

        return
        [
            new TokenNextActionPayload($"dotnet atomui token {control.ControlName} Padding --chain", "Inspect a token dependency chain."),
            new TokenNextActionPayload($"dotnet atomui token {control.ControlName} --usage", "Show ControlTheme token usage.")
        ];
    }

    private static int ScopeOrder(string scope)
    {
        return scope.Equals("shared", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
    }

    private static int KindOrder(string kind)
    {
        return kind.ToLowerInvariant() switch
        {
            "seed" => 0,
            "map" => 1,
            "alias" => 2,
            "control" => 3,
            "resource" => 4,
            _ => 5
        };
    }

    private static int CategoryOrderIndex(string category)
    {
        var index = Array.FindIndex(CategoryOrder, item => item.Equals(category, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? CategoryOrder.Length : index;
    }

    private static string FormatCategory(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "color" => "Color",
            "size" => "Size",
            "font" => "Font",
            "motion" => "Motion",
            "radius" => "Radius",
            "shadow" => "Shadow",
            "spacing" => "Spacing",
            "state" => "State",
            "layout" => "Layout",
            _ => "Other"
        };
    }

    private static bool IsClose(string candidate, string value)
    {
        return candidate.Contains(value, StringComparison.OrdinalIgnoreCase)
               || value.Contains(candidate, StringComparison.OrdinalIgnoreCase)
               || candidate.StartsWith(value[..Math.Min(value.Length, Math.Min(3, value.Length))], StringComparison.OrdinalIgnoreCase);
    }

    private static TokenQueryResult Invalid(string message)
    {
        return TokenQueryResult.Failure(TokenQueryStatus.InvalidArgument, AtomUICliErrorCodes.ArgumentInvalidValue, message, []);
    }
}

public sealed record TokenQueryRequest(
    string? ControlName,
    string? TokenName,
    string Scope,
    string Kind,
    string? Category,
    string? Match,
    string? ProductId,
    string Theme,
    string? Include,
    bool IncludeUsage,
    bool IncludeChain,
    bool IncludeSource,
    bool CustomizableOnly,
    bool UsedOnly,
    bool Strict,
    string? TargetVersion);

public sealed record TokenQueryResult(
    TokenQueryStatus Status,
    TokenCommandPayload? Payload,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<string> Suggestions)
{
    public static TokenQueryResult Success(TokenCommandPayload payload)
    {
        return new TokenQueryResult(TokenQueryStatus.Found, payload, null, null, []);
    }

    public static TokenQueryResult Failure(
        TokenQueryStatus status,
        string errorCode,
        string errorMessage,
        IReadOnlyList<string> suggestions)
    {
        return new TokenQueryResult(status, null, errorCode, errorMessage, suggestions);
    }
}

public enum TokenQueryStatus
{
    Found,
    InvalidArgument,
    ControlNotFound,
    TokenNotFound,
    DataUnavailable,
    DataReferenceMissing
}
