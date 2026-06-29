namespace AtomUI.Cli.Hosting.Metadata;

public sealed class MetadataQueryService(MetadataCatalog catalog)
{
    public MetadataCatalog Catalog => catalog;

    public IReadOnlyList<ControlDescriptor> ListControls(
        string? productId = null,
        string? category = null,
        string? packageId = null,
        bool includeHidden = false)
    {
        var normalizedCategory = NormalizeFilter(category);
        return catalog.Controls
            .Where(control => Matches(control.ProductId, productId)
                              && Matches(control.PackageId, packageId)
                              && (includeHidden || !control.IsHidden)
                              && (string.IsNullOrWhiteSpace(normalizedCategory) || MatchesCategory(control, normalizedCategory)))
            .OrderBy(control => GetCategoryOrder(control.CategoryId))
            .ThenBy(control => control.DisplayOrder)
            .ToArray();
    }

    public IReadOnlyList<ControlCategoryDescriptor> ListCategories(string? productId = null, bool includeHidden = false)
    {
        var categoryIds = ListControls(productId, includeHidden: includeHidden)
            .Select(control => control.CategoryId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return catalog.Categories
            .Where(category => categoryIds.Contains(category.Id))
            .OrderBy(category => category.DisplayOrder)
            .ToArray();
    }

    public IReadOnlyList<ProductDescriptor> ListProducts()
    {
        return catalog.Products.ToArray();
    }

    public IReadOnlyList<PackageDescriptor> ListPackages(string? productId = null)
    {
        return catalog.Packages
            .Where(package => Matches(package.ProductId, productId))
            .ToArray();
    }

    public ProductDescriptor? FindProduct(string productId)
    {
        return catalog.Products.FirstOrDefault(product => product.Id.Equals(productId, StringComparison.OrdinalIgnoreCase));
    }

    public PackageDescriptor? FindPackage(string packageId)
    {
        return catalog.Packages.FirstOrDefault(package => package.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase));
    }

    public ControlDescriptor? FindControl(string name, string? productId = null)
    {
        return catalog.Controls.FirstOrDefault(control =>
            control.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            && Matches(control.ProductId, productId));
    }

    public PackageDescriptor? FindPackageOrProduct(string value)
    {
        return catalog.Packages.FirstOrDefault(package => package.Id.Equals(value, StringComparison.OrdinalIgnoreCase))
               ?? catalog.Products
                   .Select(product => catalog.Packages.FirstOrDefault(package => package.ProductId.Equals(product.Id, StringComparison.OrdinalIgnoreCase)))
                   .FirstOrDefault(package => package is not null && package.ProductId.Equals(value, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<TokenDescriptor> FindTokens(string? controlName, string? name = null, string? match = null)
    {
        return catalog.Tokens
            .Where(token => controlName is null
                ? token.Scope.Equals("global", StringComparison.OrdinalIgnoreCase)
                : token.ControlName?.Equals(controlName, StringComparison.OrdinalIgnoreCase) == true)
            .Where(token => string.IsNullOrWhiteSpace(name) || token.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .Where(token => string.IsNullOrWhiteSpace(match)
                            || token.Name.Contains(match, StringComparison.OrdinalIgnoreCase)
                            || token.Description.Contains(match, StringComparison.OrdinalIgnoreCase))
            .OrderBy(token => token.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<DemoDescriptor> FindDemos(string controlName, string? demoName = null)
    {
        return catalog.Demos
            .Where(demo => demo.ControlName.Equals(controlName, StringComparison.OrdinalIgnoreCase))
            .Where(demo => string.IsNullOrWhiteSpace(demoName) || demo.Name.Equals(demoName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(demo => demo.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public DocumentDescriptor? FindDocument(string kind, string targetId)
    {
        return catalog.Documents.FirstOrDefault(document =>
            document.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase)
            && document.TargetId.Equals(targetId, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<SemanticPartDescriptor> FindSemanticParts(string controlName, string? part = null)
    {
        return catalog.SemanticParts
            .Where(item => item.ControlName.Equals(controlName, StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(part) || item.Name.Equals(part, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<ChangelogEntryDescriptor> FindChangelog(string? targetId = null)
    {
        return catalog.Changelog
            .Where(item => string.IsNullOrWhiteSpace(targetId) || item.TargetId.Equals(targetId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Version, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.TargetId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string RenderSummary()
    {
        return $"AtomUI metadata {catalog.TargetVersion}: {catalog.Controls.Count} controls, {catalog.Packages.Count} packages.";
    }

    public InfoCommandPayload CreateInfoPayload(ControlDescriptor control, InfoCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(options);

        var targetVersion = options.Global.TargetVersion ?? catalog.TargetVersion;
        if (control.Name.Equals("Button", StringComparison.OrdinalIgnoreCase))
        {
            return CreateButtonInfoPayload(control, targetVersion);
        }

        return control.Name.Equals("DataGrid", StringComparison.OrdinalIgnoreCase)
            ? CreateDataGridInfoPayload(control, targetVersion)
            : CreateFallbackInfoPayload(control, targetVersion);
    }

    public ListCommandPayload CreateListPayload(ListCommandOptions options)
    {
        var controls = options.Kind is ListKind.Products or ListKind.Packages
            ? Array.Empty<ControlDescriptor>()
            : ListControls(options.Global.Product, options.Category, options.PackageId, options.IncludeHidden);
        var products = options.Kind is ListKind.Controls or ListKind.Categories
            ? Array.Empty<ProductDescriptor>()
            : ListProducts().Where(product => Matches(product.Id, options.Global.Product)).ToArray();
        var packages = options.Kind is ListKind.Controls or ListKind.Categories or ListKind.Products
            ? Array.Empty<PackageDescriptor>()
            : ListPackages(options.Global.Product);
        var categories = CreateCategoryPayloads(controls);

        return new ListCommandPayload(
            "1.0",
            catalog.TargetVersion,
            options.Kind,
            options.Global.Product,
            options.Category,
            options.PackageId,
            categories,
            products.Select(ListProductPayload.FromDescriptor).ToArray(),
            packages.Select(ListPackagePayload.FromDescriptor).ToArray(),
            []);
    }

    private InfoCommandPayload CreateButtonInfoPayload(ControlDescriptor control, string targetVersion)
    {
        var themeFile = "AtomUI.Desktop.Controls/Buttons/Themes/ButtonTheme.axaml";
        return new InfoCommandPayload(
            "1.0",
            "info",
            targetVersion,
            CreateIdentity(control),
            new InfoControlTypePayload(
                control.Namespace,
                control.PackageId,
                $"{control.Namespace}.Button",
                "Avalonia.Controls.Button",
                [
                    "ICustomizableSizeTypeAware",
                    "IWaveSpiritAwareControl",
                    "ICompactSpaceAware",
                    "IFormItemAware"
                ],
                "https://atomui.net",
                "atom"),
            new InfoControlDescriptionPayload(
                "Trigger actions and express intent with clear visual priority.",
                "Button is the primary action control in AtomUI. Use type, shape, size, icon and loading states to build predictable workflows.",
                "Use Button for explicit user commands. Prefer one primary action per region and use loading, disabled, danger and icon states to communicate intent."),
            new InfoControlUsagePayload(
                [control.PackageId],
                [control.Namespace],
                "<atom:Button ButtonType=\"Primary\" Content=\"Save\" />",
                "new Button { Content = \"Save\", ButtonType = ButtonType.Primary };"),
            [
                Api("ButtonType", "ButtonType", "Default", "Sets the visual button type, such as primary, default, dashed, text, or link."),
                Api("SizeType", "CustomizableSizeType", "Middle", "Controls button height and padding density."),
                Api("Shape", "ButtonShape", "Default", "Changes button shape between default, round, and circle."),
                Api("Icon", "Icon?", "null", "Displays an icon before or after the content, or as an icon-only button."),
                Api("IconPlacement", "ButtonIconPlacement", "Start", "Sets whether the icon appears at the start or end of the content."),
                Api("IsLoading", "bool", "false", "Shows a loading indicator and communicates that the action is in progress."),
                Api("IsDanger", "bool", "false", "Applies danger styling for destructive or high-risk actions."),
                Api("Color", "ButtonColor?", "null", "Sets the semantic color used by the Color and Variant model."),
                Api("Variant", "ButtonVariant?", "null", "Sets the visual variant, such as solid, outlined, dashed, filled, text, or link."),
                Api("CustomBackground", "IBrush?", "null", "Sets a controlled custom normal-state background overlay for solid non-danger buttons.")
            ],
            new InfoTemplateContractPayload(
                [themeFile],
                [
                    TemplatePart("PART_WaveSpirit", "WaveSpiritDecorator", themeFile, "Hosts the wave feedback visual."),
                    TemplatePart("PART_RootLayout", "DockPanel", themeFile, "Arranges loading icon, leading or trailing icon, and content."),
                    TemplatePart("PART_LoadingIcon", "LoadingOutlined", themeFile, "Displays the loading animation when IsLoading is true."),
                    TemplatePart("PART_ButtonIcon", "IconPresenter", themeFile, "Renders the configured icon."),
                    TemplatePart("PART_ContentPresenter", "ContentPresenter", themeFile, "Renders Button content.")
                ],
                ["Custom themes should keep the PART names stable so control logic and visual states continue to work."]),
            [
                State(":icononly", "pseudo-class", "Applied when the button renders as an icon-only action.", "Button"),
                State(":loading", "pseudo-class", "Applied while the loading indicator is visible.", "Button"),
                State(":danger", "pseudo-class", "Applied for destructive or high-risk actions.", "Button"),
                State(":default", "pseudo-class", "Applied for the default visual type.", "Button"),
                State(":dashed", "pseudo-class", "Applied for dashed visual type.", "Button"),
                State(":primary", "pseudo-class", "Applied for primary visual type.", "Button"),
                State(":link", "pseudo-class", "Applied for link visual type.", "Button"),
                State(":text", "pseudo-class", "Applied for text visual type.", "Button")
            ],
            [
                Token("ColorPrimary", "shared", "Color", "stable", null, "SharedTokenResource ColorPrimary", "Primary color used by primary buttons and focused interaction states."),
                Token("ControlHeight", "shared", "Double", "stable", null, "SharedTokenResource ControlHeight", "Shared control height scale used by large, middle and small buttons."),
                Token("ButtonToken", "control", "Token", "mapped", null, "ButtonTokenResource", "Control token mapping that derives Button visual values from shared tokens.")
            ],
            [
                Demo("basic", "Button type", "Use button type to express action priority in the current region.", $"{NormalizeGalleryRoute(control.GalleryRoute)}/basic", ["api"]),
                Demo("shape-icon", "Shape and icon", "Round, circle and icon buttons keep compact actions easy to recognize.", $"{NormalizeGalleryRoute(control.GalleryRoute)}/shape-icon", ["icon"]),
                Demo("size", "Size", "Switch between large, middle and small sizes to match density requirements.", $"{NormalizeGalleryRoute(control.GalleryRoute)}/size", ["layout"]),
                Demo("state", "State", "Loading, disabled, danger and ghost states communicate availability and risk.", $"{NormalizeGalleryRoute(control.GalleryRoute)}/state", ["state"])
            ],
            CreateRelatedCommands(control.Name),
            CreateDiagnostics(control));
    }

    private InfoCommandPayload CreateDataGridInfoPayload(ControlDescriptor control, string targetVersion)
    {
        var themeFile = "AtomUI.Desktop.Controls.DataGrid/Themes/DataGridTheme.axaml";
        return new InfoCommandPayload(
            "1.0",
            "info",
            targetVersion,
            CreateIdentity(control),
            new InfoControlTypePayload(
                control.Namespace,
                control.PackageId,
                $"{control.Namespace}.DataGrid",
                "Avalonia.Controls.DataGrid",
                [],
                "https://atomui.net",
                "atom"),
            new InfoControlDescriptionPayload(
                "Display, operate, and navigate tabular data.",
                "DataGrid is the AtomUI commercial table control for tabular data, selection, sorting, filtering, frozen columns, row details, pagination, and operation states.",
                "Use DataGrid for dense tabular workflows. Keep item models stable, define columns explicitly for production views, and enable sorting, filtering, resizing, or pagination only when the workflow needs them."),
            new InfoControlUsagePayload(
                [control.PackageId],
                [control.Namespace],
                "<atom:DataGrid ItemsSource=\"{Binding Items}\" />",
                "new DataGrid { ItemsSource = Items };"),
            [
                Api("ItemsSource", "IEnumerable?", "null", "Provides the row data source.", "DataGrid"),
                Api("AutoGenerateColumns", "bool", "false", "Controls whether columns are generated from the item model.", "DataGrid"),
                Api("Columns", "DataGridColumnCollection", "[]", "Defines explicit grid columns.", "DataGrid"),
                Api("ColumnGroups", "DataGridColumnGroupCollection", "[]", "Defines grouped column headers.", "DataGrid"),
                Api("SelectionMode", "DataGridSelectionMode", "Single", "Controls single or multiple row selection.", "DataGrid"),
                Api("SelectTriggerType", "DataGridSelectTriggerType", "Row", "Controls how selection is triggered.", "DataGrid"),
                Api("CanUserSortColumns", "bool", "false", "Allows users to sort columns.", "DataGrid"),
                Api("CanUserFilterColumns", "bool", "false", "Allows users to filter columns.", "DataGrid"),
                Api("CanUserResizeColumns", "bool", "false", "Allows users to resize columns.", "DataGrid"),
                Api("CanUserReorderColumns", "bool", "false", "Allows users to reorder columns.", "DataGrid"),
                Api("CanUserReorderRows", "bool", "false", "Allows users to reorder rows.", "DataGrid"),
                Api("LeftFrozenColumnCount", "int", "0", "Keeps columns frozen on the left side.", "DataGrid"),
                Api("RightFrozenColumnCount", "int", "0", "Keeps columns frozen on the right side.", "DataGrid"),
                Api("RowDetailsTemplate", "IDataTemplate?", "null", "Displays expanded row details.", "DataGrid"),
                Api("PaginationVisibility", "DataGridPaginationVisibility", "Bottom", "Controls pagination placement.", "DataGrid"),
                Api("PageSize", "int", "10", "Sets the default page size.", "DataGrid"),
                Api("IsOperating", "bool", "false", "Shows operation progress state.", "DataGrid"),
                Api("GridLinesVisibility", "DataGridGridLinesVisibility", "None", "Controls grid line rendering.", "DataGrid"),
                Api("IsReadOnly", "bool", "false", "Prevents editing when true.", "DataGrid")
            ],
            new InfoTemplateContractPayload(
                [themeFile],
                [
                    TemplatePart("PART_RowsPresenter", "DataGridRowsPresenter", themeFile, "Hosts the visible row collection."),
                    TemplatePart("PART_ColumnHeadersPresenter", "DataGridColumnHeadersPresenter", themeFile, "Hosts column headers."),
                    TemplatePart("PART_Pagination", "Pagination", themeFile, "Hosts pagination controls when enabled.")
                ],
                ["Commercial DataGrid themes should keep row, header, and pagination parts stable for virtualization and interaction behavior."]),
            [
                State(":operating", "pseudo-class", "Applied while grid operations are in progress.", "DataGrid"),
                State(":readonly", "pseudo-class", "Applied when the grid is read-only.", "DataGrid"),
                State(":selected", "interaction-state", "Applied to selected rows or cells.", "DataGrid"),
                State(":editing", "interaction-state", "Applied to editing cells.", "DataGrid")
            ],
            [
                Token("HeaderBg", "control", "Color", "stable", null, "DataGridTokenResource HeaderBg", "Header background color."),
                Token("HeaderColor", "control", "Color", "stable", null, "DataGridTokenResource HeaderColor", "Header foreground color."),
                Token("RowHoverBg", "control", "Color", "stable", null, "DataGridTokenResource RowHoverBg", "Row hover background color."),
                Token("RowSelectedBg", "control", "Color", "stable", null, "DataGridTokenResource RowSelectedBg", "Selected row background color."),
                Token("CellPadding", "control", "Thickness", "stable", null, "DataGridTokenResource CellPadding", "Default cell padding."),
                Token("BorderColor", "control", "Color", "stable", null, "DataGridTokenResource BorderColor", "Grid border color."),
                Token("PaginationMargin", "control", "Thickness", "stable", null, "DataGridTokenResource PaginationMargin", "Pagination layout margin.")
            ],
            [
                Demo("basic", "Basic DataGrid", "Display a simple item collection.", $"{NormalizeGalleryRoute(control.GalleryRoute)}/basic", ["data"]),
                Demo("columns", "Columns", "Define explicit columns and column groups.", $"{NormalizeGalleryRoute(control.GalleryRoute)}/columns", ["columns"]),
                Demo("selection", "Selection", "Configure row selection workflows.", $"{NormalizeGalleryRoute(control.GalleryRoute)}/selection", ["selection"]),
                Demo("pagination", "Pagination", "Navigate tabular data by pages.", $"{NormalizeGalleryRoute(control.GalleryRoute)}/pagination", ["pagination"])
            ],
            CreateRelatedCommands(control.Name),
            CreateDiagnostics(control));
    }

    private InfoCommandPayload CreateFallbackInfoPayload(ControlDescriptor control, string targetVersion)
    {
        var demos = FindDemos(control.Name)
            .Select(demo => new InfoDemoSummaryPayload(
                demo.Name,
                demo.Title,
                demo.Description,
                $"{NormalizeGalleryRoute(control.GalleryRoute)}/{demo.Name}",
                []))
            .ToArray();
        var tokens = FindTokens(control.Name)
            .Select(token => new InfoTokenSummaryPayload(
                token.Name,
                token.Scope.Equals("global", StringComparison.OrdinalIgnoreCase) ? "shared" : "control",
                token.Type,
                token.IsInherited ? "inherited" : "stable",
                token.DefaultValue,
                null,
                token.Description))
            .ToArray();

        return new InfoCommandPayload(
            "1.0",
            "info",
            targetVersion,
            CreateIdentity(control),
            new InfoControlTypePayload(
                control.Namespace,
                control.PackageId,
                $"{control.Namespace}.{control.Name}",
                "Avalonia.Controls.Control",
                [],
                "https://atomui.net",
                "atom"),
            new InfoControlDescriptionPayload(
                control.Description,
                control.Description,
                $"Use {control.Name} from {control.PackageId}. Run the related commands for examples, tokens, and semantic details."),
            new InfoControlUsagePayload(
                [control.PackageId],
                [control.Namespace],
                $"<atom:{control.Name} />",
                $"new {control.Name}();"),
            [],
            new InfoTemplateContractPayload([], [], []),
            [],
            tokens,
            demos,
            CreateRelatedCommands(control.Name),
            CreateDiagnostics(control));
    }

    private static InfoControlIdentityPayload CreateIdentity(ControlDescriptor control)
    {
        return new InfoControlIdentityPayload(
            control.Name,
            control.DisplayName,
            control.CategoryId,
            control.CategoryName,
            control.ProductId,
            control.PackageId,
            control.IsCommercial,
            control.IsOptionalPackage,
            NormalizeGalleryRoute(control.GalleryRoute),
            "Stable");
    }

    private static InfoApiMemberPayload Api(
        string name,
        string type,
        string defaultValue,
        string description,
        string ownerType = "Button")
    {
        return new InfoApiMemberPayload(
            name,
            "property",
            type,
            defaultValue,
            "styled",
            ownerType,
            IsBindable: true,
            IsInherited: false,
            IsCurated: true,
            IsDeprecated: false,
            Since: null,
            Replacement: null,
            description);
    }

    private static InfoTemplatePartPayload TemplatePart(string name, string type, string source, string description)
    {
        return new InfoTemplatePartPayload(name, type, IsRequired: true, source, description);
    }

    private static InfoControlStatePayload State(string name, string kind, string description, string source)
    {
        return new InfoControlStatePayload(name, kind, description, source);
    }

    private static InfoTokenSummaryPayload Token(
        string name,
        string scope,
        string type,
        string status,
        string? defaultValue,
        string? resourceKey,
        string description)
    {
        return new InfoTokenSummaryPayload(name, scope, type, status, defaultValue, resourceKey, description);
    }

    private static InfoDemoSummaryPayload Demo(
        string name,
        string title,
        string description,
        string route,
        IReadOnlyList<string> tags)
    {
        return new InfoDemoSummaryPayload(name, title, description, route, tags);
    }

    private static IReadOnlyList<InfoRelatedCommandPayload> CreateRelatedCommands(string controlName)
    {
        return
        [
            new InfoRelatedCommandPayload($"dotnet atomui demo {controlName}", "Show Gallery demos for this control."),
            new InfoRelatedCommandPayload($"dotnet atomui token {controlName}", "Show full token metadata for this control."),
            new InfoRelatedCommandPayload($"dotnet atomui semantic {controlName}", "Show semantic and template details for this control.")
        ];
    }

    private static IReadOnlyList<InfoDiagnosticPayload> CreateDiagnostics(ControlDescriptor control)
    {
        var diagnostics = new List<InfoDiagnosticPayload>();
        if (control.IsOptionalPackage)
        {
            diagnostics.Add(new InfoDiagnosticPayload(
                "ATOMUICLI_INFO_OPTIONAL_PACKAGE",
                "info",
                $"The {control.Name} control is delivered by optional package {control.PackageId}.",
                $"Run `dotnet atomui package {control.PackageId}`."));
        }

        if (control.IsCommercial)
        {
            diagnostics.Add(new InfoDiagnosticPayload(
                "ATOMUICLI_INFO_COMMERCIAL",
                "info",
                $"The {control.Name} control belongs to a commercial AtomUI product.",
                $"Run `dotnet atomui package {control.PackageId}`."));
        }

        return diagnostics;
    }

    private static string NormalizeGalleryRoute(string route)
    {
        return route;
    }

    private IReadOnlyList<ListControlCategoryPayload> CreateCategoryPayloads(IReadOnlyList<ControlDescriptor> controls)
    {
        return catalog.Categories
            .OrderBy(category => category.DisplayOrder)
            .Select(category => new ListControlCategoryPayload(
                category.Id,
                category.Name,
                category.DisplayOrder,
                controls
                    .Where(control => control.CategoryId.Equals(category.Id, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(control => control.DisplayOrder)
                    .Select(ListControlPayload.FromDescriptor)
                    .ToArray()))
            .Where(category => category.Controls.Count > 0)
            .ToArray();
    }

    private int GetCategoryOrder(string categoryId)
    {
        return catalog.Categories.FirstOrDefault(category => category.Id.Equals(categoryId, StringComparison.OrdinalIgnoreCase))?.DisplayOrder ?? int.MaxValue;
    }

    private static bool MatchesCategory(ControlDescriptor control, string normalizedFilter)
    {
        return NormalizeFilter(control.CategoryId).Equals(normalizedFilter, StringComparison.Ordinal)
               || NormalizeFilter(control.CategoryName).Equals(normalizedFilter, StringComparison.Ordinal);
    }

    internal static string NormalizeFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    }

    private static bool Matches(string value, string? filter)
    {
        return string.IsNullOrWhiteSpace(filter) || value.Equals(filter, StringComparison.OrdinalIgnoreCase);
    }
}
