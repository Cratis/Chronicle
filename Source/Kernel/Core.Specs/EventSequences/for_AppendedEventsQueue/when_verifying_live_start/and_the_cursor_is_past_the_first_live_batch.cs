// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_the_cursor_is_past_the_first_live_batch : given.a_queue_verifying_live_gaps
{
    protected override ObserverFilters? LiveFilters => null;

    void Establish() => _observer.GetState().Returns(new ObserverState { NextEventSequenceNumber = 106UL, RunningState = ObserverRunningState.Active });

    Task Because() => Deliver(EventAt(105));

    [Fact] void should_not_start_another_catch_up() => _observer.DidNotReceive().CatchUp();
    [Fact] void should_deliver_the_batch_for_normal_cursor_filtering() => _handledEvents.ShouldEqual(1);
}
