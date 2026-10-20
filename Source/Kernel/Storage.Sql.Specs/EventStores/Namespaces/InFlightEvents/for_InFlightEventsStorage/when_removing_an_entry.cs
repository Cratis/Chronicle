// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.InFlightEvents.for_InFlightEventsStorage;

public class when_removing_an_entry : given.an_in_flight_events_storage
{
    ulong[] _remaining;

    async Task Establish()
    {
        await _storage.Add("observer", "partition", 5UL);
        await _storage.Add("observer", "partition", 6UL);
        await _storage.Add("observer", "other-partition", 5UL);
    }

    async Task Because()
    {
        await _storage.Remove("observer", "partition", 5UL);
        _remaining = (await FreshStorage().GetFor("observer")).Select(_ => _.EventSequenceNumber.Value).Order().ToArray();
    }

    [Fact] void should_remove_only_the_matching_entry() => _remaining.ShouldEqual([5UL, 6UL]);
}
