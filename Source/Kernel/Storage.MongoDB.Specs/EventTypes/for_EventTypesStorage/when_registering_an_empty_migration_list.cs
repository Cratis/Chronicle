// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

[Collection(MongoDBCollection.Name)]
public class when_registering_an_empty_migration_list(MongoDBFixture fixture) : given.a_populated_migration_chain(fixture)
{
    EventTypeDefinition _cold;
    bool _changed;

    async Task Because()
    {
        _changed = await _storage.Register(_stale with { Migrations = [] });
        _cold = await ReadCold();
    }

    [Fact] void should_not_report_a_change() => _changed.ShouldBeFalse();
    [Fact] void should_preserve_all_schemas() => ShouldRetainSchemas(_cold);
    [Fact] void should_preserve_all_bridges() => ShouldRetainBridges(_cold);
}
