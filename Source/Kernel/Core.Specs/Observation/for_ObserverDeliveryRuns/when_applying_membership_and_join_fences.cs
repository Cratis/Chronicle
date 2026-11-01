// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverDeliveryRuns;

public class when_applying_membership_and_join_fences : given.a_controlled_subscription
{
    AppendedEvent[] _live;
    AppendedEvent[] _replay;

    void Because()
    {
        _live = ObserverDeliveryRuns.Eligible(_subscription, [Event(_e, 9), Event(_e, 10), Event(_a, 11)]);
        _replay = ObserverDeliveryRuns.Eligible(_subscription, [Event(_e, 9, EventObservationState.Replay)]);
    }

    [Fact] void should_exclude_live_events_below_the_join_fence() => _live.Length.ShouldEqual(2);
    [Fact] void should_include_the_event_at_the_join_fence() => _live[0].Context.SequenceNumber.ShouldEqual((EventSequenceNumber)10);
    [Fact] void should_retain_order_after_applying_the_fence() => _live[1].Context.SequenceNumber.ShouldEqual((EventSequenceNumber)11);
    [Fact] void should_ignore_the_live_join_fence_during_replay() => _replay.Length.ShouldEqual(1);
}
