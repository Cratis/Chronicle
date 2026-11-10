// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling_a_page;

/// <summary>
/// The subscription takes only events from event sources of type "account". The partition of another type is left out of the
/// page and skipped, as it would be one partition at a time.
/// </summary>
public class and_an_event_source_type_filter_excludes_one_partition : given.an_observer_with_a_page_subscriber
{
    void Establish()
    {
        subscription = subscription with { Filters = new ObserverFilters([], new EventSourceType("account")) };
        _observer.SetSubscription(subscription);
        _pageSubscriber.OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(new ObserverPageResult(new Dictionary<Key, ObserverSubscriberResult>
            {
                [(Key)"A"] = ObserverSubscriberResult.Ok(10UL),
                [(Key)"C"] = ObserverSubscriberResult.Ok(12UL)
            }));
    }

    async Task Because() => await _observer.HandlePage(
    [
        new((Key)"A", [OfType(EventAt(10UL), "account")]),
        new((Key)"B", [OfType(EventAt(11UL), "order")]),
        new((Key)"C", [OfType(EventAt(12UL), "account")])
    ]);

    [Fact] void should_deliver_only_the_matching_partitions_in_one_page() => _pageSubscriber.Received(1).OnNextPartitions(Arg.Is<IReadOnlyList<ObserverPartitionBatch>>(_ => _.Select(b => b.Partition).SequenceEqual(new[] { (Key)"A", (Key)"C" })), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_deliver_the_excluded_partition_on_its_own() => _pageSubscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_move_the_cursor_past_every_partition() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);

    static AppendedEvent OfType(AppendedEvent @event, string eventSourceType) => @event with { Context = @event.Context with { EventSourceType = new EventSourceType(eventSourceType) } };
}
