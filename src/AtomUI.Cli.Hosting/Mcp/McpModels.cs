namespace AtomUI.Cli.Hosting.Mcp;

public sealed record McpToolDescriptor(
    string Name,
    string Description,
    string RelatedCommand,
    bool IsReadOnly);

public sealed record McpToolCatalog(IReadOnlyList<McpToolDescriptor> Tools)
{
    public static McpToolCatalog CreateDefault()
    {
        return new McpToolCatalog(
        [
            new McpToolDescriptor("atomui_list", "List AtomUI controls, products, and packages.", "list", true),
            new McpToolDescriptor("atomui_info", "Get AtomUI control information.", "info", true),
            new McpToolDescriptor("atomui_doc", "Get AtomUI documentation.", "doc", true),
            new McpToolDescriptor("atomui_demo", "Get AtomUI demos.", "demo", true),
            new McpToolDescriptor("atomui_token", "Get AtomUI tokens.", "token", true),
            new McpToolDescriptor("atomui_semantic", "Get AtomUI semantic parts.", "semantic", true),
            new McpToolDescriptor("atomui_package", "Get AtomUI package information.", "package", true),
            new McpToolDescriptor("atomui_changelog", "Get AtomUI changelog entries.", "changelog", true),
            new McpToolDescriptor("atomui_doctor", "Run read-only AtomUI diagnostics.", "doctor", true)
        ]);
    }

    public string RenderText()
    {
        return string.Join(Environment.NewLine, Tools.Select(tool => $"{tool.Name}: {tool.Description}"));
    }
}
