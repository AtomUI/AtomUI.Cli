using System.Reflection;

namespace AtomUI.Cli.Hosting.Commands.BuiltIn;

internal sealed record CliProductInfo(string Name, string Version, string Copyright)
{
    public static CliProductInfo Current { get; } = Create(typeof(CliProductInfo).Assembly);

    private static CliProductInfo Create(Assembly assembly)
    {
        var name = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                   ?? assembly.GetName().Name
                   ?? "AtomUI Cli";
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly.GetName().Version?.ToString()
                      ?? "0.0.0";
        var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
                        ?? "Copyright (c) 2018-2026 Qinware Technologies Co., Ltd. All rights reserved.";

        return new CliProductInfo(name, version, copyright);
    }
}
