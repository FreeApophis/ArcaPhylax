using ArcaPhylax.Abstractions;
using ArcaPhylax.Data;
using ArcaPhylax.Data.Content;
using ArcaPhylax.Data.Postgres.Provisioning;
using ArcaPhylax.Infrastructure.Migrations;
using Autofac;

namespace ArcaPhylax.Infrastructure;

/// <summary>
/// Registers the persistence and provisioning services that back the host and, through the
/// abstraction interfaces, any plugin resolved from a child scope. Kept out of the UI host so the
/// data layer can be referenced (and tested) without pulling in Avalonia.
/// </summary>
public sealed class InfrastructureModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // SQLite + repositories
        builder.RegisterType<SqliteConnectionFactory>().As<ISqliteConnectionFactory>().SingleInstance();
        builder.RegisterType<DatabaseBackup>().As<IDatabaseBackup>().SingleInstance();
        builder.RegisterType<MigrationRunner>().AsSelf().SingleInstance();
        builder.RegisterType<DbInitializer>().As<IDbInitializer>().SingleInstance();
        builder.RegisterType<DocumentRepository>().As<IDocumentRepository>().SingleInstance();
        builder.RegisterType<DroppedItemsRepository>().As<IDroppedItemsRepository>().SingleInstance();

        // Content store
        builder.RegisterType<ItemRepository>().As<IItemRepository>().SingleInstance();
        builder.RegisterType<BlobRepository>().As<IBlobRepository>().SingleInstance();
        builder.RegisterType<TagRepository>().As<ITagRepository>().SingleInstance();
        builder.RegisterType<AttributeRepository>().As<IAttributeRepository>().SingleInstance();
        builder.RegisterType<SetRepository>().As<ISetRepository>().SingleInstance();
        builder.RegisterType<SearchService>().As<ISearchService>().SingleInstance();

        builder.RegisterType<IndexingQueue>().AsSelf().SingleInstance();
        builder.RegisterType<IndexingQueue>().As<IIndexingQueue>().SingleInstance();
        builder.RegisterType<IndexingWorker>().As<IIndexingWorker>().SingleInstance();

        // Postgres provisioning
        builder.RegisterType<PostgresProvisioningOptions>().AsSelf().SingleInstance();
        builder.RegisterType<RuntimeOperatingSystemInfo>().As<IOperatingSystemInfo>().SingleInstance();
        builder.RegisterType<ProcessCommandRunner>().As<ICommandRunner>().SingleInstance();
        builder.RegisterType<LinuxDistributionDetector>().As<ILinuxDistributionDetector>().SingleInstance();
        builder.RegisterType<PostgresDetector>().As<IPostgresDetector>().SingleInstance();
        builder.RegisterType<CompositePostgresInstaller>().As<IPostgresInstaller>().SingleInstance();
        builder.RegisterType<PostgresProvisioner>().As<IPostgresProvisioner>().SingleInstance();
    }
}
