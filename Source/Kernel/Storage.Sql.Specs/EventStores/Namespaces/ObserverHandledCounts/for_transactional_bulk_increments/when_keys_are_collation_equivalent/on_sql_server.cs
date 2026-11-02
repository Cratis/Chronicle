// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.when_keys_are_collation_equivalent;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : given.a_counts_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    ObserverHandledCountEntry[] _rows;

    async Task Because()
    {
        await _harness.Storage.Increment("observer",
        [
            new((Key)"partition", new Dictionary<EventTypeId, EventCount> { ["type"] = 2UL }),
            new((Key)"PARTITION", new Dictionary<EventTypeId, EventCount> { ["TYPE"] = 3UL })
        ]);
        await _harness.Storage.Increment("observer",
        [
            new((Key)"partition", new Dictionary<EventTypeId, EventCount> { ["type"] = 5UL }),
            new((Key)"PARTITION", new Dictionary<EventTypeId, EventCount> { ["TYPE"] = 7UL })
        ]);
        _rows = await _harness.Rows();
    }

    [Fact] void should_group_source_keys_using_database_collation() => _rows.Length.ShouldEqual(1);
    [Fact] void should_add_every_equivalent_delta() => _rows.Single().Count.ShouldEqual(17L);
}
