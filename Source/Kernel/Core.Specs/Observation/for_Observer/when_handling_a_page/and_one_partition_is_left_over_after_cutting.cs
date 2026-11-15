// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling_a_page;

/// <summary>
/// Five partitions of two events each, with a live delivery budget of four, make two pages of two partitions. The fifth
/// partition is left on its own and is delivered by itself.
/// </summary>
public class and_one_partition_is_left_over_after_cutting : given.an_observer_with_a_page_subscriber
{
    readonly List<IReadOnlyList<ObserverPartitionBatch>> _pages = [];

    void Establish()
    {
        _pageSubscriber.OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var page = call.Arg<IReadOnlyList<ObserverPartitionBatch>>();
                _pages.Add(page);
                return new ObserverPageResult(page.ToDictionary(_ => _.Partition, _ => ObserverSubscriberResult.Ok(_.Events[^1].Context.SequenceNumber)));
            });
        _pageSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => ObserverSubscriberResult.Ok(call.Arg<IEnumerable<AppendedEvent>>().Last().Context.SequenceNumber));
    }

    async Task Because() => await _observer.HandlePage(
    [
        .. Enumerable.Range(0, 5).Select(index => new ObserverPartitionBatch((Key)$"P{index}", [EventAt(10UL + ((ulong)index * 2)), EventAt(11UL + ((ulong)index * 2))]))
    ]);

    [Fact] void should_deliver_two_pages() => _pages.Count.ShouldEqual(2);
    [Fact] void should_deliver_the_last_partition_on_its_own() => _pageSubscriber.Received(1).OnNext((Key)"P4", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_move_the_cursor_past_every_partition() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)20UL);
    [Fact] void should_not_fail_any_partition() => _failedPartitionsState.Partitions.ShouldBeEmpty();

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, LiveDeliveryBudget = 4 };
}
