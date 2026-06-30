namespace AtomUI.Cli.MetadataBuilder;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = BuilderOptions.Parse(args);
            var extractor = new TokenSourceExtractor();
            var snapshot = extractor.Extract(options.SourceRoot, options.TargetVersion);
            TokenSnapshotCodeWriter.Write(snapshot, options.OutputPath);
            Console.WriteLine($"Generated AtomUI token snapshot: {snapshot.ControlTokenSets.Count} control token sets, {snapshot.SharedTokens.Count} shared tokens.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"AtomUI token snapshot generation failed: {exception.Message}");
            return 1;
        }
    }
}
