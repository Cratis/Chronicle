// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling_a_page;

/// <summary>
/// A client always subscribes with filters, even when they filter nothing: no tags, no event source type and every stream.
/// Such a subscription still takes a page spanning partitions in one call.
/// </summary>
public class and_the_subscription_has_filters_that_exclude_nothing : given.an_observer_with_a_page_subscriber
{
    void Establish()
    {
        subscription = subscription with { Filters = new ObserverFilters([], EventSourceType.Unspecified, EventStreamType.All) };
        _observer.SetSubscription(subscription);
        _pageSubscriber.OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(new ObserverPageResult(new Dictionary<Key, ObserverSubscriberResult>
            {
                [(Key)"A"] = ObserverSubscriberResult.Ok(10UL),
                [(Key)"B"] = ObserverSubscriberResult.Ok(11UL)
            }));
    }

    async Task Because() => await _observer.HandlePage(
    [
        new((Key)"A", [EventAt(10UL)]),
        new((Key)"B", [EventAt(11UL)])
    ]);

    [Fact] void should_deliver_both_partitions_in_one_page() => _pageSubscriber.Received(1).OnNextPartitions(Arg.Is<IReadOnlyList<ObserverPartitionBatch>>(_ => _.Count == 2), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_deliver_per_partition() => _pageSubscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_move_the_cursor_past_the_page() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)12UL);
}
