using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;

internal sealed class SemanticContractProcessor : ISourceAnalysisProcessor
{
    public string Id => "semantic-contract";

    public IReadOnlySet<SourceAnalysisFeature> Provides { get; } =
        new HashSet<SourceAnalysisFeature> { SourceAnalysisFeature.SemanticContract };

    public IReadOnlySet<SourceAnalysisFeature> Requires { get; } =
        new HashSet<SourceAnalysisFeature>
        {
            SourceAnalysisFeature.PseudoClass,
            SourceAnalysisFeature.ControlTheme,
            SourceAnalysisFeature.ThemeVisualTree,
            SourceAnalysisFeature.ThemeSelector,
            SourceAnalysisFeature.TemplateBinding
        };

    public SourceAnalysisPhase Phase => SourceAnalysisPhase.AssembleContracts;

    public ValueTask ExecuteAsync(SourceAnalysisContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var nodes = context.Facts.GetByPrefix("theme-node:")
            .Select(fact => fact.Value)
            .OfType<ThemeNodeFact>()
            .GroupBy(node => (node.ControlName, node.Name), StringTupleComparer.Instance);
        var bindings = context.Facts.GetByPrefix("template-binding:")
            .Select(fact => fact.Value)
            .OfType<TemplateBindingFact>()
            .ToArray();
        var selectors = context.Facts.GetByPrefix("theme-selector:")
            .Select(fact => fact.Value)
            .OfType<ThemeSelectorFact>()
            .ToArray();
        var stateFlows = context.Facts.GetByPrefix("state-flow:")
            .Select(fact => fact.Value)
            .OfType<StateFlowFact>()
            .ToArray();
        var pseudoClasses = context.Facts.GetByPrefix("pseudo-class:")
            .Select(fact => fact.Value)
            .OfType<PseudoClassFact>()
            .ToArray();

        foreach (var group in nodes)
        {
            var first = group.First();
            var partBindings = bindings
                .Where(binding => binding.ControlName.Equals(first.ControlName, StringComparison.Ordinal)
                                  && binding.PartName is not null
                                  && binding.PartName.Equals(first.Name, StringComparison.Ordinal))
                .Select(binding => binding.BoundApi);
            var partSelectors = selectors
                .Where(selector => selector.ControlName.Equals(first.ControlName, StringComparison.Ordinal)
                                   && selector.TargetParts.Contains(first.Name, StringComparer.Ordinal))
                .ToArray();
            var selectorPseudoClasses = partSelectors
                .SelectMany(selector => selector.PseudoClasses);
            var inferredStateFlows = InferStateFlowsForPart(first, stateFlows);
            var boundApis = partBindings
                .Concat(inferredStateFlows.SelectMany(flow => flow.ReferencedApis))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var partPseudoClasses = selectorPseudoClasses
                .Concat(inferredStateFlows.Select(flow => flow.PseudoClass))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            context.Facts.Add(
                $"semantic-part:{first.ControlName}:{first.Name}",
                Id,
                new SemanticPartFact(
                    first.ControlName,
                    first.Name,
                    first.NodeType,
                    CreatePartDescription(first, boundApis, partPseudoClasses),
                    boundApis,
                    partPseudoClasses,
                    [],
                    first.SourceLocation),
                first.SourceLocation);
        }

        foreach (var group in pseudoClasses.GroupBy(item => (item.ControlName, item.Name), StringTupleComparer.Instance))
        {
            var first = group.First();
            var flows = stateFlows
                .Where(flow => flow.ControlName.Equals(first.ControlName, StringComparison.Ordinal)
                               && flow.PseudoClass.Equals(first.Name, StringComparison.Ordinal))
                .ToArray();
            var matchedSelectors = selectors
                .Where(selector => selector.ControlName.Equals(first.ControlName, StringComparison.Ordinal)
                                   && selector.PseudoClasses.Contains(first.Name, StringComparer.Ordinal))
                .Select(selector => selector.Selector)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var boundApis = flows
                .SelectMany(flow => flow.ReferencedApis)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            context.Facts.Add(
                $"semantic-pseudo-class:{first.ControlName}:{first.Name}",
                Id,
                new SemanticPseudoClassFact(
                    first.ControlName,
                    first.Name,
                    CreatePseudoClassDescription(first.Name, flows),
                    boundApis,
                    matchedSelectors,
                    first.SourceLocation),
                first.SourceLocation);
        }

        return ValueTask.CompletedTask;
    }

    private static IReadOnlyList<StateFlowFact> InferStateFlowsForPart(
        ThemeNodeFact part,
        IReadOnlyList<StateFlowFact> stateFlows)
    {
        if (!part.Name.Contains("Loading", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return stateFlows
            .Where(flow => flow.ControlName.Equals(part.ControlName, StringComparison.Ordinal)
                           && flow.PseudoClass.Equals(":loading", StringComparison.Ordinal))
            .ToArray();
    }

    private static string CreatePartDescription(
        ThemeNodeFact part,
        IReadOnlyList<string> boundApis,
        IReadOnlyList<string> pseudoClasses)
    {
        if (pseudoClasses.Contains(":loading", StringComparer.Ordinal))
        {
            return $"{part.Name} is a {part.NodeType} template part controlled by {string.Join(", ", boundApis)} and {string.Join(", ", pseudoClasses)}.";
        }

        if (boundApis.Count > 0)
        {
            return $"{part.Name} is a {part.NodeType} template part bound to {string.Join(", ", boundApis)}.";
        }

        return $"{part.Name} is a {part.NodeType} template part extracted from {part.ThemeName}.";
    }

    private static string CreatePseudoClassDescription(string pseudoClass, IReadOnlyList<StateFlowFact> flows)
    {
        if (flows.Count == 0)
        {
            return $"{pseudoClass} is declared on the control.";
        }

        var apis = flows
            .SelectMany(flow => flow.ReferencedApis)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        return $"{pseudoClass} is applied when {string.Join(" or ", flows.Select(flow => flow.Condition).Distinct(StringComparer.Ordinal))}; related API: {string.Join(", ", apis)}.";
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string First, string Second)>
    {
        public static readonly StringTupleComparer Instance = new();

        public bool Equals((string First, string Second) x, (string First, string Second) y)
        {
            return string.Equals(x.First, y.First, StringComparison.Ordinal)
                   && string.Equals(x.Second, y.Second, StringComparison.Ordinal);
        }

        public int GetHashCode((string First, string Second) obj)
        {
            return HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(obj.First),
                StringComparer.Ordinal.GetHashCode(obj.Second));
        }
    }
}
