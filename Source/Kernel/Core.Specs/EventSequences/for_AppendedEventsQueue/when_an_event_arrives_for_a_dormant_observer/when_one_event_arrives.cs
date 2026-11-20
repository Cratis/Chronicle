// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_an_event_arrives_for_a_dormant_observer;

public class when_one_event_arrives : given.a_queue_with_a_stored_durable_subscription
{
    async Task Because()
    {
        await _queue.Enqueue([EventAt(7)]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_activate_the_observer_before_delivering() => _calls.ToArray().ShouldEqual(["ensure", "handle"]);
    [Fact] void should_deliver_the_event_once() => _handledEvents.ShouldEqual(1);
    [Fact] void should_no_longer_hold_it_as_dormant() => _queue.DormantCount.ShouldEqual(0);
}
