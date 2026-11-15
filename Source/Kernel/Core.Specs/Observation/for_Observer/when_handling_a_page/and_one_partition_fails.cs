// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling_a_page;

public class and_one_partition_fails : given.an_observer_with_a_page_subscriber
{
    void Establish() =>
        _pageSubscriber.OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(new ObserverPageResult(new Dictionary<Key, ObserverSubscriberResult>
            {
                [(Key)"A"] = ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "A could not be handled"),
                [(Key)"B"] = ObserverSubscriberResult.Ok(11UL)
            }));

    async Task Because() => await _observer.HandlePage(
    [
        new((Key)"A", [EventAt(10UL)]),
        new((Key)"B", [EventAt(11UL)])
    ]);

    [Fact] void should_fail_only_the_partition_that_failed() => _failedPartitionsState.Partitions.Select(_ => _.Partition.Value.ToString()).ShouldContainOnly("A");
    [Fact] void should_park_the_failed_partition_at_its_first_event() => _failedPartitionsState.Partitions.Single().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)10UL);
    [Fact] void should_still_move_the_cursor_for_the_partition_that_succeeded() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)12UL);
    [Fact] void should_leave_no_partition_in_flight() => _stateStorage.State.InFlightPartitions.ShouldBeEmpty();
}
