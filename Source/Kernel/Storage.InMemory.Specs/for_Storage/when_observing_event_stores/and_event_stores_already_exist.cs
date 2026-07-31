// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.InMemory.for_Storage.when_observing_event_stores;

public class and_event_stores_already_exist : given.a_storage
{
    static readonly EventStoreName _eventStore = "SomeEventStore";
    readonly List<IEnumerable<EventStoreName>> _received = [];
    IEnumerable<EventStoreName> _snapshot;

    void Establish() => _storage.GetEventStore(_eventStore);

    async Task Because()
    {
        _snapshot = await _storage.GetEventStores();
        _storage.ObserveEventStores().Subscribe(_received.Add);
    }

    [Fact] void should_report_the_same_event_stores_the_snapshot_reports() => _received[0].ShouldContainOnly([.. _snapshot]);
}
