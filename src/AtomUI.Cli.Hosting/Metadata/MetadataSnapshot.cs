namespace AtomUI.Cli.Hosting.Metadata;

public sealed record MetadataCatalog(
    string SchemaVersion,
    string TargetVersion,
    IReadOnlyList<ProductDescriptor> Products,
    IReadOnlyList<PackageDescriptor> Packages,
    IReadOnlyList<ControlDescriptor> Controls,
    IReadOnlyList<TokenDescriptor> Tokens,
    IReadOnlyList<DemoDescriptor> Demos,
    IReadOnlyList<DocumentDescriptor> Documents,
    IReadOnlyList<SemanticPartDescriptor> SemanticParts,
    IReadOnlyList<ChangelogEntryDescriptor> Changelog)
{
    public static MetadataCatalog CreateDefault()
    {
        return new MetadataCatalog(
            "1.0",
            "1.0.0-alpha.1",
            [
                new ProductDescriptor("desktop", "AtomUI Desktop", "Public desktop controls."),
                new ProductDescriptor("datagrid", "AtomUI DataGrid", "Commercial data grid controls.")
            ],
            [
                new PackageDescriptor("AtomUI.Controls", "desktop", "1.0.0-alpha.1", "Core desktop controls.", ["Button"]),
                new PackageDescriptor("AtomUI.Controls.DataGrid", "datagrid", "1.0.0-alpha.1", "Data grid controls.", ["DataGrid"])
            ],
            [
                new ControlDescriptor("Button", "desktop", "AtomUI.Controls", "AtomUI.Controls", "Clickable command control.", "Input"),
                new ControlDescriptor("DataGrid", "datagrid", "AtomUI.Controls.DataGrid", "AtomUI.Controls.DataGrid", "Tabular data presentation control.", "Data")
            ],
            [
                new TokenDescriptor("colorPrimary", "global", null, "Color", "#1677ff", "Primary brand color.", false),
                new TokenDescriptor("buttonHeight", "control", "Button", "Double", "32", "Default Button height.", false),
                new TokenDescriptor("rowHoverBackground", "control", "DataGrid", "Color", "#f5f5f5", "DataGrid row hover background.", false)
            ],
            [
                new DemoDescriptor("Button", "basic", "Basic Button", "A basic clickable Button.", "<Button Content=\"Save\" />", "new Button { Content = \"Save\" };"),
                new DemoDescriptor("DataGrid", "basic", "Basic DataGrid", "A basic DataGrid.", "<DataGrid ItemsSource=\"{Binding Items}\" />", "new DataGrid();")
            ],
            [
                new DocumentDescriptor("control", "Button", "Button", "## Button\n\nUse Button for explicit user commands."),
                new DocumentDescriptor("control", "DataGrid", "DataGrid", "## DataGrid\n\nUse DataGrid for tabular data."),
                new DocumentDescriptor("topic", "design-language", "Design Language", "## AtomUI Design Language\n\nPrefer clear hierarchy and tokenized styling.")
            ],
            [
                new SemanticPartDescriptor("Button", "root", "Root visual element.", ["pointerover", "pressed"]),
                new SemanticPartDescriptor("Button", "contentPresenter", "Button content presenter.", []),
                new SemanticPartDescriptor("DataGrid", "rowsPresenter", "Rows presenter.", ["selected", "editing"])
            ],
            [
                new ChangelogEntryDescriptor("1.0.0-alpha.1", "control", "Button", "info", "Initial Button metadata."),
                new ChangelogEntryDescriptor("1.0.0-alpha.1", "package", "AtomUI.Controls.DataGrid", "info", "Initial DataGrid metadata.")
            ]);
    }
}

public sealed record ProductDescriptor(string Id, string Name, string Description);

public sealed record PackageDescriptor(
    string Id,
    string ProductId,
    string Version,
    string Description,
    IReadOnlyList<string> Controls);

public sealed record ControlDescriptor(
    string Name,
    string ProductId,
    string PackageId,
    string Namespace,
    string Description,
    string Category);

public sealed record TokenDescriptor(
    string Name,
    string Scope,
    string? ControlName,
    string Type,
    string DefaultValue,
    string Description,
    bool IsInherited);

public sealed record DemoDescriptor(
    string ControlName,
    string Name,
    string Title,
    string Description,
    string Xaml,
    string CSharp);

public sealed record DocumentDescriptor(
    string Kind,
    string TargetId,
    string Title,
    string Markdown);

public sealed record SemanticPartDescriptor(
    string ControlName,
    string Name,
    string Description,
    IReadOnlyList<string> PseudoClasses);

public sealed record ChangelogEntryDescriptor(
    string Version,
    string TargetKind,
    string TargetId,
    string Severity,
    string Message);
