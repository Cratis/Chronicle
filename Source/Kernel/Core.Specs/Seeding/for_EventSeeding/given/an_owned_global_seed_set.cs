// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.given;

public class an_owned_global_seed_set : a_global_event_seeding_grain
{
    protected EventSeeds _stored;
    protected IResultAwareEventSeeding _namespaceGrain;
    protected INamespaces _namespaces;

    void Establish()
    {
        SeededEventEntry[] entries =
        [
            StoredEntry(1, SeedOrigin.Declared, "program"),
            StoredEntry(2, SeedOrigin.Declared, "other"),
            StoredEntry(3, SeedOrigin.Added, "program"),
            StoredEntry(4, SeedOrigin.Unknown, string.Empty)
        ];
        _stored = new(
            entries.GroupBy(_ => _.EventTypeId).ToDictionary(_ => _.Key, _ => (IEnumerable<SeededEventEntry>)_.ToArray()),
            entries.GroupBy(_ => _.EventSourceId).ToDictionary(_ => _.Key, _ => (IEnumerable<SeededEventEntry>)_.ToArray()));
        _state.State.Returns(_ => _stored);
        _state.When(_ => _.State = Arg.Any<EventSeeds>()).Do(call => _stored = call.Arg<EventSeeds>());
        _namespaces = Substitute.For<INamespaces>();
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<Concepts.EventStoreNamespaceName>>(["existing"]));
        _grainFactory.GetGrain<INamespaces>(Arg.Any<string>()).Returns(_namespaces);
        _namespaceGrain = Substitute.For<IResultAwareEventSeeding>();
        _namespaceGrain.SeedWithResult(Arg.Any<IEnumerable<SeedingEntry>>()).Returns(Task.FromResult(SeedingResult.Complete));
        _grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForNamespace("TestEventStore", "existing").ToString()).Returns(_namespaceGrain);
    }

    protected static SeededEventEntry StoredEntry(int index, SeedOrigin origin, string owner)
    {
        var entry = AnEntry(index);
        return new(entry.EventSourceId, entry.EventTypeId, entry.Content, []) { Origin = origin, Owner = owner };
    }

    protected IEnumerable<EventSourceId> StoredSources => _stored.ByEventSource.Keys;
}
