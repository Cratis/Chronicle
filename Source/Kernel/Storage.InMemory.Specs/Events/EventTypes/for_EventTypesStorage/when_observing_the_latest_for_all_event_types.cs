// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_observing_the_latest_for_all_event_types : given.an_event_types_storage
{
    readonly List<IEnumerable<EventTypeSchema>> _received = [];

    async Task Establish()
    {
        await _storage.Register(new EventType("registered-before-observing", EventTypeGeneration.First), new JsonSchema());
        _storage.ObserveLatestForAllEventTypes().Subscribe(_received.Add);
    }

    async Task Because() => await _storage.Register(new EventType("registered-while-observing", EventTypeGeneration.First), new JsonSchema());

    [Fact] void should_seed_the_observer_with_what_was_already_registered() =>
        _received[0].Select(_ => _.Type.Id).ShouldContainOnly(new EventTypeId("registered-before-observing"));

    [Fact] void should_notify_the_observer_of_the_new_registration() =>
        _received[^1].Select(_ => _.Type.Id).ShouldContainOnly(
            new EventTypeId("registered-before-observing"),
            new EventTypeId("registered-while-observing"));

    [Fact]
    async Task should_not_affect_other_observers_when_one_completes()
    {
        var received = new List<IEnumerable<EventTypeSchema>>();
        var leaving = _storage.ObserveLatestForAllEventTypes();
        _storage.ObserveLatestForAllEventTypes().Subscribe(received.Add);
        leaving.OnCompleted();

        await _storage.Register(new EventType("registered-after-one-left", EventTypeGeneration.First), new JsonSchema());

        received[^1].Select(_ => _.Type.Id).ShouldContain(new EventTypeId("registered-after-one-left"));
    }
}
