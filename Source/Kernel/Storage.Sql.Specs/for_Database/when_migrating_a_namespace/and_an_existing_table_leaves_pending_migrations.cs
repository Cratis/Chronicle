// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_migrating_a_namespace;

[Collection(PostgreSqlCollection.Name)]
public class and_an_existing_table_leaves_pending_migrations(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    Exception _first;
    Exception _second;

    async Task Establish()
    {
        var target = new Npgsql.NpgsqlConnectionStringBuilder(ConnectionString).Database + "_incomplete";
        await CreateLegacyDatabase(target);
        await Execute(ConnectionFor(target), "CREATE TABLE \"FailedPartitions\" (\"Id\" int)");
        await using var cluster = await Database.Cluster();
        cluster.DbContext.DatabaseNames.Add(new DatabaseNameBinding { Kind = DatabaseKind.Namespace, EventStore = Store, Namespace = Namespace, PhysicalName = target });
        await cluster.DbContext.SaveChangesAsync();
    }

    async Task Because()
    {
        _first = await Catch.Exception(async () => { await using var scope = await Database.Namespace(Store, Namespace); });
        _second = await Catch.Exception(async () => { await using var scope = await Database.Namespace(Store, Namespace); });
    }

    [Fact] void should_fail_with_the_pending_migration() => _first.ShouldBeOfExactType<DatabaseMigrationIncomplete>();
    [Fact] void should_not_mark_the_database_migrated() => _second.ShouldBeOfExactType<DatabaseMigrationIncomplete>();
}
