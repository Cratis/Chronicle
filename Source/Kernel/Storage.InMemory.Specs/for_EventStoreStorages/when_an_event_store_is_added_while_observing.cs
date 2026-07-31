// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorages;

public class when_an_event_store_is_added_while_observing : given.an_empty_registry
{
    static readonly EventStoreName _first = "FirstEventStore";
    static readonly EventStoreName _second = "SecondEventStore";
    readonly List<IEnumerable<EventStoreName>> _received = [];

    void Establish()
    {
        _storages.GetOrCreate(_first);
        _storages.Observe().Subscribe(_received.Add);
    }

    void Because() => _storages.GetOrCreate(_second);

    [Fact] void should_notify_the_observer_of_the_added_event_store() => _received.Count.ShouldEqual(2);

    [Fact] void should_include_both_event_stores_in_the_latest_notification() =>
        _received[^1].ShouldContainOnly(_first, _second);
}
