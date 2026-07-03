using AtomUI.Cli.Modularity;
using AtomUI.Modularity;

namespace AtomUI.Cli.Hosting.Metadata;

[Module(DisplayName = "AtomUI Cli Metadata")]
public sealed partial class AtomUICliMetadataModule : AtomUICliModule
{
    private static readonly HashSet<OutputFormat> KnowledgeFormats =
    [
        OutputFormat.Text,
        OutputFormat.Json,
        OutputFormat.Markdown
    ];

    public override void ConfigureServices(ModuleServiceConfigurationContext context)
    {
        context.Services.AddSingleton(MetadataCatalog.CreateDefault());
        context.Services.AddSingleton<MetadataQueryService, MetadataQueryService>();
        context.Services.AddSingleton(DocumentSnapshotRegistry.CreateDefault());
        context.Services.AddSingleton<DocumentationQueryService, DocumentationQueryService>();
        context.Services.AddSingleton<DesignDocumentQueryService, DesignDocumentQueryService>();
        context.Services.AddSingleton<DocumentSectionSelector, DocumentSectionSelector>();
        context.Services.AddSingleton<DocOutputRenderer, DocOutputRenderer>();
        context.Services.AddSingleton<DemoQueryService, DemoQueryService>();
        context.Services.AddSingleton<DemoOutputRenderer, DemoOutputRenderer>();
        context.Services.AddSingleton(TokenSnapshotRegistry.CreateDefault());
        context.Services.AddSingleton<TokenQueryService, TokenQueryService>();
        context.Services.AddSingleton<TokenOutputRenderer, TokenOutputRenderer>();
        context.Services.AddSingleton(SemanticSnapshotRegistry.CreateDefault());
        context.Services.AddSingleton<SemanticQueryService, SemanticQueryService>();
        context.Services.AddSingleton<SemanticOutputRenderer, SemanticOutputRenderer>();
        context.Services.AddSingleton(PackageSnapshotRegistry.CreateDefault());
        context.Services.AddSingleton<PackageQueryService, PackageQueryService>();
        context.Services.AddSingleton<PackageOutputRenderer, PackageOutputRenderer>();
        context.Services.AddTransient<ListCommandHandler, ListCommandHandler>();
        context.Services.AddTransient<InfoCommandHandler, InfoCommandHandler>();
        context.Services.AddTransient<DocCommandHandler, DocCommandHandler>();
        context.Services.AddTransient<DemoCommandHandler, DemoCommandHandler>();
        context.Services.AddTransient<TokenCommandHandler, TokenCommandHandler>();
        context.Services.AddTransient<SemanticCommandHandler, SemanticCommandHandler>();
        context.Services.AddTransient<DesignCommandHandler, DesignCommandHandler>();
        context.Services.AddTransient<PackageCommandHandler, PackageCommandHandler>();
        context.Services.AddTransient<ChangelogCommandHandler, ChangelogCommandHandler>();
    }

    public override void ConfigureAtomUICliCommands(AtomUICliCommandContributionContext context)
    {
        context.Add<ListCommandOptions, ListCommandHandler>("list", ListCommandOptions.Parse, CommandGroup.Knowledge, KnowledgeFormats);
        context.Add<InfoCommandOptions, InfoCommandHandler>("info", InfoCommandOptions.Parse, CommandGroup.Knowledge, KnowledgeFormats);
        context.Add<DocCommandOptions, DocCommandHandler>("doc", DocCommandOptions.Parse, CommandGroup.Knowledge, KnowledgeFormats);
        context.Add<DemoCommandOptions, DemoCommandHandler>("demo", DemoCommandOptions.Parse, CommandGroup.Knowledge, KnowledgeFormats);
        context.Add<TokenCommandOptions, TokenCommandHandler>("token", TokenCommandOptions.Parse, CommandGroup.Knowledge, KnowledgeFormats);
        context.Add<SemanticCommandOptions, SemanticCommandHandler>("semantic", SemanticCommandOptions.Parse, CommandGroup.Knowledge, KnowledgeFormats);
        context.Add<DesignCommandOptions, DesignCommandHandler>("design.md", DesignCommandOptions.Parse, CommandGroup.Knowledge, KnowledgeFormats);
        context.Add<PackageCommandOptions, PackageCommandHandler>("package", PackageCommandOptions.Parse, CommandGroup.Knowledge, KnowledgeFormats);
        context.Add<ChangelogCommandOptions, ChangelogCommandHandler>("changelog", ChangelogCommandOptions.Parse, CommandGroup.Knowledge, KnowledgeFormats);
    }
}
