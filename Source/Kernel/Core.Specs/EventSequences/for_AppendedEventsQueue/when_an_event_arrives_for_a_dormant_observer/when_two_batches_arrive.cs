// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_an_event_arrives_for_a_dormant_observer;

public class when_two_batches_arrive : given.a_queue_with_a_stored_durable_subscription
{
    async Task Because()
    {
        await _queue.Enqueue([EventAt(7)]);
        await _queue.Enqueue([EventAt(8)]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_activate_the_observer_once() => _calls.Count(_ => _ == "ensure").ShouldEqual(1);
    [Fact] void should_deliver_every_event() => _handledEvents.ShouldEqual(2);
}
