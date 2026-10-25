// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

[Collection(PostgreSqlCollection.Name)]
public class and_postgresql_pooling_is_configured(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    string _connectionString;

    async Task Because()
    {
        var database = CreateDatabase(
            new()
            {
                Type = StorageType.PostgreSql,
                ConnectionDetails = ConnectionString,
                PostgreSqlPooling = new() { MaxPoolSize = 7, ConnectionIdleLifetimeSeconds = 12, ConnectionPruningIntervalSeconds = 3 }
            },
            Services);
        await using (var scope = await database.EventStore(Store))
        {
            _connectionString = scope.DbContext.Database.GetConnectionString()!;
        }

        await using var cluster = await database.Cluster();
        var name = await cluster.DbContext.DatabaseNames
            .Where(row => row.Kind == DatabaseKind.EventStore && row.EventStore == Store)
            .Select(row => row.PhysicalName)
            .SingleAsync();
        AdditionalDatabases.Add(name);
    }

    [Fact] void should_apply_the_maximum_pool_size() => new NpgsqlConnectionStringBuilder(_connectionString).MaxPoolSize.ShouldEqual(7);
    [Fact] void should_apply_the_idle_lifetime() => new NpgsqlConnectionStringBuilder(_connectionString).ConnectionIdleLifetime.ShouldEqual(12);
    [Fact] void should_apply_the_pruning_interval() => new NpgsqlConnectionStringBuilder(_connectionString).ConnectionPruningInterval.ShouldEqual(3);
}
