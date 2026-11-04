// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_registering_a_stale_batch : given.a_populated_migration_chain
{
    EventTypeDefinition _cold;
    IEnumerable<EventTypeId> _changed;

    async Task Because()
    {
        _changed = await RegisterBatch(_stale);
        _cold = await ReadCold();
    }

    [Fact] void should_not_report_mutated_ids() => _changed.ShouldBeEmpty();
    [Fact] void should_keep_all_generations() => ShouldRetainSchemas(_cold);
    [Fact] void should_keep_all_bridges() => ShouldRetainBridges(_cold);
}
