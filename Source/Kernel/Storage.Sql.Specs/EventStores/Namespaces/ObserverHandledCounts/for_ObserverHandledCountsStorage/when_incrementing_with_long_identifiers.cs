// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_ObserverHandledCountsStorage;

public class when_incrementing_with_long_identifiers : given.an_observer_handled_counts_storage
{
    IReadOnlyDictionary<EventTypeId, EventCount> _result;
    string _observer;
    string _eventType;

    async Task Because()
    {
        _observer = new string('o', 200);
        _eventType = new string('e', 200);
        await _storage.Increment(_observer, "partition", new Dictionary<EventTypeId, EventCount> { [_eventType] = 2UL });
        await _storage.Increment(_observer, "partition", new Dictionary<EventTypeId, EventCount> { [_eventType] = 3UL });
        _result = await FreshStorage().GetFor(_observer, "partition");
    }

    [Fact] void should_add_up_counts() => _result[_eventType].Value.ShouldEqual(5UL);
    [Fact] void should_have_one_event_type() => _result.Count.ShouldEqual(1);
}
