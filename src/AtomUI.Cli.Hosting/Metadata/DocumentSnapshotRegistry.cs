namespace AtomUI.Cli.Hosting.Metadata;

public sealed class DocumentSnapshotRegistry(IReadOnlyList<DocumentSnapshot> snapshots)
{
    private const string SnapshotId = "atomui-docs-v6-builtin";
    private const string SchemaVersion = "1.0";
    private const string TargetVersion = "6.0";
    private const string Language = "zh";
    private const string ButtonSourcePath = "src/AtomUI.Desktop.Controls/Buttons/Button.cs";
    private const string ButtonThemePath = "src/AtomUI.Desktop.Controls/Buttons/Themes/ButtonTheme.axaml";
    private const string ButtonGalleryPath = "controlgallery/AtomUIGallery/ShowCases/General/Button/Views/ButtonShowCase.axaml";

    public IReadOnlyList<DocumentSnapshot> Snapshots { get; } = snapshots;

    public static DocumentSnapshotRegistry CreateDefault()
    {
        var source = new DocumentSourceIdentity(
            "../ReferenceProjects/AtomUI",
            "release/6.0",
            "builtin-doc-snapshot",
            "2026-06-29T00:00:00Z");

        var snapshot = new DocumentSnapshot(
            SchemaVersion,
            SnapshotId,
            TargetVersion,
            "desktop",
            "AtomUI.Desktop.Controls",
            Language,
            source,
            [
                CreateButton(source),
                CreateDataGrid(source)
            ],
            [
                CreateDesignLanguageTopic(source)
            ]);

        return new DocumentSnapshotRegistry([snapshot]);
    }

    private static ControlDocument CreateButton(DocumentSourceIdentity source)
    {
        var identity = new ControlDocumentIdentity(
            "button",
            "Button",
            "Button",
            "general",
            "desktop",
            "AtomUI.Desktop.Controls",
            "AtomUI.Controls",
            "https://atomui.net",
            "Avalonia.Controls.Button",
            "stable",
            false);
        var usage = new ControlUsageDocument(
            "Button 是 AtomUI 中用于触发明确用户动作的基础控件，适合保存、提交、取消、删除、跳转等显式命令。",
            [
                "页面或表单需要用户触发一个明确命令。",
                "需要通过 Primary、Default、Text、Link、Dashed、Danger 等视觉优先级表达动作层级。",
                "需要展示 loading、禁用、图标、块级按钮、颜色变体或主题状态。"
            ],
            [
                "只用于展示状态而不触发命令时，应使用 Tag、Badge 或文本。",
                "需要在多个选项中选择一个值时，应使用 Radio、Segmented 或 Select。"
            ],
            [
                Snippet("xml", "<atom:Button ButtonType=\"Primary\" Content=\"Save\" />", ButtonGalleryPath, 24),
                Snippet("csharp", "new Button { Content = \"Save\", ButtonType = ButtonType.Primary };", ButtonSourcePath, null)
            ]);
        var apiSurface = new ControlApiSurfaceDocument(
            CreateButtonApiMembers(),
            CreateButtonApiEvents(),
            [
                new ApiInheritanceDocument("Command", "inherited-contract", "Avalonia.Controls.Button", "Button 继承 Avalonia 命令模型，业务命令仍然通过 Command/CommandParameter 接入。"),
                new ApiInheritanceDocument("Click", "inherited-contract", "Avalonia.Controls.Button", "Click 是最基础的用户动作事件；AtomUI 扩展视觉状态，不改写事件语义。"),
                new ApiInheritanceDocument("Classes", "inherited-contract", "Avalonia.Controls.Control", "高级主题定制可以通过样式类参与 selector 匹配。")
            ],
            []);
        var logicStructure = CreateButtonLogicStructure();
        var theme = CreateButtonTheme();
        var examples = CreateButtonExamples();
        var tokens = CreateButtonTokens();
        var semanticParts = CreateButtonSemanticParts();
        var sourceFiles = CreateButtonSourceFiles();

        return new ControlDocument(
            identity.Id,
            identity.Name,
            identity.DisplayName,
            identity.CategoryId,
            identity.ProductId,
            identity.PackageId,
            identity.IsCommercial,
            identity.Status,
            usage.Summary,
            identity,
            usage,
            apiSurface,
            logicStructure,
            theme,
            examples,
            tokens,
            semanticParts,
            sourceFiles,
            CreateButtonSections(examples),
            [
                new DocumentRelatedItem("command", "info", "Control metadata", "dotnet atomui info Button"),
                new DocumentRelatedItem("command", "demo", "Button demos", "dotnet atomui demo Button --list"),
                new DocumentRelatedItem("command", "token", "Button tokens", "dotnet atomui token Button"),
                new DocumentRelatedItem("command", "semantic", "Button semantic parts", "dotnet atomui semantic Button --include-template")
            ],
            [],
            source,
            SnapshotId,
            SchemaVersion,
            TargetVersion,
            Language);
    }

    private static IReadOnlyList<ApiMemberDocument> CreateButtonApiMembers()
    {
        return
        [
            Member("ButtonType", "styled-property", "public", "public ButtonType ButtonType { get; set; }", "ButtonType", "Default", "设置按钮视觉优先级，例如 Default、Primary、Dashed、Text、Link。", 42),
            Member("Shape", "styled-property", "public", "public ButtonShape Shape { get; set; }", "ButtonShape", "Default", "控制按钮外形，例如默认矩形、圆角或圆形图标按钮。", 53),
            Member("IsDanger", "styled-property", "public", "public bool IsDanger { get; set; }", "bool", "false", "声明破坏性动作状态，并同步 danger 伪类和主题变量。", 64),
            Member("IsGhost", "styled-property", "public", "public bool IsGhost { get; set; }", "bool", "false", "启用透明背景的 ghost 视觉状态。", 75),
            Member("IsLoading", "styled-property", "public", "public bool IsLoading { get; set; }", "bool", "false", "显示加载图标并阻止重复触发操作。", 86),
            Member("SizeType", "styled-property", "public", "public CustomizableSizeType SizeType { get; set; }", "CustomizableSizeType", "Middle", "接入 AtomUI 尺寸体系，影响高度、间距和字号。", 97),
            Member("Icon", "styled-property", "public", "public Icon? Icon { get; set; }", "Icon?", "null", "配置按钮图标，模板消费 PART_ButtonIcon。", 108),
            Member("IconPlacement", "styled-property", "public", "public Dock IconPlacement { get; set; }", "Dock", "Left", "控制图标相对内容的位置。", 119),
            Member("IsMotionEnabled", "styled-property", "public", "public bool IsMotionEnabled { get; set; }", "bool", "true", "控制按钮动效是否启用。", 130),
            Member("IsWaveSpiritEnabled", "styled-property", "public", "public bool IsWaveSpiritEnabled { get; set; }", "bool", "true", "控制点击波纹反馈是否启用。", 141),
            Member("Color", "styled-property", "public", "public ButtonColor? Color { get; set; }", "ButtonColor?", "null", "选择预设颜色通道，和 Variant 一起参与 ResolveEffectiveColorAndVariant。", 152),
            Member("Variant", "styled-property", "public", "public ButtonVariant? Variant { get; set; }", "ButtonVariant?", "null", "选择 Filled、Outlined、Text、Link 等颜色变体。", 163),
            Member("CustomBackground", "styled-property", "public", "public IBrush? CustomBackground { get; set; }", "IBrush?", "null", "提供自定义背景画刷，主题由 CustomBackgroundLayer 消费。", 174),
            Member(".ctor", "constructor", "public", "public Button()", null, null, "创建 Button 并初始化 AtomUI 状态同步。", 30),
            Member("GetBorderThicknessForCompactSpace", "protected-method", "protected", "protected virtual Thickness GetBorderThicknessForCompactSpace()", "Thickness", null, "紧凑布局下计算边框厚度；派生控件可覆盖但应保持主题 Token 兼容。", 196),
            Member("NotifySetFormValue", "protected-method", "protected", "protected virtual void NotifySetFormValue(object? value)", "void", null, "表单系统设置值时调用，用于同步 IFormItemAware 契约。", 205),
            Member("NotifyGetFormValue", "protected-method", "protected", "protected virtual object? NotifyGetFormValue()", "object?", null, "表单系统读取当前值时调用。", 214),
            Member("NotifyClearFormValue", "protected-method", "protected", "protected virtual void NotifyClearFormValue()", "void", null, "表单系统清空值时调用。", 223),
            Member("NotifyValidateStatus", "protected-method", "protected", "protected virtual void NotifyValidateStatus(FormValidateStatus status)", "void", null, "表单校验状态变化时同步按钮状态。", 232),
            Member("OnInitialized", "protected-method", "protected override", "protected override void OnInitialized()", "void", null, "初始化阶段挂接主题资源和基础状态。", 246),
            Member("OnLoaded", "protected-method", "protected override", "protected override void OnLoaded(RoutedEventArgs e)", "void", null, "进入 visual tree 后刷新最终伪类和动画状态。", 258),
            Member("OnAttachedToVisualTree", "protected-method", "protected override", "protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)", "void", null, "附加到视觉树时绑定运行时协作对象。", 270),
            Member("OnDetachedFromVisualTree", "protected-method", "protected override", "protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)", "void", null, "从视觉树移除时释放运行时协作对象。", 282),
            Member("OnApplyTemplate", "protected-method", "protected override", "protected override void OnApplyTemplate(TemplateAppliedEventArgs e)", "void", null, "获取 PART_WaveSpirit、PART_ButtonIcon、PART_LoadingIcon 和 PART_ContentPresenter，并同步模板状态。", 296),
            Member("MeasureOverride", "protected-method", "protected override", "protected override Size MeasureOverride(Size availableSize)", "Size", null, "根据图标、loading、content 和 SizeType 参与测量。", 322),
            Member("OnPropertyChanged", "protected-method", "protected override", "protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)", "void", null, "属性变化入口，驱动伪类、颜色变体和主题状态同步。", 342)
        ];
    }

    private static ApiMemberDocument Member(
        string name,
        string kind,
        string accessibility,
        string signature,
        string? type,
        string? defaultValue,
        string description,
        int? sourceLine)
    {
        return new ApiMemberDocument(
            name,
            kind,
            "AtomUI.Controls.Button",
            accessibility,
            signature,
            type,
            defaultValue,
            kind is "styled-property" or "direct-property" ? "public-api" : "extension-point",
            description,
            ButtonSourcePath,
            sourceLine);
    }

    private static IReadOnlyList<ApiEventDocument> CreateButtonApiEvents()
    {
        return
        [
            new ApiEventDocument(
                "Click",
                "inherited-event",
                "Avalonia.Controls.Button",
                "public",
                "public event EventHandler<RoutedEventArgs>? Click",
                "bubble",
                "RoutedEventArgs",
                "继承自 Avalonia Button 的点击事件，表示用户激活按钮。",
                ButtonSourcePath,
                null),
            new ApiEventDocument(
                "IFormItemAware.ValueChanged",
                "interface-event",
                "AtomUI.Controls.Button",
                "explicit",
                "event EventHandler? IFormItemAware.ValueChanged",
                null,
                "EventArgs",
                "表单协作系统监听的显式接口事件，用于表单值变化通知。",
                ButtonSourcePath,
                238)
        ];
    }

    private static ControlLogicStructureDocument CreateButtonLogicStructure()
    {
        return new ControlLogicStructureDocument(
            [
                new LogicNodeDocument("button", "control", "AtomUI.Controls.Button", "AtomUI Button 控件类型。", ["ButtonType", "IsLoading", "Color", "Variant"]),
                new LogicNodeDocument("avalonia-button", "base-type", "Avalonia.Controls.Button", "继承 Avalonia 命令和点击行为。", ["Command", "Click"]),
                new LogicNodeDocument("form-aware", "interface", "IFormItemAware", "接入 AtomUI 表单协作契约。", ["NotifySetFormValue", "NotifyValidateStatus"])
            ],
            [
                new LogicNodeDocument("action-style", "api-group", "Action style", "ButtonType、IsDanger、IsGhost、Color、Variant 共同决定视觉层级。", ["ButtonType", "IsDanger", "IsGhost", "Color", "Variant"]),
                new LogicNodeDocument("density", "api-group", "Density", "SizeType 和 Shape 共同决定尺寸、圆角和图标按钮密度。", ["SizeType", "Shape"]),
                new LogicNodeDocument("content", "api-group", "Content and icon", "Icon、IconPlacement、Content 和 IsLoading 决定模板内容区结构。", ["Icon", "IconPlacement", "IsLoading"]),
                new LogicNodeDocument("motion", "api-group", "Motion", "IsMotionEnabled 与 IsWaveSpiritEnabled 控制动效和点击反馈。", ["IsMotionEnabled", "IsWaveSpiritEnabled"])
            ],
            [
                new LogicFlowDocument(
                    "effective-color",
                    "ResolveEffectiveColorAndVariant",
                    [
                        "读取 ButtonType、IsDanger、IsGhost、Color、Variant 和 CustomBackground。",
                        "ResolveEffectiveColorAndVariant 归一为内部 effective color / variant。",
                        "OnPropertyChanged 在相关属性变化时重新计算。",
                        "计算结果通过伪类、theme variable 和 TemplateBinding 进入 ControlTheme。"
                    ]),
                new LogicFlowDocument(
                    "loading-state",
                    "Loading state",
                    [
                        "IsLoading 变化进入 OnPropertyChanged。",
                        "同步 :loading 伪类。",
                        "PART_LoadingIcon 显示加载动画。",
                        "按钮内容区保留测量空间，避免 loading 状态造成布局跳动。"
                    ]),
                new LogicFlowDocument(
                    "template-attach",
                    "Template attach",
                    [
                        "OnApplyTemplate 获取命名节点。",
                        "绑定 PART_WaveSpirit、PART_ButtonIcon、PART_LoadingIcon 和 PART_ContentPresenter。",
                        "将当前 public API 状态同步到模板节点。"
                    ])
            ],
            [
                new LogicNodeDocument("wave-spirit", "runtime-collaborator", "WaveSpirit", "点击反馈协作对象，由 PART_WaveSpirit 承载。", ["IsWaveSpiritEnabled"]),
                new LogicNodeDocument("form-item", "runtime-collaborator", "Form item bridge", "表单容器通过 IFormItemAware 读取和校验按钮状态。", ["NotifyValidateStatus"])
            ],
            [
                new LogicFlowDocument(
                    "theme-bridge",
                    "Public API to ControlTheme",
                    [
                        "StyledProperty 变化进入 OnPropertyChanged。",
                        "状态归一后更新 pseudo classes。",
                        "ControlTheme selector 匹配 ButtonType、danger、ghost、loading、shape、size 等状态。",
                        "TemplateBinding 将 Icon、Content、CustomBackground 等值传递到命名节点。"
                    ])
            ]);
    }

    private static ControlThemeDocument CreateButtonTheme()
    {
        return new ControlThemeDocument(
            "Button",
            [
                new ThemeTemplateDocument(
                    "button-default-template",
                    "ControlTheme x:Key=\"{x:Type atom:Button}\" TargetType=\"atom:Button\"",
                    ButtonThemePath,
                    14,
                    [
                        new ThemeNodeDocument(
                            "Panel",
                            "PART_WaveSpirit",
                            "stable",
                            [
                                new ThemeNodeDocument("Border", "ShadowsFrame", "internal", []),
                                new ThemeNodeDocument(
                                    "Border",
                                    "Frame",
                                    "stable",
                                    [
                                        new ThemeNodeDocument("Border", "CustomBackgroundLayer", "stable", []),
                                        new ThemeNodeDocument(
                                            "Grid",
                                            "PART_RootLayout",
                                            "stable",
                                            [
                                                new ThemeNodeDocument("LoadingIcon", "PART_LoadingIcon", "stable", []),
                                                new ThemeNodeDocument("IconPresenter", "PART_ButtonIcon", "stable", []),
                                                new ThemeNodeDocument("ContentPresenter", "PART_ContentPresenter", "stable", [])
                                            ])
                                    ])
                            ])
                    ])
            ],
            [
                new ThemeSelectorGroupDocument("variant", ["Button.primary", "Button.dashed", "Button.text", "Button.link"], "Template variant: Default / Link / Primary / Text"),
                new ThemeSelectorGroupDocument("state", ["Button:loading", "Button:danger", "Button:ghost", "Button:pointerover", "Button:pressed"], "用户交互状态和业务状态 selector。"),
                new ThemeSelectorGroupDocument("size", ["Button.small", "Button.middle", "Button.large"], "尺寸 selector 消费 SizeType。")
            ],
            [
                new ThemeBindingDocument("PART_ButtonIcon", "Icon", "Icon"),
                new ThemeBindingDocument("PART_ButtonIcon", "Dock", "IconPlacement"),
                new ThemeBindingDocument("PART_LoadingIcon", "IsVisible", "IsLoading"),
                new ThemeBindingDocument("PART_ContentPresenter", "Content", "Content"),
                new ThemeBindingDocument("CustomBackgroundLayer", "Background", "CustomBackground")
            ],
            [
                new ThemeTokenUsageDocument("DynamicResource", "ButtonToken", "Button", "control-token"),
                new ThemeTokenUsageDocument("DynamicResource", "ColorPrimary", "Button.primary / Frame", "Background"),
                new ThemeTokenUsageDocument("DynamicResource", "ControlHeight", "Button", "MinHeight"),
                new ThemeTokenUsageDocument("DynamicResource", "BorderRadius", "Frame", "CornerRadius")
            ],
            [
                new ThemeCustomizationBoundaryDocument("PART_LoadingIcon", "stable", "允许通过 ControlTheme 替换加载视觉，但应保留 IsLoading 绑定。"),
                new ThemeCustomizationBoundaryDocument("PART_ContentPresenter", "stable", "允许定制内容呈现，必须保留 Content 绑定和布局占位。"),
                new ThemeCustomizationBoundaryDocument("Frame", "stable", "允许定制边框和背景，必须保留状态 selector 的资源消费。"),
                new ThemeCustomizationBoundaryDocument("ShadowsFrame", "internal", "阴影层属于内部视觉细节，商业主题覆盖时应谨慎。")
            ]);
    }

    private static IReadOnlyList<ControlExampleDocument> CreateButtonExamples()
    {
        return
        [
            Example("button-type", "按钮类型", "展示 Default、Primary、Dashed、Text 和 Link 的视觉优先级。", "basic", 10, "<atom:Button ButtonType=\"Primary\" Content=\"Primary\" />"),
            Example("button-shape", "按钮外形", "使用 Shape 切换默认、圆角和圆形图标按钮。", "basic", 20, "<atom:Button Shape=\"Round\" Content=\"Round\" />"),
            Example("button-size", "按钮尺寸", "使用 SizeType 适配 Small、Middle 和 Large 密度。", "basic", 30, "<atom:Button SizeType=\"Large\" ButtonType=\"Primary\" Content=\"Large\" />"),
            Example("button-icon", "图标按钮", "在按钮内显示图标，适合工具栏或明确动作入口。", "basic", 40, "<atom:Button ButtonType=\"Primary\" Content=\"Create\">\n  <atom:Button.Icon>\n    <atom:Icon Value=\"PlusOutlined\" />\n  </atom:Button.Icon>\n</atom:Button>"),
            Example("button-icon-placement", "图标位置", "通过 IconPlacement 控制图标和内容的相对位置。", "basic", 50, "<atom:Button IconPlacement=\"Right\" Content=\"Next\">\n  <atom:Button.Icon>\n    <atom:Icon Value=\"ArrowRightOutlined\" />\n  </atom:Button.Icon>\n</atom:Button>"),
            Example("button-loading", "加载状态", "命令执行中设置 IsLoading，避免重复触发并提供进度反馈。", "state", 60, "<atom:Button ButtonType=\"Primary\" IsLoading=\"True\" Content=\"Saving\" />"),
            Example("button-block", "块级按钮", "在表单底部或移动布局中使用水平铺满按钮。", "layout", 70, "<atom:Button ButtonType=\"Primary\" HorizontalAlignment=\"Stretch\" Content=\"Submit\" />"),
            Example("button-danger", "危险动作", "删除、清空等破坏性动作应显式使用 danger 视觉状态。", "state", 80, "<atom:Button ButtonType=\"Primary\" IsDanger=\"True\" Content=\"Delete\" />"),
            Example("button-ghost", "幽灵按钮", "在有色背景或强调区域上使用透明背景按钮。", "theme", 90, "<atom:Button ButtonType=\"Primary\" IsGhost=\"True\" Content=\"Ghost\" />"),
            Example("button-disabled", "禁用状态", "当动作条件不满足时禁用按钮。", "state", 91, "<atom:Button IsEnabled=\"False\" Content=\"Disabled\" />"),
            Example("button-gradient", "渐变按钮", "通过 CustomBackground 提供自定义背景画刷。", "theme", 92, "<atom:Button ButtonType=\"Primary\" Content=\"Gradient\">\n  <atom:Button.CustomBackground>\n    <LinearGradientBrush StartPoint=\"0%,0%\" EndPoint=\"100%,0%\">\n      <GradientStop Color=\"#1677FF\" Offset=\"0\" />\n      <GradientStop Color=\"#13C2C2\" Offset=\"1\" />\n    </LinearGradientBrush>\n  </atom:Button.CustomBackground>\n</atom:Button>"),
            Example("button-color-variant", "颜色变体", "使用 Color 和 Variant 组合表达预设颜色通道和视觉变体。", "theme", 93, "<atom:Button Color=\"Success\" Variant=\"Outlined\" Content=\"Success\" />")
        ];
    }

    private static ControlExampleDocument Example(
        string sourceKey,
        string title,
        string description,
        string kind,
        int priority,
        string xaml)
    {
        return new ControlExampleDocument(
            sourceKey,
            title,
            description,
            kind,
            priority,
            null,
            [Snippet("xml", xaml, ButtonGalleryPath, null)],
            ButtonGalleryPath,
            null);
    }

    private static IReadOnlyList<ControlTokenDocument> CreateButtonTokens()
    {
        return
        [
            new ControlTokenDocument("ButtonToken", "control", "stable", "Button 控件专用 Token 映射，承载按钮边距、圆角、颜色和状态变量。", ["Button", "Frame", "PART_LoadingIcon"]),
            new ControlTokenDocument("ColorPrimary", "shared", "stable", "Primary 按钮使用的品牌主色。", ["Button.primary / Frame.Background"]),
            new ControlTokenDocument("ControlHeight", "shared", "stable", "按钮尺寸系统的高度基准。", ["Button.MinHeight"]),
            new ControlTokenDocument("BorderRadius", "shared", "stable", "Frame 圆角资源。", ["Frame.CornerRadius"])
        ];
    }

    private static IReadOnlyList<ControlSemanticPartDocument> CreateButtonSemanticParts()
    {
        return
        [
            new ControlSemanticPartDocument("PART_WaveSpirit", "WaveSpirit", "点击波纹反馈承载节点。", ["IsWaveSpiritEnabled"], ["ButtonToken"], "stable"),
            new ControlSemanticPartDocument("PART_ButtonIcon", "IconPresenter", "渲染 Icon 并响应 IconPlacement。", ["Icon", "IconPlacement"], ["ButtonToken"], "stable"),
            new ControlSemanticPartDocument("PART_LoadingIcon", "LoadingIcon", "渲染 loading 动画并响应 IsLoading。", ["IsLoading"], ["ButtonToken", "ColorPrimary"], "stable"),
            new ControlSemanticPartDocument("PART_ContentPresenter", "ContentPresenter", "渲染 Content 并保留 loading 状态下的内容布局。", ["Content"], ["ButtonToken"], "stable"),
            new ControlSemanticPartDocument(":loading", "PseudoClass", "IsLoading 为 true 时应用。", ["IsLoading"], [], "stable"),
            new ControlSemanticPartDocument(":danger", "PseudoClass", "IsDanger 为 true 时应用。", ["IsDanger"], ["ColorError"], "stable"),
            new ControlSemanticPartDocument(":ghost", "PseudoClass", "IsGhost 为 true 时应用。", ["IsGhost"], [], "stable")
        ];
    }

    private static IReadOnlyList<ControlSourceFileDocument> CreateButtonSourceFiles()
    {
        return
        [
            new ControlSourceFileDocument("control", ButtonSourcePath, "Button 控件 public API、生命周期和状态同步。"),
            new ControlSourceFileDocument("theme", ButtonThemePath, "Button ControlTheme、命名节点、selector 和 Token 使用点。"),
            new ControlSourceFileDocument("gallery", ButtonGalleryPath, "Button Gallery 示例入口。"),
            new ControlSourceFileDocument("docs", "docs/controls/desktop/general/button/overview.md", "Button 使用文档。"),
            new ControlSourceFileDocument("docs", "docs/controls/desktop/general/button/implementation.md", "Button 实现说明。"),
            new ControlSourceFileDocument("docs", "docs/controls/desktop/general/button/token.md", "Button Token 文档。")
        ];
    }

    private static IReadOnlyList<DocumentSectionContent> CreateButtonSections(IReadOnlyList<ControlExampleDocument> examples)
    {
        return
        [
            Section(
                "overview",
                "Overview",
                100,
                "Button 是 AtomUI 中用于触发明确用户动作的基础控件。它继承 Avalonia Button 的命令和 Click 语义，并在 AtomUI 中扩展了 ButtonType、Shape、Loading、Danger、Ghost、Color、Variant、WaveSpirit 和主题 Token 能力。",
                ["curated"]),
            Section(
                "install",
                "Install",
                200,
                "Package: `AtomUI.Desktop.Controls`\n\nNamespace: `AtomUI.Controls`\n\nXAML namespace: `https://atomui.net`\n\nRecommended prefix: `atom`",
                ["package"]),
            Section(
                "usage",
                "Usage",
                300,
                "```xml\n<atom:Button ButtonType=\"Primary\" Content=\"Save\" />\n```\n\n```csharp\nnew Button { Content = \"Save\", ButtonType = ButtonType.Primary };\n```",
                ["demo", "generated"]),
            Section(
                "scenarios",
                "常见使用场景",
                400,
                "- 表单提交、保存、取消、删除等明确动作。\n- 工具栏中带图标的短命令。\n- 异步命令执行中的 loading 反馈。\n- 需要通过 Danger、Ghost、Link、Text 或颜色变体表达动作语义的区域。",
                ["curated"]),
            Section(
                "examples",
                "使用示例",
                500,
                RenderExampleSection(examples),
                ["demo"]),
            Section(
                "api",
                "API",
                600,
                CreateButtonApiMarkdown(),
                ["api", "event"]),
            Section(
                "properties",
                "公共属性",
                610,
                "| Name | Type | Avalonia kind | Default | Description |\n| --- | --- | --- | --- | --- |\n| ButtonType | ButtonType | StyledProperty | Default | 设置视觉优先级。 |\n| Shape | ButtonShape | StyledProperty | Default | 设置按钮外形。 |\n| IsLoading | bool | StyledProperty | false | 显示 loading 状态。 |\n| Color | ButtonColor? | StyledProperty | null | 设置预设颜色通道。 |\n| Variant | ButtonVariant? | StyledProperty | null | 设置颜色视觉变体。 |",
                ["api"]),
            Section(
                "methods",
                "公共方法与 Protected 扩展点",
                620,
                "Button 当前不增加新的 public 方法，主要扩展点是 protected 生命周期方法：OnApplyTemplate、MeasureOverride、OnPropertyChanged、OnLoaded、OnAttachedToVisualTree、OnDetachedFromVisualTree、GetBorderThicknessForCompactSpace、NotifySetFormValue、NotifyValidateStatus。",
                ["api"]),
            Section(
                "events",
                "事件",
                630,
                "| Name | Kind | Event args | Description |\n| --- | --- | --- | --- |\n| Click | inherited-event | RoutedEventArgs | 继承自 Avalonia Button 的用户激活事件。 |\n| IFormItemAware.ValueChanged | interface-event | EventArgs | 表单协作系统监听的显式接口事件。 |",
                ["event"]),
            Section(
                "logic",
                "逻辑结构",
                700,
                "```text\nButton\n  -> Avalonia.Controls.Button\n  -> IFormItemAware\n\nPublic API\n  -> action style: ButtonType / IsDanger / IsGhost / Color / Variant / CustomBackground\n  -> density: SizeType / Shape\n  -> content: Icon / IconPlacement / Content / IsLoading\n  -> motion: IsMotionEnabled / IsWaveSpiritEnabled\n\nState normalization\n  -> OnPropertyChanged\n  -> ResolveEffectiveColorAndVariant\n  -> pseudo classes: :loading / :danger / :ghost\n  -> ControlTheme selector and TemplateBinding\n\nTemplate attach\n  -> OnApplyTemplate\n  -> PART_WaveSpirit\n  -> PART_ButtonIcon\n  -> PART_LoadingIcon\n  -> PART_ContentPresenter\n```",
                ["logic"]),
            Section(
                "theme",
                "ControlTheme 结构",
                800,
                "Template variant: Default / Link / Primary / Text\n\n```text\nButton ControlTheme\n  -> PART_WaveSpirit\n     -> ShadowsFrame\n     -> Frame\n        -> CustomBackgroundLayer\n        -> PART_RootLayout\n           -> PART_LoadingIcon\n           -> PART_ButtonIcon\n           -> PART_ContentPresenter\n```\n\n关键 TemplateBinding：Icon、IconPlacement、IsLoading、Content、CustomBackground。\n\n关键 selector：Button.primary、Button.dashed、Button.text、Button.link、Button:loading、Button:danger、Button:ghost、Button:pointerover、Button:pressed。",
                ["theme"]),
            Section(
                "tokens",
                "主题与 Design Token",
                900,
                "| Token | Scope | Description |\n| --- | --- | --- |\n| ButtonToken | control | Button 专用 Token 映射。 |\n| ColorPrimary | shared | Primary 按钮使用的品牌主色。 |\n| ControlHeight | shared | 尺寸体系高度基准。 |\n| BorderRadius | shared | Frame 圆角资源。 |",
                ["token"]),
            Section(
                "semantic",
                "语义结构",
                1000,
                "| Name | Kind | Responsibility |\n| --- | --- | --- |\n| PART_WaveSpirit | template part | 点击反馈。 |\n| PART_ButtonIcon | template part | 图标呈现。 |\n| PART_LoadingIcon | template part | loading 动画。 |\n| PART_ContentPresenter | template part | 内容呈现。 |\n| :loading | pseudo class | loading 状态。 |\n| :danger | pseudo class | 危险动作状态。 |",
                ["semantic"]),
            Section(
                "demos",
                "Demos",
                1100,
                "| Demo | Description | Command |\n| --- | --- | --- |\n| basic | Button type and priority. | `dotnet atomui demo Button basic` |\n| state | Loading, disabled, and danger states. | `dotnet atomui demo Button state` |\n| theme | Color, variant, ghost, gradient and theme usage. | `dotnet atomui demo Button theme` |",
                ["demo"]),
            Section(
                "changelog",
                "Changelog",
                1200,
                "- 6.0: 内置 Button 文档快照覆盖 API surface、事件、逻辑结构、ControlTheme、Token、语义结构和 Gallery 示例。",
                ["changelog"]),
            Section(
                "source",
                "源码索引",
                1300,
                "- `src/AtomUI.Desktop.Controls/Buttons/Button.cs`\n- `src/AtomUI.Desktop.Controls/Buttons/Themes/ButtonTheme.axaml`\n- `controlgallery/AtomUIGallery/ShowCases/General/Button/Views/ButtonShowCase.axaml`\n- `docs/controls/desktop/general/button/overview.md`\n- `docs/controls/desktop/general/button/implementation.md`\n- `docs/controls/desktop/general/button/token.md`",
                ["generated"])
        ];
    }

    private static string CreateButtonApiMarkdown()
    {
        return """
### 公共属性

| Name | Type | Avalonia kind | Default | Description |
| --- | --- | --- | --- | --- |
| ButtonType | ButtonType | StyledProperty | Default | 设置 Default、Primary、Dashed、Text、Link 等视觉优先级。 |
| Shape | ButtonShape | StyledProperty | Default | 控制默认、圆角或圆形图标按钮外形。 |
| IsDanger | bool | StyledProperty | false | 应用破坏性动作状态。 |
| IsGhost | bool | StyledProperty | false | 启用透明背景视觉。 |
| IsLoading | bool | StyledProperty | false | 显示 loading 并进入 :loading 状态。 |
| SizeType | CustomizableSizeType | StyledProperty | Middle | 接入 AtomUI 尺寸体系。 |
| Icon | Icon? | StyledProperty | null | 配置按钮图标。 |
| IconPlacement | Dock | StyledProperty | Left | 控制图标位置。 |
| IsMotionEnabled | bool | StyledProperty | true | 控制动效。 |
| IsWaveSpiritEnabled | bool | StyledProperty | true | 控制点击波纹反馈。 |
| Color | ButtonColor? | StyledProperty | null | 选择预设颜色通道。 |
| Variant | ButtonVariant? | StyledProperty | null | 选择 Filled、Outlined、Text、Link 等颜色变体。 |
| CustomBackground | IBrush? | StyledProperty | null | 自定义背景画刷。 |

### 公共方法

Button 不增加新的 public 方法，命令和 Click 语义继承自 Avalonia Button。

### 事件

| Name | Kind | Event args | Description |
| --- | --- | --- | --- |
| Click | inherited-event | RoutedEventArgs | 用户激活按钮时触发。 |
| IFormItemAware.ValueChanged | interface-event | EventArgs | 表单协作系统监听的显式接口事件。 |

### Protected 扩展点

| Name | Lifecycle | Description |
| --- | --- | --- |
| OnInitialized | initialization | 初始化主题资源和基础状态。 |
| OnLoaded | loaded | 进入 visual tree 后刷新状态。 |
| OnAttachedToVisualTree | attach | 绑定运行时协作对象。 |
| OnDetachedFromVisualTree | detach | 释放运行时协作对象。 |
| OnApplyTemplate | template | 获取 PART_WaveSpirit、PART_ButtonIcon、PART_LoadingIcon 和 PART_ContentPresenter。 |
| MeasureOverride | measure | 根据图标、loading、content 和 SizeType 参与测量。 |
| OnPropertyChanged | property-change | 属性变化时同步伪类、颜色变体和主题状态。 |
| GetBorderThicknessForCompactSpace | layout | 紧凑布局下计算边框厚度。 |
| NotifySetFormValue | form | 表单系统设置值。 |
| NotifyValidateStatus | form | 表单校验状态同步。 |

### 显式接口契约

| Contract | Member | Description |
| --- | --- | --- |
| IFormItemAware | ValueChanged | 表单值变化通知事件。 |
| IFormItemAware | NotifySetFormValue | 表单容器写入值。 |
| IFormItemAware | NotifyValidateStatus | 表单容器同步校验状态。 |

### 关键继承契约

| Member | Declaring type | Reason |
| --- | --- | --- |
| Command | Avalonia.Controls.Button | 保持 Avalonia 命令模型。 |
| Click | Avalonia.Controls.Button | 保持标准用户激活事件。 |
| Classes | Avalonia.Controls.Control | 支持高级样式 selector。 |
""";
    }

    private static string RenderExampleSection(IReadOnlyList<ControlExampleDocument> examples)
    {
        return string.Join(
            "\n\n",
            examples.Select(example => $"### {example.Title}\n\nSourceKey: `{example.SourceKey}`\n\n{example.Description}"));
    }

    private static ControlDocument CreateDataGrid(DocumentSourceIdentity source)
    {
        var identity = new ControlDocumentIdentity(
            "datagrid",
            "DataGrid",
            "DataGrid",
            "data-display",
            "datagrid",
            "AtomUI.Desktop.Controls.DataGrid",
            "AtomUI.Controls.DataGrid",
            "https://atomui.net",
            "Avalonia.Controls.TemplatedControl",
            "commercial",
            true);
        var usage = new ControlUsageDocument(
            "DataGrid 用于展示和编辑结构化表格数据，是商业控件产品中的数据密集型控件。",
            ["需要展示可排序、可编辑或密集行列数据。"],
            ["少量静态键值信息应使用 Descriptions 或 Table。"],
            [Snippet("xml", "<atom:DataGrid ItemsSource=\"{Binding Items}\" />", "controlgallery/AtomUIGallery/ShowCases/DataDisplay/DataGrid/Views/DataGridShowCase.axaml", null)]);
        var examples = new[]
        {
            new ControlExampleDocument(
                "datagrid-basic",
                "基础表格",
                "绑定 ItemsSource 展示行数据。",
                "basic",
                10,
                null,
                usage.MinimalSnippets,
                "controlgallery/AtomUIGallery/ShowCases/DataDisplay/DataGrid/Views/DataGridShowCase.axaml",
                null)
        };
        var apiSurface = new ControlApiSurfaceDocument(
            [
                new ApiMemberDocument("ItemsSource", "styled-property", "AtomUI.Controls.DataGrid", "public", "public IEnumerable? ItemsSource { get; set; }", "IEnumerable?", "null", "public-api", "提供表格行数据。", "src/AtomUI.Desktop.Controls.DataGrid/DataGrid.cs", null),
                new ApiMemberDocument("CanUserSortColumns", "styled-property", "AtomUI.Controls.DataGrid", "public", "public bool CanUserSortColumns { get; set; }", "bool", "true", "public-api", "允许用户点击列头排序。", "src/AtomUI.Desktop.Controls.DataGrid/DataGrid.cs", null)
            ],
            [],
            [],
            [new ApiSurfaceDiagnostic("ATOMUICLI_DOC_COMMERCIAL", "公开快照只包含商业控件基础契约，完整 API 需要商业数据根。", "warning", null)]);
        var logic = new ControlLogicStructureDocument(
            [new LogicNodeDocument("datagrid", "control", "AtomUI.Controls.DataGrid", "商业 DataGrid 控件。", ["ItemsSource"])],
            [new LogicNodeDocument("data", "api-group", "Data source", "ItemsSource 驱动行数据。", ["ItemsSource"])],
            [new LogicFlowDocument("items-source", "ItemsSource flow", ["ItemsSource 变化", "刷新行模型", "更新模板呈现"])],
            [],
            []);
        var theme = new ControlThemeDocument(
            "DataGrid",
            [],
            [],
            [],
            [new ThemeTokenUsageDocument("DynamicResource", "HeaderBg", "DataGridColumnHeader", "Background")],
            []);
        var tokens = new[]
        {
            new ControlTokenDocument("HeaderBg", "control", "stable", "列头背景。", ["DataGridColumnHeader.Background"]),
            new ControlTokenDocument("RowHoverBackground", "control", "stable", "行 hover 背景。", ["DataGridRow:pointerover.Background"])
        };

        return new ControlDocument(
            identity.Id,
            identity.Name,
            identity.DisplayName,
            identity.CategoryId,
            identity.ProductId,
            identity.PackageId,
            identity.IsCommercial,
            identity.Status,
            usage.Summary,
            identity,
            usage,
            apiSurface,
            logic,
            theme,
            examples,
            tokens,
            [],
            [new ControlSourceFileDocument("control", "src/AtomUI.Desktop.Controls.DataGrid/DataGrid.cs", "商业 DataGrid 控件入口。")],
            [
                Section(
                    "overview",
                    "Overview",
                    100,
                    "DataGrid displays structured tabular data and supports commercial data workflows that need sorting, editing, and dense row presentation.",
                    ["curated"]),
                Section(
                    "install",
                    "Install",
                    200,
                    "Package: `AtomUI.Desktop.Controls.DataGrid`\n\nProduct: `datagrid`\n\nThis is a commercial control. Configure the commercial data root or package source before relying on private API documentation.",
                    ["package"]),
                Section(
                    "usage",
                    "Usage",
                    300,
                    "```xml\n<atom:DataGrid ItemsSource=\"{Binding Items}\" />\n```",
                    ["demo", "generated"]),
                Section(
                    "api",
                    "API",
                    500,
                    "| Property | Type | Default | Description |\n| --- | --- | --- | --- |\n| ItemsSource | IEnumerable? | null | Provides the rows displayed by the grid. |\n| CanUserSortColumns | bool | true | Enables column sorting interaction. |",
                    ["api"]),
                Section(
                    "tokens",
                    "Tokens",
                    600,
                    "| Token | Scope | Description |\n| --- | --- | --- |\n| HeaderBg | control | Header background brush. |\n| RowHoverBackground | control | Row hover background brush. |",
                    ["token"]),
                Section(
                    "demos",
                    "Demos",
                    800,
                    "| Demo | Description | Command |\n| --- | --- | --- |\n| basic | Basic tabular data display. | `dotnet atomui demo DataGrid basic` |",
                    ["demo"])
            ],
            [
                new DocumentRelatedItem("command", "info", "Control metadata", "dotnet atomui info DataGrid --product datagrid"),
                new DocumentRelatedItem("command", "demo", "DataGrid demos", "dotnet atomui demo DataGrid --list")
            ],
            [
                new DocumentWarning("ATOMUICLI_DOC_COMMERCIAL", "DataGrid is a commercial control; public snapshots may omit private contract details.", "install")
            ],
            source,
            SnapshotId,
            SchemaVersion,
            TargetVersion,
            Language);
    }

    private static TopicDocument CreateDesignLanguageTopic(DocumentSourceIdentity source)
    {
        return new TopicDocument(
            "design-language",
            "AtomUI Design Language",
            Language,
            [
                Section(
                    "overview",
                    "Overview",
                    100,
                    "AtomUI documentation should prefer clear hierarchy, stable tokens, explicit state language, and predictable control contracts.",
                    ["curated"]),
                Section(
                    "usage",
                    "Usage",
                    300,
                    "Use design language guidance when writing control docs, demos, and theme customization notes.",
                    ["curated"])
            ],
            [
                new DocumentRelatedItem("command", "list", "Control catalog", "dotnet atomui list"),
                new DocumentRelatedItem("command", "token", "Global tokens", "dotnet atomui token")
            ],
            [],
            source,
            SnapshotId,
            SchemaVersion,
            TargetVersion);
    }

    private static CodeSnippetDocument Snippet(
        string language,
        string code,
        string? sourcePath,
        int? sourceLine)
    {
        return new CodeSnippetDocument(language, code, sourcePath, sourceLine);
    }

    private static DocumentSectionContent Section(
        string id,
        string title,
        int order,
        string markdown,
        IReadOnlyList<string> sourceKinds)
    {
        return new DocumentSectionContent(id, title, order, markdown, sourceKinds);
    }
}
