// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_registering_an_empty_migration_list : given.a_populated_migration_chain
{
    EventTypeDefinition _cold;
    bool _changed;

    async Task Because()
    {
        _changed = await _storage.Register(_stale with { Migrations = [] });
        _cold = await ReadCold();
    }

    [Fact] void should_not_report_a_change() => _changed.ShouldBeFalse();
    [Fact] void should_keep_the_bridge_chain() => ShouldRetainBridges(_cold);
    [Fact] void should_keep_the_schema_map() => ShouldRetainSchemas(_cold);
}
