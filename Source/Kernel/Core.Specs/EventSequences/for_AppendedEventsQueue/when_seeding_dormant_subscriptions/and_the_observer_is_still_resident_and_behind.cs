// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_seeding_dormant_subscriptions;

public class and_the_observer_is_still_resident_and_behind : given.a_queue_with_a_stored_durable_subscription
{
    void Establish()
    {
        _observer.GetState().Returns(new ObserverState { NextEventSequenceNumber = 102UL, RunningState = ObserverRunningState.Active });
        _observer.CatchUp().Returns(_ =>
        {
            _calls.Enqueue("catch_up");
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await _queue.Enqueue([EventAt(105)]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_resume_from_the_cursor_before_delivering_the_doorbell() => _calls.ToArray().ShouldEqual(new[] { "ensure", "catch_up", "handle" });
}
