// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

[Collection(MongoDBCollection.Name)]
public class when_registering_a_stale_batch(MongoDBFixture fixture) : given.a_populated_migration_chain(fixture)
{
    EventTypeDefinition _cold;
    IEnumerable<EventTypeId> _changed;

    async Task Because()
    {
        _changed = await RegisterBatch(_stale);
        _cold = await ReadCold();
    }

    [Fact] void should_not_report_a_change() => _changed.ShouldBeEmpty();
    [Fact] void should_keep_all_schemas() => ShouldRetainSchemas(_cold);
    [Fact] void should_keep_all_bridges() => ShouldRetainBridges(_cold);
}
