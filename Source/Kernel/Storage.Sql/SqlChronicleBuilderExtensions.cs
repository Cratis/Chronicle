// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.DependencyInjection;
using SqlStorage = Cratis.Chronicle.Storage.Sql.SystemStorage;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for <see cref="IChronicleBuilder"/> for configuring Chronicle to use SQL.
/// </summary>
public static class SqlChronicleBuilderExtensions
{
    /// <summary>
    /// Configure Chronicle to use SQL, based on the <see cref="ChronicleOptions"/>.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="options"><see cref="ChronicleOptions"/> to use.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithSql(this IChronicleBuilder builder, ChronicleOptions options)
    {
        builder.SiloBuilder.AddStartupTask<ClusterDbContextMigrator>(ServiceLifecycleStage.First);

        builder.Services.AddSingleton<IDatabase, Database>();
        builder.Services.AddSingleton<IClusterStorage, ClusterStorage>();
        builder.Services.AddSingleton<IEncryptionKeyStorage, EncryptionKeyStorage>();
        builder.Services.AddDbContextFactory<ClusterDbContext>((serviceProvider, optionsBuilder) =>
        {
            optionsBuilder
                .UseDatabaseFromConnectionString(options.Storage.ConnectionDetails)
                .UseApplicationServiceProvider(serviceProvider);
        });

        builder.Services.AddSingleton<IReminderTable, ReminderTable>();
        builder.Services.AddSingleton<ISystemStorage, SqlStorage>();
        builder.Services.AddSingleton<IStorage, Storage.Storage>();
        builder.Services.AddSingleton<IReadModelMigrator, ReadModelMigrator>();
        builder.Services.AddSingleton<IEventSequenceMigrator, EventSequenceMigrator>();
        builder.Services.AddSingleton<IUniqueConstraintMigrator, UniqueConstraintMigrator>();
        builder.Services.AddSingleton(typeof(ITableMigrator<>), typeof(TableMigrator<>));
        builder.Services.AddSingleton<ISinkFactory, SinkFactory>();

        AddHealthCheck(builder, options);

        return builder;
    }

    static void AddHealthCheck(IChronicleBuilder builder, ChronicleOptions options)
    {
        var connectionString = options.Storage.ConnectionDetails;

        if (string.Equals(options.Storage.Type, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddHealthChecks().AddNpgSql(connectionString, name: "postgresql", timeout: TimeSpan.FromSeconds(3));
        }
        else if (string.Equals(options.Storage.Type, StorageType.MsSql, StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddHealthChecks().AddSqlServer(connectionString, name: "mssql", timeout: TimeSpan.FromSeconds(3));
        }
    }
}
