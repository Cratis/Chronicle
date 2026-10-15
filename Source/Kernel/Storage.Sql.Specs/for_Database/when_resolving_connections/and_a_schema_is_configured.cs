// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

[Collection(PostgreSqlCollection.Name)]
public class and_a_schema_is_configured(PostgreSqlFixture fixture) : given.a_postgresql_database_with_connection_resolution(fixture)
{
    long _tablesInSchema;
    long _tablesInPublic;

    async Task Because()
    {
        var database = CreateDatabase(new() { Type = StorageType.PostgreSql, ConnectionDetails = ConnectionString, Schema = "chronicle" }, Services);
        await using (var scope = await database.EventStore(Store))
        {
        }

        // The bindings live in the cluster database's schema too, so they are read through the same configuration.
        string name;
        await using (var cluster = await database.Cluster())
        {
            name = await cluster.DbContext.DatabaseNames.Where(row => row.Kind == DatabaseKind.EventStore && row.EventStore == Store).Select(row => row.PhysicalName).SingleAsync();
        }

        AdditionalDatabases.Add(name);
        var store = ConnectionFor(name);
        _tablesInSchema = await Count(store, "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'chronicle'");
        _tablesInPublic = await Count(store, "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'");
    }

    [Fact] void should_keep_the_tables_in_the_schema() => _tablesInSchema.ShouldBeGreaterThan(0L);
    [Fact] void should_keep_no_tables_in_the_default_schema() => _tablesInPublic.ShouldEqual(0L);
}
