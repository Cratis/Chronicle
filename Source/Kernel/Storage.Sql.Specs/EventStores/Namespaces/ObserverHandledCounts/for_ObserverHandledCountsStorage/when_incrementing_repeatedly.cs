// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_ObserverHandledCountsStorage;

public class when_incrementing_repeatedly : given.an_observer_handled_counts_storage
{
    IReadOnlyDictionary<EventTypeId, EventCount> _result;
    IReadOnlyDictionary<EventTypeId, EventCount> _otherPartition;

    async Task Because()
    {
        await _storage.Increment("observer", "partition", new Dictionary<EventTypeId, EventCount> { ["type-a"] = 2UL, ["type-b"] = 1UL });
        await _storage.Increment("observer", "partition", new Dictionary<EventTypeId, EventCount> { ["type-a"] = 3UL });
        await _storage.Increment("observer", "partition", new Dictionary<EventTypeId, EventCount>());
        _result = await FreshStorage().GetFor("observer", "partition");
        _otherPartition = await FreshStorage().GetFor("observer", "other");
    }

    [Fact] void should_add_up_counts_per_event_type() => _result["type-a"].Value.ShouldEqual(5UL);
    [Fact] void should_keep_other_event_types() => _result["type-b"].Value.ShouldEqual(1UL);
    [Fact] void should_have_two_event_types() => _result.Count.ShouldEqual(2);
    [Fact] void should_return_empty_for_other_partitions() => _otherPartition.Count.ShouldEqual(0);
}
