// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling_a_page;

public class and_the_subscriber_does_not_take_pages : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL, LastHandledEventSequenceNumber = 9UL };
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => ObserverSubscriberResult.Ok(call.ArgAt<IEnumerable<AppendedEvent>>(1).Last().Context.SequenceNumber));
    }

    async Task Because() => await _observer.HandlePage(
    [
        new((Key)"A", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 10UL)]),
        new((Key)"B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 11UL)])
    ]);

    [Fact] void should_deliver_one_partition_at_a_time() => _subscriber.Received(2).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_move_the_cursor_past_the_page() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)12UL);
}
