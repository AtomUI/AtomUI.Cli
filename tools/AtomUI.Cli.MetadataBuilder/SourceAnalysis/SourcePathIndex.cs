namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis;

internal sealed class SourcePathIndex(string sourceRoot)
{
    public string SourceRoot { get; } = Path.GetFullPath(sourceRoot);

    public string GetFullPath(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(SourceRoot, relativePath));
    }

    public string GetRelativePath(string fullPath)
    {
        return Path.GetRelativePath(SourceRoot, fullPath).Replace(Path.DirectorySeparatorChar, '/');
    }
}
