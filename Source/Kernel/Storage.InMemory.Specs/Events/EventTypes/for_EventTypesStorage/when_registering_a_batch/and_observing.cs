// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.when_registering_a_batch;

public class and_observing : given.a_batch_of_event_types
{
    IEnumerable<EventTypeId> _changed;

    void Establish() => _storage.ObserveLatestForAllEventTypes().Subscribe(_published.Add);

    async Task Because() => _changed = await _storage.Register(_batch);

    [Fact] void should_publish_once_after_the_seed() => _published.Count.ShouldEqual(2);
    [Fact] void should_publish_every_type_in_the_batch() => _published[^1].Select(_ => _.Type.Id).ShouldContainOnly(new EventTypeId("simple-event"), new EventTypeId("migrated-event"));
    [Fact] void should_return_the_changed_ids() => _changed.ShouldContainOnly(new EventTypeId("simple-event"), new EventTypeId("migrated-event"));
}
