// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

        builder.Services.AddStorageConnectionResolution(SqlConnectionResolution.StorageFor(options.Storage.ConnectionDetails), options.Storage.ConnectionResolution);
        builder.Services.AddSingleton(sp => new SqlConnectionResolution(options.Storage, sp.GetRequiredService<IStorageConnectionResolution>()));
        builder.Services.AddSingleton<IDatabase, Database>();
        builder.Services.AddSingleton<ReplayingTables>();
        builder.Services.AddSingleton<IClusterStorage, ClusterStorage>();
        builder.Services.AddSingleton<EncryptionKeyStorage>();
        builder.Services.AddSingleton<IEncryptionKeyStorage>(sp => new CacheEncryptionKeyStorage(sp.GetRequiredService<EncryptionKeyStorage>()));

        builder.Services.AddSingleton(typeof(ITableMigrator<>), typeof(TableMigrator<>));
        builder.Services.AddSingleton<IEventSequenceMigrator, EventSequenceMigrator>();
        builder.Services.AddSingleton<IUniqueConstraintMigrator, UniqueConstraintMigrator>();
        builder.Services.AddSingleton<IReminderTable, ReminderTable>();
        if (options.Clustering.Type == ClusteringType.Sql)
        {
            builder.Services.AddSingleton<IMembershipTable, SqlMembershipTable>();
        }

        builder.Services.AddSingleton<ISystemStorage, SqlStorage>();
        builder.Services.AddSingleton<IStorage, Storage.Storage>();
        builder.Services.AddSingleton<IStorageErrorClassifier, SqlStorageErrorClassifier>();

        // The Cratis.Orleans job system storage: jobs DbContext options resolved per event store and
        // namespace onto the same database the namespace's own DbContext uses.
        builder.Services.AddSingleton<Cratis.Orleans.Storage.IJobsStorage>(sp => new Cratis.Orleans.Storage.Sql.SqlJobsStorage(
            sp.GetRequiredService<Cratis.Orleans.Jobs.IJobTypes>(),
            Microsoft.Extensions.Options.Options.Create(new Cratis.Orleans.Storage.Sql.SqlJobsStorageOptions
            {
                OptionsResolver = (scope, @namespace) => sp.GetRequiredService<IDatabase>().GetJobsDbContextOptions(scope, @namespace)
            }),
            sp));
        builder.Services.AddSingleton<IReadModelMigrator, ReadModelMigrator>();
        builder.Services.AddSingleton<ISinkFactory, SinkFactory>();

        AddHealthCheck(builder, options);

        return builder;
    }

    static void AddHealthCheck(IChronicleBuilder builder, ChronicleOptions options)
    {
        var storage = options.Storage;
        string name;
        if (string.Equals(storage.Type, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            name = "postgresql";
        }
        else if (string.Equals(storage.Type, StorageType.MsSql, StringComparison.OrdinalIgnoreCase))
        {
            name = "mssql";
        }
        else
        {
            return;
        }

        // Connects the way the storage does, through the resolved identity, rather than with the raw connection details.
        builder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
            name,
            sp => new SqlConnectionHealthCheck(storage.ConnectionDetails, SqlConnectionResolution.From(sp, storage).PrepareRuntimeConnection),
            failureStatus: null,
            tags: null,
            timeout: TimeSpan.FromSeconds(3)));
    }
}
