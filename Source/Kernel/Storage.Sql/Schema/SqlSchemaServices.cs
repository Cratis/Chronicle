// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// The SQL schema services shared by the kernel and one-shot schema actions.
/// </summary>
public static class SqlSchemaServices
{
    /// <summary>
    /// Adds SQL schema services without Orleans or an HTTP host.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="options">The Chronicle options.</param>
    public static void Add(IServiceCollection services, ChronicleOptions options)
    {
        services.AddStorageConnectionResolution(SqlConnectionResolution.StorageFor(options.Storage.ConnectionDetails), options.Storage.ConnectionResolution);
        services.AddSingleton(sp => new SqlConnectionResolution(options.Storage, sp.GetRequiredService<IStorageConnectionResolution>()));
        services.AddSingleton<Database>();
        services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<Database>());
        services.AddSingleton(typeof(ITableMigrator<>), typeof(TableMigrator<>));
        services.AddSingleton<IEventSequenceMigrator, EventSequenceMigrator>();
        services.AddSingleton<IUniqueConstraintMigrator, UniqueConstraintMigrator>();
        services.AddSingleton<IReadModelMigrator, ReadModelMigrator>();
        services.AddSingleton<ISchemaProvisioner, SchemaProvisioner>();
    }
}
