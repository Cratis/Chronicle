// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_saving;

public class with_changed_handled_counts : given.an_observer_state_storage
{
    KernelObserverState _saved;

    async Task Establish() => await _storage.Save(new() { Identifier = "observer", HandledEventCount = 7, HandledEventCountPerEventType = new Dictionary<EventTypeId, EventCount> { ["event-a"] = 4, ["event-b"] = 3 } }, Concepts.EventSequences.EventSequenceId.Log);

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", HandledEventCount = 9, HandledEventCountPerEventType = new Dictionary<EventTypeId, EventCount> { ["event-a"] = 9 } }, Concepts.EventSequences.EventSequenceId.Log);
        _saved = await _storage.Get("observer");
    }

    [Fact] void should_update_the_handled_event_count() => _saved.HandledEventCount.Value.ShouldEqual(9UL);
    [Fact] void should_replace_the_counts_per_event_type() => _saved.HandledEventCountPerEventType.ShouldContainOnly(new Dictionary<EventTypeId, EventCount> { ["event-a"] = 9 });
}
