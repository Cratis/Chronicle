// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_an_event_store;

[Collection(PostgreSqlCollection.Name)]
public class and_unrelated_stores_share_a_legacy_database_with_data(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    Exception _first;
    Exception _second;

    async Task Establish()
    {
        Store = new string('s', 90);
        await SeedStore(Store + "first");
        await SeedStore(Store + "second");
        var legacy = LogicalStoreName[..63];
        await CreateLegacyDatabase(legacy);
        await Execute(ConnectionFor(legacy), "CREATE TABLE \"EventTypes\" (\"Id\" int); INSERT INTO \"EventTypes\" VALUES (1)");
    }

    async Task Because()
    {
        _first = await Catch.Exception(async () => { await using var scope = await Database.EventStore(Store + "first"); });
        _second = await Catch.Exception(async () => { await using var scope = await Database.EventStore(Store + "second"); });
    }

    [Fact] void should_refuse_the_first_store() => _first.ShouldBeOfExactType<DatabaseOwnershipUnresolved>();
    [Fact] void should_refuse_the_second_store() => _second.ShouldBeOfExactType<DatabaseOwnershipUnresolved>();
}
