// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling_a_page;

public class and_every_partition_is_handled : given.an_observer_with_a_page_subscriber
{
    void Establish() =>
        _pageSubscriber.OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(new ObserverPageResult(new Dictionary<Key, ObserverSubscriberResult>
            {
                [(Key)"A"] = ObserverSubscriberResult.Ok(12UL),
                [(Key)"B"] = ObserverSubscriberResult.Ok(11UL)
            }));

    async Task Because() => await _observer.HandlePage(
    [
        new((Key)"A", [EventAt(10UL)]),
        new((Key)"B", [EventAt(11UL)]),
        new((Key)"A", [EventAt(12UL)])
    ]);

    [Fact] void should_deliver_the_page_in_one_call() => _pageSubscriber.Received(1).OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_deliver_each_partition_once() => _pageSubscriber.Received().OnNextPartitions(Arg.Is<IReadOnlyList<ObserverPartitionBatch>>(_ => _.Count == 2 && _.Single(b => b.Partition.Equals((Key)"A")).Events.Count == 2), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_deliver_per_partition() => _pageSubscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_move_the_cursor_past_the_whole_page() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);
    [Fact] void should_record_the_last_handled_event() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)12UL);
    [Fact] void should_leave_no_partition_in_flight() => _stateStorage.State.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_not_fail_any_partition() => _failedPartitionsState.Partitions.ShouldBeEmpty();
}
