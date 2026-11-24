// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_a_verified_subscription_wakes_again : given.a_queue_verifying_live_gaps
{
    async Task Because()
    {
        await Deliver(MatchingAt(105));
        _history = [MatchingAt(107)];
        _observer.GetState().Returns(new ObserverState { NextEventSequenceNumber = 106UL, RunningState = ObserverRunningState.Active });
        (await _queue.TryMarkDormant(_observerKey)).ShouldBeTrue();
        await Deliver(MatchingAt(110));
    }

    [Fact] void should_check_again_after_waking() => _observer.Received(2).GetState();
    [Fact] void should_not_deliver_past_the_new_gap() => _handledEvents.ShouldEqual(1);
    [Fact] void should_request_catch_up() => _observer.Received(1).CatchUp();
}
