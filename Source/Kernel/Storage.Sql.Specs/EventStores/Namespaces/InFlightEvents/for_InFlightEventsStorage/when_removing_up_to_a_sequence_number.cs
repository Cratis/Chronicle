// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.InFlightEvents.for_InFlightEventsStorage;

public class when_removing_up_to_a_sequence_number : given.an_in_flight_events_storage
{
    (string Partition, ulong Number)[] _remaining;

    async Task Establish()
    {
        await _storage.Add("observer", "partition", 4UL);
        await _storage.Add("observer", "partition", 10UL);
        await _storage.Add("observer", "partition", 11UL);
        await _storage.Add("observer", "other-partition", 4UL);
        await _storage.Add("other-observer", "partition", 4UL);
    }

    async Task Because()
    {
        await _storage.RemoveUpTo("observer", "partition", 10UL);
        _remaining = (await FreshStorage().GetFor("observer"))
            .Select(_ => (_.Partition.ToString(), _.EventSequenceNumber.Value))
            .OrderBy(_ => _.Item1).ThenBy(_ => _.Item2).ToArray();
    }

    [Fact] void should_remove_inclusive_of_the_sequence_number_in_the_partition() => _remaining.ShouldEqual([("other-partition", 4UL), ("partition", 11UL)]);
    [Fact] async Task should_not_touch_other_observers() => (await FreshStorage().GetFor("other-observer")).Count().ShouldEqual(1);
}
