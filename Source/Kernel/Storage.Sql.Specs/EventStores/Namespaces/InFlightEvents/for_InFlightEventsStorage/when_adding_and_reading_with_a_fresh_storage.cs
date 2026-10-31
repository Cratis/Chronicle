// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.InFlightEvents.for_InFlightEventsStorage;

public class when_adding_and_reading_with_a_fresh_storage : given.an_in_flight_events_storage
{
    InFlightEvent[] _result;

    async Task Establish()
    {
        await _storage.Add("observer", "partition", 5UL);
        await _storage.Add("observer", "other-partition", 6UL);
        await _storage.Add("other-observer", "partition", 7UL);
    }

    async Task Because() => _result = (await FreshStorage().GetFor("observer")).OrderBy(_ => _.EventSequenceNumber.Value).ToArray();

    [Fact] void should_return_only_the_entries_of_the_observer() => _result.Length.ShouldEqual(2);
    [Fact] void should_keep_the_partition() => _result[0].Partition.ToString().ShouldEqual("partition");
    [Fact] void should_keep_the_sequence_number() => _result[0].EventSequenceNumber.Value.ShouldEqual(5UL);
    [Fact] void should_keep_the_observer() => _result[0].ObserverId.Value.ShouldEqual("observer");
    [Fact] void should_assign_an_identifier() => _result[0].Id.ShouldNotEqual(InFlightEventId.NotSet);
}
