namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis;

internal sealed class SourceAnalysisProcessorRegistry
{
    private readonly List<RegisteredProcessor> _processors = [];
    private readonly Dictionary<string, RegisteredProcessor> _byId = new(StringComparer.Ordinal);

    public void Add(ISourceAnalysisProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(processor);
        if (string.IsNullOrWhiteSpace(processor.Id))
        {
            throw new InvalidOperationException("Source analysis processor id cannot be empty.");
        }

        if (_byId.ContainsKey(processor.Id))
        {
            throw new InvalidOperationException($"Duplicate source analysis processor id '{processor.Id}'.");
        }

        var registered = new RegisteredProcessor(processor, _processors.Count);
        _processors.Add(registered);
        _byId.Add(processor.Id, registered);
    }

    public SourceAnalysisPlan CreatePlan(IReadOnlySet<SourceAnalysisFeature> requiredFeatures)
    {
        ArgumentNullException.ThrowIfNull(requiredFeatures);
        var selected = new Dictionary<string, RegisteredProcessor>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);

        foreach (var feature in requiredFeatures)
        {
            IncludeFeature(feature, selected, visiting);
        }

        var ordered = selected.Values
            .OrderBy(item => item.Processor.Phase)
            .ThenBy(item => item.Order)
            .Select(item => item.Processor)
            .ToArray();
        return new SourceAnalysisPlan(ordered);
    }

    private void IncludeFeature(
        SourceAnalysisFeature feature,
        Dictionary<string, RegisteredProcessor> selected,
        HashSet<string> visiting)
    {
        if (selected.Values.Any(item => item.Processor.Provides.Contains(feature)))
        {
            return;
        }

        var provider = _processors.FirstOrDefault(item => item.Processor.Provides.Contains(feature));
        if (provider is null)
        {
            throw new InvalidOperationException($"No source analysis processor provides feature '{feature}'.");
        }

        IncludeProcessor(provider, selected, visiting);
    }

    private void IncludeProcessor(
        RegisteredProcessor processor,
        Dictionary<string, RegisteredProcessor> selected,
        HashSet<string> visiting)
    {
        if (selected.ContainsKey(processor.Processor.Id))
        {
            return;
        }

        if (!visiting.Add(processor.Processor.Id))
        {
            throw new InvalidOperationException($"Circular source analysis processor dependency at '{processor.Processor.Id}'.");
        }

        foreach (var requiredFeature in processor.Processor.Requires)
        {
            IncludeFeature(requiredFeature, selected, visiting);
        }

        visiting.Remove(processor.Processor.Id);
        selected.Add(processor.Processor.Id, processor);
    }

    private sealed record RegisteredProcessor(ISourceAnalysisProcessor Processor, int Order);
}
