// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_an_event_arrives_for_a_dormant_observer;

public class when_waking_fails : given.a_queue_with_a_stored_durable_subscription
{
    void Establish() => _observer.Ensure().Returns(Task.FromException(new InvalidOperationException("cannot activate")));

    async Task Because()
    {
        await _queue.Enqueue([EventAt(7)]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_not_deliver_to_an_observer_that_did_not_wake() => _handledEvents.ShouldEqual(0);
    [Fact] async Task should_hand_the_observer_to_catch_up_by_dropping_its_subscription() => (await _queue.GetSubscriptions()).ShouldBeEmpty();
}
