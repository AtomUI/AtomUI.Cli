namespace AtomUI.Cli.Hosting.Commands;

public sealed class ModuleActivationPlanner(Type coreModuleType)
{
    public ModuleActivationPlan Plan(CommandManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var moduleTypes = new List<Type>
        {
            coreModuleType,
            manifest.OwnerModuleType
        };
        moduleTypes.AddRange(manifest.RequiredModules);

        return new ModuleActivationPlan(Array.AsReadOnly(moduleTypes.Distinct().ToArray()));
    }
}

public sealed record ModuleActivationPlan(IReadOnlyList<Type> ModuleTypes);
