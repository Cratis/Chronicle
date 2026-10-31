// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_an_event_store;

[Collection(SqlServerCollection.Name)]
public class and_sql_server_names_differ_only_in_case(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
{
    Exception _error;

    async Task Establish() { await using var scope = await Database.EventStore("Orders"); }

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        await using var scope = await Database.EventStore("orders");
    });

    [Fact] void should_not_bind_another_identity_to_the_same_database() => _error.ShouldBeOfExactType<DatabaseOwnershipUnresolved>();
}
