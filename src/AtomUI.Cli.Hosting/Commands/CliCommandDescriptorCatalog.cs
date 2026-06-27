namespace AtomUI.Cli.Hosting.Commands;

public sealed class CliCommandDescriptorCatalog
{
    private readonly Dictionary<string, CliCommandDescriptor> _descriptorsByName;

    private CliCommandDescriptorCatalog(IReadOnlyList<CliCommandDescriptor> descriptors)
    {
        Descriptors = descriptors;
        _descriptorsByName = descriptors.ToDictionary(
            descriptor => descriptor.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    public static CliCommandDescriptorCatalog Empty { get; } = new(Array.Empty<CliCommandDescriptor>());

    public IReadOnlyList<CliCommandDescriptor> Descriptors { get; }

    public static CliCommandDescriptorCatalog Create(IEnumerable<CliCommandDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        var descriptorArray = descriptors.ToArray();

        var duplicate = descriptorArray
            .GroupBy(descriptor => descriptor.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new ArgumentException($"Command '{duplicate.Key}' is registered more than once.", nameof(descriptors));
        }

        return new CliCommandDescriptorCatalog(Array.AsReadOnly(descriptorArray));
    }

    public bool TryFind(string name, out CliCommandDescriptor? descriptor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _descriptorsByName.TryGetValue(name, out descriptor);
    }
}
