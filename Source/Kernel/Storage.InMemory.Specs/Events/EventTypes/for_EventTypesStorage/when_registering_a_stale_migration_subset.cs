// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_registering_a_stale_migration_subset : given.a_populated_migration_chain
{
    EventTypeDefinition _stored;
    IEnumerable<EventTypeId> _changed;

    async Task Because()
    {
        _changed = await RegisterBatch(_stale);
        _stored = await _storage.GetDefinition(_chain.Id);
    }

    [Fact] Task should_still_upcast_from_one_to_three() => ShouldMigrateAcrossChain(_stored, (1, "one"), (3, "three"));
    [Fact] Task should_still_downcast_from_three_to_one() => ShouldMigrateAcrossChain(_stored, (3, "three"), (1, "one"));
    [Fact] void should_not_report_mutated_ids() => _changed.ShouldBeEmpty();
    [Fact] void should_keep_all_generations() => _stored.Generations.Select(_ => _.Generation.Value).ShouldContainOnly(1u, 2u, 3u);
    [Fact] void should_keep_the_full_bridge_chain() => EventTypeMigrationsVersion.For(_stored.Migrations).ShouldEqual(_version);
}
