using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis;

internal sealed class SourceFactStore
{
    private readonly Dictionary<string, List<SourceFactContribution>> _facts = new(StringComparer.Ordinal);

    public void Add(string key, string processorId, object value, SourceLocation? sourceLocation = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Source fact key cannot be empty.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(processorId))
        {
            throw new ArgumentException("Source fact processor id cannot be empty.", nameof(processorId));
        }

        ArgumentNullException.ThrowIfNull(value);
        if (!_facts.TryGetValue(key, out var contributions))
        {
            contributions = [];
            _facts.Add(key, contributions);
        }

        contributions.Add(new SourceFactContribution(key, processorId, value, sourceLocation));
    }

    public IReadOnlyList<SourceFactContribution> Get(string key)
    {
        return _facts.TryGetValue(key, out var contributions)
            ? contributions
            : [];
    }

    public IReadOnlyList<SourceFactContribution> GetByPrefix(string prefix)
    {
        return _facts
            .Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SelectMany(pair => pair.Value)
            .ToArray();
    }
}

internal sealed record SourceFactContribution(
    string Key,
    string ProcessorId,
    object Value,
    SourceLocation? SourceLocation);
