using System.Text;

namespace AtomUI.Cli.Hosting.Metadata;

public sealed class TokenOutputRenderer
{
    public string RenderText(TokenCommandPayload payload, bool includeDetail)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Tokens.Count == 1 && !string.IsNullOrWhiteSpace(payload.Query.Token))
        {
            return RenderTokenDetailText(payload, includeDetail);
        }

        return payload.Control is null
            ? RenderSharedText(payload, includeDetail)
            : RenderControlText(payload, includeDetail);
    }

    public string RenderMarkdown(TokenCommandPayload payload, bool includeDetail)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return payload.Control is null
            ? RenderSharedMarkdown(payload, includeDetail)
            : RenderControlMarkdown(payload, includeDetail);
    }

    private static string RenderSharedText(TokenCommandPayload payload, bool includeDetail)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"AtomUI Tokens {payload.TargetVersion}");
        builder.AppendLine();
        builder.AppendLine("Shared tokens");

        foreach (var group in payload.Tokens.GroupBy(token => token.Kind, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"  {FormatKind(group.Key)}");
            foreach (var token in group.OrderBy(token => token.Name, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"    {token.Name.PadRight(20)} {token.Type.PadRight(12)} {FormatValue(token)}");
                builder.AppendLine($"      Description: {FormatDescription(token.Description)}");
            }

            builder.AppendLine();
        }

        AppendNextActions(builder, payload);
        return builder.ToString().TrimEnd();
    }

    private static string RenderControlText(TokenCommandPayload payload, bool includeDetail)
    {
        var control = payload.Control ?? throw new InvalidOperationException("Control payload is required.");
        var builder = new StringBuilder();
        builder.AppendLine($"AtomUI Token: {control.Name}");
        builder.AppendLine();
        builder.AppendLine("Token scope");
        builder.AppendLine($"  Token id: {control.TokenId}");
        builder.AppendLine($"  Token type: {control.TokenTypeName}");
        builder.AppendLine($"  Resource: {control.ResourceKind} / {control.ResourceKeyKind}");
        builder.AppendLine($"  Registered controls: {string.Join(", ", control.RegisteredControls)}");

        foreach (var group in payload.Groups)
        {
            builder.AppendLine();
            builder.AppendLine(group.Title);
            var nameWidth = Math.Max(20, group.Tokens.Max(token => token.Name.Length));
            var typeWidth = Math.Max(11, group.Tokens.Max(token => token.Type.Length));
            foreach (var token in group.Tokens)
            {
                builder.AppendLine($"  {token.Name.PadRight(nameWidth)} {token.Type.PadRight(typeWidth)}");
                builder.AppendLine($"    Description: {FormatDescription(token.Description)}");
                builder.AppendLine($"    Source: {FormatTextSource(token)}");
                builder.AppendLine($"    XAML: {FormatXaml(token)}");
            }
        }

        AppendNextActions(builder, payload);
        return builder.ToString().TrimEnd();
    }

    private static string RenderTokenDetailText(TokenCommandPayload payload, bool includeDetail)
    {
        var control = payload.Control;
        var token = payload.Tokens[0];
        var builder = new StringBuilder();
        builder.AppendLine(control is null ? token.Name : $"{control.Name}.{token.Name}");
        builder.AppendLine();
        builder.AppendLine($"Description: {FormatDescription(token.Description)}");
        builder.AppendLine($"Type: {token.Type}");
        if (!string.IsNullOrWhiteSpace(token.Resource.ResourceKey))
        {
            builder.AppendLine($"Resource key: {token.Resource.ResourceKey}");
        }

        if (!string.IsNullOrWhiteSpace(token.Resource.MarkupExtension))
        {
            builder.AppendLine($"XAML: {token.Resource.MarkupExtension}");
        }

        builder.AppendLine($"Customization: {token.Customization.Level}");

        if (token.Values.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Value source:");
            foreach (var value in token.Values)
            {
                var valueText = string.IsNullOrWhiteSpace(value.Value) ? string.Empty : $" = {value.Value}";
                builder.AppendLine($"  {value.Source}{valueText}");
            }
        }

        if (payload.Graph.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Depends on:");
            foreach (var edge in payload.Graph)
            {
                builder.AppendLine($"  {edge.From}");
            }
        }

        if (payload.Usages.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Used by:");
            foreach (var usage in payload.Usages)
            {
                builder.AppendLine($"  {usage.ThemeName}");
                builder.AppendLine($"    Selector: {usage.Selector}");
                builder.AppendLine($"    Setter: {usage.TargetProperty}");
                builder.AppendLine($"    Resource: {usage.ResourceExpression}");
                if (includeDetail)
                {
                    builder.AppendLine($"    Source: {usage.SourcePath}");
                }
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderSharedMarkdown(TokenCommandPayload payload, bool includeDetail)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# AtomUI Tokens {payload.TargetVersion}");
        builder.AppendLine();
        foreach (var group in payload.Tokens.GroupBy(token => token.Kind, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"## {FormatKind(group.Key)}");
            builder.AppendLine();
            AppendTokenTable(builder, group, includeDetail);
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderControlMarkdown(TokenCommandPayload payload, bool includeDetail)
    {
        var control = payload.Control ?? throw new InvalidOperationException("Control payload is required.");
        var builder = new StringBuilder();
        builder.AppendLine($"# {control.Name} Tokens");
        builder.AppendLine();
        builder.AppendLine("## Scope");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine($"| Token id | {control.TokenId} |");
        builder.AppendLine($"| Token type | `{control.TokenTypeName}` |");
        builder.AppendLine($"| Resource | {control.ResourceKind} / {control.ResourceKeyKind} |");
        builder.AppendLine($"| Registered controls | {string.Join(", ", control.RegisteredControls)} |");

        foreach (var group in payload.Groups)
        {
            builder.AppendLine();
            builder.AppendLine($"## {group.Title}");
            builder.AppendLine();
            AppendTokenTable(builder, group.Tokens, includeDetail);
        }

        if (payload.Usages.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Usage");
            builder.AppendLine();
            builder.AppendLine("| Theme | Selector | Property | Resource |");
            builder.AppendLine("| --- | --- | --- | --- |");
            foreach (var usage in payload.Usages)
            {
                builder.AppendLine($"| {usage.ThemeName} | `{usage.Selector}` | {usage.TargetProperty} | `{usage.ResourceExpression}` |");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendTokenTable(StringBuilder builder, IEnumerable<TokenDocumentPayload> tokens, bool includeDetail)
    {
        builder.AppendLine("| Token | Type | Description | Source | XAML |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var token in tokens)
        {
            builder.AppendLine($"| {EscapeMarkdown(token.Name)} | {EscapeMarkdown(token.Type)} | {EscapeMarkdown(FormatDescription(token.Description))} | {EscapeMarkdown(FormatMarkdownSource(token))} | `{EscapeMarkdown(FormatXaml(token))}` |");
        }
    }

    private static void AppendNextActions(StringBuilder builder, TokenCommandPayload payload)
    {
        if (payload.NextActions.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Run:");
        foreach (var action in payload.NextActions)
        {
            builder.AppendLine($"  {action.Command}");
        }
    }

    private static string FormatTextSource(TokenDocumentPayload token)
    {
        if (token.Dependencies.Count == 0)
        {
            return "calculated";
        }

        if (token.Dependencies.All(edge => edge.From.StartsWith("Shared.", StringComparison.Ordinal)))
        {
            return $"from shared {string.Join(", ", token.Dependencies.Select(edge => StripSharedPrefix(edge.From)))}";
        }

        return $"derived from {string.Join(", ", token.Dependencies.Select(edge => edge.From))}";
    }

    private static string FormatMarkdownSource(TokenDocumentPayload token)
    {
        if (token.Dependencies.Count == 0)
        {
            return "calculated";
        }

        if (token.Dependencies.All(edge => edge.From.StartsWith("Shared.", StringComparison.Ordinal)))
        {
            return $"from shared {string.Join(", ", token.Dependencies.Select(edge => StripSharedPrefix(edge.From)))}";
        }

        return $"derived from {string.Join(", ", token.Dependencies.Select(edge => edge.From))}";
    }

    private static string StripSharedPrefix(string dependency)
    {
        const string prefix = "Shared.";
        return dependency.StartsWith(prefix, StringComparison.Ordinal)
            ? dependency[prefix.Length..]
            : dependency;
    }

    private static string FormatValue(TokenDocumentPayload token)
    {
        return string.IsNullOrWhiteSpace(token.DefaultValue) ? "derived" : token.DefaultValue;
    }

    private static string FormatDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? "未提供源码 XML 描述。"
            : description.Trim();
    }

    private static string FormatXaml(TokenDocumentPayload token)
    {
        return string.IsNullOrWhiteSpace(token.Resource.MarkupExtension)
            ? "-"
            : token.Resource.MarkupExtension;
    }

    private static string EscapeMarkdown(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace(Environment.NewLine, " ", StringComparison.Ordinal);
    }

    private static string FormatKind(string kind)
    {
        return kind.ToLowerInvariant() switch
        {
            "seed" => "Seed",
            "map" => "Map",
            "alias" => "Alias",
            "control" => "Control",
            "resource" => "Resource",
            _ => "Other"
        };
    }
}
