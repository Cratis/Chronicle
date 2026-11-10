// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling_a_page;

/// <summary>
/// With a live delivery budget of four, the partition with six events is over the budget on its own. It is kept out of the
/// page, and the two partitions around it still go together in one page.
/// </summary>
public class and_one_partition_is_over_the_live_delivery_budget : given.an_observer_with_a_page_subscriber
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
        new((Key)"before", [EventAt(10UL), EventAt(11UL)]),
        new((Key)"large", [EventAt(12UL), EventAt(13UL), EventAt(14UL), EventAt(15UL), EventAt(16UL), EventAt(17UL)]),
        new((Key)"after", [EventAt(18UL), EventAt(19UL)])
    ]);

    [Fact] void should_deliver_one_page() => _pages.Count.ShouldEqual(1);
    [Fact] void should_page_the_partitions_around_the_large_one() => _pages[0].Select(_ => _.Partition).ShouldEqual([(Key)"before", (Key)"after"]);

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, LiveDeliveryBudget = 4 };
}
