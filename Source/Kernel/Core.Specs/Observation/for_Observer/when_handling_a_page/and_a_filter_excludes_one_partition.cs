// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling_a_page;

/// <summary>
/// The subscription takes only events tagged "kept". The partition with no such event is left out of the page and skipped,
/// as it would be one partition at a time.
/// </summary>
public class and_a_filter_excludes_one_partition : given.an_observer_with_a_page_subscriber
{
    void Establish()
    {
        subscription = subscription with { Filters = new ObserverFilters(["kept"]) };
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
        new((Key)"A", [Tagged(EventAt(10UL), "kept")]),
        new((Key)"B", [EventAt(11UL)]),
        new((Key)"C", [Tagged(EventAt(12UL), "kept")])
    ]);

    [Fact] void should_deliver_only_the_matching_partitions_in_one_page() => _pageSubscriber.Received(1).OnNextPartitions(Arg.Is<IReadOnlyList<ObserverPartitionBatch>>(_ => _.Select(b => b.Partition).SequenceEqual(new[] { (Key)"A", (Key)"C" })), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_deliver_the_excluded_partition_on_its_own() => _pageSubscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_move_the_cursor_past_every_partition() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);
    [Fact] void should_not_fail_any_partition() => _failedPartitionsState.Partitions.ShouldBeEmpty();

    static AppendedEvent Tagged(AppendedEvent @event, string tag) => @event with { Context = @event.Context with { Tags = [new Tag(tag)] } };
}
