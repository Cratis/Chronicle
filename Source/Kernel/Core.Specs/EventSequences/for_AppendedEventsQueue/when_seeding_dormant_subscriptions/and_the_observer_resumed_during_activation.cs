// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_seeding_dormant_subscriptions;

public class and_the_observer_resumed_during_activation : given.a_queue_with_a_stored_durable_subscription
{
    void Establish() => _observer.GetState().Returns(new ObserverState { NextEventSequenceNumber = 106UL, RunningState = ObserverRunningState.Active });

    async Task Because()
    {
        await _queue.Enqueue([EventAt(105)]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_not_start_another_catch_up() => _observer.DidNotReceive().CatchUp();
    [Fact] void should_deliver_the_doorbell_for_normal_cursor_filtering() => _handledEvents.ShouldEqual(1);
}
