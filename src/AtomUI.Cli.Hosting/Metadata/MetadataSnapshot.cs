namespace AtomUI.Cli.Hosting.Metadata;

public sealed record MetadataCatalog(
    string SchemaVersion,
    string TargetVersion,
    IReadOnlyList<ProductDescriptor> Products,
    IReadOnlyList<PackageDescriptor> Packages,
    IReadOnlyList<ControlCategoryDescriptor> Categories,
    IReadOnlyList<ControlDescriptor> Controls,
    IReadOnlyList<TokenDescriptor> Tokens,
    IReadOnlyList<DemoDescriptor> Demos,
    IReadOnlyList<DocumentDescriptor> Documents,
    IReadOnlyList<SemanticPartDescriptor> SemanticParts,
    IReadOnlyList<ChangelogEntryDescriptor> Changelog)
{
    public static MetadataCatalog CreateDefault()
    {
        var categories = CreateCategories();
        var controls = CreateControls(categories);

        return new MetadataCatalog(
            "1.0",
            "1.0.0-alpha.1",
            [
                new ProductDescriptor("desktop", "AtomUI Desktop", "Public desktop controls."),
                new ProductDescriptor("colorpicker", "AtomUI ColorPicker", "Optional color picker controls."),
                new ProductDescriptor("datagrid", "AtomUI DataGrid", "Commercial data grid controls.")
            ],
            [
                new PackageDescriptor(
                    "AtomUI.Desktop.Controls",
                    "desktop",
                    "1.0.0-alpha.1",
                    "Core desktop controls.",
                    controls.Where(control => control.PackageId.Equals("AtomUI.Desktop.Controls", StringComparison.Ordinal)).Select(control => control.Name).ToArray(),
                    IsOptional: false,
                    IsCommercial: false),
                new PackageDescriptor(
                    "AtomUI.Desktop.Controls.ColorPicker",
                    "colorpicker",
                    "1.0.0-alpha.1",
                    "Optional color picker controls.",
                    ["ColorPicker"],
                    IsOptional: true,
                    IsCommercial: false),
                new PackageDescriptor(
                    "AtomUI.Desktop.Controls.DataGrid",
                    "datagrid",
                    "1.0.0-alpha.1",
                    "Commercial data grid controls.",
                    ["DataGrid"],
                    IsOptional: true,
                    IsCommercial: true)
            ],
            categories,
            controls,
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
                new ChangelogEntryDescriptor("1.0.0-alpha.1", "package", "AtomUI.Desktop.Controls.DataGrid", "info", "Initial DataGrid metadata.")
            ]);
    }

    private static ControlCategoryDescriptor[] CreateCategories()
    {
        return
        [
            new ControlCategoryDescriptor("general", "General", "General", 100),
            new ControlCategoryDescriptor("layout", "Layout", "Layout", 200),
            new ControlCategoryDescriptor("navigation", "Navigation", "Navigation", 300),
            new ControlCategoryDescriptor("data-entry", "Data Entry", "DataEntry", 400),
            new ControlCategoryDescriptor("data-display", "Data Display", "DataDisplay", 500),
            new ControlCategoryDescriptor("feedback", "Feedback", "Feedback", 600),
            new ControlCategoryDescriptor("other", "Other", "Other", 700)
        ];
    }

    private static ControlDescriptor[] CreateControls(IReadOnlyList<ControlCategoryDescriptor> categories)
    {
        return
        [
            Control(categories, "Button", "general", 30, "Command button."),
            Control(categories, "FloatButton", "general", 40, "Floating action button."),
            Control(categories, "SplitButton", "general", 50, "Split action button."),
            Control(categories, "Separator", "general", 60, "Visual separator."),

            Control(categories, "FlexPanel", "layout", 110, "Flexible layout panel."),
            Control(categories, "Grid", "layout", 120, "Grid layout panel."),
            Control(categories, "Space", "layout", 130, "Spacing layout container."),
            Control(categories, "Splitter", "layout", 140, "Draggable split layout."),
            Control(categories, "Masonry", "layout", 150, "Masonry layout container."),

            Control(categories, "Breadcrumb", "navigation", 210, "Breadcrumb navigation."),
            Control(categories, "ButtonSpinner", "navigation", 220, "Button-based spinner input."),
            Control(categories, "ComboBox", "navigation", 230, "Combined selection input."),
            Control(categories, "DropdownButton", "navigation", 240, "Dropdown action button."),
            Control(categories, "Menu", "navigation", 250, "Menu navigation."),
            Control(categories, "Pagination", "navigation", 260, "Pagination navigation."),
            Control(categories, "Steps", "navigation", 270, "Step navigation."),
            Control(categories, "TabControl", "navigation", 280, "Tabbed content control."),
            Control(categories, "TabStrip", "navigation", 290, "Tab strip navigation."),

            Control(categories, "AutoComplete", "data-entry", 310, "Autocomplete input."),
            Control(categories, "Cascader", "data-entry", 320, "Cascading selection input."),
            Control(categories, "CheckBox", "data-entry", 330, "Checkbox input."),
            Control(
                categories,
                "ColorPicker",
                "data-entry",
                340,
                "Color selection input.",
                "AtomUI.Desktop.Controls.ColorPicker",
                "colorpicker",
                isCommercial: false,
                isOptionalPackage: true),
            Control(categories, "DatePicker", "data-entry", 350, "Date selection input."),
            Control(categories, "TimePicker", "data-entry", 360, "Time selection input."),
            Control(categories, "Form", "data-entry", 370, "Form container and validation entry."),
            Control(categories, "LineEdit", "data-entry", 380, "Single-line text input."),
            Control(categories, "Mentions", "data-entry", 390, "Mention input."),
            Control(categories, "NumberUpDown", "data-entry", 400, "Numeric stepper input."),
            Control(categories, "RadioButton", "data-entry", 410, "Radio button input."),
            Control(categories, "Rate", "data-entry", 420, "Rating input."),
            Control(categories, "Select", "data-entry", 430, "Selection input."),
            Control(categories, "Slider", "data-entry", 440, "Slider input."),
            Control(categories, "ToggleSwitch", "data-entry", 450, "Toggle switch input."),
            Control(categories, "TreeSelect", "data-entry", 460, "Tree selection input."),
            Control(categories, "Transfer", "data-entry", 470, "Transfer selection control."),
            Control(categories, "Upload", "data-entry", 480, "Upload entry control."),

            Control(categories, "Avatar", "data-display", 510, "Avatar display."),
            Control(categories, "Badge", "data-display", 520, "Badge display."),
            Control(categories, "Calendar", "data-display", 530, "Calendar display."),
            Control(categories, "Card", "data-display", 540, "Card container."),
            Control(categories, "Carousel", "data-display", 550, "Carousel display."),
            Control(categories, "Collapse", "data-display", 560, "Collapsible panels."),
            Control(categories, "Descriptions", "data-display", 570, "Description list."),
            Control(
                categories,
                "DataGrid",
                "data-display",
                580,
                "Tabular data presentation control.",
                "AtomUI.Desktop.Controls.DataGrid",
                "datagrid",
                isCommercial: true,
                isOptionalPackage: true),
            Control(categories, "Expander", "data-display", 590, "Expandable content container."),
            Control(categories, "Empty", "data-display", 600, "Empty state display."),
            Control(categories, "ImagePreviewer", "data-display", 610, "Image preview control."),
            Control(categories, "GroupBox", "data-display", 620, "Grouped content container."),
            Control(categories, "InfoFlyout", "data-display", 630, "Information flyout."),
            Control(categories, "List", "data-display", 640, "List display."),
            Control(categories, "QRCode", "data-display", 650, "QR code display."),
            Control(categories, "Segmented", "data-display", 660, "Segmented display and selection control."),
            Control(categories, "Statistic", "data-display", 670, "Statistic display."),
            Control(categories, "Tag", "data-display", 680, "Tag display."),
            Control(categories, "Timeline", "data-display", 690, "Timeline display."),
            Control(categories, "TreeView", "data-display", 700, "Tree display."),
            Control(categories, "Tooltip", "data-display", 710, "Tooltip display."),
            Control(categories, "Tour", "data-display", 720, "Guided tour control."),

            Control(categories, "Alert", "feedback", 810, "Alert message."),
            Control(categories, "Drawer", "feedback", 820, "Drawer panel."),
            Control(categories, "Message", "feedback", 830, "Global message."),
            Control(categories, "Modal", "feedback", 840, "Modal dialog."),
            Control(categories, "Notification", "feedback", 850, "Notification message."),
            Control(categories, "PopupConfirm", "feedback", 860, "Popup confirmation."),
            Control(categories, "ProgressBar", "feedback", 870, "Progress bar."),
            Control(categories, "Result", "feedback", 880, "Result display."),
            Control(categories, "Skeleton", "feedback", 890, "Skeleton placeholder."),
            Control(categories, "Spin", "feedback", 900, "Loading spinner."),
            Control(categories, "Watermark", "feedback", 910, "Watermark display."),

            Control(categories, "BorderBeam", "other", 1010, "Border beam effect."),
            Control(categories, "Splash", "other", 1020, "Splash screen control.")
        ];
    }

    private static ControlDescriptor Control(
        IReadOnlyList<ControlCategoryDescriptor> categories,
        string name,
        string categoryId,
        int displayOrder,
        string description,
        string packageId = "AtomUI.Desktop.Controls",
        string productId = "desktop",
        bool isCommercial = false,
        bool isOptionalPackage = false,
        bool isHidden = false)
    {
        var category = categories.First(item => item.Id.Equals(categoryId, StringComparison.Ordinal));
        return new ControlDescriptor(
            name,
            name,
            productId,
            packageId,
            "AtomUI.Desktop.Controls",
            $"{category.RouteSegment}/{name}",
            description,
            category.Id,
            category.Name,
            displayOrder,
            isCommercial,
            isOptionalPackage,
            isHidden);
    }
}

public sealed record ProductDescriptor(string Id, string Name, string Description);

public sealed record PackageDescriptor(
    string Id,
    string ProductId,
    string Version,
    string Description,
    IReadOnlyList<string> Controls,
    bool IsOptional,
    bool IsCommercial);

public sealed record ControlCategoryDescriptor(
    string Id,
    string Name,
    string RouteSegment,
    int DisplayOrder);

public sealed record ControlDescriptor(
    string Name,
    string DisplayName,
    string ProductId,
    string PackageId,
    string Namespace,
    string GalleryRoute,
    string Description,
    string CategoryId,
    string CategoryName,
    int DisplayOrder,
    bool IsCommercial,
    bool IsOptionalPackage,
    bool IsHidden)
{
    public string Category => CategoryName;
}

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
