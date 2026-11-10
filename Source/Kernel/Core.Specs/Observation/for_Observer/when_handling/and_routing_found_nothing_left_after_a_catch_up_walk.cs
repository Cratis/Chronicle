// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// A catch-up walked every partition, and routing has since found nothing left to catch up, so live delivery takes over again.
/// </summary>
public class and_routing_found_nothing_left_after_a_catch_up_walk : given.an_observer_with_a_page_subscriber
{
    async Task Establish()
    {
        await _observer.RegisterCatchingUpPartitions([(Key)"known-when-prepared"]);
        _observer.ResumeLiveDeliveryAfterWalk();
        _pageSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(10UL));
    }

    async Task Because() => await _observer.Handle("first-seen-after-prepared", [EventAt(10UL)]);

    [Fact] void should_deliver_the_event_live() => _pageSubscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_move_the_cursor_past_it() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)11UL);
}
