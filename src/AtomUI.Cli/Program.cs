using AtomUI.Cli.Hosting;

namespace AtomUI.Cli.Entry;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        await using var application = AtomUICliApplication
            .CreateBuilder(args)
            .Build();

        return await application.RunAsync(args);
    }
}
