// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Reproduces https://github.com/Cratis/Chronicle/issues/5786: the sweep reads the event appended after it enumerated
/// its partitions, so delivering it live as well would hand the subscriber the same event twice.
/// </summary>
public class and_a_sweeping_catch_up_did_not_register_the_partition : given.an_observer_with_a_page_subscriber_catching_up
{
    async Task Because() => await _observer.Handle(PartitionAppendedAfterEnumeration, [EventAt(12)]);

    [Fact] void should_not_deliver_the_event_live() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_deliver_the_event_in_a_page() => _pageSubscriber.DidNotReceive().OnNextPartitions(Arg.Any<IReadOnlyList<ObserverPartitionBatch>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_leave_the_position_for_the_sweep() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)10UL);
    [Fact] async Task should_keep_holding_back_live_delivery() => (await _observer.IsPreparingCatchup()).ShouldBeTrue();
}
