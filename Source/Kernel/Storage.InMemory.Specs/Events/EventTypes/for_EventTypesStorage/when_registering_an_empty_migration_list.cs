// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_registering_an_empty_migration_list : given.a_populated_migration_chain
{
    EventTypeDefinition _stored;
    IEnumerable<EventTypeId> _changed;

    async Task Because()
    {
        _changed = await RegisterBatch(_stale with { Migrations = [] });
        _stored = await _storage.GetDefinition(_chain.Id);
    }

    [Fact] void should_not_report_a_change() => _changed.ShouldBeEmpty();
    [Fact] void should_keep_the_chain() => EventTypeMigrationsVersion.For(_stored.Migrations).ShouldEqual(_version);
}
