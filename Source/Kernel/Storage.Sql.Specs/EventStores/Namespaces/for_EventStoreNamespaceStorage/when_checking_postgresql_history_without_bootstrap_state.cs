// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_EventStoreNamespaceStorage;

[Trait("Category", "Container")]
[Collection(PostgreSqlCollection.Name)]
public class when_checking_postgresql_history_without_bootstrap_state(PostgreSqlFixture fixture) : for_Database.given.a_postgresql_database(fixture)
{
    bool _hasData;

    async Task Establish()
    {
        await using var scope = await Database.Namespace(Store, Namespace);
        await scope.DbContext.Database.ExecuteSqlRawAsync("DROP TABLE \"ReplayedReadModels\"; CREATE TABLE \"custom-events\" (sequence_number BIGINT, content TEXT); INSERT INTO \"custom-events\" VALUES (0, 'payload');");
    }

    async Task Because() => _hasData = await Database.NamespaceHasEvents(Store, Namespace);

    [Fact] void should_find_custom_events_independently_of_bootstrap_state() => _hasData.ShouldBeTrue();
}
