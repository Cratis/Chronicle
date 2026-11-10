// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling_a_page;

/// <summary>
/// Two client instances serve the subscription, and the selector keeps every partition with one of them. A page goes to one
/// instance, so the partitions of a page all belong to that instance, and no partition is moved to the other one.
/// </summary>
public class and_partitions_are_served_by_two_client_instances : given.an_observer_with_a_page_subscriber
{
    const int Partitions = 12;
    readonly List<IReadOnlyList<ObserverPartitionBatch>> _pages = [];
    readonly RoundRobinObserverSubscriberSelector _selector = new();

    void Establish()
    {
        subscription = subscription with
        {
            Targets =
            [
                new ObserverSubscriberTarget(SiloAddress.Zero, new ConnectedClient { ConnectionId = new ConnectionId("first") }),
                new ObserverSubscriberTarget(SiloAddress.Zero, new ConnectedClient { ConnectionId = new ConnectionId("second") })
            ]
        };
        _observer.SetSubscription(subscription);
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
        .. Enumerable.Range(0, Partitions).Select(index => new ObserverPartitionBatch((Key)$"partition-{index}", [EventAt(10UL + (ulong)index)]))
    ]);

    [Fact] void should_deliver_a_page() => _pages.ShouldNotBeEmpty();
    [Fact] void should_keep_every_page_with_one_instance() => _pages.ShouldEachConformTo(page => page.Select(_ => InstanceOf(_.Partition)).Distinct().Count() == 1);
    [Fact] void should_page_both_instances() => _pages.Select(page => InstanceOf(page[0].Partition)).Distinct().Count().ShouldEqual(2);
    [Fact] void should_move_the_cursor_past_every_partition() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)22UL);
    [Fact] void should_not_fail_any_partition() => _failedPartitionsState.Partitions.ShouldBeEmpty();

    string InstanceOf(Key partition) => _selector.Select(subscription, partition).ConnectedClient!.ConnectionId.Value;
}
