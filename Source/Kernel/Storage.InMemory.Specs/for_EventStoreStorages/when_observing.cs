// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorages;

public class when_observing : given.an_empty_registry
{
    static readonly EventStoreName _eventStore = "SomeEventStore";
    readonly List<IEnumerable<EventStoreName>> _received = [];

    void Establish() => _storages.GetOrCreate(_eventStore);

    void Because() => _storages.Observe().Subscribe(_received.Add);

    [Fact] void should_seed_the_observer_with_the_event_stores_registered_before_it_subscribed() =>
        _received[0].ShouldContainOnly(_eventStore);
}
