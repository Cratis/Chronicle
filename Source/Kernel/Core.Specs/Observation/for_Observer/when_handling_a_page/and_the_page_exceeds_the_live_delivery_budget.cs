// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling_a_page;

/// <summary>
/// Six partitions of two events each come to twelve events, over a live delivery budget of four. They are delivered as three
/// pages of two partitions each, every page within the budget, instead of one partition at a time.
/// </summary>
public class and_the_page_exceeds_the_live_delivery_budget : given.an_observer_with_a_page_subscriber
{
    readonly List<IReadOnlyList<ObserverPartitionBatch>> _pages = [];

    void Establish() =>
        _pageSubscriber.OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var page = call.Arg<IReadOnlyList<ObserverPartitionBatch>>();
                _pages.Add(page);
                return new ObserverPageResult(page.ToDictionary(_ => _.Partition, _ => ObserverSubscriberResult.Ok(_.Events[^1].Context.SequenceNumber)));
            });

    async Task Because() => await _observer.HandlePage(
    [
        .. Enumerable.Range(0, 6).Select(index => new ObserverPartitionBatch((Key)$"P{index}", [EventAt(10UL + ((ulong)index * 2)), EventAt(11UL + ((ulong)index * 2))]))
    ]);

    [Fact] void should_deliver_three_pages() => _pages.Count.ShouldEqual(3);
    [Fact] void should_put_two_partitions_in_each_page() => _pages.ShouldEachConformTo(_ => _.Count == 2);
    [Fact] void should_keep_each_page_within_the_budget() => _pages.ShouldEachConformTo(_ => _.Sum(batch => batch.Events.Count) <= 4);
    [Fact] void should_not_deliver_per_partition() => _pageSubscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_move_the_cursor_past_every_partition() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)22UL);
    [Fact] void should_not_fail_any_partition() => _failedPartitionsState.Partitions.ShouldBeEmpty();

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, LiveDeliveryBudget = 4 };
}
