// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_ObserverHandledCountsStorage;

public class when_removing_counts : given.an_observer_handled_counts_storage
{
    IReadOnlyDictionary<EventTypeId, EventCount> _removedPartition;
    IReadOnlyDictionary<EventTypeId, EventCount> _keptPartition;
    IReadOnlyDictionary<EventTypeId, EventCount> _removedObserverPartition;
    IReadOnlyDictionary<EventTypeId, EventCount> _otherObserver;

    async Task Establish()
    {
        var counts = new Dictionary<EventTypeId, EventCount> { ["type-a"] = 1UL };
        await _storage.Increment("observer", "p1", counts);
        await _storage.Increment("observer", "p2", counts);
        await _storage.Increment("other-observer", "p1", counts);
    }

    async Task Because()
    {
        await _storage.RemoveFor("observer", "p1");
        _removedPartition = await FreshStorage().GetFor("observer", "p1");
        _keptPartition = await FreshStorage().GetFor("observer", "p2");
        await _storage.RemoveAllFor("observer");
        _removedObserverPartition = await FreshStorage().GetFor("observer", "p2");
        _otherObserver = await FreshStorage().GetFor("other-observer", "p1");
    }

    [Fact] void should_remove_the_partition() => _removedPartition.Count.ShouldEqual(0);
    [Fact] void should_keep_other_partitions_when_removing_one() => _keptPartition.Count.ShouldEqual(1);
    [Fact] void should_remove_all_partitions_for_the_observer() => _removedObserverPartition.Count.ShouldEqual(0);
    [Fact] void should_keep_other_observers() => _otherObserver.Count.ShouldEqual(1);
}
