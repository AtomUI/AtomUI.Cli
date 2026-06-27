using System.Diagnostics.CodeAnalysis;
using AtomUI.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace AtomUI.Cli.Hosting.DependencyInjection;

internal static class ModuleServiceDescriptorMapper
{
    public static void AddModuleServices(
        IServiceCollection services,
        IEnumerable<ModuleServiceDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(descriptors);

        foreach (var descriptor in descriptors)
        {
            services.Add(ToServiceDescriptor(descriptor));
        }
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072",
        Justification = "Module service implementation types come from explicit AtomUI module registrations and are the only controlled DI activation boundary.")]
    private static ServiceDescriptor ToServiceDescriptor(ModuleServiceDescriptor descriptor)
    {
        if (descriptor.Instance is not null)
        {
            return new ServiceDescriptor(descriptor.ServiceType, descriptor.Instance);
        }

        var lifetime = descriptor.Lifetime switch
        {
            ModuleServiceLifetime.Singleton => ServiceLifetime.Singleton,
            ModuleServiceLifetime.Scoped => ServiceLifetime.Scoped,
            ModuleServiceLifetime.Transient => ServiceLifetime.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.Lifetime, "Unsupported module service lifetime.")
        };

        return new ServiceDescriptor(descriptor.ServiceType, descriptor.ImplementationType!, lifetime);
    }
}
