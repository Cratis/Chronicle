// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.when_registering_a_batch;

public class and_nothing_changed : given.a_batch_of_event_types
{
    IEnumerable<EventTypeId> _changed;

    async Task Establish()
    {
        await _storage.Register(_batch);
        _storage.ObserveLatestForAllEventTypes().Subscribe(_published.Add);
    }

    async Task Because() => _changed = await _storage.Register(_batch);

    [Fact] void should_not_publish_again() => _published.Count.ShouldEqual(1);
    [Fact] void should_return_no_changed_ids() => _changed.ShouldBeEmpty();
}
