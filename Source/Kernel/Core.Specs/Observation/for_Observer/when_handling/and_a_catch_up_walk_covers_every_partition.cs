// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// The observer takes pages spanning partitions, so its catch-up walks every partition to the end of the log. The walk
/// registered only the partitions known when it was prepared; an event of a partition that first appears afterwards is
/// still read by the walk, so delivering it live as well would hand it to the subscriber twice.
/// </summary>
public class and_a_catch_up_walk_covers_every_partition : given.an_observer_with_a_page_subscriber
{
    async Task Establish() => await _observer.RegisterCatchingUpPartitions([(Key)"known-when-prepared"]);

    async Task Because() => await _observer.Handle("first-seen-after-prepared", [EventAt(10UL)]);

    [Fact] void should_leave_the_event_to_the_walk() => _pageSubscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_move_the_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)10UL);
}
