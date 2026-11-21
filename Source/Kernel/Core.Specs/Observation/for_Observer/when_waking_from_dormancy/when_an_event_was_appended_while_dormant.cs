// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy;

public class when_an_event_was_appended_while_dormant : given.a_dormant_client_observer
{
    void Establish() => Append(EventAt(0), EventAt(1));

    async Task Because()
    {
        await Wake();
        await RingDoorbell("partition", EventAt(0), EventAt(1));
    }

    [Fact] void should_deliver_once() => _delivered.Count.ShouldEqual(1);
    [Fact] void should_deliver_in_order() => _delivered[0].Events.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([0UL, 1UL]);
    [Fact] void should_move_the_cursor_past_what_it_delivered() => _stateStorage.State.NextEventSequenceNumber.Value.ShouldEqual(2UL);
    [Fact] async Task should_be_subscribed() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] void should_take_the_queue_subscription() => _appendedEventsQueues.Received().Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
    [Fact] void should_not_start_a_catch_up_job() => CheckDidNotStartCatchupJob();
}
